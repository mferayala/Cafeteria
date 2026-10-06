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
            MinimizeBox = False
            ' Sin esto WinForms calcula el escalado con los valores por defecto y no
            ' con los que el Designer escribe: a 125% o 150% la fuente crece pero el
            ' ancho de las columnas no, y "Código de barras" queda en "Código de".
            AutoScaleMode = AutoScaleMode.Dpi
            AutoScaleDimensions = New SizeF(96, 96)
            ' 16 filas de 30 + separador 40 + botonera 44 = 504px de contenido, y el
            ' alto viejo daba 388px utiles: sobraban 116px que se perdian detras de una
            ' barra de desplazamiento. Con 560 de alto entran las 16 filas enteras.
            ' Las 16 filas piden 530px (13 de 30, la observacion 56, botones 40 y
            ' pie 44) y el padding vertical se come 32. Con 560 de alto quedaban
            ' 528 utiles y faltaban 2: el formulario pedia scrollbar para dos
            ' pixeles. Con 574 entran las 16 filas y sobran 12.
            ClientSize = New Size(720, 574)
            BackColor = Tema.BgPrincipal
            Font = Tema.Fuente(Tema.TamNormal)

            ' RowCount era 13 y se usan filas hasta la 15, asi que las ultimas se
            ' quedaban con el alto por defecto y el aviso del ultimo ingreso salia
            ' cortado. Se declara el alto de cada fila.
            Dim raiz As New TableLayoutPanel With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 2,
                .RowCount = 16,
                .BackColor = Tema.BgPrincipal,
                .Padding = New Padding(16)
            }
            ' La columna de etiqueta es lo bastante ancha para el texto mas largo
            ' ("Código de barras", "Cantidad a reponer") con la fuente del tema. Con
            ' 130 quedaban cortadas y con elipsis abajo.
            raiz.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 175))
            raiz.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            For i = 0 To 13
                raiz.RowStyles.Add(New RowStyle(SizeType.Absolute, 30))
            Next
            raiz.RowStyles.Add(New RowStyle(SizeType.Absolute, 40))
            raiz.RowStyles.Add(New RowStyle(SizeType.Absolute, 44))
            ' La observacion es el unico campo de varias lineas: necesita 56px de
            ' alto para las tres lineas que se ven, y su fila media 30. Por ahi
            ' aparecia cortado por abajo.
            raiz.RowStyles(13) = New RowStyle(SizeType.Absolute, 56)

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

            txtObservacion = New TextBox With {.Dock = DockStyle.Fill, .Multiline = True, .ScrollBars = ScrollBars.None}
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
            txtStockMinimo.Text = StockService.MinimoDe(producto).ToString()

            ' Se propone justamente lo que falta para llegar al minimo. Si el
            ' producto ya esta por encima, se propone 1 y no un numero inventado.
            Dim faltante = StockService.MinimoDe(producto) - producto.Stock
            txtCantidad.Value = Math.Max(1, Math.Min(faltante, 100000))
            ActualizarResultante()
        End Sub

        Private Sub ActualizarResultante()
            Dim cant = CInt(txtCantidad.Value)
            txtStockResultante.Text = (producto.Stock + cant).ToString()

            Dim minimo = StockService.MinimoDe(producto)
            Dim faltan = minimo - producto.Stock

            ' En dos lineas: en una sola no entra en el ancho de la columna y
            ' la geometria el recorte. La segunda parte va sola para que se lea
            ' sin completar lo que se cuta.
            lblUltimo.Text = "Minimo " & minimo & ". Le faltan " & Math.Max(0, faltan) &
                             " unidad(es) para llegar al minimo." & Environment.NewLine &
                             "Ultimo ingreso: " & UltimoIngreso()
            lblUltimo.ForeColor = If(faltan <= 0, Tema.TextoSec, Tema.Naranja)
        End Sub

        ''' <summary>Como llego la ultima vez, o que nunca ingreso.</summary>
        Private Function UltimoIngreso() As String
            Dim ingresos = IngresoService.PorProducto(producto)
            If ingresos.Count = 0 Then Return "todavia no tiene ingresos."
            Dim ultimo = ingresos(0)
            Return IngresoService.Describir(ultimo) & ", " &
                   ultimo.CantidadDe(producto).ToString() & " u."
        End Function

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
