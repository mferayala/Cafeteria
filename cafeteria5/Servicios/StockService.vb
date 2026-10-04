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
        ''' Productos que el pedido pide y el deposito no tiene, con el detalle de cuanto
        ''' falta.
        '''
        ''' Se consulta ANTES de cobrar. Antes el descuento usaba Math.Max(0, ...): si el
        ''' deposito no alcanzaba, el stock llegaba a cero igual y la venta se cobraba
        ''' completa como si hubiera salido todo. El faltante quedaba escondido y nadie
        ''' se enteraba hasta ver el historial.
        ''' </summary>
        Public Function Faltantes(pedido As Pedido) As List(Of String)
            Dim problemas As New List(Of String)
            If pedido Is Nothing OrElse pedido.Items Is Nothing Then Return problemas

            For Each item In pedido.Items
                If item.Cantidad > item.Producto.Stock Then
                    problemas.Add(item.Producto.Nombre & ": pide " & item.Cantidad &
                                  " y hay " & item.Producto.Stock)
                End If
            Next
            Return problemas
        End Function

        ''' <summary>Si el pedido se puede cobrar con lo que hay en el deposito.</summary>
        Public Function HayStock(pedido As Pedido) As Boolean
            Return Faltantes(pedido).Count = 0
        End Function

        ''' <summary>
        ''' Aplicar al confirmar. Deja el stock en el valor real pedido y deja el pedido
        ''' marcado como descontado, para que un cancel posterior sepa que hay que devolver
        ''' esas unidades.
        '''
        ''' Si el deposito no alcanza NO se descuenta a medias y avisa con el detalle: es
        ''' preferible no cobrar a dejar el stock en cero con unidades que nunca salieron.
        ''' Si el asiento del movimiento falla, se devuelve el stock ya descontado, para
        ''' que no quede una operacion sin registro.
        ''' </summary>
        Public Sub Descontar(pedido As Pedido)
            If pedido Is Nothing OrElse pedido.Items Is Nothing Then Return
            If pedido.StockDescontado Then Return

            Dim problemas = Faltantes(pedido)
            If problemas.Count > 0 Then
                Throw New InvalidOperationException(
                    "No hay stock suficiente: " & String.Join("; ", problemas))
            End If

            Dim aplicado As New List(Of Tuple(Of Producto, Integer))
            Try
                For Each item In pedido.Items
                    item.Producto.Stock -= item.Cantidad
                    aplicado.Add(Tuple.Create(item.Producto, item.Cantidad))
                Next
                MovimientoStockService.RegistrarVenta(pedido)
            Catch
                For Each par In aplicado
                    par.Item1.Stock += par.Item2
                Next
                Throw
            End Try

            pedido.StockDescontado = True
        End Sub

        ''' <summary>
        ''' Aplicar al cancelar un pedido que ya habia descontado stock. Si el pedido nunca
        ''' se desconto no hace nada: armar y descartar un pedido en curso no toca el stock.
        ''' Cancelar dos veces tampoco suma unidades de mas, porque el pedido queda sin la
        ''' marca de descuento.
        '''
        ''' Igual que al descontar, si el asiento falla se deshace la devolucion: no puede
        ''' quedar el stock repuesto sin el movimiento que lo explique.
        ''' </summary>
        Public Sub Restaurar(pedido As Pedido)
            If Not pedido.StockDescontado Then Return

            Dim aplicado As New List(Of Tuple(Of Producto, Integer))
            Try
                For Each item In pedido.Items
                    item.Producto.Stock += item.Cantidad
                    aplicado.Add(Tuple.Create(item.Producto, item.Cantidad))
                Next
                MovimientoStockService.RegistrarCancelacion(pedido)
            Catch
                For Each par In aplicado
                    par.Item1.Stock -= par.Item2
                Next
                Throw
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
                ' El ajuste manual tiene que quedar asentado o no vale: si no se
                ' puede guardar el movimiento se vuelve el deposito a como estaba y
                ' el error sube, en vez de perder el cambio sin que nadie lo sepa.
                producto.Stock = antes
                Throw
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