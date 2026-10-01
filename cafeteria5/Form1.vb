Imports System
Imports System.Windows.Forms
Imports System.Collections.Generic
Imports System.Text
Imports System.Linq
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Printing
Imports System.IO
Imports System.Media
Imports MySql.Data.MySqlClient

' ══════════════════════════════════════════════════════
'  PUNTO DE ENTRADA
' ══════════════════════════════════════════════════════
Public Module Program
    <STAThread>
    Sub Main()
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        Application.Run(New LoginForm())
    End Sub
End Module

' ══════════════════════════════════════════════════════
'  ENUMS
' ══════════════════════════════════════════════════════
Public Enum RolUsuario
    Administrador
    Cajero
    Cocina
End Enum

Public Enum EstadoPedido
    EnPreparacion
    Listo
    Entregado
    Cancelado
End Enum

' ══════════════════════════════════════════════════════
'  TEMA
' ══════════════════════════════════════════════════════
Public Module Tema
    Public ModoOscuro As Boolean = False

    Public ReadOnly Property BgPrincipal As Color
        Get
            Return If(ModoOscuro, Color.FromArgb(18, 18, 18), Color.FromArgb(245, 240, 232))
        End Get
    End Property
    Public ReadOnly Property Surface As Color
        Get
            Return If(ModoOscuro, Color.FromArgb(30, 30, 30), Color.FromArgb(255, 255, 255))
        End Get
    End Property
    Public ReadOnly Property Surface2 As Color
        Get
            Return If(ModoOscuro, Color.FromArgb(40, 40, 40), Color.FromArgb(250, 247, 242))
        End Get
    End Property
    Public ReadOnly Property Surface3 As Color
        Get
            Return If(ModoOscuro, Color.FromArgb(55, 55, 55), Color.FromArgb(240, 234, 216))
        End Get
    End Property
    Public ReadOnly Property TextoPrinc As Color
        Get
            Return If(ModoOscuro, Color.FromArgb(230, 220, 210), Color.FromArgb(42, 26, 10))
        End Get
    End Property
    Public ReadOnly Property TextoSec As Color
        Get
            Return If(ModoOscuro, Color.FromArgb(170, 150, 130), Color.FromArgb(107, 80, 64))
        End Get
    End Property
    Public ReadOnly Property TextoTer As Color
        Get
            Return If(ModoOscuro, Color.FromArgb(120, 100, 80), Color.FromArgb(160, 128, 96))
        End Get
    End Property
    Public ReadOnly Property Borde As Color
        Get
            Return If(ModoOscuro, Color.FromArgb(60, 50, 40), Color.FromArgb(221, 208, 184))
        End Get
    End Property
    Public ReadOnly Property SidebarBg As Color
        Get
            Return If(ModoOscuro, Color.FromArgb(10, 8, 5), Color.FromArgb(28, 18, 8))
        End Get
    End Property

    Public ReadOnly Acento As Color = Color.FromArgb(107, 58, 31)
    Public ReadOnly Acento2 As Color = Color.FromArgb(193, 127, 58)
    Public ReadOnly Acento3 As Color = Color.FromArgb(232, 201, 122)
    Public ReadOnly Verde As Color = Color.FromArgb(46, 125, 82)
    Public ReadOnly Rojo As Color = Color.FromArgb(192, 57, 43)
    Public ReadOnly Azul As Color = Color.FromArgb(36, 113, 163)
    Public ReadOnly Naranja As Color = Color.FromArgb(214, 137, 16)

    Public ReadOnly FntBold As New Font("Segoe UI", 9, FontStyle.Bold)
    Public ReadOnly FntReg As New Font("Segoe UI", 9, FontStyle.Regular)
    Public ReadOnly FntInput As New Font("Segoe UI", 10, FontStyle.Regular)
    Public ReadOnly FntBtn As New Font("Segoe UI", 10, FontStyle.Bold)
    Public ReadOnly FntMono As New Font("Courier New", 9, FontStyle.Regular)
    Public ReadOnly FntSmall As New Font("Segoe UI", 8, FontStyle.Regular)
    Public ReadOnly FntSmallB As New Font("Segoe UI", 8, FontStyle.Bold)

    Public Function CrearBoton(texto As String, bg As Color, fg As Color,
                               Optional w As Integer = 200,
                               Optional h As Integer = 40) As Button
        Dim btn As New Button With {
            .Text = texto, .Size = New Size(w, h),
            .BackColor = bg, .ForeColor = fg,
            .Font = FntBtn, .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand,
            .TextAlign = ContentAlignment.MiddleCenter
        }
        btn.FlatAppearance.BorderSize = 0
        btn.Tag = bg
        AddHandler btn.MouseEnter, AddressOf BtnHoverIn
        AddHandler btn.MouseLeave, AddressOf BtnHoverOut
        Return btn
    End Function

    Public Sub BtnHoverIn(s As Object, e As EventArgs)
        Dim b = CType(s, Button)
        If TypeOf b.Tag Is Color Then
            Dim c As Color = CType(b.Tag, Color)
            b.BackColor = Color.FromArgb(Math.Max(0, c.R - 20),
                                         Math.Max(0, c.G - 20),
                                         Math.Max(0, c.B - 20))
        End If
    End Sub

    Public Sub BtnHoverOut(s As Object, e As EventArgs)
        Dim b = CType(s, Button)
        If TypeOf b.Tag Is Color Then
            b.BackColor = CType(b.Tag, Color)
        End If
    End Sub

    Public Function CrearInput(Optional pw As Boolean = False) As TextBox
        Return New TextBox With {
            .Font = FntInput, .BackColor = Surface2,
            .ForeColor = TextoPrinc, .BorderStyle = BorderStyle.FixedSingle,
            .UseSystemPasswordChar = pw, .Height = 32
        }
    End Function

    Public Function CrearLabel(texto As String,
                               Optional bold As Boolean = False,
                               Optional sz As Single = 9) As Label
        Dim fnt As New Font("Segoe UI", sz, If(bold, FontStyle.Bold, FontStyle.Regular))
        Return New Label With {
            .Text = texto, .Font = fnt,
            .ForeColor = If(bold, TextoPrinc, TextoSec),
            .BackColor = Color.Transparent, .AutoSize = True
        }
    End Function

    Public Function MkCard(x As Integer, y As Integer, w As Integer, h As Integer) As Panel
        Return New Panel With {
            .Location = New Point(x, y), .Size = New Size(w, h),
            .BackColor = Surface
        }
    End Function

    Public Function MkSep(x As Integer, y As Integer, w As Integer) As Panel
        Return New Panel With {
            .Location = New Point(x, y), .Size = New Size(w, 1),
            .BackColor = Borde
        }
    End Function
End Module

' ══════════════════════════════════════════════════════
'  CONFIGURACION SIMPLE SIN BASE DE DATOS
' ══════════════════════════════════════════════════════
Public Module AppConfig
    Private ReadOnly Property RutaLogin As String
        Get
            Return Path.Combine(Application.StartupPath, "login_autoguardado.txt")
        End Get
    End Property

    Public Sub GuardarLogin(usuario As String, rol As String, recordar As Boolean)
        Try
            If recordar Then
                File.WriteAllLines(RutaLogin, New String() {usuario, rol}, Encoding.UTF8)
            ElseIf File.Exists(RutaLogin) Then
                File.Delete(RutaLogin)
            End If
        Catch
        End Try
    End Sub

    Public Sub CargarLogin(txtUsuario As TextBox, cmbRol As ComboBox, chkRecordar As CheckBox)
        Try
            If Not File.Exists(RutaLogin) Then Return
            Dim datos = File.ReadAllLines(RutaLogin, Encoding.UTF8)
            If datos.Length > 0 Then txtUsuario.Text = datos(0)
            If datos.Length > 1 AndAlso cmbRol.Items.Contains(datos(1)) Then cmbRol.SelectedItem = datos(1)
            chkRecordar.Checked = True
        Catch
        End Try
    End Sub
End Module
' ══════════════════════════════════════════════════════
'  MODELOS
' ══════════════════════════════════════════════════════
Public Class EspecificacionGrupo
    Public Property Etiqueta As String
    Public Property Opciones As String()
End Class

Public Class Producto
    Public Property ID As Integer
    Public Property Nombre As String
    Public Property Emoji As String
    Public Property Precio As Decimal
    Public Property Stock As Integer
    Public Property Categoria As String
    Public Property Descripcion As String
    Public Property Specs As New List(Of EspecificacionGrupo)()
End Class

Public Class DetallePedido
    Public Property Producto As Producto
    Public Property Cantidad As Integer
    Public Property Especificaciones As String
    Public ReadOnly Property Subtotal As Decimal
        Get
            Return Producto.Precio * Cantidad
        End Get
    End Property
End Class

Public Class Pedido
    Public Property ID As Integer
    Public Property NombreCliente As String
    Public Property TelefonoCliente As String
    Public Property TipoServicio As String
    Public Property Mesa As String
    Public Property Items As New List(Of DetallePedido)()
    Public Property Estado As EstadoPedido = EstadoPedido.EnPreparacion
    Public Property MetodoPago As String = "Efectivo"
    Public Property FechaHora As DateTime = DateTime.Now
    Public Property Descuento As Decimal = 0
    Public Property UsuarioResponsable As String = ""
    Public Property MotivoCancelacion As String = ""
    Public ReadOnly Property Subtotal As Decimal
        Get
            Dim s As Decimal = 0
            For Each i In Items
                s += i.Subtotal
            Next
            Return s
        End Get
    End Property
    Public ReadOnly Property Total As Decimal
        Get
            Return Math.Max(0, Subtotal - Descuento)
        End Get
    End Property
End Class

Public Class ClienteRegistrado
    Public Property Nombre As String
    Public Property Telefono As String
    Public Property CantidadPedidos As Integer
    Public Property TotalGastado As Decimal
    Public Property UltimoPedido As Date
    Public Property MetodoPagoHabitual As String
    Public Property HistorialPedidos As New List(Of Integer)()
End Class

Public Class Usuario
    Public Property Email As String
    Public Property Telefono As String
    Public Property Password As String
    Public Property Rol As RolUsuario
    Public Property Nombre As String
End Class

Public Class MovimientoCaja
    Public Property Tipo As String ' "Apertura", "Venta", "Egreso", "Cierre"
    Public Property Monto As Decimal
    Public Property Descripcion As String
    Public Property FechaHora As DateTime = DateTime.Now
    Public Property Usuario As String
End Class

Public Class SesionCaja
    Public Property Abierta As Boolean = False
    Public Property MontoInicial As Decimal = 0
    Public Property FechaApertura As DateTime
    Public Property UsuarioApertura As String
    Public Property Movimientos As New List(Of MovimientoCaja)()
    Public ReadOnly Property TotalVentas As Decimal
        Get
            Return Movimientos.Where(Function(m) m.Tipo = "Venta").Sum(Function(m) m.Monto)
        End Get
    End Property
    Public ReadOnly Property TotalEgresos As Decimal
        Get
            Return Movimientos.Where(Function(m) m.Tipo = "Egreso").Sum(Function(m) m.Monto)
        End Get
    End Property
    Public ReadOnly Property SaldoActual As Decimal
        Get
            Return MontoInicial + TotalVentas - TotalEgresos
        End Get
    End Property
End Class

Public Class ContactoProveedor
    Public Property Nombre As String
    Public Property Cargo As String
    Public Property Email As String
    Public Property Telefono As String
End Class

Public Class TelefonoProveedor
    Public Property Tipo As String
    Public Property Numero As String
End Class

Public Class ArchivoProveedor
    Public Property Nombre As String
    Public Property Ruta As String
    Public Property Fecha As DateTime = DateTime.Now
End Class

Public Class MovimientoProveedor
    Public Property Fecha As DateTime = DateTime.Now
    Public Property Tipo As String
    Public Property Descripcion As String
    Public Property Monto As Decimal
    Public Property Vencimiento As DateTime?
    Public Property Pagado As Boolean = False
End Class

Public Class Proveedor
    Public Property Codigo As String
    Public Property Nombre As String
    Public Property Domicilio As String
    Public Property CodigoPostal As String
    Public Property TelefonoLaboral As String
    Public Property TelefonoParticular As String
    Public Property Celular As String
    Public Property Fax As String
    Public Property ContactoPrincipal As String
    Public Property TipoIVA As String
    Public Property CUIT As String
    Public Property TipoDocumento As String
    Public Property NumeroDocumento As String
    Public Property Pais As String = "Argentina"
    Public Property Provincia As String
    Public Property Localidad As String
    Public Property Actividad As String
    Public Property Categoria As String
    Public Property ObservacionesRapidas As String
    Public Property Activo As Boolean = True
    Public Property Email As String
    Public Property SitioWeb As String
    Public Property FechaAlta As DateTime = DateTime.Now
    Public Property CondicionComercial As String
    Public Property FormaPagoHabitual As String
    Public Property Banco As String
    Public Property CBU As String
    Public Property [Alias] As String
    Public Property Observaciones As String
    Public Property NotasInternas As String
    Public Property DescuentoGeneral As Decimal
    Public Property DescuentoPorVolumen As Decimal
    Public Property ObservacionesComerciales As String
    Public Property TotalCompras As Decimal
    Public Property CantidadOrdenes As Integer
    Public Property UltimaCompra As DateTime?
    Public Property SaldoPendiente As Decimal
    Public Property FechaUltimaOperacion As DateTime?
    Public Property CreadoPor As String
    Public Property ModificadoPor As String
    Public Property FechaCreacion As DateTime = DateTime.Now
    Public Property FechaModificacion As DateTime = DateTime.Now
    Public Property HistorialCambios As New List(Of String)()
    Public Property Contactos As New List(Of ContactoProveedor)()
    Public Property Telefonos As New List(Of TelefonoProveedor)()
    Public Property Archivos As New List(Of ArchivoProveedor)()
    Public Property Movimientos As New List(Of MovimientoProveedor)()
End Class

