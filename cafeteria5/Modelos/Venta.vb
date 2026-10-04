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
    ''' Si la venta esta pagada o todavia le falta dinero.
    '''
    ''' Es un eje APARTE del estado de la venta, y esa separacion es a proposito. Que una
    ''' venta este pagada y que la mercaderia este lista son dos cosas sin relacion: un
    ''' cliente que paga y se va tiene una venta pagada que no pasa por ninguna
    ''' preparacion, y una venta fiada puede estar ya entregada y aun debiendo.
    '''
    ''' Por eso esto no va dentro de EstadoVenta: una venta anulada esta anulada haya sido
    ''' pagada o no, y el estado de pago es lo que se settles despues con un pago nuevo.
    ''' </summary>
    Public Enum EstadoPago
        Pagada
        Pendiente
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

        ''' <summary>
        ''' Si esta pagada o fiada. Una venta a cuenta sigue confirmada y descuenta stock igual:
        ''' la mercaderia sale del deposito, lo que falta es que la plata entre.
        ''' </summary>
        Public Property Pago As EstadoPago = EstadoPago.Pagada

        ''' <summary>
        ''' Que esta venta se armo a cuenta, se haya saldado o no.
        '''
        ''' Se guarda aparte del estado de pago porque hace falta para anular: si a una
        ''' venta a cuenta ya le Pagaron una parte, anularla no puede devolverse sola,
        ''' porque hay que decidir que pasa con esa plata. Sin este dato no se podria
        ''' distinguir "nunca entro plata" de "entro y hay que devolverla".
        ''' </summary>
        Public Property FueACuenta As Boolean

        ''' Cuanto se ha cobrado de esta venta. Va sumando con cada pago posterior, asi que una
        ''' deuda se puede saldar en partes sin perder el rastro de cuanto se puso cada vez.
        ''' </summary>
        Public Property MontoPagado As Decimal

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

        ''' <summary>Cuanto le queda deber al cliente de esta venta. Cero si esta pagada.</summary>
        Public ReadOnly Property Saldo As Decimal
            Get
                Return Math.Max(0D, Total - MontoPagado)
            End Get
        End Property

        Public ReadOnly Property EstaPagada As Boolean
            Get
                Return Pago = EstadoPago.Pagada OrElse Saldo <= 0D
            End Get
        End Property

        ''' <summary>Si se puede intentar cobrar algo mas contra esta venta.</summary>
        Public ReadOnly Property TieneSaldo As Boolean
            Get
                Return Not EstaAnulada AndAlso Saldo > 0D
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
