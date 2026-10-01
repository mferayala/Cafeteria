Imports System.Collections.Generic

Namespace CafeteriaOS

    Public Class DetallePedido
        Public Property Producto As Producto
        Public Property Cantidad As Integer

        ''' <summary>
        ''' Texto ya resuelto de las especificaciones elegidas ("Leche: Entera | Azucar: Normal").
        ''' </summary>
        Public Property Especificaciones As String

        Public ReadOnly Property Subtotal As Decimal
            Get
                Return Producto.Precio * Cantidad
            End Get
        End Property
    End Class

    Public Class Pedido
        Public Property ID As Integer
        Public Property NombreCliente As String
        Public Property TelefonoCliente As String
        Public Property TipoServicio As TipoServicio
        Public Property Mesa As String
        Public Property Items As New List(Of DetallePedido)()
        Public Property Estado As EstadoPedido
        Public Property MetodoPago As MetodoPago
        Public Property FechaHora As DateTime = DateTime.Now
        Public Property Descuento As Decimal
        Public Property UsuarioResponsable As String
        Public Property MotivoCancelacion As String

        ''' <summary>
        ''' Marca que a este pedido ya se le desconto stock al confirmarse. La deja en True
        ''' StockService.Descontar y en False StockService.Restaurar, para que restaurar dos
        ''' veces el mismo pedido no sume unidades de mas.
        ''' </summary>
        Public Property StockDescontado As Boolean

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

        Public ReadOnly Property EsDeLocal As Boolean
            Get
                Return TipoServicio = TipoServicio.EnElLocal
            End Get
        End Property
    End Class

End Namespace