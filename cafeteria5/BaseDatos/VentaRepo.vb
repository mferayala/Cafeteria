Imports System.Collections.Generic
Imports System.Linq
Imports MySql.Data.MySqlClient

Namespace CafeteriaOS

    ''' <summary>
    ''' Ventas, pagos y su efecto en stock y caja.
    '''
    ''' Cada operacion (confirmar, anular, cobrar) es UNA transaccion: la venta, su
    ''' detalle, el pago, los movimientos de stock y el movimiento de caja se guardan
    ''' juntos. Si cualquiera falla no queda nada a medias en la base.
    ''' </summary>
    Public NotInheritable Class VentaRepo

        Private Sub New()
        End Sub

        ''' <summary>
        ''' Guarda una venta recien confirmada. Lo que ya hicieron los servicios en
        ''' memoria (descontar stock, anotar en caja) se lee de Datos y se escribe aca.
        ''' </summary>
        Public Shared Sub GuardarConfirmacion(venta As Venta)
            Dim movimientos = Datos.ListaMovimientos.
                Where(Function(m) m.Tipo = TipoMovimientoStock.Venta AndAlso m.ReferenciaID = venta.ID).
                ToList()

            Dim sesion = Datos.Caja
            Dim movCaja As MovimientoCaja = Nothing
            If sesion IsNot Nothing Then
                movCaja = sesion.Movimientos.LastOrDefault(
                    Function(m) m.Tipo = MovimientoCaja.TipoVenta AndAlso m.VentaID = venta.ID)
            End If

            Db.EnTransaccion(
                Sub(cx, tx)
                    StockRepo.BloquearProductos(cx, tx, venta.Items.Select(Function(i) i.ProductoID))
                    Dim metodoId = Db.MetodoPagoId(cx, tx, venta.MetodoPago)

                    Db.Ejecutar(cx, tx,
                        "INSERT INTO ventas " &
                        "  (id, numero, empleado_id, cliente_nombre, cliente_telefono, tipo_servicio, mesa, " &
                        "   metodo_pago_id, estado, estado_pago, fue_a_cuenta, subtotal, descuento, total, " &
                        "   monto_pagado, efectivo_recibido, stock_descontado, fecha_venta) " &
                        "VALUES (@id, @numero, @emp, @cliente, @tel, @tipo, @mesa, @metodo, @estado, @pago, " &
                        "        @aCuenta, @subtotal, @descuento, @total, @pagado, @efectivo, @descontado, @fecha)",
                        "@id", venta.ID,
                        "@numero", venta.Numero,
                        "@emp", Db.EmpleadoId(),
                        "@cliente", Db.NuloSiVacio(venta.ClienteNombre),
                        "@tel", Db.NuloSiVacio(venta.ClienteTelefono),
                        "@tipo", venta.TipoServicio.ToString(),
                        "@mesa", Db.NuloSiVacio(venta.Mesa),
                        "@metodo", metodoId,
                        "@estado", venta.Estado.ToString(),
                        "@pago", venta.Pago.ToString(),
                        "@aCuenta", venta.FueACuenta,
                        "@subtotal", venta.Subtotal,
                        "@descuento", venta.Descuento,
                        "@total", venta.Total,
                        "@pagado", venta.MontoPagado,
                        "@efectivo", venta.EfectivoRecibido,
                        "@descontado", venta.StockDescontado,
                        "@fecha", venta.FechaHora)

                    For Each item In venta.Items
                        Db.Ejecutar(cx, tx,
                            "INSERT INTO detalle_ventas " &
                            "  (venta_id, producto_id, producto_nombre, cantidad, precio_unitario, subtotal, especificaciones) " &
                            "VALUES (@venta, @prod, @nombre, @cant, @precio, @subtotal, @specs)",
                            "@venta", venta.ID,
                            "@prod", item.ProductoID,
                            "@nombre", item.ProductoNombre,
                            "@cant", item.Cantidad,
                            "@precio", item.PrecioUnitario,
                            "@subtotal", item.Subtotal,
                            "@specs", Db.NuloSiVacio(item.Especificaciones))
                    Next

                    ' Una venta cobrada en el momento tiene su pago. Una "a cuenta" no
                    ' tiene ninguno todavia: los pagos llegan despues con GuardarPago.
                    If venta.MontoPagado > 0D Then
                        InsertarPago(cx, tx, venta.ID, metodoId, venta.MontoPagado,
                                     If(venta.EfectivoComoMetodo, CObj(venta.EfectivoRecibido), Nothing),
                                     venta.FechaHora)
                    End If

                    For Each m In movimientos
                        StockRepo.Aplicar(cx, tx, m)
                    Next

                    If movCaja IsNot Nothing Then
                        CajaRepo.InsertarMovimiento(cx, tx, sesion.ID, movCaja)
                    End If
                End Sub)
        End Sub

        ''' <summary>
        ''' Guarda la anulacion: marca la venta, devuelve el stock con sus movimientos,
        ''' saca el cobro de la caja abierta y cancela el pedido de cocina si lo habia.
        ''' La venta NO se borra: queda en la tabla como anulada.
        ''' </summary>
        Public Shared Sub GuardarAnulacion(venta As Venta)
            Dim movimientos = Datos.ListaMovimientos.
                Where(Function(m) m.Tipo = TipoMovimientoStock.Cancelacion AndAlso m.ReferenciaID = venta.ID).
                ToList()

            Dim sesionId = If(Datos.Caja Is Nothing, 0, Datos.Caja.ID)

            Db.EnTransaccion(
                Sub(cx, tx)
                    StockRepo.BloquearProductos(cx, tx, venta.Items.Select(Function(i) i.ProductoID))
                    Db.Ejecutar(cx, tx,
                        "UPDATE ventas SET estado = @estado, stock_descontado = @descontado, " &
                        "  anulada_en = @fecha, anulada_por = @emp, motivo_anulacion = @motivo " &
                        "WHERE id = @id",
                        "@estado", venta.Estado.ToString(),
                        "@descontado", venta.StockDescontado,
                        "@fecha", venta.AnuladaFecha,
                        "@emp", Db.EmpleadoId(),
                        "@motivo", Db.NuloSiVacio(venta.AnuladaMotivo),
                        "@id", venta.ID)

                    For Each m In movimientos
                        StockRepo.Aplicar(cx, tx, m)
                    Next

                    ' Igual que CajaService.RevertirVenta: solo se quita el cobro si es de
                    ' la caja que esta abierta ahora. Un turno ya cerrado no se toca.
                    Db.Ejecutar(cx, tx,
                        "DELETE FROM movimientos_caja " &
                        "WHERE venta_id = @venta AND tipo = 'Venta' AND sesion_caja_id = @s",
                        "@venta", venta.ID, "@s", sesionId)

                    Db.Ejecutar(cx, tx,
                        "UPDATE pedidos SET estado = 'Cancelado', motivo_cancelacion = @motivo, " &
                        "  cancelado_en = NOW() " &
                        "WHERE venta_id = @venta AND estado <> 'Cancelado'",
                        "@motivo", Db.NuloSiVacio(venta.AnuladaMotivo),
                        "@venta", venta.ID)
                End Sub)
        End Sub

        ''' <summary>
        ''' Cobro de una venta a cuenta (puede ser parcial): actualiza lo pagado, agrega
        ''' la fila en pagos y el movimiento de caja si la caja estaba abierta.
        ''' </summary>
        Public Shared Sub GuardarPago(venta As Venta,
                                      monto As Decimal,
                                      metodo As MetodoPago,
                                      movCaja As MovimientoCaja)
            Dim sesionId = If(Datos.Caja Is Nothing, 0, Datos.Caja.ID)

            Db.EnTransaccion(
                Sub(cx, tx)
                    Dim metodoId = Db.MetodoPagoId(cx, tx, metodo)

                    Db.Ejecutar(cx, tx,
                        "UPDATE ventas SET monto_pagado = @pagado, estado_pago = @pago, " &
                        "  metodo_pago_id = @metodo WHERE id = @id",
                        "@pagado", venta.MontoPagado,
                        "@pago", venta.Pago.ToString(),
                        "@metodo", metodoId,
                        "@id", venta.ID)

                    InsertarPago(cx, tx, venta.ID, metodoId, monto, Nothing, DateTime.Now)

                    If movCaja IsNot Nothing Then
                        CajaRepo.InsertarMovimiento(cx, tx, sesionId, movCaja)
                    End If
                End Sub)
        End Sub

        ''' <summary>Le asigna a la venta el cliente que la hizo.</summary>
        Public Shared Sub AsignarCliente(cx As MySqlConnection,
                                         tx As MySqlTransaction,
                                         ventaId As Integer,
                                         clienteId As Integer)
            Db.Ejecutar(cx, tx, "UPDATE ventas SET cliente_id = @c WHERE id = @v",
                        "@c", clienteId, "@v", ventaId)
        End Sub

        Private Shared Sub InsertarPago(cx As MySqlConnection,
                                        tx As MySqlTransaction,
                                        ventaId As Integer,
                                        metodoId As Integer,
                                        monto As Decimal,
                                        efectivo As Object,
                                        fecha As DateTime)
            Db.Ejecutar(cx, tx,
                "INSERT INTO pagos (venta_id, metodo_pago_id, empleado_id, monto, efectivo_recibido, pagado_en) " &
                "VALUES (@venta, @metodo, @emp, @monto, @efectivo, @fecha)",
                "@venta", ventaId,
                "@metodo", metodoId,
                "@emp", Db.EmpleadoId(),
                "@monto", monto,
                "@efectivo", efectivo,
                "@fecha", fecha)
        End Sub

    End Class

    ''' <summary>Pedidos de cocina (tablero).</summary>
    Public NotInheritable Class PedidoRepo

        Private Sub New()
        End Sub

        ''' <summary>
        ''' Alta del pedido con su detalle. El cliente se toma de la venta que lo origino
        ''' con una subconsulta, asi no hace falta pasarlo aparte.
        ''' </summary>
        Public Shared Sub Insertar(p As Pedido)
            Db.EnTransaccion(
                Sub(cx, tx)
                    Db.Ejecutar(cx, tx,
                        "INSERT INTO pedidos " &
                        "  (id, venta_id, cliente_id, empleado_id, cliente_nombre, cliente_telefono, estado, tipo, " &
                        "   mesa, metodo_pago_id, subtotal, descuento, total, creado_en, confirmado_en) " &
                        "VALUES (@id, @venta, (SELECT v.cliente_id FROM ventas v WHERE v.id = @venta), @emp, " &
                        "        @cliente, @tel, @estado, @tipo, @mesa, @metodo, @subtotal, @descuento, @total, " &
                        "        @fecha, NOW())",
                        "@id", p.ID,
                        "@venta", Db.NuloSiCero(p.VentaID),
                        "@emp", Db.EmpleadoId(),
                        "@cliente", Db.NuloSiVacio(p.NombreCliente),
                        "@tel", Db.NuloSiVacio(p.TelefonoCliente),
                        "@estado", p.Estado.ToString(),
                        "@tipo", p.TipoServicio.ToString(),
                        "@mesa", Db.NuloSiVacio(p.Mesa),
                        "@metodo", Db.MetodoPagoId(cx, tx, p.MetodoPago),
                        "@subtotal", p.Subtotal,
                        "@descuento", p.Descuento,
                        "@total", p.Total,
                        "@fecha", p.FechaHora)

                    For Each item In p.Items
                        Db.Ejecutar(cx, tx,
                            "INSERT INTO detalle_pedidos " &
                            "  (pedido_id, producto_id, cantidad, precio_unitario, subtotal, observaciones) " &
                            "VALUES (@pedido, @prod, @cant, @precio, @subtotal, @obs)",
                            "@pedido", p.ID,
                            "@prod", item.Producto.ID,
                            "@cant", item.Cantidad,
                            "@precio", item.Producto.Precio,
                            "@subtotal", item.Subtotal,
                            "@obs", Db.NuloSiVacio(item.Especificaciones))
                    Next
                End Sub)
        End Sub

        Public Shared Sub CambiarEstado(p As Pedido)
            Db.EnTransaccion(
                Sub(cx, tx)
                    Db.Ejecutar(cx, tx,
                        "UPDATE pedidos SET estado = @estado, motivo_cancelacion = @motivo, " &
                        "  cancelado_en = IF(@estado = 'Cancelado', NOW(), NULL) " &
                        "WHERE id = @id",
                        "@estado", p.Estado.ToString(),
                        "@motivo", Db.NuloSiVacio(p.MotivoCancelacion),
                        "@id", p.ID)
                End Sub)
        End Sub

    End Class

    ''' <summary>Clientes registrados desde el punto de venta.</summary>
    Public NotInheritable Class ClienteRepo

        Private Sub New()
        End Sub

        ''' <summary>
        ''' Da de alta al cliente si es nuevo (o actualiza su nombre) y lo vincula con la
        ''' venta que acaba de hacer.
        ''' </summary>
        Public Shared Sub GuardarConVenta(c As ClienteRegistrado, ventaId As Integer)
            Dim telefono = If(c.Telefono = ClienteRegistrado.SinTelefono, Nothing, Db.NuloSiVacio(c.Telefono))

            ' El id nuevo se asigna recien al confirmar la transaccion (ver ProveedorRepo).
            Dim esNuevo = c.ID = 0
            Dim clienteId = c.ID

            Db.EnTransaccion(
                Sub(cx, tx)
                    If esNuevo Then
                        clienteId = Db.Insertar(cx, tx,
                            "INSERT INTO clientes (nombre, telefono) VALUES (@nombre, @tel)",
                            "@nombre", c.Nombre, "@tel", telefono)
                    Else
                        Db.Ejecutar(cx, tx,
                            "UPDATE clientes SET nombre = @nombre, telefono = @tel WHERE id = @id",
                            "@nombre", c.Nombre, "@tel", telefono, "@id", c.ID)
                    End If

                    If ventaId > 0 Then VentaRepo.AsignarCliente(cx, tx, ventaId, clienteId)
                End Sub)
            If esNuevo AndAlso clienteId > 0 Then c.ID = clienteId
        End Sub

    End Class

End Namespace
