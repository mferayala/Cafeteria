Imports System.Drawing
Imports System.Globalization
Imports System.Linq
Imports System.Windows.Forms

Namespace CafeteriaOS

    ''' <summary>
    ''' Consulta de los movimientos de stock: de donde salio y a donde fue cada unidad.
    '''
    ''' El historial de ingresos alcanza para ubicar de que proveedor vino una
    ''' mercaderia. Para saber por que el deposito esta como esta no: falta saber que
    ''' se vendio, que se ajusto a mano y que se devolvio. Esta pantalla cierra eso, y
    ''' por eso mira todos los tipos de movimiento y no solo los ingresos.
    '''
    ''' Es de solo lectura. Corregir el stock se hace desde Stock, que deja el
    ''' movimiento correspondiente; aca el asiento no se toca.
    ''' </summary>
    Partial Class MovimientosStockForm
        Inherits Form

        Private WithEvents txtBuscar As TextBox
        Private WithEvents cmbTipo As ComboBox
        Private WithEvents dtpDesde As DateTimePicker
        Private WithEvents dtpHasta As DateTimePicker
        Private WithEvents chkSoloEsteProducto As CheckBox
        Private WithEvents tabla As DataGridView
        Private WithEvents btnCerrar As Button

        Private lblResumen As Label
        Private productoFijado As Producto

        ''' <summary>
        ''' Historial completo. Se puede abrir acotado a un producto, que es como se
        ''' llega desde el panel de stock.
        ''' </summary>
        Public Sub New(Optional producto As Producto = Nothing)
            productoFijado = producto
            ArmarInterfaz()
            CargarTipos()
            CargarMovimientos()
        End Sub

        Public Shared Function Ver(Optional producto As Producto = Nothing) As Boolean
            Using f As New MovimientosStockForm(producto)
                f.ShowDialog()
            End Using
            Return True
        End Function

        Private Sub ArmarInterfaz()
            Text = If(productoFijado Is Nothing,
                      "Movimientos de stock", "Movimientos de " & productoFijado.NombreCompleto())
            StartPosition = FormStartPosition.CenterParent
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ClientSize = New Size(1080, 640)
            ' Sin esto WinForms escala con los valores por defecto y no con los que
            ' escribe el Designer: a 125% o 150% la fuente crece y las columnas no,
            ' y los labels quedan cortados.
            AutoScaleMode = AutoScaleMode.Dpi
            AutoScaleDimensions = New SizeF(96, 96)
            BackColor = Tema.BgPrincipal
            Font = Tema.Fuente(Tema.TamNormal)

            Dim raiz As New TableLayoutPanel With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 4,
                .BackColor = Tema.BgPrincipal,
                .Padding = New Padding(16)
            }
            raiz.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            raiz.RowStyles.Add(New RowStyle(SizeType.Absolute, 46))
            raiz.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            raiz.RowStyles.Add(New RowStyle(SizeType.Absolute, 26))
            raiz.RowStyles.Add(New RowStyle(SizeType.Absolute, 44))

            raiz.Controls.Add(ArmarFiltros(), 0, 0)
            raiz.Controls.Add(ArmarTabla(), 0, 1)
            raiz.Controls.Add(ArmarResumen(), 0, 2)
            raiz.Controls.Add(ArmarAcciones(), 0, 3)

            Controls.Add(raiz)
        End Sub

        Private Function ArmarFiltros() As Control
            txtBuscar = UiKit.CampoBusqueda("Buscar por producto u observacion")
            txtBuscar.Dock = DockStyle.Fill
            txtBuscar.AccessibleName = "Buscar movimiento"
            AddHandler txtBuscar.TextChanged, Sub() CargarMovimientos()

            cmbTipo = Tema.CrearCombo()
            cmbTipo.Dock = DockStyle.Fill
            cmbTipo.DropDownStyle = ComboBoxStyle.DropDownList
            AddHandler cmbTipo.SelectedIndexChanged, Sub() CargarMovimientos()

            dtpDesde = New DateTimePicker With {
                .Dock = DockStyle.Fill,
                .Format = DateTimePickerFormat.Short,
                .CustomFormat = "dd/MM/yyyy"
            }
            dtpHasta = New DateTimePicker With {
                .Dock = DockStyle.Fill,
                .Format = DateTimePickerFormat.Short,
                .CustomFormat = "dd/MM/yyyy",
                .Value = DateTime.Today
            }
            AddHandler dtpDesde.ValueChanged, Sub() CargarMovimientos()
            AddHandler dtpHasta.ValueChanged, Sub() CargarMovimientos()

            chkSoloEsteProducto = Tema.CrearCheck("Solo este producto")
            chkSoloEsteProducto.Enabled = productoFijado IsNot Nothing
            chkSoloEsteProducto.Checked = productoFijado IsNot Nothing
            AddHandler chkSoloEsteProducto.CheckedChanged, Sub() CargarMovimientos()

            Dim host As New TableLayoutPanel With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 7,
                .RowCount = 1,
                .BackColor = Color.Transparent
            }
            host.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            host.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 8))
            host.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 170))
            host.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 62))
            host.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 132))
            host.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 132))
            host.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 200))

            host.Controls.Add(txtBuscar, 0, 0)
            host.Controls.Add(cmbTipo, 2, 0)
            host.Controls.Add(Etiqueta("Desde"), 3, 0)
            host.Controls.Add(dtpDesde, 4, 0)
            host.Controls.Add(dtpHasta, 5, 0)
            host.Controls.Add(chkSoloEsteProducto, 6, 0)
            Return host
        End Function

        Private Function ArmarTabla() As Control
            tabla = Tema.CrearTabla()
            tabla.Dock = DockStyle.Fill
            tabla.ReadOnly = True
            tabla.AllowUserToAddRows = False
            tabla.AllowUserToDeleteRows = False
            tabla.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            UiKit.Columna(tabla, "Cuando", 140)
            UiKit.Columna(tabla, "Producto", 260)
            UiKit.Columna(tabla, "Movimiento", 130)
            UiKit.Columna(tabla, "Cantidad", 90)
            UiKit.Columna(tabla, "Origen", 120)
            UiKit.Columna(tabla, "Referencia", 100)
            UiKit.Columna(tabla, "Quien", 130)
            UiKit.Columna(tabla, "Detalle", 220)
            tabla.Columns(0).FillWeight = 14
            tabla.Columns(1).FillWeight = 26
            tabla.Columns(2).FillWeight = 12
            tabla.Columns(3).FillWeight = 8
            tabla.Columns(4).FillWeight = 12
            tabla.Columns(5).FillWeight = 10
            tabla.Columns(6).FillWeight = 12
            tabla.Columns(7).FillWeight = 20
            Return tabla
        End Function

        Private Function ArmarResumen() As Control
            lblResumen = Tema.CrearLabel("", Tono.Secundario, Tema.TamMini)
            lblResumen.Dock = DockStyle.Fill
            lblResumen.AutoSize = False
            lblResumen.TextAlign = ContentAlignment.MiddleLeft
            Return lblResumen
        End Function

        Private Function ArmarAcciones() As Control
            btnCerrar = Tema.CrearBoton("Cerrar", EstiloBoton.Neutro, 130, 36)
            AddHandler btnCerrar.Click, Sub() Close()

            Dim acciones As New FlowLayoutPanel With {
                .Dock = DockStyle.Fill,
                .FlowDirection = FlowDirection.RightToLeft,
                .WrapContents = False,
                .BackColor = Color.Transparent
            }
            acciones.Controls.Add(btnCerrar)
            Return acciones
        End Function

        Private Function Etiqueta(texto As String) As Label
            Return New Label With {
                .Text = texto,
                .Dock = DockStyle.Fill,
                .TextAlign = ContentAlignment.MiddleLeft
            }
        End Function

        Private Sub CargarTipos()
            cmbTipo.Items.Clear()
            cmbTipo.Items.Add("(todos)")
            For Each t In [Enum].GetValues(GetType(TipoMovimientoStock))
                cmbTipo.Items.Add(t.ToString())
            Next
            cmbTipo.SelectedIndex = 0
        End Sub

        Private Sub CargarMovimientos()
            If tabla Is Nothing Then Return

            Dim texto = If(txtBuscar.Text, "").Trim().ToLowerInvariant()
            Dim tipo = If(txtTipoElegido(), "")
            Dim desde = dtpDesde.Value.Date
            Dim hasta = dtpHasta.Value.Date

            Dim filtrados = Datos.ListaMovimientos.
                Where(Function(m) m.Fecha.Date >= desde AndAlso m.Fecha.Date <= hasta).
                Where(Function(m) tipo.Length = 0 OrElse
                               m.Tipo.ToString().Equals(tipo, StringComparison.CurrentCultureIgnoreCase)).
                Where(Function(m) CumpleFiltro(m, texto)).
                OrderByDescending(Function(m) m.Fecha).
                ThenByDescending(Function(m) m.ID).
                ToList()

            tabla.Rows.Clear()
            For Each m In filtrados
                Dim suma = If(m.Cantidad > 0, "+" & m.Cantidad.ToString(),
                              m.Cantidad.ToString())
                Dim referencia = If(m.ReferenciaID > 0,
                                    m.Origen & " N. " & m.ReferenciaID.ToString(), "")
                Dim indice = tabla.Rows.Add(m.Fecha.ToString("dd/MM/yyyy HH:mm"),
                                            If(m.Producto Is Nothing, "(producto eliminado)",
                                               m.Producto.NombreCompleto()),
                                            m.Tipo.ToString(), suma,
                                            If(m.Origen, ""), referencia,
                                            If(m.Usuario, ""), If(m.Observacion, ""))
                tabla.Rows(indice).Tag = m
                tabla.Rows(indice).DefaultCellStyle.ForeColor = ColorDeMovimiento(m.Tipo)
            Next

            ActualizarResumen(filtrados)
        End Sub

        ''' <summary>
        ''' Decide si un movimiento entra en la lista. Va aparte de la consulta para
        ''' no encadenar condiciones dentro del Where: queda mas legible y se puede
        ''' verificar por partes.
        ''' </summary>
        Private Function CumpleFiltro(m As MovimientoStock, texto As String) As Boolean
            If chkSoloEsteProducto.Checked AndAlso productoFijado IsNot Nothing Then
                If Not ReferenceEquals(m.Producto, productoFijado) Then Return False
            End If

            If texto.Length = 0 Then Return True

            Dim nombreProducto = If(m.Producto Is Nothing, "", m.Producto.NombreCompleto())
            Return nombreProducto.ToLowerInvariant().Contains(texto) OrElse
                   If(m.Observacion, "").ToLowerInvariant().Contains(texto) OrElse
                   If(m.Origen, "").ToLowerInvariant().Contains(texto)
        End Function

        Private Sub ActualizarResumen(movimientos As List(Of MovimientoStock))
            Dim entradas = movimientos.Where(Function(m) m.Cantidad > 0).Sum(Function(m) m.Cantidad)
            Dim salidas = -movimientos.Where(Function(m) m.Cantidad < 0).Sum(Function(m) m.Cantidad)

            lblResumen.Text = movimientos.Count & " movimiento(s) entre el " &
                              dtpDesde.Value.ToString("dd/MM/yyyy") & " y el " &
                              dtpHasta.Value.ToString("dd/MM/yyyy") & ". " &
                              "Entraron " & entradas & " unidad(es), salieron " & salidas & "."
        End Sub

        ''' <summary>
        ''' El color dice de que lado esta el movimiento: verde lo que suma, rojo lo
        ''' que resta. Es una lectura rapida, no el unico dato: la columna Cantidad
        ''' lleva el signo.
        ''' </summary>
        Private Function ColorDeMovimiento(tipo As TipoMovimientoStock) As Color
            Select Case tipo
                Case TipoMovimientoStock.Venta
                    Return Tema.Rojo
                Case TipoMovimientoStock.Cancelacion,
                     TipoMovimientoStock.Devolucion
                    Return Tema.Naranja
                Case Else
                    Return Tema.TextoPrinc
            End Select
        End Function

        Private Function txtTipoElegido() As String
            Dim valor = If(cmbTipo.SelectedItem, "").ToString().Trim()
            If valor = "(todos)" Then Return ""
            Return valor
        End Function

    End Class

End Namespace
