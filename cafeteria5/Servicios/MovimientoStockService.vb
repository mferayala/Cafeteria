Imports System

Namespace CafeteriaOS

    ''' <summary>
    ''' Asientos de stock. Cada operacion que cambia el stock deja un movimiento: por eso
    ''' se puede responder de donde sale cada unidad y quien la toco.
    '''
    ''' El tipo lo declara el que llama, nunca se deduce aca. StockService es el unico
    ''' que llama a Registrar, y por lo tanto hay un solo asiento por operacion.
    ''' </summary>
    Public Module MovimientoStockService

        ''' <summary>
        ''' Deja constancia de un cambio de stock. La cantidad va con signo: positiva si
        ''' sumo unidades, negativa si las resto.
        ''' </summary>
        Public Sub Registrar(producto As Producto,
                             cantidad As Integer,
                             tipo As TipoMovimientoStock,
                             origen As String,
                             referenciaID As Integer,
                             usuario As String,
                             observacion As String)

            If producto Is Nothing OrElse cantidad = 0 Then Return

            Datos.RegistrarMovimiento(New MovimientoStock With {
                .Producto = producto,
                .Cantidad = cantidad,
                .Tipo = tipo,
                .Origen = If(String.IsNullOrWhiteSpace(origen), "Sistema", origen),
                .ReferenciaID = referenciaID,
                .Usuario = If(String.IsNullOrWhiteSpace(usuario),
                              Datos.UsuarioActualNombre(), usuario),
                .Observacion = If(String.IsNullOrWhiteSpace(observacion),
                                  DescripcionPorDefecto(tipo), observacion)
            })
        End Sub

        ''' <summary>Texto de respaldo para cuando quien opera no escribe nada.</summary>
        Public Function DescripcionPorDefecto(tipo As TipoMovimientoStock) As String
            Select Case tipo
                Case TipoMovimientoStock.Ingreso : Return "Ingreso de mercaderia"
                Case TipoMovimientoStock.Reposicion : Return "Reposicion de stock"
                Case TipoMovimientoStock.Ajuste : Return "Ajuste manual de stock"
                Case TipoMovimientoStock.Venta : Return "Venta"
                Case TipoMovimientoStock.Cancelacion : Return "Cancelacion de venta"
                Case TipoMovimientoStock.Devolucion : Return "Devolucion"
                Case Else : Return "Movimiento de stock"
            End Select
        End Function

        ''' <summary>
        ''' Una venta descuenta una unidad por cada linea. La recibe StockService al
        ''' confirmar, y queda firmada con el numero de la venta, no con el del pedido:
        ''' una venta de mostrador no tiene pedido asociado.
        ''' </summary>
        Public Sub RegistrarVenta(venta As Venta)
            If venta Is Nothing Then Return
            For Each item In venta.Items
                Registrar(item.Producto, -item.Cantidad, TipoMovimientoStock.Venta,
                          "POS", venta.ID, Datos.UsuarioActualNombre(),
                          "Venta " & venta.Numero)
            Next
        End Sub

        ''' <summary>
        ''' Anular una venta devuelve las unidades. Solo se asienta si la venta
        ''' realmente habia descontado stock: anular dos veces no genera dos devoluciones.
        ''' </summary>
        Public Sub RegistrarCancelacion(venta As Venta)
            If venta Is Nothing OrElse Not venta.StockDescontado Then Return
            For Each item In venta.Items
                Registrar(item.Producto, item.Cantidad, TipoMovimientoStock.Cancelacion,
                          "POS", venta.ID, Datos.UsuarioActualNombre(),
                          "Anulacion de la venta " & venta.Numero)
            Next
        End Sub

        ''' <summary>
        ''' Historial de un producto del mas nuevo al mas viejo. Es la trazabilidad
        ''' completa: no solo los ingresos, tambien las reposiciones, los ajustes y las
        ''' ventas, que antes no quedaban registrados.
        ''' </summary>
        Public Function Historial(producto As Producto) As System.Collections.Generic.List(Of MovimientoStock)
            If producto Is Nothing Then
                Return New System.Collections.Generic.List(Of MovimientoStock)()
            End If
            Return Datos.ListaMovimientos.
                Where(Function(m) ReferenceEquals(m.Producto, producto)).
                OrderByDescending(Function(m) m.Fecha).
                ThenByDescending(Function(m) m.ID).
                ToList()
        End Function

    End Module

End Namespace
