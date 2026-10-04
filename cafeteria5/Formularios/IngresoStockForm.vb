Imports System.Drawing
Imports System.Linq
Imports System.Windows.Forms

Namespace CafeteriaOS

    ''' <summary>
    ''' Carga de mercaderia que trajo un proveedor. Se pueden agregar varias lineas de
    ''' producto en el mismo ingreso y al confirmar el stock sube con todas juntas.
    ''' </summary>
    Partial Class IngresoStockForm
        Inherits Form

        Private proveedor As Proveedor
        Private lineas As New List(Of DetalleIngreso)()

        Private WithEvents btnAgregar As Button
        Private WithEvents btnQuitar As Button
        Private WithEvents btnConfirmar As Button
        Private WithEvents btnCancelar As Button

        Private cmbProducto As ComboBox
        Private numCantidad As NumericUpDown
        Private lblProveedor As Label
        Private lblResumen As Label
        Private WithEvents tabla As DataGridView

        Private txtCodigoBarra As TextBox
        Private txtMarca As TextBox
        Private txtUnidadMedida As TextBox
        Private numPrecioCosto As NumericUpDown
        Private numPrecioVenta As NumericUpDown
        Private lblPrecioRecomendado As Label

        Public Sub New(proveedor As Proveedor)
            Me.proveedor = proveedor
            ArmarInterfaz()
            CargarProductos()
            ActualizarResumen()
        End Sub

        ''' <summary>
        ''' Abre el ingreso para un proveedor. Devuelve el ingreso creado o Nothing si
        ''' se cancela.
        ''' </summary>
        Public Shared Function Registrar(proveedor As Proveedor) As IngresoStock
            If proveedor Is Nothing Then
                MessageBox.Show("Elegi un proveedor de la lista.", "Ingreso de mercaderia",
                                MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return Nothing
            End If
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

        Private Sub PrecargarProducto(producto As Producto)
            If producto Is Nothing Then Return
            Try
                cmbProducto.SelectedItem = producto
            Catch
            End Try
            cmbProducto.Enabled = False
            numCantidad.Value = 1
            numCantidad.Focus()
            ' Para reposición rápida: agregar línea automáticamente
            Dim existente = lineas.FirstOrDefault(Function(d) ReferenceEquals(d.Producto, producto))
            If existente Is Nothing Then
                lineas.Add(New DetalleIngreso With {.Producto = producto, .Cantidad = CInt(numCantidad.Value)})
            Else
                existente.Cantidad = CInt(numCantidad.Value)
            End If
            ActualizarResumen()
        End Sub

        Private ingresoCreado As IngresoStock

        #Region "Construccion"

        Private Sub ArmarInterfaz()
            Me.Name = "th.fondo"
            Me.Text = "Ingreso de mercaderia"
            Me.Font = Tema.Fuente(Tema.TamNormal)
            Me.BackColor = Tema.BgPrincipal
            Me.ForeColor = Tema.TextoPrinc
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.StartPosition = FormStartPosition.CenterParent
            Me.ClientSize = New System.Drawing.Size(860, 620)

            Dim raiz As New TableLayoutPanel With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 5,
                .BackColor = Tema.BgPrincipal,
                .Padding = New Padding(16)
            }
            raiz.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            raiz.RowStyles.Add(New RowStyle(SizeType.Absolute, 54))
            raiz.RowStyles.Add(New RowStyle(SizeType.Absolute, 110))
            raiz.RowStyles.Add(New RowStyle(SizeType.Absolute, 46))
            raiz.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            raiz.RowStyles.Add(New RowStyle(SizeType.Absolute, 50))

            lblProveedor = Tema.CrearLabel("", Tono.Secundario, Tema.TamNormal)
            lblProveedor.Dock = DockStyle.Fill
            lblProveedor.AutoSize = False
            lblProveedor.TextAlign = ContentAlignment.MiddleLeft
            raiz.Controls.Add(lblProveedor, 0, 0)

            Dim carga As New TableLayoutPanel With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 4,
                .RowCount = 1,
                .BackColor = Color.Transparent
            }
            carga.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            carga.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 120))
            carga.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 90))
            carga.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 110))

            cmbProducto = Tema.CrearCombo()
            cmbProducto.Dock = DockStyle.Fill
            cmbProducto.DropDownStyle = ComboBoxStyle.DropDownList

            numCantidad = New NumericUpDown With {
                .Dock = DockStyle.Fill,
                .Minimum = 1,
                .Maximum = 100000,
                .Value = 1,
                .Margin = New Padding(6, 0, 6, 0),
                .Font = Tema.Fuente(Tema.TamNormal)
            }

            btnAgregar = Tema.CrearBoton("Agregar", EstiloBoton.Info, 110, 34)
            btnQuitar = Tema.CrearBoton("Quitar", EstiloBoton.Neutro, 90, 34)
            btnQuitar.Enabled = False

            carga.Controls.Add(cmbProducto, 0, 0)
            carga.Controls.Add(numCantidad, 1, 0)
            carga.Controls.Add(btnAgregar, 2, 0)
            carga.Controls.Add(btnQuitar, 3, 0)
            raiz.Controls.Add(carga, 0, 1)

            lblResumen = Tema.CrearLabel("", Tono.Primario, Tema.TamMini, FontStyle.Bold)
            lblResumen.Dock = DockStyle.Fill
            lblResumen.AutoSize = False
            lblResumen.TextAlign = ContentAlignment.MiddleLeft
            raiz.Controls.Add(lblResumen, 0, 2)

            tabla = Tema.CrearTabla()
            tabla.Dock = DockStyle.Fill
            UiKit.Columna(tabla, "Producto", 320)
            UiKit.Columna(tabla, "Stock actual", 110)
            UiKit.Columna(tabla, "Cantidad", 110)
            UiKit.Columna(tabla, "Stock resultante", 130)
            UiKit.Columna(tabla, "Como llego", 240)
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
            raiz.Controls.Add(tabla, 0, 3)

            btnConfirmar = Tema.CrearBoton("Confirmar ingreso", EstiloBoton.Exito, 190, 38)
            btnCancelar = Tema.CrearBoton("Cancelar", EstiloBoton.Neutro, 130, 38)
            Dim acciones As New FlowLayoutPanel With {
                .Dock = DockStyle.Fill,
                .FlowDirection = FlowDirection.RightToLeft,
                .WrapContents = False,
                .BackColor = Color.Transparent
            }
            acciones.Controls.Add(btnCancelar)
            acciones.Controls.Add(btnConfirmar)
            raiz.Controls.Add(acciones, 0, 4)

            Controls.Add(raiz)

            ActualizarCabecera()
        End Sub

        Private Sub CargarProductos()
            Dim productos = Datos.ListaProductos.OrderBy(Function(p) p.Nombre).ToList()
            cmbProducto.Items.Clear()
            For Each p In productos
                cmbProducto.Items.Add(p)
            Next
            If productos.Count > 0 Then cmbProducto.SelectedIndex = 0
        End Sub

        Private Sub ActualizarCabecera()
            Dim texto = "Ingreso de stock"
            If proveedor IsNot Nothing Then
                texto = "Ingreso de " & proveedor.Nombre
                If Not String.IsNullOrWhiteSpace(proveedor.ContactoPrincipal) Then
                    texto &= " - contacto: " & proveedor.ContactoPrincipal.Trim()
                End If
            Else
                texto = "Ingreso / reposicion de stock (sin proveedor)"
            End If
            lblProveedor.Text = texto
        End Sub

        #End Region

        #Region "Manejo"

        Private Sub btnAgregar_Click(sender As Object, e As EventArgs) Handles btnAgregar.Click
            Dim producto = TryCast(cmbProducto.SelectedItem, Producto)
            If producto Is Nothing Then
                MessageBox.Show("Elegi un producto.", "Ingreso de mercaderia",
                                MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim cantidad = CInt(numCantidad.Value)
            Dim existente = lineas.FirstOrDefault(Function(d) ReferenceEquals(d.Producto, producto))
            If existente Is Nothing Then
                lineas.Add(New DetalleIngreso With {.Producto = producto, .Cantidad = cantidad})
            Else
                existente.Cantidad += cantidad
            End If
            ActualizarResumen()
        End Sub

        Private Sub btnQuitar_Click(sender As Object, e As EventArgs) Handles btnQuitar.Click
            If tabla.SelectedRows.Count = 0 Then Return
            Dim indice = tabla.SelectedRows(0).Index
            If indice < 0 OrElse indice >= lineas.Count Then Return
            lineas.RemoveAt(indice)
            ActualizarResumen()
        End Sub

        Private Sub tabla_SelectionChanged(sender As Object, e As EventArgs) Handles tabla.SelectionChanged
            btnQuitar.Enabled = tabla.SelectedRows.Count > 0
        End Sub

        Private Sub btnConfirmar_Click(sender As Object, e As EventArgs) Handles btnConfirmar.Click
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
            DialogResult = DialogResult.OK
            Close()
        End Sub

        Private Sub btnCancelar_Click(sender As Object, e As EventArgs) Handles btnCancelar.Click
            DialogResult = DialogResult.Cancel
            Close()
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

        Private Sub ActualizarResumen()
            tabla.Rows.Clear()
            For Each d In lineas
                Dim antes = d.Producto.Stock
                Dim indice = tabla.Rows.Add(d.Producto.Nombre, antes.ToString(), d.Cantidad.ToString(),
                                            (antes + d.Cantidad).ToString(), d.Observacion)
                tabla.Rows(indice).Tag = d
            Next

            lblResumen.Text = lineas.Count & " producto(s), " &
                              lineas.Sum(Function(d) d.Cantidad) & " unidad(es) a ingresar."
        End Sub

        #End Region

    End Class

End Namespace