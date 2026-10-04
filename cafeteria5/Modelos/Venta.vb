Imports System.Collections.Generic

Namespace CafeteriaOS

    ''' <summary>
    ''' Estado de una venta. No tiene estado "en curso": una venta o esta confirmada
    ''' o esta anulada. Lo que esta en curso es un Pedido, que todavia no es un hecho
    ''' economico y por eso no toca stock ni caja.
    ''' </summary>
    Public Enum EstadoVenta
        Confirmada
        Anulada
    End Enum

    ''' <summary>
    ''' Una linea de venta CONGELADA.
    '''
    ''' Guarda el nombre y el precio del producto en el momento de la venta, no una
    ''' referencia al producto vivo. Sin este snapshot, editar el precio de un producto
    ''' cambiaba el importe de todas las ventas anteriores que lo contenian, y el
    ''' historial dejaba de coincidir con la plata que efectivamente entró.
    '''
    ''' El Producto se conserva igual, pero solo para poder devolver las unidades si la
    ''' venta se anula: para calcular importes nunca se lee de ahí.
    ''' </summary>
    Public Class DetalleVenta
        Public Property Producto As Producto
        Public Property ProductoID As Integer
        Public Property ProductoNombre As String
        Public Property Cantidad As Integer
        Public Property PrecioUnitario As Decimal
        Public Property Especificaciones As String

        Public ReadOnly Property Subtotal As Decimal
            Get
                Return PrecioUnitario * Cantidad
            End Get
        End Property
    End Class

    ''' <summary>
    ''' Hecho economico confirmado: lo que salio del negocio, por cuanto y como se pago.
    '''
    ''' Es distinta del Pedido a proposito. El Pedido es el borrador que se esta armando y
    ''' se puede cambiar, cancelar o descartar sin dejar rastro. La Venta no se edita:
    ''' una vez confirmada queda como quedo, y si algo sale mal se ANULA, que es una
    ''' operacion nueva que deja la anulacion asentada en vez de borrar el original.
    '''
    ''' Esta separacion es la que permite que el punto de venta sea independiente del
    ''' tablero de pedidos: una venta en mostrador no tiene por que pasar por ningun
    ''' estado de preparacion.
    ''' </summary>
    Public Class Venta
        Public Property ID As Integer
        Public Property Numero As String
        Public Property FechaHora As DateTime = DateTime.Now

        Public Property ClienteNombre As String
        Public Property ClienteTelefono As String
        Public Property TipoServicio As TipoServicio
        Public Property Mesa As String

        Public Property Items As New List(Of DetalleVenta)()
        Public Property Descuento As Decimal
        Public Property MetodoPago As MetodoPago

        ''' <summary>Efectivo entregado, solo cuando el pago fue en efectivo.</summary>
        Public Property EfectivoRecibido As Decimal

        Public Property Usuario As String
        Public Property Estado As EstadoVenta = EstadoVenta.Confirmada

        ''' <summary>
        ''' Marca que a esta venta ya se le desconto stock. La deja en True al confirmar
        ''' y en False al anular, para que anular dos veces no devuelva unidades de mas.
        ''' </summary>
        Public Property StockDescontado As Boolean

        Public Property AnuladaFecha As DateTime?
        Public Property AnuladaMotivo As String
        Public Property AnuladaUsuario As String

        Public ReadOnly Property Subtotal As Decimal
            Get
                Dim s As Decimal = 0D
                For Each i In Items
                    s += i.Subtotal
                Next
                Return s
            End Get
        End Property

        Public ReadOnly Property Total As Decimal
            Get
                Return Math.Max(0D, Subtotal - Descuento)
            End Get
        End Property

        Public ReadOnly Property Cambio As Decimal
            Get
                Return Math.Max(0D, EfectivoRecibido - Total)
            End Get
        End Property

        Public ReadOnly Property EstaAnulada As Boolean
            Get
                Return Estado = EstadoVenta.Anulada
            End Get
        End Property

        Public ReadOnly Property EfectivoComoMetodo As Boolean
            Get
                Return MetodoPago = MetodoPago.Efectivo
            End Get
        End Property

        Public ReadOnly Property EsDeLocal As Boolean
            Get
                Return TipoServicio = TipoServicio.EnElLocal
            End Get
        End Property
    End Class

End Namespace
