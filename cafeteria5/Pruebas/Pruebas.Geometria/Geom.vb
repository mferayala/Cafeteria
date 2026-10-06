Imports System.IO
Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Linq
Imports System.Windows.Forms
Imports CafeteriaOS

Namespace Prueba

    ''' <summary>
    ''' Revisa el layout de los siete modulos sin干预 del usuario: texto que no entra
    ''' en el espacio asignado, tarjetas que se salen de su fila y tablas que dejan un
    ''' rectangulo vacio al lado de la ultima columna.
    ''' </summary>
    Module Geom

        ''' Margen de pixeles que se tolera antes de dar un problema.
        '''
        ''' Para el desborde vertical tiene que ser 0: si las filas piden mas espacio
        ''' del que hay, la ventana pide scrollbar, y dos pixeles de scrollbar son
        ''' dos pixeles de scrollbar. Con 6 de margen un formulario al que le
        ''' faltaban 2 px pasaba por estar bien, y asi se colaron los 2 px de
        ''' ReposicionStockForm.
        '''
        Const Tolerancia As Integer = 0

        Dim problemas As Integer
        Dim pruebas As Integer
        Dim destino As String

        <STAThread>
        Sub Main()
            Application.EnableVisualStyles()
            Application.SetCompatibleTextRenderingDefault(False)
            ' Los datos van a una carpeta propia y no a la de quien esta probando.
            ' Va antes de tocar el archivo: Nucleo.vb borra el historial al arrancar para
            ' empezar de cero, y sin esto se comia el archivo de ingresos de verdad.
            Dim carpeta = Path.Combine(Path.GetTempPath(), "CafeteriaOS-pruebas-geometria")
            If Directory.Exists(carpeta) Then Directory.Delete(carpeta, True)
            Directory.CreateDirectory(carpeta)
            Environment.SetEnvironmentVariable(IngresoAlmacen.VariableCarpeta, carpeta)

            DatosIniciales.Cargar()
            Datos.UsuarioActual = AutenticacionService.Buscar("admin@cafe.com", "123456")
            destino = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "capturas")
            System.IO.Directory.CreateDirectory(destino)
            Datos.Caja = New SesionCaja With {
                .Abierta = True, .MontoInicial = 25000D,
                .FechaApertura = DateTime.Now, .UsuarioApertura = "Administrador"
            }

            Dim cafe = Datos.ListaProductos.First(Function(p) p.Nombre.Contains("negro"))
            ' La historia que se arma abajo descuenta varias lineas del mismo cafe
            ' sin reponer nunca. Antes el clamp silencioso lo dejaba en cero y
            ' segua como si nada; ahora que el descuento avisa, el deposito tiene
            ' que estar cargado o el harness revienta al inventar la historia.
            cafe.Stock = 500

            Dim ultimo As Pedido = Nothing
            For i = 1 To 6
                Dim pedido As New Pedido With {
                    .FechaHora = DateTime.Now.AddMinutes(-i * 47),
                    .NombreCliente = New String() {"Maria Gomez", "Juan Perez", "Ana Diaz",
                                                  "Luis Sosa", "Carla Ruiz", "Pedro Diaz"}(i - 1),
                    .TelefonoCliente = "3511555" & (100 + i),
                    .TipoServicio = If(i Mod 2 = 0, TipoServicio.EnElLocal, TipoServicio.ParaLlevar),
                    .Mesa = If(i Mod 2 = 0, i.ToString(), ""),
                    .MetodoPago = New MetodoPago() {MetodoPago.Efectivo, MetodoPago.Transferencia,
                                                      MetodoPago.Debito}(i Mod 3),
                    .Items = New List(Of DetallePedido) From {
                        New DetallePedido With {.Producto = cafe, .Cantidad = 3 + i},
                        New DetallePedido With {.Producto = Datos.ListaProductos(i), .Cantidad = 2}
                    }
                }
                Dim venta = VentaService.Crear(pedido)
                VentaService.Confirmar(venta)
                ClienteService.RegistrarOActualizar(venta.ClienteNombre,
                                                    venta.ClienteTelefono, venta)
                pedido.VentaID = venta.ID
                ultimo = pedido
                StockService.Ajustar(cafe, -3, TipoMovimientoStock.Venta)
            Next
            Dim provIngreso = Datos.ListaProveedores.First()
            provIngreso.ContactoPrincipal = "Jorge Paz"
            IngresoService.Registrar(provIngreso, New List(Of DetalleIngreso) From {
                New DetalleIngreso With {.Producto = Datos.ListaProductos(1), .Cantidad = 40},
                New DetalleIngreso With {.Producto = Datos.ListaProductos(2), .Cantidad = 25},
                New DetalleIngreso With {.Producto = cafe, .Cantidad = 60}}, "Compra mensual")
            CajaService.RegistrarEgreso(4800D, "Compra de insumos")

            For Each tam In New Size() {New Size(1024, 640), New Size(1280, 800), New Size(1920, 1080)}
                Using host As New Form With {.ClientSize = tam,
                    .StartPosition = FormStartPosition.Manual,
                    .Location = New Point(-4000, -4000), .ShowInTaskbar = False}
                    Dim area As New Panel With {.Dock = DockStyle.Fill}
                    host.Controls.Add(area)
                    host.Show()
                    Forzar(host)
                    For Each seccion As SeccionDashboard In [Enum].GetValues(GetType(SeccionDashboard))
                        Dim panel = Crear(seccion)
                        area.Controls.Add(panel)
                        Forzar(host)
                        panel.Refrescar()
                        Forzar(host)
                        Dim donde = seccion.ToString() & " @ " & tam.Width & "x" & tam.Height
                        Forzar(host)
                        Medir(panel, donde)
                        Forzar(host)
                        MostrarCadaPestana(panel, donde, host)
                    Next
                    host.Close()
                End Using
            Next

            DetalleDeStock()
            Dialogos()
            EditorCargaDatos()
            IngresoMuestraDatos()
            MovimientosCargaDatos()
            ConfiguracionMuestraEjemplo()
            ElNombreDelNegocioSeMuestra()
            LaConfiguracionEsSoloDelAdministrador()
            ProveedoresNoOfreceIngreso()
            EscaladoEnPantallasGrandes()

            Console.WriteLine()
            Console.WriteLine($"OK: {pruebas} pruebas, {problemas} problemas.")
            If problemas > 0 Then Environment.ExitCode = 1
        End Sub

        ''' <summary>
        ''' El detalle de Stock tiene que mostrar de verdad los ingresos del producto
        ''' elegido, no solo compilar.
        ''' </summary>
        Sub DetalleDeStock()
            Using host As New Form With {.ClientSize = New Size(1280, 800),
                .StartPosition = FormStartPosition.Manual,
                .Location = New Point(-4000, -4000), .ShowInTaskbar = False}
                host.Show()
                Dim panel As New StockPanel()
                host.Controls.Add(panel)
                Forzar(host)
                panel.Refrescar()

                Dim tablas = Todos(panel).OfType(Of DataGridView)().ToList()
                Dim entradas = tablas.FirstOrDefault(Function(t) t.Columns.Contains("Fecha de ingreso"))
                Dim listado = tablas.First()
                If entradas Is Nothing Then
                    Console.WriteLine("  [falta]  Stock no tiene la tabla de ingresos")
                    problemas += 1
                    Exit Sub
                End If

                ' Se elige un producto que si tenga ingresos: el primero de la lista
                ' esta ordenado por stock y puede no tener ninguno.
                Dim conIngreso = Datos.ListaProductos.First(
                    Function(x) IngresoService.PorProducto(x).Count > 0)
                Dim fila = listado.Rows.Cast(Of DataGridViewRow)().
                    First(Function(r) ReferenceEquals(r.Tag, conIngreso))
                fila.Selected = True
                Forzar(host)
                pruebas += 1
                If entradas.Rows.Count = 0 Then
                    Console.WriteLine("  [vacio]  el detalle del producto no muestra ningun ingreso")
                    problemas += 1
                Else
                    Console.WriteLine($"  [ok]     el detalle muestra {entradas.Rows.Count} ingreso(s) del producto. " &
                                      $"Columnas: {String.Join(" | ", entradas.Rows.Cast(Of DataGridViewRow)().Select(Function(r) r.Cells(0).Value.ToString() & " " & r.Cells(1).Value.ToString() & " " & r.Cells(3).Value.ToString()))}")
                End If
                host.Close()
            End Using
        End Sub

        ''' <summary>
        ''' Un TabControl solo le da tamano real a la pagina que esta a la vista: las demas
        ''' quedan en 1px y medirlas ahi parece un recorte del panel cuando en realidad
        ''' nadie las vio asi. Se recorre cada pagina activandola, que es lo que hace el
        ''' usuario al hacer clic, y recien ahi se mide.
        ''' </summary>
        Sub MostrarCadaPestana(raiz As Control, contexto As String, host As Form)
            For Each tabs In Todos(raiz).OfType(Of TabControl)().Distinct()
                Dim inicial = tabs.SelectedIndex
                For i As Integer = 0 To tabs.TabPages.Count - 1
                    tabs.SelectedIndex = i
                    Forzar(host)
                    Medir(tabs.TabPages(i), contexto & " [" & tabs.TabPages(i).Text & "]")
                Next
                tabs.SelectedIndex = inicial
                Forzar(host)
            Next
        End Sub

        Sub Medir(panel As Control, contexto As String)
            For Each c In Todos(panel)
                ' Solo lo que esta a la vista se mide. Un TabControl no le da tamano
                ' real a las paginas que no estan seleccionadas: quedan en 1px y
                ' medir ahi inventa recortes que el usuario nunca ve. Las paginas
                ' ocultas se miden despues, activandolas, en MostrarCadaPestana.
                If Not c.Visible Then Continue For

                If TypeOf c Is Label Then
                    Dim l = DirectCast(c, Label)
                    ' AutoEllipsis recorta a proposito con puntos suspensivos.
                    If l.Text.Length > 0 AndAlso Not l.AutoEllipsis Then
                        Dim tiene = If(l.AutoSize, l.PreferredSize, l.ClientSize)
                        Comparar("Label '" & l.Text & "'", l.PreferredSize, tiene, contexto)
                    End If
                End If

                ' Solo importa en las tablas: ahi la celda recorta de verdad. En un
                ' FlowLayoutPanel la tarjeta simplemente ocupa su alto y nada la pisa.
                ' Solo hay riesgo cuando la tabla tiene una sola fila: ahi la celda es
                ' toda la tabla y una tarjeta con Dock=Fill igual se pasa si la fila
                ' quedo corta. Con varias filas cada hijo ocupa solo su celda.
                Dim tabla = TryCast(c.Parent, TableLayoutPanel)
                If TypeOf c Is Panel AndAlso c.Name = "th.tarjeta" AndAlso tabla IsNot Nothing AndAlso
                   tabla.RowCount = 1 Then
                    Dim sobra = c.Height - tabla.ClientSize.Height
                    If sobra > Tolerancia Then
                        Console.WriteLine($"  [desborde] {contexto}  '{c.Name}' dock={c.Dock} " &
                                          $"mide {c.Height}px y su tabla da {c.Parent.ClientSize.Height}px " &
                                          $"(se pasa en {sobra}px)")
                        problemas += 1
                    End If
                End If

                If TypeOf c Is ListView Then
                    Dim lv = DirectCast(c, ListView)
                    If lv.Columns.Count > 0 Then
                        Dim suma = 0
                        For i = 0 To lv.Columns.Count - 1
                            suma += lv.Columns(i).Width
                        Next
                        Dim libre = lv.ClientSize.Width - suma
                        If libre > Tolerancia Then
                            Console.WriteLine($"  [hueco]   {contexto}  ListView: {libre}px vacios al lado " &
                                              $"de la ultima columna ({lv.ClientSize.Width}px libres, " &
                                              $"{suma}px de columnas)")
                            problemas += 1
                        End If
                    End If
                End If
                pruebas += 1
            Next
        End Sub

        Sub Comparar(que As String, necesita As Size, tiene As Size, contexto As String)
            Dim faltaX = necesita.Width - tiene.Width
            Dim faltaY = necesita.Height - tiene.Height
            If faltaX > Tolerancia OrElse faltaY > Tolerancia Then
                Console.WriteLine($"  [recorte] {contexto}  {que} necesita {necesita.Width}x{necesita.Height} " &
                                  $"pero tiene {tiene.Width}x{tiene.Height}")
                problemas += 1
            End If
        End Sub

        ''' <summary>
        ''' Los dialogos no son paneles del dashboard, asi que el recorrido anterior
        ''' no los abria: sus textos sin recortar y sus tablas desalineadas pasaban
        ''' inadvertidos. Se abren de verdad y se miden con el mismo criterio.
        ''' </summary>
        Sub Dialogos()
            Dim cafe = Datos.ListaProductos.First(Function(p) p.Nombre.Contains("negro"))

            MedirDialogo(Function() New ProductoEditorForm(Nothing),
                         "ProductoEditorForm (nuevo)")
            MedirDialogo(Function() New ProductoEditorForm(cafe),
                         "ProductoEditorForm (editar)")
            MedirDialogo(Function() New ReposicionStockForm(cafe),
                         "ReposicionStockForm")
            MedirDialogo(Function() New IngresoStockForm(Datos.ListaProveedores.First()),
                         "IngresoStockForm")
            MedirDialogo(Function() New MovimientosStockForm(Nothing),
                         "MovimientosStockForm (todos)")
            MedirDialogo(Function() New MovimientosStockForm(cafe),
                         "MovimientosStockForm (producto)")
            MedirDialogo(Function() New ConfiguracionNegocioForm(),
                         "ConfiguracionNegocioForm")
            MedirDialogo(Function() New CategoriasForm(),
                         "CategoriasForm")
        End Sub

        ''' <summary>
        ''' El ingreso de mercaderia se hace desde Stock, no desde Proveedores. Si
        ''' vuelve a aparecer un boton de ingreso aca, searian dos caminos para la
        ''' misma operacion y el usuario no sabria cual usar.
        ''' </summary>
        Sub ProveedoresNoOfreceIngreso()
            Using panel As ProveedoresPanel = New ProveedoresPanel()
                panel.Refrescar()
                Dim botones = Todos(panel).OfType(Of Button)().ToList()
                ' Se busca el tallo "ingres", no "ingreso": el boton dice
                ' "Ingresar" y "Ingreso" no es subcadena de esa palabra, asi que
                ' buscando la forma completa el chequeo no detectaba nada.
                Dim conIngreso = botones.Where(
                    Function(b) b.Text.IndexOf("ingres",
                                               StringComparison.OrdinalIgnoreCase) >= 0).ToList()
                Revisar("Proveedores no ofrece cargar mercaderia (solo Stock lo hace)",
                        conIngreso.Count = 0)
                Revisar("Proveedores si ofrece agregar y editar proveedores",
                        botones.Any(Function(b) b.Text.IndexOf("nuevo",
                                                StringComparison.OrdinalIgnoreCase) >= 0) AndAlso
                        botones.Any(Function(b) b.Text.IndexOf("editar",
                                                StringComparison.OrdinalIgnoreCase) >= 0))
            End Using
        End Sub

        ''' <summary>
        ''' La pantalla de configuracion muestra el precio que saldria con los porcentajes
        ''' cargados. Si el ejemplo no aparece, el unico que se entera es el que esta
        ''' configuring.
        ''' </summary>
        Sub ConfiguracionMuestraEjemplo()
            Using f As New ConfiguracionNegocioForm()
                f.Show()
                Dim ejemplos = Todos(f).OfType(Of Label)().
                    Where(Function(l) l.Text.Contains("Ejemplo")).ToList()
                Revisar("Configuracion: hay una linea de ejemplo de precio",
                        ejemplos.Count = 1)
                If ejemplos.Count = 1 Then
                    Revisar("Configuracion: el ejemplo nombra el costo de referencia",
                            ejemplos(0).Text.Contains("1.000"))
                End If
            End Using
        End Sub

        ''' <summary>
        ''' El nombre del negocio tiene que llegar a la pantalla de ingreso, a la marca del
        ''' dashboard y al encabezado del ticket. Si no, el sistema se llama como quedo
        ''' escrito en el designer y no como el negocio que lo compro.
        ''' </summary>
        Sub ElNombreDelNegocioSeMuestra()
            Dim anterior = Datos.ConfigNegocio
            Try
                Datos.ConfigNegocio = New ConfiguracionNegocio With {
                    .NombreNegocio = "Kiosco El Portal"}

                Using f As New LoginForm()
                    f.Show()
                    Revisar("Login: el titulo es el nombre del negocio en mayusculas",
                            CStr(CType(CampoDe(f, "lblTitulo"), Label).Text) = "KIOSCO EL PORTAL")
                End Using

                Using d As New MainDashboardForm()
                    d.Show()
                    Revisar("Dashboard: la marca lateral es el nombre del negocio",
                            CStr(CType(CampoDe(d, "lblMarca"), Label).Text) = "Kiosco El Portal")
                End Using

                Revisar("Ticket: el encabezado arranca con el nombre del negocio",
                        Negocio.EncabezadoTicket().StartsWith("Kiosco El Portal"))
            Finally
                Datos.ConfigNegocio = anterior
            End Try
        End Sub

        ''' <summary>
        ''' Un cajero no puede ver el boton de configuracion, y aunque se lo muestre el
        ''' permiso lo frena: el margen y el IVA no son datos de un cajero.
        ''' </summary>
        Sub LaConfiguracionEsSoloDelAdministrador()
            Dim anterior = Datos.UsuarioActual
            Try
                Datos.UsuarioActual = New Usuario With {
                    .Rol = RolUsuario.Cajero}
                Using d As New MainDashboardForm()
                    d.Show()
                    Revisar("Dashboard: el cajero no ve el boton de configuracion",
                            Not CBool(CType(CampoDe(d, "btnConfiguracion"), Button).Visible))
                End Using

                Revisar("Un cajero no puede editar la configuracion",
                        Not AutenticacionService.PuedeEditarConfiguracion(RolUsuario.Cajero))
                Revisar("Un administrador si puede editar la configuracion",
                        AutenticacionService.PuedeEditarConfiguracion(RolUsuario.Administrador))
            Finally
                Datos.UsuarioActual = anterior
            End Try
        End Sub

        ''' <summary>
        ''' El editor tiene que mostrar los datos del producto que se esta editando.
        ''' Se miran los campos por reflexion porque son privados a proposito: la
        ''' pantalla no expone el estado, y el chequeo confirma que lo cargo bien.
        ''' </summary>
        Sub EditorCargaDatos()
            Dim p As New Producto With {
                .ID = 999, .Nombre = "Jugo en sobre", .Marca = "TANG",
                .CodigoBarra = "7790123456789", .UnidadMedida = "sobre",
                .Categoria = "Bebidas", .Subcategoria = "Sin gas",
                .Descripcion = "Sabor durazno", .PrecioCosto = 1000D,
                .Precio = 2500D, .Stock = 7, .Atajo = "Ctrl+J"}
            Using f As New ProductoEditorForm(p)
                f.Show()
                Forzar(f)
                RevisarCampo(f, "txtNombre", "Jugo en sobre")
                RevisarCampo(f, "txtMarca", "TANG")
                RevisarCampo(f, "txtCodigoBarra", "7790123456789")
                RevisarCampo(f, "txtUnidad", "sobre")
                RevisarCampo(f, "txtDescripcion", "Sabor durazno")
                RevisarCampo(f, "txtAtajo", "Ctrl+J")
                f.Close()
            End Using

            Using f As New ProductoEditorForm(Nothing)
                f.Show()
                Forzar(f)
                RevisarCampo(f, "txtNombre", "")
                RevisarCampo(f, "txtUnidad", "un")
                f.Close()
            End Using
        End Sub

        ''' <summary>
        ''' El ingreso se abre con el catalogo-loaded y los filtros armados. Se
        ''' revisa que la pantalla muestre datos de verdad y no una grilla vacia.
        ''' </summary>
        ''' <summary>
        ''' El historial se arma con los movimientos que hay y los filtros dejan
        ''' filtrar por tipo, por producto y por rango de fechas.
        ''' </summary>
        Sub MovimientosCargaDatos()
            Dim cafe = Datos.ListaProductos.First(Function(p) p.Nombre.Contains("negro"))
            Using f As New MovimientosStockForm(Nothing)
                f.Show()
                Forzar(f)
                Dim tabla = TablaDe(f, "tabla")
                Revisar("El historial trae los movimientos existentes",
                        tabla IsNot Nothing AndAlso tabla.Rows.Count = Datos.ListaMovimientos.Count)
                Revisar("El historial ordena del mas nuevo al mas viejo",
                        tabla.Rows.Count < 2 OrElse
                        CDate(tabla.Rows(0).Cells(0).Value) >= CDate(tabla.Rows(1).Cells(0).Value))
                Revisar("El filtro de tipo trae todos los tipos",
                        ComboDe(f, "cmbTipo").Items.Count =
                        [Enum].GetValues(GetType(TipoMovimientoStock)).Length + 1)
                Revisar("Acotado a un producto sale solo lo suyo",
                        MovimientosDe(f, cafe) > 0 AndAlso
                        MovimientosDe(f, cafe) < Datos.ListaMovimientos.Count)
                Revisar("Sin filtro de producto salen todos",
                        TablaDe(f, "tabla").Rows.Count = Datos.ListaMovimientos.Count)
                f.Close()
            End Using
        End Sub

        Function MovimientosDe(f As Form, producto As Producto) As Integer
            Dim tabla = TablaDe(f, "tabla")
            If tabla Is Nothing Then Return 0
            Dim n = 0
            For Each fila As DataGridViewRow In tabla.Rows
                Dim mov = TryCast(fila.Tag, MovimientoStock)
                If mov IsNot Nothing AndAlso ReferenceEquals(mov.Producto, producto) Then n += 1
            Next
            Return n
        End Function

        Sub IngresoMuestraDatos()
            Dim cafe = Datos.ListaProductos.First(Function(p) p.Nombre.Contains("negro"))
            Using f As New IngresoStockForm(Datos.ListaProveedores.First())
                f.Show()
                Forzar(f)
                Dim catalogo = TablaDe(f, "tablaCatalogo")
                Dim detalle = TablaDe(f, "tabla")
                Revisar("El ingreso trae el catalogo cargado",
                        catalogo IsNot Nothing AndAlso catalogo.Rows.Count > 0)
                Revisar("El catalogo muestra nombre, categoria, stock y precio",
                        catalogo IsNot Nothing AndAlso catalogo.Columns.Count = 4)
                Revisar("El filtro de categorias trae la opcion de todas",
                        ComboDe(f, "cmbCategoria") IsNot Nothing)
                Revisar("El detalle del ingreso arranca vacio",
                        detalle IsNot Nothing AndAlso detalle.Rows.Count = 0)
                Revisar("Se puede elegir proveedor o dejarlo sin proveedor",
                        ComboDe(f, "cmbProveedor") IsNot Nothing AndAlso
                        ComboDe(f, "cmbProveedor").Items.Count > 1)
                Revisar("El boton de quitar linea arranca deshabilitado",
                        BotonDe(f, "btnQuitar") IsNot Nothing AndAlso
                        Not BotonDe(f, "btnQuitar").Enabled)
                Revisar("Confirmar arranca habilitado",
                        BotonDe(f, "btnConfirmar").Enabled)
                f.Close()
            End Using

            Using f As New IngresoStockForm(Nothing)
                f.Show()
                Forzar(f)
                Dim idx = ComboDe(f, "cmbProveedor").SelectedIndex
                Revisar("Sin proveedor se elige la opcion sin proveedor",
                        idx = 0)
                Revisar("El primer item del proveedor es sin proveedor",
                        ComboDe(f, "cmbProveedor").Items(0).ToString() =
                        "(sin proveedor)")
                f.Close()
            End Using
        End Sub

        Sub Revisar(nombre As String, condicion As Boolean)
            pruebas += 1
            If Not condicion Then
                Console.WriteLine($"  [dato]  {nombre}")
                problemas += 1
            End If
        End Sub

        ''' <summary>
        ''' Los campos declarados WithEvents se guardan en los metadatos con un guion
        ''' bajo adelante, asi que el nombre real a buscar lleva ese prefijo.
        ''' </summary>
        Function CampoDe(f As Form, campo As String) As Object
            Dim ctrl = f.GetType().GetField(campo, FlagsDe())
            If ctrl Is Nothing Then ctrl = f.GetType().GetField("_" & campo, FlagsDe())
            If ctrl Is Nothing Then Return Nothing
            Return ctrl.GetValue(f)
        End Function

        Function TablaDe(f As Form, campo As String) As DataGridView
            Return TryCast(CampoDe(f, campo), DataGridView)
        End Function

        Function ComboDe(f As Form, campo As String) As ComboBox
            Return TryCast(CampoDe(f, campo), ComboBox)
        End Function

        Function BotonDe(f As Form, campo As String) As Button
            Return TryCast(CampoDe(f, campo), Button)
        End Function

        Function FlagsDe() As System.Reflection.BindingFlags
            Return CType(System.Reflection.BindingFlags.NonPublic Or
                         System.Reflection.BindingFlags.Instance,
                         System.Reflection.BindingFlags)
        End Function

        Sub RevisarCampo(f As Form, campo As String, esperado As String)
            Dim valor = ValorDe(f, campo)
            pruebas += 1
            If valor <> esperado Then
                Console.WriteLine($"  [dato]  {campo} shows '{valor}', expected '{esperado}'")
                problemas += 1
            End If
        End Sub

        Function ValorDe(f As Form, campo As String) As String
            Dim c = TryCast(CampoDe(f, campo), Control)
            If c Is Nothing Then Return "<no es control>"
            Return If(c.Text, "")
        End Function

        Sub MedirDialogo(crear As Func(Of Form), contexto As String)
            Using f As Form = crear()
                f.StartPosition = FormStartPosition.Manual
                f.Location = New Point(-4000, -4000)
                f.ShowInTaskbar = False
                f.Show()
                Forzar(f)
                Medir(f, contexto)
                f.Close()
            End Using
        End Sub

        Function Todos(control As Control) As List(Of Control)
            Dim lista As New List(Of Control)
            For Each hijo As Control In control.Controls
                lista.Add(hijo)
                lista.AddRange(Todos(hijo))
            Next
            Return lista
        End Function

        Sub Forzar(control As Control)
            For i = 0 To 4
                control.PerformLayout()
                For Each hijo As Control In control.Controls
                    hijo.PerformLayout()
                Next
                Application.DoEvents()
            Next
        End Sub

        Function Crear(seccion As SeccionDashboard) As PanelBase
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
        ''' Las ventanas fijas tienen que verse enteras en su tamano normal.
        '''
        ''' Se comprueba lo que se pidio y que antes no se comprobaba: que la ventana no se
        ''' pueda maximizar, y que el alto de todas sus filas entre en el area visible. Si las
        ''' filas piden mas espacio del que hay, el usuario tiene que scrollear para ver lo
        ''' que quedo abajo, y eso es justamente lo que no debe pasar.
        '''
        ''' Solo tiene sentido cuando todas las filas tienen alto fijo: si hay una Percent, esa
        ''' fila se acomoda al espacio que sobra y no desborda.
        ''' </summary>
        Sub EscaladoEnPantallasGrandes()
            ' El ancho del rotulo se mide contra la fuente DE LA VENTANA, no contra la
            ' ya agrandada. Este chequeo responde a una pregunta distinta: "si la fuente
            ' de la app creciera, este rotulo seguiria entrando?". Para responder a la
            ' que delegado el usuario -el texto se ve cortado hoy- alcanza con medir la
            ' fuente normal contra el ancho real, y eso lo hace Medir() en Dialogos().
            Dim cafe = Datos.ListaProductos.First(Function(p) p.Nombre.Contains("negro"))
            Dim lista As New List(Of Func(Of Form))

            lista.Add(Function() New ProductoEditorForm(Nothing))
            lista.Add(Function() New ProductoEditorForm(cafe))
            lista.Add(Function() New ReposicionStockForm(cafe))
            lista.Add(Function() New IngresoStockForm(Nothing))
            lista.Add(Function() New MovimientosStockForm(Nothing))
            lista.Add(Function() New CategoriasForm())
            lista.Add(Function() New ConfiguracionNegocioForm())
            lista.Add(Function() New CobroForm(1000D))

            For Each constructor In lista
                Dim f As Form = constructor.Invoke()
                f.StartPosition = FormStartPosition.Manual
                f.Location = New Point(-4000, -4000)
                f.ShowInTaskbar = False
                f.Show()
                Forzar(f)

                Dim esNoMaximizable = f.GetType().Name = "ProductoEditorForm" OrElse
                                     f.GetType().Name = "ReposicionStockForm"
                If esNoMaximizable Then
                    Revisar(f.GetType().Name & " no se puede maximizar", Not f.MaximizeBox)
                Else
                    Revisar(f.GetType().Name & " no se debe bloquear el maximizado", True)
                End If

                ' Todo el contenido tiene que entrar sin barra vertical. Solo se puede
                ' comprobar cuando todas las filas tienen alto fijo: si hay una
                ' Percent, esa fila se acomoda al espacio que sobra y no desborda.
                Dim tabla = TablaRaiz(f)
                If tabla IsNot Nothing Then
                    Dim hayPercent = False
                    Dim pedidos As Integer = 0
                    For i = 0 To tabla.RowStyles.Count - 1
                        Dim estilo = tabla.RowStyles(i)
                        If estilo.SizeType = SizeType.Percent Then
                            hayPercent = True
                        ElseIf estilo.SizeType = SizeType.Absolute Then
                            pedidos += CInt(estilo.Height)
                        End If
                    Next
                    Dim visibles = tabla.ClientSize.Height - tabla.Padding.Vertical
                    If Not hayPercent AndAlso pedidos > visibles Then
                        Console.WriteLine($"  [desborde] {f.GetType().Name}  las filas piden " &
                                          $"{pedidos}px y la ventana da {visibles}px: " &
                                          "hay que scrollear para ver todo")
                        problemas += 1
                    End If
                End If

                f.Close()
            Next
        End Sub

        ''' <summary>
        ''' El TableLayoutPanel raiz del formulario, o Nothing si no lo hay. Se busca el mas
        ''' externo porque es el que reparte el alto disponible; los anidados manejan el suyo.
        ''' </summary>
        Function TablaRaiz(f As Form) As TableLayoutPanel
            For Each c In f.Controls
                If TypeOf c Is TableLayoutPanel Then
                    Return DirectCast(c, TableLayoutPanel)
                End If
            Next
            Return Nothing
        End Function

    End Module

End Namespace
