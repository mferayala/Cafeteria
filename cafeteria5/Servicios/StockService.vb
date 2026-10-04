Imports System.Collections.Generic
Imports System.Linq

Namespace CafeteriaOS

    ''' <summary>
    ''' Regla unica de stock del sistema.
    '''
    ''' El stock SOLO cambia en tres momentos:
    '''   1. al confirmar un pedido (descuenta),
    '''   2. al cancelar un pedido ya confirmado (restaura),
    '''   3. desde el modulo de stock (ajuste manual).
    '''
    ''' Armar o descartar un pedido que aun no se confirmo nunca toca el stock, por eso
    ''' cancelar un pedido en curso no suma unidades de más.
    '''
    ''' Descontar y restaurar reciben el Pedido, no la lista de items, porque el pedido
    ''' guarda la marca de si su stock ya fue descontado. Sin esa marca, cancelar dos veces
    ''' el mismo pedido devolvia el doble.
    ''' </summary>
    Public Module StockService

        Public Const UmbralCritico As Integer = 10

        Public Function Solicitado(items As IEnumerable(Of DetallePedido), producto As Producto) As Integer
            If items Is Nothing Then Return 0
            Return items.Where(Function(i) i.Producto.ID = producto.ID).
                         Sum(Function(i) i.Cantidad)
        End Function

        ''' <summary>Unidades que todavia se pueden agregar al pedido en curso.</summary>
        Public Function Disponible(items As IEnumerable(Of DetallePedido), producto As Producto) As Integer
            Return Math.Max(0, producto.Stock - Solicitado(items, producto))
        End Function

        Public Function PuedeAgregar(items As IEnumerable(Of DetallePedido), producto As Producto, cantidad As Integer) As Boolean
            Return producto.Stock - Solicitado(items, producto) >= cantidad
        End Function

        ''' <summary>
        ''' Aplicar al confirmar. Deja el stock en el valor real pedido y deja el pedido
        ''' marcado como descontado, para que un cancel posterior sepa que hay que devolver
        ''' esas unidades.
        ''' </summary>
        Public Sub Descontar(pedido As Pedido)
            For Each item In pedido.Items
                item.Producto.Stock = Math.Max(0, item.Producto.Stock - item.Cantidad)
            Next
            Try
                MovimientoStockService.RegistrarVenta(pedido)
            Catch
            End Try
            pedido.StockDescontado = True
        End Sub

        ''' <summary>
        ''' Aplicar al cancelar un pedido que ya habia descontado stock. Si el pedido nunca
        ''' se desconto no hace nada: armar y descartar un pedido en curso no toca el stock.
        ''' Cancelar dos veces tampoco suma unidades de mas, porque el pedido queda sin la
        ''' marca de descuento.
        ''' </summary>
        Public Sub Restaurar(pedido As Pedido)
            If Not pedido.StockDescontado Then Return
            For Each item In pedido.Items
                item.Producto.Stock += item.Cantidad
            Next
            Try
                MovimientoStockService.RegistrarCancelacion(pedido)
            Catch
            End Try
            pedido.StockDescontado = False
        End Sub

        Public Sub Ajustar(producto As Producto, delta As Integer, Optional usuario As String = Nothing, Optional obs As String = Nothing)
            If producto Is Nothing OrElse delta = 0 Then Return
            producto.Stock = Math.Max(0, producto.Stock + delta)
            Try
                MovimientoStockService.RegistrarAjuste(producto, delta, usuario, obs)
            Catch
            End Try
        End Sub

        Public Function Criticos() As List(Of Producto)
            Return Datos.ListaProductos.Where(Function(p) p.Stock <= UmbralCritico).ToList()
        End Function

        Public Function EstaAgotado(producto As Producto) As Boolean
            Return producto.Stock <= 0
        End Function

    End Module

End Namespace