# =====================================================================
#  CAFETERIA - Preparar la PC que guarda la base de datos (la de XAMPP)
#
#  No se ejecuta directamente: hacer doble clic en instalar_servidor.bat
#  (en la carpeta principal del proyecto), que pide permiso de administrador.
#
#  Que hace (se puede ejecutar mas de una vez sin romper nada):
#    1. Deja MySQL de XAMPP como servicio de Windows: arranca solo al prender
#       la PC, sin abrir el panel de XAMPP.
#    2. Si la base "cafeteria" no existe, la crea (cafeteria.sql).
#       Si existe, hace un backup y la actualiza (actualizacion_login_imagenes.sql).
#    3. Carga las fotos de los productos (imagenes_productos.sql).
#    4. Crea el usuario de MySQL "cafeteria" para que entren las otras PCs.
#    5. Abre el puerto de MySQL en el firewall de Windows (solo red privada).
#    6. Muestra la IP y los datos para escribir en las otras PCs.
# =====================================================================

param(
    [string]$Xampp = "C:\xampp",
    [int]$Puerto = 3306,
    [string]$ClaveRoot = "",
    [string]$ClaveRed = "",
    # Para pruebas: no tocar servicios ni firewall, y no esperar Enter.
    [switch]$SinServicio,
    [switch]$SinFirewall,
    [switch]$NoPausa
)

$ErrorActionPreference = "Stop"
$raiz = Split-Path -Parent $PSScriptRoot
$bin = Join-Path $Xampp "mysql\bin"
$mysql = Join-Path $bin "mysql.exe"
$mysqladmin = Join-Path $bin "mysqladmin.exe"
$mysqld = Join-Path $bin "mysqld.exe"
$mysqldump = Join-Path $bin "mysqldump.exe"
$NombreServicio = "mysql"

function Paso($texto) { Write-Host ""; Write-Host "== $texto" -ForegroundColor Cyan }
function Bien($texto) { Write-Host "   OK  $texto" -ForegroundColor Green }
function Aviso($texto) { Write-Host "   !!  $texto" -ForegroundColor Yellow }
function Pausa($texto) { if (-not $NoPausa) { [void](Read-Host $texto) } }
function Falla($texto) {
    Write-Host ""
    Write-Host "   ERROR: $texto" -ForegroundColor Red
    Write-Host "   No se cambio nada mas. Si hace falta, el backup esta en la carpeta backups." -ForegroundColor Red
    Pausa "Enter para salir"
    exit 1
}

function ArgsRoot {
    $a = @("-uroot", "-h127.0.0.1", "-P$Puerto", "--default-character-set=utf8mb4")
    if ($ClaveRoot -ne "") { $a += "-p$ClaveRoot" }
    return $a
}

# Ejecuta una consulta y devuelve el resultado (sin encabezados).
function Sql($consulta) {
    $salida = & $mysql (ArgsRoot) -N -B -e $consulta 2>&1
    if ($LASTEXITCODE -ne 0) { throw "MySQL: $salida" }
    return $salida
}

