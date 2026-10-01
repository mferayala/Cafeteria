Imports System.Drawing
Imports System.Globalization
Imports System.Linq
Imports System.Windows.Forms

Namespace CafeteriaOS

    ''' <summary>
    ''' Reportes del dia. Solo calcula y dibuja: las cifras salen de ReporteService, que
    ''' es el unico que consulta pedidos. El historial de horas y los productos mas
    ''' vendidos se dibujan con barras simples, sin depender de una libreria de graficos.
    ''' </summary>
    Public Class ReportesPanel
        Inherits PanelBase

        Private indVentas As UiKit.Indicador
        Private indPedidos As UiKit.Indicador
        Private indTicket As UiKit.Indicador
        Private indPico As UiKit.Indicador
        Private panelHoras As Panel
        Private tablaProductos As DataGridView
        Private tablaPedidos As DataGridView
        Private WithEvents chkSoloHoy As CheckBox

        Public Sub New()
            MyBase.New("Reportes", "Ventas, medios de pago y productos mas vendidos")
            ArmarLayout()
        End Sub

        Private Sub ArmarLayout()
            Dim raiz As New TableLayoutPanel With {
                .Name = "th.fondo",
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 5,
                .BackColor = Tema.BgPrincipal
            }
            raiz.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            raiz.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            raiz.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            raiz.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            raiz.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            raiz.RowStyles.Add(New RowStyle(SizeType.AutoSize))

            indVentas = New UiKit.Indicador("Ventas de hoy", "--", Tema.Verde)
            indPedidos = New UiKit.Indicador("Pedidos de hoy", "--", Tema.Azul)
            indTicket = New UiKit.Indicador("Ticket promedio", "--", Tema.Acento)
            indPico = New UiKit.Indicador("Horario pico", "--", Tema.Naranja)

            Dim indicadores As New TableLayoutPanel With {
                .Name = "th.fondo",
                .Dock = DockStyle.Fill,
                .ColumnCount = 4,
                .RowCount = 1,
                .BackColor = Color.Transparent,
                .Height = 74
            }
            For i = 0 To 3
                indicadores.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25))
            Next
            indicadores.Controls.Add(indVentas.Raiz, 0, 0)
            indicadores.Controls.Add(indPedidos.Raiz, 1, 0)
            indicadores.Controls.Add(indTicket.Raiz, 2, 0)
            indicadores.Controls.Add(indPico.Raiz, 3, 0)
            raiz.Controls.Add(indicadores, 0, 0)

            panelHoras = New Panel With {
                .Name = "th.tarjeta",
                .Dock = DockStyle.Fill,
                .BackColor = Tema.Superficie,
                .Padding = New Padding(12)
            }
            raiz.Controls.Add(panelHoras, 0, 1)

            tablaProductos = Tema.CrearTabla()
            tablaProductos.Dock = DockStyle.Fill
            UiKit.Columna(tablaProductos, "Producto", 260)
            UiKit.Columna(tablaProductos, "Unidades", 110)
            tablaProductos.Columns(0).FillWeight = 70
            tablaProductos.Columns(1).FillWeight = 30
            raiz.Controls.Add(tablaProductos, 0, 2)

            tablaPedidos = Tema.CrearTabla()
            tablaPedidos.Dock = DockStyle.Fill
            UiKit.Columna(tablaPedidos, "N.", 60)
            UiKit.Columna(tablaPedidos, "Hora", 90)
            UiKit.Columna(tablaPedidos, "Cliente", 200)
            UiKit.Columna(tablaPedidos, "Servicio", 120)
            UiKit.Columna(tablaPedidos, "Pago", 120)
            UiKit.Columna(tablaPedidos, "Estado", 120)
            UiKit.Columna(tablaPedidos, "Total", 120)
            raiz.Controls.Add(tablaPedidos, 0, 3)

            chkSoloHoy = Tema.CrearCheck("Solo pedidos de hoy")
            chkSoloHoy.Checked = True
            Dim btnExportar = Tema.CrearBoton("Exportar reporte", EstiloBoton.Primario, 160, 38)
            AddHandler btnExportar.Click, Sub(s, e) Exportar()
            raiz.Controls.Add(UiKit.Botonera(chkSoloHoy, btnExportar), 0, 4)
            Contenido.Controls.Add(raiz)
        End Sub

        Public Overrides Sub Refrescar()
            indVentas.Actualizar(ReporteService.VentasHoy().ToString("C2"))
            indPedidos.Actualizar(ReporteService.CantidadVentasHoy().ToString())
            indTicket.Actualizar(ReporteService.TicketPromedio().ToString("C2"))
            indPico.Actualizar(ReporteService.HorarioPico())

            PintarHoras()
            PintarProductos()
            PintarPedidos()

            SetearSubtitulo("Pago mas usado: " & ReporteService.PagoLider())
        End Sub

        Private Sub PintarHoras()
            panelHoras.SuspendLayout()
            While panelHoras.Controls.Count > 0
                panelHoras.Controls(0).Dispose()
            End While

            Dim porHora = ReporteService.CantidadPorHora()
            Dim titulo = New Label With {
                .Name = "th.tono.Primario",
                .Text = "Pedidos por hora (hoy)",
                .Font = Tema.Fuente(Tema.TamNormal, FontStyle.Bold),
                .ForeColor = Tema.TextoPrinc,
                .BackColor = Color.Transparent,
                .Dock = DockStyle.Top,
                .Height = 24
            }
            panelHoras.Controls.Add(titulo)

            Dim barras As New Panel With {
                .Name = "th.fondo",
                .Dock = DockStyle.Fill,
                .BackColor = Color.Transparent
            }

            If porHora.Count = 0 Then
                Dim vacio = New Label With {
                    .Name = "th.tono.Terciario",
                    .Text = "Todavia no hay pedidos cargados hoy.",
                    .Font = Tema.Fuente(Tema.TamNormal),
                    .ForeColor = Tema.TextoTer,
                    .BackColor = Color.Transparent,
                    .Dock = DockStyle.Fill,
                    .TextAlign = ContentAlignment.MiddleCenter
                }
                barras.Controls.Add(vacio)
            Else
                Dim maximo = porHora.Values.Max()
                Dim altoMaximo = Math.Max(20, barras.ClientSize.Height - 30)
                For Each kv In porHora.OrderBy(Function(k) k.Key)
                    Dim alto = If(maximo = 0, 1, CInt(altoMaximo * kv.Value / maximo))
                    Dim barra As New Panel With {
                        .Name = "th.cabecera",
                        .Width = 54,
                        .Height = Math.Max(6, alto),
                        .BackColor = Tema.Acento,
                        .Margin = New Padding(4, 0, 4, 0)
                    }
                    Dim columna As New FlowLayoutPanel With {
                        .Name = "th.fondo",
                        .FlowDirection = FlowDirection.BottomUp,
                        .WrapContents = False,
                        .AutoSize = False,
                        .Width = 62,
                        .Height = altoMaximo + 26,
                        .BackColor = Color.Transparent
                    }
                    columna.Controls.Add(barra)
                    Dim pie As New Label With {
                        .Name = "th.tono.Secundario",
                        .Text = kv.Key.ToString("00") & "h",
                        .Font = Tema.Fuente(Tema.TamMicro),
                        .ForeColor = Tema.TextoSec,
                        .BackColor = Color.Transparent,
                        .Width = 54,
                        .Height = 16,
                        .TextAlign = ContentAlignment.MiddleCenter
                    }
                    columna.Controls.Add(pie)
                    barras.Controls.Add(columna)
                Next
            End If

            panelHoras.Controls.Add(barras)
            panelHoras.ResumeLayout()
        End Sub

        Private Sub PintarProductos()
            tablaProductos.Rows.Clear()
            For Each par In ReporteService.TopProductos(15)
                tablaProductos.Rows.Add(par.Key, par.Value)
            Next
        End Sub

        Private Sub PintarPedidos()
            tablaPedidos.Rows.Clear()
            Dim pedidos = If(chkSoloHoy.Checked,
                             ReporteService.PedidosHoy(),
                             ReporteService.PedidosEntre(DateTime.Today.AddDays(-7), DateTime.Today))

            For Each p In pedidos
                tablaPedidos.Rows.Add(p.ID, p.FechaHora.ToString("HH:mm"),
                                      If(String.IsNullOrWhiteSpace(p.NombreCliente), "Mostrador", p.NombreCliente),
                                      p.TipoServicio.ToString(), p.MetodoPago.ToString(), p.Estado.ToString(),
                                      p.Total.ToString("C2", CultureInfo.CurrentCulture))
            Next
        End Sub

        Private Sub chkSoloHoy_CheckedChanged(sender As Object, e As EventArgs) Handles chkSoloHoy.CheckedChanged
            If tablaPedidos Is Nothing Then Return
            PintarPedidos()
        End Sub

        Private Sub Exportar()
            TicketService.GuardarArchivo(ReporteService.ExportarReporte(),
                "reporte_" & DateTime.Today.ToString("yyyyMMdd") & ".txt")
        End Sub
    End Class

End Namespace