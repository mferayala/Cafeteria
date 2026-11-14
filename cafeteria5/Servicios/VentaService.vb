Imports System.Collections.Generic
Imports System.Linq

Namespace CafeteriaOS

    ''' <summary>
    ''' Paso de un Pedido en curso a una Venta confirmada, y vuelta.
    '''
    ''' Es el unico lugar donde se crea una Venta. Concentrarlo aqui es lo que mantiene
    ''' la garantia del snapshot: si cada pantalla pudiera armar su propia venta, cada
    ''' una copiaria los datos a su manera y alguna se olvidaria de congelar el precio.
    ''' </summary>
    Public Module VentaService

        ''' <summary>
        ''' Convierte el pedido en curso en una venta inmutable. NO descuenta stock ni
        ''' toca la caja: eso lo hace Confirmar, porque el orden importa y cada paso
        ''' necesita poder deshacerse por separado.
        '''
        ''' El cobro es opcional para poder armar la venta y revisar el stock ANTES de
        ''' abrir la pantalla de pago. Si el usuario cancela el cobro, la venta se
        ''' descarta y no llego a registrarse.
        ''' </summary>
        Public Function Crear(pedido As Pedido,
                              Optional cobro As ResultadoCobro = Nothing,
                              Optional usuario As String = Nothing) As Venta

            If pedido Is Nothing Then
                Throw New ArgumentNullException(NameOf(pedido))
            End If
            If pedido.Items Is Nothing OrElse pedido.Items.Count = 0 Then
                Throw New InvalidOperationException("Una venta necesita al menos una linea.")
            End If

            ' El numero se reserva al armar la venta, no al confirmarla, para que dos
            ' ventas preparadas al mismo tiempo no salgan con el mismo numero.
            Dim numero = Datos.ReservarIdVenta()

            ' Sin cobro todavia la venta se trata como pagada, que es el caso normal:
            ' el POS siempre pasa por la pantalla de cobro antes de confirmar. Queda
            ' pendiente solo cuando el cliente se lleva la mercaderia a cuenta.
            Dim cobroAhora = cobro Is Nothing OrElse cobro.CobradoAhora

            Dim venta As New Venta With {
                .ID = numero,
                .Numero = "V-" & numero.ToString("0000"),
                .FechaHora = DateTime.Now,
                .ClienteNombre = If(pedido.NombreCliente, String.Empty),
                .ClienteTelefono = If(pedido.TelefonoCliente, String.Empty),
                .TipoServicio = pedido.TipoServicio,
                .Mesa = If(pedido.Mesa, String.Empty),
                .Descuento = pedido.Descuento,
                .MetodoPago = If(cobro Is Nothing, pedido.MetodoPago, cobro.MetodoPago),
                .EfectivoRecibido = If(cobro Is Nothing, 0D, cobro.EfectivoRecibido),
                .Pago = If(cobroAhora, EstadoPago.Pagada, EstadoPago.Pendiente),
                .FueACuenta = Not cobroAhora,
                .Usuario = If(usuario, Datos.UsuarioActualNombre()),
                .Estado = EstadoVenta.Confirmada
            }

            ' El precio se congela ACA, con el valor de ahora. Guardar el producto
            ' vivo y leer su precio despues permitiria que cambiar el precio de un
            ' producto reescribiera el pasado.
            For Each item In pedido.Items
                venta.Items.Add(New DetalleVenta With {
                    .Producto = item.Producto,
                    .ProductoID = item.Producto.ID,
                    .ProductoNombre = item.Producto.Nombre,
                    .Cantidad = item.Cantidad,
                    .PrecioUnitario = item.Producto.Precio,
                    .Especificaciones = item.Especificaciones
                })
            Next

            ' MontoPagado se setea DESPUES de cargar las lineas, porque el total sale de
            ' ellas: una venta pagada arranca con el total completo como cobrado, y una
            ' a cuenta arranca en cero sin dejar nada pagado.
            If cobroAhora Then venta.MontoPagado = venta.Total

            Return venta
        End Function

        ''' <summary>
        ''' Le aplica a una venta ya armada el resultado del cobro, sin volver a crearla.
        '''
        ''' Existe para no reservar dos numeros por venta. Antes el punto de venta armaba
        ''' una venta para revisar el stock, cobraba, y armaba OTRA para confirmar: la
        ''' primera se descartaba pero su numero ya estaba gastado, asi que cada venta
        ''' salia con un numero impar de mas.
        '''
        ''' Solo se acepta mientras la venta no este registrada: despues de confirmar la
        ''' venta ya esta en caja y en el historial, y cambiarle el cobro a mano
        ''' dejaria el movimiento de caja diciendo una cosa y la venta otra.
        ''' </summary>
        Public Sub AplicarCobro(venta As Venta, cobro As ResultadoCobro)
            If venta Is Nothing Then
                Throw New ArgumentNullException(NameOf(venta))
            End If
            If cobro Is Nothing Then
                Throw New ArgumentNullException(NameOf(cobro))
            End If
            If venta.EstaAnulada Then
                Throw New InvalidOperationException("No se puede cobrar una venta anulada.")
            End If
            If Datos.ListaVentas.Contains(venta) Then
                Throw New InvalidOperationException(
                    "La venta " & venta.Numero & " ya esta registrada: no se le puede cambiar el cobro.")
            End If

            venta.MetodoPago = cobro.MetodoPago
            venta.EfectivoRecibido = cobro.EfectivoRecibido
            venta.Pago = If(cobro.CobradoAhora, EstadoPago.Pagada, EstadoPago.Pendiente)
            venta.FueACuenta = Not cobro.CobradoAhora

            ' Igual que en Crear: el total sale de las lineas, que ya estan cargadas.
            ' Una venta pagada arranca con el total completo como cobrado, y una a
            ' cuenta en cero.
            venta.MontoPagado = If(cobro.CobradoAhora, venta.Total, 0D)
        End Sub

        ''' <summary>
        ''' Confirma la venta: la registra, descuenta el stock y asienta el cobro en caja.
        '''
        ''' El numero ya esta reservado desde Crear, no se asigna aca.
        '''
        ''' Todo va en un solo bloque con devolucion en caso de fallo. Si algo se rompe a
        ''' mitad de camino, el stock vuelve y la venta no queda registrada ni cobrada:
        ''' no puede existir una venta cobrada sin los productos que la justifican.
        ''' </summary>
        Public Sub Confirmar(venta As Venta)
            If venta Is Nothing Then
                Throw New ArgumentNullException(NameOf(venta))
            End If
            If venta.EstaAnulada Then
                Throw New InvalidOperationException("No se puede confirmar una venta anulada.")
            End If

            Try
                Datos.RegistrarVenta(venta)
                StockService.Descontar(venta)

                ' A cuenta NO toca caja: la mercaderia sale del deposito pero la plata
                ' todavia no entro. El movimiento aparece recien cuando se cobre.
                If venta.EstaPagada Then CajaService.RegistrarVenta(venta)

                ' Con todo aplicado en memoria se guarda en MySQL en una sola
                ' transaccion. Si la base rechaza algo, se cae al Catch de abajo y la
                ' venta se deshace tambien en memoria.
                VentaRepo.GuardarConfirmacion(venta)
            Catch
                Try
                    CajaService.RevertirVenta(venta)
                    StockService.Restaurar(venta)
                Catch
                    ' Si la devolucion tampoco se puede asentar, el movimiento de
                    ' caja quedo sin su reverso: se avisa para que quede a la vista en
                    ' vez de tragarselo y seguir como si la venta no hubiera pasado.
                End Try

                ' La venta no llega a estar registrada: se saca del historial para no
                ' dejarla a medio confirmar.
                If Datos.ListaVentas.Contains(venta) Then Datos.ListaVentas.Remove(venta)

                ' Los movimientos de stock de una venta que no llego a guardarse no
                ' tienen que quedar en el historial.
                Datos.ListaMovimientos.RemoveAll(
                    Function(m) m.ReferenciaID = venta.ID AndAlso
                                (m.Tipo = TipoMovimientoStock.Venta OrElse
                                 m.Tipo = TipoMovimientoStock.Cancelacion))
                Throw
            End Try
        End Sub

        ''' <summary>
        ''' Anula una venta. No la borra: deja el original y asienta por que se dio de
        ''' baja, porque una venta que existio y se corrigio forma parte del historial.
        '''
        ''' Anular dos veces no devuelve unidades de mas ni toca la caja dos veces.
        ''' </summary>
        Public Sub Anular(venta As Venta,
                          Optional motivo As String = Nothing,
                          Optional usuario As String = Nothing)
            If venta Is Nothing Then
                Throw New ArgumentNullException(NameOf(venta))
            End If
            If venta.EstaAnulada Then Return

            ' Anular una venta a cuenta que ya recibio plata no se puede hacer solo:
            ' devolver el stock es facil, pero esa plata hay que devolvérsela al cliente
            ' y eso es una decision, no un efecto secundario. Se frena acá para que
            ' alguien la tome a mano en vez de que el sistema reinvente el dinero.
            If venta.FueACuenta AndAlso venta.MontoPagado > 0D Then
                Throw New InvalidOperationException(
                    "La venta " & venta.Numero & " ya tiene " &
                    venta.MontoPagado.ToString("C2") & " cobrados. " &
                    "Anularla exige devolver esa plata al cliente.")
            End If

            ' Foto de como estaba todo, para poder volver atras si MySQL rechaza la
            ' anulacion: en ese caso la venta tiene que seguir confirmada tambien en
            ' memoria, con su stock, su cobro en caja y su pedido.
            Dim estabaDescontado = venta.StockDescontado
            Dim cajaAntes = If(Datos.Caja Is Nothing, Nothing, Datos.Caja.Movimientos.ToList())
            Dim pedido = Datos.ListaPedidos.FirstOrDefault(
                Function(p) p.VentaID = venta.ID AndAlso p.Estado <> EstadoPedido.Cancelado)
            Dim pedidoEstado = If(pedido Is Nothing, EstadoPedido.Cancelado, pedido.Estado)
            Dim pedidoMotivo = If(pedido Is Nothing, Nothing, pedido.MotivoCancelacion)

            Try
                StockService.Restaurar(venta)
                CajaService.RevertirVenta(venta)
            Catch
                ' Si el stock no se pudo devolver, la venta sigue en pie: queda como
                ' estaba, sin anular, para no perder mercaderia sin avisar.
                Throw
            End Try

            venta.Estado = EstadoVenta.Anulada
            venta.AnuladaFecha = DateTime.Now
            venta.AnuladaMotivo = If(String.IsNullOrWhiteSpace(motivo), "Sin motivo", motivo.Trim())
            venta.AnuladaUsuario = If(usuario, Datos.UsuarioActualNombre())

            ' El pedido de preparacion se da de baja con su venta. Si no, el tablero
            ' seguia mostrando un pedido cuya venta ya no existe, y alguien iba a
            ' preparar mercaderia que ya se devolvio.
            PedidoService.DarDeBajaPorVenta(venta.ID, venta.AnuladaMotivo)

            Try
                VentaRepo.GuardarAnulacion(venta)
            Catch
                If estabaDescontado Then
                    For Each item In venta.Items
                        If item.Producto IsNot Nothing Then item.Producto.Stock -= item.Cantidad
                    Next
                    venta.StockDescontado = True
                End If
                Datos.ListaMovimientos.RemoveAll(
                    Function(m) m.Tipo = TipoMovimientoStock.Cancelacion AndAlso m.ReferenciaID = venta.ID)
                If cajaAntes IsNot Nothing Then
                    Datos.Caja.Movimientos.Clear()
                    Datos.Caja.Movimientos.AddRange(cajaAntes)
                End If
                If pedido IsNot Nothing Then
                    pedido.Estado = pedidoEstado
                    pedido.MotivoCancelacion = pedidoMotivo
                End If
                venta.Estado = EstadoVenta.Confirmada
                venta.AnuladaFecha = Nothing
                venta.AnuladaMotivo = Nothing
                venta.AnuladaUsuario = Nothing
                Throw
            End Try
        End Sub

        ''' <summary>
        ''' Cobra parte o todo lo que falta de una venta a cuenta.
        '''
        ''' Acepta pagos parciales a proposito: un cliente puede saldar en dos visitas y
        ''' cada pago queda asentado por separado en caja. Cuando el saldo llega a cero
        ''' la venta pasa a Pagada sola, sin que nadie la toque a mano.
        '''
        ''' No se cobra una venta anulada ni una que ya esta saldada, ni se cobra mas de lo
        ''' que debe: cada uno de esos casos tiraria un error que el que llama tiene que
        ''' ver, no un ajuste silencioso.
        ''' </summary>
        Public Sub RegistrarPago(venta As Venta,
                                 monto As Decimal,
                                 Optional metodoPago As MetodoPago = MetodoPago.Efectivo,
                                 Optional usuario As String = Nothing)

            If venta Is Nothing Then
                Throw New ArgumentNullException(NameOf(venta))
            End If
            If venta.EstaAnulada Then
                Throw New InvalidOperationException("La venta " & venta.Numero & " esta anulada.")
            End If
            If monto <= 0D Then
                Throw New InvalidOperationException("El importe a cobrar tiene que ser mayor a cero.")
            End If
            If Not venta.TieneSaldo Then
                Throw New InvalidOperationException("La venta " & venta.Numero & " no tiene saldo pendiente.")
            End If
            If monto > venta.Saldo Then
                Throw New InvalidOperationException(
                    "El importe supera el saldo pendiente de " & venta.Saldo.ToString("C2") & ".")
            End If

            Dim pagadoAntes = venta.MontoPagado
            Dim pagoAntes = venta.Pago
            Dim metodoAntes = venta.MetodoPago
            Dim movimientosCajaAntes = If(Datos.Caja Is Nothing, 0, Datos.Caja.Movimientos.Count)

            venta.MontoPagado += monto
            venta.Pago = If(venta.Saldo <= 0D, EstadoPago.Pagada, EstadoPago.Pendiente)
            venta.MetodoPago = metodoPago

            ' El movimiento va por el importe cobrado, no por el total: una deuda
            ' saldada en dos pagos suma en caja lo que realmente entro.
            CajaService.RegistrarCobroDeuda(venta, monto, usuario)

            ' Si la caja estaba abierta, RegistrarCobroDeuda acaba de agregar un
            ' movimiento: es el ultimo de la lista.
            Dim movCaja As MovimientoCaja = Nothing
            If Datos.Caja IsNot Nothing AndAlso Datos.Caja.Movimientos.Count > movimientosCajaAntes Then
                movCaja = Datos.Caja.Movimientos.Last()
            End If

            Try
                VentaRepo.GuardarPago(venta, monto, metodoPago, movCaja)
            Catch
                venta.MontoPagado = pagadoAntes
                venta.Pago = pagoAntes
                venta.MetodoPago = metodoAntes
                If movCaja IsNot Nothing Then Datos.Caja.Movimientos.Remove(movCaja)
                Throw
            End Try
        End Sub

        ''' <summary>
        ''' Ventas que todavia tienen plata pendiente. Sin clave devuelve todas.
        '''
        ''' Las anuladas nunca aparecen: una venta que se dio de baja no genera deuda.
        ''' </summary>
        Public Function Pendientes(Optional claveCliente As String = Nothing) As List(Of Venta)
            Dim lista = Datos.ListaVentas.Where(Function(v) v.TieneSaldo)
            If Not String.IsNullOrWhiteSpace(claveCliente) Then
                lista = lista.Where(Function(v) ClienteService.ClaveDe(v.ClienteNombre, v.ClienteTelefono) = claveCliente)
            End If
            Return lista.ToList()
        End Function

        ''' <summary>Total que debe un cliente en todas sus ventas pendientes.</summary>
        Public Function DeudaDe(claveCliente As String) As Decimal
            Return Pendientes(claveCliente).Sum(Function(v) v.Saldo)
        End Function

        Public Function Listar(Optional soloConfirmadas As Boolean = False) As List(Of Venta)
            If soloConfirmadas Then
                Return Datos.ListaVentas.Where(Function(v) Not v.EstaAnulada).ToList()
            End If
            Return Datos.ListaVentas.ToList()
        End Function

        ''' <summary>
        ''' Venta por id. El id cero devuelve Nothing y no la primera de la lista: es lo
        ''' que pasa con un pedido que todavia no se cobro.
        ''' </summary>
        Public Function Buscar(ventaID As Integer) As Venta
            If ventaID <= 0 Then Return Nothing
            Return Datos.ListaVentas.FirstOrDefault(Function(v) v.ID = ventaID)
        End Function

        Public Function Anuladas() As List(Of Venta)
            Return Datos.ListaVentas.Where(Function(v) v.EstaAnulada).ToList()
        End Function

        ''' <summary>Total cobrado en un rango, sin contar lo anulado.</summary>
        Public Function TotalCobrado(desde As DateTime, hasta As DateTime) As Decimal
            Return Datos.ListaVentas.
                Where(Function(v) Not v.EstaAnulada AndAlso
                           v.FechaHora >= desde AndAlso v.FechaHora <= hasta).
                Sum(Function(v) v.Total)
        End Function

    End Module

End Namespace
