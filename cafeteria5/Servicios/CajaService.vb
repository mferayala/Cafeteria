Imports System.Linq

Namespace CafeteriaOS

    ''' <summary>
    ''' Reglas de la caja. Ningun panel toca Datos.Caja directamente: todos pasan por
    ''' aca, que es el unico lugar donde se abre, se cierra y se anota un movimiento.
    '''
    ''' Cada apertura crea una sesion nueva: los movimientos del turno anterior no se
    ''' arrastran al saldo del dia.
    ''' </summary>
    Public Module CajaService

        Public Function EstaAbierta() As Boolean
            Return Datos.Caja IsNot Nothing AndAlso Datos.Caja.Abierta
        End Function

        Public Sub Abrir(montoInicial As Decimal)
            Dim sesion As New SesionCaja With {
                .Abierta = True,
                .MontoInicial = montoInicial,
                .FechaApertura = DateTime.Now,
                .UsuarioApertura = Datos.UsuarioActualNombre()
            }
            ' Primero se crea el turno en MySQL (le da el ID) y recien despues pasa a
            ' ser la caja actual: si la base falla, la caja sigue cerrada.
            CajaRepo.AbrirSesion(sesion)
            Datos.Caja = sesion
        End Sub

        ''' <summary>Cierra el turno: deja de aceptar ventas y registra el cierre.</summary>
        Public Sub Cerrar()
            If Not EstaAbierta() Then Return
            Dim cierre As New MovimientoCaja With {
                .Tipo = MovimientoCaja.TipoCierre,
                .Monto = Datos.Caja.SaldoActual,
                .Descripcion = "Cierre de caja",
                .Usuario = Datos.UsuarioActualNombre()
            }
            CajaRepo.CerrarSesion(Datos.Caja, cierre)
            Datos.Caja.Movimientos.Add(cierre)
            Datos.Caja.Abierta = False
        End Sub

        ''' <summary>
        ''' Asienta el cobro de una venta en la caja abierta.
        '''
        ''' El movimiento guarda el id de la venta ademas de la descripcion. Antes solo
        ''' existia el texto "Pedido N. X" y la revocacion lo buscaba por ahi: dos ventas
        ''' con la misma descripcion revocaban la misma y la otra quedaba colgada en el
        ''' saldo.
        ''' </summary>
        Public Sub RegistrarVenta(venta As Venta)
            If venta Is Nothing Then Exit Sub
            ' El control de si esta pagada vive ACA y no en quien llama: si queda solo
            ' en VentaService.Confirmar, cualquier otro que asiente una venta a cuenta
            ' sumaria a la caja plata que todavia no entro.
            If Not venta.EstaPagada Then Exit Sub
            If Not EstaAbierta() Then Exit Sub
            Datos.Caja.Movimientos.Add(New MovimientoCaja With {
                .Tipo = MovimientoCaja.TipoVenta,
                .Monto = venta.Total,
                .Descripcion = "Venta " & venta.Numero,
                .Usuario = Datos.UsuarioActualNombre(),
                .VentaID = venta.ID
            })
        End Sub

        ''' <summary>
        ''' Asienta el cobro de una deuda ya existente. Va en su propio tipo de movimiento
        ''' y no como una venta mas: una venta a cuenta no es una venta, es el cobro de
        ''' una venta anterior, y en el cierre de caja tienen que poder verse separadas.
        '''
        ''' El monto es el que se acaba de cobrar, no el total de la venta, para que una
        ''' deuda saldada en varias cuotas sume en caja lo que realmente entro.
        ''' </summary>
        Public Sub RegistrarCobroDeuda(venta As Venta,
                                       monto As Decimal,
                                       Optional usuario As String = Nothing)
            If venta Is Nothing OrElse Datos.Caja Is Nothing Then Exit Sub
            If Not EstaAbierta() Then Exit Sub

            Datos.Caja.Movimientos.Add(New MovimientoCaja With {
                .Tipo = MovimientoCaja.TipoCobroDeuda,
                .Monto = monto,
                .Descripcion = "Cobro de " & venta.Numero & " (saldo)",
                .Usuario = If(usuario, Datos.UsuarioActualNombre()),
                .VentaID = venta.ID
            })
        End Sub

        ''' <summary>
        ''' Da de baja el cobro de una venta anulada. Busca por id y no por texto, asi
        ''' que anular dos veces no descuenta dos veces y nunca toca el movimiento de
        ''' otra venta.
        ''' </summary>
        Public Sub RevertirVenta(venta As Venta)
            If Datos.Caja Is Nothing Then Exit Sub
            If venta Is Nothing Then Exit Sub
            Dim movimiento = Datos.Caja.Movimientos.LastOrDefault(
                Function(m) m.Tipo = MovimientoCaja.TipoVenta AndAlso m.VentaID = venta.ID)
            If movimiento IsNot Nothing Then Datos.Caja.Movimientos.Remove(movimiento)
        End Sub

        Public Sub RegistrarEgreso(monto As Decimal, descripcion As String)
            If Not EstaAbierta() Then Exit Sub
            Dim egreso As New MovimientoCaja With {
                .Tipo = MovimientoCaja.TipoEgreso,
                .Monto = monto,
                .Descripcion = If(String.IsNullOrWhiteSpace(descripcion), "Egreso", descripcion),
                .Usuario = Datos.UsuarioActualNombre()
            }
            CajaRepo.GuardarMovimiento(Datos.Caja.ID, egreso)
            Datos.Caja.Movimientos.Add(egreso)
        End Sub

    End Module

End Namespace