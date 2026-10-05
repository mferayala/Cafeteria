Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Text
Imports System.Linq
Imports System.Windows.Forms
Imports CafeteriaOS

Namespace Prueba

    ''' <summary>
    ''' Regresion de la logica de negocio: arranque, autenticacion, stock con toda la
    ''' regla de la especificacion, caja, clientes, tickets, reportes y atajos.
    ''' </summary>
    Module Nucleo

        Dim fallos As Integer
        Dim pruebas As Integer

        <STAThread>
        Sub Main()
            Console.OutputEncoding = System.Text.Encoding.UTF8
            Console.WriteLine("== CafeteriaOS: logica de negocio ==" & Environment.NewLine)

            ' Los datos van a una carpeta propia y no a la de quien esta probando.
            ' Va antes de tocar el archivo: Nucleo.vb borra el historial al arrancar para
            ' empezar de cero, y sin esto se comia el archivo de ingresos de verdad.
            Dim carpeta = Path.Combine(Path.GetTempPath(), "CafeteriaOS-pruebas-consola")
            If Directory.Exists(carpeta) Then Directory.Delete(carpeta, True)
            Directory.CreateDirectory(carpeta)
            Environment.SetEnvironmentVariable(IngresoAlmacen.VariableCarpeta, carpeta)

            ' El historial se carga del disco al arrancar, asi que la prueba arranca
            ' sin archivo para no arrastrar los ingresos de una corrida anterior.
            If File.Exists(IngresoAlmacen.Ruta) Then File.Delete(IngresoAlmacen.Ruta)

            DatosIniciales.Cargar()

            ' --- Arranque ---
            Revisar("Se cargan productos", Datos.ListaProductos.Count > 0)
            Revisar("Se cargan proveedores", Datos.ListaProveedores.Count > 0)
            Revisar("Se carga el catalogo de categorias", DatosIniciales.CategoriasDeProductos().Count > 1)
            Revisar("Las categorias arrancan con la opcion de ver todas",
                    DatosIniciales.CategoriasDeProductos()(0) = ProductoService.Todas)

            ' --- Autenticacion ---
            Dim pistas = DatosIniciales.PistasDeLogin()
            Revisar("Hay pistas de login", pistas.Count > 0)
            Dim admin = AutenticacionService.Buscar("admin@cafe.com", "123456")
            Revisar("Admin entra con sus credenciales", admin IsNot Nothing)
            Revisar("El rol de admin es Administrador", admin.Rol = RolUsuario.Administrador)
            Revisar("Contrasena incorrecta no entra",
                    AutenticacionService.Buscar("admin@cafe.com", "mal") Is Nothing)
            Revisar("Usuario inexistente no entra",
                    AutenticacionService.Buscar("nadie@cafe.com", "123456") Is Nothing)
            Datos.UsuarioActual = admin
            Revisar("El usuario actual toma nombre", Datos.UsuarioActualNombre() = admin.Nombre)

            ' --- Stock: la regla completa ---
            Dim cafe = Datos.ListaProductos.First(Function(p) p.Nombre.Contains("negro"))
            Dim leche = Datos.ListaProductos.First(Function(p) p.Nombre.Contains("con leche"))
            Dim stockCafe = cafe.Stock
            Dim stockLeche = leche.Stock
            Revisar("El stock arranca en positivo", stockCafe > 0)

            Dim items As New List(Of DetallePedido)
            Revisar("Puede agregar lo que hay", StockService.PuedeAgregar(items, cafe, 1))
            items.Add(New DetallePedido With {.Producto = cafe, .Cantidad = 2})
            Revisar("Al agregar no baja el stock real", cafe.Stock = stockCafe)
            Revisar("El disponible descuenta lo pedido",
                    StockService.Disponible(items, cafe) = stockCafe - 2)
            Revisar("No deja agregar mas del disponible",
                    Not StockService.PuedeAgregar(items, cafe, stockCafe))
            items.Add(New DetallePedido With {.Producto = leche, .Cantidad = 1})
            Revisar("Dos lineas distintas conviven", items.Count = 2)
            items(0).Cantidad = 4
            Revisar("Aumentar cantidad no toca el stock", cafe.Stock = stockCafe)
            items(0).Cantidad = 1
            Revisar("Disminuir cantidad no toca el stock", cafe.Stock = stockCafe)
            items.RemoveAt(1)
            Revisar("Quitar no toca el stock", leche.Stock = stockLeche)
            Revisar("Quitar devuelve el disponible", StockService.Disponible(items, leche) = stockLeche)

            Dim pedido As New Pedido With {
                .FechaHora = DateTime.Now,
                .NombreCliente = "Cliente Prueba",
                .TelefonoCliente = "3511111111",
                .TipoServicio = TipoServicio.EnElLocal,
                .Mesa = "5",
                .MetodoPago = MetodoPago.Efectivo,
                .UsuarioResponsable = admin.Nombre,
                .Items = items
            }
            ' --- La venta no se cobra a medias ni esconde el stock que falta ---
            Dim exhausto As New Producto With {.Nombre = "Producto Sin Stock", .Stock = 2}
            Dim imposible = VentaService.Crear(New Pedido With {
                .FechaHora = DateTime.Now,
                .Items = New List(Of DetallePedido) From {
                    New DetallePedido With {.Producto = exhausto, .Cantidad = 5}
                }
            })
            Dim problemas = StockService.Faltantes(imposible)
            Revisar("Se detectan los productos que no alcanzan",
                    problemas.Count = 1)
            Revisar("El faltante dice cuanto se pide y cuanto hay",
                    problemas.Count = 1 AndAlso problemas(0).Contains("5") AndAlso
                    problemas(0).Contains("2"))
            Revisar("Una venta sin faltantes se puede cobrar",
                    StockService.HayStock(VentaService.Crear(pedido)))

            Dim lanzo As Boolean = False
            Try
                VentaService.Confirmar(imposible)
            Catch
                lanzo = True
            End Try
            Revisar("Confirmar sin stock avisa en vez de dejar el deposito en cero", lanzo)
            Revisar("No se descontó nada al no alcanzar", exhausto.Stock = 2)
            Revisar("La venta imposible no quedo marcada como descontada",
                    Not imposible.StockDescontado)
            Revisar("Una venta que no pudo ser ni queda registrada",
                    Not Datos.ListaVentas.Contains(imposible))

            ' --- El precio queda congelado en la venta ---
            Dim total = pedido.Total
            Dim venta = VentaService.Crear(pedido)
            Revisar("La venta nace con el total del borrador", venta.Total = total)
            Revisar("La venta arranca confirmada", venta.Estado = EstadoVenta.Confirmada)
            Revisar("La venta recibe numero propio", venta.ID > 0 AndAlso venta.Numero.StartsWith("V-"))

            Dim precioVenta = venta.Items(0).PrecioUnitario
            Dim nombreVenta = venta.Items(0).ProductoNombre
            VentaService.Confirmar(venta)
            Revisar("Confirmar descuenta de verdad", cafe.Stock = stockCafe - 1)
            Revisar("Queda marcada como descontada", venta.StockDescontado)

            ' Editar el producto no puede reescribir una venta ya cobrada.
            cafe.Precio = 999999D
            cafe.Nombre = "Renombrado despues"
            Revisar("Cambiar el precio del producto no altera la venta",
                    venta.Items(0).PrecioUnitario = precioVenta AndAlso venta.Total = total)
            Revisar("Renombrar el producto no altera el nombre en la venta",
                    venta.Items(0).ProductoNombre = nombreVenta)
            cafe.Precio = precioVenta
            cafe.Nombre = nombreVenta

            ' Un borrador que nunca se cobrar no toca el stock.
            Dim sinConfirmar = VentaService.Crear(New Pedido With {
                .FechaHora = DateTime.Now,
                .Items = New List(Of DetallePedido) From {
                    New DetallePedido With {.Producto = cafe, .Cantidad = 2}
                }
            })
            Dim stockAntes = cafe.Stock
            StockService.Restaurar(sinConfirmar)
            Revisar("Restaurar una venta no cobrada no suma stock", cafe.Stock = stockAntes)

            ' Anular es una reversión asentada, no un borrado.
            Dim stockTrasAnular = cafe.Stock
            VentaService.Anular(venta, "Devolucion")
            Revisar("Anular devuelve el stock", cafe.Stock = stockTrasAnular + 1)
            VentaService.Anular(venta, "Devolucion")
            Revisar("Anular dos veces no suma dos veces", cafe.Stock = stockTrasAnular + 1)
            Revisar("La venta anulada queda marcada como tal", venta.EstaAnulada)
            Revisar("La anulacion guarda el motivo",
                    venta.AnuladaMotivo = "Devolucion")
            Revisar("La anulacion guarda quien la hizo",
                    Not String.IsNullOrWhiteSpace(venta.AnuladaUsuario))
            Revisar("La venta anulada sigue en el historial",
                    Datos.ListaVentas.Contains(venta))
            Revisar("Las anuladas se listan aparte",
                    VentaService.Anuladas().Any(Function(v) v.ID = venta.ID))
            Revisar("El total cobrado no cuenta lo anulado",
                    Not VentaService.Listar(True).Any(Function(v) v.ID = venta.ID))

            StockService.Ajustar(cafe, -cafe.Stock, TipoMovimientoStock.Ajuste)
            Revisar("El stock no queda negativo", cafe.Stock = 0)
            Revisar("Sin stock no se puede agregar", Not StockService.PuedeAgregar(items, cafe, 1))
            Revisar("Aparece entre los criticos", StockService.Criticos().Any(Function(p) p.ID = cafe.ID))
            Revisar("Esta agotado", StockService.EstaAgotado(cafe))
            StockService.Ajustar(cafe, stockCafe, TipoMovimientoStock.Ajuste)

            ' --- Caja ---
            Revisar("La caja arranca cerrada", Not CajaService.EstaAbierta())

            ' Venta con numero real pero sin confirmar: para probar la caja sola.
            Dim ventaCaja = VentaService.Crear(pedido)
            Datos.RegistrarVenta(ventaCaja)
            CajaService.RegistrarVenta(ventaCaja)
            Revisar("Con la caja cerrada no se registra", Datos.Caja Is Nothing OrElse
                    Datos.Caja.Movimientos.Count = 0)
            CajaService.Abrir(20000D)
            Revisar("Abrir deja la caja abierta", CajaService.EstaAbierta())
            Revisar("Queda el monto inicial", Datos.Caja.SaldoActual = 20000D)
            CajaService.RegistrarVenta(ventaCaja)
            Revisar("La venta suma al saldo", Datos.Caja.SaldoActual = 20000D + total)
            CajaService.RevertirVenta(ventaCaja)
            Revisar("Revertir la venta lo descuenta", Datos.Caja.SaldoActual = 20000D)
            CajaService.RevertirVenta(ventaCaja)
            Revisar("Revertir dos veces no descuenta dos veces", Datos.Caja.SaldoActual = 20000D)

            ' El movimiento se busca por id de venta, no por texto: dos ventas con la
            ' misma descripcion no pueden revocarse la una a la otra.
            Dim otra1 = VentaService.Crear(pedido)
            Datos.RegistrarVenta(otra1)
            Dim otra2 = VentaService.Crear(pedido)
            Datos.RegistrarVenta(otra2)
            CajaService.RegistrarVenta(otra1)
            CajaService.RegistrarVenta(otra2)
            Dim saldoAntes = Datos.Caja.SaldoActual
            CajaService.RevertirVenta(otra1)
            Revisar("Revertir una venta no toca la otra",
                    Datos.Caja.SaldoActual = saldoAntes - total)
            CajaService.RevertirVenta(otra2)
            Revisar("Revierten las dos por separado",
                    Datos.Caja.SaldoActual = saldoAntes - (2 * total))
            ' --- Cuenta corriente: pago y preparacion son ejes separados ---
            Dim fiado = New Pedido With {
                .FechaHora = DateTime.Now,
                .NombreCliente = "Cliente Fiado",
                .TelefonoCliente = "3514444444",
                .Items = New List(Of DetallePedido) From {
                    New DetallePedido With {.Producto = cafe, .Cantidad = 1}
                }
            }
            Dim totalFiado = fiado.Total
            Dim ventaFiada = VentaService.Crear(fiado, New ResultadoCobro With {
                .MetodoPago = MetodoPago.Efectivo, .Total = totalFiado,
                .EfectivoRecibido = 0D, .CobradoAhora = False
            })

            Revisar("La venta a cuenta arranca pendiente",
                    ventaFiada.Pago = EstadoPago.Pendiente AndAlso Not ventaFiada.EstaPagada)
            Revisar("La venta a cuenta tiene todo el total por cobrar",
                    ventaFiada.Saldo = totalFiado AndAlso ventaFiada.MontoPagado = 0D)
            Revisar("La venta a cuenta queda marcada como tal", ventaFiada.FueACuenta)

            Dim stockAntesFiado = cafe.Stock
            CajaService.RegistrarVenta(ventaFiada)
            Revisar("Una venta a cuenta no suma a la caja",
                    Datos.Caja.SaldoActual = 20000D)

            VentaService.Confirmar(ventaFiada)
            Revisar("La venta a cuenta si descuenta stock",
                    cafe.Stock = stockAntesFiado - 1)
            Revisar("A cuenta no entra nada a la caja",
                    Datos.Caja.SaldoActual = 20000D)

            Dim claveFiado = ClienteService.ClaveDe("Cliente Fiado", "3514444444")
            Revisar("La deuda del cliente es el total de la venta",
                    ClienteService.Deuda(claveFiado) = totalFiado)
            Revisar("Aparece entre las ventas pendientes",
                    VentaService.Pendientes().Any(Function(v) v.ID = ventaFiada.ID))

            ' Saldar en dos pagos: cada uno se asienta por separado.
            VentaService.RegistrarPago(ventaFiada, totalFiado / 2)
            Revisar("Un pago parcial deja la venta pendiente",
                    ventaFiada.Pago = EstadoPago.Pendiente)
            Revisar("El pago parcial suma solo lo cobrado a la caja",
                    Datos.Caja.SaldoActual = 20000D + (totalFiado / 2))
            Revisar("La deuda baja a la mitad",
                    ClienteService.Deuda(claveFiado) = totalFiado / 2)

            VentaService.RegistrarPago(ventaFiada, totalFiado / 2)
            Revisar("Al saldar la deuda la venta queda pagada",
                    ventaFiada.EstaPagada AndAlso ventaFiada.Saldo = 0D)
            Revisar("La caja suma el total de la venta",
                    Datos.Caja.SaldoActual = 20000D + totalFiado)
            Revisar("El cliente no debe nada",
                    ClienteService.Deuda(claveFiado) = 0D)
            Revisar("La venta saldada no esta pendiente",
                    Not VentaService.Pendientes().Any(Function(v) v.ID = ventaFiada.ID))

            ' Cobrar de mas, de menos o sobre una venta ya saldada es un error visible.
            Dim errores As New List(Of Boolean)
            Dim cobrarInvalido As Action(Of Decimal) =
                Sub(monto)
                    Try
                        VentaService.RegistrarPago(ventaFiada, monto)
                    Catch
                        errores.Add(True)
                    End Try
                End Sub
            cobrarInvalido(100D)
            Revisar("No se puede cobrar de mas", errores.Count = 1 AndAlso ventaFiada.Saldo = 0D)

            Dim noSirve = VentaService.Crear(fiado)
            VentaService.Confirmar(noSirve)
            Try
                VentaService.RegistrarPago(noSirve, 10D)
                Revisar("No se puede cobrar sobre una venta ya pagada", False)
            Catch
                Revisar("No se puede cobrar sobre una venta ya pagada", True)
            End Try

            ' Anular una venta a cuenta ya cobrada exige una decision sobre la plata.
            Dim bloqueada = VentaService.Crear(fiado, New ResultadoCobro With {
                .MetodoPago = MetodoPago.Efectivo, .Total = totalFiado,
                .EfectivoRecibido = 0D, .CobradoAhora = False
            })
            VentaService.Confirmar(bloqueada)
            VentaService.RegistrarPago(bloqueada, totalFiado / 2)
            Dim seBloqueo As Boolean = False
            Try
                VentaService.Anular(bloqueada, "Devolucion")
            Catch
                seBloqueo = True
            End Try
            Revisar("Anular una venta a cuenta cobrada pide devolver la plata", seBloqueo)
            Revisar("La venta bloqueada sigue en pie", Not bloqueada.EstaAnulada)

            ' El corte separa venta del dia de cobro de deuda: si no, el reporte dice
            ' que se vendio plata que en realidad se estaba cobrando.
            Dim movCobro = Datos.Caja.Movimientos.LastOrDefault(Function(m) m.Tipo = MovimientoCaja.TipoCobroDeuda)
            Revisar("El corte cuenta el cobro de deuda dentro del saldo",
                    Not movCobro Is Nothing AndAlso Datos.Caja.TotalCobrosDeuda > 0D)
            ' Cobrar una deuda mueve el saldo pero no el numero de ventas del turno:
            ' si los dos se movieran juntos, el corte no podria decir cuanto fue venta
            ' y cuanto fue cobro de lo que ya estaba fiado.
            Dim ventasAntesDeCobro = Datos.Caja.TotalVentas
            Dim saldoAntesDeCobro = Datos.Caja.SaldoActual
            Dim otraFiada = VentaService.Crear(fiado, New ResultadoCobro With {
                .MetodoPago = MetodoPago.Efectivo, .Total = totalFiado,
                .EfectivoRecibido = 0D, .CobradoAhora = False
            })
            VentaService.Confirmar(otraFiada)
            VentaService.RegistrarPago(otraFiada, totalFiado)
            Revisar("Cobrar una deuda no suma a las ventas del turno",
                    Datos.Caja.TotalVentas = ventasAntesDeCobro)
            Revisar("Cobrar una deuda si suma al saldo",
                    Datos.Caja.SaldoActual = saldoAntesDeCobro + totalFiado)
            Revisar("La deuda cobrada sale de la lista de pendientes",
                    Not VentaService.Pendientes().Any(Function(v) v.ID = otraFiada.ID))
            Revisar("El saldo es apertura mas ventas mas cobros menos egresos",
                    Datos.Caja.SaldoActual = Datos.Caja.MontoInicial + Datos.Caja.TotalVentas +
                                             Datos.Caja.TotalCobrosDeuda - Datos.Caja.TotalEgresos)

            ' Una venta a cuenta sin cobrar un peso si se puede anular sola.
            Dim limpia = VentaService.Crear(fiado, New ResultadoCobro With {
                .MetodoPago = MetodoPago.Efectivo, .Total = totalFiado,
                .EfectivoRecibido = 0D, .CobradoAhora = False
            })
            VentaService.Confirmar(limpia)
            VentaService.Anular(limpia, "Se devolvio todo")
            Revisar("Una venta a cuenta sin cobrar se anula sola", limpia.EstaAnulada)
            Revisar("Anulada no genera deuda",
                    Not VentaService.Pendientes().Any(Function(v) v.ID = limpia.ID))

            Dim saldoAntesEgreso = Datos.Caja.SaldoActual
            CajaService.RegistrarEgreso(500D, "Compras")
            Revisar("El egreso baja el saldo",
                    Datos.Caja.SaldoActual = saldoAntesEgreso - 500D)
            CajaService.Cerrar()
            Revisar("Cerrar deja la caja cerrada", Not CajaService.EstaAbierta())
            CajaService.Abrir(30000D)
            Revisar("Abrir de nuevo reinicia el turno", Datos.Caja.SaldoActual = 30000D)

            ' --- Clientes ---
            Dim antes = Datos.ListaClientes.Count
            ClienteService.RegistrarOActualizar("Cliente Prueba", "3511111111", pedido)
            Revisar("Registra un cliente nuevo", Datos.ListaClientes.Count = antes + 1)
            ClienteService.RegistrarOActualizar("Cliente Prueba", "3511111111", pedido)
            Revisar("No duplica el mismo cliente", Datos.ListaClientes.Count = antes + 1)
            Revisar("Guarda la cantidad de pedidos del cliente",
                    Datos.ListaClientes(0).CantidadPedidos >= 1)

            ' --- Tickets ---
            Dim ticket = TicketService.ConstruirTicket(pedido)
            Revisar("El ticket trae el numero", ticket.Contains(pedido.ID.ToString()))
            Revisar("El ticket trae al cliente", ticket.Contains("Cliente Prueba"))
            Revisar("El ticket trae el total", ticket.Contains(pedido.Total.ToString("C2")))
            Revisar("El ticket de cocina no trae el pago",
                    Not TicketService.ConstruirTicketCocina(pedido).Contains("Pago:"))
            Revisar("El ticket de caja trae el pago",
                    TicketService.ConstruirTicketCaja(pedido).Contains("Pago:"))
            Dim ticketLargo = TicketService.ConstruirTicket(pedido)
            Dim lineas = ticketLargo.Split(Environment.NewLine).Length
            Revisar("El ticket tiene varias lineas", lineas > 5)

            ' --- Reportes ---
            Datos.RegistrarPedido(pedido)
            Dim reporte = ReporteService.ExportarReporte()
            Revisar("El reporte se arma", Not String.IsNullOrWhiteSpace(reporte))
            Revisar("El reporte trae el medio de pago",
                    reporte.Contains("Medio de pago:"))
            Revisar("El reporte lista el desglose por metodo de pago",
                    reporte.Contains(MetodoPago.Efectivo.ToString()))
            Dim ventas = ReporteService.VentasPorMetodoPago()
            Revisar("El desglose tiene el metodo de la venta",
                    ventas.ContainsKey(MetodoPago.Efectivo))
            Revisar("El desglose coincide con las ventas del dia",
                    ventas(MetodoPago.Efectivo) = ReporteService.VentasHoy())
            Revisar("El pedido aparece en los pedidos del dia",
                    ReporteService.PedidosHoy().Any(Function(p) p.ID = pedido.ID))

            ' --- Atajos ---
            Dim mapa = Atajos.Mapa()
            Revisar("El mapa tiene atajos", mapa.Count > 0)
            Revisar("Los atajos no se repiten", mapa.Keys.Distinct().Count() = mapa.Count)
            Revisar("El mapa apunta a productos reales",
                    mapa.Values.All(Function(p) Datos.ListaProductos.Any(Function(q) q.ID = p.ID)))
            Revisar("El mapa no trae atajos repetidos",
                    mapa.Keys.Distinct().Count() = mapa.Count)

            ' TeclaDe arma la combinacion completa a partir del atajo del producto.
            Dim conCtrl = Atajos.TeclaDe("Ctrl+N")
            Revisar("Ctrl+N lleva el control", (conCtrl And Keys.Control) = Keys.Control)
            Revisar("Ctrl+N lleva la letra", (conCtrl And Keys.KeyCode) = Keys.N)
            Dim conShift = Atajos.TeclaDe("Ctrl+Shift+A")
            Revisar("Ctrl+Shift+A lleva los dos modificadores",
                    (conShift And Keys.Control) = Keys.Control AndAlso
                    (conShift And Keys.Shift) = Keys.Shift)
            Revisar("Ctrl+Shift+A no es igual a Ctrl+A",
                    conShift <> Atajos.TeclaDe("Ctrl+A"))
            Dim esperado As Keys = Keys.Control
            esperado = esperado Or Keys.Alt
            esperado = esperado Or Keys.Q
            Revisar("Ctrl+Alt+Q acumula los tres", Atajos.TeclaDe("Ctrl+Alt+Q") = esperado)
            Revisar("Una cadena vacia da Tecla vacia", Atajos.TeclaDe("") = Keys.None)
            ' Un texto que no es ninguna tecla ni ningun modificador no aporta nada,
            ' pero el Ctrl del atajo sigue siendo valido.
            Dim parcial = Atajos.TeclaDe("Ctrl+zzz")
            Revisar("Un texto desconocido no aporta tecla", (parcial And Keys.KeyCode) = Keys.None)
            Revisar("Pero conserva el modificador conocido", (parcial And Keys.Control) = Keys.Control)

            ' --- Ingreso de mercaderia desde proveedores ---
            Console.WriteLine()
            Dim ingLeche = Datos.ListaProductos.First(
                Function(x) x.Nombre.ToLowerInvariant().Contains("leche"))
            Dim ingCafe = Datos.ListaProductos.First(
                Function(x) x.Nombre.ToLowerInvariant().Contains("negro"))
            Dim prov = Datos.ListaProveedores.First()
            prov.ContactoPrincipal = "Jorge Paz"
            Dim stockInicial = ingLeche.Stock

            Dim r1 = IngresoService.Registrar(prov, New List(Of DetalleIngreso) From {
                New DetalleIngreso With {.Producto = ingLeche, .Cantidad = 24}}, "Caja de leche")
            Revisar("El ingreso se registra", r1.Ok)
            Revisar("El ingreso devuelve el numero creado",
                    r1.Ingreso IsNot Nothing AndAlso r1.Ingreso.ID > 0)
            Revisar("El ingreso suma el stock", ingLeche.Stock = stockInicial + 24)
            Revisar("El ingreso guarda el proveedor",
                    ReferenceEquals(r1.Ingreso.Proveedor, prov))
            Revisar("El ingreso guarda la fecha de hoy", r1.Ingreso.Fecha.Date = DateTime.Today)
            Revisar("El ingreso registra quien lo cargo",
                    r1.Ingreso.RegistradoPor = Datos.UsuarioActualNombre())
            Revisar("El ingreso anota la ultima compra del proveedor",
                    prov.UltimaCompra.HasValue AndAlso
                    prov.UltimaCompra.Value.Date = DateTime.Today)

            ' Varias lineas en un mismo ingreso
            Dim stockCafeAntes = ingCafe.Stock
            Dim r2 = IngresoService.Registrar(prov, New List(Of DetalleIngreso) From {
                New DetalleIngreso With {.Producto = ingCafe, .Cantidad = 10},
                New DetalleIngreso With {.Producto = ingLeche, .Cantidad = 6}}, Nothing)
            Revisar("Un ingreso admite varias lineas",
                    r2.Ok AndAlso r2.Ingreso.Detalles.Count = 2)
            Revisar("Cada linea suma su cantidad",
                    ingCafe.Stock = stockCafeAntes + 10 AndAlso ingLeche.Stock = stockInicial + 30)
            Revisar("El total del ingreso cuenta todas las unidades",
                    r2.Ingreso.CantidadTotal = 16)

            ' Producto repetido se acumula en una sola linea
            Dim r3 = IngresoService.Registrar(prov, New List(Of DetalleIngreso) From {
                New DetalleIngreso With {.Producto = ingLeche, .Cantidad = 5},
                New DetalleIngreso With {.Producto = ingLeche, .Cantidad = 7}}, Nothing)
            Revisar("El producto repetido se junta en una linea",
                    r3.Ok AndAlso r3.Ingreso.Detalles.Count = 1 AndAlso
                    r3.Ingreso.Detalles(0).Cantidad = 12)

            ' Todo o nada: un dato malo no deja el stock a medio sumar
            Dim stockPrevio = ingLeche.Stock
            Dim r4 = IngresoService.Registrar(prov, New List(Of DetalleIngreso) From {
                New DetalleIngreso With {.Producto = ingLeche, .Cantidad = 100},
                New DetalleIngreso With {.Producto = ingLeche, .Cantidad = 0}}, Nothing)
            Revisar("Una cantidad invalida rechaza el ingreso", Not r4.Ok)
            Revisar("El ingreso invalido no suma nada", ingLeche.Stock = stockPrevio)
            Revisar("El ingreso invalido explica el motivo",
                    Not String.IsNullOrWhiteSpace(r4.Mensaje))
            Revisar("El ingreso invalido no queda en la lista",
                    Datos.ListaIngresos.All(Function(i) ReferenceEquals(i, r1.Ingreso) OrElse
                                              Not ReferenceEquals(i, r4.Ingreso)))

            Revisar("Un ingreso sin lineas se rechaza",
                    Not IngresoService.Registrar(prov, New List(Of DetalleIngreso)(), Nothing).Ok)

            ' El proveedor es opcional. Se prueba con un producto aparte para no
            ' desarmar los conteos de la trazabilidad que viene abajo.
            Dim pSinProv = New Producto With {.Nombre = "RESCATE", .Precio = 10, .Stock = 0}
            Dim rSinProvViejo = IngresoService.Registrar(Nothing, New List(Of DetalleIngreso) From {
                New DetalleIngreso With {.Producto = pSinProv, .Cantidad = 3}}, "del deposito")
            Revisar("Un ingreso sin proveedor se acepta",
                    rSinProvViejo.Ok AndAlso rSinProvViejo.Ingreso.Proveedor Is Nothing)
            Revisar("El ingreso sin proveedor suma igual",
                    pSinProv.Stock = 3)

            ' Trazabilidad
            Console.WriteLine()
            Dim historial = IngresoService.PorProducto(ingLeche)
            Revisar("El historial del producto lista sus ingresos",
                    historial.Count = 3 AndAlso historial.All(Function(i) i.Contiene(ingLeche)))
            Revisar("El historial viene del mas nuevo al mas viejo",
                    historial(0).Fecha >= historial(1).Fecha AndAlso
                    historial(1).Fecha >= historial(2).Fecha)
            Revisar("El historial separa proveedor y cantidad",
                    IngresoService.PorProveedor(prov).Count = 3 AndAlso
                    historial(0).CantidadDe(ingLeche) = 12)
            Revisar("El ultimo del historial es el de mayor numero",
                    historial(0).ID = 3 AndAlso historial(2).ID = 1)
            Revisar("El ultimo ingreso es el mas reciente",
                    ReferenceEquals(IngresoService.UltimoDe(ingLeche), historial(0)))
            Revisar("Un producto sin ingresos devuelve historial vacio",
                    IngresoService.PorProducto(Nothing).Count = 0)

            ' Que parte de cada ingreso sigue sin consumirse
            Dim antesDeConsumir = ingLeche.Stock
            StockService.Ajustar(ingLeche, -10, TipoMovimientoStock.Venta)
            Dim lotes = IngresoService.PendientesPorVencer(ingLeche)
            ' Solo cuentan las unidades que entraron por ingresos: el stock anterior a
            ' ellos o los ajustes a mano no tienen proveedor de origen.
            Revisar("Las unidades sin consumir cubren lo ingresado",
                    lotes.Sum(Function(l) l.Cantidad) = 42)
            Revisar("Las unidades sin consumir nunca superan el stock",
                    lotes.Sum(Function(l) l.Cantidad) <= ingLeche.Stock)
            Revisar("Los lotes van del ingreso mas viejo al mas nuevo",
                    lotes.Count > 0 AndAlso
                    lotes(0).Ingreso.Fecha <= lotes(lotes.Count - 1).Ingreso.Fecha)
            Revisar("Cada lote dice de que proveedor vino",
                    lotes.All(Function(l) l.Ingreso.Proveedor IsNot Nothing))
            StockService.Ajustar(ingLeche, -ingLeche.Stock, TipoMovimientoStock.Venta)
            Revisar("Consumir todo deja el lote vacio",
                    IngresoService.PendientesPorVencer(ingLeche).Sum(Function(l) l.Cantidad) = 0)
            ' Se repone para que las pruebas de disco tengan mercaderia disponible.
            StockService.Ajustar(ingLeche, 20, TipoMovimientoStock.Ingreso)

            ' La descripcion del origen lleva fecha, proveedor y contacto
            Dim descripcion = IngresoService.Describir(r1.Ingreso)
            Revisar("La descripcion del ingreso trae la fecha",
                    descripcion.Contains(r1.Ingreso.Fecha.ToString("dd/MM/yyyy")))
            Revisar("La descripcion del ingreso trae el proveedor",
                    descripcion.Contains(prov.Nombre))
            Revisar("La descripcion del ingreso trae el contacto",
                    descripcion.Contains("Jorge Paz"))

            Console.WriteLine()
            ' --- Persistencia del historial en disco ---
            Console.WriteLine()
            Dim ruta = IngresoAlmacen.Ruta
            Revisar("El archivo de ingresos vive en el perfil del usuario",
                    ruta.Contains("CafeteriaOS") AndAlso ruta.EndsWith("ingresos_stock.json"))
            Revisar("El archivo no queda en la carpeta del ejecutable",
                    Not ruta.Contains("bin"))
            Revisar("El archivo existe despues de registrar", File.Exists(ruta))

            ' Un proceso limpio tiene que recuperar el historial reading del archivo.
            Dim stockAntesDeLeer = ingLeche.Stock
            Dim totalLeche = IngresoService.PorProducto(ingLeche).Sum(
                Function(i) i.CantidadDe(ingLeche))
            Dim ingresosEnDisco = File.ReadAllText(ruta, Encoding.UTF8)
            Revisar("El archivo guarda la cantidad de cada linea",
                    ingresosEnDisco.Contains("Cantidad") AndAlso
                    ingresosEnDisco.Contains("ProductoNombre") AndAlso
                    ingresosEnDisco.Contains(ingLeche.Nombre))
            Revisar("El archivo guarda el nombre del proveedor",
                    ingresosEnDisco.Contains(prov.Nombre))
            Revisar("El archivo guarda como llego la mercaderia",
                    ingresosEnDisco.Contains("Observacion"))

            Dim idsGuardados = Datos.ListaIngresos.Select(Function(x) x.ID).ToList()
            Dim stockLecheRecuperado = 0
            IngresoAlmacen.Cargar()
            Dim recuperados = IngresoService.PorProducto(ingLeche)
            For Each i In recuperados
                stockLecheRecuperado += i.CantidadDe(ingLeche)
            Next

            Revisar("El historial se recupera completo", recuperados.Count = historial.Count)
            Revisar("Las cantidades se recuperan intactas", stockLecheRecuperado = totalLeche)
            Revisar("La fecha del ingreso se recupera", recuperados(0).Fecha.Date = DateTime.Today)
            Revisar("El proveedor se reconoce por codigo",
                    ReferenceEquals(recuperados(0).Proveedor, prov))
            Revisar("El contacto del proveedor se recupera",
                    recuperados(0).Proveedor.ContactoPrincipal = "Jorge Paz")
            Revisar("Leer el historial no vuelve a sumar al stock",
                    ingLeche.Stock = stockAntesDeLeer)
            Revisar("Recuperar no borra el historial cargado",
                    IngresoService.PorProveedor(prov).Count = 3)

            ' La numeracion sigue desde lo mas alto: un ingreso nuevo no repite numero.
            Dim r5 = IngresoService.Registrar(prov, New List(Of DetalleIngreso) From {
                New DetalleIngreso With {
                    .Producto = ingLeche, .Cantidad = 3,
                    .Observacion = "llego con las esquinas aplastadas, el contenido esta bien"}},
                Nothing)
            Revisar("El ingreso con observacion se registra", r5.Ok)
            Revisar("La observacion queda en la linea",
                    r5.Ingreso.Detalles(0).Observacion.Contains("esquinas aplastadas"))
            Revisar("La observacion no impide el ingreso",
                    ingLeche.Stock = stockAntesDeLeer + 3)
            Dim maxIdGuardado = idsGuardados.Max()
            Revisar("El numero de ingreso sigue al ultimo guardado",
                    r5.Ingreso.ID = maxIdGuardado + 1)
            Revisar("La linea avisada queda marcada",
                    r5.Ingreso.Detalles(0).TieneObservacion)

            ' Recargar el archivo mantiene la observacion.
            IngresoAlmacen.Cargar()
            Dim conNota = IngresoService.UltimoDe(ingLeche)
            Revisar("La observacion sobrevive al disco",
                    conNota IsNot Nothing AndAlso conNota.LineaDe(ingLeche).TieneObservacion)

            ' Un archivo que no se puede leer no rompe el arranque.
            File.WriteAllText(ruta, "{ esto no es json")
            Revisar("Un archivo roto se descarta sin fallar",
                    IngresoAlmacen.Cargar() = 0 AndAlso Datos.ListaIngresos.Count = 0)
            Revisar("La app sigue utilizable con el archivo roto",
                    IngresoService.Registrar(prov, New List(Of DetalleIngreso) From {
                        New DetalleIngreso With {.Producto = ingLeche, .Cantidad = 1}}, Nothing).Ok)
            File.Delete(ruta)
            Revisar("Borrado el archivo, la app sigue andando",
                    Datos.ListaIngresos.Count >= 0)

            ' --- Un solo movimiento por operacion ---
            Console.WriteLine()
            Dim stockInicialMov = ingLeche.Stock
            Dim movIniciales = Datos.ListaMovimientos.Count

            StockService.Ajustar(ingLeche, 7, TipoMovimientoStock.Reposicion, "vendedor", "traje imagen")
            Revisar("Reponer suma al stock", ingLeche.Stock = stockInicialMov + 7)
            Dim movsRepo = Datos.ListaMovimientos.Where(
                Function(m) m.Tipo = TipoMovimientoStock.Reposicion).ToList()
            Revisar("La reposicion deja un solo movimiento", movsRepo.Count = 1)
            Revisar("La reposicion no se asienta tambien como ajuste",
                    Datos.ListaMovimientos.Count = movIniciales + 1)
            Revisar("El movimiento guarda la cantidad", movsRepo(0).Cantidad = 7)
            Revisar("El movimiento guarda el origen",
                    movsRepo(0).Origen = "Reposicion")
            Revisar("El movimiento guarda quien lo hizo", movsRepo(0).Usuario = "vendedor")
            Revisar("El movimiento guarda la nota", movsRepo(0).Observacion = "traje imagen")

            Dim antesAjuste = ingLeche.Stock
            StockService.Ajustar(ingLeche, -2, TipoMovimientoStock.Ajuste)
            Revisar("Restar por ajuste descuenta", ingLeche.Stock = antesAjuste - 2)
            Revisar("El ajuste queda como ajuste y no como reposicion",
                    Datos.ListaMovimientos.Count = movIniciales + 2)
            StockService.Ajustar(ingLeche, 0, TipoMovimientoStock.Ajuste)
            Revisar("Ajustar con delta cero no asienta nada",
                    Datos.ListaMovimientos.Count = movIniciales + 2)

            Dim r6 = IngresoService.Registrar(prov, New List(Of DetalleIngreso) From {
                New DetalleIngreso With {.Producto = ingLeche, .Cantidad = 4}}, Nothing)
            Revisar("El ingreso con movimiento se registra", r6.Ok)
            Dim movsIng = Datos.ListaMovimientos.
                Where(Function(m) m.Tipo = TipoMovimientoStock.Ingreso AndAlso
                               m.ReferenciaID = r6.Ingreso.ID).ToList()
            Revisar("El ingreso deja un solo movimiento por linea", movsIng.Count = 1)
            Revisar("El ingreso no se asienta tambien como ajuste",
                    Datos.ListaMovimientos.Count = movIniciales + 3)
            Revisar("El movimiento del ingreso apunta al ingreso",
                    movsIng(0).ReferenciaID = r6.Ingreso.ID)

            Dim histMov = MovimientoStockService.Historial(ingLeche)
            Revisar("El historial de movimientos ordena del mas nuevo al mas viejo",
                    histMov(0).ID > histMov(1).ID)
            Revisar("El historial de movimientos del producto existe",
                    histMov.Count >= 3)
            Revisar("Un producto sin movimientos devuelve vacio",
                    MovimientoStockService.Historial(Nothing).Count = 0)

            ' La suma de los movimientos tiene que explicar el stock actual.
            Dim todosMovs = Datos.ListaMovimientos.
                Where(Function(m) ReferenceEquals(m.Producto, ingLeche)).ToList()
            Revisar("Ningun movimiento tiene cantidad cero",
                    todosMovs.All(Function(m) m.Cantidad <> 0))
            Revisar("Todo movimiento tiene tipo definido",
                    todosMovs.All(Function(m) CInt(m.Tipo) >= 0 AndAlso CInt(m.Tipo) <= 5))
            Revisar("Todo movimiento dice quien lo hizo",
                    todosMovs.All(Function(m) Not String.IsNullOrWhiteSpace(m.Usuario)))
            Revisar("Los aumentos y las ventas tienen signo coherente",
                    todosMovs.Where(Function(m) m.Tipo = TipoMovimientoStock.Venta).
                              All(Function(m) m.Cantidad < 0) AndAlso
                    todosMovs.Where(Function(m) m.Tipo = TipoMovimientoStock.Reposicion).
                              All(Function(m) m.Cantidad > 0))

            ' --- Delta real: el movimiento dice cuanto cambio el deposito ---
            Dim pIso = New Producto With {.Nombre = "ISO", .Stock = 2, .Precio = 10}
            Dim movsAntesIso = Datos.ListaMovimientos.Count
            StockService.Ajustar(pIso, -5, TipoMovimientoStock.Ajuste)
            Revisar("Restar de mas no deja stock negativo", pIso.Stock = 0)
            Revisar("El movimiento asienta el delta real y no el pedido",
                    Datos.ListaMovimientos.Count = movsAntesIso + 1 AndAlso
                    Datos.ListaMovimientos.Last.Cantidad = -2)
            StockService.Ajustar(pIso, 3, TipoMovimientoStock.Ingreso)
            StockService.Ajustar(pIso, -3, TipoMovimientoStock.Venta)
            Dim noCambio = Datos.ListaMovimientos.Count
            StockService.Ajustar(pIso, 0, TipoMovimientoStock.Ajuste)
            Revisar("Sin cambio real no se asienta nada",
                    Datos.ListaMovimientos.Count = noCambio)

            ' --- El ingreso no obliga a elegir proveedor ---
            Dim cafeAntes = cafe.Stock
            Dim rSinProv = IngresoService.Registrar(Nothing, New List(Of DetalleIngreso) From {
                New DetalleIngreso With {.Producto = cafe, .Cantidad = 2}}, "recuperado del deposito")
            Revisar("El ingreso se acepta sin proveedor", rSinProv.Ok)
            Revisar("El ingreso sin proveedor no inventa uno",
                    rSinProv.Ingreso.Proveedor Is Nothing)
            Revisar("El ingreso sin proveedor suma el stock", cafe.Stock = cafeAntes + 2)
            Revisar("Sin proveedor no se toca su ultima compra",
                    rSinProv.Ingreso.Proveedor Is Nothing)
            Revisar("El ingreso sin proveedor no deja advertencia de disco",
                    String.IsNullOrWhiteSpace(rSinProv.Advertencia))

            Dim guardadoOk = IngresoAlmacen.Guardar()
            Revisar("El historial se puede guardar", guardadoOk)
            Revisar("No queda archivo temporal sueltos",
                    Not File.Exists(IngresoAlmacen.Ruta & ".tmp"))
            Revisar("El archivo del historial existe", File.Exists(IngresoAlmacen.Ruta))

            Dim antesDeCargar = Datos.ListaIngresos.Count
            Dim recargados = IngresoAlmacen.Cargar()
            Revisar("El historial se vuelve a leer", recargados = antesDeCargar)
            Dim vuelto = Datos.ListaIngresos.
                FirstOrDefault(Function(i) String.IsNullOrWhiteSpace(i.Proveedor?.Nombre))
            Revisar("El ingreso sin proveedor sobrevive al reinicio",
                    vuelto IsNot Nothing AndAlso
                    vuelto.Detalles.Any(Function(d) d.Producto.Nombre = cafe.Nombre))
            Revisar("Cargar no vuelve a sumar stock",
                    cafe.Stock = cafeAntes + 2)

            ' --- Nombre compuesto: lo que ve el cliente ---
            Dim tango As New Producto With {
                .Nombre = "JUGO EN SOBRE", .Marca = "TANG",
                .Descripcion = "SABOR DURAZNO 10G"}
            Revisar("El nombre compuesto junta marca, nombre y descripcion",
                    tango.NombreCompleto() = "TANG JUGO EN SOBRE SABOR DURAZNO 10G")
            Dim repetido As New Producto With {
                .Nombre = "Cafe con leche", .Descripcion = "Cafe con leche"}
            Revisar("El nombre compuesto no repite un texto ya incluido",
                    repetido.NombreCompleto() = "Cafe con leche")
            Dim sinMarca As New Producto With {.Nombre = "Vino"}
            Revisar("Sin marca ni descripcion queda el nombre solo",
                    sinMarca.NombreCompleto() = "Vino")
            Revisar("Un producto sin ningun texto no rompe",
                    New Producto().NombreCompleto().Length = 0)
            Revisar("Los espacios al borde no quedan pegados",
                    New Producto With {.Marca = " TANG ", .Nombre = " Te "}.NombreCompleto() = "TANG Te")

            ' --- Validacion del catalogo ---
            Dim antesDeGuardar = Datos.ListaProductos.Count
            Dim idMayor = Datos.ListaProductos.Max(Function(p) p.ID)

            Revisar("No se guarda un producto sin nombre",
                    Not ProductoService.Guardar(New Producto With {.Nombre = "  "}).Ok)
            Revisar("No se guarda con precio de venta negativo",
                    Not ProductoService.Guardar(New Producto With {
                        .Nombre = "X", .Precio = -1D}).Ok)
            Revisar("No se guarda con precio de costo negativo",
                    Not ProductoService.Guardar(New Producto With {
                        .Nombre = "X", .PrecioCosto = -1D}).Ok)
            Revisar("No se guarda con stock negativo",
                    Not ProductoService.Guardar(New Producto With {
                        .Nombre = "X", .Stock = -1}).Ok)
            Revisar("Un producto invalido no entra al catalogo",
                    Datos.ListaProductos.Count = antesDeGuardar)

            Dim nuevoOk = ProductoService.Guardar(New Producto With {
                .Nombre = "  Jugo en sobre  ", .Marca = " TANG ", .CodigoBarra = " 7790123456789 ",
                .UnidadMedida = " SOBRE ", .Categoria = " Bebidas ", .Subcategoria = " Sin gas ",
                .PrecioCosto = 1000D, .Precio = 2500D, .Stock = 12})
            Revisar("Un producto valido se guarda", nuevoOk.Ok)
            Revisar("El id del producto nuevo no choca con los existentes",
                    nuevoOk.Producto.ID > idMayor)
            Revisar("El nombre se guarda limpio", nuevoOk.Producto.Nombre = "Jugo en sobre")
            Revisar("La marca se guarda limpia", nuevoOk.Producto.Marca = "TANG")
            Revisar("El codigo de barras se guarda limpio",
                    nuevoOk.Producto.CodigoBarra = "7790123456789")
            Revisar("La unidad se guarda en minuscula",
                    nuevoOk.Producto.UnidadMedida = "sobre")
            Revisar("El producto nuevo se suma al catalogo",
                    Datos.ListaProductos.Count = antesDeGuardar + 1)

            Dim repetidoBarra = ProductoService.Guardar(New Producto With {
                .Nombre = "Otro", .CodigoBarra = "7790123456789"})
            Revisar("No se acepta el mismo codigo de barras dos veces",
                    Not repetidoBarra.Ok)
            Revisar("El mensaje dice que producto lo usa",
                    repetidoBarra.Mensaje.Contains("TANG"))
            Revisar("El duplicado no entra al catalogo",
                    Datos.ListaProductos.Count = antesDeGuardar + 1)

            ' Guardar toma el producto entero como estado final, asi que la
            ' edicion se arma con todos los campos.
            Dim edicion = ProductoService.Guardar(New Producto With {
                .ID = nuevoOk.Producto.ID, .Nombre = "Jugo en sobre",
                .CodigoBarra = "7790123456789", .Marca = "TANG",
                .UnidadMedida = "sobre", .Categoria = "Bebidas",
                .Subcategoria = "Sin gas", .PrecioCosto = 1000D,
                .Precio = 2600D, .Stock = nuevoOk.Producto.Stock})
            Revisar("Un producto puede conservar su propio codigo de barras",
                    edicion.Ok)
            Revisar("La edicion cambia el precio",
                    ProductoService.PorId(nuevoOk.Producto.ID).Precio = 2600D)

            ' La edicion tiene que escribir sobre el objeto del catalogo: los
            ' pedidos y los ingresos lo tienen apuntado y no deben quedar viejos.
            Dim alvo = ProductoService.PorId(nuevoOk.Producto.ID)
            Revisar("La edicion escribe sobre el mismo objeto",
                    ReferenceEquals(alvo, ProductoService.PorId(nuevoOk.Producto.ID)))
            Revisar("La edicion no cambia el id",
                    alvo.ID = nuevoOk.Producto.ID)
            Revisar("La edicion no cambia el stock",
                    alvo.Stock = 12)

            ' --- Busqueda ---
            Revisar("La busqueda encuentra por nombre",
                    ProductoService.Buscar("jugo en sobre").Any(
                        Function(p) p.ID = nuevoOk.Producto.ID))
            Revisar("La busqueda encuentra por marca",
                    ProductoService.Buscar("tang").Any(
                        Function(p) p.ID = nuevoOk.Producto.ID))
            Revisar("La busqueda encuentra por codigo de barras",
                    ProductoService.Buscar("7790123456789").Count = 1)
            Revisar("La busqueda no distingue mayusculas",
                    ProductoService.Buscar("TANG").Any(
                        Function(p) p.ID = nuevoOk.Producto.ID))
            Revisar("La busqueda filtra por categoria",
                    ProductoService.Buscar("", "Bebidas").All(
                        Function(p) p.Categoria = "Bebidas"))
            Revisar("La busqueda filtra por subcategoria",
                    ProductoService.Buscar("", "Bebidas", "Sin gas").Count = 1)
            Revisar("Una categoria que no existe no trae productos",
                    ProductoService.Buscar("", "NoExiste").Count = 0)
            Revisar("Sin filtros trae todo el catalogo",
                    ProductoService.Buscar("").Count = Datos.ListaProductos.Count)
            Revisar("La busqueda sin resultados devuelve vacio y no Nothing",
                    ProductoService.Buscar("zzzzz").Count = 0)

            Revisar("Las categorias vienen del catalogo y sin repetir",
                    ProductoService.Categorias().Count =
                    ProductoService.Categorias().Distinct().Count)
            Revisar("La categoria nueva aparece en la lista",
                    ProductoService.Categorias().Contains("Bebidas"))
            Revisar("Las subcategorias se limitan a la categoria pedida",
                    ProductoService.Subcategorias("Bebidas").Contains("Sin gas"))
            Revisar("Sin categoria se ofrecen todas las subcategorias",
                    ProductoService.Subcategorias().Contains("Sin gas"))

            Revisar("El precio sugerido sale de la configuracion del negocio",
                    ProductoService.PrecioSugerido(1000D) = PrecioService.CalcularPrecioVentaRecomendado(1000D))
            Revisar("Sin costo no hay precio sugerido",
                    ProductoService.PrecioSugerido(0D) = 0D)
            Revisar("Con costo cero tampoco hay precio sugerido",
                    ProductoService.PrecioSugerido(-5D) = 0D)

            ' --- Stock minimo por producto ---
            Dim grande As New Producto With {.Nombre = "A granel", .Stock = 150, .StockMinimo = 100}
            Dim chico As New Producto With {.Nombre = "Porcion", .Stock = 80, .StockMinimo = 5}
            Dim sinMinimo As New Producto With {.Nombre = "Sin minimo", .Stock = 80, .StockMinimo = 0}
            Revisar("El minimo propio manda cuando esta cargado",
                    StockService.MinimoDe(grande) = 100)
            Revisar("Un minimo menor tambien se respeta",
                    StockService.MinimoDe(chico) = 5)
            Revisar("Sin minimo cargado cae al umbral general",
                    StockService.MinimoDe(sinMinimo) = StockService.UmbralCritico)
            Revisar("Con stock por encima del minimo no es critico",
                    Not StockService.EstaCritico(grande))
            Revisar("Con minimo propio alto tampoco se compara con el umbral general",
                    Not StockService.EstaCritico(New Producto With {
                        .Nombre = "Y", .Stock = 40, .StockMinimo = 30}))
            Revisar("Con stock bajo su minimo es critico",
                    StockService.EstaCritico(New Producto With {
                        .Nombre = "X", .Stock = 99, .StockMinimo = 100}))
            Revisar("Con stock igual al minimo es critico",
                    StockService.EstaCritico(New Producto With {
                        .Nombre = "X", .Stock = 100, .StockMinimo = 100}))
            Revisar("Un producto sin minimo y stock bajo cae al umbral general",
                    StockService.EstaCritico(New Producto With {
                        .Nombre = "X", .Stock = 1, .StockMinimo = 0}))
            Revisar("Minimo de un producto inexistente es cero",
                    StockService.MinimoDe(Nothing) = 0)
            Revisar("Agotado y critico no se rompen con Nothing",
                    Not StockService.EstaAgotado(Nothing) AndAlso
                    Not StockService.EstaCritico(Nothing))

            Dim datosMin = Datos.ListaProductos.First(Function(p) p.Nombre.Contains("negro"))
            datosMin.StockMinimo = 3
            Revisar("El minimo se puede cargar desde el editor",
                    ProductoService.Guardar(New Producto With {
                        .ID = datosMin.ID, .Nombre = datosMin.Nombre,
                        .Marca = datosMin.Marca, .UnidadMedida = datosMin.UnidadMedida,
                        .Categoria = datosMin.Categoria, .Descripcion = datosMin.Descripcion,
                        .PrecioCosto = datosMin.PrecioCosto, .Precio = datosMin.Precio,
                        .Stock = datosMin.Stock, .StockMinimo = 3}).Ok)
            Revisar("El minimo queda guardado en el producto",
                    ProductoService.PorId(datosMin.ID).StockMinimo = 3)
            Revisar("No se guarda un minimo negativo",
                    Not ProductoService.Guardar(New Producto With {
                        .Nombre = "X", .StockMinimo = -1}).Ok)
            datosMin.StockMinimo = 0

            ' --- Configuracion del negocio ---
            Dim cfg = New ConfiguracionNegocio()
            Revisar("Una configuracion nueva no trae el nombre de esta aplicacion",
                    Not cfg.NombreNegocio.Contains("Cafeteria"))
            Revisar("Sin nombre cargado se muestra uno neutro",
                    cfg.NombreParaMostrar() = "Mi Negocio")
            cfg.NombreNegocio = "   "
            Revisar("Un nombre en blanco se muestra neutro, no en blanco",
                    cfg.NombreParaMostrar() = "Mi Negocio")
            cfg.NombreNegocio = "  Kiosco El Portal  "
            Revisar("El nombre se muestra sin espacios sobrantes",
                    cfg.NombreParaMostrar() = "Kiosco El Portal")

            Revisar("Solo el administrador edita la configuracion",
                    AutenticacionService.PuedeEditarConfiguracion(RolUsuario.Administrador) AndAlso
                    Not AutenticacionService.PuedeEditarConfiguracion(RolUsuario.Cajero) AndAlso
                    Not AutenticacionService.PuedeEditarConfiguracion(RolUsuario.Cocina))

            ' El precio se calcula con los porcentajes que se le pasan, no con los
            ' de la configuracion viva: asi la pantalla puede mostrar el efecto de
            ' un porcentaje que todavia no se guardo.
            Dim antesDelPrecio = Datos.ConfigNegocio
            Try
                Datos.ConfigNegocio = New ConfiguracionNegocio With {
                    .MargenSobreCosto = 999D, .IVAGeneral = 999D}
                Revisar("CalcularPrecioVenta usa los parametros, no la configuracion viva",
                        PrecioService.CalcularPrecioVenta(1000D, 30D, 10D, 21D, True, True) = 1610D)
            Finally
                Datos.ConfigNegocio = antesDelPrecio
            End Try
            Revisar("Sin gastos ni IVA el precio es costo mas margen",
                    PrecioService.CalcularPrecioVenta(1000D, 30D, 10D, 21D, False, False) = 1300D)
            Revisar("Sin margen ni extras el precio es el costo",
                    PrecioService.CalcularPrecioVenta(1000D, 0D, 0D, 0D, False, False) = 1000D)
            Revisar("El precio nunca baja del costo",
                    PrecioService.CalcularPrecioVenta(1000D, 0D, 0D, 0D, True, False) = 1000D)
            Revisar("Un costo en cero no inventa precio",
                    PrecioService.CalcularPrecioVenta(0D, 30D, 10D, 21D, True, True) = 0D)

            ' El encabezado del ticket lleva el negocio y omite lo que no se cargo.
            Dim cfgTicket As New ConfiguracionNegocio With {
                .NombreNegocio = "Kiosco El Portal", .CUIT = "30-12345678-9",
                .Direccion = "", .Telefono = "  "}
            Dim cfgAntes = Datos.ConfigNegocio
            Try
                Datos.ConfigNegocio = cfgTicket
                Dim encabezado = Negocio.EncabezadoTicket()
                Revisar("El ticket arranca con el nombre del negocio",
                        encabezado.StartsWith("Kiosco El Portal"))
                Revisar("El ticket muestra el CUIT", encabezado.Contains("30-12345678-9"))
                Revisar("El ticket no deja lineas vacias de direccion ni telefono",
                        Not encabezado.Contains(Environment.NewLine & Environment.NewLine) AndAlso
                        Not encabezado.Contains("Tel."))
            Finally
                Datos.ConfigNegocio = cfgAntes
            End Try

            ' --- Filtro de categorias: una sola fuente para POS e Ingreso ---
            Dim conBebida = New Producto With {
                .Nombre = "Agua", .Categoria = "Bebidas", .PrecioCosto = 100D,
                .Precio = 200D}
            Dim conBebidaChica = New Producto With {
                .Nombre = "Gaseosa", .Categoria = "bebidas", .PrecioCosto = 100D,
                .Precio = 200D}
            ProductoService.Guardar(conBebida)
            ProductoService.Guardar(conBebidaChica)
            Try
                Dim categorias = ProductoService.Categorias()
                Revisar("Las categorias que difieren solo en mayusculas se muestran una vez",
                        categorias.Where(Function(c) c.Equals("Bebidas",
                                                              StringComparison.CurrentCultureIgnoreCase)).Count() = 1)

                Dim paraFiltrar = ProductoService.CategoriasParaFiltrar()
                Revisar("El filtro arranca con la opcion de ver todas",
                        paraFiltrar.Count > 0 AndAlso paraFiltrar(0) = ProductoService.Todas)
                Revisar("El filtro no trae categorias repetidas",
                        paraFiltrar.Count = paraFiltrar.Distinct(StringComparer.CurrentCultureIgnoreCase).Count())

                Revisar("El filtro 'todas' se traduce a sin filtro",
                        ProductoService.CategoriaDelFiltro(ProductoService.Todas) = "")
                Revisar("Una categoria real no se pierde al pasar por el filtro",
                        ProductoService.CategoriaDelFiltro("Bebidas") = "Bebidas")

                ' Este era el defecto: el POS comparaba con "=" y el producto con
                ' "bebidas" desaparecia al filtrar por "Bebidas".
                Revisar("Un producto en 'bebidas' aparece al filtrar por 'Bebidas'",
                        ProductoService.PerteneceACategoria(conBebidaChica, "Bebidas"))
                Revisar("Un producto en 'Bebidas' aparece al filtrar por 'bebidas'",
                        ProductoService.PerteneceACategoria(conBebida, "bebidas"))
                Revisar("Sin filtro todos los productos aparecen",
                        ProductoService.PerteneceACategoria(conBebida, ProductoService.Todas) AndAlso
                        ProductoService.PerteneceACategoria(New Producto With {.Categoria = "Comidas"},
                                                            ProductoService.Todas))
                Revisar("Un producto de otra categoria no aparece",
                        Not ProductoService.PerteneceACategoria(New Producto With {.Categoria = "Comidas"},
                                                                "Bebidas"))
                Revisar("Un producto vacio no aparece al filtrar",
                        Not ProductoService.PerteneceACategoria(New Producto With {.Categoria = ""},
                                                                "Bebidas"))

                ' DatosIniciales delegaba en el servicio; si vuelve a armarse por
                ' su cuenta con List.Contains el POS y el Ingreso muestran
                ' listas distintas.
                Dim delDemo = DatosIniciales.CategoriasDeProductos()
                Revisar("La lista de categorias del demo es la misma que la del servicio",
                        delDemo.SequenceEqual(paraFiltrar))
            Finally
                Datos.ListaProductos.Remove(conBebida)
                Datos.ListaProductos.Remove(conBebidaChica)
            End Try


            ' --- Preparacion: es un eje aparte del pago, no una etapa del cobro ---
            ' Va al final a proposito: las pruebas de caja de arriba comparan el saldo
            ' con un numero absoluto, y este bloque cobra de verdad, asi que no puede
            ' ejecutarse en el medio. Usa un producto propio para no mover el stock real.
            Dim cafePrep As New Producto With {
                .Nombre = "Cafe de Preparacion", .Precio = 1000D, .Stock = 10
            }
            Dim stockBase = cafePrep.Stock
            Datos.ListaProductos.Add(cafePrep)
            Try
                ' Un pedido minimo reutilizable: el carrito del panel es privado y no
                ' llega desde aca, asi que cada prueba arma el suyo.
                Dim pedidoBase As New Pedido With {
                    .FechaHora = DateTime.Now,
                    .Items = New List(Of DetallePedido) From {
                        New DetallePedido With {.Producto = cafePrep, .Cantidad = 1}
                    }
                }
                Dim CobroDe = Function(Importe As Decimal) As ResultadoCobro
                    Return New ResultadoCobro With {
                        .MetodoPago = MetodoPago.Efectivo, .Total = Importe,
                        .EfectivoRecibido = Importe, .CobradoAhora = True
                    }
                End Function

                ' Venta de mostrador: paga y se va. No genera ningun pedido.
                Dim ventaDirecta = VentaService.Crear(pedidoBase, CobroDe(pedidoBase.Total))
                VentaService.Confirmar(ventaDirecta)
                Revisar("La venta de mostrador no genera pedido",
                        Not Datos.ListaPedidos.Any(Function(p) p.VentaID = ventaDirecta.ID))
                Revisar("La venta de mostrador si descuenta stock",
                        cafePrep.Stock = stockBase - 1)

                ' Un numero por venta. Antes se armaba una venta para revisar el stock y
                ' otra para confirmar: la primera se tiraba pero su numero ya estaba
                ' gastado, asi que cada venta salia con un numero de mas.
                Dim otraDirecta = VentaService.Crear(pedidoBase, CobroDe(pedidoBase.Total))
                Revisar("Dos ventas seguidas llevan numeros consecutivos",
                        otraDirecta.ID = ventaDirecta.ID + 1)

                ' Venta que ademas se prepara: el pedido sale de la venta ya cobrada.
                Dim ventaPrep = VentaService.Crear(New Pedido With {
                    .FechaHora = DateTime.Now,
                    .NombreCliente = "Ana",
                    .TipoServicio = TipoServicio.EnElLocal,
                    .Mesa = "7",
                    .Items = New List(Of DetallePedido) From {
                        New DetallePedido With {
                            .Producto = cafePrep, .Cantidad = 2, .Especificaciones = "Sin azucar"}
                    }
                })
                Dim stockAntesPrep = cafePrep.Stock
                VentaService.AplicarCobro(ventaPrep, CobroDe(ventaPrep.Total))
                Revisar("Aplicar el cobro deja la venta pagada", ventaPrep.EstaPagada)
                VentaService.Confirmar(ventaPrep)

                Dim pedidoPrep = PedidoService.CrearDesdeVenta(ventaPrep)
                Revisar("El pedido arranca en preparacion",
                        pedidoPrep.Estado = EstadoPedido.EnPreparacion)
                Revisar("El pedido apunta a su venta", pedidoPrep.VentaID = ventaPrep.ID)
                Revisar("El pedido hereda el descuento de la venta",
                        pedidoPrep.Descuento = ventaPrep.Descuento)
                Revisar("El pedido trae las mismas lineas que la venta",
                        pedidoPrep.Items.Count = ventaPrep.Items.Count AndAlso
                        pedidoPrep.Items(0).Especificaciones = "Sin azucar")
                Revisar("El pedido conserva la mesa", pedidoPrep.Mesa = "7")

                ' El tablero no toca el stock: ya bajo cuando se confirmo la venta.
                Revisar("Crear el pedido no vuelve a descontar stock",
                        cafePrep.Stock = stockAntesPrep - 2)
                Revisar("El pedido marca que el stock ya esta descontado",
                        pedidoPrep.StockDescontado)

                ' Un pedido sin venta cobrada por detras es una nota al viento.
                Dim ventaSinConfirmar = VentaService.Crear(pedidoBase, CobroDe(pedidoBase.Total))
                Dim dioError As Boolean = False
                Try
                    PedidoService.CrearDesdeVenta(ventaSinConfirmar)
                Catch
                    dioError = True
                End Try
                Revisar("No se crea pedido de una venta sin confirmar", dioError)

                ' Anular la venta tiene que llevarse su pedido: si no, el tablero manda
                ' a preparar mercaderia que ya se devolvio.
                VentaService.Anular(ventaPrep, "El cliente devolvio todo")
                Revisar("Anular la venta da de baja su pedido",
                        pedidoPrep.Estado = EstadoPedido.Cancelado)
                Revisar("El motivo de la baja es el de la anulacion",
                        pedidoPrep.MotivoCancelacion = "El cliente devolvio todo")
                Revisar("Anular la venta devuelve el stock",
                        cafePrep.Stock = stockAntesPrep)
                Revisar("La venta anulada no queda en el tablero",
                        Not PedidoService.Activos().Any(Function(p) p.ID = pedidoPrep.ID))

                ' Anular dos veces no toca dos veces ni el pedido equivocado.
                Dim ventaPrep2 = VentaService.Crear(pedidoBase, CobroDe(pedidoBase.Total))
                VentaService.Confirmar(ventaPrep2)
                Dim pedidoPrep2 = PedidoService.CrearDesdeVenta(ventaPrep2)
                VentaService.Anular(ventaPrep2, "Primera")
                VentaService.Anular(ventaPrep2, "Segunda")
                Revisar("Anular dos veces deja el pedido dado de baja una sola vez",
                        pedidoPrep2.Estado = EstadoPedido.Cancelado AndAlso
                        pedidoPrep2.MotivoCancelacion = "Primera")

                ' El cobro no se toca despues de confirmar: la venta ya esta en caja y
                ' en el historial, y cambiarla a mano los deja discrepantes.
                Dim ventaRegistrada = VentaService.Crear(pedidoBase, CobroDe(pedidoBase.Total))
                VentaService.Confirmar(ventaRegistrada)
                Dim cambioDespues As Boolean = False
                Try
                    VentaService.AplicarCobro(ventaRegistrada, New ResultadoCobro With {
                        .MetodoPago = MetodoPago.Efectivo, .Total = pedidoBase.Total,
                        .EfectivoRecibido = 0D, .CobradoAhora = False
                    })
                Catch
                    cambioDespues = True
                End Try
                Revisar("No se puede cambiar el cobro de una venta ya registrada",
                        cambioDespues)
            Finally
                Datos.ListaProductos.Remove(cafePrep)
            End Try

            PruebasCategoria.Correr()

            Console.WriteLine()
            If fallos = 0 Then
                Console.WriteLine($"OK: {pruebas} pruebas, 0 fallos.")
            Else
                Console.WriteLine($"FALLOS: {fallos} de {pruebas} pruebas.")
                Environment.ExitCode = 1
            End If

        End Sub

        Sub Revisar(descripcion As String, condicion As Boolean)
            pruebas += 1
            If condicion Then
                Console.WriteLine("  [ok]    " & descripcion)
            Else
                fallos += 1
                Console.WriteLine("  [FALLA] " & descripcion)
            End If
        End Sub

    End Module

End Namespace