' ══════════════════════════════════════════════════════
'  DATOS GLOBALES
' ══════════════════════════════════════════════════════
Public Module Datos
    Public ListaPedidos As New List(Of Pedido)()
    Public ListaClientes As New List(Of ClienteRegistrado)()
    Public UsuarioActual As Usuario = Nothing
    Public Caja As New SesionCaja()
    Public ListaProveedores As New List(Of Proveedor) From {
        New Proveedor With {.Codigo = "PRV001", .Nombre = "Distribuidora Argentina", .CUIT = "30-12345678-9", .TelefonoLaboral = "4444-3333", .Celular = "3764-111111", .Domicilio = "Cordoba 234", .CodigoPostal = "1824", .Pais = "Argentina", .Provincia = "Buenos Aires", .Localidad = "Lanus Oeste", .Actividad = "Distribuidora", .Categoria = "Mayorista", .TipoIVA = "Resp. Inscripto", .ContactoPrincipal = "Perez", .Email = "ventas@distribuidora.com", .FormaPagoHabitual = "Transferencia", .TotalCompras = 250000, .CantidadOrdenes = 8, .UltimaCompra = DateTime.Today.AddDays(-4), .SaldoPendiente = 15000, .FechaUltimaOperacion = DateTime.Today.AddDays(-4), .CreadoPor = "Sistema", .ModificadoPor = "Sistema", .Contactos = New List(Of ContactoProveedor) From {New ContactoProveedor With {.Nombre = "Perez", .Cargo = "Ventas", .Email = "ventas@distribuidora.com", .Telefono = "4444-3333"}}, .Telefonos = New List(Of TelefonoProveedor) From {New TelefonoProveedor With {.Tipo = "Laboral", .Numero = "4444-3333"}}, .Movimientos = New List(Of MovimientoProveedor) From {New MovimientoProveedor With {.Tipo = "Factura", .Descripcion = "Compra de cafe e insumos", .Monto = 15000, .Vencimiento = DateTime.Today.AddDays(-2), .Pagado = False}}},
        New Proveedor With {.Codigo = "PRV002", .Nombre = "Panificados Mitre", .CUIT = "27-87654321-5", .TelefonoLaboral = "3764-222222", .Celular = "3764-333333", .Domicilio = "Mitre 890", .Provincia = "Misiones", .Localidad = "Posadas", .Actividad = "Panaderia", .Categoria = "Panificados", .ContactoPrincipal = "Laura Benitez", .Email = "pedidos@panificadosmitre.com", .FormaPagoHabitual = "Efectivo", .TotalCompras = 120000, .CantidadOrdenes = 14, .UltimaCompra = DateTime.Today.AddDays(-1), .SaldoPendiente = 0, .CreadoPor = "Sistema", .ModificadoPor = "Sistema"},
        New Proveedor With {.Codigo = "PRV003", .Nombre = "Bebidas del Norte", .CUIT = "20-11223344-6", .TelefonoLaboral = "3764-555555", .Domicilio = "Ruta 12 km 8", .Provincia = "Misiones", .Localidad = "Garupa", .Actividad = "Bebidas", .Categoria = "Bebidas", .ContactoPrincipal = "Carlos Diaz", .Email = "admin@bebidasnorte.com", .FormaPagoHabitual = "Cuenta corriente", .TotalCompras = 98000, .CantidadOrdenes = 5, .UltimaCompra = DateTime.Today.AddDays(-12), .SaldoPendiente = 8500, .CreadoPor = "Sistema", .ModificadoPor = "Sistema"}
    }
    Public BitacoraProveedores As New List(Of String)()

    Public ListaProductos As New List(Of Producto) From {
        New Producto With {.ID = 1, .Nombre = "Cafe negro", .Emoji = "Ctrl+N", .Precio = 3000, .Stock = 45, .Categoria = "Cafes", .Descripcion = "Cafe solo"},
        New Producto With {.ID = 2, .Nombre = "Cafe con leche", .Emoji = "Ctrl+C", .Precio = 3500, .Stock = 40, .Categoria = "Cafes", .Descripcion = "Cafe con leche", .Specs = New List(Of EspecificacionGrupo) From {New EspecificacionGrupo With {.Etiqueta = "Leche", .Opciones = New String() {"Entera", "Descremada", "Vegetal"}}, New EspecificacionGrupo With {.Etiqueta = "Azucar", .Opciones = New String() {"Sin azucar", "Normal", "Edulcorante"}}}},
        New Producto With {.ID = 3, .Nombre = "Te/mate cocido solo", .Emoji = "Ctrl+T", .Precio = 3000, .Stock = 60, .Categoria = "Cafes", .Descripcion = "Infusion sola", .Specs = New List(Of EspecificacionGrupo) From {New EspecificacionGrupo With {.Etiqueta = "Infusion", .Opciones = New String() {"Te", "Mate cocido"}}}},
        New Producto With {.ID = 4, .Nombre = "Te/mate cocido con leche", .Emoji = "Ctrl+L", .Precio = 3500, .Stock = 55, .Categoria = "Cafes", .Descripcion = "Infusion con leche"},
        New Producto With {.ID = 5, .Nombre = "Capuccino", .Emoji = "Ctrl+P", .Precio = 4000, .Stock = 35, .Categoria = "Cafes", .Descripcion = "Con espuma de leche"},
        New Producto With {.ID = 6, .Nombre = "Submarino", .Emoji = "Ctrl+S", .Precio = 4500, .Stock = 25, .Categoria = "Cafes", .Descripcion = "Leche caliente con chocolate"},
        New Producto With {.ID = 7, .Nombre = "Chocolatada fria/caliente", .Emoji = "Ctrl+H", .Precio = 4000, .Stock = 30, .Categoria = "Cafes", .Descripcion = "Chocolatada a eleccion", .Specs = New List(Of EspecificacionGrupo) From {New EspecificacionGrupo With {.Etiqueta = "Temperatura", .Opciones = New String() {"Fria", "Caliente"}}}},
        New Producto With {.ID = 8, .Nombre = "Frapuccino", .Emoji = "Ctrl+F", .Precio = 4000, .Stock = 28, .Categoria = "Cafes", .Descripcion = "Cafe frio batido"},
        New Producto With {.ID = 9, .Nombre = "Cafe c/leche + 2 medialunas", .Emoji = "Promo", .Precio = 5000, .Stock = 25, .Categoria = "Promos", .Descripcion = "Promo desayuno"},
        New Producto With {.ID = 10, .Nombre = "Cafe c/leche + chipas", .Emoji = "Promo", .Precio = 5000, .Stock = 25, .Categoria = "Promos", .Descripcion = "Promo con chipas"},
        New Producto With {.ID = 11, .Nombre = "Cafe c/leche + tostado JYQ", .Emoji = "Promo", .Precio = 6000, .Stock = 20, .Categoria = "Promos", .Descripcion = "Promo con tostado"},
        New Producto With {.ID = 12, .Nombre = "Cafe c/leche + 2 medialunas + jugo", .Emoji = "Promo", .Precio = 6000, .Stock = 20, .Categoria = "Promos", .Descripcion = "Promo completa"},
        New Producto With {.ID = 13, .Nombre = "Exprimido chico", .Emoji = "Promo", .Precio = 0, .Stock = 20, .Categoria = "Promos", .Descripcion = "Precio pendiente de cargar"},
        New Producto With {.ID = 14, .Nombre = "Cocido quemado + mbeju", .Emoji = "Promo", .Precio = 6000, .Stock = 20, .Categoria = "Promos", .Descripcion = "Promo regional"},
        New Producto With {.ID = 15, .Nombre = "Frapuccino + 2 medialunas", .Emoji = "Promo", .Precio = 5500, .Stock = 20, .Categoria = "Promos", .Descripcion = "Promo fria"},
        New Producto With {.ID = 16, .Nombre = "Chipa", .Emoji = "Ctrl+A", .Precio = 100, .Stock = 100, .Categoria = "Panificados", .Descripcion = "Chipa por unidad"},
        New Producto With {.ID = 17, .Nombre = "Chipa 4 unidades", .Emoji = "4x", .Precio = 3500, .Stock = 50, .Categoria = "Panificados", .Descripcion = "Combo 4 chipas"},
        New Producto With {.ID = 18, .Nombre = "Chipitas 100g", .Emoji = "100g", .Precio = 2000, .Stock = 40, .Categoria = "Panificados", .Descripcion = "Chipitas por 100g"},
        New Producto With {.ID = 19, .Nombre = "Chipitas 250g", .Emoji = "250g", .Precio = 4500, .Stock = 30, .Categoria = "Panificados", .Descripcion = "Chipitas por 250g"},
        New Producto With {.ID = 20, .Nombre = "Mbeju", .Emoji = "Ctrl+M", .Precio = 3500, .Stock = 20, .Categoria = "Panificados", .Descripcion = "Mbeju"},
        New Producto With {.ID = 21, .Nombre = "Medialuna", .Emoji = "Ctrl+D", .Precio = 1000, .Stock = 40, .Categoria = "Panificados", .Descripcion = "Medialuna"},
        New Producto With {.ID = 22, .Nombre = "Medialuna c/JYQ", .Emoji = "JYQ", .Precio = 1500, .Stock = 30, .Categoria = "Panificados", .Descripcion = "Medialuna con jamon y queso"},
        New Producto With {.ID = 23, .Nombre = "Alfajor de maicena", .Emoji = "Alf", .Precio = 1000, .Stock = 30, .Categoria = "Panificados", .Descripcion = "Alfajor de maicena"},
        New Producto With {.ID = 24, .Nombre = "Sandwich de miga JYQ", .Emoji = "Mig", .Precio = 3000, .Stock = 25, .Categoria = "Panificados", .Descripcion = "Sandwich de miga"},
        New Producto With {.ID = 25, .Nombre = "Tostado de JYQ", .Emoji = "Ctrl+O", .Precio = 3500, .Stock = 20, .Categoria = "Panificados", .Descripcion = "Tostado de jamon y queso"},
        New Producto With {.ID = 26, .Nombre = "Hamburguesa simple", .Emoji = "H1", .Precio = 4000, .Stock = 20, .Categoria = "Hamburguesas", .Descripcion = "Hamburguesa simple"},
        New Producto With {.ID = 27, .Nombre = "Hamburguesa doble", .Emoji = "H2", .Precio = 5000, .Stock = 20, .Categoria = "Hamburguesas", .Descripcion = "Hamburguesa doble"},
        New Producto With {.ID = 28, .Nombre = "Combo hamburguesa simple", .Emoji = "Combo", .Precio = 9000, .Stock = 15, .Categoria = "Hamburguesas", .Descripcion = "Hamburguesa simple con acompanamiento"},
        New Producto With {.ID = 29, .Nombre = "Porcion de papas", .Emoji = "Papa", .Precio = 3500, .Stock = 25, .Categoria = "Hamburguesas", .Descripcion = "Papas fritas"},
        New Producto With {.ID = 30, .Nombre = "Licuado comun local", .Emoji = "Lic", .Precio = 3500, .Stock = 30, .Categoria = "Licuados", .Descripcion = "Licuado para consumir en local", .Specs = New List(Of EspecificacionGrupo) From {New EspecificacionGrupo With {.Etiqueta = "Fruta", .Opciones = New String() {"Frutilla", "Banana", "Durazno", "Anana", "Melon", "Mango", "Guayaba", "Naranja"}}}},
        New Producto With {.ID = 31, .Nombre = "Licuado comun para llevar", .Emoji = "Lic", .Precio = 3700, .Stock = 30, .Categoria = "Licuados", .Descripcion = "Licuado para llevar", .Specs = New List(Of EspecificacionGrupo) From {New EspecificacionGrupo With {.Etiqueta = "Fruta", .Opciones = New String() {"Frutilla", "Banana", "Durazno", "Anana", "Melon", "Mango", "Guayaba", "Naranja"}}}},
        New Producto With {.ID = 32, .Nombre = "Licuado comun + tostado local", .Emoji = "Lic+", .Precio = 6500, .Stock = 20, .Categoria = "Licuados", .Descripcion = "Licuado comun con tostado"},
        New Producto With {.ID = 33, .Nombre = "Licuado comun + tostado llevar", .Emoji = "Lic+", .Precio = 6700, .Stock = 20, .Categoria = "Licuados", .Descripcion = "Licuado comun con tostado para llevar"},
        New Producto With {.ID = 34, .Nombre = "Licuado especial local", .Emoji = "Esp", .Precio = 4000, .Stock = 25, .Categoria = "Licuados", .Descripcion = "Licuado especial", .Specs = New List(Of EspecificacionGrupo) From {New EspecificacionGrupo With {.Etiqueta = "Sabor", .Opciones = New String() {"Super frutilla", "Banana c/leche", "Durazno c/leche", "Naranja-mango", "Mango-durazno", "Anana-naranja", "Frutos rojos", "Frutos del bosque", "Arandanos", "Kiwi", "Frutilla-anana", "Frambuesa", "Mora", "Frambuesa-frutilla", "Maracuya"}}}},
        New Producto With {.ID = 35, .Nombre = "Licuado especial para llevar", .Emoji = "Esp", .Precio = 4200, .Stock = 25, .Categoria = "Licuados", .Descripcion = "Licuado especial para llevar"},
        New Producto With {.ID = 36, .Nombre = "Licuado especial + tostado local", .Emoji = "Esp+", .Precio = 7000, .Stock = 20, .Categoria = "Licuados", .Descripcion = "Licuado especial con tostado"},
        New Producto With {.ID = 37, .Nombre = "Licuado especial + tostado llevar", .Emoji = "Esp+", .Precio = 7200, .Stock = 20, .Categoria = "Licuados", .Descripcion = "Licuado especial con tostado para llevar"},
        New Producto With {.ID = 38, .Nombre = "Limonada vaso local", .Emoji = "Lim", .Precio = 2000, .Stock = 30, .Categoria = "Limonadas", .Descripcion = "Limon, menta y jengibre"},
        New Producto With {.ID = 39, .Nombre = "Limonada vaso para llevar", .Emoji = "Lim", .Precio = 2200, .Stock = 30, .Categoria = "Limonadas", .Descripcion = "Limonada para llevar"},
        New Producto With {.ID = 40, .Nombre = "Limonada jarra", .Emoji = "Jarra", .Precio = 600, .Stock = 15, .Categoria = "Limonadas", .Descripcion = "Jarra de limonada"}
    }

    Public Function VentasHoy() As Decimal
        Return ListaPedidos.Where(Function(p) p.FechaHora.Date = DateTime.Today AndAlso p.Estado <> EstadoPedido.Cancelado).Sum(Function(p) p.Total)
    End Function

    Public Function TicketPromedio() As Decimal
        Dim pedidosValidos = ListaPedidos.Where(Function(p) p.Estado <> EstadoPedido.Cancelado).ToList()
        If pedidosValidos.Count = 0 Then Return 0
        Return pedidosValidos.Sum(Function(p) p.Total) / pedidosValidos.Count
    End Function

    Public Function PagoLider() As String
        If ListaPedidos.Count = 0 Then Return "Sin datos"
        Dim query = From p In ListaPedidos
                    Where p.Estado <> EstadoPedido.Cancelado
                    Group p By p.MetodoPago Into Grp = Group
                    Select MetodoPago, N = Grp.Count()
                    Order By N Descending
        If Not query.Any() Then Return "Sin datos"
        Return query.First().MetodoPago
    End Function

    Public Sub RegistrarOActualizarCliente(nombre As String, telefono As String, total As Decimal, pago As String, pedidoId As Integer)
        Dim cl As ClienteRegistrado = Nothing
        For Each c In ListaClientes
            If c.Telefono = telefono OrElse c.Nombre = nombre Then
                cl = c
                Exit For
            End If
        Next
        If cl IsNot Nothing Then
            cl.CantidadPedidos += 1
            cl.TotalGastado += total
            cl.UltimoPedido = DateTime.Today
            cl.MetodoPagoHabitual = pago
            cl.HistorialPedidos.Add(pedidoId)
        Else
            Dim nuevo As New ClienteRegistrado With {
                .Nombre = nombre,
                .Telefono = If(String.IsNullOrEmpty(telefono), "--", telefono),
                .CantidadPedidos = 1, .TotalGastado = total,
                .UltimoPedido = DateTime.Today, .MetodoPagoHabitual = pago
            }
            nuevo.HistorialPedidos.Add(pedidoId)
            ListaClientes.Add(nuevo)
        End If
    End Sub

    Public Function TextoTicketCliente(pedido As Pedido, Optional incluirPago As Boolean = True) As String
        Dim sb As New StringBuilder()
        sb.AppendLine("========================================")
        sb.AppendLine("      CAFETERIA MITRE -- CLIENTE       ")
        sb.AppendLine("========================================")
        sb.AppendLine("Pedido N.:  " & pedido.ID.ToString())
        sb.AppendLine("Fecha:      " & pedido.FechaHora.ToString("dd/MM/yyyy HH:mm"))
        sb.AppendLine("Cliente:    " & pedido.NombreCliente)
        sb.AppendLine("Telefono:   " & If(String.IsNullOrWhiteSpace(pedido.TelefonoCliente), "--", pedido.TelefonoCliente))
        sb.AppendLine("Servicio:   " & pedido.TipoServicio)
        If pedido.Mesa <> "" Then sb.AppendLine("Mesa:       " & pedido.Mesa)
        sb.AppendLine("Atendido:   " & pedido.UsuarioResponsable)
        sb.AppendLine("----------------------------------------")
        For Each item In pedido.Items
            sb.AppendLine(item.Cantidad & " x " & item.Producto.Nombre)
            sb.AppendLine("Unitario:   $" & item.Producto.Precio.ToString("F2"))
            sb.AppendLine("Subtotal:   $" & item.Subtotal.ToString("F2"))
            If Not String.IsNullOrWhiteSpace(item.Especificaciones) Then
                sb.AppendLine("Detalle:    " & item.Especificaciones)
            End If
            sb.AppendLine()
        Next
        sb.AppendLine("----------------------------------------")
        sb.AppendLine("Subtotal:   $" & pedido.Subtotal.ToString("F2"))
        If pedido.Descuento > 0 Then sb.AppendLine("Descuento:  -$" & pedido.Descuento.ToString("F2"))
        sb.AppendLine("TOTAL:      $" & pedido.Total.ToString("F2"))
        If incluirPago Then sb.AppendLine("Pago:       " & pedido.MetodoPago)
        sb.AppendLine("========================================")
        sb.AppendLine("      Gracias por su visita!           ")
        sb.AppendLine("========================================")
        Return sb.ToString()
    End Function

    Public Function TextoTicketCocina(pedido As Pedido) As String
        Dim sb As New StringBuilder()
        sb.AppendLine("========================================")
        sb.AppendLine("      CAFETERIA MITRE -- COCINA        ")
        sb.AppendLine("========================================")
        sb.AppendLine("Pedido N.:  " & pedido.ID.ToString())
        sb.AppendLine("Hora:       " & pedido.FechaHora.ToString("HH:mm"))
        sb.AppendLine("Servicio:   " & pedido.TipoServicio)
        If pedido.Mesa <> "" Then sb.AppendLine("Mesa:       " & pedido.Mesa)
        sb.AppendLine("Cliente:    " & pedido.NombreCliente)
        sb.AppendLine("----------------------------------------")
        For Each item In pedido.Items
            sb.AppendLine(item.Cantidad & " x " & item.Producto.Nombre.ToUpper())
            If Not String.IsNullOrWhiteSpace(item.Especificaciones) Then
                sb.AppendLine("  " & item.Especificaciones)
            End If
            sb.AppendLine()
        Next
        sb.AppendLine("========================================")
        Return sb.ToString()
    End Function

    Public Function TextoTicketCaja(pedido As Pedido, Optional incluirPago As Boolean = True) As String
        Dim sb As New StringBuilder()
        sb.AppendLine("========================================")
        sb.AppendLine("       CAFETERIA MITRE -- CAJA         ")
        sb.AppendLine("========================================")
        sb.AppendLine("Venta N.:   " & pedido.ID.ToString())
        sb.AppendLine("Fecha:      " & pedido.FechaHora.ToString("dd/MM/yyyy HH:mm"))
        sb.AppendLine("Cajera/o:   " & pedido.UsuarioResponsable)
        sb.AppendLine("Cliente:    " & pedido.NombreCliente & " | " & If(String.IsNullOrWhiteSpace(pedido.TelefonoCliente), "--", pedido.TelefonoCliente))
        sb.AppendLine("Servicio:   " & pedido.TipoServicio & " - " & pedido.Mesa)
        If incluirPago Then sb.AppendLine("Pago:       " & pedido.MetodoPago)
        sb.AppendLine("----------------------------------------")
        For Each item In pedido.Items
            sb.AppendLine(item.Cantidad & " x " & item.Producto.Nombre & " @ $" & item.Producto.Precio.ToString("F2"))
            sb.AppendLine("Total item: $" & item.Subtotal.ToString("F2"))
        Next
        sb.AppendLine("----------------------------------------")
        sb.AppendLine("Subtotal:   $" & pedido.Subtotal.ToString("F2"))
        If pedido.Descuento > 0 Then sb.AppendLine("Descuento:  -$" & pedido.Descuento.ToString("F2"))
        sb.AppendLine("TOTAL:      $" & pedido.Total.ToString("F2"))
        sb.AppendLine("========================================")
        Return sb.ToString()
    End Function

    Public Function TextoTicket(pedido As Pedido) As String
        Return TextoTicketCliente(pedido, True)
    End Function

    Public Function CostoEstimadoVentasHoy() As Decimal
        Return ListaPedidos.Where(Function(p) p.FechaHora.Date = DateTime.Today AndAlso p.Estado <> EstadoPedido.Cancelado).Sum(Function(p) p.Total * 0.45D)
    End Function

    Public Function GananciaEstimadaHoy() As Decimal
        Return VentasHoy() - CostoEstimadoVentasHoy()
    End Function

    Public Function PerdidasEstimadasHoy() As Decimal
        Return Caja.TotalEgresos
    End Function

    Public Function HorarioPicoPedidos() As String
        Dim pedidosHoy = ListaPedidos.Where(Function(p) p.FechaHora.Date = DateTime.Today AndAlso p.Estado <> EstadoPedido.Cancelado).ToList()
        If pedidosHoy.Count = 0 Then Return "Sin datos"
        Dim q = From p In pedidosHoy
                Group p By Hora = p.FechaHora.Hour Into Grupo = Group
                Select Hora, Cantidad = Grupo.Count()
                Order By Cantidad Descending
        Dim top = q.First()
        Return top.Hora.ToString("00") & ":00 - " & (top.Hora + 1).ToString("00") & ":00 (" & top.Cantidad & " pedidos)"
    End Function
    Public Function ExportarReporteTxt() As String
        Dim sb As New StringBuilder()
        sb.AppendLine("========================================")
        sb.AppendLine("   REPORTE GENERAL -- CAFETERIA MITRE  ")
        sb.AppendLine("========================================")
        sb.AppendLine("Generado: " & DateTime.Now.ToString("dd/MM/yyyy HH:mm"))
        sb.AppendLine()
        sb.AppendLine("--- RESUMEN ---")
        sb.AppendLine("Total pedidos:    " & ListaPedidos.Count)
        sb.AppendLine("Pedidos activos:  " & ListaPedidos.Where(Function(p) p.Estado <> EstadoPedido.Cancelado).Count())
        sb.AppendLine("Cancelados:       " & ListaPedidos.Where(Function(p) p.Estado = EstadoPedido.Cancelado).Count())
        sb.AppendLine("Ventas del dia:   $" & VentasHoy().ToString("N2"))
        sb.AppendLine("Ticket promedio:  $" & TicketPromedio().ToString("N2"))
        sb.AppendLine("Metodo lider:     " & PagoLider())
        sb.AppendLine("Costo estimado:   $" & CostoEstimadoVentasHoy().ToString("N2"))
        sb.AppendLine("Ganancia estim.:  $" & GananciaEstimadaHoy().ToString("N2"))
        sb.AppendLine("Perdidas/egresos: $" & PerdidasEstimadasHoy().ToString("N2"))
        sb.AppendLine("Horario pico:     " & HorarioPicoPedidos())
        sb.AppendLine()
        sb.AppendLine("--- CAJA ---")
        sb.AppendLine("Estado:           " & If(Caja.Abierta, "Abierta", "Cerrada"))
        sb.AppendLine("Monto inicial:    $" & Caja.MontoInicial.ToString("N2"))
        sb.AppendLine("Total ventas:     $" & Caja.TotalVentas.ToString("N2"))
        sb.AppendLine("Total egresos:    $" & Caja.TotalEgresos.ToString("N2"))
        sb.AppendLine("Saldo actual:     $" & Caja.SaldoActual.ToString("N2"))
        sb.AppendLine()
        sb.AppendLine("--- PEDIDOS ---")
        For Each p In ListaPedidos
            sb.AppendLine("#" & p.ID & " | " & p.NombreCliente & " | " & p.Mesa & " | $" & p.Total.ToString("N2") & " | " & p.Estado.ToString() & " | " & p.FechaHora.ToString("HH:mm"))
        Next
        sb.AppendLine()
        sb.AppendLine("--- STOCK CRITICO ---")
        For Each p In ListaProductos.Where(Function(x) x.Stock <= 10)
            sb.AppendLine(p.Nombre & ": " & p.Stock & " unidades")
        Next
        sb.AppendLine("========================================")
        Return sb.ToString()
    End Function
End Module

' ══════════════════════════════════════════════════════
'  FORM ESPECIFICACIONES
' ══════════════════════════════════════════════════════
Public Class EspecificacionesForm
    Inherits Form

    Public Property GruposSeleccionados As New Dictionary(Of String, String)()
    Public Property NotasAdicionales As String = ""

    Private _producto As Producto
    Private _chips As New Dictionary(Of String, List(Of Button))()
    Private _txtNota As TextBox

    Public Sub New(prod As Producto)
        _producto = prod
        Me.Text = "Personalizar -- " & prod.Nombre
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Tema.Surface

        Dim hdr As New Panel With {.Dock = DockStyle.Top, .Height = 70, .BackColor = Tema.Acento}
        Dim lNom As New Label With {
            .Text = prod.Nombre,
            .Font = New Font("Segoe UI", 13, FontStyle.Bold),
            .ForeColor = Color.White, .Location = New Point(16, 14),
            .AutoSize = True, .BackColor = Color.Transparent
        }
        Dim lSub As New Label With {
            .Text = "Personaliza tu pedido  --  $" & prod.Precio.ToString("N0"),
            .Font = Tema.FntSmall,
            .ForeColor = Tema.Acento3, .Location = New Point(16, 40),
            .AutoSize = True, .BackColor = Color.Transparent
        }
        hdr.Controls.AddRange(New Control() {lNom, lSub})

        Dim scroll As New Panel With {
            .Dock = DockStyle.Fill, .AutoScroll = True,
            .BackColor = Tema.Surface, .Padding = New Padding(16, 12, 16, 12)
        }

        Dim y As Integer = 12
        For Each sg In prod.Specs
            Dim grpEtiqueta As String = sg.Etiqueta
            Dim lGrp As New Label With {
                .Text = sg.Etiqueta.ToUpper(),
                .Font = Tema.FntSmallB, .ForeColor = Tema.TextoTer,
                .Location = New Point(0, y), .AutoSize = True,
                .BackColor = Color.Transparent
            }
            scroll.Controls.Add(lGrp)
            y += 22

            Dim chips As New List(Of Button)()
            _chips(grpEtiqueta) = chips
            GruposSeleccionados(grpEtiqueta) = sg.Opciones(0)

            Dim chipX As Integer = 0
            Dim isFirst As Boolean = True
            For Each op In sg.Opciones
                Dim opName As String = op
                Dim chip As New Button With {
                    .Text = opName,
                    .Font = Tema.FntSmall,
                    .FlatStyle = FlatStyle.Flat,
                    .Cursor = Cursors.Hand,
                    .AutoSize = True,
                    .Padding = New Padding(10, 5, 10, 5),
                    .Tag = grpEtiqueta
                }
                chip.FlatAppearance.BorderSize = 1

                If chipX + 120 > 320 Then
                    chipX = 0
                    y += 34
                End If
                chip.Location = New Point(chipX, y)
                chipX += chip.PreferredSize.Width + 6

                If isFirst Then
                    chip.BackColor = Tema.Acento
                    chip.ForeColor = Color.White
                    chip.FlatAppearance.BorderColor = Tema.Acento
                    isFirst = False
                Else
                    chip.BackColor = Tema.Surface2
                    chip.ForeColor = Tema.TextoSec
                    chip.FlatAppearance.BorderColor = Tema.Borde
                End If

                AddHandler chip.Click, AddressOf ChipClick
                chips.Add(chip)
                scroll.Controls.Add(chip)
            Next
            y += 40

            Dim sep = Tema.MkSep(0, y, 340)
            scroll.Controls.Add(sep)
            y += 10
        Next

        Dim lNota As New Label With {
            .Text = "OBSERVACIONES ADICIONALES",
            .Font = Tema.FntSmallB, .ForeColor = Tema.TextoTer,
            .Location = New Point(0, y), .AutoSize = True,
            .BackColor = Color.Transparent
        }
        scroll.Controls.Add(lNota)
        y += 22

        _txtNota = New TextBox With {
            .Location = New Point(0, y), .Width = 340, .Height = 56,
            .Multiline = True, .Font = Tema.FntInput,
            .BackColor = Tema.Surface2, .ForeColor = Tema.TextoPrinc,
            .BorderStyle = BorderStyle.FixedSingle
        }
        scroll.Controls.Add(_txtNota)
        y += 66

        scroll.AutoScrollMinSize = New Size(0, y + 20)

        Dim footer As New Panel With {.Dock = DockStyle.Bottom, .Height = 58, .BackColor = Tema.Surface2}
        Dim btnOk = Tema.CrearBoton("Agregar al pedido", Tema.Verde, Color.White, 170, 38)
        btnOk.Location = New Point(14, 10)
        Dim btnCan = Tema.CrearBoton("Cancelar", Tema.Surface3, Tema.TextoSec, 110, 38)
        btnCan.Location = New Point(192, 10)
        footer.Controls.AddRange(New Control() {btnOk, btnCan})

        Me.Controls.AddRange(New Control() {footer, scroll, hdr})
        Me.ClientSize = New Size(370, Math.Min(520, y + 140))

        AddHandler btnOk.Click, AddressOf BtnOkClick
        AddHandler btnCan.Click, AddressOf BtnCanClick
    End Sub

    Private Sub ChipClick(sender As Object, e As EventArgs)
        Dim clicked = CType(sender, Button)
        Dim grp As String = clicked.Tag.ToString()
        For Each c In _chips(grp)
            c.BackColor = Tema.Surface2
            c.ForeColor = Tema.TextoSec
            c.FlatAppearance.BorderColor = Tema.Borde
        Next
        clicked.BackColor = Tema.Acento
        clicked.ForeColor = Color.White
        clicked.FlatAppearance.BorderColor = Tema.Acento
        GruposSeleccionados(grp) = clicked.Text
    End Sub

    Private Sub BtnOkClick(sender As Object, e As EventArgs)
        NotasAdicionales = _txtNota.Text
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    Private Sub BtnCanClick(sender As Object, e As EventArgs)
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub
End Class