# Importa un archivo .sql tal cual (sin pasar por la consola, para no romper las tildes).
function Importar($archivo) {
    $errores = [System.IO.Path]::GetTempFileName()
    $p = Start-Process -FilePath $mysql -ArgumentList (ArgsRoot) -RedirectStandardInput $archivo `
                       -RedirectStandardError $errores -NoNewWindow -Wait -PassThru
    $texto = Get-Content $errores -Raw
    Remove-Item $errores -ErrorAction SilentlyContinue
    if ($p.ExitCode -ne 0) { throw "Al importar $(Split-Path -Leaf $archivo): $texto" }
}

function Conecta {
    & $mysqladmin (ArgsRoot) ping 2>&1 | Out-Null
    return ($LASTEXITCODE -eq 0)
}

function EsAdministrador {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    return (New-Object Security.Principal.WindowsPrincipal $id).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)
}

Write-Host "==============================================================="
Write-Host "  Cafeteria - preparar la PC de la base de datos"
Write-Host "==============================================================="

# ---------------------------------------------------------------------
Paso "Revisando XAMPP y los archivos del proyecto"
if (-not (Test-Path $mysql)) { Falla "No encuentro MySQL de XAMPP en $bin. Si XAMPP esta en otra carpeta, avisame." }
foreach ($f in @("cafeteria.sql", "actualizacion_login_imagenes.sql", "imagenes_productos.sql")) {
    if (-not (Test-Path (Join-Path $raiz $f))) { Falla "Falta el archivo $f en $raiz. Baja el proyecto completo de GitHub." }
}
if ((-not $SinServicio -or -not $SinFirewall) -and -not (EsAdministrador)) {
    Falla "Hace falta permiso de administrador. Ejecuta instalar_servidor.bat con doble clic."
}
Bien "XAMPP en $Xampp"

# ---------------------------------------------------------------------
Paso "1. MySQL como servicio de Windows (arranca solo con la PC)"
if ($SinServicio) {
    Aviso "Salteado (modo prueba)."
} else {
    $servicio = Get-Service -Name $NombreServicio -ErrorAction SilentlyContinue
    $corriendoSuelto = (Get-Process mysqld -ErrorAction SilentlyContinue) -and
                       (-not $servicio -or $servicio.Status -ne "Running")
    if ($corriendoSuelto) {
        Write-Host "   MySQL esta abierto desde el panel de XAMPP: lo cierro para pasarlo a servicio."
        Write-Host "   (Si el panel de XAMPP esta abierto, despues va a mostrar MySQL como servicio.)"
        & $mysqladmin (ArgsRoot) shutdown 2>&1 | Out-Null
        Start-Sleep -Seconds 3
    }
    if (-not $servicio) {
        & $mysqld --install $NombreServicio "--defaults-file=$(Join-Path $bin 'my.ini')" | Out-Null
        if ($LASTEXITCODE -ne 0) { Falla "No se pudo crear el servicio de MySQL." }
        $servicio = Get-Service -Name $NombreServicio
    }
    Set-Service -Name $NombreServicio -StartupType Automatic
    if ($servicio.Status -ne "Running") { Start-Service -Name $NombreServicio }
    Bien "Servicio '$NombreServicio' en inicio automatico."
}

$listo = $false
for ($i = 0; $i -lt 30; $i++) {
    if (Conecta) { $listo = $true; break }
    Start-Sleep -Seconds 1
}
if (-not $listo) { Falla "MySQL no responde en el puerto $Puerto. Si root tiene contrasena, ejecuta con -ClaveRoot." }
Bien "MySQL responde."

# ---------------------------------------------------------------------
Paso "2. Base de datos 'cafeteria'"
try {
    $existe = [int](Sql "SELECT COUNT(*) FROM information_schema.schemata WHERE schema_name = 'cafeteria'")
    if ($existe -eq 0) {
        Write-Host "   No existe: la creo desde cero con cafeteria.sql."
        Importar (Join-Path $raiz "cafeteria.sql")
        Bien "Base creada con los datos de ejemplo."
    } else {
        Write-Host "   Ya existe: antes de tocarla hago un backup."
        Write-Host "   IMPORTANTE: el programa tiene que estar CERRADO en todas las PCs." -ForegroundColor Yellow
        Pausa "   Cuando este cerrado en todas, apreta Enter"

        $carpeta = Join-Path $raiz "backups"
        New-Item -ItemType Directory -Force $carpeta | Out-Null
        $backup = Join-Path $carpeta ("cafeteria_" + (Get-Date -Format "yyyy-MM-dd_HH-mm-ss") + ".sql")
        $err = [System.IO.Path]::GetTempFileName()
        $p = Start-Process -FilePath $mysqldump `
             -ArgumentList ((ArgsRoot) + @("--single-transaction", "--routines", "--triggers", "--add-drop-table", "--databases", "cafeteria")) `
             -RedirectStandardOutput $backup -RedirectStandardError $err -NoNewWindow -Wait -PassThru
        if ($p.ExitCode -ne 0 -or (Get-Item $backup).Length -lt 1000) {
            Falla "No se pudo hacer el backup: $(Get-Content $err -Raw). No toque la base."
        }
        Remove-Item $err -ErrorAction SilentlyContinue
        Bien "Backup en $backup"

        $decimales = [int](Sql @"
SELECT (SELECT COUNT(*) FROM cafeteria.productos WHERE stock_actual <> ROUND(stock_actual) OR stock_minimo <> ROUND(stock_minimo))
     + (SELECT COUNT(*) FROM cafeteria.detalle_ventas WHERE cantidad <> ROUND(cantidad))
     + (SELECT COUNT(*) FROM cafeteria.detalle_pedidos WHERE cantidad <> ROUND(cantidad))
     + (SELECT COUNT(*) FROM cafeteria.detalle_ingresos WHERE cantidad <> ROUND(cantidad))
     + (SELECT COUNT(*) FROM cafeteria.movimientos_stock WHERE cantidad <> ROUND(cantidad))
"@)
        if ($decimales -gt 0) { Falla "Hay $decimales cantidades con decimales; pasarlas a enteras las redondearia. Avisame antes de seguir." }

        Importar (Join-Path $raiz "actualizacion_login_imagenes.sql")
        Bien "Base actualizada (no se borro ningun dato)."
    }
} catch {
    Falla $_.Exception.Message
}

# ---------------------------------------------------------------------
Paso "3. Fotos de los productos"
try {
    Importar (Join-Path $raiz "imagenes_productos.sql")
    Bien "Fotos cargadas en los productos que no tenian."
} catch {
    Falla $_.Exception.Message
}

# ---------------------------------------------------------------------
Paso "4. Usuario de MySQL para las otras PCs"
# Las tablas de permisos de MySQL (base "mysql") se danan cuando MySQL se cierra de
# golpe (apagon, PC apagada con XAMPP abierto), y entonces no se puede crear el
# usuario ("Index for table 'db' is corrupt"). --auto-repair repara solo las danadas.
& (Join-Path $bin "mysqlcheck.exe") (ArgsRoot) --check --auto-repair --databases mysql 2>&1 | Out-Null
try {
    $hay = [int](Sql "SELECT COUNT(*) FROM mysql.user WHERE user = 'cafeteria' AND host = '%'")
    if ($hay -gt 0 -and $ClaveRed -eq "") {
        $ClaveRed = $null
        Bien "El usuario 'cafeteria' ya existia: no le cambio la contrasena (esta en conexion_para_otras_pcs.txt)."
    } else {
        if ($ClaveRed -eq "") {
            # Letras y numeros, sin caracteres raros para que no haya problemas al escribirla.
            $letras = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKMNPQRSTUVWXYZ23456789".ToCharArray()
            $ClaveRed = -join (1..14 | ForEach-Object { $letras | Get-Random })
        }
        if ($ClaveRed -notmatch '^[A-Za-z0-9]+$') { Falla "La contrasena de red solo puede tener letras y numeros." }
        if ($hay -gt 0) {
            Sql "ALTER USER 'cafeteria'@'%' IDENTIFIED BY '$ClaveRed'" | Out-Null
        } else {
            Sql "CREATE USER 'cafeteria'@'%' IDENTIFIED BY '$ClaveRed'" | Out-Null
        }
        try {
            Sql "GRANT ALL PRIVILEGES ON cafeteria.* TO 'cafeteria'@'%'; FLUSH PRIVILEGES;" | Out-Null
        } catch {
            # Sin permisos el usuario no sirve, y su contrasena no llego a mostrarse:
            # si quedara creado, la proxima vez se lo daria por bueno sin saber la clave.
            if ($hay -eq 0) { try { Sql "DROP USER 'cafeteria'@'%'" | Out-Null } catch { } }
            throw
        }
        Bien "Usuario 'cafeteria' listo (solo tiene permiso sobre la base cafeteria)."
    }
} catch {
    Falla $_.Exception.Message
}

# ---------------------------------------------------------------------
Paso "5. Firewall de Windows"
if ($SinFirewall) {
    Aviso "Salteado (modo prueba)."
} else {
    if (-not (Get-NetFirewallRule -DisplayName "MySQL Cafeteria" -ErrorAction SilentlyContinue)) {
        New-NetFirewallRule -DisplayName "MySQL Cafeteria" -Direction Inbound -Protocol TCP `
            -LocalPort $Puerto -Action Allow -Profile Private,Domain | Out-Null
    }
    Bien "Puerto $Puerto abierto para la red privada."

    $publicas = Get-NetConnectionProfile | Where-Object { $_.NetworkCategory -eq "Public" }
    foreach ($red in $publicas) {
        Aviso "La red '$($red.Name)' esta como PUBLICA: Windows no deja entrar a las otras PCs."
        if ($NoPausa) { continue }
        $r = Read-Host "   Es la red del negocio (no un wifi publico)? Pasarla a PRIVADA? (S/N)"
        if ($r -match '^[sS]') {
            Set-NetConnectionProfile -InterfaceIndex $red.InterfaceIndex -NetworkCategory Private
            Bien "Red '$($red.Name)' ahora es privada."
        }
    }
}

