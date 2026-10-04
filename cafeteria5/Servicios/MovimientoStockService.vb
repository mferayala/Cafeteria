Imports System

Namespace CafeteriaOS

    Public Module MovimientoStockService

        Public Sub RegistrarIngreso(ingreso As IngresoStock, usuario As String)
            If ingreso Is Nothing Then Return
            For Each d In ingreso.Detalles
                If d.Producto Is Nothing OrElse d.Cantidad <= 0 Then Continue For
                Datos.RegistrarMovimiento(New MovimientoStock With {
                    .Producto = d.Producto,
                    .Cantidad = d.Cantidad,
                    .Tipo = TipoMovimientoStock.Ingreso,
                    .Origen = "IngresoStock",
                    .ReferenciaID = ingreso.ID,
                    .Usuario = If(String.IsNullOrWhiteSpace(usuario), Datos.UsuarioActualNombre(), usuario),
                    .Observacion = If(String.IsNullOrWhiteSpace(d.Observacion), "Ingreso de stock", d.Observacion)
                })
            Next
        End Sub

        Public Sub RegistrarReposicion(producto As Producto, cantidad As Integer, usuario As String, obs As String)
            If producto Is Nothing OrElse cantidad <= 0 Then Return
            Datos.RegistrarMovimiento(New MovimientoStock With {
                .Producto = producto,
                .Cantidad = cantidad,
                .Tipo = TipoMovimientoStock.Reposicion,
                .Origen = "Reposicion",
                .ReferenciaID = 0,
                .Usuario = If(String.IsNullOrWhiteSpace(usuario), Datos.UsuarioActualNombre(), usuario),
                .Observacion = If(String.IsNullOrWhiteSpace(obs), "Reposición de stock", obs)
            })
        End Sub

        Public Sub RegistrarAjuste(producto As Producto, delta As Integer, usuario As String, obs As String)
            If producto Is Nothing OrElse delta = 0 Then Return
            Datos.RegistrarMovimiento(New MovimientoStock With {
                .Producto = producto,
                .Cantidad = delta,
                .Tipo = TipoMovimientoStock.Ajuste,
                .Origen = "Ajuste",
                .ReferenciaID = 0,
                .Usuario = If(String.IsNullOrWhiteSpace(usuario), Datos.UsuarioActualNombre(), usuario),
                .Observacion = If(String.IsNullOrWhiteSpace(obs), "Ajuste manual de stock", obs)
            })
        End Sub

        Public Sub RegistrarVenta(pedido As Pedido)
            If pedido Is Nothing Then Return
            For Each item In pedido.Items
                If item.Producto Is Nothing Then Continue For
                Datos.RegistrarMovimiento(New MovimientoStock With {
                    .Producto = item.Producto,
                    .Cantidad = -item.Cantidad,
                    .Tipo = TipoMovimientoStock.Venta,
                    .Origen = "POS",
                    .ReferenciaID = pedido.ID,
                    .Usuario = Datos.UsuarioActualNombre(),
                    .Observacion = "Venta Pedido #" & pedido.ID
                })
            Next
        End Sub

        Public Sub RegistrarCancelacion(pedido As Pedido)
            If pedido Is Nothing OrElse Not pedido.StockDescontado Then Return
            For Each item In pedido.Items
                If item.Producto Is Nothing Then Continue For
                Datos.RegistrarMovimiento(New MovimientoStock With {
                    .Producto = item.Producto,
                    .Cantidad = item.Cantidad,
                    .Tipo = TipoMovimientoStock.Cancelacion,
                    .Origen = "POS",
                    .ReferenciaID = pedido.ID,
                    .Usuario = Datos.UsuarioActualNombre(),
                    .Observacion = "Cancelación Pedido #" & pedido.ID & " - " & If(String.IsNullOrWhiteSpace(pedido.MotivoCancelacion), "", pedido.MotivoCancelacion)
                })
            Next
        End Sub

    End Module

End Namespace
