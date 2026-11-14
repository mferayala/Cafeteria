Imports System.Collections.Generic
Imports MySql.Data.MySqlClient

Namespace CafeteriaOS

    ''' <summary>
    ''' Movimientos de stock e ingresos de mercaderia.
    '''
    ''' Toda modificacion del stock en MySQL pasa por Aplicar: actualiza
    ''' productos.stock_actual y deja la fila en movimientos_stock con el stock de antes y
    ''' el de despues, dentro de la misma transaccion. Asi la tabla de movimientos explica
    ''' siempre el numero que se ve en productos.
    ''' </summary>
    Public NotInheritable Class StockRepo

        Private Sub New()
        End Sub

        ''' <summary>
        ''' Aplica un movimiento ya registrado en memoria. Se usa dentro de otras
        ''' transacciones (venta, anulacion, ingreso), por eso recibe cx y tx.
        '''
        ''' FOR UPDATE bloquea la fila del producto hasta el Commit: si dos cajas
        ''' vendieran el mismo producto a la vez, la segunda espera a la primera en vez
        ''' de pisar su descuento.
        ''' </summary>
        Public Shared Sub Aplicar(cx As MySqlConnection, tx As MySqlTransaction, m As MovimientoStock)
            If m Is Nothing OrElse m.Producto Is Nothing OrElse m.Cantidad = 0 Then Return

            Dim valor = Db.Escalar(cx, tx,
                "SELECT stock_actual FROM productos WHERE id = @id FOR UPDATE",
                "@id", m.Producto.ID)
            If valor Is Nothing Then
                Throw New InvalidOperationException(
                    "El producto " & m.Producto.Nombre & " no existe en la base de datos.")
            End If

            Dim anterior = Convert.ToDecimal(valor)
            Dim posterior = Math.Max(0D, anterior + m.Cantidad)

            Db.Ejecutar(cx, tx, "UPDATE productos SET stock_actual = @s WHERE id = @id",
                        "@s", posterior, "@id", m.Producto.ID)

            Db.Ejecutar(cx, tx,
                "INSERT INTO movimientos_stock " &
                "  (id, producto_id, usuario_id, tipo, cantidad, stock_anterior, stock_posterior, " &
                "   referencia_tipo, referencia_id, motivo, creado_en) " &
                "VALUES (@id, @prod, @usuario, @tipo, @cant, @antes, @despues, @origen, @ref, @motivo, @fecha)",
                "@id", m.ID,
                "@prod", m.Producto.ID,
                "@usuario", Db.UsuarioId(),
                "@tipo", m.Tipo.ToString(),
                "@cant", m.Cantidad,
                "@antes", anterior,
                "@despues", posterior,
                "@origen", Db.NuloSiVacio(m.Origen),
                "@ref", Db.NuloSiCero(m.ReferenciaID),
                "@motivo", Db.NuloSiVacio(m.Observacion),
                "@fecha", m.Fecha)
        End Sub

        ''' <summary>Ajuste manual o reposicion: un solo movimiento en su propia transaccion.</summary>
        Public Shared Sub GuardarAjuste(m As MovimientoStock)
            Db.EnTransaccion(Sub(cx, tx) Aplicar(cx, tx, m))
        End Sub

        ''' <summary>
        ''' Ingreso de mercaderia completo: cabecera, lineas, movimientos de stock y la
        ''' fecha de ultima compra del proveedor. Todo o nada.
        ''' </summary>
        Public Shared Sub GuardarIngreso(ingreso As IngresoStock, movimientos As List(Of MovimientoStock))
            Db.EnTransaccion(
                Sub(cx, tx)
                    Dim proveedorId As Object = Nothing
                    If ingreso.Proveedor IsNot Nothing Then proveedorId = Db.NuloSiCero(ingreso.Proveedor.ID)

                    Db.Ejecutar(cx, tx,
                        "INSERT INTO ingresos_stock (id, proveedor_id, usuario_id, fecha, observaciones) " &
                        "VALUES (@id, @prov, @usuario, @fecha, @obs)",
                        "@id", ingreso.ID,
                        "@prov", proveedorId,
                        "@usuario", Db.UsuarioId(),
                        "@fecha", ingreso.Fecha,
                        "@obs", Db.NuloSiVacio(ingreso.Observaciones))

                    For Each d In ingreso.Detalles
                        Db.Ejecutar(cx, tx,
                            "INSERT INTO detalle_ingresos (ingreso_id, producto_id, cantidad, observacion) " &
                            "VALUES (@ing, @prod, @cant, @obs)",
                            "@ing", ingreso.ID,
                            "@prod", d.Producto.ID,
                            "@cant", d.Cantidad,
                            "@obs", Db.NuloSiVacio(d.Observacion))
                    Next

                    For Each m In movimientos
                        Aplicar(cx, tx, m)
                    Next

                    If proveedorId IsNot Nothing Then
                        Db.Ejecutar(cx, tx,
                            "UPDATE proveedores SET ultima_compra = @f, fecha_ultima_operacion = @f WHERE id = @id",
                            "@f", ingreso.Fecha, "@id", proveedorId)
                    End If
                End Sub)
        End Sub

    End Class

End Namespace
