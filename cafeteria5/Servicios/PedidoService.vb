Imports System.Linq

Namespace CafeteriaOS

    ''' <summary>
    ''' Alta de pedidos que van a preparacion.
    '''
    ''' Vive en su propio servicio y no dentro de VentaService a proposito: preparar es
    ''' opcional y la venta no tiene por que saber nada de tablero. Un cliente que pide
    ''' y paga para irse no genera ningun pedido, y el sistema funciona igual asi.
    '''
    ''' El unico requisito es que la venta este confirmada: un pedido sin venta cobrada
    ''' por detras es una nota al viento, y anular la venta tiene que poderdeedarse de
    ''' baja el pedido que la originó.
    ''' </summary>
    Public Module PedidoService

        ''' <summary>
        ''' Crea el pedido de preparacion a partir de una venta ya cobrada.
        '''
        ''' El pedido sale de la venta y no al reves: la venta tiene el detalle con los
        ''' precios congelados y el descuento ya aplicado, y el tablero necesita esa misma
        ''' informacion para que lo que se ve en pantalla sea lo que se cobro.
        '''
        ''' No puede fallar por falta de stock: el stock ya bajo cuando se confirmo la
        ''' venta, que es el momento en que la mercaderia sale del deposito.
        ''' </summary>
        Public Function CrearDesdeVenta(venta As Venta) As Pedido
            If venta Is Nothing Then
                Throw New ArgumentNullException(NameOf(venta))
            End If
            If Not Datos.ListaVentas.Contains(venta) Then
                Throw New InvalidOperationException(
                    "La venta todavia no esta confirmada: no hay pedido que preparar.")
            End If
            If venta.EstaAnulada Then
                Throw New InvalidOperationException("La venta " & venta.Numero & " esta anulada.")
            End If
            If venta.Items.Count = 0 Then
                Throw New InvalidOperationException("Una venta sin lineas no genera pedido.")
            End If

            ' StockDescontado va en True porque el stock bajo al confirmarse la venta, que
            ' es cuando la mercaderia sale del deposito. Crear el pedido no mueve nada.
            Dim pedido As New Pedido With {
                .NombreCliente = venta.ClienteNombre,
                .TelefonoCliente = venta.ClienteTelefono,
                .TipoServicio = venta.TipoServicio,
                .Mesa = venta.Mesa,
                .Estado = EstadoPedido.EnPreparacion,
                .MetodoPago = venta.MetodoPago,
                .FechaHora = venta.FechaHora,
                .Descuento = venta.Descuento,
                .UsuarioResponsable = venta.Usuario,
                .VentaID = venta.ID,
                .StockDescontado = True
            }

            For Each linea In venta.Items
                pedido.Items.Add(New DetallePedido With {
                    .Producto = linea.Producto,
                    .Cantidad = linea.Cantidad,
                    .Especificaciones = linea.Especificaciones
                })
            Next

            Datos.RegistrarPedido(pedido)
            Return pedido
        End Function

        ''' <summary>Pedidos que estan en el tablero, en el orden en que se tomaron.</summary>
        Public Function Activos() As List(Of Pedido)
            Return Datos.ListaPedidos.
                Where(Function(p) p.Estado <> EstadoPedido.Cancelado).
                OrderBy(Function(p) p.FechaHora).
                ToList()
        End Function

        ''' <summary>
        ''' Da de baja el pedido de una venta anulada, si todavia estaba en el tablero.
        '''
        ''' Se busca por VentaID y no por indice: anular una venta dos veces, o dos ventas
        ''' distintas, nunca tocan el pedido equivocado.
        '''
        Public Function DarDeBajaPorVenta(ventaID As Integer, motivo As String) As Pedido
            Dim pedido = Datos.ListaPedidos.FirstOrDefault(
                Function(p) p.VentaID = ventaID AndAlso p.Estado <> EstadoPedido.Cancelado)
            If pedido Is Nothing Then Return Nothing

            pedido.Estado = EstadoPedido.Cancelado
            pedido.MotivoCancelacion = motivo
            Return pedido
        End Function

    End Module

End Namespace
