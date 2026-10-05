Imports System.Drawing
Imports System.Linq
Imports System.Windows.Forms

Namespace CafeteriaOS

    ''' <summary>
    ''' Ingreso de mercaderia al deposito.
    '''
    ''' Es una operacion interna del negocio, no una venta: no cobra, no genera ticket
    ''' y no pasa por caja. Solo suma stock y deja de donde vino cada unidad.
    '''
    ''' El proveedor es opcional. Hay ingresos que no vienen de una compra (una
    ''' devolucion, un rescate de mercaderia, una correccion de deposito) y obligar a
    ''' elegir uno terminaba inventando de quien era la mercaderia.
    '''
    ''' Arriba va el catalogo con sus filtros, que es donde se busca y se elige; abajo
    ''' el detalle del ingreso, que es lo que se confirma. Se separan porque son dos
    ''' cosas distintas: una es de donde se saca el producto, la otra es lo que entra.
    ''' </summary>
    Partial Class IngresoStockForm
        Inherits Form

        Private proveedor As Proveedor
        Private lineas As New List(Of DetalleIngreso)()
        Private ingresoCreado As IngresoStock

        Private WithEvents cmbProveedor As ComboBox
        Private WithEvents txtBuscar As TextBox
        Private WithEvents cmbCategoria As ComboBox
        Private WithEvents cmbSubcategoria As ComboBox
        Private WithEvents btnNuevoConCodigo As Button
        Private WithEvents tablaCatalogo As DataGridView
        Private WithEvents numCantidad As NumericUpDown
        Private WithEvents tabla As DataGridView

        Private WithEvents btnAgregar As Button
        Private WithEvents btnQuitar As Button
        Private WithEvents btnConfirmar As Button
        Private WithEvents btnCancelar As Button

        Private lblProveedor As Label
        Private lblResumen As Label
        Private lblSinCodigo As Label

        Public Sub New(proveedor As Proveedor)
            Me.proveedor = proveedor
            ArmarInterfaz()
            CargarProveedores()
            CargarFiltros()
            ' El catalogo se carga de entrada: se entra aca para buscar un producto,
            ' no para mirarlo vacio. La grilla se vuelve a pintar sola cuando cambia
            ' el texto o los filtros.
            BuscarProducto("")
            ActualizarResumen()
        End Sub

        ''' <summary>
        ''' Abre el ingreso, opcionalmente para un proveedor. Devuelve el ingreso creado
        ''' o Nothing si se cancela.
        ''' </summary>
        Public Shared Function Registrar(Optional proveedor As Proveedor = Nothing) As IngresoStock
            Using f As New IngresoStockForm(proveedor)
                If f.ShowDialog() <> DialogResult.OK Then Return Nothing
                Return f.ingresoCreado
            End Using
        End Function

        Public Shared Function Reponer(producto As Producto) As IngresoStock
            If producto Is Nothing Then
                MessageBox.Show("Elegi un producto de la lista.", "Ingresar stock",
                                MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return Nothing
            End If
            Using f As New IngresoStockForm(Nothing)
                f.PrecargarProducto(producto)
                If f.ShowDialog() <> DialogResult.OK Then Return Nothing
                Return f.ingresoCreado
            End Using
        End Function

        ''' <summary>
        ''' Deja el producto ya elegido y en el foco la cantidad. Se usa cuando se
        ''' entra desde el panel de stock, donde el producto ya se sabe y no hay nada
        ''' que buscar.
        ''' </summary>
        Private Sub PrecargarProducto(producto As Producto)
            If producto Is Nothing Then Return

            BuscarProducto(producto.NombreCompleto())

            If ProductoDeFilaSeleccionada() Is Nothing Then
                For Each fila As DataGridViewRow In tablaCatalogo.Rows
                    If ReferenceEquals(fila.Tag, producto) Then
                        fila.Selected = True
                        tablaCatalogo.CurrentCell = fila.Cells(0)
                        Exit For
                    End If
                Next
            End If

            numCantidad.Value = Math.Max(1, numCantidad.Minimum)
            numCantidad.Focus()
            numCantidad.Select()
        End Sub

        #Region "Construccion"

        Private Sub ArmarInterfaz()
            Me.Name = "th.fondo"
            Me.Text = "Ingreso de mercaderia"
            Me.Font = Tema.Fuente(Tema.TamNormal)
            Me.BackColor = Tema.BgPrincipal
            Me.ForeColor = Tema.TextoPrinc
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            Me.StartPosition = FormStartPosition.CenterParent
            Me.ClientSize = New Size(1000, 760)
            ' Sin esto WinForms escala con los valores por defecto y no con los que
            ' escribe el Designer: a 125% o 150% la fuente crece y las columnas no,
            ' y los labels quedan cortados.
            Me.AutoScaleMode = AutoScaleMode.Dpi
            Me.AutoScaleDimensions = New SizeF(96, 96)
            Me.MinimizeBox = False

            Dim raiz As New TableLayoutPanel With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 7,
                .BackColor = Tema.BgPrincipal,
                .Padding = New Padding(16)
            }
            raiz.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            raiz.RowStyles.Add(New RowStyle(SizeType.Absolute, 40))
            raiz.RowStyles.Add(New RowStyle(SizeType.Absolute, 44))
            raiz.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            raiz.RowStyles.Add(New RowStyle(SizeType.Absolute, 48))
            raiz.RowStyles.Add(New RowStyle(SizeType.Absolute, 26))
            raiz.RowStyles.Add(New RowStyle(SizeType.Percent, 80))
            raiz.RowStyles.Add(New RowStyle(SizeType.Absolute, 46))

            raiz.Controls.Add(ArmarCabecera(), 0, 0)
            raiz.Controls.Add(ArmarFiltros(), 0, 1)
            raiz.Controls.Add(ArmarCatalogo(), 0, 2)
            raiz.Controls.Add(ArmarCarga(), 0, 3)
            raiz.Controls.Add(ArmarAvisoDeCodigo(), 0, 4)
            raiz.Controls.Add(ArmarDetalle(), 0, 5)
            raiz.Controls.Add(ArmarAcciones(), 0, 6)

            Controls.Add(raiz)
        End Sub

        ''' <summary>
        ''' El proveedor. El primer item es "sin proveedor" y no es un error elegirlo:
        ''' queda como "Ajuste manual / Reposicion" en el historial.
        ''' </summary>
        Private Function ArmarCabecera() As Control
            Dim lbl As New Label With {
                .Text = "Proveedor",
                .Dock = DockStyle.Fill,
                .TextAlign = ContentAlignment.MiddleLeft
            }

            cmbProveedor = Tema.CrearCombo()
            cmbProveedor.Dock = DockStyle.Fill
            cmbProveedor.DropDownStyle = ComboBoxStyle.DropDownList
            ' Sin DisplayMember el combo dibuja el nombre del tipo: la lista se
            ' llena igual, pero todas las filas dicen "CafeteriaOS.Proveedor" y no
            ' se distingue un proveedor de otro. El SelectedItem sigue siendo el
            ' objeto Proveedor.
            cmbProveedor.DisplayMember = NameOf(Proveedor.Nombre)
            AddHandler cmbProveedor.SelectedIndexChanged, Sub() ElegirProveedor()

            Dim host As New TableLayoutPanel With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 4,
                .RowCount = 1,
                .BackColor = Color.Transparent
            }
            host.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 90))
            host.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 280))
            host.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 20))
            host.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))

            lblProveedor = Tema.CrearLabel("", Tono.Secundario, Tema.TamMini)
            lblProveedor.Dock = DockStyle.Fill
            lblProveedor.AutoSize = False
            lblProveedor.TextAlign = ContentAlignment.MiddleLeft

            host.Controls.Add(lbl, 0, 0)
            host.Controls.Add(cmbProveedor, 1, 0)
            host.Controls.Add(New Panel With {.Dock = DockStyle.Fill, .BackColor = Color.Transparent}, 2, 0)
            host.Controls.Add(lblProveedor, 3, 0)
            Return host
        End Function

        ''' <summary>Busqueda por texto, categoria y subcategoria del catalogo.</summary>
        Private Function ArmarFiltros() As Control
            txtBuscar = UiKit.CampoBusqueda("Buscar por nombre, marca o codigo de barras")
            txtBuscar.Dock = DockStyle.Fill
            txtBuscar.AccessibleName = "Buscar producto"
            AddHandler txtBuscar.TextChanged, Sub() BuscarProducto(txtBuscar.Text)

            cmbCategoria = Tema.CrearCombo()
            cmbCategoria.Dock = DockStyle.Fill
            cmbCategoria.DropDownStyle = ComboBoxStyle.DropDownList
            AddHandler cmbCategoria.SelectedIndexChanged, Sub() CargarSubcategorias()

            cmbSubcategoria = Tema.CrearCombo()
            cmbSubcategoria.Dock = DockStyle.Fill
            cmbSubcategoria.DropDownStyle = ComboBoxStyle.DropDownList

            Dim host As New TableLayoutPanel With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 4,
                .RowCount = 1,
                .BackColor = Color.Transparent
            }
            host.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            host.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 10))
            host.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 190))
            host.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 190))

            host.Controls.Add(txtBuscar, 0, 0)
            host.Controls.Add(New Panel With {.Dock = DockStyle.Fill, .BackColor = Color.Transparent}, 1, 0)
            host.Controls.Add(cmbCategoria, 2, 0)
            host.Controls.Add(cmbSubcategoria, 3, 0)
            Return host
        End Function

        Private Function ArmarCatalogo() As Control
            tablaCatalogo = Tema.CrearTabla()
            tablaCatalogo.Dock = DockStyle.Fill
            tablaCatalogo.ReadOnly = True
            tablaCatalogo.AllowUserToAddRows = False
            tablaCatalogo.AllowUserToDeleteRows = False
            tablaCatalogo.MultiSelect = False
            tablaCatalogo.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            UiKit.Columna(tablaCatalogo, "Producto", 340)
            UiKit.Columna(tablaCatalogo, "Categoria", 170)
            UiKit.Columna(tablaCatalogo, "Stock", 90)
            UiKit.Columna(tablaCatalogo, "Precio", 100)
            tablaCatalogo.Columns(0).FillWeight = 55
            tablaCatalogo.Columns(1).FillWeight = 25
            tablaCatalogo.Columns(2).FillWeight = 10
            tablaCatalogo.Columns(3).FillWeight = 15
            AddHandler tablaCatalogo.SelectionChanged, Sub() ProductoElegidoCambio()
            Return tablaCatalogo
        End Function

        ''' <summary>Cantidad a ingresar del producto elegido.</summary>
        Private Function ArmarCarga() As Control
            numCantidad = New NumericUpDown With {
                .Dock = DockStyle.Fill,
                .Minimum = 1,
                .Maximum = 100000,
                .Value = 1,
                .Font = Tema.Fuente(Tema.TamNormal)
            }

            btnAgregar = Tema.CrearBoton("Agregar al ingreso", EstiloBoton.Info, 170, 34)
            btnQuitar = Tema.CrearBoton("Quitar linea", EstiloBoton.Neutro, 130, 34)
            btnQuitar.Enabled = False
            AddHandler btnAgregar.Click, Sub() AgregarLinea()
            AddHandler btnQuitar.Click, Sub() QuitarLinea()

            lblResumen = Tema.CrearLabel("", Tono.Primario, Tema.TamMini, FontStyle.Bold)
            lblResumen.Dock = DockStyle.Fill
            lblResumen.AutoSize = False
            ' El resumen es texto largo ("12 producto(s), 340 unidad(es) a ingresar")
            ' y la columna es la que sobra: no puede recortarse.
            lblResumen.TextAlign = ContentAlignment.MiddleRight
            lblResumen.AutoEllipsis = False

            Dim host As New TableLayoutPanel With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 5,
                .RowCount = 1,
                .BackColor = Color.Transparent
            }
            host.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 85))
            host.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 100))
            host.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 170))
            host.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 130))
            host.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))

            host.Controls.Add(Etiqueta("Cantidad"), 0, 0)
            host.Controls.Add(numCantidad, 1, 0)
            host.Controls.Add(btnAgregar, 2, 0)
            host.Controls.Add(btnQuitar, 3, 0)
            host.Controls.Add(lblResumen, 4, 0)
            Return host
        End Function

        ''' <summary>
        ''' Aviso de codigo de barras sin resultado. Un numero que no esta en el
        ''' catalogo tiene dos salidas: registrar el producto, o admitirse que se
        ''' escribio mal. Se ofrecen las dos, porque decidirlo de antemano seria
        ''' adivinar.
        ''' </summary>
        Private Function ArmarAvisoDeCodigo() As Control
            lblSinCodigo = Tema.CrearLabel("", Tono.Atencion, Tema.TamMini)
            lblSinCodigo.Dock = DockStyle.Fill
            lblSinCodigo.AutoSize = False
            lblSinCodigo.TextAlign = ContentAlignment.MiddleLeft

            btnNuevoConCodigo = Tema.CrearBoton("Registrar producto", EstiloBoton.Neutro, 170, 28)
            btnNuevoConCodigo.Visible = False
            AddHandler btnNuevoConCodigo.Click, Sub() RegistrarProductoConCodigo()

            Dim host As New TableLayoutPanel With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 2,
                .RowCount = 1,
                .BackColor = Color.Transparent
            }
            host.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            host.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 180))
            host.Controls.Add(lblSinCodigo, 0, 0)
            host.Controls.Add(btnNuevoConCodigo, 1, 0)
            Return host
        End Function

        Private Function ArmarDetalle() As Control
            tabla = Tema.CrearTabla()
            tabla.Dock = DockStyle.Fill
            UiKit.Columna(tabla, "Producto", 300)
            UiKit.Columna(tabla, "Stock actual", 110)
            UiKit.Columna(tabla, "Cantidad", 110)
            UiKit.Columna(tabla, "Stock resultante", 130)
            UiKit.Columna(tabla, "Como llego", 300)
            tabla.AllowUserToAddRows = False
            tabla.AllowUserToDeleteRows = False
            tabla.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            tabla.Columns(3).DefaultCellStyle.ForeColor = Tema.Azul
            tabla.Columns(3).DefaultCellStyle.Font = Tema.Fuente(Tema.TamNormal, FontStyle.Bold)
            ' Todo es solo lectura menos la observacion: ahi se anota como llego la
            ' mercaderia, por ejemplo si vino con las esquinas aplastadas.
            For i = 0 To 3
                tabla.Columns(i).ReadOnly = True
            Next
            tabla.Columns(4).ReadOnly = False
            AddHandler tabla.SelectionChanged, Sub() LineaSeleccionadaCambio()
            Return tabla
        End Function

        Private Function ArmarAcciones() As Control
            btnConfirmar = Tema.CrearBoton("Confirmar ingreso", EstiloBoton.Exito, 190, 38)
            btnCancelar = Tema.CrearBoton("Cancelar", EstiloBoton.Neutro, 130, 38)
            AddHandler btnConfirmar.Click, Sub() Confirmar()
            AddHandler btnCancelar.Click, Sub() Cancelar()

            Dim acciones As New FlowLayoutPanel With {
                .Dock = DockStyle.Fill,
                .FlowDirection = FlowDirection.RightToLeft,
                .WrapContents = False,
                .BackColor = Color.Transparent
            }
            acciones.Controls.Add(btnCancelar)
            acciones.Controls.Add(btnConfirmar)
            Return acciones
        End Function

        Private Function Etiqueta(texto As String) As Label
            Return New Label With {
                .Text = texto,
                .Dock = DockStyle.Fill,
                .TextAlign = ContentAlignment.MiddleLeft
            }
        End Function

        #End Region

        #Region "Carga de listas"

        Private Sub CargarProveedores()
            cmbProveedor.Items.Clear()
            cmbProveedor.Items.Add("(sin proveedor)")

            Dim proveedores = Datos.ListaProveedores.
                Where(Function(p) p.Activo).
                OrderBy(Function(p) p.Nombre).
                ToList()
            For Each p In proveedores
                cmbProveedor.Items.Add(p)
            Next

            If proveedor Is Nothing Then
                cmbProveedor.SelectedIndex = 0
            Else
                Dim i = proveedores.FindIndex(Function(p) ReferenceEquals(p, proveedor))
                cmbProveedor.SelectedIndex = If(i >= 0, i + 1, 0)
                If i < 0 Then proveedor = Nothing
            End If
            ActualizarCabecera()
        End Sub

        Private Sub ElegirProveedor()
            If cmbProveedor.SelectedIndex <= 0 Then
                proveedor = Nothing
            Else
                proveedor = TryCast(cmbProveedor.SelectedItem, Proveedor)
            End If
            ActualizarCabecera()
        End Sub

        Private Sub ActualizarCabecera()
            If lblProveedor Is Nothing Then Return
            If proveedor Is Nothing Then
                lblProveedor.Text = "Quedara registrado como ajuste manual, sin proveedor."
                lblProveedor.ForeColor = Tema.TextoSec
            Else
                Dim texto = "Ingreso de " & proveedor.Nombre
                If Not String.IsNullOrWhiteSpace(proveedor.ContactoPrincipal) Then
                    texto &= " - contacto: " & proveedor.ContactoPrincipal.Trim()
                End If
                lblProveedor.Text = texto
                lblProveedor.ForeColor = Tema.TextoSec
            End If
        End Sub

        Private Sub CargarFiltros()
            cmbCategoria.Items.Clear()
            cmbCategoria.Items.AddRange(ProductoService.CategoriasParaFiltrar().ToArray())
            cmbCategoria.SelectedIndex = 0

            CargarSubcategorias()
        End Sub

        Private Sub CargarSubcategorias()
            Dim categoria = ProductoService.CategoriaDelFiltro(SiTexto(cmbCategoria.SelectedItem))

            cmbSubcategoria.Items.Clear()
            cmbSubcategoria.Items.Add(ProductoService.Todas)
            For Each s In ProductoService.Subcategorias(categoria)
                cmbSubcategoria.Items.Add(s)
            Next
            cmbSubcategoria.SelectedIndex = 0
        End Sub

        #End Region

        #Region "Catalogo"

        Private Sub BuscarProducto(texto As String)
            If tablaCatalogo Is Nothing Then Return

            Dim categoria = ProductoService.CategoriaDelFiltro(SiTexto(cmbCategoria.SelectedItem))
            Dim subcategoria = ProductoService.CategoriaDelFiltro(SiTexto(cmbSubcategoria.SelectedItem))

            Dim encontrados = ProductoService.Buscar(texto, categoria, subcategoria)

            tablaCatalogo.Rows.Clear()
            For Each p In encontrados
                Dim indice = tablaCatalogo.Rows.Add(p.NombreCompleto(), If(p.Categoria, "-"),
                                                    p.Stock.ToString(), p.Precio.ToString("C2"))
                tablaCatalogo.Rows(indice).Tag = p
            Next

            ActualizarAvisoDeCodigo(texto, encontrados.Count)
        End Sub

        ''' <summary>
        ''' Si lo escrito parece un codigo de barras y no esta en el catalogo, se avisa
        ''' y se ofrece dar de alta el producto con ese codigo. Solo cuando parece un
        ''' codigo: un nombre mal escrito no se pide registrar como producto nuevo.
        ''' </summary>
        Private Sub ActualizarAvisoDeCodigo(texto As String, encontrados As Integer)
            Dim escrito = SiTexto(texto)
            Dim pareceCodigo = escrito.Length >= 4 AndAlso escrito.All(Function(c) Char.IsDigit(c))

            If pareceCodigo AndAlso escrito.Length > 0 AndAlso encontrados = 0 Then
                lblSinCodigo.Text = "El codigo " & escrito &
                                    " no esta en el catalogo. Registralo o volve a buscar."
                btnNuevoConCodigo.Visible = True
            Else
                lblSinCodigo.Text = ""
                btnNuevoConCodigo.Visible = False
            End If
        End Sub

        Private Sub RegistrarProductoConCodigo()
            Dim codigo = SiTexto(txtBuscar.Text)
            If codigo.Length = 0 Then Return

            Dim guardado = ProductoEditorForm.Editar(New Producto With {.CodigoBarra = codigo})
            If guardado Is Nothing Then Return

            txtBuscar.Text = ""
            BuscarProducto(guardado.NombreCompleto())
            For Each fila As DataGridViewRow In tablaCatalogo.Rows
                If ReferenceEquals(fila.Tag, guardado) Then
                    fila.Selected = True
                    tablaCatalogo.CurrentCell = fila.Cells(0)
                    Exit For
                End If
            Next
            numCantidad.Focus()
        End Sub

        Private Function ProductoDeFilaSeleccionada() As Producto
            If tablaCatalogo.SelectedRows.Count = 0 Then Return Nothing
            Dim fila = tablaCatalogo.SelectedRows(0)
            If fila.Index < 0 OrElse fila.Index >= tablaCatalogo.Rows.Count Then Return Nothing
            Return TryCast(fila.Tag, Producto)
        End Function

        Private Sub ProductoElegidoCambio()
            Dim producto = ProductoDeFilaSeleccionada()
            If producto Is Nothing Then Return
            ' Cuando se elige con el teclado el foco tiene que volver a la cantidad:
            ' si no, hay que tomarlo con el mouse y se corta el flujo del ingreso.
            If tablaCatalogo.Focused Then
                numCantidad.Focus()
                numCantidad.Select()
            End If
        End Sub

        #End Region

        #Region "Detalle del ingreso"

        Private Sub AgregarLinea()
            Dim producto = ProductoDeFilaSeleccionada()
            If producto Is Nothing Then
                MessageBox.Show("Elegi un producto del catalogo.", "Ingreso de mercaderia",
                                MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim cantidad = CInt(numCantidad.Value)
            Dim existente = lineas.FirstOrDefault(
                Function(d) ReferenceEquals(d.Producto, producto))

            If existente Is Nothing Then
                lineas.Add(New DetalleIngreso With {.Producto = producto, .Cantidad = cantidad})
            Else
                ' El mismo producto dos veces se acumula en una sola linea. Es el
                ' mismo producto exacto: otra marca u otra unidad son otros
                ' productos y van por separado.
                existente.Cantidad += cantidad
            End If

            ActualizarResumen()
        End Sub

        Private Sub QuitarLinea()
            If tabla.SelectedRows.Count = 0 Then Return
            Dim indice = tabla.SelectedRows(0).Index
            If indice < 0 OrElse indice >= lineas.Count Then Return
            lineas.RemoveAt(indice)
            ActualizarResumen()
        End Sub

        Private Sub LineaSeleccionadaCambio()
            If btnQuitar Is Nothing Then Return
            btnQuitar.Enabled = tabla.SelectedRows.Count > 0
        End Sub

        Private Sub ActualizarResumen()
            tabla.Rows.Clear()
            For Each d In lineas
                Dim antes = d.Producto.Stock
                Dim indice = tabla.Rows.Add(d.Producto.NombreCompleto(), antes.ToString(),
                                            d.Cantidad.ToString(),
                                            (antes + d.Cantidad).ToString(), d.Observacion)
                tabla.Rows(indice).Tag = d
            Next
            btnQuitar.Enabled = lineas.Count > 0

            lblResumen.Text = lineas.Count & " producto(s), " &
                              lineas.Sum(Function(d) d.Cantidad) & " unidad(es) a ingresar."
        End Sub

        ''' <summary>
        ''' Copia lo anotado en la columna de observacion a las lineas. Se lee antes de
        ''' confirmar porque la tabla se dibuja desde las lineas y no al reves.
        ''' </summary>
        Private Sub LeerObservaciones()
            For Each fila As DataGridViewRow In tabla.Rows
                Dim linea = TryCast(fila.Tag, DetalleIngreso)
                If linea Is Nothing Then Continue For
                linea.Observacion = If(fila.Cells(4).Value Is Nothing, "",
                                       fila.Cells(4).Value.ToString().Trim())
            Next
        End Sub

        #End Region

        #Region "Confirmacion"

        Private Sub Confirmar()
            If lineas.Count = 0 Then
                MessageBox.Show("Carga al menos un producto.", "Ingreso de mercaderia",
                                MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            LeerObservaciones()

            Dim resultado = IngresoService.Registrar(proveedor, lineas, Nothing)
            If Not resultado.Ok Then
                MessageBox.Show(resultado.Mensaje, "Ingreso de mercaderia",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            ingresoCreado = resultado.Ingreso

            ' El ingreso quedo aplicado, pero el historial no se pudo escribir. No
            ' es motivo para dizer que fallo: se avisa aparte.
            If Not String.IsNullOrWhiteSpace(resultado.Advertencia) Then
                MessageBox.Show(resultado.Advertencia, "Ingreso de mercaderia",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If

            DialogResult = DialogResult.OK
            Close()
        End Sub

        Private Sub Cancelar()
            DialogResult = DialogResult.Cancel
            Close()
        End Sub

        Private Shared Function SiTexto(valor As Object) As String
            If valor Is Nothing Then Return ""
            Return If(valor.ToString(), "").Trim()
        End Function

        #End Region

    End Class

End Namespace
