Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.IO
Imports System.Linq
Imports System.Text

Namespace CafeteriaOS

    ''' <summary>
    ''' Consultas de negocio para los reportes. Separa el calculo de la presentacion: los
    ''' paneles solo pintan lo que devuelve este modulo.
    ''' </summary>
    Public Module ReporteService

        Public Function PedidosEntre(desde As DateTime, hasta As DateTime) As List(Of Pedido)
            Dim limite = hasta.Date.AddDays(1)
            Return Datos.ListaPedidos.
                Where(Function(p) p.FechaHora >= desde AndAlso p.FechaHora < limite).
                OrderBy(Function(p) p.FechaHora).
                ToList()
        End Function

        Public Function PedidosHoy() As List(Of Pedido)
            Return PedidosEntre(DateTime.Today, DateTime.Today)
        End Function

        ''' <summary>
        ''' Cuenta como venta todo pedido que no fue cancelado. El cobro y el descuento de
        ''' stock ocurren al mismo paso, cuando el pedido se registra en el punto de venta.
        ''' </summary>
        Private Function Confirmados(lista As IEnumerable(Of Pedido)) As List(Of Pedido)
            Return lista.Where(Function(p) p.Estado <> EstadoPedido.Cancelado).ToList()
        End Function

        Public Function VentasHoy() As Decimal
            Return Confirmados(PedidosHoy()).Sum(Function(p) p.Total)
        End Function

        Public Function CantidadVentasHoy() As Integer
            Return Confirmados(PedidosHoy()).Count
        End Function

        Public Function TicketPromedio() As Decimal
            Dim ventas = Confirmados(PedidosHoy())
            If ventas.Count = 0 Then Return 0D
            Return ventas.Average(Function(p) p.Total)
        End Function

        Public Function VentasPorMetodoPago() As Dictionary(Of MetodoPago, Decimal)
            Return Confirmados(PedidosHoy()).
                GroupBy(Function(p) p.MetodoPago).
                ToDictionary(Function(g) g.Key, Function(g) g.Sum(Function(p) p.Total))
        End Function

        Public Function PagoLider() As String
            Dim datos = VentasPorMetodoPago()
            If datos.Count = 0 Then Return "-"
            Return datos.OrderByDescending(Function(kv) kv.Value).First().Key.ToString()
        End Function

        Public Function TopProductos(cantidad As Integer) As List(Of KeyValuePair(Of String, Integer))
            Dim pedidos = Confirmados(PedidosHoy())
            Return pedidos.
                SelectMany(Function(p) p.Items).
                GroupBy(Function(i) i.Producto.Nombre).
                Select(Function(g) New KeyValuePair(Of String, Integer)(g.Key, g.Sum(Function(i) i.Cantidad))).
                OrderByDescending(Function(kv) kv.Value).
                Take(cantidad).
                ToList()
        End Function

        Public Function CantidadPorHora() As Dictionary(Of Integer, Integer)
            Dim porHora As New Dictionary(Of Integer, Integer)()
            For Each p In Confirmados(PedidosHoy())
                Dim h = p.FechaHora.Hour
                porHora(h) = If(porHora.ContainsKey(h), porHora(h), 0) + 1
            Next
            Return porHora
        End Function

        Public Function HorarioPico() As String
            Dim porHora = CantidadPorHora()
            If porHora.Count = 0 Then Return "-"
            Dim hora = porHora.OrderByDescending(Function(kv) kv.Value).First().Key
            Return hora.ToString("00") & ":00 - " & (hora + 1).ToString("00") & ":00"
        End Function

        Public Function ExportarReporte() As String
            Dim s As New StringBuilder()
            Dim hoy = DateTime.Today.ToString("dd/MM/yyyy")
            s.AppendLine("REPORTE DE VENTAS - " & hoy)
            s.AppendLine("====================================")
            s.AppendLine("Ventas totales:    " & VentasHoy().ToString("C"))
            s.AppendLine("Pedidos:           " & CantidadVentasHoy().ToString())
            s.AppendLine("Ticket promedio:   " & TicketPromedio().ToString("C"))
            s.AppendLine("Medio de pago:     " & PagoLider())
            For Each par In VentasPorMetodoPago().OrderByDescending(Function(kv) kv.Value)
                s.AppendLine("  " & par.Key.ToString().PadRight(14) & par.Value.ToString("C"))
            Next
            s.AppendLine("Horario pico:      " & HorarioPico())
            s.AppendLine("Egresos del dia:   " & Datos.Caja.EgresosDelDia.ToString("C"))
            s.AppendLine("Caja abierta:      " & If(Datos.Caja.Abierta, "si", "no"))
            s.AppendLine("Saldo de caja:     " & Datos.Caja.SaldoActual.ToString("C"))
            s.AppendLine()
            s.AppendLine("PEDIDOS DEL DIA")
            s.AppendLine("------------------------------------")
            For Each p In PedidosHoy()
                s.AppendLine(String.Format(CultureInfo.InvariantCulture,
                                          "#{0}  {1:HH:mm}  {2,-16}  {3,10}",
                                          p.ID, p.FechaHora, p.NombreCliente, p.Total.ToString("C")))
            Next
            s.AppendLine()
            s.AppendLine("PRODUCTOS MAS VENDIDOS")
            s.AppendLine("------------------------------------")
            For Each par In TopProductos(10)
                s.AppendLine(par.Key & ": " & par.Value)
            Next
            Return s.ToString()
        End Function

    End Module

End Namespace