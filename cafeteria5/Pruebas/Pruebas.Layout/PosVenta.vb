Imports System.IO
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Linq
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports CafeteriaOS

Namespace Prueba

    ''' <summary>
    ''' Bug C: NullReferenceException al confirmar la venta.
    '''
    ''' Se prueba la matriz completa: con y sin preparacion, con y sin dejar a
    ''' cuenta. Antes fallaba en las cuatro. No dependia de ninguna de las dos
    ''' opciones, y esa es la pista de donde venia: si el error fuera del cobro o
    ''' del tablero, solo pasaria con una de las combinaciones.
    '''
    ''' La causa era que chkPreparar se creaba con "Dim" dentro del metodo que arma la
    ''' pantalla, no como campo. Entonces el campo de la clase quedaba en Nothing
    ''' mientras existia una variable local con el mismo nombre, y al leer
    ''' "chkPreparar.Checked" para saber si la venta iba al tablero se reventaba en
    ''' todos los casos. Ya estaba arreglado; lo que faltaba era una prueba que lo
    ''' sostenga, porque el arreglo es de una linea y se pierde sin avisar.
    '''
    ''' El flujo es el real del POS: se hace clic en una tarjeta del catalogo, se
    ''' tilda "Requiere preparacion", se pulsa "Registrar y cobrar", se contesta el
    ''' cobro y se cierra el ticket. No se toca ningun metodo privado.
    '''
    ''' El cobro, los avisos y el ticket son modales, asi que el hijo no puede
    ''' avanzar solo: hace falta un bombeo de mensajes que los atienda mientras corre
    ''' el clic. Un Forms.Timer sirve para eso, porque encola un WM_TIMER que SI se
    ''' despacha mientras hay un ShowDialog abierto, a diferencia de
    ''' Application.DoEvents, que solo corre entre clicks.
    '''
    ''' Cada combinacion corre en un proceso hijo: si la excepcion se escapa del
    ''' manejador del boton, el proceso muere y la traza queda en stderr, que es lo
    ''' que hay que leer para saber donde esta la falla.
    ''' </summary>
    Module PruebasPosVentaBugC

        Sub Correr()
            For Each preparacion As Boolean In New Boolean() {False, True}
                For Each aCuenta As Boolean In New Boolean() {False, True}
                    ConfirmarVentaNoExplota(preparacion, aCuenta)
                Next
            Next
        End Sub

        Sub ConfirmarVentaNoExplota(preparacion As Boolean, aCuenta As Boolean)
            Dim nombre = "preparacion=" & preparacion & " aCuenta=" & aCuenta
            Dim r = PruebasProductoEditor.CorrerEnProcesoHijo(
                        "pos-venta " & preparacion & " " & aCuenta)

            ' El hijo no se cuelga en ningun dialogo: es lo primero que se verifica
            ' porque si el proceso queda trabado no hay nada mas que leer.
            Revisar("[POS " & nombre & "] el flujo no se queda trabado en un dialogo " &
                    "(codigo " & r.Codigo & ")", r.Codigo = 0)
            If r.Codigo <> 0 Then
                Console.WriteLine("--- traza del hijo [POS " & nombre & "] ---")
                Console.WriteLine(r.Salida)
                Return
            End If

            ' Si el hijo reporto una excepcion se imprime acá. Sin esto el fallo
            ' dice que algo se rompio pero no cual, y el que tendria que leerla
            ' tendria que volver a correr el hijo a mano para enterarse.
            Dim excepcion = Linea(r.Salida, "POS exception")
            If excepcion.Length > 0 Then
                Console.WriteLine("--- excepcion del hijo [POS " & nombre & "] ---")
                Console.WriteLine(excepcion)
            End If
            Dim falloDelCobro = Linea(r.Salida, "POS cobro-error")
            If falloDelCobro.Length > 0 Then
                Console.WriteLine("--- fallo al cobrar [POS " & nombre & "] ---")
                Console.WriteLine(falloDelCobro)
            End If

            Revisar("[POS " & nombre & "] no se escapo ninguna excepcion del manejador " &
                    "del boton", excepcion = String.Empty)
            Revisar("[POS " & nombre & "] la venta quedo registrada",
                    Linea(r.Salida, "POS venta-registrada") = "1")

            ' El pago tiene que quedar como el usuario lo pidio. Si esto no se
            ' revisa, un cobro que degrade a "Pagada" una venta a cuenta pasaria
            ' desapercibido.
            Revisar("[POS " & nombre & "] la venta quedo " & EstadoEsperado(aCuenta) &
                    " (quedo " & Linea(r.Salida, "POS venta-pagada") & ")",
                    Linea(r.Salida, "POS venta-pagada") = EstadoEsperado(aCuenta))
            Revisar("[POS " & nombre & "] la marca de a cuenta es la correcta",
                    Linea(r.Salida, "POS venta-a-cuenta") = If(aCuenta, "True", "False"))

            ' Y con preparacion tiene que haber pedido en el tablero; sin ella, no.
            ' Esta es la parte que leia chkPreparar y por ahi se reventaba.
            Revisar("[POS " & nombre & "] el tablero " &
                    If(preparacion, "recibe", "no recibe") & " el pedido (quedo " &
                    Linea(r.Salida, "POS pedido-real") & ")",
                    Linea(r.Salida, "POS pedido-real") = If(preparacion, "1", "0"))
        End Sub

        Function EstadoEsperado(aCuenta As Boolean) As String
            Return If(aCuenta, "Pendiente", "Pagada")
        End Function

        Function Linea(salida As String, clave As String) As String
            Dim prefijo = clave & "="
            For Each l As String In salida.Replace(vbCrLf, vbLf).Split(vbLf)
                If l.StartsWith(prefijo, StringComparison.Ordinal) Then
                    Return l.Substring(prefijo.Length).Trim()
                End If
            Next
            Return String.Empty
        End Function

        ' ---------------------------------------------------------------- hijo

        Public Sub MainHijo(args As String())
            Dim preparacion = args(1) = "True"
            Dim aCuenta = args(2) = "True"

            PrepararDatos()

            Using host As New Form With {
                .ClientSize = New Size(1180, 760),
                .StartPosition = FormStartPosition.Manual,
                .Location = New Point(-4000, -4000),
                .ShowInTaskbar = False}

                Dim panel As New POSPanel()
                host.Controls.Add(panel)
                host.Show()
                Forzar(host)

                Dim alertas As New List(Of String)
                Using reloj As New Timer With {.Interval = 30}
                    Dim cobrado As Boolean
                    Dim falloCobro As String = Nothing

                    AddHandler reloj.Tick,
                        Sub(sender, e)
                            Dim cobroForm = FormAbierto(Of CobroForm)
                            If cobroForm IsNot Nothing AndAlso Not cobrado Then
                                cobrado = True
                                Try
                                    AtenderCobro(cobroForm, aCuenta)
                                Catch ex As Exception
                                    falloCobro = ex.GetType().FullName & ": " & ex.Message &
                                                  vbCrLf & ex.StackTrace
                                End Try
                                Return
                            End If

                            TitulosDelHijo = alertas
                            If CerrarAvisos() Then Return

                            Dim otra = OtraVentanaDelFlujo(host)
                            If otra IsNot Nothing Then
                                If Not alertas.Contains(otra.Text) Then alertas.Add(otra.Text)
                                otra.Close()
                            End If
                        End Sub

                    reloj.Start()
                    Dim fallo As String = Nothing
                    Try
                        CargarCarrito(host)
                        TildarPreparacion(host, preparacion)
                        Exigir(PreparacionTildada(host) = preparacion,
                               "la casilla de preparacion no quedo como se pidio")
                        Boton(host, "Registrar y cobrar").PerformClick()
                    Catch ex As Exception
                        ' La excepcion que se escapa del manejador del boton es el
                        ' bug. Se escribe completa porque el numero de linea es lo
                        ' que dice donde esta la falla.
                        fallo = ex.GetType().FullName & ": " & ex.Message & vbCrLf & ex.StackTrace
                    Finally
                        reloj.Stop()
                    End Try

                    ' Si el ticket o un aviso siguen abiertos, el flujo se quedo a
                    ' medias: se cierran para poder terminar el hijo.
                    For i = 0 To 40
                        Dim abierta = OtraVentanaDelFlujo(host)
                        If abierta Is Nothing Then Exit For
                        If Not alertas.Contains(abierta.Text) Then alertas.Add(abierta.Text)
                        abierta.Close()
                        Application.DoEvents()
                        Threading.Thread.Sleep(25)
                    Next

                    Forzar(host)

                    Dim venta = UltimaVenta()
                    Console.WriteLine("POS exception=" & If(fallo, ""))
                    Console.WriteLine("POS cobro-error=" & If(falloCobro, ""))
                    Console.WriteLine("POS venta-registrada=" & If(venta IsNot Nothing, "1", "0"))
                    Console.WriteLine("POS venta-pagada=" & If(venta?.Pago.ToString(), ""))
                    Console.WriteLine("POS venta-a-cuenta=" & If(venta?.FueACuenta.ToString(), ""))
                    Console.WriteLine("POS pedido-real=" & UltimoPedido())
                    Console.WriteLine("POS alertas=" & alertas.Count)
                    For Each a In alertas
                        Console.WriteLine("POS alerta=" & a.Replace(vbCrLf, " / "))
                    Next
                End Using
            End Using
        End Sub

        ''' <summary>Hace clic en la primera tarjeta del catalogo: eso agrega el producto.</summary>
        Sub CargarCarrito(host As Form)
            Dim flujo = FlujoDelCatalogo(host)
            If flujo Is Nothing OrElse flujo.Controls.Count = 0 Then
                Throw New InvalidOperationException("El catalogo del POS salio vacio.")
            End If

            Clic(flujo.Controls(0))

            Dim items = ListadoDeItems(host)
            If items Is Nothing OrElse items.Items.Count = 0 Then
                Throw New InvalidOperationException(
                    "El clic en la tarjeta no agrego nada al pedido.")
            End If
        End Sub

        Sub AtenderCobro(cobroForm As CobroForm, aCuenta As Boolean)
            Dim casilla = Buscar(Of CheckBox)(cobroForm)
            If casilla Is Nothing Then
                Throw New InvalidOperationException(
                    "El formulario de cobro no tiene la casilla de dejar a cuenta.")
            End If
            casilla.Checked = aCuenta

            If Not aCuenta Then CobrarConEfectivo(cobroForm)

            ' El boton de cobrar no tiene Name, y su texto cambia a "Dejar a cuenta"
            ' cuando se tilda la casilla. Se busca por texto porque es lo unico que
            ' se puede sostener en las dos formas de pago.
            Dim cobrar = BotonesDe(cobroForm).FirstOrDefault(
                Function(b) b.Text = "Confirmar cobro" OrElse b.Text = "Dejar a cuenta")
            If cobrar Is Nothing Then
                Throw New InvalidOperationException(
                    "El formulario de cobro no tiene boton de cobrar. Botones: " &
                    String.Join(", ", BotonesDe(cobroForm).Select(Function(b) b.Text)))
            End If

            ' El clic se pospone con BeginInvoke y no se hace aca. Si se hiciera aca,
            ' el MessageBox que puede abrir el propio boton abriria su bucle modal
            ' DENTRO de este manejador del timer, el handler no volveria nunca y el
            ' timer no volveria a tildar: el hijo se quedaria colgado sin poder
            ' avanzar ni imprimir nada.
            cobroForm.BeginInvoke(Sub() cobrar.PerformClick())
        End Sub

        ''' <summary>Escribe un importe que cubre cualquier total.</summary>
        '''
        ''' El formulario de cobro no le pone Name a los campos, asi que se toman los
        ''' textbox por orden: CobroForm tiene uno solo, el del importe entregado. Se
        ''' entrega de sobra a proposito; lo que se prueba aca es que la venta llegue
        ''' a confirmarse, no el calculo del cambio, que ya tiene su propia prueba.
        ''' </summary>
        Sub CobrarConEfectivo(cobroForm As CobroForm)
            Dim campos = BuscarTodos(Of TextBox)(cobroForm)
            If campos.Count = 0 Then
                Throw New InvalidOperationException(
                    "El cobro no tiene donde escribir el importe entregado.")
            End If
            campos(0).Text = "999999"
        End Sub

        ' -------------------------------------------------------Find y helpers

        ''' <summary>
        ''' Busca el primer control del tipo pedido en todo el arbol.
        ''' </summary>
        ''' El flujo del catalogo esta metido varios niveles adentro (panel, tabla,
        ''' flujo), asi que mirar solo los hijos directos no lo encuentra.
        Function Buscar(Of T As Control)(raiz As Control) As T
            If TypeOf raiz Is T Then Return DirectCast(raiz, T)
            For Each hijo As Control In raiz.Controls
                Dim encontrado = Buscar(Of T)(hijo)
                If encontrado IsNot Nothing Then Return encontrado
            Next
            Return Nothing
        End Function

        Function BuscarTodos(Of T As Control)(raiz As Control) As List(Of T)
            Dim lista As New List(Of T)
            If TypeOf raiz Is T Then lista.Add(DirectCast(raiz, T))
            For Each hijo As Control In raiz.Controls
                lista.AddRange(BuscarTodos(Of T)(hijo))
            Next
            Return lista
        End Function

        Function BotonesDe(raiz As Control) As List(Of Button)
            Return BuscarTodos(Of Button)(raiz)
        End Function

        ''' <summary>
        ''' Primer form visible del tipo pedido, sin usar reflexion.
        ''' </summary>
        ''' Compara con TypeOf y no con GetType: el ticket es un TicketForm, no un
        ''' Form, asi que con igualdad exacta de tipos nunca se encontraba y el flujo
        ''' se quedaba esperando a un dialogo que nadie cerraba.
        Function FormAbierto(Of T As Form)() As T
            For Each f As Form In Application.OpenForms
                If f.Visible AndAlso TypeOf f Is T Then Return DirectCast(f, T)
            Next
            Return Nothing
        End Function

        ''' <summary>Primera ventana propia del flujo que no sea el host ni el cobro.</summary>
        Function OtraVentanaDelFlujo(host As Form) As Form
            For Each f As Form In Application.OpenForms
                If f.Visible AndAlso f IsNot host AndAlso TypeOf f IsNot CobroForm Then
                    Return f
                End If
            Next
            Return Nothing
        End Function

        Function FlujoDelCatalogo(host As Form) As FlowLayoutPanel
            For Each panel In host.Controls.OfType(Of POSPanel)()
                Dim flujo = Buscar(Of FlowLayoutPanel)(panel)
                If flujo IsNot Nothing AndAlso flujo.Controls.Count > 0 Then Return flujo
            Next
            Return Nothing
        End Function

        Function ListadoDeItems(host As Form) As ListView
            For Each panel In host.Controls.OfType(Of POSPanel)()
                Dim lista = Buscar(Of ListView)(panel)
                If lista IsNot Nothing Then Return lista
            Next
            Return Nothing
        End Function

        Function Boton(f As Form, texto As String) As Button
            Dim b = Buscar(Of Button)(f)
            If b Is Nothing Then
                Throw New InvalidOperationException("El panel del POS no tiene botones.")
            End If
            Dim pedido = BotonesDe(f).FirstOrDefault(Function(x) x.Text = texto)
            If pedido Is Nothing Then
                Throw New InvalidOperationException("No se encontro el boton """ & texto & """.")
            End If
            Return pedido
        End Function

        ''' <summary>Tilda o destilda la casilla de preparacion del POS.</summary>
        Sub TildarPreparacion(f As Form, valor As Boolean)
            Dim casilla = Buscar(Of CheckBox)(f)
            If casilla Is Nothing Then
                Throw New InvalidOperationException(
                    "El panel del POS no tiene la casilla de preparacion.")
            End If
            casilla.Checked = valor
            Forzar(f)
        End Sub

        Function PreparacionTildada(f As Form) As Boolean
            Dim c = Buscar(Of CheckBox)(f)
            Return c IsNot Nothing AndAlso c.Checked
        End Function

        Function UltimaVenta() As Venta
            If Datos.ListaVentas Is Nothing Then Return Nothing
            Return Datos.ListaVentas.LastOrDefault()
        End Function

        Function UltimoPedido() As String
            If Datos.ListaPedidos Is Nothing OrElse Datos.ListaPedidos.Count = 0 Then Return "0"
            Return "1"
        End Function

        Sub PrepararDatos()
            Dim carpeta = Path.Combine(Path.GetTempPath(), "CafeteriaOS-pruebas-pos-venta")
            If Directory.Exists(carpeta) Then Directory.Delete(carpeta, True)
            Directory.CreateDirectory(carpeta)
            Environment.SetEnvironmentVariable(IngresoAlmacen.VariableCarpeta, carpeta)

            DatosIniciales.Cargar()
            Datos.UsuarioActual = AutenticacionService.Buscar("admin@cafe.com", "123456")

            ' Sin caja abierta el flujo ofrece abrirla con un InputBox, que es otro
            ' modal. Se deja abierta para que la venta siga derecho al cobro.
            Datos.Caja = New SesionCaja With {
                .Abierta = True, .MontoInicial = 25000D,
                .FechaApertura = DateTime.Now, .UsuarioApertura = "Administrador"
            }
        End Sub

        Sub Exigir(condicion As Boolean, texto As String)
            If Not condicion Then Throw New InvalidOperationException(texto)
        End Sub

        Sub Forzar(control As Control)
            For i = 1 To 3
                control.PerformLayout()
                Application.DoEvents()
            Next
        End Sub

        ' ------------------------------------------------- clic real de mouse

        Const WM_LBUTTONDOWN As Integer = &H201
        Const WM_LBUTTONUP As Integer = &H202
        Const WM_CLOSE As Integer = &H10
        Const CLASE_DIALOGO As String = "#32770"

        Delegate Function EnumWindowsProc(hWnd As IntPtr, lParam As IntPtr) As Boolean

        <DllImport("user32.dll")>
        Private Function SendMessage(hWnd As IntPtr, wMsg As Integer,
                                     wParam As IntPtr, lParam As IntPtr) As IntPtr
        End Function

        <DllImport("user32.dll")>
        Private Function PostMessage(hWnd As IntPtr, wMsg As Integer,
                                     wParam As IntPtr, lParam As IntPtr) As Boolean
        End Function

        <DllImport("user32.dll")>
        Private Function EnumWindows(proc As EnumWindowsProc, lParam As IntPtr) As Boolean
        End Function

        <DllImport("user32.dll", CharSet:=CharSet.Unicode)>
        Private Function GetClassName(hWnd As IntPtr, sb As System.Text.StringBuilder,
                                      max As Integer) As Integer
        End Function

        <DllImport("user32.dll", CharSet:=CharSet.Unicode)>
        Private Function GetWindowText(hWnd As IntPtr, sb As System.Text.StringBuilder,
                                       max As Integer) As Integer
        End Function

        <DllImport("user32.dll")>
        Private Function IsWindowVisible(hWnd As IntPtr) As Boolean
        End Function

        ''' <summary>
        ''' Cierra los MessageBox abiertos y devuelve si cerro alguno.
        ''' </summary>
        ''' Un MessageBox no es un Form gestionado: no aparece en Application.OpenForms
        ''' y Form.ActiveForm lo devuelve vacio mientras no haya ningun form activo.
        ''' Con preparacion el primer dialogo que aparece es justamente un MessageBox,
        ''' asi que sin buscarlos como ventanas nativas de clase "#32770" el flujo se
        ''' trababa ahi y el hijo se quedaba colgado.
        '''
        ''' El cierre va con PostMessage y no con SendMessage: el aviso se cierra desde
        ''' el mismo hilo que esta bombeando su bucle modal, y un SendMessage ahi queda
        ''' esperando una respuesta que no llega nunca, porque ese hilo no vuelve a
        ''' atender su cola hasta que el MessageBox se cierra.
        Function CerrarAvisos() As Boolean
            CerradosDelHijo = False
            EnumWindows(AddressOf RecorrerVentanasDeAviso, IntPtr.Zero)
            Return CerradosDelHijo
        End Function

        Private CerradosDelHijo As Boolean
        Private TitulosDelHijo As List(Of String)

        Private Function RecorrerVentanasDeAviso(hWnd As IntPtr, lParam As IntPtr) As Boolean
            If Not IsWindowVisible(hWnd) Then Return True

            Dim clase As New System.Text.StringBuilder(64)
            GetClassName(hWnd, clase, clase.Capacity)
            If clase.ToString() <> CLASE_DIALOGO Then Return True

            Dim titulo As New System.Text.StringBuilder(512)
            GetWindowText(hWnd, titulo, titulo.Capacity)

            Dim texto = titulo.ToString()
            If Not TitulosDelHijo.Contains(texto) Then TitulosDelHijo.Add(texto)
            PostMessage(hWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero)
            CerradosDelHijo = True
            Return True
        End Function

        ''' <summary>Clic de verdad sobre un control, con los mensajes del mouse.</summary>
        '''
        ''' La tarjeta del catalogo es un Panel con etiquetas y no tiene boton, y
        ''' ConectarTarjeta engancha el agregar al evento Click del Panel. Un Panel no
        ''' tiene PerformClick (eso es solo de Button), asi que la unica forma de
        ''' disparar el manejador sin usar reflexion es mandarle el clic de verdad.
        Sub Clic(control As Control)
            Dim h = control.Handle
            Dim lParam = New IntPtr((4 << 16) Or 4)
            SendMessage(h, WM_LBUTTONDOWN, New IntPtr(1), lParam)
            SendMessage(h, WM_LBUTTONUP, IntPtr.Zero, lParam)
            Forzar(control)
        End Sub

    End Module

End Namespace
