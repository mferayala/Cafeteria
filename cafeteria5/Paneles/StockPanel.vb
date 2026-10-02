Imports System.Drawing
Imports System.Linq
Imports System.Windows.Forms

Namespace CafeteriaOS

    ''' <summary>
    ''' Modulo de stock. Muestra el catalogo con el stock real y permite corregirlo a mano.
    ''' El ajuste es la unica operacion ademas de confirmar y cancelar pedidos que puede
    ''' modificar el stock, y por eso pasa siempre por StockService.
    ''' </summary>
    Public Class StockPanel
        Inherits PanelBase

        Private WithEvents txtBuscar As TextBox
        Private WithEvents chkSoloCriticos As CheckBox
        Private WithEvents tabla As DataGridView
        Private WithEvents tablaIngresos As DataGridView
        Private lblDetalle As Label
        Private productos As New System.Collections.Generic.List(Of Producto)()

        Public Sub New()
            MyBase.New("Stock", "Control de existencias del catalogo")
            ArmarLayout()
        End Sub

        Private Sub ArmarLayout()
            Dim raiz As New TableLayoutPanel With {
                .Name = "th.fondo",
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 4,
                .BackColor = Tema.BgPrincipal
            }
            raiz.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            raiz.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            raiz.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            raiz.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            raiz.RowStyles.Add(New RowStyle(SizeType.Percent, 42))

            txtBuscar = Tema.CrearInput()
            txtBuscar.Dock = DockStyle.Fill
            txtBuscar.AccessibleName = "Buscar producto"
            chkSoloCriticos = Tema.CrearCheck("Solo stock critico o agotado")

            Dim filtros As New TableLayoutPanel With {
                .Name = "th.fondo",
                .Dock = DockStyle.Fill,
                .ColumnCount = 2,
                .RowCount = 1,
                .BackColor = Color.Transparent,
                .Height = 34
            }
            filtros.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            filtros.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
            filtros.Controls.Add(txtBuscar, 0, 0)
            filtros.Controls.Add(chkSoloCriticos, 1, 0)
            raiz.Controls.Add(filtros, 0, 0)

            tabla = Tema.CrearTabla()
            tabla.Anchor = AnchorStyles.Top
            tabla.Dock = DockStyle.Fill
            UiKit.Columna(tabla, "ID", 50)
            UiKit.Columna(tabla, "Producto", 240)
            UiKit.Columna(tabla, "Categoria", 130)
            UiKit.Columna(tabla, "Precio", 100)
            UiKit.Columna(tabla, "Stock", 80)
            UiKit.Columna(tabla, "Estado", 130)
            tabla.Columns(0).FillWeight = 8
            tabla.Columns(1).FillWeight = 40
            tabla.Columns(2).FillWeight = 20
            tabla.Columns(3).FillWeight = 16
            tabla.Columns(4).FillWeight = 12
            tabla.Columns(5).FillWeight = 20
            raiz.Controls.Add(tabla, 0, 1)

            Dim btnMenos = Tema.CrearBoton("-1", EstiloBoton.Neutro, 70, 36)
            Dim btnMas = Tema.CrearBoton("+1", EstiloBoton.Neutro, 70, 36)
            Dim btnDiez = Tema.CrearBoton("+10", EstiloBoton.Info, 70, 36)
            Dim btnReponer = Tema.CrearBoton("Reponer stock", EstiloBoton.Exito, 160, 36)
            AddHandler btnMenos.Click, Sub(s, e) Ajustar(-1)
            AddHandler btnMas.Click, Sub(s, e) Ajustar(1)
            AddHandler btnDiez.Click, Sub(s, e) Ajustar(10)
            AddHandler btnReponer.Click, Sub(s, e) Reponer()

            raiz.Controls.Add(UiKit.Botonera(btnMenos, btnMas, btnDiez, btnReponer), 0, 2)
            raiz.Controls.Add(ArmarDetalle(), 0, 3)
            Contenido.Controls.Add(raiz)
        End Sub

        ''' <summary>
        ''' Panel de detalle: de que proveedor y en que fecha entro cada unidad del
        ''' producto elegido. Es el historial que permite ubicar el origen de una
        ''' mercaderia que se danio o vencio.
        ''' </summary>
        Private Function ArmarDetalle() As Control
            Dim tarjeta As New Panel With {
                .Name = "th.tarjeta",
                .Dock = DockStyle.Fill,
                .BackColor = Tema.Superficie,
                .Padding = New Padding(12, 10, 12, 10)
            }

            Dim layout As New TableLayoutPanel With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 2,
                .BackColor = Color.Transparent
            }
            layout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            layout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            layout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

            lblDetalle = Tema.CrearLabel("", Tono.Secundario, Tema.TamMini)
            lblDetalle.Dock = DockStyle.Fill
            lblDetalle.AutoSize = False
            lblDetalle.Height = 18
            lblDetalle.TextAlign = ContentAlignment.MiddleLeft

            tablaIngresos = Tema.CrearTabla()
            tablaIngresos.Dock = DockStyle.Fill
            tablaIngresos.ReadOnly = True
            tablaIngresos.AllowUserToAddRows = False
            tablaIngresos.AllowUserToDeleteRows = False
            tablaIngresos.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            UiKit.Columna(tablaIngresos, "Fecha de ingreso", 130)
            UiKit.Columna(tablaIngresos, "Proveedor", 230)
            UiKit.Columna(tablaIngresos, "Contacto", 160)
            UiKit.Columna(tablaIngresos, "Cantidad", 90)
            UiKit.Columna(tablaIngresos, "Sin consumir", 110)
            UiKit.Columna(tablaIngresos, "Como llego", 260)
            tablaIngresos.Columns(1).FillWeight = 40
            tablaIngresos.Columns(2).FillWeight = 24
            tablaIngresos.Columns(3).FillWeight = 8
            tablaIngresos.Columns(4).FillWeight = 8
            tablaIngresos.Columns(5).FillWeight = 40

            layout.Controls.Add(lblDetalle, 0, 0)
            layout.Controls.Add(tablaIngresos, 0, 1)
            tarjeta.Controls.Add(layout)
            Return tarjeta
        End Function

        ''' <summary>
        ''' Muestra de que proveedor y en que fecha entro cada unidad del producto
        ''' elegido, del ingreso mas nuevo al mas viejo.
        ''' </summary>
        Private Sub MostrarDetalle()
            tablaIngresos.Rows.Clear()

            Dim producto = ProductoSeleccionado()
            If producto Is Nothing Then
                lblDetalle.Text = "Elegi un producto para ver de que proveedor ingreso cada unidad."
                Return
            End If

            Dim ingresos = IngresoService.PorProducto(producto)
            If ingresos.Count = 0 Then
                lblDetalle.Text = producto.Nombre & ": sin ingresos de proveedores. " &
                                  "El stock se esta ajustando a mano."
                Return
            End If

            Dim pendientes = IngresoService.PendientesPorVencer(producto)
            Dim restante As Integer
            For Each lote In pendientes
                restante += lote.Cantidad
            Next

            Dim ultimo = ingresos(0)
            lblDetalle.Text = producto.Nombre & ": " & ingresos.Count & " ingreso(s). " &
                              "El ultimo sumo " & ultimo.CantidadDe(producto) & " unidad(es), " &
                              IngresoService.Describir(ultimo) & ". " &
                              "Sin consumir: " & restante & " de " & producto.Stock & "."

            For Each ingreso In ingresos
                Dim cantidad = ingreso.CantidadDe(producto)
                Dim sinConsumir = pendientes.
                    Where(Function(l) ReferenceEquals(l.Ingreso, ingreso)).
                    Sum(Function(l) l.Cantidad)
                Dim linea = ingreso.LineaDe(producto)
                Dim observacion = If(linea Is Nothing, Nothing, linea.Observacion)
                Dim indice = tablaIngresos.Rows.Add(ingreso.FechaCorta(),
                                                    If(ingreso.Proveedor Is Nothing, "", ingreso.Proveedor.Nombre),
                                                    If(ingreso.Proveedor Is Nothing, "",
                                                       ingreso.Proveedor.ContactoPrincipal),
                                                    cantidad.ToString(),
                                                    sinConsumir.ToString(),
                                                    If(observacion, ""))
                tablaIngresos.Rows(indice).DefaultCellStyle.ForeColor =
                    If(sinConsumir = 0, Tema.TextoSec, Tema.TextoPrinc)
                ' La mercaderia se acepta igual, pero queda a la vista que llego
                ' con un detalle para no perder el dato.
                If String.IsNullOrWhiteSpace(observacion) Then
                    tablaIngresos.Rows(indice).Cells(5).Style.ForeColor = Tema.Naranja
                    tablaIngresos.Rows(indice).Cells(5).Style.Font =
                        Tema.Fuente(Tema.TamMini, FontStyle.Bold)
                End If
            Next
        End Sub

        Private Sub tabla_SelectionChanged(sender As Object, e As EventArgs) Handles tabla.SelectionChanged
            MostrarDetalle()
        End Sub

        Public Overrides Sub Refrescar()
            Dim texto = If(txtBuscar.Text, String.Empty).Trim().ToLowerInvariant()

            productos = Datos.ListaProductos.
                Where(Function(p) texto.Length = 0 OrElse
                               p.Nombre.ToLowerInvariant().Contains(texto) OrElse
                               p.Categoria.ToLowerInvariant().Contains(texto)).
                Where(Function(p) Not chkSoloCriticos.Checked OrElse
                               p.Stock <= StockService.UmbralCritico).
                OrderBy(Function(p) p.Stock).
                ThenBy(Function(p) p.Nombre).
                ToList()

            tabla.Rows.Clear()
            For Each p In productos
                Dim estado = If(p.Stock <= 0, "Agotado",
                           If(p.Stock <= StockService.UmbralCritico, "Critico", "Disponible"))
                Dim indice = tabla.Rows.Add(p.ID, p.Nombre, p.Categoria,
                                            p.Precio.ToString("C2"), p.Stock, estado)
                tabla.Rows(indice).Tag = p
                tabla.Rows(indice).DefaultCellStyle.ForeColor =
                    If(p.Stock <= 0, Tema.Rojo,
                       If(p.Stock <= StockService.UmbralCritico, Tema.Naranja, Tema.TextoPrinc))
            Next

            Dim criticos = StockService.Criticos().Count
            SetearSubtitulo(criticos & " producto(s) en stock critico")
            MostrarDetalle()
        End Sub

        Private Sub Ajustar(delta As Integer)
            Dim producto = ProductoSeleccionado()
            If producto Is Nothing Then
                MessageBox.Show("Elegi un producto de la lista.", "Stock",
                                MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If
            StockService.Ajustar(producto, delta)
            Refrescar()
        End Sub

        Private Sub Reponer()
            Dim producto = ProductoSeleccionado()
            If producto Is Nothing Then
                MessageBox.Show("Elegi un producto de la lista.", "Stock",
                                MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            ' El boton suma las unidades que se escriban, asi que el dialogo propone
            ' justamente las que faltan para llegar al umbral y no un numero fijo.
            Dim faltante = StockService.UmbralCritico - producto.Stock
            If faltante < 1 Then
                MessageBox.Show(producto.Nombre & " ya tiene " & producto.Stock &
                                " unidades, no hay nada critico para reponer.",
                                "Reponer stock", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim cantidad As Integer
            Dim escrito = InputBox("Cuantas unidades de " & producto.Nombre & " vas a reponer?" &
                                   Environment.NewLine & "Stock actual: " & producto.Stock &
                                   Environment.NewLine & "Minimo: " & StockService.UmbralCritico,
                                   "Reponer stock", faltante.ToString())
            If Not Integer.TryParse(escrito, cantidad) OrElse cantidad <= 0 Then Return

            StockService.Ajustar(producto, cantidad)
            Refrescar()
        End Sub

        Private Function ProductoSeleccionado() As Producto
            If tabla.SelectedRows.Count = 0 Then Return Nothing
            Return TryCast(tabla.SelectedRows(0).Tag, Producto)
        End Function

        Private Sub txtBuscar_TextChanged(sender As Object, e As EventArgs) Handles txtBuscar.TextChanged
            If tabla Is Nothing Then Return
            Refrescar()
        End Sub

        Private Sub chkSoloCriticos_CheckedChanged(sender As Object, e As EventArgs) Handles chkSoloCriticos.CheckedChanged
            If tabla Is Nothing Then Return
            Refrescar()
        End Sub
    End Class

End Namespace