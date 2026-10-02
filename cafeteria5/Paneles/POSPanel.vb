Imports System.Collections.Generic
Imports System.Drawing
Imports System.Globalization
Imports System.Linq
Imports System.Windows.Forms

Namespace CafeteriaOS

    ''' <summary>
    ''' Punto de venta. Arma el pedido, valida contra el stock disponible y lo registra
    ''' cobrando. El stock se descuenta en un solo momento, al registrar: armar o limpiar
    ''' el pedido no lo modifica, y por eso limpiar no genera diferencias de stock.
    '''
    ''' Las tarjetas de producto conectan un unico manejador por control, incluido cada
    ''' control hijo. Asi un clic se procesa una sola vez, en lugar de llegar dos veces
    ''' por propagacion entre la etiqueta y su contenedor.
    ''' </summary>
    Public Class POSPanel
        Inherits PanelBase

        Private pedidoEnCurso As Pedido
        Private mapaAtajos As Dictionary(Of Keys, Producto)

        Private WithEvents txtBuscar As TextBox
        Private WithEvents cmbCategoria As ComboBox
        Private flowProductos As FlowLayoutPanel
        Private WithEvents cmbServicio As ComboBox
        Private WithEvents txtMesa As TextBox
        Private WithEvents txtCliente As TextBox
        Private WithEvents txtTelefono As TextBox
        Private WithEvents numDescuento As NumericUpDown
        Private WithEvents lstItems As ListView
        Private lblSubtotal As Label
        Private lblTotal As Label
        Private lblStockBajo As Label

        Public Sub New()
            MyBase.New("Punto de venta", "Armar el pedido y cobrarlo")
            ArmarLayout()
            NuevoPedido()
        End Sub

        #Region "Layout"

        Private Sub ArmarLayout()
            ' El POS se reparte con dos columnas en porcentaje en lugar de un
            ' SplitContainer. El divisor obliga a fijar tamanos minimos (380 + 340 + 8)
            ' y si la ventana queda mas angosta que esa suma, WinForms recalcula el
            ' SplitterDistance y tira InvalidOperationException al abrir el modulo.
            ' Con porcentajes el panel se acomoda solo al ancho disponible, que es lo
            ' que pide la especificacion para el Dashboard maximizado o restaurado.
            Dim raiz As New TableLayoutPanel With {
                .Name = "th.fondo",
                .Dock = DockStyle.Fill,
                .ColumnCount = 2,
                .RowCount = 1,
                .BackColor = Tema.BgPrincipal
            }
            raiz.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 58))
            raiz.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 42))
            raiz.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

            raiz.Controls.Add(ArmarCatalogo(), 0, 0)
            raiz.Controls.Add(ArmarComprobante(), 1, 0)
            Contenido.Controls.Add(raiz)

            ' Las tarjetas del catalogo se reacomodan cada vez que cambia el ancho.
            AddHandler flowProductos.SizeChanged, Sub(s, e) AjustarAnchoCatalogo()
            AjustarAnchoCatalogo()
        End Sub

        Private Function ArmarCatalogo() As Control
            Dim raiz As New TableLayoutPanel With {
                .Name = "th.fondo",
                .Dock = DockStyle.Fill,
                .ColumnCount = 2,
                .RowCount = 2,
                .BackColor = Tema.BgPrincipal
            }
            raiz.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            raiz.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
            raiz.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            raiz.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

            txtBuscar = Tema.CrearInput()
            txtBuscar.Dock = DockStyle.Fill
            txtBuscar.AccessibleName = "Buscar producto"

            cmbCategoria = Tema.CrearCombo()
            cmbCategoria.Width = 160
            cmbCategoria.Dock = DockStyle.Fill

            raiz.Controls.Add(txtBuscar, 0, 0)
            raiz.Controls.Add(cmbCategoria, 1, 0)

            flowProductos = New FlowLayoutPanel With {
                .Name = "th.fondo",
                .Dock = DockStyle.Fill,
                .AutoScroll = True,
                .WrapContents = True,
                .FlowDirection = FlowDirection.LeftToRight,
                .BackColor = Tema.BgPrincipal,
                .Padding = New Padding(0, 8, 0, 0)
            }
            raiz.Controls.Add(flowProductos, 0, 1)
            raiz.SetColumnSpan(flowProductos, 2)
            Return raiz
        End Function

        Private Function ArmarComprobante() As Control
            Dim raiz As New TableLayoutPanel With {
                .Name = "th.tarjeta",
                .Dock = DockStyle.Fill,
                .ColumnCount = 2,
                .RowCount = 10,
                .BackColor = Tema.Superficie,
                .Padding = New Padding(14)
            }
            raiz.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
            raiz.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            For i = 0 To 9
                raiz.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            Next
            raiz.RowStyles(5) = New RowStyle(SizeType.Percent, 100)

            cmbServicio = Tema.CrearCombo()
            cmbServicio.Dock = DockStyle.Fill
            cmbServicio.Items.AddRange([Enum].GetNames(GetType(TipoServicio)))
            cmbServicio.SelectedIndex = 0

            txtMesa = Tema.CrearInput()
            txtMesa.Dock = DockStyle.Fill
            txtMesa.MaxLength = 12
            txtMesa.AccessibleName = "Mesa"

            txtCliente = Tema.CrearInput()
            txtCliente.Dock = DockStyle.Fill
            txtCliente.AccessibleName = "Cliente"

            txtTelefono = Tema.CrearInput()
            txtTelefono.Dock = DockStyle.Fill
            txtTelefono.MaxLength = 20
            txtTelefono.AccessibleName = "Telefono"

            numDescuento = New NumericUpDown With {
                .Name = "th.campo",
                .Dock = DockStyle.Fill,
                .DecimalPlaces = 0,
                .Minimum = 0D,
                .Maximum = 1000000D,
                .Increment = 100D,
                .Font = Tema.Fuente(Tema.TamNormal),
                .BackColor = Tema.Superficie2,
                .ForeColor = Tema.TextoPrinc,
                .TextAlign = HorizontalAlignment.Right,
                .ThousandsSeparator = True
            }

            raiz.Controls.Add(Rotulo("Servicio"), 0, 0)
            raiz.Controls.Add(cmbServicio, 1, 0)
            raiz.Controls.Add(Rotulo("Mesa"), 0, 1)
            raiz.Controls.Add(txtMesa, 1, 1)
            raiz.Controls.Add(Rotulo("Cliente"), 0, 2)
            raiz.Controls.Add(txtCliente, 1, 2)
            raiz.Controls.Add(Rotulo("Telefono"), 0, 3)
            raiz.Controls.Add(txtTelefono, 1, 3)
            raiz.Controls.Add(Rotulo("Descuento"), 0, 4)
            raiz.Controls.Add(numDescuento, 1, 4)

            lstItems = New ListView With {
                .Name = "th.lista",
                .Dock = DockStyle.Fill,
                .View = View.Details,
                .FullRowSelect = True,
                .MultiSelect = False,
                .HideSelection = False,
                .HeaderStyle = ColumnHeaderStyle.Nonclickable,
                .BorderStyle = BorderStyle.None,
                .BackColor = Tema.Superficie2,
                .ForeColor = Tema.TextoPrinc,
                .Font = Tema.Fuente(Tema.TamMini)
            }
            lstItems.Columns.Add("Producto", 190)
            lstItems.Columns.Add("Cant.", 48)
            lstItems.Columns.Add("Precio", 78)
            lstItems.Columns.Add("Subtotal", 90)

            Dim btnMas = Tema.CrearBoton("+", EstiloBoton.Neutro, 44, 34)
            Dim btnMenos = Tema.CrearBoton("-", EstiloBoton.Neutro, 44, 34)
            Dim btnQuitar = Tema.CrearBoton("Quitar", EstiloBoton.Peligro, 90, 34)
            AddHandler btnMas.Click, Sub(s, e) CambiarCantidad(1)
            AddHandler btnMenos.Click, Sub(s, e) CambiarCantidad(-1)
            AddHandler btnQuitar.Click, Sub(s, e) QuitarItem()

            Dim panelItems As New TableLayoutPanel With {
                .Name = "th.fondo",
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 2,
                .BackColor = Color.Transparent
            }
            panelItems.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            panelItems.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            panelItems.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            panelItems.Controls.Add(lstItems, 0, 0)
            panelItems.Controls.Add(UiKit.Botonera(btnMas, btnMenos, btnQuitar), 0, 1)
            raiz.Controls.Add(panelItems, 0, 5)
            raiz.SetColumnSpan(panelItems, 2)

            lblSubtotal = TotalLabel("Subtotal")
            lblTotal = TotalLabel("TOTAL")
            lblTotal.Font = Tema.Fuente(Tema.TamSeccion, FontStyle.Bold)
            raiz.Controls.Add(Rotulo("Subtotal"), 0, 6)
            raiz.Controls.Add(lblSubtotal, 1, 6)
            raiz.Controls.Add(Rotulo("Total"), 0, 7)
            raiz.Controls.Add(lblTotal, 1, 7)

            Dim btnLimpiar = Tema.CrearBoton("Limpiar", EstiloBoton.Neutro, 110, 40)
            Dim btnRegistrar = Tema.CrearBoton("Registrar y cobrar", EstiloBoton.Exito, 190, 40)
            AddHandler btnLimpiar.Click, Sub(s, e) NuevoPedido()
            AddHandler btnRegistrar.Click, Sub(s, e) RegistrarPedido()
            Dim barra = UiKit.Botonera(btnLimpiar, btnRegistrar)
            raiz.Controls.Add(barra, 0, 8)
            raiz.SetColumnSpan(barra, 2)

            lblStockBajo = Tema.CrearLabel("", Tono.Peligro, Tema.TamMini, FontStyle.Bold)
            lblStockBajo.Dock = DockStyle.Fill
            lblStockBajo.Height = 18
            raiz.Controls.Add(lblStockBajo, 0, 9)
            raiz.SetColumnSpan(lblStockBajo, 2)
            Return raiz
        End Function

        Private Shared Function Rotulo(texto As String) As Label
            Dim l As Label = Tema.CrearLabel(texto, Tono.Secundario, Tema.TamMini, FontStyle.Bold)
            l.Dock = DockStyle.Fill
            l.AutoSize = False
            l.Width = 100
            l.TextAlign = ContentAlignment.MiddleLeft
            l.Margin = New Padding(0, 3, 10, 3)
            Return l
        End Function

        Private Shared Function TotalLabel(prefijo As String) As Label
            Dim l As Label = Tema.CrearLabel(prefijo & ": --", Tono.Primario, Tema.TamNormal, FontStyle.Bold)
            l.Dock = DockStyle.Fill
            l.AutoSize = False
            l.Height = 24
            l.TextAlign = ContentAlignment.MiddleRight
            Return l
        End Function

        Private Sub AjustarAnchoCatalogo()
            If flowProductos.ClientSize.Width <= 0 Then Return
            Dim ancho = flowProductos.ClientSize.Width - 4
            Dim columnas = Math.Max(1, ancho \ 180)
            Dim card = Math.Max(150, (ancho - 10 * (columnas - 1)) \ columnas)
            For Each c As Control In flowProductos.Controls
                c.Width = card
            Next
        End Sub

        #End Region

        #Region "Catalogo"

        Private Sub txtBuscar_TextChanged(sender As Object, e As EventArgs) Handles txtBuscar.TextChanged
            If flowProductos Is Nothing Then Return
            PintarCatalogo()
        End Sub

        Private Sub cmbCategoria_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbCategoria.SelectedIndexChanged
            If flowProductos Is Nothing Then Return
            PintarCatalogo()
        End Sub

        Public Overrides Sub Refrescar()
            mapaAtajos = Atajos.Mapa()
            CargarCategorias()
            PintarCatalogo()
            PintarItems()
        End Sub

        Private Sub CargarCategorias()
            Dim actual = If(cmbCategoria.SelectedItem?.ToString(), "Todos")
            cmbCategoria.Items.Clear()
            cmbCategoria.Items.AddRange(DatosIniciales.CategoriasDeProductos().ToArray())
            Dim indice = cmbCategoria.Items.IndexOf(actual)
            cmbCategoria.SelectedIndex = If(indice >= 0, indice, 0)
        End Sub

        Private Sub PintarCatalogo()
            Dim texto = If(txtBuscar.Text, String.Empty).Trim().ToLowerInvariant()
            Dim categoria = If(cmbCategoria.SelectedItem?.ToString(), "Todos")

            flowProductos.SuspendLayout()
            While flowProductos.Controls.Count > 0
                flowProductos.Controls(0).Dispose()
            End While

            For Each producto In Datos.ListaProductos
                Dim coincideCategoria = categoria = "Todos" OrElse producto.Categoria = categoria
                Dim coincideTexto = texto.Length = 0 OrElse
                                     producto.Nombre.ToLowerInvariant().Contains(texto) OrElse
                                     producto.Descripcion.ToLowerInvariant().Contains(texto)
                If coincideCategoria AndAlso coincideTexto Then
                    flowProductos.Controls.Add(ArmarTarjeta(producto))
                End If
            Next

            flowProductos.ResumeLayout()
            AjustarAnchoCatalogo()
        End Sub

        Private Function ArmarTarjeta(producto As Producto) As Panel
            Dim disponible = StockService.Disponible(pedidoEnCurso.Items, producto)
            Dim tonoStock As Tono = If(disponible <= 0, Tono.Peligro,
                                        If(disponible <= StockService.UmbralCritico, Tono.Atencion, Tono.Exito))

            Dim tarjeta As New Panel With {
                .Name = "th.tarjeta",
                .Height = 104,
                .BackColor = Tema.Superficie,
                .Cursor = Cursors.Hand,
                .Margin = New Padding(0, 0, 10, 10),
                .Padding = New Padding(8),
                .Tag = producto
            }

            Dim lblNombre = New Label With {
                .Name = "th.tono.Primario",
                .Text = producto.Nombre,
                .Font = Tema.Fuente(Tema.TamMini, FontStyle.Bold),
                .ForeColor = Tema.TextoPrinc,
                .BackColor = Color.Transparent,
                .Dock = DockStyle.Top,
                .Height = 32,
                .AutoEllipsis = True
            }

            Dim lblPrecio = New Label With {
                .Name = "th.tono.Acento",
                .Text = If(producto.TienePrecio(), producto.Precio.ToString("C2", CultureInfo.CurrentCulture), "Sin precio"),
                .Font = Tema.Fuente(Tema.TamNormal, FontStyle.Bold),
                .ForeColor = Tema.Acento,
                .BackColor = Color.Transparent,
                .Dock = DockStyle.Top,
                .Height = 20,
                .TextAlign = ContentAlignment.MiddleLeft
            }

            Dim lblStock = New Label With {
                .Name = "th.tono." & tonoStock.ToString(),
                .Text = If(disponible <= 0, "Sin stock", "Stock: " & disponible),
                .Font = Tema.Fuente(Tema.TamMicro),
                .ForeColor = Tema.ColorDeTono(tonoStock),
                .BackColor = Color.Transparent,
                .Dock = DockStyle.Bottom,
                .Height = 16
            }

            Dim lblAtajo = New Label With {
                .Name = "th.tono.Terciario",
                .Text = If(String.IsNullOrWhiteSpace(producto.Atajo), producto.Categoria, producto.Atajo),
                .Font = Tema.Fuente(Tema.TamMicro),
                .ForeColor = Tema.TextoTer,
                .BackColor = Color.Transparent,
                .Dock = DockStyle.Fill,
                .TextAlign = ContentAlignment.BottomLeft
            }

            tarjeta.Controls.AddRange(New Control() {lblAtajo, lblPrecio, lblNombre, lblStock})
            If disponible <= 0 Or Not producto.TienePrecio() Then tarjeta.Enabled = False

            ConectarTarjeta(tarjeta, producto)
            Return tarjeta
        End Function

        ''' <summary>
        ''' Un unico manejador por control. El click siempre llega a un solo control (el
        ''' que esta bajo el cursor), por lo que la accion se ejecuta una sola vez.
        ''' </summary>
        Private Sub ConectarTarjeta(control As Control, producto As Producto)
            AddHandler control.Click, Sub(s, e) AgregarProducto(producto)
            For Each hijo As Control In control.Controls
                ConectarTarjeta(hijo, producto)
            Next
        End Sub

        #End Region

        #Region "Pedido en curso"

        Private Sub NuevoPedido()
            ' Limpiar el comprobante no toca el stock: nada fue descontado todavia.
            For Each campo In New Control() {txtMesa, txtCliente, txtTelefono}
                campo.Text = String.Empty
            Next
            numDescuento.Value = 0D

            pedidoEnCurso = New Pedido With {
                .Estado = EstadoPedido.EnPreparacion,
                .TipoServicio = DirectCast([Enum].Parse(GetType(TipoServicio),
                                    If(cmbServicio.SelectedItem?.ToString(), "EnElLocal")), TipoServicio),
                .UsuarioResponsable = Datos.UsuarioActualNombre()
            }
            PintarCatalogo()
            PintarItems()
        End Sub

        Private Sub AgregarProducto(producto As Producto)
            Dim detalle = DetalleDe(producto)

            If StockService.Disponible(pedidoEnCurso.Items, producto) <= 0 Then
                AvisarStock(producto)
                Return
            End If

            Dim especificaciones As String = String.Empty
            If producto.Specs.Count > 0 Then
                Dim elegido = EspecificacionesForm.Pedir(producto)
                If elegido Is Nothing Then Return
                especificaciones = elegido
            End If

            If detalle Is Nothing Then
                pedidoEnCurso.Items.Add(New DetallePedido With {
                    .Producto = producto,
                    .Cantidad = 1,
                    .Especificaciones = especificaciones
                })
            Else
                detalle.Cantidad += 1
                If especificaciones.Length > 0 Then detalle.Especificaciones = especificaciones
            End If

            PintarCatalogo()
            PintarItems()
        End Sub

        Private Function DetalleDe(producto As Producto) As DetallePedido
            Return pedidoEnCurso.Items.FirstOrDefault(Function(i) i.Producto.ID = producto.ID)
        End Function

        Private Sub CambiarCantidad(delta As Integer)
            If lstItems.SelectedIndices.Count = 0 Then Return
            Dim indice = lstItems.SelectedIndices(0)
            If indice < 0 OrElse indice >= pedidoEnCurso.Items.Count Then Return

            Dim detalle = pedidoEnCurso.Items(indice)
            If delta > 0 Then
                If Not StockService.PuedeAgregar(pedidoEnCurso.Items, detalle.Producto, 1) Then
                    AvisarStock(detalle.Producto)
                    Return
                End If
            End If

            detalle.Cantidad += delta
            If detalle.Cantidad <= 0 Then pedidoEnCurso.Items.RemoveAt(indice)

            PintarCatalogo()
            PintarItems()
        End Sub

        Private Sub QuitarItem()
            If lstItems.SelectedIndices.Count = 0 Then Return
            Dim indice = lstItems.SelectedIndices(0)
            If indice >= 0 AndAlso indice < pedidoEnCurso.Items.Count Then
                pedidoEnCurso.Items.RemoveAt(indice)
            End If
            PintarCatalogo()
            PintarItems()
        End Sub

        Private Sub AvisarStock(producto As Producto)
            lblStockBajo.Text = "No hay stock suficiente de " & producto.Nombre & "."
            lblStockBajo.Name = "th.tono.Peligro"
        End Sub

        Private Sub PintarItems()
            lstItems.Items.Clear()
            pedidoEnCurso.Descuento = Decimal.Round(numDescuento.Value, 2)

            For Each item In pedidoEnCurso.Items
                Dim fila As New ListViewItem(item.Producto.Nombre)
                fila.SubItems.Add(item.Cantidad.ToString())
                fila.SubItems.Add(item.Producto.Precio.ToString("C2"))
                fila.SubItems.Add(item.Subtotal.ToString("C2"))
                If Not String.IsNullOrWhiteSpace(item.Especificaciones) Then
                    fila.ToolTipText = item.Especificaciones
                End If
                If Not item.Producto.TienePrecio() Then fila.ForeColor = Tema.Rojo
                lstItems.Items.Add(fila)
            Next

            lblSubtotal.Text = "Subtotal: " & pedidoEnCurso.Subtotal.ToString("C2")
            lblTotal.Text = "TOTAL: " & pedidoEnCurso.Total.ToString("C2")
            SetearSubtitulo(pedidoEnCurso.Items.Count & " item(s) - " & pedidoEnCurso.Total.ToString("C2"))
        End Sub

        Private Sub numDescuento_ValueChanged(sender As Object, e As EventArgs) Handles numDescuento.ValueChanged
            If pedidoEnCurso Is Nothing Then Return
            PintarItems()
        End Sub

        Private Sub cmbServicio_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbServicio.SelectedIndexChanged
            If pedidoEnCurso Is Nothing Then Return
            pedidoEnCurso.TipoServicio = DirectCast(
                [Enum].Parse(GetType(TipoServicio), cmbServicio.SelectedItem.ToString()), TipoServicio)
            txtMesa.Enabled = pedidoEnCurso.TipoServicio = TipoServicio.EnElLocal
            If Not txtMesa.Enabled Then txtMesa.Text = String.Empty
        End Sub

        #End Region

        #Region "Registro y cobro"

        Private Sub RegistrarPedido()
            If pedidoEnCurso.Items.Count = 0 Then
                MessageBox.Show("Agrega al menos un producto al pedido.", "Pedido",
                                MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If
            If Not AsegurarCajaAbierta() Then Return

            Dim pago = CobroForm.Pedir(pedidoEnCurso.Total)
            If pago = Nothing Then Return

            ' Unico punto donde baja el stock.
            StockService.Descontar(pedidoEnCurso)

            pedidoEnCurso.NombreCliente = txtCliente.Text.Trim()
            pedidoEnCurso.TelefonoCliente = ClienteService.NormalizarTelefono(txtTelefono.Text)
            pedidoEnCurso.MetodoPago = pago
            pedidoEnCurso.Mesa = If(pedidoEnCurso.TipoServicio = TipoServicio.EnElLocal, txtMesa.Text.Trim(), String.Empty)

            Datos.RegistrarPedido(pedidoEnCurso)
            ClienteService.RegistrarOActualizar(pedidoEnCurso.NombreCliente, pedidoEnCurso.TelefonoCliente, pedidoEnCurso)

            CajaService.RegistrarVenta(pedidoEnCurso)

            Dim registrado = pedidoEnCurso
            NuevoPedido()

            Using f As New TicketForm(TicketService.ConstruirTicket(registrado), "Ticket del pedido N. " & registrado.ID)
                f.ShowDialog()
            End Using

            lblStockBajo.Text = String.Empty
        End Sub

        #End Region

        #Region "Caja"

        ''' <summary>
        ''' No se cobra con la caja cerrada: sin caja abierta la venta no quedaria
        ''' registrada en ningun lado. Se ofrece abrirla en el momento.
        ''' </summary>
        Private Function AsegurarCajaAbierta() As Boolean
            If CajaService.EstaAbierta() Then Return True

            If MessageBox.Show("La caja esta cerrada. Abrirla ahora para poder cobrar?",
                               "Caja cerrada", MessageBoxButtons.OKCancel,
                               MessageBoxIcon.Question) <> DialogResult.OK Then
                Return False
            End If

            Dim monto As Decimal
            Dim texto = InputBox("Monto con el que se abre la caja:", "Abrir caja", "0")
            If Not Decimal.TryParse(texto, monto) OrElse monto < 0D Then
                MessageBox.Show("El monto de apertura no es valido.", "Caja",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return False
            End If

            CajaService.Abrir(monto)
            Return True
        End Function

        #End Region

        #Region "Atajos de teclado"

        ''' <summary>
        ''' Busca el atajo pulsado en el mapa. Se consulta la tecla completa (con Ctrl) y
        ''' tambien la tecla pelada, para que un atajo definido como "1" funcione igual
        ''' que uno definido como "Ctrl+1" sin tener que tener dos entradas en el producto.
        ''' </summary>
        Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
            If mapaAtajos Is Nothing OrElse mapaAtajos.Count = 0 Then
                Return MyBase.ProcessCmdKey(msg, keyData)
            End If

            Dim producto As Producto = Nothing
            If mapaAtajos.TryGetValue(keyData, producto) Then
                AgregarProducto(producto)
                Return True
            End If
            If mapaAtajos.TryGetValue(keyData And Keys.KeyCode, producto) Then
                AgregarProducto(producto)
                Return True
            End If

            Return MyBase.ProcessCmdKey(msg, keyData)
        End Function

        #End Region
    End Class

End Namespace