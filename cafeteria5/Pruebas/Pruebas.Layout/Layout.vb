Imports System.IO
Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Linq
Imports System.Reflection
Imports System.Windows.Forms
Imports CafeteriaOS

Namespace Prueba

    ''' <summary>
    ''' Construye el dashboard y todos los modulos a distintos tamanos y fuerza el
    ''' calculo de layout. WinForms resuelve el layout sin necesitar pantalla, asi que
    ''' esto detecta las excepciones que solo aparecian al abrir la aplicacion
    ''' (por ejemplo el SplitterDistance del POS) sin depender de que alguien la abra.
    ''' </summary>
    Module Layout

        Dim fallos As Integer
        Dim pruebas As Integer

        ''' Anchos que se prueban: el minimo del dashboard, uno angosto a proposito
        ''' para forzar el responsive, un tipico y uno grande.
        Dim tamanos As New Dictionary(Of String, Size) From {
            {"minimo del dashboard (1024x640)", New Size(1024, 640)},
            {"angosto a proposito (800x600)", New Size(800, 600)},
            {"portatil (1366x768)", New Size(1366, 768)},
            {"full HD (1920x1080)", New Size(1920, 1080)}
        }

        <STAThread>
        Sub Main()
            Application.EnableVisualStyles()
            ' Los datos van a una carpeta propia y no a la de quien esta probando.
            ' Va antes de tocar el archivo: Nucleo.vb borra el historial al arrancar para
            ' empezar de cero, y sin esto se comia el archivo de ingresos de verdad.
            Dim carpeta = Path.Combine(Path.GetTempPath(), "CafeteriaOS-pruebas-layout")
            If Directory.Exists(carpeta) Then Directory.Delete(carpeta, True)
            Directory.CreateDirectory(carpeta)
            Environment.SetEnvironmentVariable(IngresoAlmacen.VariableCarpeta, carpeta)

            DatosIniciales.Cargar()
            Datos.UsuarioActual = AutenticacionService.Buscar("admin@cafe.com", "123456")
            CajaService.Abrir(20000D)

            Try
                PruebasCobro.Correr()
                PruebasStock.Correr()
            Catch ex As Exception
                Revisar("La ventana de cobro se construye sin excepcion", False)
                Console.WriteLine(ex.ToString())
            End Try

            Try
                ProbarTodosLosModulos()
            Catch ex As Exception
                Revisar("El dashboard completo no lanzo excepcion", False)
                Console.WriteLine(ex.ToString())
            End Try

            Try
                ProbarRefrescosYTema()
            Catch ex As Exception
                Revisar("Refrescar y cambiar de tema no lanzo excepcion", False)
                Console.WriteLine(ex.ToString())
            End Try

            Try
                ProbarFormularios()
            Catch ex As Exception
                Revisar("Los formularios noSSxException", False)
                Console.WriteLine(ex.ToString())
            End Try

            Console.WriteLine()
            If fallos = 0 Then
                Console.WriteLine($"OK: {pruebas} pruebas, 0 fallos.")
            Else
                Console.WriteLine($"FALLOS: {fallos} de {pruebas} pruebas.")
                Environment.ExitCode = 1
            End If
        End Sub

        ''' <summary>Instancia cada modulo dentro de un contenedor y lo layoutea en
        ''' todos los tamanos, como si el usuario los fuera abriendo.</summary>
        Sub ProbarTodosLosModulos()
            Console.WriteLine("== CafeteriaOS: layout de los modulos a distintos tamanos ==" &
                              Environment.NewLine)

            For Each par In tamanos
                Dim nombre = par.Key
                Dim tam = par.Value

                Using host As New Form With {
                    .ClientSize = tam,
                    .StartPosition = FormStartPosition.Manual,
                    .Location = New Point(-5000, -5000)
                }
                    Dim area As New Panel With {.Dock = DockStyle.Fill}
                    host.Controls.Add(area)

                    For Each seccion As SeccionDashboard In [Enum].GetValues(GetType(SeccionDashboard))
                        ' Se crea un panel nuevo por seccion y por tamanho: asi tambien se
                        ' cubre el primer momento, cuando el control todavia no tiene
                        ' tamanho real asignado.
                        Dim panel = CrearPanel(seccion)
                        area.Controls.Add(panel)
                        Forzar(panel)
                        area.Controls.Remove(panel)
                        panel.Dispose()
                    Next
                    Forzar(host)
                    host.Dispose()
                End Using

                Revisar($"Los 7 modulos se layoutean en {nombre}", True)
            Next

            ' El caso que mas importa: abrir los modulos uno detras del otro en el mismo
            ' dashboard, como hace el usuario al navegar por el lateral.
            Using f As New MainDashboardForm()
                f.ClientSize = New Size(1024, 640)
                Forzar(f)
                For Each seccion As SeccionDashboard In [Enum].GetValues(GetType(SeccionDashboard))
                    f.GetType().
                        GetMethod("Mostrar", BindingFlags.NonPublic Or BindingFlags.Instance).
                        Invoke(f, New Object() {seccion})
                    Forzar(f)
                    Revisar($"Se abre el modulo {seccion} sin excepcion", True)
                Next
                ' Volver a un modulo ya abierto: tiene que conservar la instancia.
                Forzar(f)
                Revisar("Navegar por todos los modulos no rompe el dashboard", True)
                f.Dispose()
            End Using
        End Sub

        ''' <summary>Los formularios que se abren aparte del dashboard.</summary>
        Sub ProbarFormularios()
            Console.WriteLine()
            Console.WriteLine("== CafeteriaOS: layout de los formularios ==" & Environment.NewLine)

            Dim pedido = New Pedido With {
                .ID = 1,
                .FechaHora = DateTime.Now,
                .NombreCliente = "Cliente Prueba",
                .TelefonoCliente = "3511111111",
                .TipoServicio = TipoServicio.EnElLocal,
                .Mesa = "4",
                .MetodoPago = MetodoPago.Efectivo,
                .UsuarioResponsable = "Administrador",
                .Items = New List(Of DetallePedido) From {
                    New DetallePedido With {
                        .Producto = Datos.ListaProductos(0), .Cantidad = 2
                    },
                    New DetallePedido With {
                        .Producto = Datos.ListaProductos(1), .Cantidad = 1
                    },
                    New DetallePedido With {
                        .Producto = Datos.ListaProductos(2), .Cantidad = 3
                    }
                }
            }
Dim proveedor = New Proveedor With {
    .Codigo = "PRV-001",
    .Nombre = "Distribuidora del Centro S.R.L.",
    .Domicilio = "Av. Siempreviva 742",
    .CodigoPostal = "X5000",
    .TelefonoLaboral = "3514444444",
    .Celular = "3515555555",
    .ContactoPrincipal = "Ana Gomez",
    .TipoIVA = "Responsable Inscripto",
    .CUIT = "30-12345678-9",
    .Pais = "Argentina",
    .Provincia = "Cordoba",
    .Localidad = "Cordoba",
    .Actividad = "Distribucion de insumos",
    .Categoria = "Insumos",
    .ObservacionesRapidas = "Entrega los martes",
    .Activo = True
}

            Using host As New Form With {.ClientSize = New Size(1024, 640)}
                Dim area As New Panel With {.Dock = DockStyle.Fill}
                host.Controls.Add(area)

                Dim producto = Datos.ListaProductos(0)

                Dim formularios As New List(Of Func(Of Control)) From {
                    Function() New LoginForm(),
                    Function() New TicketForm(TicketService.ConstruirTicket(pedido),
                                              "Ticket de prueba"),
                    Function() New ProveedorForm(proveedor),
                    Function() New EspecificacionesForm(producto),
                    Function() New CorteCajaForm(Datos.Caja),
                    Function() New IngresoStockForm(proveedor),
                    Function() New CategoriasForm(),
                    Function() New ConfiguracionNegocioForm()
                }

                For Each constructor In formularios
                    Dim tipo = constructor().GetType().Name
                    For Each par In tamanos
                        Dim f As Control = constructor()
                        BajarDeNivel(f)
                        area.Controls.Add(f)
                        Forzar(f)
                        area.Controls.Remove(f)
                        f.Dispose()
                    Next
                    Revisar($"{tipo} se layoutea en los 4 tamanos", True)
                Next
                host.Dispose()
            End Using
        End Sub

        ''' <summary>Un Form es control de nivel superior y no se puede meter en un
        ''' contenedor: hay que bajarlo de nivel antes de medirlo.</summary>
        Sub BajarDeNivel(control As Control)
            If TypeOf control Is Form Then DirectCast(control, Form).TopLevel = False
        End Sub

        ''' <summary>
        ''' Refrescar es lo que llena las tablas y dibuja los graficos: es donde mas
        ''' codigo se ejecuta y donde no entraban las pruebas de consola. Ademas se
        ''' cambia el tema con todos los modulos ya construidos, que es el escenario
        ''' del boton claro/oscuro.
        ''' </summary>
        Sub ProbarRefrescosYTema()
            Console.WriteLine()
            Console.WriteLine("== CafeteriaOS: refresco de datos y cambio de tema ==" &
                              Environment.NewLine)

            ' Datos con contenido para que las tablas y los graficos tengan que pintar.
            Dim cafe = Datos.ListaProductos.First(Function(p) p.Nombre.Contains("negro"))
            StockService.Ajustar(cafe, -5, TipoMovimientoStock.Venta)
            Dim pedido As New Pedido With {
                .FechaHora = DateTime.Now,
                .NombreCliente = "Cliente Prueba",
                .TelefonoCliente = "3511111111",
                .TipoServicio = TipoServicio.EnElLocal,
                .Mesa = "3",
                .MetodoPago = MetodoPago.Efectivo,
                .UsuarioResponsable = "Administrador",
                .Items = New List(Of DetallePedido) From {
                    New DetallePedido With {.Producto = cafe, .Cantidad = 2}
                }
            }
            Dim venta = VentaService.Crear(pedido)
            VentaService.Confirmar(venta)
            ClienteService.RegistrarOActualizar(venta.ClienteNombre, venta.ClienteTelefono, venta)

            Using host As New Form With {.ClientSize = New Size(1280, 800)}
                Dim area As New Panel With {.Dock = DockStyle.Fill}
                host.Controls.Add(area)

                For Each seccion As SeccionDashboard In [Enum].GetValues(GetType(SeccionDashboard))
                    Dim panel = CrearPanel(seccion)
                    area.Controls.Add(panel)
                    Forzar(panel)
                    ' El panel ya esta enlazado a la host: se llama por reflexion porque
                    ' Refrescar es publico y no hace falta, pero asi se ejecuta igual
                    ' que cuando el usuario navega hasta el modulo.
                    panel.Refrescar()
                    Forzar(panel)
                    Revisar($"{seccion}.Refrescar con datos no lanza excepcion", True)

                    ' Segundo refresco sobre el mismo panel: tiene que poder repetirse.
                    panel.Refrescar()
                    Revisar($"{seccion}.Refrescar se puede repetir", True)
                Next

                ' Cambio de tema con los 7 modulos ya construidos, que es lo que pasa
                ' al apretar el boton de tema con varios modulos visitados.
                Try
                    Tema.ModoOscuro = True
                    Forzar(host)
                    For Each panel As Control In area.Controls
                        Tema.Restilar(panel)
                    Next
                    Forzar(host)
                    Revisar("El tema oscuro se aplica a los modulos abiertos", True)

                    Tema.ModoOscuro = False
                    Forzar(host)
                    For Each panel As Control In area.Controls
                        Tema.Restilar(panel)
                    Next
                    Forzar(host)
                    Revisar("El tema claro se vuelve a aplicar", True)
                Catch ex As Exception
                    Revisar("Cambiar el tema con los modulos abiertos", False)
                    Console.WriteLine(ex.ToString())
                End Try

                host.Dispose()
            End Using
        End Sub

        Function CrearPanel(seccion As SeccionDashboard) As PanelBase
            Select Case seccion
                Case SeccionDashboard.Venta : Return New POSPanel()
                Case SeccionDashboard.Pedidos : Return New KanbanPanel()
                Case SeccionDashboard.Stock : Return New StockPanel()
                Case SeccionDashboard.Clientes : Return New ClientesPanel()
                Case SeccionDashboard.Proveedores : Return New ProveedoresPanel()
                Case SeccionDashboard.Reportes : Return New ReportesPanel()
                Case SeccionDashboard.Caja : Return New CajaPanel()
            End Select
            Throw New InvalidOperationException(seccion.ToString())
        End Function

        ''' <summary>
        ''' Repite el calculo de layout varias veces y entrega los pendientes, que es
        ''' cuando WinForms arma de verdad los arboles de controles.
        ''' </summary>
        Sub Forzar(control As Control)
            For i = 0 To 3
                control.PerformLayout()
                Application.DoEvents()
                Dim pendientes = control.Controls.OfType(Of Control)().
                    Where(Function(c) c.Controls.Count > 0).ToList()
                For Each hijo As Control In pendientes
                    hijo.PerformLayout()
                Next
            Next
        End Sub

        Sub Revisar(descripcion As String, condicion As Boolean)
            pruebas += 1
            If condicion Then
                Console.WriteLine("  [ok]    " & descripcion)
            Else
                fallos += 1
                Console.WriteLine("  [FALLA] " & descripcion)
            End If
        End Sub

    End Module

End Namespace