Imports System.Drawing
Imports System.Globalization
Imports System.Linq
Imports System.Windows.Forms

Namespace CafeteriaOS

    ''' <summary>
    ''' Historico de clientes. El registro se crea solo cuando se cobra un pedido, en el
    ''' punto de venta; este modulo consulta, filtra y muestra los pedidos de cada cliente.
    ''' </summary>
    Public Class ClientesPanel
        Inherits PanelBase

        Private WithEvents txtBuscar As TextBox
        Private tabla As DataGridView
        Private detalle As ListView
        Private lblDetalle As Label

        Public Sub New()
            MyBase.New("Clientes", "Historial de compras")
            ArmarLayout()
        End Sub

        Private Sub ArmarLayout()
            Dim raiz As New TableLayoutPanel With {
                .Name = "th.fondo",
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 3,
                .BackColor = Tema.BgPrincipal
            }
            raiz.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            raiz.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            raiz.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            raiz.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

            txtBuscar = Tema.CrearInput()
            txtBuscar.Dock = DockStyle.Fill
            txtBuscar.AccessibleName = "Buscar cliente"
            raiz.Controls.Add(txtBuscar, 0, 0)

            tabla = Tema.CrearTabla()
            tabla.Dock = DockStyle.Fill
            UiKit.Columna(tabla, "Cliente", 200)
            UiKit.Columna(tabla, "Telefono", 130)
            UiKit.Columna(tabla, "Pedidos", 90)
            UiKit.Columna(tabla, "Total gasto", 130)
            UiKit.Columna(tabla, "Pago habitual", 130)
            UiKit.Columna(tabla, "Ultimo pedido", 140)
            tabla.Columns(0).FillWeight = 30
            tabla.Columns(1).FillWeight = 18
            tabla.Columns(2).FillWeight = 12
            tabla.Columns(3).FillWeight = 18
            tabla.Columns(4).FillWeight = 14
            tabla.Columns(5).FillWeight = 18
            AddHandler tabla.SelectionChanged, Sub(s, e) PintarDetalle()
            raiz.Controls.Add(tabla, 0, 1)

            lblDetalle = Tema.CrearLabel("Elegi un cliente para ver su historial.", Tono.Secundario, Tema.TamMini)
            lblDetalle.Dock = DockStyle.Fill
            lblDetalle.AutoSize = False
            lblDetalle.Height = 22
            lblDetalle.TextAlign = ContentAlignment.MiddleLeft
            raiz.Controls.Add(lblDetalle, 0, 2)

            detalle = New ListView With {
                .Name = "th.lista",
                .Dock = DockStyle.Fill,
                .View = View.Details,
                .FullRowSelect = True,
                .HeaderStyle = ColumnHeaderStyle.Nonclickable,
                .BorderStyle = BorderStyle.None,
                .BackColor = Tema.Superficie2,
                .ForeColor = Tema.TextoPrinc,
                .Font = Tema.Fuente(Tema.TamMini)
            }
            detalle.Columns.Add("Pedido", 80)
            detalle.Columns.Add("Fecha", 150)
            detalle.Columns.Add("Total", 110)
            detalle.Columns.Add("Pago", 120)
            detalle.Columns.Add("Estado", 120)
            AddHandler detalle.ClientSizeChanged,
                Sub(s, e) UiKit.EstirarColumnas(detalle, 1)
            UiKit.EstirarColumnas(detalle, 1)
            raiz.Controls.Add(detalle, 0, 3)
            Contenido.Controls.Add(raiz)
        End Sub

        Public Overrides Sub Refrescar()
            tabla.Rows.Clear()
            Dim clientes = ClienteService.Buscar(txtBuscar.Text).
                OrderByDescending(Function(c) c.UltimoPedido)

            For Each c In clientes
                Dim indice = tabla.Rows.Add(c.Nombre, c.Telefono, c.CantidadPedidos,
                                            c.TotalGastado.ToString("C2", CultureInfo.CurrentCulture),
                                            c.MetodoPagoHabitual.ToString(),
                                            If(c.UltimoPedido = DateTime.MinValue, "-", c.UltimoPedido.ToString("dd/MM/yyyy")))
                tabla.Rows(indice).Tag = c
            Next

            SetearSubtitulo(clientes.Count & " cliente(s) registrado(s)")
        End Sub

        Private Sub PintarDetalle()
            detalle.Items.Clear()
            Dim cliente = ClienteSeleccionado()
            If cliente Is Nothing Then
                lblDetalle.Text = "Elegi un cliente para ver su historial."
                return
            End If

            lblDetalle.Text = cliente.Nombre & " (" & cliente.Telefono & ") - " &
                              cliente.CantidadPedidos & " pedido(s), " &
                              cliente.TotalGastado.ToString("C2")

            For Each id In cliente.HistorialPedidos
                Dim pedido = Datos.ListaPedidos.FirstOrDefault(Function(p) p.ID = id)
                If pedido Is Nothing Then Continue For
                Dim fila As New ListViewItem("#" & pedido.ID.ToString())
                fila.SubItems.Add(pedido.FechaHora.ToString("dd/MM/yyyy HH:mm"))
                fila.SubItems.Add(pedido.Total.ToString("C2", CultureInfo.CurrentCulture))
                fila.SubItems.Add(pedido.MetodoPago.ToString())
                fila.SubItems.Add(pedido.Estado.ToString())
                detalle.Items.Add(fila)
            Next
        End Sub

        Private Function ClienteSeleccionado() As ClienteRegistrado
            If tabla.SelectedRows.Count = 0 Then Return Nothing
            Return TryCast(tabla.SelectedRows(0).Tag, ClienteRegistrado)
        End Function

        Private Sub txtBuscar_TextChanged(sender As Object, e As EventArgs) Handles txtBuscar.TextChanged
            If tabla Is Nothing Then Return
            Refrescar()
        End Sub
    End Class

End Namespace