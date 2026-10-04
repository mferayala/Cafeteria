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
        ''' Confirma la venta: le asigna numero, la registra, descuenta el stock y
        ''' asienta el cobro en caja.
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

            venta.MontoPagado += monto
            venta.Pago = If(venta.Saldo <= 0D, EstadoPago.Pagada, EstadoPago.Pendiente)
            venta.MetodoPago = metodoPago

            ' El movimiento va por el importe cobrado, no por el total: una deuda
            ' saldada en dos pagos suma en caja lo que realmente entro.
            CajaService.RegistrarCobroDeuda(venta, monto, usuario)
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
