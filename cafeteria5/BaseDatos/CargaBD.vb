Imports System.Collections.Generic
Imports System.Linq
Imports MySql.Data.MySqlClient

Namespace CafeteriaOS

    ''' <summary>
    ''' Carga inicial desde MySQL. Reemplaza a DatosIniciales.Cargar cuando hay base de
    ''' datos: llena las mismas listas de Datos que usaban las pantallas, asi que el
    ''' resto del programa no se entera de donde vienen los datos.
    '''
    ''' El orden importa: los productos necesitan las categorias, las ventas necesitan
    ''' los productos, los ingresos necesitan proveedores y productos, etc.
    ''' </summary>
    Public NotInheritable Class CargaBD

        Private Sub New()
        End Sub

        Public Shared Sub CargarTodo()
            Datos.UsuarioActual = Nothing
            Datos.ListaCategorias.Clear()
            Datos.ListaProductos.Clear()
            Datos.ListaProveedores.Clear()
            Datos.ListaIngresos.Clear()
            Datos.ListaMovimientos.Clear()
            Datos.ListaVentas.Clear()
            Datos.ListaPedidos.Clear()
            Datos.ListaClientes.Clear()

            Using cx = Db.Abrir()
                Datos.ConfigNegocio = ConfigRepo.Cargar(cx)
                CargarCategorias(cx)
                Dim productos = CargarProductos(cx)
                Dim proveedores = ProveedorRepo.CargarTodos(cx)
                Datos.ListaProveedores.AddRange(proveedores.Values)
                CargarIngresos(cx, productos, proveedores)
                CargarMovimientosStock(cx, productos)
                CargarVentas(cx, productos)
                CargarPedidos(cx, productos)
                CargarClientes(cx)
                Datos.Caja = CajaRepo.CargarSesionAbierta(cx)
                AjustarContadores(cx)
            End Using
        End Sub

        ' ------------------------------------------------------------------
        '  Catalogo
        ' ------------------------------------------------------------------

        Private Shared Sub CargarCategorias(cx As MySqlConnection)
            Dim porId As New Dictionary(Of Integer, Categoria)()

            Using cmd = Db.Comando(cx, Nothing, "SELECT id, nombre FROM categorias ORDER BY nombre")
                Using r = cmd.ExecuteReader()
                    While r.Read()
                        Dim c As New Categoria With {
                            .ID = Db.Entero(r, "id"),
                            .Nombre = Db.Texto(r, "nombre")
                        }
                        porId(c.ID) = c
                        Datos.ListaCategorias.Add(c)
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
        End Sub

        ''' <summary>
        ''' Carga todos los productos, tambien los inactivos: una venta vieja puede
        ''' apuntar a un producto que ya no se vende y tiene que poder encontrarlo.
        ''' </summary>
        Private Shared Function CargarProductos(cx As MySqlConnection) As Dictionary(Of Integer, Producto)
            Dim porId As New Dictionary(Of Integer, Producto)()

            Dim sql =
                "SELECT p.*, c.nombre AS categoria_nombre, s.nombre AS subcategoria_nombre " &
                "FROM productos p " &
                "LEFT JOIN categorias c ON c.id = p.categoria_id " &
                "LEFT JOIN subcategorias s ON s.id = p.subcategoria_id " &
                "ORDER BY p.id"

            Using cmd = Db.Comando(cx, Nothing, sql)
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
                            .Atajo = Db.Texto(r, "atajo")
                        }
                        porId(p.ID) = p
                        Datos.ListaProductos.Add(p)
                    End While
                End Using
            End Using

            ' Opciones (especificaciones): una fila por opcion, agrupadas por "grupo".
            Dim opciones As New Dictionary(Of String, List(Of String))()
            Dim grupos As New List(Of Tuple(Of Producto, String))()

            Using cmd = Db.Comando(cx, Nothing,
                "SELECT producto_id, grupo, opcion FROM producto_opciones ORDER BY producto_id, orden, id")
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

            Return porId
        End Function

        ' ------------------------------------------------------------------
        '  Stock
        ' ------------------------------------------------------------------

        Private Shared Sub CargarIngresos(cx As MySqlConnection,
                                   productos As Dictionary(Of Integer, Producto),
                                   proveedores As Dictionary(Of Integer, Proveedor))
            Dim porId As New Dictionary(Of Integer, IngresoStock)()

            Dim sql =
                "SELECT i.*, u.nombre_usuario FROM ingresos_stock i " &
                "LEFT JOIN usuarios u ON u.id = i.usuario_id ORDER BY i.id"

            Using cmd = Db.Comando(cx, Nothing, sql)
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
                        Datos.ListaIngresos.Add(ingreso)
                    End While
                End Using
            End Using

            Using cmd = Db.Comando(cx, Nothing, "SELECT * FROM detalle_ingresos ORDER BY id")
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
        End Sub

        Private Shared Sub CargarMovimientosStock(cx As MySqlConnection,
                                           productos As Dictionary(Of Integer, Producto))
            Dim sql =
                "SELECT m.*, u.nombre_usuario FROM movimientos_stock m " &
                "LEFT JOIN usuarios u ON u.id = m.usuario_id ORDER BY m.id"

            Using cmd = Db.Comando(cx, Nothing, sql)
                Using r = cmd.ExecuteReader()
                    While r.Read()
                        Dim producto As Producto = Nothing
                        If Not productos.TryGetValue(Db.Entero(r, "producto_id"), producto) Then Continue While

                        Dim usuario = Db.Texto(r, "nombre_usuario")
                        Datos.ListaMovimientos.Add(New MovimientoStock With {
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
        End Sub

        ' ------------------------------------------------------------------
        '  Ventas y pedidos
        ' ------------------------------------------------------------------

        Private Shared Sub CargarVentas(cx As MySqlConnection, productos As Dictionary(Of Integer, Producto))
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
                "ORDER BY v.id"

            Using cmd = Db.Comando(cx, Nothing, sql)
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
                        Datos.ListaVentas.Add(v)
                    End While
                End Using
            End Using

            Using cmd = Db.Comando(cx, Nothing, "SELECT * FROM detalle_ventas ORDER BY venta_id, id")
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
        End Sub

        Private Shared Sub CargarPedidos(cx As MySqlConnection, productos As Dictionary(Of Integer, Producto))
            Dim porId As New Dictionary(Of Integer, Pedido)()

            Dim sql =
                "SELECT p.*, mp.nombre AS metodo, u.nombre_usuario AS usuario " &
                "FROM pedidos p " &
                "LEFT JOIN metodos_pago mp ON mp.id = p.metodo_pago_id " &
                "LEFT JOIN empleados e ON e.id = p.empleado_id " &
                "LEFT JOIN usuarios u ON u.id = e.usuario_id " &
                "ORDER BY p.id"

            Using cmd = Db.Comando(cx, Nothing, sql)
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
                        Datos.ListaPedidos.Add(p)
                    End While
                End Using
            End Using

            Using cmd = Db.Comando(cx, Nothing, "SELECT * FROM detalle_pedidos ORDER BY pedido_id, id")
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
        End Sub

        ''' <summary>
        ''' Los clientes guardan solo nombre y telefono. La cantidad de compras, el total
        ''' gastado y el medio de pago habitual se calculan de sus ventas, asi nunca
        ''' quedan desfasados de lo que dice el historial.
        ''' </summary>
        Private Shared Sub CargarClientes(cx As MySqlConnection)
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
                        Datos.ListaClientes.Add(c)
                    End While
                End Using
            End Using
        End Sub

        ''' <summary>Los numeros nuevos siguen desde el mayor id ya guardado.</summary>
        Private Shared Sub AjustarContadores(cx As MySqlConnection)
            Datos.AjustarContadores(
                Maximo(cx, "ventas"),
                Maximo(cx, "pedidos"),
                Maximo(cx, "movimientos_stock"),
                Maximo(cx, "ingresos_stock"))
        End Sub

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

End Namespace
