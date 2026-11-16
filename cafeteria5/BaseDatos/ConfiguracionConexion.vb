Imports System.Collections.Generic
Imports System.IO
Imports System.Text
Imports MySql.Data.MySqlClient

Namespace CafeteriaOS

    ''' <summary>
    ''' Donde esta la base de datos, para ESTA PC.
    '''
    ''' Antes la conexion estaba escrita en el codigo (Server=localhost): servia solo en
    ''' la PC que tiene XAMPP. Desde otra computadora "localhost" es ella misma, que no
    ''' tiene la base, y el programa no arrancaba. Ahora cada PC guarda la suya en
    ''' conexion.ini, al lado del programa:
    '''   - En la PC de XAMPP: Servidor = localhost (lo que viene por defecto).
    '''   - En las demas: Servidor = la IP de la PC de XAMPP (por ejemplo 192.168.0.10).
    ''' Si el archivo no existe, se usan los valores por defecto de XAMPP.
    '''
    ''' La contraseña queda escrita en el archivo: cualquiera con acceso a esta PC puede
    ''' leerla. Por eso conviene un usuario de MySQL solo para el programa, con permiso
    ''' sobre la base cafeteria y nada mas (ver PASO_A_PASO_BASE_DE_DATOS.md).
    ''' </summary>
    Public Class ConfiguracionConexion
        Public Property Servidor As String = "localhost"
        Public Property Puerto As Integer = 3306
        Public Property Base As String = "cafeteria"
        Public Property Usuario As String = "root"
        Public Property Contrasena As String = ""

        Public Shared ReadOnly Property Ruta As String
            Get
                Return Path.Combine(AppContext.BaseDirectory, "conexion.ini")
            End Get
        End Property

        ''' <summary>
        ''' SslMode=Disabled: el MySQL de XAMPP no trae certificados.
        ''' AllowUserVariables: para @usuario_id, que leen los triggers de auditoria.
        ''' ConnectionTimeout corto: si la PC de la base esta apagada o sin red, el aviso
        ''' sale en segundos y no despues de 15.
        ''' </summary>
        Public Function CadenaConexion() As String
            Dim b As New MySqlConnectionStringBuilder With {
                .Server = If(String.IsNullOrWhiteSpace(Servidor), "localhost", Servidor.Trim()),
                .Port = CUInt(If(Puerto > 0, Puerto, 3306)),
                .Database = If(String.IsNullOrWhiteSpace(Base), "cafeteria", Base.Trim()),
                .UserID = If(Usuario, "").Trim(),
                .Password = If(Contrasena, ""),
                .CharacterSet = "utf8mb4",
                .SslMode = MySqlSslMode.Disabled,
                .AllowUserVariables = True,
                .ConnectionTimeout = 5
            }
            Return b.ConnectionString
        End Function

        ''' <summary>
        ''' Traduce los errores tipicos de MySQL a que hay que revisar. El mensaje
        ''' original va al final, por si hace falta buscarlo.
        ''' </summary>
        Public Shared Function Explicar(mensaje As String) As String
            Dim m = If(mensaje, "")
            Dim consejo As String
            If m.Contains("is not allowed to connect") Then
                consejo = "MySQL no deja entrar a este usuario desde esta PC. En la PC de XAMPP hay " &
                          "que crear un usuario para la red (paso 5.2 del paso a paso)."
            ElseIf m.Contains("Access denied") Then
                consejo = "Usuario o contraseña de MySQL incorrectos."
            ElseIf m.Contains("Unknown database") Then
                consejo = "En ese servidor no existe la base de datos. Revisá el nombre (cafeteria) " &
                          "o que estés apuntando a la PC correcta."
            ElseIf m.Contains("Unable to connect") OrElse m.Contains("timeout", StringComparison.OrdinalIgnoreCase) Then
                consejo = "No se encontró el servidor. Revisá la IP, que la PC de XAMPP esté prendida " &
                          "con MySQL en verde, que las dos PCs estén en la misma red y que el firewall " &
                          "de la PC de XAMPP deje pasar el puerto 3306 (paso 5.2 del paso a paso)."
            ElseIf m.Contains("productos") AndAlso m.Contains("doesn't exist") Then
                consejo = "La base existe pero está vacía: falta importar cafeteria.sql."
            Else
                consejo = "Revisá los datos de conexión."
            End If
            Return consejo & vbCrLf & vbCrLf & "Detalle: " & m
        End Function

        Public Shared Function Cargar() As ConfiguracionConexion
            Dim c As New ConfiguracionConexion()
            If Not File.Exists(Ruta) Then Return c
            For Each linea In File.ReadAllLines(Ruta, Encoding.UTF8)
                Dim igual = linea.IndexOf("="c)
                If igual <= 0 OrElse linea.TrimStart().StartsWith("#") Then Continue For
                Dim clave = linea.Substring(0, igual).Trim().ToLowerInvariant()
                Dim valor = linea.Substring(igual + 1).Trim()
                Select Case clave
                    Case "servidor" : c.Servidor = valor
                    Case "puerto"
                        Dim p As Integer
                        If Integer.TryParse(valor, p) Then c.Puerto = p
                    Case "base" : c.Base = valor
                    Case "usuario" : c.Usuario = valor
                    Case "contrasena" : c.Contrasena = valor
                End Select
            Next
            Return c
        End Function

        Public Sub Guardar()
            Dim lineas As New List(Of String) From {
                "# Conexion a la base de datos de la Cafeteria, para ESTA PC.",
                "# En la PC que tiene XAMPP: servidor=localhost.",
                "# En las demas: servidor=IP de la PC de XAMPP (por ejemplo 192.168.0.10).",
                "servidor=" & Servidor,
                "puerto=" & Puerto,
                "base=" & Base,
                "usuario=" & Usuario,
                "contrasena=" & Contrasena
            }
            File.WriteAllLines(Ruta, lineas, New UTF8Encoding(False))
        End Sub
    End Class

End Namespace
