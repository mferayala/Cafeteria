Imports System
Imports System.Collections.Generic
Imports System.Linq

Namespace CafeteriaOS

    Public Class MovimientoCaja
        Public Property Tipo As String
        Public Property Monto As Decimal
        Public Property Descripcion As String
        Public Property FechaHora As DateTime = DateTime.Now
        Public Property Usuario As String

        ''' <summary>
        ''' Venta que originó el movimiento, cuando lo hubo. Cero para apertura, egreso
        ''' y cierre.
        '''
        ''' Se guarda el id y no solo la Descripcion porque el texto es para que lo lea
        ''' una persona: dos ventas pueden coincidir en la descripcion y entonces el
        ''' movimiento de una termina revocando el de la otra al anular. Con el id el
        ''' vinculo es exacto.
        ''' </summary>
        Public Property VentaID As Integer

        Public Const TipoApertura As String = "Apertura"
        Public Const TipoVenta As String = "Venta"
        Public Const TipoEgreso As String = "Egreso"
        Public Const TipoCierre As String = "Cierre"
    End Class

    ''' <summary>
    ''' Caja de la sesion. Los movimientos pertenecen a una unica apertura: al reabrir se
    ''' descartan los de la sesion anterior para que el saldo refleje solo la caja abierta.
    ''' </summary>
    Public Class SesionCaja
        Public Property Abierta As Boolean
        Public Property MontoInicial As Decimal
        Public Property FechaApertura As DateTime
        Public Property UsuarioApertura As String
        Public Property Movimientos As New List(Of MovimientoCaja)()

        Public ReadOnly Property TotalVentas As Decimal
            Get
                Return Movimientos.Where(Function(m) m.Tipo = MovimientoCaja.TipoVenta).Sum(Function(m) m.Monto)
            End Get
        End Property

        Public ReadOnly Property TotalEgresos As Decimal
            Get
                Return Movimientos.Where(Function(m) m.Tipo = MovimientoCaja.TipoEgreso).Sum(Function(m) m.Monto)
            End Get
        End Property

        Public ReadOnly Property EgresosDelDia As Decimal
            Get
                Return Movimientos.
                    Where(Function(m) m.Tipo = MovimientoCaja.TipoEgreso AndAlso m.FechaHora.Date = Date.Today).
                    Sum(Function(m) m.Monto)
            End Get
        End Property

        Public ReadOnly Property SaldoActual As Decimal
            Get
                Return MontoInicial + TotalVentas - TotalEgresos
            End Get
        End Property
    End Class

End Namespace