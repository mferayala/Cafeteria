Namespace CafeteriaOS

    Public Enum RolUsuario
        Administrador
        Cajero
        Cocina
    End Enum

    Public Enum EstadoPedido
        EnPreparacion
        Listo
        Entregado
        Cancelado
    End Enum

    Public Enum TipoServicio
        EnElLocal
        ParaLlevar
    End Enum

    Public Enum MetodoPago
        Efectivo
        Debito
        Credito
        Transferencia
        MercadoPago
    End Enum

    Public Enum EstiloBoton
        Primario
        Exito
        Peligro
        Aviso
        Info
        Neutro
        Sutil
    End Enum

    Public Enum SeccionDashboard
        Venta
        Pedidos
        Stock
        Clientes
        Proveedores
        Reportes
        Caja
    End Enum

End Namespace