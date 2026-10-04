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
                CajaService.RegistrarVenta(venta)
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
