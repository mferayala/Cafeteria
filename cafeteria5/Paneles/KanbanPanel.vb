Imports System.Drawing
Imports System.Globalization
Imports System.Linq
Imports System.Windows.Forms

Namespace CafeteriaOS

    ''' <summary>
    ''' Tablero de pedidos por estado. Mover un pedido a la derecha es un cambio de
    ''' estado; cancelar uno devuelve el stock que se habia descontado y da de baja la
    ''' venta correspondiente en la caja.
    ''' </summary>
    Public Class KanbanPanel
        Inherits PanelBase

        Private columnas As New Dictionary(Of EstadoPedido, ListView)()
        Private ordenes As New Dictionary(Of EstadoPedido, Label)()
        Private actual As Pedido

        Public Sub New()
            MyBase.New("Pedidos", "Estado de cada pedido en el local")
            ArmarLayout()
        End Sub

        #Region "Layout"

        Private Sub ArmarLayout()
            Dim raiz As New TableLayoutPanel With {
                .Name = "th.fondo",
                .Dock = DockStyle.Fill,
                .ColumnCount = 4,
                .RowCount = 2,
                .BackColor = Tema.BgPrincipal
            }
            For i = 0 To 3
                raiz.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25))
            Next
            raiz.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            raiz.RowStyles.Add(New RowStyle(SizeType.AutoSize))

            For i = 0 To 3
                raiz.Controls.Add(ArmarColumna(i), i, 0)
            Next

            Dim btnListo = Tema.CrearBoton("Marcar listo", EstiloBoton.Info, 130, 38)
            Dim btnEntregado = Tema.CrearBoton("Marcar entregado", EstiloBoton.Exito, 150, 38)
            Dim btnCancelar = Tema.CrearBoton("Cancelar pedido", EstiloBoton.Peligro, 140, 38)
            Dim btnCocina = Tema.CrearBoton("Ticket cocina", EstiloBoton.Neutro, 130, 38)
            Dim btnCaja = Tema.CrearBoton("Ticket caja", EstiloBoton.Neutro, 120, 38)
            Dim btnVer = Tema.CrearBoton("Ver ticket", EstiloBoton.Neutro, 120, 38)

            AddHandler btnListo.Click, Sub(s, e) Avanzar(EstadoPedido.Listo)
            AddHandler btnEntregado.Click, Sub(s, e) Avanzar(EstadoPedido.Entregado)
            AddHandler btnCancelar.Click, Sub(s, e) Cancelar()
            AddHandler btnCocina.Click, Sub(s, e) VerTicket("Cocina", AddressOf TicketService.ConstruirTicketCocina)
            AddHandler btnCaja.Click, Sub(s, e) VerTicket("Caja", AddressOf TicketService.ConstruirTicketCaja)
            AddHandler btnVer.Click, Sub(s, e) VerTicket("Cliente", AddressOf TicketService.ConstruirTicket)

            raiz.Controls.Add(UiKit.Botonera(btnListo, btnEntregado, btnCancelar, btnCocina, btnCaja, btnVer), 0, 1)
            raiz.SetColumnSpan(raiz.GetControlFromPosition(0, 1), 4)
            Contenido.Controls.Add(raiz)
        End Sub

        Private Function ArmarColumna(indice As Integer) As Control
            Dim estado = [Enum].GetValues(GetType(EstadoPedido)).Cast(Of EstadoPedido)()(indice)
            Dim color = ColorDeEstado(estado)

            Dim panel As New TableLayoutPanel With {
                .Name = "th.tarjeta",
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 2,
                .BackColor = Tema.Superficie,
                .Margin = New Padding(6),
                .Padding = New Padding(1)
            }
            panel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            panel.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            panel.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

            Dim cabecera As New Panel With {
                .Name = "th.cabecera",
                .Dock = DockStyle.Fill,
                .Height = 34,
                .BackColor = color
            }
            ordenes(estado) = New Label With {
                .Name = "th.tono.SobreAcento",
                .Text = TituloDe(estado) & " (0)",
                .Font = Tema.Fuente(Tema.TamMini, FontStyle.Bold),
                .ForeColor = Color.White,
                .BackColor = Color.Transparent,
                .Dock = DockStyle.Fill,
                .TextAlign = ContentAlignment.MiddleCenter
            }
            cabecera.Controls.Add(ordenes(estado))
            panel.Controls.Add(cabecera, 0, 0)

            Dim lista As New ListView With {
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
            lista.Columns.Add("N.", 42)
            lista.Columns.Add("Cliente", 110)
            lista.Columns.Add("Total", 80)
            lista.Columns.Add("Items", 46)
            AddHandler lista.SelectedIndexChanged, Sub(s, e) Seleccionar(lista, estado)
            columnas(estado) = lista
            panel.Controls.Add(lista, 0, 1)

            return panel
        End Function

        Private Shared Function TituloDe(estado As EstadoPedido) As String
            Select Case estado
                Case EstadoPedido.EnPreparacion : Return "En preparacion"
                Case EstadoPedido.Listo : Return "Listos"
                Case EstadoPedido.Entregado : Return "Entregados"
                Case EstadoPedido.Cancelado : Return "Cancelados"
            End Select
            Return estado.ToString()
        End Function

        Private Shared Function ColorDeEstado(estado As EstadoPedido) As Color
            Select Case estado
                Case EstadoPedido.EnPreparacion : Return Tema.Naranja
                Case EstadoPedido.Listo : Return Tema.Azul
                Case EstadoPedido.Entregado : Return Tema.Verde
                Case EstadoPedido.Cancelado : Return Tema.Rojo
            End Select
            Return Tema.Acento
        End Function

        #End Region

        #Region "Datos"

        Public Overrides Sub Refrescar()
            For Each estado In columnas.Keys.ToArray()
                Dim lista = columnas(estado)
                lista.Items.Clear()
                Dim pedidos = Datos.ListaPedidos.
                    Where(Function(p) p.Estado = estado).
                    OrderByDescending(Function(p) p.FechaHora).
                    ToList()

                For Each pedido In pedidos
                    Dim fila As New ListViewItem("#" & pedido.ID.ToString())
                    fila.SubItems.Add(If(String.IsNullOrWhiteSpace(pedido.NombreCliente), "Mostrador", pedido.NombreCliente))
                    fila.SubItems.Add(pedido.Total.ToString("C0", CultureInfo.CurrentCulture))
                    fila.SubItems.Add(pedido.Items.Count.ToString())
                    fila.Tag = pedido
                    If pedido.Estado = EstadoPedido.EnPreparacion Then
                        fila.ForeColor = Tema.Naranja
                    ElseIf pedido.Estado = EstadoPedido.Listo Then
                        fila.ForeColor = Tema.Azul
                    End If
                    lista.Items.Add(fila)
                Next

                ordenes(estado).Text = TituloDe(estado) & " (" & pedidos.Count & ")"
            Next

            Dim activos = Datos.ListaPedidos.Where(Function(p) p.Estado <> EstadoPedido.Cancelado).Count()
            SetearSubtitulo(activos & " pedido(s) activo(s)")
        End Sub

        Private Sub Seleccionar(lista As ListView, estado As EstadoPedido)
            If lista.SelectedIndices.Count = 0 Then Exit Sub
            actual = TryCast(lista.SelectedItems(0).Tag, Pedido)
        End Sub

        #End Region

        #Region "Acciones"

        Private Sub Avanzar(nuevo As EstadoPedido)
            If actual Is Nothing Then
                Avisar("Elegi un pedido del tablero.")
                Return
            End If
            If actual.Estado = EstadoPedido.Cancelado Then
                Avisar("Un pedido cancelado no se puede volver a mover.")
                Return
            End If

            actual.Estado = nuevo
            If nuevo = EstadoPedido.Entregado Then
                actual.MotivoCancelacion = String.Empty
            End If
            Refrescar()
        End Sub

        Private Sub Cancelar()
            If actual Is Nothing Then
                Avisar("Elegi un pedido del tablero.")
                Return
            End If
            If actual.Estado = EstadoPedido.Cancelado Then
                Avisar("Ese pedido ya estaba cancelado.")
                Return
            End If

            Dim motivo = InputBox("Motivo de la cancelacion del pedido N. " & actual.ID,
                                  "Cancelar pedido", "Devolucion")
            If motivo Is Nothing Then Return

            ' Un pedido confirmado habia descontado stock y habia registrado su venta: se
            ' revierte todo. Si nunca se confirmo, StockService.Restaurar no hace nada.
            StockService.Restaurar(actual)
            CajaService.RevertirVenta(actual)

            actual.Estado = EstadoPedido.Cancelado
            actual.MotivoCancelacion = motivo
            Refrescar()
        End Sub

        Private Sub VerTicket(titulo As String, constructor As Func(Of Pedido, String))
            If actual Is Nothing Then
                Avisar("Elegi un pedido del tablero.")
                Return
            End If
            Using f As New TicketForm(constructor(actual), "Ticket de " & titulo.ToLowerInvariant() &
                                       " - pedido N. " & actual.ID)
                f.ShowDialog()
            End Using
        End Sub

        Private Sub Avisar(mensaje As String)
            MessageBox.Show(mensaje, "Pedidos", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Sub

        #End Region
    End Class

End Namespace