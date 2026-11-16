Imports System.Collections.Generic
Imports System.Linq
Imports MySql.Data.MySqlClient

Namespace CafeteriaOS

    ''' <summary>
    ''' Todo lo que se lee de MySQL hacia las listas de Datos que usan las pantallas.
    '''
    ''' CargarTodo: al arrancar, lee la base entera.
    ''' Sincronizar: despues, cada pocos segundos, trae solo lo que cambio desde la
    ''' ultima vez (lo que hicieron las otras PCs) y lo mezcla con lo que hay en memoria.
    ''' Sin esto, la PC de cocina no veia los pedidos que cargaba la caja hasta
    ''' reiniciar el programa, y cada caja vendia con el stock que habia al abrir.
    '''
    ''' Las dos usan las mismas funciones Leer..., con o sin filtro de fecha, para que una
    ''' fila se convierta en objeto siempre del mismo modo.
    ''' </summary>
    Public NotInheritable Class CargaBD

        Private Sub New()
        End Sub

        ''' <summary>
        ''' Una transaccion que ya escribio su fila pero todavia no hizo Commit no se ve
        ''' desde otra conexion: por eso cada sincronizacion vuelve a mirar unos segundos
        ''' antes de la anterior. Leer de mas no hace dano (se mezcla por id).
        ''' </summary>
        Private Const MargenSegundos As Integer = 10

        ''' <summary>Hora del servidor MySQL en la ultima lectura (no la de esta PC).</summary>
        Private Shared ultimaLectura As DateTime = DateTime.MinValue

        ' ==================================================================
        '  Carga completa
        ' ==================================================================

        Public Shared Sub CargarTodo()
            Dim lote = Leer(Nothing,
                            New Dictionary(Of Integer, Producto)(),
                            New Dictionary(Of Integer, Proveedor)())

            Datos.UsuarioActual = Nothing
            Datos.ConfigNegocio = lote.Config
            Reemplazar(Datos.ListaCategorias, lote.Categorias)
            Reemplazar(Datos.ListaProductos, lote.Productos)
            Reemplazar(Datos.ListaProveedores, lote.Proveedores)
            Reemplazar(Datos.ListaIngresos, lote.Ingresos)
            Reemplazar(Datos.ListaMovimientos, lote.Movimientos)
            Reemplazar(Datos.ListaVentas, lote.Ventas)
            Reemplazar(Datos.ListaPedidos, lote.Pedidos)
            Reemplazar(Datos.ListaClientes, lote.Clientes)
            Datos.Caja = lote.Caja
            Datos.AjustarContadores(lote.MaxVenta, lote.MaxPedido, lote.MaxMovimiento, lote.MaxIngreso)
            ultimaLectura = lote.HoraServidor
        End Sub

        ' ==================================================================
        '  Sincronizacion
        ' ==================================================================

        ''' <summary>
        ''' Lee de la base lo que cambio desde la ultima vez. Se puede llamar desde un
        ''' hilo de fondo: no toca las listas de Datos, solo lee. Los productos y
        ''' proveedores que recibe son una copia de las listas, tomada en el hilo de la
        ''' pantalla, y sirven para enganchar las ventas nuevas con los objetos que ya
        ''' existen en memoria.
        ''' </summary>
        Public Shared Function LeerCambios(productosEnMemoria As Dictionary(Of Integer, Producto),
                                           proveedoresEnMemoria As Dictionary(Of Integer, Proveedor)) As LoteDeDatos
            Return Leer(ultimaLectura.AddSeconds(-MargenSegundos), productosEnMemoria, proveedoresEnMemoria)
        End Function

        ''' <summary>
        ''' Mezcla lo leido con lo que hay en memoria. Va en el hilo de la pantalla.
        ''' Devuelve True si algo cambio, para refrescar la pantalla solo en ese caso.
        ''' </summary>
        Public Shared Function AplicarCambios(lote As LoteDeDatos) As Boolean
            If lote Is Nothing Then Return False
            Dim cambio = False

            cambio = Copiador.CopiarSobre(Datos.ConfigNegocio, lote.Config) Or cambio

            If FirmaCategorias(Datos.ListaCategorias) <> FirmaCategorias(lote.Categorias) Then
                Reemplazar(Datos.ListaCategorias, lote.Categorias)
                cambio = True
            End If

            cambio = Mezclar(Datos.ListaProductos, lote.Productos, Function(p) p.ID) Or cambio

            ' Los proveedores llegan completos: el que ya no esta, se elimino en otra PC.
            cambio = Mezclar(Datos.ListaProveedores, lote.Proveedores, Function(p) p.ID) Or cambio
            Dim vigentes = New HashSet(Of Integer)(lote.Proveedores.Select(Function(p) p.ID))
            If Datos.ListaProveedores.RemoveAll(Function(p) p.ID > 0 AndAlso Not vigentes.Contains(p.ID)) > 0 Then
                cambio = True
            End If

            cambio = Agregar(Datos.ListaIngresos, lote.Ingresos, Function(i) i.ID) Or cambio
            cambio = Agregar(Datos.ListaMovimientos, lote.Movimientos, Function(m) m.ID) Or cambio
            cambio = Mezclar(Datos.ListaVentas, lote.Ventas, Function(v) v.ID) Or cambio
            cambio = Mezclar(Datos.ListaPedidos, lote.Pedidos, Function(p) p.ID) Or cambio

            If FirmaClientes(Datos.ListaClientes) <> FirmaClientes(lote.Clientes) Then
                Reemplazar(Datos.ListaClientes, lote.Clientes)
                cambio = True
            End If

            If FirmaCaja(Datos.Caja) <> FirmaCaja(lote.Caja) Then
                Datos.Caja = lote.Caja
                cambio = True
            End If

            ultimaLectura = lote.HoraServidor
            Return cambio
        End Function

        Private Shared Sub Reemplazar(Of T)(destino As List(Of T), origen As List(Of T))
            destino.Clear()
            destino.AddRange(origen)
        End Sub

        ''' <summary>Actualiza los que ya estan (sin cambiar el objeto) y agrega los nuevos.</summary>
        Private Shared Function Mezclar(Of T As Class)(destino As List(Of T),
                                                       leidos As List(Of T),
                                                       idDe As Func(Of T, Integer)) As Boolean
            Dim cambio = False
            Dim porId As New Dictionary(Of Integer, T)()
            For Each existente In destino
                porId(idDe(existente)) = existente
            Next
            For Each nuevo In leidos
                Dim actual As T = Nothing
                If porId.TryGetValue(idDe(nuevo), actual) Then
                    If Copiador.CopiarSobre(actual, nuevo) Then cambio = True
                Else
                    destino.Add(nuevo)
                    porId(idDe(nuevo)) = nuevo
                    cambio = True
                End If
            Next
            Return cambio
        End Function

        ''' <summary>Para lo que no cambia despues de creado (movimientos, ingresos).</summary>
        Private Shared Function Agregar(Of T As Class)(destino As List(Of T),
                                                       leidos As List(Of T),
                                                       idDe As Func(Of T, Integer)) As Boolean
            Dim conocidos = New HashSet(Of Integer)(destino.Select(idDe))
            Dim cambio = False
            For Each nuevo In leidos
                If conocidos.Add(idDe(nuevo)) Then
                    destino.Add(nuevo)
                    cambio = True
                End If
            Next
            Return cambio
        End Function

        Private Shared Function FirmaCategorias(lista As List(Of Categoria)) As String
            Return String.Join("|", lista.Select(Function(c) c.ID & ":" & c.Nombre & ":" & String.Join(",", c.Subcategorias)))
        End Function

        Private Shared Function FirmaClientes(lista As List(Of ClienteRegistrado)) As String
            Return String.Join("|", lista.Select(Function(c) c.ID & ":" & c.Nombre & ":" & c.Telefono & ":" &
                                                             c.CantidadPedidos & ":" & c.TotalGastado))
        End Function

        Private Shared Function FirmaCaja(c As SesionCaja) As String
            If c Is Nothing Then Return ""
            Return c.ID & ":" & c.Abierta & ":" & c.MontoInicial & ":" &
                   String.Join(",", c.Movimientos.Select(Function(m) m.Tipo & m.Monto & "/" & m.VentaID))
        End Function

        ' ==================================================================
        '  Lectura
        ' ==================================================================

        ''' <summary>
        ''' Lee la base. Con desde = Nothing trae todo; con fecha, solo las filas de
        ''' productos, ventas, pedidos, movimientos e ingresos modificadas desde entonces
        ''' (columna actualizado_en, que pone el servidor y no el reloj de cada PC). Lo
        ''' chico (categorias, proveedores, clientes, caja, configuracion) se lee entero.
        ''' </summary>
        Private Shared Function Leer(desde As DateTime?,
                                     productosEnMemoria As Dictionary(Of Integer, Producto),
                                     proveedoresEnMemoria As Dictionary(Of Integer, Proveedor)) As LoteDeDatos
            Dim lote As New LoteDeDatos()

            Using cx = Db.Abrir()
                ' La hora se toma ANTES de leer: lo que cambie durante la lectura entra
                ' en la proxima vuelta.
                lote.HoraServidor = Convert.ToDateTime(Db.Escalar(cx, Nothing, "SELECT NOW()"))

                lote.Config = ConfigRepo.Cargar(cx)
                lote.Categorias = LeerCategorias(cx)
                lote.Productos = LeerProductos(cx, desde)

                ' Para enganchar ventas, pedidos y movimientos se usa el objeto que ya
                ' esta en memoria (asi la mezcla lo actualiza en el lugar) y, si es un
                ' producto nuevo, el recien leido.
                Dim productos As New Dictionary(Of Integer, Producto)(productosEnMemoria)
                For Each p In lote.Productos
                    If Not productos.ContainsKey(p.ID) Then productos(p.ID) = p
                Next

                Dim proveedoresLeidos = ProveedorRepo.CargarTodos(cx)
                lote.Proveedores = proveedoresLeidos.Values.ToList()
                Dim proveedores As New Dictionary(Of Integer, Proveedor)(proveedoresEnMemoria)
                For Each par In proveedoresLeidos
                    If Not proveedores.ContainsKey(par.Key) Then proveedores(par.Key) = par.Value
                Next

                lote.Ingresos = LeerIngresos(cx, productos, proveedores, desde)
                lote.Movimientos = LeerMovimientosStock(cx, productos, desde)
                lote.Ventas = LeerVentas(cx, productos, desde)
                lote.Pedidos = LeerPedidos(cx, productos, desde)
                lote.Clientes = LeerClientes(cx)
                lote.Caja = CajaRepo.CargarSesionAbierta(cx)

                lote.MaxVenta = Maximo(cx, "ventas")
                lote.MaxPedido = Maximo(cx, "pedidos")
                lote.MaxMovimiento = Maximo(cx, "movimientos_stock")
                lote.MaxIngreso = Maximo(cx, "ingresos_stock")
            End Using

            Return lote
        End Function

        ''' <summary>"WHERE tabla.actualizado_en >= @desde", o nada si se lee todo.</summary>
        Private Shared Function FiltroDesde(tabla As String, desde As DateTime?) As String
            If Not desde.HasValue Then Return ""
            Return " WHERE " & tabla & ".actualizado_en >= @desde "
        End Function

        ''' <summary>Ids separados por coma para un IN (...). Son enteros: no hay inyeccion posible.</summary>
        Private Shared Function ListaIds(ids As IEnumerable(Of Integer)) As String
            Return String.Join(",", ids.Select(Function(i) i.ToString()))
        End Function

        ' ------------------------------------------------------------------
        '  Catalogo
        ' ------------------------------------------------------------------

        Private Shared Function LeerCategorias(cx As MySqlConnection) As List(Of Categoria)
            Dim lista As New List(Of Categoria)()
            Dim porId As New Dictionary(Of Integer, Categoria)()

            Using cmd = Db.Comando(cx, Nothing, "SELECT id, nombre FROM categorias ORDER BY nombre")
                Using r = cmd.ExecuteReader()
                    While r.Read()
                        Dim c As New Categoria With {
                            .ID = Db.Entero(r, "id"),
                            .Nombre = Db.Texto(r, "nombre")
                        }
                        porId(c.ID) = c
                        lista.Add(c)
                    End While
                End Using
            End Using

            Using cmd = Db.Comando(cx, Nothing,
                "SELECT categoria_id, nombre FROM subcategorias ORDER BY id")
                Using r = cmd.ExecuteReader()
                    While r.Read()
                        Dim c As Categoria = Nothing
                        If porId.TryGetValue(Db.Entero(r, "categoria_id"), c) Then
                            c.Subcategorias.Add(Db.Texto(r, "nombre"))
                        End If
                    End While
                End Using
            End Using
            Return lista
        End Function

        ''' <summary>
        ''' Productos, tambien los inactivos: una venta vieja puede apuntar a un
        ''' producto que ya no se vende y tiene que poder encontrarlo.
        ''' </summary>
        Private Shared Function LeerProductos(cx As MySqlConnection, desde As DateTime?) As List(Of Producto)
            Dim lista As New List(Of Producto)()
            Dim porId As New Dictionary(Of Integer, Producto)()

            Dim sql =
                "SELECT p.*, c.nombre AS categoria_nombre, s.nombre AS subcategoria_nombre " &
                "FROM productos p " &
                "LEFT JOIN categorias c ON c.id = p.categoria_id " &
                "LEFT JOIN subcategorias s ON s.id = p.subcategoria_id " &
                FiltroDesde("p", desde) &
                "ORDER BY p.id"

            Using cmd = Db.Comando(cx, Nothing, sql, "@desde", desde)
                Using r = cmd.ExecuteReader()
                    While r.Read()
                        Dim p As New Producto With {
                            .ID = Db.Entero(r, "id"),
                            .Nombre = Db.Texto(r, "nombre"),
                            .CodigoBarra = Db.Texto(r, "codigo"),
                            .Marca = Db.Texto(r, "marca"),
                            .Descripcion = Db.Texto(r, "descripcion"),
                            .UnidadMedida = Db.Texto(r, "unidad_medida"),
                            .PrecioCosto = Db.Dinero(r, "costo"),
                            .Precio = Db.Dinero(r, "precio_venta"),
                            .Stock = Db.Entero(r, "stock_actual"),
                            .StockMinimo = Db.Entero(r, "stock_minimo"),
                            .Categoria = Db.Texto(r, "categoria_nombre"),
                            .Subcategoria = Db.Texto(r, "subcategoria_nombre"),
                            .Atajo = Db.Texto(r, "atajo"),
                            .Imagen = Db.Bytes(r, "imagen"),
                            .Activo = Db.Booleano(r, "activo")
                        }
                        porId(p.ID) = p
                        lista.Add(p)
                    End While
                End Using
            End Using
            If lista.Count = 0 Then Return lista

            ' Opciones (especificaciones): una fila por opcion, agrupadas por "grupo".
            Dim opciones As New Dictionary(Of String, List(Of String))()
            Dim grupos As New List(Of Tuple(Of Producto, String))()

            Using cmd = Db.Comando(cx, Nothing,
                "SELECT producto_id, grupo, opcion FROM producto_opciones " &
                "WHERE producto_id IN (" & ListaIds(porId.Keys) & ") " &
                "ORDER BY producto_id, orden, id")
                Using r = cmd.ExecuteReader()
                    While r.Read()
                        Dim p As Producto = Nothing
                        If Not porId.TryGetValue(Db.Entero(r, "producto_id"), p) Then Continue While
                        Dim grupo = Db.Texto(r, "grupo")
                        Dim clave = p.ID.ToString() & "|" & grupo
                        If Not opciones.ContainsKey(clave) Then
                            opciones(clave) = New List(Of String)()
                            grupos.Add(Tuple.Create(p, grupo))
                        End If
                        opciones(clave).Add(Db.Texto(r, "opcion"))
                    End While
                End Using
            End Using

            For Each g In grupos
                g.Item1.Specs.Add(New EspecificacionGrupo With {
                    .Etiqueta = g.Item2,
                    .Opciones = opciones(g.Item1.ID.ToString() & "|" & g.Item2).ToArray()
                })
            Next

            Return lista
        End Function

        ' ------------------------------------------------------------------
        '  Stock
        ' ------------------------------------------------------------------

        Private Shared Function LeerIngresos(cx As MySqlConnection,
                                             productos As Dictionary(Of Integer, Producto),
                                             proveedores As Dictionary(Of Integer, Proveedor),
                                             desde As DateTime?) As List(Of IngresoStock)
            Dim lista As New List(Of IngresoStock)()
            Dim porId As New Dictionary(Of Integer, IngresoStock)()

            Dim sql =
                "SELECT i.*, u.nombre_usuario FROM ingresos_stock i " &
                "LEFT JOIN usuarios u ON u.id = i.usuario_id " &
                FiltroDesde("i", desde) & "ORDER BY i.id"

            Using cmd = Db.Comando(cx, Nothing, sql, "@desde", desde)
                Using r = cmd.ExecuteReader()
                    While r.Read()
                        Dim proveedor As Proveedor = Nothing
                        proveedores.TryGetValue(Db.Entero(r, "proveedor_id"), proveedor)

                        Dim ingreso As New IngresoStock With {
                            .ID = Db.Entero(r, "id"),
                            .Fecha = Db.Fecha(r, "fecha"),
                            .Proveedor = proveedor,
                            .Observaciones = Db.Texto(r, "observaciones"),
                            .RegistradoPor = Db.Texto(r, "nombre_usuario")
                        }
                        porId(ingreso.ID) = ingreso
                        lista.Add(ingreso)
                    End While
                End Using
            End Using
            If lista.Count = 0 Then Return lista

            Using cmd = Db.Comando(cx, Nothing,
                "SELECT * FROM detalle_ingresos WHERE ingreso_id IN (" & ListaIds(porId.Keys) & ") ORDER BY id")
                Using r = cmd.ExecuteReader()
                    While r.Read()
                        Dim ingreso As IngresoStock = Nothing
                        Dim producto As Producto = Nothing
                        If porId.TryGetValue(Db.Entero(r, "ingreso_id"), ingreso) AndAlso
                           productos.TryGetValue(Db.Entero(r, "producto_id"), producto) Then
                            ingreso.Detalles.Add(New DetalleIngreso With {
                                .Producto = producto,
                                .Cantidad = Db.Entero(r, "cantidad"),
                                .Observacion = Db.Texto(r, "observacion")
                            })
                        End If
                    End While
                End Using
            End Using
            Return lista
        End Function

        Private Shared Function LeerMovimientosStock(cx As MySqlConnection,
                                                     productos As Dictionary(Of Integer, Producto),
                                                     desde As DateTime?) As List(Of MovimientoStock)
            Dim lista As New List(Of MovimientoStock)()
            Dim sql =
                "SELECT m.*, u.nombre_usuario FROM movimientos_stock m " &
                "LEFT JOIN usuarios u ON u.id = m.usuario_id " &
                FiltroDesde("m", desde) & "ORDER BY m.id"

            Using cmd = Db.Comando(cx, Nothing, sql, "@desde", desde)
                Using r = cmd.ExecuteReader()
                    While r.Read()
                        Dim producto As Producto = Nothing
                        If Not productos.TryGetValue(Db.Entero(r, "producto_id"), producto) Then Continue While

                        Dim usuario = Db.Texto(r, "nombre_usuario")
                        lista.Add(New MovimientoStock With {
                            .ID = Db.Entero(r, "id"),
                            .Fecha = Db.Fecha(r, "creado_en"),
                            .Producto = producto,
                            .Cantidad = Db.Entero(r, "cantidad"),
                            .Tipo = Db.EnumDe(Of TipoMovimientoStock)(Db.Texto(r, "tipo")),
                            .Origen = Db.Texto(r, "referencia_tipo"),
                            .ReferenciaID = Db.Entero(r, "referencia_id"),
                            .Usuario = If(usuario.Length = 0, "Sistema", usuario),
                            .Observacion = Db.Texto(r, "motivo")
                        })
                    End While
                End Using
            End Using
            Return lista
        End Function

        ' ------------------------------------------------------------------
        '  Ventas y pedidos
        ' ------------------------------------------------------------------

        Private Shared Function LeerVentas(cx As MySqlConnection,
                                           productos As Dictionary(Of Integer, Producto),
                                           desde As DateTime?) As List(Of Venta)
            Dim lista As New List(Of Venta)()
            Dim porId As New Dictionary(Of Integer, Venta)()

            Dim sql =
                "SELECT v.*, mp.nombre AS metodo, " &
                "       u.nombre_usuario AS usuario, ua.nombre_usuario AS usuario_anulo " &
                "FROM ventas v " &
                "JOIN metodos_pago mp ON mp.id = v.metodo_pago_id " &
                "LEFT JOIN empleados e  ON e.id  = v.empleado_id " &
                "LEFT JOIN usuarios  u  ON u.id  = e.usuario_id " &
                "LEFT JOIN empleados ea ON ea.id = v.anulada_por " &
                "LEFT JOIN usuarios  ua ON ua.id = ea.usuario_id " &
                FiltroDesde("v", desde) &
                "ORDER BY v.id"

            Using cmd = Db.Comando(cx, Nothing, sql, "@desde", desde)
                Using r = cmd.ExecuteReader()
                    While r.Read()
                        Dim v As New Venta With {
                            .ID = Db.Entero(r, "id"),
                            .Numero = Db.Texto(r, "numero"),
                            .FechaHora = Db.Fecha(r, "fecha_venta"),
                            .ClienteNombre = Db.Texto(r, "cliente_nombre"),
                            .ClienteTelefono = Db.Texto(r, "cliente_telefono"),
                            .TipoServicio = Db.EnumDe(Of TipoServicio)(Db.Texto(r, "tipo_servicio")),
                            .Mesa = Db.Texto(r, "mesa"),
                            .Descuento = Db.Dinero(r, "descuento"),
                            .MetodoPago = Db.EnumDe(Of MetodoPago)(Db.Texto(r, "metodo")),
                            .EfectivoRecibido = Db.Dinero(r, "efectivo_recibido"),
                            .Pago = Db.EnumDe(Of EstadoPago)(Db.Texto(r, "estado_pago")),
                            .FueACuenta = Db.Booleano(r, "fue_a_cuenta"),
                            .MontoPagado = Db.Dinero(r, "monto_pagado"),
                            .Usuario = Db.Texto(r, "usuario"),
                            .Estado = Db.EnumDe(Of EstadoVenta)(Db.Texto(r, "estado")),
                            .StockDescontado = Db.Booleano(r, "stock_descontado"),
                            .AnuladaFecha = Db.FechaNula(r, "anulada_en"),
                            .AnuladaMotivo = Db.Texto(r, "motivo_anulacion"),
                            .AnuladaUsuario = Db.Texto(r, "usuario_anulo")
                        }
                        porId(v.ID) = v
                        lista.Add(v)
                    End While
                End Using
            End Using
            If lista.Count = 0 Then Return lista

            Using cmd = Db.Comando(cx, Nothing,
                "SELECT * FROM detalle_ventas WHERE venta_id IN (" & ListaIds(porId.Keys) & ") ORDER BY venta_id, id")
                Using r = cmd.ExecuteReader()
                    While r.Read()
                        Dim v As Venta = Nothing
                        If Not porId.TryGetValue(Db.Entero(r, "venta_id"), v) Then Continue While
                        Dim producto As Producto = Nothing
                        productos.TryGetValue(Db.Entero(r, "producto_id"), producto)
                        v.Items.Add(New DetalleVenta With {
                            .Producto = producto,
                            .ProductoID = Db.Entero(r, "producto_id"),
                            .ProductoNombre = Db.Texto(r, "producto_nombre"),
                            .Cantidad = Db.Entero(r, "cantidad"),
                            .PrecioUnitario = Db.Dinero(r, "precio_unitario"),
                            .Especificaciones = Db.Texto(r, "especificaciones")
                        })
                    End While
                End Using
            End Using
            Return lista
        End Function

        Private Shared Function LeerPedidos(cx As MySqlConnection,
                                            productos As Dictionary(Of Integer, Producto),
                                            desde As DateTime?) As List(Of Pedido)
            Dim lista As New List(Of Pedido)()
            Dim porId As New Dictionary(Of Integer, Pedido)()

            Dim sql =
                "SELECT p.*, mp.nombre AS metodo, u.nombre_usuario AS usuario " &
                "FROM pedidos p " &
                "LEFT JOIN metodos_pago mp ON mp.id = p.metodo_pago_id " &
                "LEFT JOIN empleados e ON e.id = p.empleado_id " &
                "LEFT JOIN usuarios u ON u.id = e.usuario_id " &
                FiltroDesde("p", desde) &
                "ORDER BY p.id"

            Using cmd = Db.Comando(cx, Nothing, sql, "@desde", desde)
                Using r = cmd.ExecuteReader()
                    While r.Read()
                        Dim p As New Pedido With {
                            .ID = Db.Entero(r, "id"),
                            .VentaID = Db.Entero(r, "venta_id"),
                            .NombreCliente = Db.Texto(r, "cliente_nombre"),
                            .TelefonoCliente = Db.Texto(r, "cliente_telefono"),
                            .Estado = Db.EnumDe(Of EstadoPedido)(Db.Texto(r, "estado")),
                            .TipoServicio = Db.EnumDe(Of TipoServicio)(Db.Texto(r, "tipo")),
                            .Mesa = Db.Texto(r, "mesa"),
                            .MetodoPago = Db.EnumDe(Of MetodoPago)(Db.Texto(r, "metodo")),
                            .FechaHora = Db.Fecha(r, "creado_en"),
                            .Descuento = Db.Dinero(r, "descuento"),
                            .UsuarioResponsable = Db.Texto(r, "usuario"),
                            .MotivoCancelacion = Db.Texto(r, "motivo_cancelacion"),
                            .StockDescontado = True
                        }
                        porId(p.ID) = p
                        lista.Add(p)
                    End While
                End Using
            End Using
            If lista.Count = 0 Then Return lista

            Using cmd = Db.Comando(cx, Nothing,
                "SELECT * FROM detalle_pedidos WHERE pedido_id IN (" & ListaIds(porId.Keys) & ") ORDER BY pedido_id, id")
                Using r = cmd.ExecuteReader()
                    While r.Read()
                        Dim p As Pedido = Nothing
                        Dim producto As Producto = Nothing
                        If porId.TryGetValue(Db.Entero(r, "pedido_id"), p) AndAlso
                           productos.TryGetValue(Db.Entero(r, "producto_id"), producto) Then
                            p.Items.Add(New DetallePedido With {
                                .Producto = producto,
                                .Cantidad = Db.Entero(r, "cantidad"),
                                .Especificaciones = Db.Texto(r, "observaciones")
                            })
                        End If
                    End While
                End Using
            End Using
            Return lista
        End Function

        ''' <summary>
        ''' Los clientes guardan solo nombre y telefono. La cantidad de compras, el total
        ''' gastado y el medio de pago habitual se calculan de sus ventas, asi nunca
        ''' quedan desfasados de lo que dice el historial.
        ''' </summary>
        Private Shared Function LeerClientes(cx As MySqlConnection) As List(Of ClienteRegistrado)
            Dim lista As New List(Of ClienteRegistrado)()
            Dim sql =
                "SELECT c.id, c.nombre, c.telefono, " &
                "       COUNT(v.id) AS cantidad, " &
                "       COALESCE(SUM(v.total), 0) AS total, " &
                "       MAX(v.fecha_venta) AS ultima, " &
                "       CAST(GROUP_CONCAT(v.id ORDER BY v.fecha_venta SEPARATOR ',') AS CHAR) AS historial, " &
                "       (SELECT mp.nombre FROM ventas v2 " &
                "          JOIN metodos_pago mp ON mp.id = v2.metodo_pago_id " &
                "         WHERE v2.cliente_id = c.id " &
                "         ORDER BY v2.fecha_venta DESC LIMIT 1) AS metodo " &
                "FROM clientes c " &
                "LEFT JOIN ventas v ON v.cliente_id = c.id " &
                "WHERE c.activo = 1 " &
                "GROUP BY c.id, c.nombre, c.telefono " &
                "ORDER BY c.nombre"

            Using cmd = Db.Comando(cx, Nothing, sql)
                Using r = cmd.ExecuteReader()
                    While r.Read()
                        Dim telefono = Db.Texto(r, "telefono")
                        Dim c As New ClienteRegistrado With {
                            .ID = Db.Entero(r, "id"),
                            .Nombre = Db.Texto(r, "nombre"),
                            .Telefono = If(telefono.Length = 0, ClienteRegistrado.SinTelefono, telefono),
                            .CantidadPedidos = Db.Entero(r, "cantidad"),
                            .TotalGastado = Db.Dinero(r, "total"),
                            .UltimoPedido = Db.Fecha(r, "ultima"),
                            .MetodoPagoHabitual = Db.EnumDe(Of MetodoPago)(Db.Texto(r, "metodo"))
                        }
                        For Each parte In Db.Texto(r, "historial").Split(","c)
                            Dim id As Integer
                            If Integer.TryParse(parte, id) Then c.HistorialPedidos.Add(id)
                        Next
                        lista.Add(c)
                    End While
                End Using
            End Using
            Return lista
        End Function

        ''' <summary>
        ''' Mayor id de una tabla. El nombre de la tabla va pegado al SQL porque los
        ''' parametros no sirven para nombres de tablas; solo se llama con nombres fijos
        ''' escritos aca arriba, nunca con texto que venga del usuario.
        ''' </summary>
        Private Shared Function Maximo(cx As MySqlConnection, tabla As String) As Integer
            Dim valor = Db.Escalar(cx, Nothing, "SELECT COALESCE(MAX(id), 0) FROM " & tabla)
            If valor Is Nothing Then Return 0
            Return Convert.ToInt32(valor)
        End Function

    End Class

    ''' <summary>Todo lo que trae una lectura de la base, antes de mezclarlo con la memoria.</summary>
    Public Class LoteDeDatos
        Public Property HoraServidor As DateTime
        Public Property Config As ConfiguracionNegocio
        Public Property Categorias As New List(Of Categoria)()
        Public Property Productos As New List(Of Producto)()
        Public Property Proveedores As New List(Of Proveedor)()
        Public Property Ingresos As New List(Of IngresoStock)()
        Public Property Movimientos As New List(Of MovimientoStock)()
        Public Property Ventas As New List(Of Venta)()
        Public Property Pedidos As New List(Of Pedido)()
        Public Property Clientes As New List(Of ClienteRegistrado)()
        Public Property Caja As SesionCaja
        Public Property MaxVenta As Integer
        Public Property MaxPedido As Integer
        Public Property MaxMovimiento As Integer
        Public Property MaxIngreso As Integer
    End Class

End Namespace
