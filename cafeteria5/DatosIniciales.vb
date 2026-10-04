Imports System.Collections.Generic
Imports System.Linq

Namespace CafeteriaOS

    ''' <summary>
    ''' Datos de arranque del sistema. La version sin base de datos carga todo en memoria.
    ''' </summary>
    Public Module DatosIniciales

        ''' <summary>
        ''' Carga el estado de arranque. La version sin base de datos arma todo en memoria;
        ''' es el unico lugar donde se inicializan las listas compartidas.
        ''' </summary>
        Public Sub Cargar()
            Datos.ListaProductos.Clear()
            Datos.ListaProductos.AddRange(Productos())
            Datos.ListaProveedores.Clear()
            Datos.ListaProveedores.AddRange(Proveedores())
            Datos.ListaClientes.Clear()
            Datos.ListaPedidos.Clear()
            ' El historial de ingresos va al final: para reconstruirlo hacen falta los
            ' proveedores y los productos ya cargados.
            IngresoAlmacen.Cargar()
            Datos.UsuarioActual = Nothing
            Datos.Caja = New SesionCaja()
        End Sub

        Private ReadOnly Property LecheOpciones() As String()
            Get
                Return New String() {"Entera", "Descremada", "Vegetal"}
            End Get
        End Property

        Private ReadOnly Property AzucarOpciones() As String()
            Get
                Return New String() {"Sin azucar", "Normal", "Edulcorante"}
            End Get
        End Property

        Private ReadOnly Property InfusionOpciones() As String()
            Get
                Return New String() {"Te", "Mate cocido"}
            End Get
        End Property

        Private ReadOnly Property TemperaturaOpciones() As String()
            Get
                Return New String() {"Fria", "Caliente"}
            End Get
        End Property

        Private ReadOnly Property FrutaOpciones() As String()
            Get
                Return New String() {"Frutilla", "Banana", "Durazno", "Anana", "Melon", "Mango", "Guayaba", "Naranja"}
            End Get
        End Property

        Private ReadOnly Property SaborOpciones() As String()
            Get
                Return New String() {
                    "Super frutilla", "Banana c/leche", "Durazno c/leche", "Naranja-mango",
                    "Mango-durazno", "Anana-naranja", "Frutos rojos", "Frutos del bosque",
                    "Arandanos", "Kiwi", "Frutilla-anana", "Frambuesa", "Mora",
                    "Frambuesa-frutilla", "Maracuya"}
            End Get
        End Property

        Private Function Grupo(etiqueta As String, opciones As String()) As EspecificacionGrupo
            Return New EspecificacionGrupo With {.Etiqueta = etiqueta, .Opciones = opciones}
        End Function

        Private Function NuevoProducto(id As Integer,
                           nombre As String,
                           precio As Decimal,
                           stock As Integer,
                           categoria As String,
                           descripcion As String,
                           Optional atajo As String = "",
                           Optional specs As List(Of EspecificacionGrupo) = Nothing) As Producto

            Dim prod As New Producto With {
                .ID = id,
                .Nombre = nombre,
                .Precio = precio,
                .Stock = stock,
                .Categoria = categoria,
                .Descripcion = descripcion,
                .Atajo = atajo,
                .CodigoBarra = "",
                .Marca = "",
                .UnidadMedida = "un",
                .PrecioCosto = 0D
            }
            If specs IsNot Nothing Then prod.Specs = specs
            Return prod
        End Function

        ''' <summary>
        ''' Catálogo de ejemplo (demo genérico). Los atajos son unicos en toda la lista para que no haya dos
        ''' productos compitiendo por la misma tecla.
        ''' </summary>
        Public Function Productos() As List(Of Producto)
            Return New List(Of Producto) From {
                NuevoProducto(1, "Cafe negro", 3000D, 45, "Cafes", "Cafe solo", "Ctrl+1"),
                NuevoProducto(2, "Cafe con leche", 3500D, 40, "Cafes", "Cafe con leche", "Ctrl+2",
                  New List(Of EspecificacionGrupo) From {Grupo("Leche", LecheOpciones), Grupo("Azucar", AzucarOpciones)}),
                NuevoProducto(3, "Te/mate cocido solo", 3000D, 60, "Cafes", "Infusion sola", "Ctrl+3",
                  New List(Of EspecificacionGrupo) From {Grupo("Infusion", InfusionOpciones)}),
                NuevoProducto(4, "Te/mate cocido con leche", 3500D, 55, "Cafes", "Infusion con leche", "Ctrl+4"),
                NuevoProducto(5, "Capuccino", 4000D, 35, "Cafes", "Con espuma de leche", "Ctrl+5"),
                NuevoProducto(6, "Submarino", 4500D, 25, "Cafes", "Leche caliente con chocolate", "Ctrl+6"),
                NuevoProducto(7, "Chocolatada fria/caliente", 4000D, 30, "Cafes", "Chocolatada a eleccion", "Ctrl+7",
                  New List(Of EspecificacionGrupo) From {Grupo("Temperatura", TemperaturaOpciones)}),
                NuevoProducto(8, "Frapuccino", 4000D, 28, "Cafes", "Cafe frio batido", "Ctrl+8"),
                NuevoProducto(9, "Cafe c/leche + 2 medialunas", 5000D, 25, "Promos", "Promo desayuno"),
                NuevoProducto(10, "Cafe c/leche + chipas", 5000D, 25, "Promos", "Promo con chipas"),
                NuevoProducto(11, "Cafe c/leche + tostado JYQ", 6000D, 20, "Promos", "Promo con tostado"),
                NuevoProducto(12, "Cafe c/leche + 2 medialunas + jugo", 6000D, 20, "Promos", "Promo completa"),
                NuevoProducto(13, "Exprimido chico", 0D, 20, "Promos", "Precio pendiente de cargar"),
                NuevoProducto(14, "Cocido quemado + mbeju", 6000D, 20, "Promos", "Promo regional"),
                NuevoProducto(15, "Frapuccino + 2 medialunas", 5500D, 20, "Promos", "Promo fria"),
                NuevoProducto(16, "Chipa", 100D, 100, "Panificados", "Chipa por unidad", "Ctrl+9"),
                NuevoProducto(17, "Chipa 4 unidades", 3500D, 50, "Panificados", "Combo 4 chipas"),
                NuevoProducto(18, "Chipitas 100g", 2000D, 40, "Panificados", "Chipitas por 100g"),
                NuevoProducto(19, "Chipitas 250g", 4500D, 30, "Panificados", "Chipitas por 250g"),
                NuevoProducto(20, "Mbeju", 3500D, 20, "Panificados", "Mbeju", "Ctrl+0"),
                NuevoProducto(21, "Medialuna", 1000D, 40, "Panificados", "Medialuna", "Ctrl+Q"),
                NuevoProducto(22, "Medialuna c/JYQ", 1500D, 30, "Panificados", "Medialuna con jamon y queso"),
                NuevoProducto(23, "Alfajor de maicena", 1000D, 30, "Panificados", "Alfajor de maicena"),
                NuevoProducto(24, "Sandwich de miga JYQ", 3000D, 25, "Panificados", "Sandwich de miga"),
                NuevoProducto(25, "Tostado de JYQ", 3500D, 20, "Panificados", "Tostado de jamon y queso", "Ctrl+W"),
                NuevoProducto(26, "Hamburguesa simple", 4000D, 20, "Hamburguesas", "Hamburguesa simple"),
                NuevoProducto(27, "Hamburguesa doble", 5000D, 20, "Hamburguesas", "Hamburguesa doble"),
                NuevoProducto(28, "Combo hamburguesa simple", 9000D, 15, "Hamburguesas", "Hamburguesa simple con acompanamiento"),
                NuevoProducto(29, "Porcion de papas", 3500D, 25, "Hamburguesas", "Papas fritas"),
                NuevoProducto(30, "Licuado comun local", 3500D, 30, "Licuados", "Licuado para consumir en local", "",
                  New List(Of EspecificacionGrupo) From {Grupo("Fruta", FrutaOpciones)}),
                NuevoProducto(31, "Licuado comun para llevar", 3700D, 30, "Licuados", "Licuado para llevar", "",
                  New List(Of EspecificacionGrupo) From {Grupo("Fruta", FrutaOpciones)}),
                NuevoProducto(32, "Licuado comun + tostado local", 6500D, 20, "Licuados", "Licuado comun con tostado"),
                NuevoProducto(33, "Licuado comun + tostado llevar", 6700D, 20, "Licuados", "Licuado comun con tostado para llevar"),
                NuevoProducto(34, "Licuado especial local", 4000D, 25, "Licuados", "Licuado especial", "",
                  New List(Of EspecificacionGrupo) From {Grupo("Sabor", SaborOpciones)}),
                NuevoProducto(35, "Licuado especial para llevar", 4200D, 25, "Licuados", "Licuado especial para llevar"),
                NuevoProducto(36, "Licuado especial + tostado local", 7000D, 20, "Licuados", "Licuado especial con tostado"),
                NuevoProducto(37, "Licuado especial + tostado llevar", 7200D, 20, "Licuados", "Licuado especial con tostado para llevar"),
                NuevoProducto(38, "Limonada vaso local", 2000D, 30, "Limonadas", "Limón, menta y jengibre", "",
                  New List(Of EspecificacionGrupo) From {Grupo("Temperatura", TemperaturaOpciones)}),
                NuevoProducto(39, "Limonada vaso para llevar", 2200D, 30, "Limonadas", "Limonada para llevar"),
                NuevoProducto(40, "Limonada jarra", 600D, 15, "Limonadas", "Jarra de limonada")
            }
        End Function

        Public Function Usuarios() As List(Of Usuario)
            Return New List(Of Usuario) From {
                New Usuario With {.Nombre = "Administrador", .Email = "admin@cafe.com", .Telefono = "12345678", .Contrasena = "123456", .Rol = RolUsuario.Administrador},
                New Usuario With {.Nombre = "Cajera", .Email = "cajera@cafe.com", .Telefono = "87654321", .Contrasena = "caja123", .Rol = RolUsuario.Cajero},
                New Usuario With {.Nombre = "Cocinera", .Email = "cocina@cafe.com", .Telefono = "11111111", .Contrasena = "cocina123", .Rol = RolUsuario.Cocina}
            }
        End Function

        ''' <summary>
        ''' Texto de ayuda del login. Se arma desde <see cref="Usuarios"/> para que las
        ''' claves mostradas nunca puedan quedar desfasadas respecto de los datos reales.
        ''' </summary>
        Public Function PistasDeLogin() As String
            Dim lineas = New List(Of String) From {"Cuentas de prueba"}
            For Each usuario In Usuarios()
                lineas.Add(usuario.Identificador & " / " & usuario.Contrasena)
            Next
            Return String.Join(Environment.NewLine, lineas)
        End Function

        Public Function Proveedores() As List(Of Proveedor)
            Return New List(Of Proveedor) From {
                New Proveedor With {
                    .Codigo = "PRV001", .Nombre = "Distribuidora Argentina",
                    .CUIT = "30-12345678-9",
                    .TelefonoLaboral = "4444-3333", .Celular = "3764-111111",
                    .Domicilio = "Cordoba 234", .CodigoPostal = "1824",
                    .Pais = "Argentina", .Provincia = "Buenos Aires", .Localidad = "Lanus Oeste",
                    .Actividad = "Distribuidora", .Categoria = "Mayorista",
                    .TipoIVA = "Resp. Inscripto", .ContactoPrincipal = "Perez",
                    .Email = "ventas@distribuidora.com", .FormaPagoHabitual = "Transferencia",
                    .TotalCompras = 250000D, .CantidadOrdenes = 8,
                    .UltimaCompra = Today(-4), .SaldoPendiente = 15000D,
                    .FechaUltimaOperacion = Today(-4), .CreadoPor = "Sistema", .ModificadoPor = "Sistema",
                    .Contactos = New List(Of ContactoProveedor) From {
                        New ContactoProveedor With {.Nombre = "Perez", .Cargo = "Ventas", .Email = "ventas@distribuidora.com", .Telefono = "4444-3333"}},
                    .Telefonos = New List(Of TelefonoProveedor) From {
                        New TelefonoProveedor With {.Tipo = "Laboral", .Numero = "4444-3333"}},
                    .Movimientos = New List(Of MovimientoProveedor) From {
                        New MovimientoProveedor With {.Tipo = "Factura", .Descripcion = "Compra de cafe e insumos",
                                                       .Monto = 15000D, .Vencimiento = Today(-2), .Pagado = False}}
                },
                New Proveedor With {
                    .Codigo = "PRV002", .Nombre = "Panificados Mitre",
                    .CUIT = "27-87654321-5",
                    .TelefonoLaboral = "3764-222222", .Celular = "3764-333333",
                    .Domicilio = "Mitre 890", .Pais = "Argentina",
                    .Provincia = "Misiones", .Localidad = "Posadas",
                    .Actividad = "Panaderia", .Categoria = "Panificados",
                    .ContactoPrincipal = "Laura Benitez",
                    .Email = "pedidos@panificadosmitre.com", .FormaPagoHabitual = "Efectivo",
                    .TotalCompras = 120000D, .CantidadOrdenes = 14,
                    .UltimaCompra = Today(-1), .SaldoPendiente = 0D,
                    .CreadoPor = "Sistema", .ModificadoPor = "Sistema"
                },
                New Proveedor With {
                    .Codigo = "PRV003", .Nombre = "Bebidas del Norte",
                    .CUIT = "20-11223344-6",
                    .TelefonoLaboral = "3764-555555",
                    .Domicilio = "Ruta 12 km 8", .Pais = "Argentina",
                    .Provincia = "Misiones", .Localidad = "Garupa",
                    .Actividad = "Bebidas", .Categoria = "Bebidas",
                    .ContactoPrincipal = "Carlos Diaz",
                    .Email = "admin@bebidasnorte.com", .FormaPagoHabitual = "Cuenta corriente",
                    .TotalCompras = 98000D, .CantidadOrdenes = 5,
                    .UltimaCompra = Today(-12), .SaldoPendiente = 8500D,
                    .CreadoPor = "Sistema", .ModificadoPor = "Sistema"
                }
            }
        End Function

        Private Function Today(offset As Integer) As DateTime
            Return DateTime.Today.AddDays(offset)
        End Function

        ''' <summary>Categorias del catalogo, en el orden en que aparecen, siempre con "Todos" al principio.</summary>
        Public Function CategoriasDeProductos() As List(Of String)
            Dim categorias As New List(Of String) From {"Todos"}
            For Each p In Datos.ListaProductos
                If Not categorias.Contains(p.Categoria) Then categorias.Add(p.Categoria)
            Next
            Return categorias
        End Function

    End Module

End Namespace