# ---------------------------------------------------------------------
Paso "6. Datos para las otras PCs"
$ips = Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
       Where-Object { $_.IPAddress -notlike "127.*" -and $_.IPAddress -notlike "169.254.*" } |
       Select-Object -ExpandProperty IPAddress
$ip = ($ips | Select-Object -First 1)

Write-Host ""
Write-Host "   En cada una de las OTRAS PCs, al abrir el programa aparece la ventana" -ForegroundColor White
Write-Host "   'Conexion con la base de datos'. Escribi:" -ForegroundColor White
Write-Host ""
Write-Host "      Servidor (IP):  $ip" -ForegroundColor Green
if (@($ips).Count -gt 1) { Write-Host "                      (si no anda, proba con: $(@($ips) -join ', '))" }
Write-Host "      Puerto:         $Puerto" -ForegroundColor Green
Write-Host "      Base de datos:  cafeteria" -ForegroundColor Green
Write-Host "      Usuario MySQL:  cafeteria" -ForegroundColor Green
if ($ClaveRed) {
    Write-Host "      Contrasena:     $ClaveRed" -ForegroundColor Green
    $ini = Join-Path $raiz "conexion_para_otras_pcs.txt"
    @(
        "Datos para la ventana 'Conexion con la base de datos' de las otras PCs",
        "(generado el $(Get-Date -Format 'dd/MM/yyyy HH:mm')). GUARDAR EN LUGAR SEGURO.",
        "",
        "Servidor (IP):  $ip",
        "Puerto:         $Puerto",
        "Base de datos:  cafeteria",
        "Usuario MySQL:  cafeteria",
        "Contrasena:     $ClaveRed"
    ) | Set-Content -Path $ini -Encoding UTF8
    Write-Host ""
    Write-Host "   Tambien quedaron anotados en: $ini"
} else {
    Write-Host "      Contrasena:     la que se genero la primera vez (no se cambio, para que" -ForegroundColor Green
    Write-Host "                      las PCs que ya la tienen sigan andando). Esta anotada en:" -ForegroundColor Green
    Write-Host "                      $(Join-Path $raiz 'conexion_para_otras_pcs.txt')" -ForegroundColor Green
}
Write-Host ""
Write-Host "   En ESTA PC no hay que escribir nada: el programa usa localhost."
Write-Host "   Conviene que el router le de siempre la misma IP a esta PC (reserva DHCP)."
Write-Host ""
Write-Host "==============================================================="
Write-Host "  Listo." -ForegroundColor Green
Write-Host "==============================================================="
Pausa "Enter para cerrar"
