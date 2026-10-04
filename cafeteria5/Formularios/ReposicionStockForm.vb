Imports System.Drawing
Imports System.Windows.Forms

Namespace CafeteriaOS

    Partial Class ReposicionStockForm
        Inherits Form

        Private producto As Producto
        Private txtCodigo As TextBox
        Private txtCodigoBarra As TextBox
        Private txtNombre As TextBox
        Private txtMarca As TextBox
        Private txtUnidad As TextBox
        Private txtCategoria As TextBox
        Private txtPrecioCosto As TextBox
        Private txtPrecioVenta As TextBox
        Private txtStockActual As TextBox
        Private txtStockMinimo As TextBox
        Private txtCantidad As NumericUpDown
        Private txtStockResultante As TextBox
        Private txtObservacion As TextBox
        Private lblUltimo As Label
        Private WithEvents btnConfirmar As Button
        Private WithEvents btnCancelar As Button

        Public Sub New(prod As Producto)
            producto = prod
            ArmarInterfaz()
            CargarDatos()
        End Sub

        Public Shared Function Reponer(prod As Producto) As Boolean
            If prod Is Nothing Then
                MessageBox.Show("Elegi un producto.", "Reposición de stock", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return False
            End If
            Using f As New ReposicionStockForm(prod)
                Return f.ShowDialog() = DialogResult.OK
            End Using
        End Function

        Private Sub ArmarInterfaz()
            Text = "Reposición de stock"
            StartPosition = FormStartPosition.CenterParent
            FormBorderStyle = FormBorderStyle.FixedDialog
            ClientSize = New Size(640, 420)
            BackColor = Tema.BgPrincipal
            Font = Tema.Fuente(Tema.TamNormal)

            Dim raiz As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 2, .RowCount = 13, .BackColor = Tema.BgPrincipal, .Padding = New Padding(16)}
            raiz.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 140))
            raiz.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))

            txtCodigo = New TextBox With {.Dock = DockStyle.Fill, .ReadOnly = True}
            txtCodigoBarra = New TextBox With {.Dock = DockStyle.Fill, .ReadOnly = True}
            txtNombre = New TextBox With {.Dock = DockStyle.Fill, .ReadOnly = True}
            txtMarca = New TextBox With {.Dock = DockStyle.Fill, .ReadOnly = True}
            txtUnidad = New TextBox With {.Dock = DockStyle.Fill, .ReadOnly = True}
            txtCategoria = New TextBox With {.Dock = DockStyle.Fill, .ReadOnly = True}
            txtPrecioCosto = New TextBox With {.Dock = DockStyle.Fill, .ReadOnly = True}
            txtPrecioVenta = New TextBox With {.Dock = DockStyle.Fill, .ReadOnly = True}
            txtStockActual = New TextBox With {.Dock = DockStyle.Fill, .ReadOnly = True}
            txtStockMinimo = New TextBox With {.Dock = DockStyle.Fill, .ReadOnly = True}

            AddRow(raiz, 0, "Código", txtCodigo)
            AddRow(raiz, 1, "Código de barras", txtCodigoBarra)
            AddRow(raiz, 2, "Nombre", txtNombre)
            AddRow(raiz, 3, "Marca", txtMarca)
            AddRow(raiz, 4, "Unidad medida", txtUnidad)
            AddRow(raiz, 5, "Categoría", txtCategoria)
            AddRow(raiz, 6, "Precio costo", txtPrecioCosto)
            AddRow(raiz, 7, "Precio venta", txtPrecioVenta)
            AddRow(raiz, 8, "Stock actual", txtStockActual)
            AddRow(raiz, 9, "Stock mínimo", txtStockMinimo)

            Dim lblRep As New Label With {.Text = "REPOSICIÓN", .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.MiddleLeft, .Font = New Font(FontFamily.GenericSansSerif, 9, FontStyle.Bold)}
            raiz.Controls.Add(lblRep, 0, 10)
            raiz.SetColumnSpan(lblRep, 2)

            txtCantidad = New NumericUpDown With {.Minimum = 1, .Maximum = 100000, .Value = 1, .Dock = DockStyle.Fill}
            AddHandler txtCantidad.ValueChanged, Sub() ActualizarResultante()
            AddRow(raiz, 11, "Cantidad a reponer", txtCantidad)

            txtStockResultante = New TextBox With {.Dock = DockStyle.Fill, .ReadOnly = True}
            AddRow(raiz, 12, "Stock resultante", txtStockResultante)

            txtObservacion = New TextBox With {.Dock = DockStyle.Fill, .Multiline = True, .Height = 50}
            AddRow(raiz, 13, "Observación", txtObservacion)

            lblUltimo = New Label With {.Dock = DockStyle.Fill, .TextAlign = ContentAlignment.MiddleLeft, .ForeColor = Tema.TextoSec, .Font = Tema.Fuente(Tema.TamMini)}
            AddRow(raiz, 14, "Último ingreso", lblUltimo)

            btnConfirmar = Tema.CrearBoton("Reponer stock", EstiloBoton.Exito, 150, 36)
            btnCancelar = Tema.CrearBoton("Cancelar", EstiloBoton.Neutro, 120, 36)
            AddHandler btnConfirmar.Click, Sub() Confirmar()
            AddHandler btnCancelar.Click, Sub() Cancelar()
            Dim acciones As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .FlowDirection = FlowDirection.RightToLeft, .WrapContents = False, .BackColor = Color.Transparent}
            acciones.Controls.Add(btnCancelar)
            acciones.Controls.Add(btnConfirmar)
            raiz.Controls.Add(acciones, 0, 15)
            raiz.SetColumnSpan(acciones, 2)

            Controls.Add(raiz)
        End Sub

        Private Sub AddRow(panel As TableLayoutPanel, row As Integer, etiqueta As String, ctrl As Control)
            Dim lbl As New Label With {.Text = etiqueta, .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.MiddleLeft}
            panel.Controls.Add(lbl, 0, row)
            panel.Controls.Add(ctrl, 1, row)
        End Sub

        Private Sub CargarDatos()
            If producto Is Nothing Then Return
            txtCodigo.Text = producto.ID.ToString()
            txtCodigoBarra.Text = If(producto.CodigoBarra, "")
            txtNombre.Text = producto.Nombre
            txtMarca.Text = If(producto.Marca, "")
            txtUnidad.Text = If(producto.UnidadMedida, "un")
            txtCategoria.Text = If(producto.Categoria, "")
            txtPrecioCosto.Text = producto.PrecioCosto.ToString("N2")
            txtPrecioVenta.Text = producto.Precio.ToString("N2")
            txtStockActual.Text = producto.Stock.ToString()
            txtStockMinimo.Text = "0"
            ActualizarResultante()
        End Sub

        Private Sub ActualizarResultante()
            txtStockResultante.Text = (producto.Stock + CInt(txtCantidad.Value)).ToString()
        End Sub

        Private Sub Confirmar()
            Dim cant = CInt(txtCantidad.Value)
            If cant <= 0 Then
                MessageBox.Show("La cantidad a reponer tiene que ser mayor a cero.",
                                "Reposición de stock", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim nota = txtObservacion.Text.Trim()
            ' Un solo asiento: Ajustar recibe el tipo Reposicion y deja el movimiento.
            StockService.Ajustar(producto, cant, TipoMovimientoStock.Reposicion,
                                 Datos.UsuarioActualNombre(), nota)

            DialogResult = DialogResult.OK
            Close()
        End Sub

        Private Sub Cancelar()
            DialogResult = DialogResult.Cancel
            Close()
        End Sub

    End Class

End Namespace
