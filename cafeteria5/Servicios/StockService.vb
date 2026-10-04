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

        ''' <summary>
        ''' Suma o resta unidades dejando un movimiento con el tipo que declara la
        ''' intencion de la operacion. El tipo es obligatorio a proposito: este es el
        ''' unico punto del sistema que registra aumentos y disminuciones manuales, asi
        ''' que no puede quedar una operacion sin asentar.
        '''
        ''' Antes cada pantalla eligia el tipo por su cuenta y ademas registraba por su
        ''' lado, de modo que el ingreso y la reposicion dejaban dos movimientos por la
        ''' misma operacion. Acá hay un solo asiento por operacion, con su tipo real.
        ''' </summary>
        Public Sub Ajustar(producto As Producto,
                           delta As Integer,
                           tipo As TipoMovimientoStock,
                           Optional usuario As String = Nothing,
                           Optional observacion As String = Nothing,
                           Optional referenciaID As Integer = 0)

            If producto Is Nothing OrElse delta = 0 Then Return

            Dim antes = producto.Stock
            producto.Stock = Math.Max(0, antes + delta)

            ' Si el stock estaba en 0 y se_restan 5, quedan 0 y no -5: el movimiento
            ' tiene que decir cuanto cambio el deposito de verdad, o el historial
            ' suma unidades que nunca entraron.
            Dim aplicado = producto.Stock - antes
            If aplicado = 0 Then Return

            Try
                MovimientoStockService.Registrar(producto, aplicado, tipo,
                                                 OrigenDe(tipo), referenciaID,
                                                 usuario, observacion)
            Catch
            End Try
        End Sub

        ''' <summary>Modulo que origina cada tipo, para poder filtrar el historial.</summary>
        Public Function OrigenDe(tipo As TipoMovimientoStock) As String
            Select Case tipo
                Case TipoMovimientoStock.Ingreso : Return "IngresoStock"
                Case TipoMovimientoStock.Reposicion : Return "Reposicion"
                Case TipoMovimientoStock.Ajuste : Return "StockPanel"
                Case Else : Return "Sistema"
            End Select
        End Function

        ''' <summary>
        ''' Minimo de stock de un producto.
        '''
        ''' Cada producto tiene el suyo porque el deposito de un paquete de servidores y
        ''' el de una bolsa de pan no se comparan: 10 unidades es critico para uno y
        ''' es mucho para el otro. El umbral global queda como piso, para que un
        ''' producto recien cargado sin minimo siga teniendo uno razonable en vez de
        ''' aparecer siempre disponible.
        ''' </summary>
        Public Function MinimoDe(p As Producto) As Integer
            If p Is Nothing Then Return 0
            Return If(p.StockMinimo > 0, p.StockMinimo, UmbralCritico)
        End Function

        Public Function Criticos() As List(Of Producto)
            Return Datos.ListaProductos.
                Where(Function(p) p.Stock <= MinimoDe(p)).
                ToList()
        End Function

        Public Function EstaAgotado(producto As Producto) As Boolean
            Return producto IsNot Nothing AndAlso producto.Stock <= 0
        End Function

        ''' <summary>Critico es estar en o por debajo del minimo propio del producto.</summary>
        Public Function EstaCritico(p As Producto) As Boolean
            Return p IsNot Nothing AndAlso p.Stock <= MinimoDe(p)
        End Function

    End Module

End Namespace