' ══════════════════════════════════════════════════════
'  FORM TICKET
' ══════════════════════════════════════════════════════
Public Class TicketForm
    Inherits Form
    Private _pedido As Pedido
    Private tabs As TabControl
    Private txtCliente As RichTextBox
    Private txtCocina As RichTextBox
    Private txtCaja As RichTextBox
    Private chkPago As CheckBox

    Public Sub New(pedido As Pedido)
        _pedido = pedido
        Me.Text = "Tickets Pedido N. " & pedido.ID
        Me.Size = New Size(560, 680)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.BackColor = Tema.BgPrincipal

        Dim hdr As New Panel With {.Location = New Point(0, 0), .Size = New Size(560, 65), .BackColor = Tema.Acento}
        Dim lh1 As New Label With {.Text = "TICKETS DE PEDIDO", .Font = New Font("Segoe UI", 14, FontStyle.Bold), .ForeColor = Color.White, .Location = New Point(18, 8), .Size = New Size(520, 28), .BackColor = Color.Transparent}
        Dim lh2 As New Label With {.Text = "Cliente / Cocina / Caja - Pedido N. " & pedido.ID & "   " & pedido.FechaHora.ToString("dd/MM/yyyy HH:mm"), .Font = Tema.FntReg, .ForeColor = Tema.Acento3, .Location = New Point(18, 38), .Size = New Size(520, 20), .BackColor = Color.Transparent}
        hdr.Controls.AddRange(New Control() {lh1, lh2})

        chkPago = New CheckBox With {
            .Text = "Incluir metodo de pago en tickets de cliente y caja",
            .Checked = True, .Location = New Point(18, 74), .Size = New Size(420, 24),
            .Font = Tema.FntReg, .ForeColor = Tema.TextoPrinc, .BackColor = Color.Transparent
        }

        tabs = New TabControl With {.Location = New Point(14, 104), .Size = New Size(528, 460), .Font = Tema.FntReg}
        txtCliente = CrearVisorTicket(Datos.TextoTicketCliente(pedido, True))
        txtCocina = CrearVisorTicket(Datos.TextoTicketCocina(pedido))
        txtCaja = CrearVisorTicket(Datos.TextoTicketCaja(pedido, True))

        Dim tabCliente As New TabPage("Cliente")
        Dim tabCocina As New TabPage("Cocina")
        Dim tabCaja As New TabPage("Caja")
        tabCliente.Controls.Add(txtCliente)
        tabCocina.Controls.Add(txtCocina)
        tabCaja.Controls.Add(txtCaja)
        tabs.TabPages.AddRange(New TabPage() {tabCliente, tabCocina, tabCaja})

        Dim btnImp = Tema.CrearBoton("Imprimir actual", Tema.Acento, Color.White, 130, 36)
        Dim btnImpTodo = Tema.CrearBoton("Imprimir 3 tickets", Tema.Verde, Color.White, 140, 36)
        Dim btnTxt = Tema.CrearBoton("Guardar TXT", Tema.Azul, Color.White, 110, 36)
        Dim btnCer = Tema.CrearBoton("Cerrar", Tema.Borde, Tema.TextoSec, 90, 36)

        btnImp.Location = New Point(14, 580)
        btnImpTodo.Location = New Point(152, 580)
        btnTxt.Location = New Point(300, 580)
        btnCer.Location = New Point(440, 580)

        Me.Controls.AddRange(New Control() {hdr, chkPago, tabs, btnImp, btnImpTodo, btnTxt, btnCer})

        AddHandler chkPago.CheckedChanged, Sub(s, e) RefrescarTickets()
        AddHandler btnCer.Click, Sub(s, e) Me.Close()
        AddHandler btnImp.Click, AddressOf ImprimirActualClick
        AddHandler btnImpTodo.Click, AddressOf ImprimirTodosClick
        AddHandler btnTxt.Click, AddressOf GuardarTxtClick
    End Sub

    Private Function CrearVisorTicket(texto As String) As RichTextBox
        Return New RichTextBox With {
            .Dock = DockStyle.Fill, .ReadOnly = True, .Font = Tema.FntMono,
            .BackColor = Color.White, .ForeColor = Color.Black,
            .BorderStyle = BorderStyle.None, .Text = texto
        }
    End Function

    Private Sub RefrescarTickets()
        txtCliente.Text = Datos.TextoTicketCliente(_pedido, chkPago.Checked)
        txtCocina.Text = Datos.TextoTicketCocina(_pedido)
        txtCaja.Text = Datos.TextoTicketCaja(_pedido, chkPago.Checked)
    End Sub

    Private Function TextoActual() As String
        Select Case tabs.SelectedIndex
            Case 1 : Return txtCocina.Text
            Case 2 : Return txtCaja.Text
            Case Else : Return txtCliente.Text
        End Select
    End Function

    Private Sub ImprimirActualClick(s As Object, e As EventArgs)
        ImprimirTexto(TextoActual())
    End Sub

    Private Sub ImprimirTodosClick(s As Object, e As EventArgs)
        ImprimirTexto(txtCliente.Text & Environment.NewLine & Environment.NewLine & txtCocina.Text & Environment.NewLine & Environment.NewLine & txtCaja.Text)
    End Sub

    Private Sub ImprimirTexto(texto As String)
        Dim pd As New PrintDocument()
        AddHandler pd.PrintPage, Sub(sender, ev)
                                     Dim fnt As New Font("Courier New", 9)
                                     Dim y As Single = ev.MarginBounds.Top
                                     Dim lh As Single = fnt.GetHeight(ev.Graphics)
                                     For Each ln In texto.Split(New String() {Environment.NewLine}, StringSplitOptions.None)
                                         ev.Graphics.DrawString(ln, fnt, Brushes.Black, ev.MarginBounds.Left, y)
                                         y += lh
                                         If y > ev.MarginBounds.Bottom Then Exit For
                                     Next
                                 End Sub
        Dim dlg As New PrintDialog With {.Document = pd}
        If dlg.ShowDialog() = DialogResult.OK Then
            pd.Print()
            MessageBox.Show("Ticket enviado a impresora.", "Listo", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
    End Sub

    Private Sub GuardarTxtClick(s As Object, e As EventArgs)
        Dim dlg As New SaveFileDialog With {
            .Filter = "Texto (*.txt)|*.txt",
            .FileName = "Tickets_Pedido_" & _pedido.ID & "_" & DateTime.Now.ToString("yyyyMMdd_HHmm") & ".txt"
        }
        If dlg.ShowDialog() = DialogResult.OK Then
            Dim contenido As String = "--- TICKET CLIENTE ---" & Environment.NewLine & txtCliente.Text & Environment.NewLine & Environment.NewLine &
                                      "--- TICKET COCINA ---" & Environment.NewLine & txtCocina.Text & Environment.NewLine & Environment.NewLine &
                                      "--- TICKET CAJA ---" & Environment.NewLine & txtCaja.Text
            File.WriteAllText(dlg.FileName, contenido, Encoding.UTF8)
            MessageBox.Show("Guardado en: " & dlg.FileName, "Listo", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
    End Sub
End Class
' ══════════════════════════════════════════════════════
'  FORM CAJA
' ══════════════════════════════════════════════════════
Public Class CajaForm
    Inherits Panel

    Private lblEstado As Label
    Private lblSaldo As Label
    Private lblVentas As Label
    Private lblEgresos As Label
    Private dgvMovimientos As DataGridView

    Public Sub New()
        Me.Dock = DockStyle.Fill
        Me.BackColor = Tema.BgPrincipal

        Dim hdr As New Panel With {.Dock = DockStyle.Top, .Height = 58, .BackColor = Tema.Acento}
        Dim lh As New Label With {.Text = "Gestion de Caja", .Font = New Font("Segoe UI", 15, FontStyle.Bold), .ForeColor = Color.White, .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.MiddleLeft, .Padding = New Padding(18, 0, 0, 0), .BackColor = Color.Transparent}
        hdr.Controls.Add(lh)

        Dim pnlStats As New Panel With {.Location = New Point(12, 66), .Size = New Size(660, 80), .BackColor = Color.Transparent}

        lblEstado = New Label With {.Location = New Point(0, 0), .Size = New Size(150, 70), .BackColor = Tema.Surface, .TextAlign = ContentAlignment.MiddleCenter, .Font = New Font("Segoe UI", 10, FontStyle.Bold)}
        lblVentas = New Label With {.Location = New Point(158, 0), .Size = New Size(150, 70), .BackColor = Tema.Surface, .TextAlign = ContentAlignment.MiddleCenter, .Font = New Font("Segoe UI", 10, FontStyle.Bold)}
        lblEgresos = New Label With {.Location = New Point(316, 0), .Size = New Size(150, 70), .BackColor = Tema.Surface, .TextAlign = ContentAlignment.MiddleCenter, .Font = New Font("Segoe UI", 10, FontStyle.Bold)}
        lblSaldo = New Label With {.Location = New Point(474, 0), .Size = New Size(186, 70), .BackColor = Tema.Surface, .TextAlign = ContentAlignment.MiddleCenter, .Font = New Font("Segoe UI", 12, FontStyle.Bold)}

        pnlStats.Controls.AddRange(New Control() {lblEstado, lblVentas, lblEgresos, lblSaldo})

        dgvMovimientos = New DataGridView With {
            .Location = New Point(12, 154), .Size = New Size(660, 280),
            .ReadOnly = True, .AllowUserToAddRows = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .BorderStyle = BorderStyle.None, .BackgroundColor = Tema.Surface,
            .RowHeadersVisible = False, .GridColor = Tema.Borde, .Font = Tema.FntReg
        }
        dgvMovimientos.Columns.AddRange(New DataGridViewColumn() {
            New DataGridViewTextBoxColumn With {.Name = "Hora", .HeaderText = "Hora"},
            New DataGridViewTextBoxColumn With {.Name = "Tipo", .HeaderText = "Tipo"},
            New DataGridViewTextBoxColumn With {.Name = "Desc", .HeaderText = "Descripcion"},
            New DataGridViewTextBoxColumn With {.Name = "Monto", .HeaderText = "Monto"},
            New DataGridViewTextBoxColumn With {.Name = "Usuario", .HeaderText = "Usuario"}
        })

        Dim btnAbrir = Tema.CrearBoton("Abrir Caja", Tema.Verde, Color.White, 150, 36)
        Dim btnCerrar = Tema.CrearBoton("Cerrar Caja", Tema.Rojo, Color.White, 150, 36)
        Dim btnEgreso = Tema.CrearBoton("Registrar Egreso", Tema.Naranja, Color.White, 160, 36)
        Dim btnCerrarForm = Tema.CrearBoton("Actualizar", Tema.Surface3, Tema.TextoSec, 110, 36)

        btnAbrir.Location = New Point(12, 446)
        btnCerrar.Location = New Point(170, 446)
        btnEgreso.Location = New Point(328, 446)
        btnCerrarForm.Location = New Point(562, 446)

        Me.Controls.AddRange(New Control() {hdr, pnlStats, dgvMovimientos, btnAbrir, btnCerrar, btnEgreso, btnCerrarForm})

        AddHandler btnAbrir.Click, AddressOf AbrirCaja
        AddHandler btnCerrar.Click, AddressOf CerrarCajaClick
        AddHandler btnEgreso.Click, AddressOf RegistrarEgreso
        AddHandler btnCerrarForm.Click, Sub(s, e) ActualizarUI()

        ActualizarUI()
    End Sub

    Private Sub ActualizarUI()
        lblEstado.Text = "Estado" & Environment.NewLine & If(Datos.Caja.Abierta, "ABIERTA", "CERRADA")
        lblEstado.ForeColor = If(Datos.Caja.Abierta, Tema.Verde, Tema.Rojo)
        lblVentas.Text = "Ventas" & Environment.NewLine & "$" & Datos.Caja.TotalVentas.ToString("N0")
        lblVentas.ForeColor = Tema.Verde
        lblEgresos.Text = "Egresos" & Environment.NewLine & "$" & Datos.Caja.TotalEgresos.ToString("N0")
        lblEgresos.ForeColor = Tema.Naranja
        lblSaldo.Text = "Saldo" & Environment.NewLine & "$" & Datos.Caja.SaldoActual.ToString("N0")
        lblSaldo.ForeColor = Tema.Acento

        dgvMovimientos.Rows.Clear()
        For Each m In Datos.Caja.Movimientos
            dgvMovimientos.Rows.Add(m.FechaHora.ToString("HH:mm"), m.Tipo, m.Descripcion, "$" & m.Monto.ToString("N0"), m.Usuario)
        Next
    End Sub

    Private Sub AbrirCaja(s As Object, e As EventArgs)
        If Datos.Caja.Abierta Then
            MessageBox.Show("La caja ya esta abierta.", "Atencion", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        Dim montoStr As String = InputBox("Ingrese el monto inicial de caja:", "Abrir Caja", "0")
        Dim monto As Decimal = 0
        If Not Decimal.TryParse(montoStr, monto) Then Return
        Datos.Caja.Abierta = True
        Datos.Caja.MontoInicial = monto
        Datos.Caja.FechaApertura = DateTime.Now
        Datos.Caja.UsuarioApertura = If(Datos.UsuarioActual IsNot Nothing, Datos.UsuarioActual.Nombre, "Sistema")
        Datos.Caja.Movimientos.Add(New MovimientoCaja With {
            .Tipo = "Apertura", .Monto = monto,
            .Descripcion = "Apertura de caja",
            .Usuario = Datos.Caja.UsuarioApertura
        })
        ActualizarUI()
        MessageBox.Show("Caja abierta con $" & monto.ToString("N0"), "Listo", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub CerrarCajaClick(s As Object, e As EventArgs)
        If Not Datos.Caja.Abierta Then
            MessageBox.Show("La caja no esta abierta.", "Atencion", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        Dim res = MessageBox.Show("Cerrar caja? Saldo final: $" & Datos.Caja.SaldoActual.ToString("N0"), "Confirmar cierre", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
        If res = DialogResult.Yes Then
            Datos.Caja.Movimientos.Add(New MovimientoCaja With {
                .Tipo = "Cierre", .Monto = Datos.Caja.SaldoActual,
                .Descripcion = "Cierre de caja",
                .Usuario = If(Datos.UsuarioActual IsNot Nothing, Datos.UsuarioActual.Nombre, "Sistema")
            })
            Datos.Caja.Abierta = False
            ActualizarUI()
            MessageBox.Show("Caja cerrada. Saldo final: $" & Datos.Caja.SaldoActual.ToString("N0"), "Caja cerrada", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
    End Sub

    Private Sub RegistrarEgreso(s As Object, e As EventArgs)
        If Not Datos.Caja.Abierta Then
            MessageBox.Show("Debe abrir la caja primero.", "Atencion", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        Dim desc As String = InputBox("Descripcion del egreso:", "Egreso de caja")
        If String.IsNullOrWhiteSpace(desc) Then Return
        Dim montoStr As String = InputBox("Monto del egreso:", "Egreso de caja", "0")
        Dim monto As Decimal = 0
        If Not Decimal.TryParse(montoStr, monto) OrElse monto <= 0 Then Return
        Datos.Caja.Movimientos.Add(New MovimientoCaja With {
            .Tipo = "Egreso", .Monto = monto,
            .Descripcion = desc,
            .Usuario = If(Datos.UsuarioActual IsNot Nothing, Datos.UsuarioActual.Nombre, "Sistema")
        })
        ActualizarUI()
    End Sub
End Class

Public Class ProveedorForm
    Inherits Form

    Private _proveedor As Proveedor
    Private _codigoOriginal As String
    Private _cuitOriginal As String
    Private campos As New Dictionary(Of String, TextBox)()
    Private chkActivo As CheckBox

    Public Sub New(proveedor As Proveedor, esNuevo As Boolean)
        _proveedor = proveedor
        _codigoOriginal = If(proveedor.Codigo, "")
        _cuitOriginal = If(proveedor.CUIT, "")
        Me.Text = If(esNuevo, "Nuevo proveedor", "Editar proveedor")
        Me.Size = New Size(900, 660)
        Me.MinimumSize = New Size(840, 600)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Tema.BgPrincipal
        Construir()
        CargarDatos()
    End Sub

    Private Sub Construir()
        Dim header As New Panel With {.Dock = DockStyle.Top, .Height = 58, .BackColor = Tema.Acento}
        Dim titulo As New Label With {.Text = "  Proveedor", .Dock = DockStyle.Fill, .ForeColor = Color.White, .Font = New Font("Segoe UI", 15, FontStyle.Bold), .TextAlign = ContentAlignment.MiddleLeft}
        header.Controls.Add(titulo)
        Me.Controls.Add(header)

        Dim tabs As New TabControl With {.Location = New Point(16, 74), .Size = New Size(850, 450), .Font = Tema.FntReg}
        tabs.TabPages.Add(BuildGenerales())
        tabs.TabPages.Add(BuildMasDatos())
        tabs.TabPages.Add(BuildDescuentos())
        tabs.TabPages.Add(BuildEstadisticas())
        tabs.TabPages.Add(BuildComprobantes())
        Me.Controls.Add(tabs)

        Dim btnGuardar = Tema.CrearBoton("Guardar", Tema.Verde, Color.White, 110, 36)
        Dim btnCancelar = Tema.CrearBoton("Cancelar", Tema.Rojo, Color.White, 110, 36)
        Dim btnNuevo = Tema.CrearBoton("Nuevo", Tema.Azul, Color.White, 90, 36)
        Dim btnBuscar = Tema.CrearBoton("Buscar", Tema.Acento2, Color.White, 90, 36)
        Dim btnImprimir = Tema.CrearBoton("Imprimir ficha", Tema.Acento, Color.White, 130, 36)
        Dim btnPdf = Tema.CrearBoton("Exportar PDF", Color.FromArgb(110, 86, 50), Color.White, 125, 36)
        btnGuardar.Location = New Point(16, 540)
        btnCancelar.Location = New Point(132, 540)
        btnNuevo.Location = New Point(248, 540)
        btnBuscar.Location = New Point(344, 540)
        btnImprimir.Location = New Point(440, 540)
        btnPdf.Location = New Point(576, 540)
        Me.Controls.AddRange(New Control() {btnGuardar, btnCancelar, btnNuevo, btnBuscar, btnImprimir, btnPdf})

        AddHandler btnGuardar.Click, AddressOf Guardar
        AddHandler btnCancelar.Click, Sub(s, e)
                                          Me.DialogResult = DialogResult.Cancel
                                          Me.Close()
                                      End Sub
        AddHandler btnNuevo.Click, Sub(s, e)
                                       For Each tb In campos.Values
                                           tb.Clear()
                                       Next
                                       chkActivo.Checked = True
                                   End Sub
        AddHandler btnBuscar.Click, Sub(s, e) campos("Codigo").Focus()
        AddHandler btnImprimir.Click, Sub(s, e) MessageBox.Show(FichaTexto(), "Ficha de proveedor")
        AddHandler btnPdf.Click, Sub(s, e) MessageBox.Show("Para PDF usa una impresora PDF desde Imprimir ficha. No se usa base de datos.", "Exportar PDF")
    End Sub

    Private Function BuildGenerales() As TabPage
        Dim tp As New TabPage("Generales") With {.BackColor = Tema.BgPrincipal}
        AddField(tp, "Codigo", "Codigo", 18, 22, 160)
        AddField(tp, "Nombre", "Nombre", 18, 62, 360)
        AddField(tp, "Domicilio", "Domicilio", 18, 102, 360)
        AddField(tp, "CodigoPostal", "Codigo postal", 18, 142, 160)
        AddField(tp, "TelefonoLaboral", "Telefono laboral", 18, 182, 220)
        AddField(tp, "TelefonoParticular", "Telefono particular", 18, 222, 220)
        AddField(tp, "Celular", "Celular", 18, 262, 220)
        AddField(tp, "Fax", "Fax", 18, 302, 220)
        AddField(tp, "ContactoPrincipal", "Contacto principal", 18, 342, 260)
        AddField(tp, "TipoIVA", "Tipo IVA", 430, 22, 210)
        AddField(tp, "CUIT", "CUIT", 430, 62, 210)
        AddField(tp, "TipoDocumento", "Tipo doc", 430, 102, 120)
        AddField(tp, "NumeroDocumento", "Nro doc", 620, 102, 160)
        AddField(tp, "Pais", "Pais", 430, 142, 210)
        AddField(tp, "Provincia", "Provincia", 430, 182, 210)
        AddField(tp, "Localidad", "Localidad", 430, 222, 210)
        AddField(tp, "Actividad", "Actividad", 430, 262, 210)
        AddField(tp, "Categoria", "Categoria", 430, 302, 210)
        AddField(tp, "ObservacionesRapidas", "Obs. rapidas", 430, 342, 360)
        chkActivo = New CheckBox With {.Text = "Proveedor activo", .Location = New Point(430, 392), .AutoSize = True, .Font = Tema.FntReg, .ForeColor = Tema.TextoPrinc, .BackColor = Color.Transparent}
        tp.Controls.Add(chkActivo)
        Return tp
    End Function

    Private Function BuildMasDatos() As TabPage
        Dim tp As New TabPage("Mas datos") With {.BackColor = Tema.BgPrincipal}
        AddField(tp, "Email", "Email", 18, 22, 300)
        AddField(tp, "SitioWeb", "Sitio web", 18, 62, 300)
        AddField(tp, "FechaAlta", "Fecha de alta", 18, 102, 160)
        AddField(tp, "CondicionComercial", "Condicion comercial", 18, 142, 260)
        AddField(tp, "FormaPagoHabitual", "Forma de pago", 18, 182, 260)
        AddField(tp, "Banco", "Banco", 430, 22, 230)
        AddField(tp, "CBU", "CBU", 430, 62, 300)
        AddField(tp, "Alias", "Alias", 430, 102, 230)
        AddField(tp, "Observaciones", "Observaciones", 430, 142, 330)
        AddMemo(tp, "NotasInternas", "Notas internas", 18, 235, 760, 110)
        AddMemo(tp, "Contactos", "Contactos / telefonos / adjuntos", 18, 360, 760, 55)
        Return tp
    End Function

    Private Function BuildDescuentos() As TabPage
        Dim tp As New TabPage("Descuentos") With {.BackColor = Tema.BgPrincipal}
        AddField(tp, "DescuentoGeneral", "Descuento general %", 18, 30, 160)
        AddField(tp, "DescuentoPorVolumen", "Desc. volumen %", 18, 72, 160)
        AddMemo(tp, "ObservacionesComerciales", "Observaciones comerciales", 18, 125, 760, 190)
        Return tp
    End Function

    Private Function BuildEstadisticas() As TabPage
        Dim tp As New TabPage("Estadisticas") With {.BackColor = Tema.BgPrincipal}
        AddField(tp, "TotalCompras", "Total compras", 18, 30, 180)
        AddField(tp, "CantidadOrdenes", "Ordenes", 18, 72, 180)
        AddField(tp, "UltimaCompra", "Ultima compra", 18, 114, 180)
        AddField(tp, "SaldoPendiente", "Saldo pendiente", 18, 156, 180)
        AddField(tp, "FechaUltimaOperacion", "Ultima operacion", 18, 198, 180)
        AddMemo(tp, "HistorialCambios", "Historial de cambios", 360, 30, 420, 280)
        Return tp
    End Function

    Private Function BuildComprobantes() As TabPage
        Dim tp As New TabPage("Comprobantes") With {.BackColor = Tema.BgPrincipal}
        AddMemo(tp, "Movimientos", "Facturas, notas de credito, pagos y ordenes", 18, 24, 760, 220)
        AddMemo(tp, "Archivos", "PDF adjuntos", 18, 270, 760, 90)
        Return tp
    End Function

    Private Sub AddField(tp As TabPage, key As String, label As String, x As Integer, y As Integer, w As Integer)
        Dim l = Tema.CrearLabel(label, False, 8)
        l.Location = New Point(x, y)
        Dim tb = Tema.CrearInput(False)
        tb.Location = New Point(x + 130, y - 4)
        tb.Width = w
        tb.MaxLength = 120
        campos(key) = tb
        tp.Controls.AddRange(New Control() {l, tb})
    End Sub

    Private Sub AddMemo(tp As TabPage, key As String, label As String, x As Integer, y As Integer, w As Integer, h As Integer)
        Dim l = Tema.CrearLabel(label, False, 8)
        l.Location = New Point(x, y)
        Dim tb As New TextBox With {.Location = New Point(x, y + 18), .Size = New Size(w, h), .Multiline = True, .ScrollBars = ScrollBars.Vertical, .Font = Tema.FntInput, .BackColor = Tema.Surface, .ForeColor = Tema.TextoPrinc, .BorderStyle = BorderStyle.FixedSingle, .MaxLength = 2000}
        campos(key) = tb
        tp.Controls.AddRange(New Control() {l, tb})
    End Sub

    Private Sub CargarDatos()
        SetText("Codigo", _proveedor.Codigo)
        SetText("Nombre", _proveedor.Nombre)
        SetText("Domicilio", _proveedor.Domicilio)
        SetText("CodigoPostal", _proveedor.CodigoPostal)
        SetText("TelefonoLaboral", _proveedor.TelefonoLaboral)
        SetText("TelefonoParticular", _proveedor.TelefonoParticular)
        SetText("Celular", _proveedor.Celular)
        SetText("Fax", _proveedor.Fax)
        SetText("ContactoPrincipal", _proveedor.ContactoPrincipal)
        SetText("TipoIVA", _proveedor.TipoIVA)
        SetText("CUIT", _proveedor.CUIT)
        SetText("TipoDocumento", _proveedor.TipoDocumento)
        SetText("NumeroDocumento", _proveedor.NumeroDocumento)
        SetText("Pais", _proveedor.Pais)
        SetText("Provincia", _proveedor.Provincia)
        SetText("Localidad", _proveedor.Localidad)
        SetText("Actividad", _proveedor.Actividad)
        SetText("Categoria", _proveedor.Categoria)
        SetText("ObservacionesRapidas", _proveedor.ObservacionesRapidas)
        SetText("Email", _proveedor.Email)
        SetText("SitioWeb", _proveedor.SitioWeb)
        SetText("FechaAlta", _proveedor.FechaAlta.ToString("dd/MM/yyyy"))
        SetText("CondicionComercial", _proveedor.CondicionComercial)
        SetText("FormaPagoHabitual", _proveedor.FormaPagoHabitual)
        SetText("Banco", _proveedor.Banco)
        SetText("CBU", _proveedor.CBU)
        SetText("Alias", _proveedor.[Alias])
        SetText("Observaciones", _proveedor.Observaciones)
        SetText("NotasInternas", _proveedor.NotasInternas)
        SetText("DescuentoGeneral", _proveedor.DescuentoGeneral.ToString("0.##"))
        SetText("DescuentoPorVolumen", _proveedor.DescuentoPorVolumen.ToString("0.##"))
        SetText("ObservacionesComerciales", _proveedor.ObservacionesComerciales)
        SetText("TotalCompras", _proveedor.TotalCompras.ToString("0.##"))
        SetText("CantidadOrdenes", _proveedor.CantidadOrdenes.ToString())
        SetText("UltimaCompra", If(_proveedor.UltimaCompra.HasValue, _proveedor.UltimaCompra.Value.ToString("dd/MM/yyyy"), ""))
        SetText("SaldoPendiente", _proveedor.SaldoPendiente.ToString("0.##"))
        SetText("FechaUltimaOperacion", If(_proveedor.FechaUltimaOperacion.HasValue, _proveedor.FechaUltimaOperacion.Value.ToString("dd/MM/yyyy"), ""))
        SetText("HistorialCambios", String.Join(vbCrLf, _proveedor.HistorialCambios))
        SetText("Movimientos", String.Join(vbCrLf, _proveedor.Movimientos.Select(Function(m) m.Fecha.ToString("dd/MM") & " - " & m.Tipo & " - " & m.Descripcion & " - $" & m.Monto.ToString("N0"))))
        SetText("Archivos", String.Join(vbCrLf, _proveedor.Archivos.Select(Function(a) a.Nombre & " - " & a.Ruta)))
        SetText("Contactos", String.Join(vbCrLf, _proveedor.Contactos.Select(Function(c) c.Nombre & " - " & c.Cargo & " - " & c.Telefono)))
        chkActivo.Checked = _proveedor.Activo
    End Sub

    Private Sub SetText(key As String, value As String)
        If campos.ContainsKey(key) Then campos(key).Text = If(value, "")
    End Sub

    Private Function Txt(key As String) As String
        If Not campos.ContainsKey(key) Then Return ""
        Return campos(key).Text.Trim()
    End Function

    Private Function CuitValido(cuit As String) As Boolean
        Dim dig = New String(cuit.Where(Function(ch) Char.IsDigit(ch)).ToArray())
        Return dig = "" OrElse dig.Length = 11
    End Function

    Private Function EmailValido(email As String) As Boolean
        Return email = "" OrElse (email.Contains("@") AndAlso email.Contains(".") AndAlso email.Length <= 120)
    End Function

    Private Function DecVal(key As String) As Decimal
        Dim d As Decimal = 0
        Decimal.TryParse(Txt(key), d)
        Return d
    End Function

    Private Function IntValCampo(key As String) As Integer
        Dim n As Integer = 0
        Integer.TryParse(Txt(key), n)
        Return n
    End Function

    Private Function FechaVal(key As String) As DateTime?
        Dim d As DateTime
        If DateTime.TryParse(Txt(key), d) Then Return d
        Return Nothing
    End Function

    Private Sub Guardar(s As Object, e As EventArgs)
        If Txt("Codigo") = "" Then MessageBox.Show("El codigo es obligatorio.") : Return
        If Txt("Nombre") = "" Then MessageBox.Show("El nombre es obligatorio.") : Return
        If Txt("Codigo").Length > 20 OrElse Txt("Nombre").Length > 120 Then MessageBox.Show("Codigo o nombre demasiado largo.") : Return
        If Not CuitValido(Txt("CUIT")) Then MessageBox.Show("El CUIT debe tener 11 numeros si se completa.") : Return
        If Not EmailValido(Txt("Email")) Then MessageBox.Show("El email no tiene un formato valido.") : Return
        If Datos.ListaProveedores.Any(Function(p) p.Codigo.Equals(Txt("Codigo"), StringComparison.OrdinalIgnoreCase) AndAlso p.Codigo <> _codigoOriginal) Then MessageBox.Show("Ya existe un proveedor con ese codigo.") : Return
        If Txt("CUIT") <> "" AndAlso Datos.ListaProveedores.Any(Function(p) p.CUIT.Equals(Txt("CUIT"), StringComparison.OrdinalIgnoreCase) AndAlso p.CUIT <> _cuitOriginal) Then MessageBox.Show("Ya existe un proveedor con ese CUIT.") : Return

        _proveedor.Codigo = Txt("Codigo")
        _proveedor.Nombre = Txt("Nombre")
        _proveedor.Domicilio = Txt("Domicilio")
        _proveedor.CodigoPostal = Txt("CodigoPostal")
        _proveedor.TelefonoLaboral = Txt("TelefonoLaboral")
        _proveedor.TelefonoParticular = Txt("TelefonoParticular")
        _proveedor.Celular = Txt("Celular")
        _proveedor.Fax = Txt("Fax")
        _proveedor.ContactoPrincipal = Txt("ContactoPrincipal")
        _proveedor.TipoIVA = Txt("TipoIVA")
        _proveedor.CUIT = Txt("CUIT")
        _proveedor.TipoDocumento = Txt("TipoDocumento")
        _proveedor.NumeroDocumento = Txt("NumeroDocumento")
        _proveedor.Pais = Txt("Pais")
        _proveedor.Provincia = Txt("Provincia")
        _proveedor.Localidad = Txt("Localidad")
        _proveedor.Actividad = Txt("Actividad")
        _proveedor.Categoria = Txt("Categoria")
        _proveedor.ObservacionesRapidas = Txt("ObservacionesRapidas")
        _proveedor.Activo = chkActivo.Checked
        _proveedor.Email = Txt("Email")
        _proveedor.SitioWeb = Txt("SitioWeb")
        If FechaVal("FechaAlta").HasValue Then _proveedor.FechaAlta = FechaVal("FechaAlta").Value
        _proveedor.CondicionComercial = Txt("CondicionComercial")
        _proveedor.FormaPagoHabitual = Txt("FormaPagoHabitual")
        _proveedor.Banco = Txt("Banco")
        _proveedor.CBU = Txt("CBU")
        _proveedor.[Alias] = Txt("Alias")
        _proveedor.Observaciones = Txt("Observaciones")
        _proveedor.NotasInternas = Txt("NotasInternas")
        _proveedor.DescuentoGeneral = DecVal("DescuentoGeneral")
        _proveedor.DescuentoPorVolumen = DecVal("DescuentoPorVolumen")
        _proveedor.ObservacionesComerciales = Txt("ObservacionesComerciales")
        _proveedor.TotalCompras = DecVal("TotalCompras")
        _proveedor.CantidadOrdenes = IntValCampo("CantidadOrdenes")
        _proveedor.UltimaCompra = FechaVal("UltimaCompra")
        _proveedor.SaldoPendiente = DecVal("SaldoPendiente")
        _proveedor.FechaUltimaOperacion = FechaVal("FechaUltimaOperacion")
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    Private Function FichaTexto() As String
        Return _proveedor.Codigo & " - " & _proveedor.Nombre & vbCrLf &
               "CUIT: " & _proveedor.CUIT & vbCrLf &
               "Telefono: " & _proveedor.TelefonoLaboral & vbCrLf &
               "Localidad: " & _proveedor.Localidad & vbCrLf &
               "Saldo: $" & _proveedor.SaldoPendiente.ToString("N0")
    End Function
End Class

' ══════════════════════════════════════════════════════
'  FORM LOGIN
' ══════════════════════════════════════════════════════
Public Class LoginForm
    Inherits Form
    Private txtU As TextBox, txtP As TextBox, cmbR As ComboBox
    Private lblError As Label
    Private chkRecordar As CheckBox

    Private Usuarios As New List(Of Usuario)()

    Public Sub New()
        Try

            ConexionBD.Open()

            MessageBox.Show("Conectado correctamente a MySQL")

            ConexionBD.Close()

        Catch ex As Exception

            MessageBox.Show(ex.Message)

        End Try
        Usuarios.Add(New Usuario With {.Email = "admin@cafe.com", .Telefono = "12345678", .Password = "123456", .Rol = RolUsuario.Administrador, .Nombre = "Administrador"})
        Usuarios.Add(New Usuario With {.Email = "cajera@cafe.com", .Telefono = "87654321", .Password = "caja123", .Rol = RolUsuario.Cajero, .Nombre = "Cajera"})
        Usuarios.Add(New Usuario With {.Email = "cocina@cafe.com", .Telefono = "11111111", .Password = "cocina123", .Rol = RolUsuario.Cocina, .Nombre = "Cocinera"})

        Me.Text = "Cafe Mitre -- Acceso"
        Me.Size = New Size(900, 580)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.BackColor = Color.White

        Dim pL As New Panel With {.Location = New Point(0, 0), .Size = New Size(400, 580), .BackColor = Tema.Acento}
        Dim lEmoji As New Label With {.Text = "CM", .Font = New Font("Segoe UI", 64, FontStyle.Bold), .ForeColor = Color.FromArgb(80, 50, 20), .Location = New Point(0, 100), .Size = New Size(400, 100), .TextAlign = ContentAlignment.MiddleCenter, .BackColor = Color.Transparent}
        Dim lNm As New Label With {.Text = "CafeteriaOS", .Font = New Font("Segoe UI", 26, FontStyle.Bold), .ForeColor = Color.White, .Location = New Point(0, 210), .Size = New Size(400, 52), .TextAlign = ContentAlignment.MiddleCenter, .BackColor = Color.Transparent}
        Dim lSb As New Label With {.Text = "Sistema Integral de Cafeteria", .Font = New Font("Segoe UI", 11), .ForeColor = Tema.Acento3, .Location = New Point(0, 268), .Size = New Size(400, 30), .TextAlign = ContentAlignment.MiddleCenter, .BackColor = Color.Transparent}
        Dim lAn As New Label With {.Text = DateTime.Now.Year.ToString(), .Font = Tema.FntSmall, .ForeColor = Color.FromArgb(140, 110, 80), .Location = New Point(0, 540), .Size = New Size(400, 25), .TextAlign = ContentAlignment.MiddleCenter, .BackColor = Color.Transparent}
        pL.Controls.AddRange(New Control() {lEmoji, lNm, lSb, lAn})

        Dim pR As New Panel With {.Location = New Point(400, 0), .Size = New Size(500, 580), .BackColor = Color.White}
        Dim lBW As New Label With {.Text = "Bienvenido", .Font = New Font("Segoe UI", 24, FontStyle.Bold), .ForeColor = Tema.TextoPrinc, .Location = New Point(60, 80), .Size = New Size(380, 46), .BackColor = Color.Transparent}
        Dim lSt As New Label With {.Text = "Ingresa tus credenciales para continuar", .Font = New Font("Segoe UI", 10), .ForeColor = Tema.TextoSec, .Location = New Point(60, 130), .Size = New Size(380, 25), .BackColor = Color.Transparent}
        Dim sep As New Panel With {.Location = New Point(60, 162), .Size = New Size(50, 3), .BackColor = Tema.Acento}

        lblError = New Label With {
            .Text = "Usuario o contrasena incorrectos.",
            .Font = Tema.FntReg, .ForeColor = Tema.Rojo,
            .Location = New Point(60, 172), .Size = New Size(375, 22),
            .BackColor = Color.FromArgb(253, 236, 234),
            .Visible = False, .TextAlign = ContentAlignment.MiddleLeft
        }

        Dim lU = Tema.CrearLabel("Correo o Telefono", True)
        lU.Location = New Point(60, 190)
        txtU = Tema.CrearInput()
        txtU.Location = New Point(60, 210) : txtU.Width = 375
        txtU.Text = "admin"

        Dim lP = Tema.CrearLabel("Contrasena", True)
        lP.Location = New Point(60, 254)
        txtP = Tema.CrearInput(True)
        txtP.Location = New Point(60, 274) : txtP.Width = 375
        txtP.Text = "123456"

        Dim lR = Tema.CrearLabel("Rol", True)
        lR.Location = New Point(60, 318)
        cmbR = New ComboBox With {
            .Location = New Point(60, 338), .Width = 375,
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Font = Tema.FntInput, .BackColor = Tema.Surface2, .FlatStyle = FlatStyle.Flat
        }
        cmbR.Items.AddRange(New String() {"Administrador", "Cajero", "Cocina"})
        cmbR.SelectedIndex = 0

        chkRecordar = New CheckBox With {
            .Text = "Recordar usuario y rol",
            .Font = Tema.FntReg, .ForeColor = Tema.TextoSec,
            .Location = New Point(60, 370), .Size = New Size(220, 22),
            .BackColor = Color.Transparent
        }

        Dim btnIng = Tema.CrearBoton("INGRESAR AL SISTEMA", Tema.Acento, Color.White, 375, 46)
        btnIng.Location = New Point(60, 396)
        btnIng.Font = New Font("Segoe UI", 12, FontStyle.Bold)

        Dim lnk As New LinkLabel With {
            .Text = "Olvide mi contraseña", .Font = Tema.FntReg,
            .Location = New Point(60, 454), .Size = New Size(200, 20),
            .LinkColor = Tema.Acento2, .BackColor = Color.Transparent
        }

        pR.Controls.AddRange(New Control() {lBW, lSt, sep, lblError, lU, txtU, lP, txtP, lR, cmbR, chkRecordar, btnIng, lnk})
        Me.Controls.AddRange(New Control() {pL, pR})

        AddHandler btnIng.Click, AddressOf IngresarClick
        AddHandler txtP.KeyDown, AddressOf TxtPKeyDown
        AddHandler lnk.Click, AddressOf RecuperarClick
        AppConfig.CargarLogin(txtU, cmbR, chkRecordar)
    End Sub

    Private Sub TxtPKeyDown(s As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            IngresarClick(s, e)
        End If
    End Sub

    Private Sub IngresarClick(s As Object, e As EventArgs)
        Try

            ConexionBD.Open()

            Dim sql As String =
                "SELECT * FROM usuarios WHERE usuario=@usuario AND contraseña=@pass"

            Dim cmd As New MySqlCommand(sql, ConexionBD)

            cmd.Parameters.AddWithValue("@usuario", txtU.Text.Trim())
            cmd.Parameters.AddWithValue("@pass", txtP.Text)

            Dim dr As MySqlDataReader = cmd.ExecuteReader()

            If dr.Read() Then

                lblError.Visible = False

                Dim d As New MainDashboardForm()
                d.Show()
                Me.Hide()

            Else

                lblError.Visible = True

            End If

            dr.Close()
            ConexionBD.Close()

        Catch ex As Exception

            MessageBox.Show(ex.Message)

        End Try
    End Sub
    Private Sub RecuperarClick(s As Object, e As EventArgs)

        MessageBox.Show(
            "Recuperación de contraseña en desarrollo",
            "Información"
        )

    End Sub
End Class

' ══════════════════════════════════════════════════════
'  MAIN DASHBOARD
' ══════════════════════════════════════════════════════
Public Class MainDashboardForm
    Inherits Form

    Private posTxtNombre As TextBox
    Private posTxtTelefono As TextBox
    Private posRbLocal As RadioButton
    Private posRbLlevar As RadioButton
    Private posLblTotal As Label
    Private posLblDescuento As Label
    Private posCmbPago As ComboBox
    Private posPedido As New Pedido()
    Private posOrderPanel As FlowLayoutPanel
    Private posActiveCat As String = "Todos"
    Private posProductGrid As FlowLayoutPanel
    Private posMesaSeleccionada As String = "Mesa 1"
    Private posMesaPanel As FlowLayoutPanel
    Private posTableSection As Panel
    Private posTxtDescuento As TextBox

    Private kanPrep As FlowLayoutPanel
    Private kanListo As FlowLayoutPanel
    Private kanEntregado As FlowLayoutPanel

    Private stockSeleccionados As New List(Of Integer)()
    Private stockGrid As FlowLayoutPanel
    Private stockLblSel As Label

    Private clientesDgv As DataGridView
    Private proveedoresDgv As DataGridView
    Private proveedorSearchBox As TextBox
    Private proveedorLblCount As Label
    Private proveedorLblPage As Label
    Private proveedorPage As Integer = 1
    Private proveedorPageSize As Integer = 12

    Private pnlContenido As Panel
    Private pnlPOS As Panel
    Private pnlKanban As Panel
    Private pnlStock As Panel
    Private pnlClientes As Panel
    Private pnlProveedores As Panel
    Private pnlReportes As Panel
    Private pnlCaja As Panel

    Private sidebarBtns As New List(Of Button)()
    Private lblUsuario As Label
    Private lblCajaStatus As Label
    Private panelActual As String = "Venta"

    ' Timer para alertas de pedidos demorados
    Private tmrAlertas As Timer

    Public Sub New()
        Me.Text = "CafeteriaOS -- Panel de Gestion"
        Me.Size = New Size(1200, 760)
        Me.MinimumSize = New Size(1100, 680)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.KeyPreview = True
        Me.BackColor = Tema.BgPrincipal

        ' TOP BAR
        Dim topBar As New Panel With {.Dock = DockStyle.Top, .Height = 54, .BackColor = Tema.Acento}
        Dim lBrand As New Label With {
            .Text = "  CafeteriaOS",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .ForeColor = Color.White, .Location = New Point(0, 0),
            .Size = New Size(260, 54), .TextAlign = ContentAlignment.MiddleLeft,
            .BackColor = Color.Transparent
        }
        lblUsuario = New Label With {
            .Font = New Font("Segoe UI", 9),
            .ForeColor = Tema.Acento3,
            .Location = New Point(260, 0), .Size = New Size(300, 54),
            .TextAlign = ContentAlignment.MiddleLeft,
            .BackColor = Color.Transparent
        }
        If Datos.UsuarioActual IsNot Nothing Then
            lblUsuario.Text = "  " & Datos.UsuarioActual.Nombre & " (" & Datos.UsuarioActual.Rol.ToString() & ")"
        End If

        lblCajaStatus = New Label With {
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .ForeColor = Color.White,
            .Location = New Point(560, 0), .Size = New Size(200, 54),
            .TextAlign = ContentAlignment.MiddleCenter,
            .BackColor = Color.Transparent
        }

        Dim clock As New Label With {
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .ForeColor = Color.White,
            .Location = New Point(1060, 0), .Size = New Size(130, 54),
            .TextAlign = ContentAlignment.MiddleCenter,
            .BackColor = Color.Transparent
        }
        clock.Text = DateTime.Now.ToString("HH:mm:ss")

        Dim btnModo = Tema.CrearBoton("Oscuro", Color.FromArgb(60, 38, 18), Color.White, 90, 30)
        btnModo.Location = New Point(860, 12)

        Dim btnSalir = Tema.CrearBoton("Salir", Tema.Rojo, Color.White, 80, 30)
        btnSalir.Location = New Point(960, 12)

        Dim tmrClock As New Timer With {.Interval = 1000}
        AddHandler tmrClock.Tick, Sub(s, e)
                                      clock.Text = DateTime.Now.ToString("HH:mm:ss")
                                      lblCajaStatus.Text = If(Datos.Caja.Abierta, "Caja Abierta", "Caja Cerrada")
                                      lblCajaStatus.ForeColor = If(Datos.Caja.Abierta, Tema.Acento3, Tema.Rojo)
                                  End Sub
        tmrClock.Start()

        topBar.Controls.AddRange(New Control() {lBrand, lblUsuario, lblCajaStatus, clock, btnModo, btnSalir})

        ' SIDEBAR
        Dim sidebar As New Panel With {.Dock = DockStyle.Left, .Width = 200, .BackColor = Tema.SidebarBg}
        Dim lLogo As New Label With {
            .Text = "Cafeteria Mitre",
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .ForeColor = Color.White, .Location = New Point(0, 18),
            .Size = New Size(200, 36), .TextAlign = ContentAlignment.MiddleCenter,
            .BackColor = Color.Transparent
        }
        Dim sepSide As New Panel With {.Location = New Point(16, 58), .Size = New Size(168, 1), .BackColor = Color.FromArgb(60, 45, 30)}
        sidebar.Controls.AddRange(New Control() {lLogo, sepSide})

        Dim menuY As Integer = 74
        For Each item In New String() {"Venta", "Pedidos", "Stock", "Clientes", "Proveedores", "Reportes", "Caja"}
            Dim txt As String = item
            Dim btn As New Button With {
                .Text = "  " & txt,
                .Location = New Point(0, menuY), .Size = New Size(200, 46),
                .BackColor = Color.Transparent,
                .ForeColor = Color.FromArgb(180, 155, 130),
                .Font = New Font("Segoe UI", 10),
                .FlatStyle = FlatStyle.Flat,
                .TextAlign = ContentAlignment.MiddleLeft,
                .Cursor = Cursors.Hand, .Tag = txt
            }
            btn.FlatAppearance.BorderSize = 0
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(55, 38, 22)
            AddHandler btn.Click, AddressOf MenuClick
            sidebar.Controls.Add(btn)
            sidebarBtns.Add(btn)
            menuY += 46
        Next

        pnlContenido = New Panel With {.Dock = DockStyle.Fill, .BackColor = Tema.BgPrincipal}

        pnlPOS = BuildPOS()
        pnlKanban = BuildKanban()
        pnlStock = BuildStock()
        pnlClientes = BuildClientes()
        pnlProveedores = BuildProveedores()
        pnlReportes = BuildReportes()
        pnlCaja = New Panel With {.BackColor = Tema.BgPrincipal}

        Me.Controls.AddRange(New Control() {pnlContenido, sidebar, topBar})
        MostrarPanel("Venta")

        ' Timer alertas pedidos demorados
        tmrAlertas = New Timer With {.Interval = 60000}
        AddHandler tmrAlertas.Tick, AddressOf VerificarPedidosDemorados
        tmrAlertas.Start()

        AddHandler btnModo.Click, Sub(s, e)
                                      Tema.ModoOscuro = Not Tema.ModoOscuro
                                      btnModo.Text = If(Tema.ModoOscuro, "Claro", "Oscuro")
                                      Me.BackColor = Tema.BgPrincipal
                                      topBar.BackColor = Tema.Acento
                                      sidebar.BackColor = Tema.SidebarBg
                                      pnlContenido.BackColor = Tema.BgPrincipal
                                      pnlPOS = BuildPOS()
                                      pnlKanban = BuildKanban()
                                      pnlStock = BuildStock()
                                      pnlClientes = BuildClientes()
                                      pnlProveedores = BuildProveedores()
                                      pnlReportes = BuildReportes()
                                      pnlCaja = New CajaForm()
                                      MostrarPanel(panelActual)
                                  End Sub
        AddHandler Me.KeyDown, AddressOf DashboardKeyDown
        AddHandler btnSalir.Click, Sub(s, e)
                                       If MessageBox.Show("Cerrar sesion?", "Salir", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                                           Datos.UsuarioActual = Nothing
                                           Dim lf As New LoginForm()
                                           lf.Show()
                                           Me.Close()
                                       End If
                                   End Sub
    End Sub

    Private Sub VerificarPedidosDemorados(s As Object, e As EventArgs)
        Dim limite As Integer = 20 ' minutos
        For Each p In Datos.ListaPedidos
            If p.Estado = EstadoPedido.EnPreparacion Then
                Dim mins As Double = (DateTime.Now - p.FechaHora).TotalMinutes
                If mins > limite Then
                    SystemSounds.Exclamation.Play()
                    MessageBox.Show("ALERTA: Pedido #" & p.ID & " de " & p.NombreCliente & " lleva mas de " & CInt(mins) & " minutos en preparacion.",
                                    "Pedido demorado", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                End If
            End If
        Next
    End Sub

    Private Sub MenuClick(s As Object, e As EventArgs)
        MostrarPanel(CType(s, Button).Tag.ToString())
    End Sub

    Private Sub MostrarPanel(nombre As String)
        panelActual = nombre
        For Each b In sidebarBtns
            If b.Tag.ToString() = nombre Then
                b.BackColor = Tema.Acento
                b.ForeColor = Color.White
            Else
                b.BackColor = Color.Transparent
                b.ForeColor = Color.FromArgb(180, 155, 130)
            End If
        Next

        pnlContenido.Controls.Clear()
        Select Case nombre
            Case "Venta"
                pnlPOS.Dock = DockStyle.Fill
                pnlContenido.Controls.Add(pnlPOS)
            Case "Pedidos"
                KanbanRefrescar()
                pnlKanban.Dock = DockStyle.Fill
                pnlContenido.Controls.Add(pnlKanban)
            Case "Stock"
                StockRefrescar()
                pnlStock.Dock = DockStyle.Fill
                pnlContenido.Controls.Add(pnlStock)
            Case "Clientes"
                ClientesRefrescar()
                pnlClientes.Dock = DockStyle.Fill
                pnlContenido.Controls.Add(pnlClientes)
            Case "Proveedores"
                If Not TienePermisoProveedores("Ver") Then
                    MessageBox.Show("No tenes permiso para ver proveedores.", "Permisos", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    MostrarPanel("Venta")
                    Return
                End If
                ProveedoresRefrescar()
                pnlProveedores.Dock = DockStyle.Fill
                pnlContenido.Controls.Add(pnlProveedores)
            Case "Reportes"
                ReportesRefrescar()
                pnlReportes.Dock = DockStyle.Fill
                pnlContenido.Controls.Add(pnlReportes)
            Case "Caja"
                pnlCaja = New CajaForm()
                pnlCaja.Dock = DockStyle.Fill
                pnlContenido.Controls.Add(pnlCaja)
        End Select
    End Sub

    Private Function MkHeader(titulo As String) As Panel
        Dim ph As New Panel With {.Dock = DockStyle.Top, .Height = 58, .BackColor = Tema.Acento}
        Dim lh As New Label With {
            .Text = titulo, .Font = New Font("Segoe UI", 15, FontStyle.Bold),
            .ForeColor = Color.White, .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleLeft,
            .Padding = New Padding(18, 0, 0, 0), .BackColor = Color.Transparent
        }
        ph.Controls.Add(lh)
        Return ph
    End Function

    Private Function EstilarDgv(dgv As DataGridView) As DataGridView
        dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(240, 230, 215)
        dgv.DefaultCellStyle.SelectionForeColor = Tema.TextoPrinc
        dgv.DefaultCellStyle.Font = Tema.FntReg
        dgv.DefaultCellStyle.BackColor = Tema.Surface
        dgv.ColumnHeadersDefaultCellStyle.BackColor = Tema.Surface2
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = Tema.TextoTer
        dgv.ColumnHeadersDefaultCellStyle.Font = Tema.FntBold
        dgv.EnableHeadersVisualStyles = False
        dgv.AlternatingRowsDefaultCellStyle.BackColor = Tema.Surface2
        Return dgv
    End Function

    ' ══ BUILD POS ══
    Private Function BuildPOS() As Panel
        Dim pnl As New Panel With {.BackColor = Tema.BgPrincipal}
        pnl.Controls.Add(MkHeader("Punto de Venta"))

        Dim leftW As Integer = 470

        Dim searchBox As New TextBox With {
            .Location = New Point(12, 66), .Size = New Size(leftW, 32),
            .Font = Tema.FntInput, .BackColor = Tema.Surface,
            .ForeColor = Tema.TextoPrinc, .BorderStyle = BorderStyle.FixedSingle
        }

        Dim catFlow As New FlowLayoutPanel With {
            .Location = New Point(12, 104), .Size = New Size(leftW, 76),
            .FlowDirection = FlowDirection.LeftToRight, .WrapContents = True,
            .AutoScroll = False, .BackColor = Color.Transparent
        }
        Dim cats As New List(Of String)()
        cats.Add("Todos")
        For Each p In Datos.ListaProductos
            If Not cats.Contains(p.Categoria) Then cats.Add(p.Categoria)
        Next
        For Each cat In cats
            Dim catName As String = cat
            Dim cpill As New Button With {
                .Text = catName, .Font = Tema.FntSmallB, .Size = New Size(108, 30),
                .Cursor = Cursors.Hand, .FlatStyle = FlatStyle.Flat,
                .Margin = New Padding(0, 3, 6, 3), .Tag = catName
            }
            cpill.FlatAppearance.BorderSize = 1
            If catName = "Todos" Then
                cpill.BackColor = Tema.Acento : cpill.ForeColor = Color.White : cpill.FlatAppearance.BorderColor = Tema.Acento
            Else
                cpill.BackColor = Tema.Surface3 : cpill.ForeColor = Tema.TextoSec : cpill.FlatAppearance.BorderColor = Tema.Borde
            End If
            AddHandler cpill.Click, AddressOf CatPillClick
            catFlow.Controls.Add(cpill)
        Next

        posProductGrid = New FlowLayoutPanel With {
            .Location = New Point(12, 188), .Size = New Size(leftW, 498),
            .FlowDirection = FlowDirection.LeftToRight, .WrapContents = True,
            .AutoScroll = True, .BackColor = Color.Transparent,
            .Padding = New Padding(0, 4, 0, 4)
        }

        Dim rightX As Integer = leftW + 28
        Dim rightW As Integer = 320
        Dim rightPanel As New Panel With {
            .Location = New Point(rightX, 66), .Size = New Size(rightW, 670),
            .BackColor = Tema.Surface
        }

        Dim lOrd As New Label With {
            .Text = "Pedido actual",
            .Font = New Font("Segoe UI", 13, FontStyle.Bold),
            .ForeColor = Tema.Acento, .Location = New Point(14, 12),
            .AutoSize = True, .BackColor = Color.Transparent
        }

        Dim lTipo = Tema.CrearLabel("TIPO DE CONSUMO", True, 8)
        lTipo.Location = New Point(14, 44) : lTipo.ForeColor = Tema.TextoTer

        posRbLocal = New RadioButton With {
            .Text = "En el local", .Font = Tema.FntReg, .ForeColor = Tema.TextoPrinc,
            .Checked = True, .Location = New Point(14, 62), .Width = 135,
            .BackColor = Color.Transparent
        }
        posRbLlevar = New RadioButton With {
            .Text = "Para llevar", .Font = Tema.FntReg, .ForeColor = Tema.TextoPrinc,
            .Location = New Point(160, 62), .Width = 135, .BackColor = Color.Transparent
        }
        AddHandler posRbLocal.CheckedChanged, AddressOf RbLocalChanged

        posTableSection = New Panel With {.Location = New Point(14, 90), .Size = New Size(292, 68), .BackColor = Color.Transparent}
        posMesaPanel = New FlowLayoutPanel With {
            .Dock = DockStyle.Fill, .BackColor = Color.Transparent,
            .FlowDirection = FlowDirection.LeftToRight, .WrapContents = True
        }
        For Each mesa In New String() {"Mesa 1", "Mesa 2", "Mesa 3", "Mesa 4", "Mesa 5", "Barra"}
            Dim mesaNm As String = mesa
            Dim mb As New Button With {
                .Text = mesaNm, .Font = Tema.FntSmallB, .Size = New Size(68, 28),
                .Cursor = Cursors.Hand, .FlatStyle = FlatStyle.Flat,
                .Margin = New Padding(0, 0, 4, 4), .Tag = mesaNm
            }
            mb.FlatAppearance.BorderSize = 1
            If mesaNm = "Mesa 1" Then
                mb.BackColor = Tema.Acento : mb.ForeColor = Color.White : mb.FlatAppearance.BorderColor = Tema.Acento
            Else
                mb.BackColor = Tema.Surface2 : mb.ForeColor = Tema.TextoSec : mb.FlatAppearance.BorderColor = Tema.Borde
            End If
            AddHandler mb.Click, AddressOf MesaBtnClick
            posMesaPanel.Controls.Add(mb)
        Next
        posTableSection.Controls.Add(posMesaPanel)

        Dim lCli = Tema.CrearLabel("DATOS DEL CLIENTE", True, 8)
        lCli.Location = New Point(14, 166) : lCli.ForeColor = Tema.TextoTer
        posTxtNombre = Tema.CrearInput()
        posTxtNombre.Location = New Point(14, 184) : posTxtNombre.Width = 292
        Dim lNomPH As New Label With {.Text = "Nombre del cliente", .Font = Tema.FntSmall, .ForeColor = Tema.TextoTer, .Location = New Point(18, 187), .AutoSize = True, .BackColor = Color.Transparent}

        posTxtTelefono = Tema.CrearInput()
        posTxtTelefono.Location = New Point(14, 222) : posTxtTelefono.Width = 292
        Dim lTelPH As New Label With {.Text = "Telefono (opcional)", .Font = Tema.FntSmall, .ForeColor = Tema.TextoTer, .Location = New Point(18, 225), .AutoSize = True, .BackColor = Color.Transparent}

        AddHandler posTxtNombre.TextChanged, AddressOf TxtNombreChanged
        AddHandler posTxtTelefono.TextChanged, AddressOf TxtTelefonoChanged

        ' Descuento
        Dim lDesc = Tema.CrearLabel("DESCUENTO ($)", True, 8)
        lDesc.Location = New Point(14, 258) : lDesc.ForeColor = Tema.TextoTer
        posTxtDescuento = Tema.CrearInput()
        posTxtDescuento.Location = New Point(14, 276) : posTxtDescuento.Width = 140 : posTxtDescuento.Text = "0"
        AddHandler posTxtDescuento.TextChanged, AddressOf DescuentoChanged

        Dim lItems = Tema.CrearLabel("ORDEN ACTUAL", True, 8)
        lItems.Location = New Point(14, 304) : lItems.ForeColor = Tema.TextoTer

        posOrderPanel = New FlowLayoutPanel With {
            .Location = New Point(14, 322), .Size = New Size(292, 130),
            .FlowDirection = FlowDirection.TopDown, .WrapContents = False,
            .AutoScroll = True, .BackColor = Color.Transparent
        }

        Dim sepT = Tema.MkSep(14, 460, 292)

        posLblDescuento = New Label With {
            .Text = "Descuento: $0", .Font = Tema.FntReg,
            .ForeColor = Tema.Rojo, .Location = New Point(14, 466),
            .Size = New Size(292, 20), .TextAlign = ContentAlignment.MiddleRight,
            .BackColor = Color.Transparent
        }

        posLblTotal = New Label With {
            .Text = "TOTAL: $0", .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .ForeColor = Tema.Acento, .Location = New Point(14, 488),
            .Size = New Size(292, 30), .TextAlign = ContentAlignment.MiddleRight,
            .BackColor = Color.Transparent
        }

        Dim lPag = Tema.CrearLabel("METODO DE PAGO", True, 8)
        lPag.Location = New Point(14, 524) : lPag.ForeColor = Tema.TextoTer
        posCmbPago = New ComboBox With {
            .Location = New Point(14, 542), .Width = 292,
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Font = Tema.FntInput, .BackColor = Tema.Surface2, .FlatStyle = FlatStyle.Flat
        }
        posCmbPago.Items.AddRange(New String() {"Efectivo", "Debito", "Credito", "MercadoPago", "Transferencia"})
        posCmbPago.SelectedIndex = 0

        Dim btnConf = Tema.CrearBoton("CONFIRMAR Y EMITIR TICKET", Tema.Verde, Color.White, 292, 46)
        btnConf.Location = New Point(14, 576)
        btnConf.Font = New Font("Segoe UI", 11, FontStyle.Bold)

        Dim btnLimp = Tema.CrearBoton("Limpiar pedido", Tema.Surface3, Tema.TextoSec, 140, 32)
        btnLimp.Location = New Point(14, 626)

        Dim btnCancelar = Tema.CrearBoton("Cancelar pedido", Tema.Rojo, Color.White, 140, 32)
        btnCancelar.Location = New Point(162, 626)

        rightPanel.Controls.AddRange(New Control() {
            lOrd, lTipo, posRbLocal, posRbLlevar, posTableSection,
            lCli, posTxtNombre, lNomPH, posTxtTelefono, lTelPH,
            lDesc, posTxtDescuento,
            lItems, posOrderPanel, sepT, posLblDescuento, posLblTotal,
            lPag, posCmbPago, btnConf, btnLimp, btnCancelar
        })

        pnl.Controls.AddRange(New Control() {searchBox, catFlow, posProductGrid, rightPanel})

        AddHandler searchBox.TextChanged, AddressOf SearchBoxChanged
        AddHandler btnConf.Click, AddressOf POSConfirmar
        AddHandler btnLimp.Click, AddressOf LimpiarPedido
        AddHandler btnCancelar.Click, AddressOf CancelarPedidoActual

        POSRenderProductos("")
        Return pnl
    End Function

    Private Sub DescuentoChanged(s As Object, e As EventArgs)
        POSActualizarOrden()
    End Sub

    Private Sub TxtNombreChanged(s As Object, e As EventArgs)
        Dim lbl = CType(posTxtNombre.Parent.Controls.OfType(Of Label)().FirstOrDefault(Function(l) l.Text = "Nombre del cliente"), Label)
        If lbl IsNot Nothing Then lbl.Visible = String.IsNullOrEmpty(posTxtNombre.Text)
    End Sub

    Private Sub TxtTelefonoChanged(s As Object, e As EventArgs)
        Dim lbl = CType(posTxtTelefono.Parent.Controls.OfType(Of Label)().FirstOrDefault(Function(l) l.Text = "Telefono (opcional)"), Label)
        If lbl IsNot Nothing Then lbl.Visible = String.IsNullOrEmpty(posTxtTelefono.Text)
    End Sub

    Private Sub RbLocalChanged(s As Object, e As EventArgs)
        posTableSection.Visible = posRbLocal.Checked
    End Sub

    Private Sub SearchBoxChanged(s As Object, e As EventArgs)
        POSRenderProductos(CType(s, TextBox).Text)
    End Sub

    Private Sub LimpiarPedido(s As Object, e As EventArgs)
        posPedido = New Pedido()
        posTxtDescuento.Text = "0"
        POSActualizarOrden()
    End Sub

    Private Sub CancelarPedidoActual(s As Object, e As EventArgs)
        If posPedido.Items.Count = 0 Then Return
        Dim motivo As String = InputBox("Motivo de cancelacion:", "Cancelar pedido")
        If String.IsNullOrWhiteSpace(motivo) Then Return
        ' Devolver stock
        For Each item In posPedido.Items
            item.Producto.Stock += item.Cantidad
        Next
        posPedido = New Pedido()
        posTxtDescuento.Text = "0"
        POSActualizarOrden()
        MessageBox.Show("Pedido cancelado. Stock restaurado.", "Cancelado", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub CatPillClick(s As Object, e As EventArgs)
        Dim btn = CType(s, Button)
        posActiveCat = btn.Tag.ToString()
        Dim flow = btn.Parent
        For Each b As Button In flow.Controls.OfType(Of Button)()
            b.BackColor = Tema.Surface3 : b.ForeColor = Tema.TextoSec : b.FlatAppearance.BorderColor = Tema.Borde
        Next
        btn.BackColor = Tema.Acento : btn.ForeColor = Color.White : btn.FlatAppearance.BorderColor = Tema.Acento
        POSRenderProductos("")
    End Sub

    Private Sub MesaBtnClick(s As Object, e As EventArgs)
        Dim btn = CType(s, Button)
        posMesaSeleccionada = btn.Tag.ToString()
        For Each b As Button In posMesaPanel.Controls.OfType(Of Button)()
            b.BackColor = Tema.Surface2 : b.ForeColor = Tema.TextoSec : b.FlatAppearance.BorderColor = Tema.Borde
        Next
        btn.BackColor = Tema.Acento : btn.ForeColor = Color.White : btn.FlatAppearance.BorderColor = Tema.Acento
    End Sub

    Private Function POSAtajoProducto(p As Producto) As String
        Select Case p.ID
            Case 1 : Return "Ctrl+E" ' Espresso
            Case 2 : Return "Ctrl+C" ' Cafe con leche
            Case 3 : Return "Ctrl+P" ' Cappuccino
            Case 4 : Return "Ctrl+L" ' Latte
            Case 5 : Return "Ctrl+H" ' Chocolate caliente
            Case 6 : Return "Ctrl+T" ' Te
            Case 7 : Return "Ctrl+J" ' Jugo
            Case 8 : Return "Ctrl+A" ' Agua
            Case 9 : Return "Ctrl+M" ' Medialuna
            Case 10 : Return "Ctrl+O" ' Tostado
            Case 11 : Return "Ctrl+U" ' Muffin
            Case 12 : Return "Ctrl+K" ' Cheese Cake
            Case 13 : Return "Ctrl+B" ' Brownie
            Case Else : Return ""
        End Select
    End Function

    Private Function POSProductoPorAtajo(key As Keys) As Producto
        Dim id As Integer = 0
        Select Case key
            Case Keys.E : id = 1
            Case Keys.C : id = 2
            Case Keys.P : id = 3
            Case Keys.L : id = 4
            Case Keys.H : id = 5
            Case Keys.T : id = 6
            Case Keys.J : id = 7
            Case Keys.A : id = 8
            Case Keys.M : id = 9
            Case Keys.O : id = 10
            Case Keys.U : id = 11
            Case Keys.K : id = 12
            Case Keys.B : id = 13
        End Select
        If id = 0 Then Return Nothing
        Return Datos.ListaProductos.FirstOrDefault(Function(p) p.ID = id)
    End Function

    Private Sub DashboardKeyDown(s As Object, e As KeyEventArgs)
        If Not e.Control Then Return
        If panelActual <> "Venta" Then Return
        Dim prod = POSProductoPorAtajo(e.KeyCode)
        If prod Is Nothing Then Return
        e.SuppressKeyPress = True
        If prod.Stock <= 0 Then
            MessageBox.Show(prod.Nombre & " no tiene stock disponible.", "Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        POSAbrirEspecificaciones(prod)
    End Sub
    Private Sub POSRenderProductos(filtro As String)
        posProductGrid.Controls.Clear()
        For Each prod In Datos.ListaProductos
            Dim catOk As Boolean = posActiveCat = "Todos" OrElse prod.Categoria = posActiveCat
            Dim nomOk As Boolean = prod.Nombre.ToLower().Contains(filtro.ToLower())
            If Not (catOk AndAlso nomOk) Then Continue For

            Dim p As Producto = prod
            Dim card As New Panel With {
                .Size = New Size(145, 118), .BackColor = Tema.Surface,
                .Margin = New Padding(0, 0, 8, 8), .Cursor = Cursors.Hand, .Tag = p
            }

            Dim lAtajo As New Label With {
                .Text = POSAtajoProducto(p), .Font = Tema.FntSmallB, .ForeColor = Tema.Acento,
                .Location = New Point(6, 4), .Size = New Size(62, 18),
                .TextAlign = ContentAlignment.MiddleLeft, .BackColor = Color.Transparent
            }
            Dim lNm As New Label With {
                .Text = p.Nombre, .Font = Tema.FntBold, .ForeColor = Tema.TextoPrinc,
                .Location = New Point(6, 24), .Size = New Size(133, 34),
                .TextAlign = ContentAlignment.TopCenter, .BackColor = Color.Transparent
            }
            Dim lPr As New Label With {
                .Text = "$" & p.Precio.ToString("N0"),
                .Font = New Font("Segoe UI", 10, FontStyle.Bold), .ForeColor = Tema.Acento2,
                .Location = New Point(0, 54), .Size = New Size(145, 22),
                .TextAlign = ContentAlignment.MiddleCenter, .BackColor = Color.Transparent
            }
            Dim lCat As New Label With {
                .Text = p.Categoria, .Font = Tema.FntSmall, .ForeColor = Tema.TextoTer,
                .Location = New Point(0, 80), .Size = New Size(145, 18),
                .TextAlign = ContentAlignment.MiddleCenter, .BackColor = Color.Transparent
            }
            Dim stockColor As Color = If(p.Stock <= 5, Tema.Rojo, If(p.Stock <= 10, Tema.Naranja, Tema.Verde))
            Dim lSt As New Label With {
                .Text = If(p.Stock <= 10, "!" & p.Stock.ToString(), p.Stock.ToString()),
                .Font = Tema.FntSmallB, .ForeColor = stockColor,
                .Location = New Point(96, 4), .Size = New Size(42, 18),
                .TextAlign = ContentAlignment.MiddleRight, .BackColor = Color.Transparent
            }

            card.Controls.AddRange(New Control() {lAtajo, lNm, lPr, lCat, lSt})

            If p.Stock > 0 Then
                AddHandler card.Click, AddressOf ProductCardClick
                For Each ctrl As Control In card.Controls
                    AddHandler ctrl.Click, AddressOf ProductCardClick
                    ctrl.Tag = p
                Next
                card.Tag = p
            Else
                card.BackColor = Color.FromArgb(245, 245, 245)
                lNm.ForeColor = Tema.TextoTer
            End If

            posProductGrid.Controls.Add(card)
        Next
    End Sub

    Private Sub ProductCardClick(s As Object, e As EventArgs)
        Dim ctrl = CType(s, Control)
        Dim prod As Producto = Nothing
        If TypeOf ctrl.Tag Is Producto Then
            prod = CType(ctrl.Tag, Producto)
        ElseIf ctrl.Parent IsNot Nothing AndAlso TypeOf ctrl.Parent.Tag Is Producto Then
            prod = CType(ctrl.Parent.Tag, Producto)
        End If
        If prod Is Nothing Then Return
        POSAbrirEspecificaciones(prod)
    End Sub

    Private Sub POSAbrirEspecificaciones(prod As Producto)
        Dim dlg As New EspecificacionesForm(prod)
        If dlg.ShowDialog(Me) <> DialogResult.OK Then Return

        Dim parts As New List(Of String)()
        For Each kvp In dlg.GruposSeleccionados
            parts.Add(kvp.Key & ": " & kvp.Value)
        Next
        If Not String.IsNullOrWhiteSpace(dlg.NotasAdicionales) Then
            parts.Add(dlg.NotasAdicionales)
        End If
        Dim specStr As String = String.Join(" | ", parts)

        Dim existing As DetallePedido = Nothing
        For Each i In posPedido.Items
            If i.Producto.ID = prod.ID AndAlso i.Especificaciones = specStr Then
                existing = i : Exit For
            End If
        Next

        If existing IsNot Nothing Then
            existing.Cantidad += 1
        Else
            posPedido.Items.Add(New DetallePedido With {.Producto = prod, .Cantidad = 1, .Especificaciones = specStr})
        End If

        POSActualizarOrden()

        If prod.Stock <= 10 Then
            SystemSounds.Beep.Play()
            MessageBox.Show("ALERTA: Stock bajo en " & prod.Nombre & " (" & prod.Stock & " unidades)",
                            "Stock Critico", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End If
    End Sub

    Private Sub POSActualizarOrden()
        posOrderPanel.Controls.Clear()

        Dim descuento As Decimal = 0
        Decimal.TryParse(posTxtDescuento.Text, descuento)
        posPedido.Descuento = Math.Max(0, descuento)

        If posPedido.Items.Count = 0 Then
            posOrderPanel.Controls.Add(New Label With {
                .Text = "Sin productos aun", .Font = Tema.FntReg, .ForeColor = Tema.TextoTer,
                .AutoSize = False, .Size = New Size(280, 40),
                .TextAlign = ContentAlignment.MiddleCenter, .BackColor = Color.Transparent
            })
        Else
            For Each item In posPedido.Items.ToList()
                Dim it As DetallePedido = item
                Dim row As New Panel With {
                    .Size = New Size(278, 52), .BackColor = Tema.Surface2,
                    .Margin = New Padding(0, 0, 0, 4), .Tag = it
                }
                Dim lNm As New Label With {.Text = it.Producto.Nombre, .Font = Tema.FntBold, .ForeColor = Tema.TextoPrinc, .Location = New Point(8, 4), .Size = New Size(160, 18), .BackColor = Color.Transparent}
                Dim lSp As New Label With {.Text = it.Especificaciones, .Font = Tema.FntSmall, .ForeColor = Tema.TextoTer, .Location = New Point(8, 22), .Size = New Size(160, 16), .BackColor = Color.Transparent}
                Dim lPr As New Label With {.Text = "$" & it.Subtotal.ToString("N0"), .Font = New Font("Segoe UI", 9, FontStyle.Bold), .ForeColor = Tema.Acento2, .Location = New Point(8, 38), .Size = New Size(100, 14), .BackColor = Color.Transparent}

                Dim btnM As New Button With {.Text = "-", .Size = New Size(24, 24), .Location = New Point(196, 8), .FlatStyle = FlatStyle.Flat, .BackColor = Tema.Surface3, .ForeColor = Tema.TextoSec, .Cursor = Cursors.Hand, .Font = New Font("Segoe UI", 12), .Tag = it}
                btnM.FlatAppearance.BorderSize = 0
                Dim lQ As New Label With {.Text = it.Cantidad.ToString(), .Font = Tema.FntBold, .Location = New Point(222, 10), .Size = New Size(20, 20), .TextAlign = ContentAlignment.MiddleCenter, .BackColor = Color.Transparent}
                Dim btnP As New Button With {.Text = "+", .Size = New Size(24, 24), .Location = New Point(244, 8), .FlatStyle = FlatStyle.Flat, .BackColor = Tema.Surface3, .ForeColor = Tema.TextoSec, .Cursor = Cursors.Hand, .Font = New Font("Segoe UI", 12), .Tag = it}
                btnP.FlatAppearance.BorderSize = 0

                AddHandler btnM.Click, AddressOf OrderItemMinus
                AddHandler btnP.Click, AddressOf OrderItemPlus
                row.Controls.AddRange(New Control() {lNm, lSp, lPr, btnM, lQ, btnP})
                posOrderPanel.Controls.Add(row)
            Next
        End If

        posLblDescuento.Text = If(posPedido.Descuento > 0, "Descuento: -$" & posPedido.Descuento.ToString("N0"), "")
        posLblTotal.Text = "TOTAL: $" & posPedido.Total.ToString("N0")
    End Sub

    Private Sub OrderItemMinus(s As Object, e As EventArgs)
        Dim it = CType(CType(s, Button).Tag, DetallePedido)
        it.Cantidad -= 1
        If it.Cantidad <= 0 Then posPedido.Items.Remove(it)
        POSActualizarOrden()
    End Sub

    Private Sub OrderItemPlus(s As Object, e As EventArgs)
        Dim it = CType(CType(s, Button).Tag, DetallePedido)
        it.Cantidad += 1
        POSActualizarOrden()
    End Sub

    Private Sub POSConfirmar(s As Object, e As EventArgs)
        If String.IsNullOrWhiteSpace(posTxtNombre.Text) Then
            MessageBox.Show("El nombre del cliente es obligatorio.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        If posPedido.Items.Count = 0 Then
            MessageBox.Show("Debe agregar al menos un producto.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        posPedido.ID = Datos.ListaPedidos.Count + 1
        posPedido.NombreCliente = posTxtNombre.Text.Trim()
        posPedido.TelefonoCliente = posTxtTelefono.Text.Trim()
        posPedido.TipoServicio = If(posRbLocal.Checked, "En el local", "Para llevar")
        posPedido.Mesa = If(posRbLocal.Checked, posMesaSeleccionada, "Para llevar")
        posPedido.MetodoPago = posCmbPago.SelectedItem.ToString()
        posPedido.UsuarioResponsable = If(Datos.UsuarioActual IsNot Nothing, Datos.UsuarioActual.Nombre, "Sistema")

        Dim descuento As Decimal = 0
        Decimal.TryParse(posTxtDescuento.Text, descuento)
        posPedido.Descuento = Math.Max(0, descuento)

        For Each item In posPedido.Items
            item.Producto.Stock = Math.Max(0, item.Producto.Stock - item.Cantidad)
        Next

        Dim stockCritico = Datos.ListaProductos.Where(Function(p) p.Stock <= 10).ToList()
        If stockCritico.Count > 0 Then
            Dim msg As New StringBuilder("Aviso para administrador: productos con stock bajo:" & Environment.NewLine)
            For Each sp In stockCritico
                msg.AppendLine("- " & sp.Nombre & ": " & sp.Stock & " unidades")
            Next
            MessageBox.Show(msg.ToString(), "Stock critico", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End If

        Datos.RegistrarOActualizarCliente(posPedido.NombreCliente, posPedido.TelefonoCliente, posPedido.Total, posPedido.MetodoPago, posPedido.ID)
        Datos.ListaPedidos.Add(posPedido)

        ' Registrar en caja si esta abierta
        If Datos.Caja.Abierta Then
            Datos.Caja.Movimientos.Add(New MovimientoCaja With {
                .Tipo = "Venta",
                .Monto = posPedido.Total,
                .Descripcion = "Pedido #" & posPedido.ID & " - " & posPedido.NombreCliente,
                .Usuario = posPedido.UsuarioResponsable
            })
        End If

        ' Sonido nuevo pedido
        SystemSounds.Asterisk.Play()

        Dim tk As New TicketForm(posPedido)
        tk.ShowDialog(Me)

        posPedido = New Pedido()
        posTxtNombre.Clear()
        posTxtTelefono.Clear()
        posTxtDescuento.Text = "0"
        POSActualizarOrden()
        POSRenderProductos("")
    End Sub

    ' ══ BUILD KANBAN ══
    Private Function BuildKanban() As Panel
        Dim pnl As New Panel With {.BackColor = Tema.BgPrincipal}
        pnl.Controls.Add(MkHeader("Monitor de Produccion -- Cocina"))

        ' Buscador de pedidos
        Dim txtBuscar As New TextBox With {
            .Location = New Point(12, 66), .Width = 300, .Height = 30,
            .Font = Tema.FntInput, .BackColor = Tema.Surface,
            .ForeColor = Tema.TextoPrinc, .BorderStyle = BorderStyle.FixedSingle
        }
        Dim lBuscar As New Label With {.Text = "Buscar pedido...", .Font = Tema.FntSmall, .ForeColor = Tema.TextoTer, .Location = New Point(16, 70), .AutoSize = True, .BackColor = Color.Transparent}
        AddHandler txtBuscar.TextChanged, Sub(s, e)
                                              lBuscar.Visible = String.IsNullOrEmpty(txtBuscar.Text)
                                              KanbanRefrescarFiltrado(txtBuscar.Text)
                                          End Sub
        pnl.Controls.AddRange(New Control() {txtBuscar, lBuscar})

        Dim titulos() As String = {"En preparacion", "Listos para servir", "Entregados"}
        Dim colores() As Color = {Tema.Naranja, Tema.Verde, Tema.TextoTer}
        Dim colW As Integer = 300

        For i As Integer = 0 To 2
            Dim idx As Integer = i
            Dim colCard = Tema.MkCard(12 + idx * (colW + 12), 106, colW, 530)

            Dim dot As New Panel With {.Location = New Point(14, 16), .Size = New Size(10, 10), .BackColor = colores(idx)}
            Dim lT As New Label With {
                .Text = titulos(idx), .Font = Tema.FntBold, .ForeColor = Tema.TextoPrinc,
                .Location = New Point(32, 12), .Size = New Size(250, 20), .BackColor = Color.Transparent
            }
            Dim sep = Tema.MkSep(14, 38, colW - 28)
            colCard.Controls.AddRange(New Control() {dot, lT, sep})

            Dim flow As New FlowLayoutPanel With {
                .Location = New Point(0, 44), .Size = New Size(colW, 482),
                .FlowDirection = FlowDirection.TopDown, .WrapContents = False,
                .AutoScroll = True, .BackColor = Color.Transparent,
                .Padding = New Padding(10, 6, 10, 6)
            }

            Select Case idx
                Case 0 : kanPrep = flow
                Case 1 : kanListo = flow
                Case 2 : kanEntregado = flow
            End Select

            colCard.Controls.Add(flow)
            pnl.Controls.Add(colCard)
        Next
        Return pnl
    End Function

    Private Sub KanbanRefrescar()
        KanbanRefrescarFiltrado("")
    End Sub

    Private Sub KanbanRefrescarFiltrado(filtro As String)
        kanPrep.Controls.Clear()
        kanListo.Controls.Clear()
        kanEntregado.Controls.Clear()

        For Each p In Datos.ListaPedidos
            If p.Estado = EstadoPedido.Cancelado Then Continue For

            Dim nomOk As Boolean = String.IsNullOrEmpty(filtro) OrElse
                                   p.NombreCliente.ToLower().Contains(filtro.ToLower()) OrElse
                                   p.ID.ToString().Contains(filtro)
            If Not nomOk Then Continue For

            Dim pedido As Pedido = p
            Dim target As FlowLayoutPanel = Nothing
            Select Case p.Estado
                Case EstadoPedido.EnPreparacion : target = kanPrep
                Case EstadoPedido.Listo : target = kanListo
                Case EstadoPedido.Entregado : target = kanEntregado
            End Select
            If target Is Nothing Then Continue For

            Dim mins As Integer = CInt((DateTime.Now - p.FechaHora).TotalMinutes)
            Dim cardBg As Color = If(p.Estado = EstadoPedido.EnPreparacion AndAlso mins > 20, Color.FromArgb(255, 243, 230), Tema.Surface2)

            Dim card As New Panel With {.Width = 268, .BackColor = cardBg, .Margin = New Padding(0, 0, 0, 8)}

            Dim lTop As New Label With {
                .Text = "Pedido #" & p.ID & "  " & If(p.Mesa <> "", p.Mesa, "Llevar"),
                .Font = New Font("Segoe UI", 10, FontStyle.Bold), .ForeColor = Tema.Acento,
                .Location = New Point(10, 8), .Size = New Size(248, 18), .BackColor = Color.Transparent
            }
            Dim lCli As New Label With {
                .Text = p.NombreCliente & If(p.TelefonoCliente <> "", " | " & p.TelefonoCliente, ""),
                .Font = Tema.FntSmall, .ForeColor = Tema.TextoSec,
                .Location = New Point(10, 28), .Size = New Size(248, 16), .BackColor = Color.Transparent
            }
            Dim lMins As New Label With {
                .Text = mins & " min",
                .Font = Tema.FntSmallB, .ForeColor = If(mins > 20, Tema.Rojo, Tema.TextoTer),
                .Location = New Point(190, 8), .Size = New Size(68, 16), .BackColor = Color.Transparent,
                .TextAlign = ContentAlignment.MiddleRight
            }

            Dim sbItems As New StringBuilder()
            For Each it In p.Items
                sbItems.AppendLine(it.Cantidad & "x " & it.Producto.Nombre)
                sbItems.AppendLine("   " & it.Especificaciones)
            Next
            Dim lItems As New Label With {
                .Text = sbItems.ToString().TrimEnd(), .Font = Tema.FntSmall, .ForeColor = Tema.TextoSec,
                .Location = New Point(10, 48), .AutoSize = False, .Width = 248,
                .MaximumSize = New Size(248, 200), .BackColor = Color.Transparent
            }
            lItems.Height = lItems.PreferredHeight

            Dim cardH As Integer = 48 + lItems.Height + 12
            Dim lTime As New Label With {
                .Text = p.FechaHora.ToString("HH:mm") & "  $" & p.Total.ToString("N0"),
                .Font = Tema.FntSmall, .ForeColor = Tema.TextoTer,
                .Location = New Point(10, 48 + lItems.Height + 2), .Size = New Size(248, 16), .BackColor = Color.Transparent
            }

            card.Controls.AddRange(New Control() {lTop, lCli, lMins, lItems, lTime})

            If p.Estado <> EstadoPedido.Entregado Then
                Dim btnAv = Tema.CrearBoton(
                    If(p.Estado = EstadoPedido.EnPreparacion, "Marcar listo", "Marcar entregado"),
                    Tema.Acento, Color.White, 248, 32)
                btnAv.Location = New Point(10, cardH)
                btnAv.Tag = pedido
                cardH += 40

                Dim btnCancel = Tema.CrearBoton("Cancelar pedido", Tema.Rojo, Color.White, 248, 28)
                btnCancel.Location = New Point(10, cardH)
                btnCancel.Tag = pedido
                cardH += 36

                AddHandler btnAv.Click, AddressOf KanbanAvanzar
                AddHandler btnCancel.Click, AddressOf KanbanCancelar
                card.Controls.AddRange(New Control() {btnAv, btnCancel})
            End If

            card.Height = cardH + 10
            target.Controls.Add(card)
        Next

        For Each flow In New FlowLayoutPanel() {kanPrep, kanListo, kanEntregado}
            If flow.Controls.Count = 0 Then
                flow.Controls.Add(New Label With {
                    .Text = "Sin pedidos", .Font = Tema.FntReg, .ForeColor = Tema.TextoTer,
                    .AutoSize = False, .Size = New Size(248, 40),
                    .TextAlign = ContentAlignment.MiddleCenter, .BackColor = Color.Transparent
                })
            End If
        Next
    End Sub

    Private Sub KanbanAvanzar(s As Object, e As EventArgs)
        Dim pedido = CType(CType(s, Button).Tag, Pedido)
        If pedido.Estado = EstadoPedido.EnPreparacion Then
            pedido.Estado = EstadoPedido.Listo
            SystemSounds.Asterisk.Play()
        ElseIf pedido.Estado = EstadoPedido.Listo Then
            pedido.Estado = EstadoPedido.Entregado
        End If
        KanbanRefrescar()
    End Sub

    Private Sub KanbanCancelar(s As Object, e As EventArgs)
        Dim pedido = CType(CType(s, Button).Tag, Pedido)
        Dim motivo As String = InputBox("Motivo de cancelacion:", "Cancelar pedido #" & pedido.ID)
        If String.IsNullOrWhiteSpace(motivo) Then Return
        pedido.Estado = EstadoPedido.Cancelado
        pedido.MotivoCancelacion = motivo
        ' Devolver stock
        For Each item In pedido.Items
            item.Producto.Stock += item.Cantidad
        Next
        KanbanRefrescar()
        MessageBox.Show("Pedido cancelado. Stock restaurado.", "Cancelado", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    ' ══ BUILD STOCK ══
    Private Function BuildStock() As Panel
        Dim pnl As New Panel With {.BackColor = Tema.BgPrincipal}
        pnl.Controls.Add(MkHeader("Control de Stock"))

        Dim toolbar As New Panel With {.Location = New Point(12, 66), .Size = New Size(960, 46), .BackColor = Color.Transparent}
        stockLblSel = New Label With {
            .Text = "Seleccionados: 0", .Font = Tema.FntReg, .ForeColor = Tema.TextoSec,
            .Location = New Point(0, 12), .AutoSize = True, .BackColor = Color.Transparent
        }
        Dim txtQty As New TextBox With {
            .Location = New Point(160, 8), .Width = 70, .Height = 30,
            .Font = Tema.FntInput, .Text = "1",
            .BackColor = Tema.Surface, .BorderStyle = BorderStyle.FixedSingle
        }
        Dim btnAdd = Tema.CrearBoton("+ Agregar stock", Tema.Verde, Color.White, 140, 32)
        btnAdd.Location = New Point(240, 7) : btnAdd.Tag = txtQty
        Dim btnSub = Tema.CrearBoton("- Descontar", Tema.Naranja, Color.White, 120, 32)
        btnSub.Location = New Point(388, 7) : btnSub.Tag = txtQty
        Dim btnClear = Tema.CrearBoton("Deseleccionar", Tema.Surface3, Tema.TextoSec, 120, 32)
        btnClear.Location = New Point(516, 7)
        Dim btnAlerta = Tema.CrearBoton("Ver criticos", Tema.Rojo, Color.White, 120, 32)
        btnAlerta.Location = New Point(644, 7)

        toolbar.Controls.AddRange(New Control() {stockLblSel, txtQty, btnAdd, btnSub, btnClear, btnAlerta})

        stockGrid = New FlowLayoutPanel With {
            .Location = New Point(12, 118), .Size = New Size(960, 510),
            .FlowDirection = FlowDirection.LeftToRight, .WrapContents = True,
            .AutoScroll = True, .BackColor = Color.Transparent,
            .Padding = New Padding(0, 4, 0, 4)
        }

        pnl.Controls.AddRange(New Control() {toolbar, stockGrid})

        AddHandler btnAdd.Click, AddressOf StockAgregar
        AddHandler btnSub.Click, AddressOf StockDescontar
        AddHandler btnClear.Click, AddressOf StockDeseleccionar
        AddHandler btnAlerta.Click, Sub(s, e)
                                        Dim criticos = Datos.ListaProductos.Where(Function(x) x.Stock <= 10).ToList()
                                        If criticos.Count = 0 Then
                                            MessageBox.Show("No hay productos en stock critico.", "Stock", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                        Else
                                            Dim msg As New StringBuilder("Productos con stock critico:" & Environment.NewLine)
                                            For Each p In criticos
                                                msg.AppendLine("  " & p.Nombre & ": " & p.Stock & " u.")
                                            Next
                                            MessageBox.Show(msg.ToString(), "Stock Critico", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                        End If
                                    End Sub
        Return pnl
    End Function

    Private Sub StockAgregar(s As Object, e As EventArgs)
        Dim txtQty = CType(CType(s, Button).Tag, TextBox)
        Dim qty As Integer = 1
        Integer.TryParse(txtQty.Text, qty)
        If qty < 1 Then qty = 1
        For Each id In stockSeleccionados
            For Each p In Datos.ListaProductos
                If p.ID = id Then p.Stock += qty
            Next
        Next
        StockRefrescar() : POSRenderProductos("")
    End Sub

    Private Sub StockDescontar(s As Object, e As EventArgs)
        Dim txtQty = CType(CType(s, Button).Tag, TextBox)
        Dim qty As Integer = 1
        Integer.TryParse(txtQty.Text, qty)
        If qty < 1 Then qty = 1
        For Each id In stockSeleccionados
            For Each p In Datos.ListaProductos
                If p.ID = id Then p.Stock = Math.Max(0, p.Stock - qty)
            Next
        Next
        StockRefrescar() : POSRenderProductos("")
    End Sub

    Private Sub StockDeseleccionar(s As Object, e As EventArgs)
        stockSeleccionados.Clear()
        stockLblSel.Text = "Seleccionados: 0"
        StockRefrescar()
    End Sub

    Private Sub StockRefrescar()
        stockGrid.Controls.Clear()
        For Each prod In Datos.ListaProductos
            Dim p As Producto = prod
            Dim isSel As Boolean = stockSeleccionados.Contains(p.ID)
            Dim pct As Integer = Math.Min(100, CInt(p.Stock / 70.0 * 100))
            Dim statusColor As Color = If(p.Stock <= 5, Tema.Rojo, If(p.Stock <= 10, Tema.Naranja, Tema.Verde))
            Dim statusTxt As String = If(p.Stock = 0, "Agotado", If(p.Stock <= 5, "Critico", If(p.Stock <= 10, "Bajo", "Normal")))

            Dim card As New Panel With {
                .Size = New Size(145, 140), .Margin = New Padding(0, 0, 8, 8),
                .BackColor = If(isSel, Color.FromArgb(253, 245, 234), Tema.Surface),
                .Cursor = Cursors.Hand, .Tag = p
            }

            Dim lNm As New Label With {.Text = p.Nombre, .Font = Tema.FntSmallB, .ForeColor = Tema.TextoPrinc, .Location = New Point(4, 8), .Size = New Size(137, 36), .TextAlign = ContentAlignment.TopCenter, .BackColor = Color.Transparent}
            Dim lQty As New Label With {.Text = p.Stock.ToString(), .Font = New Font("Segoe UI", 18, FontStyle.Bold), .ForeColor = statusColor, .Location = New Point(0, 50), .Size = New Size(145, 28), .TextAlign = ContentAlignment.MiddleCenter, .BackColor = Color.Transparent}
            Dim barOuter As New Panel With {.Location = New Point(10, 82), .Size = New Size(125, 5), .BackColor = Tema.Surface3}
            Dim barInner As New Panel With {.Location = New Point(0, 0), .Size = New Size(CInt(125.0 * pct / 100), 5), .BackColor = statusColor}
            barOuter.Controls.Add(barInner)
            Dim lSt As New Label With {.Text = statusTxt, .Font = Tema.FntSmallB, .ForeColor = statusColor, .Location = New Point(0, 92), .Size = New Size(145, 16), .TextAlign = ContentAlignment.MiddleCenter, .BackColor = Color.Transparent}

            card.Controls.AddRange(New Control() {lNm, lQty, barOuter, lSt})
            AddHandler card.Click, AddressOf StockCardClick
            For Each ctrl As Control In card.Controls
                ctrl.Tag = p
                AddHandler ctrl.Click, AddressOf StockCardClick
            Next
            stockGrid.Controls.Add(card)
        Next
        If stockLblSel IsNot Nothing Then stockLblSel.Text = "Seleccionados: " & stockSeleccionados.Count
    End Sub

    Private Sub StockCardClick(s As Object, e As EventArgs)
        Dim ctrl = CType(s, Control)
        Dim p As Producto = Nothing
        If TypeOf ctrl.Tag Is Producto Then
            p = CType(ctrl.Tag, Producto)
        ElseIf ctrl.Parent IsNot Nothing AndAlso TypeOf ctrl.Parent.Tag Is Producto Then
            p = CType(ctrl.Parent.Tag, Producto)
        End If
        If p Is Nothing Then Return
        If stockSeleccionados.Contains(p.ID) Then
            stockSeleccionados.Remove(p.ID)
        Else
            stockSeleccionados.Add(p.ID)
        End If
        StockRefrescar()
    End Sub

    ' ══ BUILD PROVEEDORES ══
    Private Function TienePermisoProveedores(accion As String) As Boolean
        If Datos.UsuarioActual Is Nothing Then Return False
        Select Case Datos.UsuarioActual.Rol
            Case RolUsuario.Administrador
                Return True
            Case RolUsuario.Cajero
                Return accion <> "Eliminar"
            Case RolUsuario.Cocina
                Return accion = "Ver"
        End Select
        Return False
    End Function

    Private Function BuildProveedores() As Panel
        Dim pnl As New Panel With {.BackColor = Tema.BgPrincipal}
        pnl.Controls.Add(MkHeader("Proveedores"))

        proveedorSearchBox = New TextBox With {
            .Location = New Point(12, 68), .Width = 360, .Height = 30,
            .Font = Tema.FntInput, .BackColor = Tema.Surface,
            .ForeColor = Tema.TextoPrinc, .BorderStyle = BorderStyle.FixedSingle
        }
        Dim lblBuscar = Tema.CrearLabel("Buscar por codigo, nombre, CUIT, telefono o localidad", False, 8)
        lblBuscar.Location = New Point(12, 98)
        proveedorLblCount = Tema.CrearLabel("0 proveedores", True, 9)
        proveedorLblCount.Location = New Point(820, 74)

        Dim btnNuevo = Tema.CrearBoton("Nuevo proveedor", Tema.Verde, Color.White, 140, 32)
        Dim btnEditar = Tema.CrearBoton("Editar", Tema.Azul, Color.White, 90, 32)
        Dim btnEliminar = Tema.CrearBoton("Eliminar", Tema.Rojo, Color.White, 90, 32)
        Dim btnDetalle = Tema.CrearBoton("Ver detalle", Tema.Acento2, Color.White, 105, 32)
        Dim btnExport = Tema.CrearBoton("Exportar Excel", Tema.Acento, Color.White, 125, 32)
        Dim btnImport = Tema.CrearBoton("Importar Excel", Color.FromArgb(110, 86, 50), Color.White, 125, 32)
        btnNuevo.Location = New Point(390, 66)
        btnEditar.Location = New Point(535, 66)
        btnEliminar.Location = New Point(630, 66)
        btnDetalle.Location = New Point(725, 66)
        btnExport.Location = New Point(12, 628)
        btnImport.Location = New Point(145, 628)

        proveedoresDgv = New DataGridView With {
            .Location = New Point(12, 128), .Size = New Size(980, 485),
            .ReadOnly = True, .AllowUserToAddRows = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .BorderStyle = BorderStyle.None, .BackgroundColor = Tema.Surface,
            .RowHeadersVisible = False, .GridColor = Tema.Borde, .Font = Tema.FntReg
        }
        EstilarDgv(proveedoresDgv)
        proveedoresDgv.Columns.AddRange(New DataGridViewColumn() {
            New DataGridViewTextBoxColumn With {.Name = "Codigo", .HeaderText = "Codigo"},
            New DataGridViewTextBoxColumn With {.Name = "Nombre", .HeaderText = "Nombre"},
            New DataGridViewTextBoxColumn With {.Name = "CUIT", .HeaderText = "CUIT"},
            New DataGridViewTextBoxColumn With {.Name = "Telefono", .HeaderText = "Telefono"},
            New DataGridViewTextBoxColumn With {.Name = "Provincia", .HeaderText = "Provincia"},
            New DataGridViewTextBoxColumn With {.Name = "Localidad", .HeaderText = "Localidad"},
            New DataGridViewTextBoxColumn With {.Name = "Categoria", .HeaderText = "Categoria"},
            New DataGridViewTextBoxColumn With {.Name = "Estado", .HeaderText = "Estado"},
            New DataGridViewTextBoxColumn With {.Name = "Saldo", .HeaderText = "Saldo"}
        })
        For Each col As DataGridViewColumn In proveedoresDgv.Columns
            col.SortMode = DataGridViewColumnSortMode.Automatic
        Next

        Dim btnPrev = Tema.CrearBoton("<", Tema.Surface3, Tema.TextoPrinc, 42, 30)
        Dim btnNext = Tema.CrearBoton(">", Tema.Surface3, Tema.TextoPrinc, 42, 30)
        proveedorLblPage = Tema.CrearLabel("Pagina 1", True, 9)
        btnPrev.Location = New Point(810, 628)
        proveedorLblPage.Location = New Point(858, 635)
        btnNext.Location = New Point(940, 628)

        AddHandler proveedorSearchBox.TextChanged, Sub(s, e)
                                                       proveedorPage = 1
                                                       ProveedoresRefrescar()
                                                   End Sub
        AddHandler btnNuevo.Click, AddressOf ProveedorNuevo
        AddHandler btnEditar.Click, AddressOf ProveedorEditar
        AddHandler btnDetalle.Click, AddressOf ProveedorDetalle
        AddHandler btnEliminar.Click, AddressOf ProveedorEliminar
        AddHandler btnExport.Click, AddressOf ProveedorExportar
        AddHandler btnImport.Click, AddressOf ProveedorImportar
        AddHandler btnPrev.Click, Sub(s, e)
                                      If proveedorPage > 1 Then proveedorPage -= 1
                                      ProveedoresRefrescar()
                                  End Sub
        AddHandler btnNext.Click, Sub(s, e)
                                      proveedorPage += 1
                                      ProveedoresRefrescar()
                                  End Sub
        AddHandler proveedoresDgv.CellDoubleClick, AddressOf ProveedorEditar

        pnl.Controls.AddRange(New Control() {proveedorSearchBox, lblBuscar, proveedorLblCount, btnNuevo, btnEditar, btnEliminar, btnDetalle, proveedoresDgv, btnExport, btnImport, btnPrev, proveedorLblPage, btnNext})
        Return pnl
    End Function

    Private Function ProveedoresFiltrados() As List(Of Proveedor)
        Dim filtro As String = If(proveedorSearchBox Is Nothing, "", proveedorSearchBox.Text.Trim().ToLower())
        Dim q = Datos.ListaProveedores.AsEnumerable()
        If filtro <> "" Then
            q = q.Where(Function(p) If(p.Codigo, "").ToLower().Contains(filtro) OrElse
                                     If(p.Nombre, "").ToLower().Contains(filtro) OrElse
                                     If(p.CUIT, "").ToLower().Contains(filtro) OrElse
                                     (If(p.TelefonoLaboral, "") & " " & If(p.Celular, "")).ToLower().Contains(filtro) OrElse
                                     If(p.Localidad, "").ToLower().Contains(filtro))
        End If
        Return q.OrderBy(Function(p) p.Nombre).ToList()
    End Function

    Private Sub ProveedoresRefrescar()
        If proveedoresDgv Is Nothing Then Return
        Dim lista = ProveedoresFiltrados()
        Dim totalPaginas As Integer = Math.Max(1, CInt(Math.Ceiling(lista.Count / CDbl(proveedorPageSize))))
        If proveedorPage > totalPaginas Then proveedorPage = totalPaginas
        proveedoresDgv.Rows.Clear()
        For Each p In lista.Skip((proveedorPage - 1) * proveedorPageSize).Take(proveedorPageSize)
            Dim deudaVencida = p.Movimientos.Any(Function(m) Not m.Pagado AndAlso m.Vencimiento.HasValue AndAlso m.Vencimiento.Value.Date < DateTime.Today)
            Dim estado = If(p.Activo, "Activo", "Inactivo") & If(deudaVencida, " - Deuda vencida", "")
            proveedoresDgv.Rows.Add(p.Codigo, p.Nombre, p.CUIT, If(String.IsNullOrWhiteSpace(p.TelefonoLaboral), p.Celular, p.TelefonoLaboral), p.Provincia, p.Localidad, p.Categoria, estado, "$" & p.SaldoPendiente.ToString("N0"))
        Next
        proveedorLblCount.Text = lista.Count & " proveedores"
        proveedorLblPage.Text = "Pagina " & proveedorPage & "/" & totalPaginas
    End Sub

    Private Function ProveedorSeleccionado() As Proveedor
        If proveedoresDgv Is Nothing OrElse proveedoresDgv.SelectedRows.Count = 0 Then Return Nothing
        Dim codigo = proveedoresDgv.SelectedRows(0).Cells("Codigo").Value.ToString()
        Return Datos.ListaProveedores.FirstOrDefault(Function(p) p.Codigo = codigo)
    End Function

    Private Sub ProveedorNuevo(s As Object, e As EventArgs)
        If Not TienePermisoProveedores("Crear") Then MessageBox.Show("No tenes permiso para crear proveedores.") : Return
        Dim p As New Proveedor With {.Codigo = "PRV" & (Datos.ListaProveedores.Count + 1).ToString("000"), .FechaAlta = DateTime.Today, .Activo = True}
        Using frm As New ProveedorForm(p, True)
            If frm.ShowDialog(Me) = DialogResult.OK Then
                p.CreadoPor = If(Datos.UsuarioActual Is Nothing, "Sistema", Datos.UsuarioActual.Nombre)
                p.ModificadoPor = p.CreadoPor
                p.FechaCreacion = DateTime.Now
                p.FechaModificacion = DateTime.Now
                p.HistorialCambios.Add("Creado por " & p.CreadoPor & " el " & DateTime.Now.ToString("dd/MM/yyyy HH:mm"))
                Datos.ListaProveedores.Add(p)
                Datos.BitacoraProveedores.Add("Nuevo proveedor " & p.Codigo & " - " & p.Nombre)
                ProveedoresRefrescar()
            End If
        End Using
    End Sub

    Private Sub ProveedorEditar(s As Object, e As EventArgs)
        If Not TienePermisoProveedores("Modificar") Then MessageBox.Show("No tenes permiso para modificar proveedores.") : Return
        Dim p = ProveedorSeleccionado()
        If p Is Nothing Then Return
        Using frm As New ProveedorForm(p, False)
            If frm.ShowDialog(Me) = DialogResult.OK Then
                p.ModificadoPor = If(Datos.UsuarioActual Is Nothing, "Sistema", Datos.UsuarioActual.Nombre)
                p.FechaModificacion = DateTime.Now
                p.HistorialCambios.Add("Modificado por " & p.ModificadoPor & " el " & DateTime.Now.ToString("dd/MM/yyyy HH:mm"))
                Datos.BitacoraProveedores.Add("Editado proveedor " & p.Codigo & " - " & p.Nombre)
                ProveedoresRefrescar()
            End If
        End Using
    End Sub

    Private Sub ProveedorDetalle(s As Object, e As EventArgs)
        Dim p = ProveedorSeleccionado()
        If p Is Nothing Then Return
        Dim info As String = p.Codigo & " - " & p.Nombre & vbCrLf &
                             "CUIT: " & p.CUIT & vbCrLf &
                             "Saldo: $" & p.SaldoPendiente.ToString("N0") & vbCrLf &
                             "Compras: $" & p.TotalCompras.ToString("N0") & vbCrLf &
                             "Ultima compra: " & If(p.UltimaCompra.HasValue, p.UltimaCompra.Value.ToString("dd/MM/yyyy"), "Sin datos")
        MessageBox.Show(info, "Detalle de proveedor", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub ProveedorEliminar(s As Object, e As EventArgs)
        If Not TienePermisoProveedores("Eliminar") Then MessageBox.Show("No tenes permiso para eliminar proveedores.") : Return
        Dim p = ProveedorSeleccionado()
        If p Is Nothing Then Return
        If p.Movimientos.Count > 0 OrElse p.TotalCompras > 0 Then
            MessageBox.Show("No se puede eliminar este proveedor porque tiene compras o movimientos asociados. Podes marcarlo como Inactivo desde Editar.", "Eliminacion bloqueada", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        If MessageBox.Show("Eliminar proveedor " & p.Nombre & "?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
            Datos.ListaProveedores.Remove(p)
            Datos.BitacoraProveedores.Add("Eliminado proveedor " & p.Codigo & " - " & p.Nombre)
            ProveedoresRefrescar()
        End If
    End Sub

    Private Sub ProveedorExportar(s As Object, e As EventArgs)
        Dim sfd As New SaveFileDialog With {.Filter = "Excel CSV|*.csv", .FileName = "proveedores.csv"}
        If sfd.ShowDialog(Me) <> DialogResult.OK Then Return
        Dim sb As New StringBuilder()
        sb.AppendLine("Codigo;Nombre;CUIT;Telefono;Provincia;Localidad;Categoria;Estado;Saldo")
        For Each p In Datos.ListaProveedores
            sb.AppendLine(String.Join(";", New String() {p.Codigo, p.Nombre, p.CUIT, p.TelefonoLaboral, p.Provincia, p.Localidad, p.Categoria, If(p.Activo, "Activo", "Inactivo"), p.SaldoPendiente.ToString("0.00")}))
        Next
        File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8)
        MessageBox.Show("Listado exportado.")
    End Sub

    Private Sub ProveedorImportar(s As Object, e As EventArgs)
        If Not TienePermisoProveedores("Crear") Then MessageBox.Show("No tenes permiso para importar proveedores.") : Return
        Dim ofd As New OpenFileDialog With {.Filter = "Excel CSV|*.csv"}
        If ofd.ShowDialog(Me) <> DialogResult.OK Then Return
        Dim agregados As Integer = 0
        For Each ln In File.ReadAllLines(ofd.FileName, Encoding.UTF8).Skip(1)
            Dim c = ln.Split(";"c)
            If c.Length < 2 Then Continue For
            Dim codigo = c(0).Trim()
            If codigo = "" OrElse Datos.ListaProveedores.Any(Function(p) p.Codigo.Equals(codigo, StringComparison.OrdinalIgnoreCase)) Then Continue For
            Datos.ListaProveedores.Add(New Proveedor With {.Codigo = codigo, .Nombre = c(1).Trim(), .CUIT = If(c.Length > 2, c(2).Trim(), ""), .TelefonoLaboral = If(c.Length > 3, c(3).Trim(), ""), .Provincia = If(c.Length > 4, c(4).Trim(), ""), .Localidad = If(c.Length > 5, c(5).Trim(), ""), .Categoria = If(c.Length > 6, c(6).Trim(), ""), .CreadoPor = If(Datos.UsuarioActual Is Nothing, "Sistema", Datos.UsuarioActual.Nombre)})
            agregados += 1
        Next
        Datos.BitacoraProveedores.Add("Importados " & agregados & " proveedores desde CSV")
        ProveedoresRefrescar()
        MessageBox.Show("Proveedores importados: " & agregados)
    End Sub

    ' ══ BUILD CLIENTES ══
    Private Function BuildClientes() As Panel
        Dim pnl As New Panel With {.BackColor = Tema.BgPrincipal}
        pnl.Controls.Add(MkHeader("Clientes Registrados"))

        Dim searchBox As New TextBox With {
            .Location = New Point(12, 68), .Width = 300, .Height = 30,
            .Font = Tema.FntInput, .BackColor = Tema.Surface,
            .ForeColor = Tema.TextoPrinc, .BorderStyle = BorderStyle.FixedSingle
        }

        clientesDgv = New DataGridView With {
            .Location = New Point(12, 108), .Size = New Size(760, 500),
            .ReadOnly = True, .AllowUserToAddRows = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .BorderStyle = BorderStyle.None, .BackgroundColor = Tema.Surface,
            .RowHeadersVisible = False, .GridColor = Tema.Borde, .Font = Tema.FntReg
        }
        EstilarDgv(clientesDgv)
        clientesDgv.Columns.AddRange(New DataGridViewColumn() {
            New DataGridViewTextBoxColumn With {.Name = "Nombre", .HeaderText = "Cliente"},
            New DataGridViewTextBoxColumn With {.Name = "Telefono", .HeaderText = "Telefono"},
            New DataGridViewTextBoxColumn With {.Name = "Pedidos", .HeaderText = "Pedidos"},
            New DataGridViewTextBoxColumn With {.Name = "Total", .HeaderText = "Total gastado"},
            New DataGridViewTextBoxColumn With {.Name = "Ultimo", .HeaderText = "Ultimo pedido"},
            New DataGridViewTextBoxColumn With {.Name = "Pago", .HeaderText = "Metodo habitual"}
        })

        ' Panel historial al costado
        Dim pnlHistorial As New Panel With {
            .Location = New Point(780, 108), .Size = New Size(200, 500),
            .BackColor = Tema.Surface
        }
        Dim lHist As New Label With {.Text = "Historial", .Font = Tema.FntBold, .ForeColor = Tema.Acento, .Location = New Point(10, 10), .AutoSize = True, .BackColor = Color.Transparent}
        Dim lstHist As New ListBox With {
            .Location = New Point(10, 34), .Size = New Size(180, 460),
            .Font = Tema.FntSmall, .BackColor = Tema.Surface2,
            .BorderStyle = BorderStyle.None
        }
        pnlHistorial.Controls.AddRange(New Control() {lHist, lstHist})

        AddHandler clientesDgv.SelectionChanged, Sub(s, e)
                                                     lstHist.Items.Clear()
                                                     If clientesDgv.SelectedRows.Count = 0 Then Return
                                                     Dim nombre As String = clientesDgv.SelectedRows(0).Cells("Nombre").Value.ToString()
                                                     Dim cl As ClienteRegistrado = Nothing
                                                     For Each c In Datos.ListaClientes
                                                         If c.Nombre = nombre Then cl = c : Exit For
                                                     Next
                                                     If cl Is Nothing Then Return
                                                     For Each pid In cl.HistorialPedidos
                                                         Dim ped = Datos.ListaPedidos.FirstOrDefault(Function(x) x.ID = pid)
                                                         If ped IsNot Nothing Then
                                                             lstHist.Items.Add("#" & ped.ID & " $" & ped.Total.ToString("N0") & " " & ped.FechaHora.ToString("dd/MM"))
                                                         End If
                                                     Next
                                                 End Sub

        pnl.Controls.AddRange(New Control() {searchBox, clientesDgv, pnlHistorial})
        AddHandler searchBox.TextChanged, AddressOf ClientesSearchChanged
        Return pnl
    End Function

    Private Sub ClientesSearchChanged(s As Object, e As EventArgs)
        ClientesRefrescar(CType(s, TextBox).Text)
    End Sub

    Private Sub ClientesRefrescar(Optional filtro As String = "")
        clientesDgv.Rows.Clear()
        For Each c In Datos.ListaClientes
            Dim nomOk As Boolean = c.Nombre.ToLower().Contains(filtro.ToLower())
            Dim telOk As Boolean = c.Telefono.Contains(filtro)
            If String.IsNullOrEmpty(filtro) OrElse nomOk OrElse telOk Then
                clientesDgv.Rows.Add(c.Nombre, c.Telefono, c.CantidadPedidos,
                                     "$" & c.TotalGastado.ToString("N0"),
                                     c.UltimoPedido.ToString("dd/MM/yyyy"),
                                     c.MetodoPagoHabitual)
            End If
        Next
    End Sub

    ' ══ BUILD REPORTES ══
    Private Function BuildReportes() As Panel
        Dim pnl As New Panel With {.BackColor = Tema.BgPrincipal, .AutoScroll = True}
        pnl.Controls.Add(MkHeader("Reportes y Estadisticas"))
        Return pnl
    End Function

    Private Sub ReportesRefrescar()
        Dim hdr As Panel = Nothing
        For Each c As Control In pnlReportes.Controls
            If TypeOf c Is Panel AndAlso CType(c, Panel).Height = 58 Then
                hdr = CType(c, Panel) : Exit For
            End If
        Next
        pnlReportes.Controls.Clear()
        If hdr IsNot Nothing Then pnlReportes.Controls.Add(hdr)

        ' Boton exportar reporte
        Dim btnExportar = Tema.CrearBoton("Exportar reporte TXT", Tema.Azul, Color.White, 200, 36)
        btnExportar.Location = New Point(770, 68)
        AddHandler btnExportar.Click, Sub(s, e)
                                          Dim dlg As New SaveFileDialog With {
                                              .Filter = "Texto (*.txt)|*.txt",
                                              .FileName = "Reporte_" & DateTime.Now.ToString("yyyyMMdd_HHmm") & ".txt"
                                          }
                                          If dlg.ShowDialog() = DialogResult.OK Then
                                              File.WriteAllText(dlg.FileName, Datos.ExportarReporteTxt(), System.Text.Encoding.UTF8)
                                              MessageBox.Show("Reporte guardado en: " & dlg.FileName, "Listo", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                          End If
                                      End Sub
        pnlReportes.Controls.Add(btnExportar)

        Dim ventasHoy As Decimal = Datos.VentasHoy()
        Dim ticket As Decimal = Datos.TicketPromedio()
        Dim pagoLider As String = Datos.PagoLider()
        Dim criticos As Integer = Datos.ListaProductos.Where(Function(p) p.Stock <= 10).Count()
        Dim cancelados As Integer = Datos.ListaPedidos.Where(Function(p) p.Estado = EstadoPedido.Cancelado).Count()
        Dim gananciaHoy As Decimal = Datos.GananciaEstimadaHoy()
        Dim perdidasHoy As Decimal = Datos.PerdidasEstimadasHoy()
        Dim horarioPico As String = Datos.HorarioPicoPedidos()

        ' Stat cards
        Dim statTitles() As String = {"Ventas del dia", "Ganancia", "Perdidas", "Stock critico", "Horario pico"}
        Dim statVals() As String = {"$" & ventasHoy.ToString("N0"), "$" & gananciaHoy.ToString("N0"), "$" & perdidasHoy.ToString("N0"), criticos.ToString(), horarioPico}
        Dim statSubs() As String = {Datos.ListaPedidos.Where(Function(p) p.Estado <> EstadoPedido.Cancelado).Count() & " pedidos", "Estimada sin BDD", "Egresos de caja", "Productos <= 10 u.", "Pedidos por hora"}
        Dim statColors() As Color = {Tema.Acento, Tema.Verde, Tema.Rojo, Tema.Naranja, Tema.Azul}

        For i As Integer = 0 To 4
            Dim card = Tema.MkCard(12 + i * 190, 68, 178, 90)
            Dim lLab = Tema.CrearLabel(statTitles(i), False, 8)
            lLab.Location = New Point(12, 10)
            Dim lVal As New Label With {
                .Text = statVals(i), .Font = New Font("Segoe UI", 18, FontStyle.Bold),
                .ForeColor = statColors(i), .Location = New Point(12, 26),
                .AutoSize = True, .BackColor = Color.Transparent
            }
            Dim lSub = Tema.CrearLabel(statSubs(i), False, 8)
            lSub.Location = New Point(12, 62)
            card.Controls.AddRange(New Control() {lLab, lVal, lSub})
            pnlReportes.Controls.Add(card)
        Next

        ' Top productos
        Dim prodCounts As New Dictionary(Of String, Integer)()
        For Each p In Datos.ListaPedidos
            If p.Estado = EstadoPedido.Cancelado Then Continue For
            For Each item In p.Items
                If prodCounts.ContainsKey(item.Producto.Nombre) Then
                    prodCounts(item.Producto.Nombre) += item.Cantidad
                Else
                    prodCounts(item.Producto.Nombre) = item.Cantidad
                End If
            Next
        Next

        Dim chartCard = Tema.MkCard(12, 172, 470, 280)
        Dim lChT As New Label With {.Text = "Productos mas vendidos", .Font = New Font("Segoe UI", 11, FontStyle.Bold), .ForeColor = Tema.Acento, .Location = New Point(14, 12), .AutoSize = True, .BackColor = Color.Transparent}
        chartCard.Controls.Add(lChT)
        Dim sortedProds = prodCounts.OrderByDescending(Function(x) x.Value).Take(6).ToList()
        Dim maxVal As Integer = If(sortedProds.Count > 0, sortedProds(0).Value, 1)
        Dim cy As Integer = 44
        If sortedProds.Count = 0 Then
            chartCard.Controls.Add(New Label With {.Text = "Sin ventas aun.", .Font = Tema.FntReg, .ForeColor = Tema.TextoTer, .Location = New Point(14, 44), .AutoSize = True, .BackColor = Color.Transparent})
        Else
            For Each kv In sortedProds
                Dim lN As New Label With {.Text = kv.Key, .Font = Tema.FntSmall, .ForeColor = Tema.TextoSec, .Location = New Point(14, cy + 2), .Size = New Size(130, 16), .BackColor = Color.Transparent}
                Dim barBg As New Panel With {.Location = New Point(150, cy), .Size = New Size(260, 20), .BackColor = Tema.Surface3}
                Dim barFg As New Panel With {.Location = New Point(0, 0), .Size = New Size(CInt(260.0 * kv.Value / maxVal), 20), .BackColor = Tema.Acento2}
                Dim lV As New Label With {.Text = kv.Value & " u.", .Font = Tema.FntSmallB, .ForeColor = Tema.Acento, .Location = New Point(418, cy + 2), .Size = New Size(40, 16), .BackColor = Color.Transparent}
                barBg.Controls.Add(barFg)
                chartCard.Controls.AddRange(New Control() {lN, barBg, lV})
                cy += 36
            Next
        End If
        pnlReportes.Controls.Add(chartCard)

        ' Por metodo de pago
        Dim payData As New Dictionary(Of String, Decimal)()
        For Each p In Datos.ListaPedidos
            If p.Estado = EstadoPedido.Cancelado Then Continue For
            If payData.ContainsKey(p.MetodoPago) Then
                payData(p.MetodoPago) += p.Total
            Else
                payData(p.MetodoPago) = p.Total
            End If
        Next

        Dim payCard = Tema.MkCard(494, 172, 470, 280)
        Dim lPayT As New Label With {.Text = "Ventas por metodo de pago", .Font = New Font("Segoe UI", 11, FontStyle.Bold), .ForeColor = Tema.Acento, .Location = New Point(14, 12), .AutoSize = True, .BackColor = Color.Transparent}
        payCard.Controls.Add(lPayT)

        Dim maxPay As Decimal = 1D
        For Each v In payData.Values
            If v > maxPay Then maxPay = v
        Next

        cy = 44
        If payData.Count = 0 Then
            payCard.Controls.Add(New Label With {.Text = "Sin ventas aun.", .Font = Tema.FntReg, .ForeColor = Tema.TextoTer, .Location = New Point(14, 44), .AutoSize = True, .BackColor = Color.Transparent})
        Else
            For Each kv In payData.OrderByDescending(Function(x) x.Value)
                Dim lN As New Label With {.Text = kv.Key, .Font = Tema.FntSmall, .ForeColor = Tema.TextoSec, .Location = New Point(14, cy + 2), .Size = New Size(130, 16), .BackColor = Color.Transparent}
                Dim barBg As New Panel With {.Location = New Point(150, cy), .Size = New Size(260, 20), .BackColor = Tema.Surface3}
                Dim barFg As New Panel With {.Location = New Point(0, 0), .Size = New Size(CInt(260.0 * kv.Value / maxPay), 20), .BackColor = Tema.Verde}
                Dim lV As New Label With {.Text = "$" & kv.Value.ToString("N0"), .Font = Tema.FntSmallB, .ForeColor = Tema.Verde, .Location = New Point(418, cy + 2), .Size = New Size(50, 16), .BackColor = Color.Transparent}
                barBg.Controls.Add(barFg)
                payCard.Controls.AddRange(New Control() {lN, barBg, lV})
                cy += 36
            Next
        End If
        pnlReportes.Controls.Add(payCard)

        ' Resumen caja
        Dim cajaCard = Tema.MkCard(12, 464, 470, 130)
        Dim lCajaT As New Label With {.Text = "Estado de Caja", .Font = New Font("Segoe UI", 11, FontStyle.Bold), .ForeColor = Tema.Acento, .Location = New Point(14, 12), .AutoSize = True, .BackColor = Color.Transparent}
        Dim lCajaEst As New Label With {.Text = "Estado: " & If(Datos.Caja.Abierta, "ABIERTA", "CERRADA"), .Font = Tema.FntBold, .ForeColor = If(Datos.Caja.Abierta, Tema.Verde, Tema.Rojo), .Location = New Point(14, 38), .AutoSize = True, .BackColor = Color.Transparent}
        Dim lCajaSaldo As New Label With {.Text = "Saldo actual: $" & Datos.Caja.SaldoActual.ToString("N0"), .Font = Tema.FntBold, .ForeColor = Tema.Acento, .Location = New Point(14, 60), .AutoSize = True, .BackColor = Color.Transparent}
        Dim lCajaVentas As New Label With {.Text = "Ventas caja: $" & Datos.Caja.TotalVentas.ToString("N0"), .Font = Tema.FntReg, .ForeColor = Tema.Verde, .Location = New Point(14, 82), .AutoSize = True, .BackColor = Color.Transparent}
        Dim lCajaEgr As New Label With {.Text = "Egresos: $" & Datos.Caja.TotalEgresos.ToString("N0"), .Font = Tema.FntReg, .ForeColor = Tema.Naranja, .Location = New Point(14, 100), .AutoSize = True, .BackColor = Color.Transparent}
        cajaCard.Controls.AddRange(New Control() {lCajaT, lCajaEst, lCajaSaldo, lCajaVentas, lCajaEgr})
        pnlReportes.Controls.Add(cajaCard)
    End Sub
End Class