Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms

Namespace CafeteriaOS

    ''' <summary>
    ''' Alta y edicion de un producto del catalogo.
    '''
    ''' Sirve para las dos cosas con un solo formulario: si se abre con Nothing arma
    ''' uno nuevo, y si se abre con un producto edita ese. Mantener una sola pantalla
    ''' evita que la carga y la edicion seンダ de distinto modo y el producto quede
    ''' con datos que el otro formulario no permite.
    '''
    ''' El precio de costo y el precio de venta estan separados a proposito: el
    ''' cliente ve lo que paga y el negocio ve lo que le cuesta. El costo nunca
    ''' aparece en el punto de venta ni en el ticket.
    ''' </summary>
    Partial Class ProductoEditorForm
        Inherits Form

        Private producto As Producto
        Private esNuevo As Boolean

        Private txtNombre As TextBox
        Private txtMarca As TextBox
        Private txtCodigoBarra As TextBox
        Private txtUnidad As TextBox
        Private txtCategoria As ComboBox
        Private txtSubcategoria As ComboBox
        Private txtDescripcion As TextBox
        Private txtCosto As TextBox
        Private txtVenta As TextBox
        Private txtStock As NumericUpDown
        Private txtStockMinimo As NumericUpDown
        Private txtAtajo As TextBox
        Private lblSugerido As Label
        Private WithEvents btnGuardar As Button
        Private WithEvents btnCancelar As Button

        ''' <summary>Abre el editor sobre un producto existente.</summary>
        Public Sub New(prod As Producto)
            producto = prod
            esNuevo = prod Is Nothing
            ArmarInterfaz()
            CargarDatos()
        End Sub

        ''' <summary>
        ''' Abre el editor y devuelve el producto guardado, o Nothing si se cancelo.
        ''' </summary>
        Public Shared Function Editar(prod As Producto) As Producto
            Using f As New ProductoEditorForm(prod)
                If f.ShowDialog() = DialogResult.OK Then Return f.producto
            End Using
            Return Nothing
        End Function

        Private Sub ArmarInterfaz()
            Text = If(esNuevo, "Nuevo producto", "Editar producto")
            StartPosition = FormStartPosition.CenterParent
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ClientSize = New Size(660, 560)
            BackColor = Tema.BgPrincipal
            Font = Tema.Fuente(Tema.TamNormal)

            Dim raiz As New TableLayoutPanel With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 3,
                .RowCount = 15,
                .BackColor = Tema.BgPrincipal,
                .Padding = New Padding(16)
            }
            raiz.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 130))
            raiz.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            raiz.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 150))

            txtNombre = Tema.CrearInput()
            txtMarca = Tema.CrearInput()
            txtCodigoBarra = Tema.CrearInput()
            txtUnidad = Tema.CrearInput()
            txtCategoria = Tema.CrearCombo()
            txtSubcategoria = Tema.CrearCombo()
            txtDescripcion = Tema.CrearInput()
            txtCosto = Tema.CrearInput()
            txtVenta = Tema.CrearInput()
            txtAtajo = Tema.CrearInput()

            ' Las categorias se escriben a mano y ademas se ofrecen las que ya
            ' existen: asi se puede cargar una nueva sin pasar por otro lado, y no
            ' hay que adivinar de antemano cuales son.
            txtCategoria.DropDownStyle = ComboBoxStyle.DropDown
            txtSubcategoria.DropDownStyle = ComboBoxStyle.DropDown
            AddHandler txtCategoria.TextChanged, Sub() CargarSugerencias()
            AddHandler txtCosto.TextChanged, Sub() ActualizarSugerido()

            AddHandler txtDescripcion.KeyDown, Sub(se, ev) GuardarConEnter(ev)

            txtStock = New NumericUpDown With {
                .Minimum = 0,
                .Maximum = 1000000,
                .Dock = DockStyle.Fill,
                .ThousandsSeparator = True
            }
            txtStockMinimo = New NumericUpDown With {
                .Minimum = 0,
                .Maximum = 1000000,
                .Dock = DockStyle.Fill,
                .ThousandsSeparator = True
            }

            AddRow(raiz, 0, "Nombre", txtNombre)
            AddRow(raiz, 1, "Marca", txtMarca)
            AddRow(raiz, 2, "Código de barras", txtCodigoBarra)
            AddRow(raiz, 3, "Descripción", txtDescripcion)
            AddRow(raiz, 4, "Unidad", txtUnidad)
            AddRow(raiz, 5, "Categoría", txtCategoria)
            AddRow(raiz, 6, "Subcategoría", txtSubcategoria)
            AddRow(raiz, 7, "Atajo", txtAtajo)

            AddSeparador(raiz, 8, "PRECIOS")

            AddRow(raiz, 9, "Precio de costo", txtCosto)
            AddRow(raiz, 10, "Precio de venta", txtVenta)

            lblSugerido = Tema.CrearLabel("", Tono.Secundario, Tema.TamMini)
            lblSugerido.Dock = DockStyle.Fill
            lblSugerido.TextAlign = ContentAlignment.TopLeft
            ' Va en una sola celda esperando las dos filas de precios: un control no
            ' puede estar en dos lugares a la vez.
            raiz.Controls.Add(lblSugerido, 2, 9)
            raiz.SetRowSpan(lblSugerido, 2)

            AddSeparador(raiz, 11, "STOCK")

            AddRow(raiz, 12, "Stock inicial", txtStock)
            AddRow(raiz, 13, "Stock minimo", txtStockMinimo)

            btnGuardar = Tema.CrearBoton("Guardar", EstiloBoton.Exito, 150, 36)
            btnCancelar = Tema.CrearBoton("Cancelar", EstiloBoton.Neutro, 120, 36)
            AddHandler btnGuardar.Click, Sub() Guardar()
            AddHandler btnCancelar.Click, Sub() Cancelar()

            Dim acciones As New FlowLayoutPanel With {
                .Dock = DockStyle.Fill,
                .FlowDirection = FlowDirection.RightToLeft,
                .WrapContents = False,
                .BackColor = Color.Transparent
            }
            acciones.Controls.Add(btnCancelar)
            acciones.Controls.Add(btnGuardar)
            raiz.Controls.Add(acciones, 0, 14)
            raiz.SetColumnSpan(acciones, 3)

            Controls.Add(raiz)
        End Sub

        Private Sub AddRow(panel As TableLayoutPanel, fila As Integer,
                           etiqueta As String, ctrl As Control)

            Dim lbl As New Label With {
                .Text = etiqueta,
                .Dock = DockStyle.Fill,
                .TextAlign = ContentAlignment.MiddleLeft
            }
            panel.Controls.Add(lbl, 0, fila)
            panel.Controls.Add(ctrl, 1, fila)
        End Sub

        Private Sub AddSeparador(panel As TableLayoutPanel, fila As Integer, titulo As String)
            Dim lbl As New Label With {
                .Text = titulo,
                .Dock = DockStyle.Fill,
                .TextAlign = ContentAlignment.MiddleLeft,
                .ForeColor = Tema.TextoSec,
                .Font = Tema.Fuente(Tema.TamMini, FontStyle.Bold),
                .Margin = New Padding(0, 8, 0, 4)
            }
            panel.Controls.Add(lbl, 0, fila)
            panel.SetColumnSpan(lbl, 3)
        End Sub

        Private Sub GuardarConEnter(ev As KeyEventArgs)
            If ev.KeyCode = Keys.Enter Then
                ev.SuppressKeyPress = True
                Guardar()
            End If
        End Sub

        ''' <summary>Ofrece las categorias y subcategorias que ya existen.</summary>
        Private Sub CargarSugerencias()
            Dim categoriaActual = Si(TextoDe(txtCategoria), "")

            txtCategoria.Items.Clear()
            For Each c In ProductoService.Categorias()
                txtCategoria.Items.Add(c)
            Next

            Dim subActual = Si(TextoDe(txtSubcategoria), "")
            txtSubcategoria.Items.Clear()
            For Each s In ProductoService.Subcategorias(categoriaActual)
                txtSubcategoria.Items.Add(s)
            Next

            txtCategoria.Text = categoriaActual
            txtSubcategoria.Text = subActual
        End Sub

        Private Sub CargarDatos()
            CargarSugerencias()

            If producto Is Nothing Then
                txtUnidad.Text = "un"
                txtStock.Value = 0
                ActualizarSugerido()
                txtNombre.Focus()
                Return
            End If

            txtNombre.Text = Si(producto.Nombre, "")
            txtMarca.Text = Si(producto.Marca, "")
            txtCodigoBarra.Text = Si(producto.CodigoBarra, "")
            txtDescripcion.Text = Si(producto.Descripcion, "")
            txtUnidad.Text = Si(producto.UnidadMedida, "un")
            txtCategoria.Text = Si(producto.Categoria, "")
            txtSubcategoria.Text = Si(producto.Subcategoria, "")
            txtAtajo.Text = Si(producto.Atajo, "")
            txtCosto.Text = producto.PrecioCosto.ToString("N2")
            txtVenta.Text = producto.Precio.ToString("N2")
            txtStock.Value = Math.Max(0, Math.Min(producto.Stock, txtStock.Maximum))
            txtStockMinimo.Value = Math.Max(0, Math.Min(producto.StockMinimo,
                                                        txtStockMinimo.Maximum))
            CargarSugerencias()
            ActualizarSugerido()
        End Sub

        ''' <summary>
        ''' Muestra cuanto saldria el precio con la configuracion del negocio. Es una
        ''' referencia: el precio que queda guardado es el que se escribio, porque el
        ''' margen de cada producto se puede negociar.
        ''' </summary>
        Private Sub ActualizarSugerido()
            Dim costo As Decimal
            If Not Decimal.TryParse(Si(TextoDe(txtCosto), "").Replace(".", ","),
                                    NumberStyles.Number, CultureInfo.CurrentCulture,
                                    costo) Then
                lblSugerido.Text = ""
                Return
            End If

            Dim sugerido = ProductoService.PrecioSugerido(costo)
            If sugerido <= 0D Then
                lblSugerido.Text = ""
                Return
            End If

            lblSugerido.Text = "Sugerido: " & sugerido.ToString("C2")
            lblSugerido.ForeColor = Tema.TextoSec
        End Sub

        Private Sub Guardar()
            Dim candidato As Producto
            If producto Is Nothing Then
                candidato = New Producto()
            Else
                ' Se trabaja sobre una copia y ProductoService decide si la escribe
                ' sobre el original: asi un guardado invalido no deja el producto a
                ' medio modificar en el catalogo.
                candidato = New Producto With {
                    .ID = producto.ID,
                    .Nombre = producto.Nombre,
                    .Stock = producto.Stock
                }
            End If

            candidato.Nombre = TextoDe(txtNombre)
            candidato.Marca = TextoDe(txtMarca)
            candidato.CodigoBarra = TextoDe(txtCodigoBarra)
            candidato.Descripcion = TextoDe(txtDescripcion)
            candidato.UnidadMedida = TextoDe(txtUnidad)
            candidato.Categoria = TextoDe(txtCategoria)
            candidato.Subcategoria = TextoDe(txtSubcategoria)
            candidato.Atajo = TextoDe(txtAtajo)

            Dim costo As Decimal
            Dim venta As Decimal

            If Not Decimal.TryParse(Si(TextoDe(txtCosto), "").Replace(".", ","),
                                    NumberStyles.Number, CultureInfo.CurrentCulture, costo) Then
                MessageBox.Show("El precio de costo tiene que ser un numero.", "Producto",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtCosto.Focus()
                Return
            End If

            If Not Decimal.TryParse(Si(TextoDe(txtVenta), "").Replace(".", ","),
                                    NumberStyles.Number, CultureInfo.CurrentCulture, venta) Then
                MessageBox.Show("El precio de venta tiene que ser un numero.", "Producto",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtVenta.Focus()
                Return
            End If

            candidato.PrecioCosto = costo
            candidato.Precio = venta
            candidato.Stock = CInt(txtStock.Value)
            candidato.StockMinimo = CInt(txtStockMinimo.Value)

            Dim resultado = ProductoService.Guardar(candidato)
            If Not resultado.Ok Then
                MessageBox.Show(resultado.Mensaje, "Producto",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            producto = resultado.Producto
            DialogResult = DialogResult.OK
            Close()
        End Sub

        Private Sub Cancelar()
            DialogResult = DialogResult.Cancel
            Close()
        End Sub

        Private Shared Function TextoDe(ctrl As Control) As String
            Return If(ctrl.Text, "").Trim()
        End Function

        Private Shared Function Si(texto As String, porDefecto As String) As String
            Return If(String.IsNullOrWhiteSpace(texto), porDefecto, texto)
        End Function

    End Class

End Namespace
