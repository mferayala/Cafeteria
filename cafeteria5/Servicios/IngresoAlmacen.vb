Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Text.Json

Namespace CafeteriaOS

    ''' <summary>
    ''' Guarda los ingresos de mercaderia en un archivo JSON del perfil del usuario.
    ''' Vive fuera de la carpeta del ejecutable a proposito: si viviera en la carpeta
    ''' del binario, recompilar el proyecto dejaria el historial vacio.
    ''' </summary>
    Public Module IngresoAlmacen

        Public ReadOnly Property Ruta As String
            Get
                Dim carpeta = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CafeteriaOS")
                Return Path.Combine(carpeta, "ingresos_stock.json")
            End Get
        End Property

        Private ReadOnly Property Opciones As JsonSerializerOptions
            Get
                Return New JsonSerializerOptions With {
                    .WriteIndented = True,
                    .Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                }
            End Get
        End Property

        ''' <summary>
        ''' Escribe el historial. Si falla no rompe el ingreso: la mercaderia ya se sumo
        ''' al stock y perder el archivo es mucho menos grave que perder la operacion.
        ''' </summary>
        Public Function Guardar() As Boolean
            Try
                Dim carpeta = Path.GetDirectoryName(Ruta)
                If Not String.IsNullOrWhiteSpace(carpeta) Then
                    Directory.CreateDirectory(carpeta)
                End If

                ' Sin Select de LINQ porque en VB "Select" es palabra clave.
                Dim aGuardar As New List(Of IngresoJson)()
                For Each ingreso In Datos.ListaIngresos
                    aGuardar.Add(TraducirAJson(ingreso))
                Next
                File.WriteAllText(Ruta, JsonSerializer.Serialize(aGuardar, Opciones),
                                  New UTF8Encoding(False))
                Return True
            Catch
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Recupera el historial. Reconstruye las referencias a producto y proveedor por
        ''' codigo o nombre y descarta lo que ya no exista en el catalogo. No toca el
        ''' stock: el historial es pasado y el stock es el estado actual, asi que volver
        ''' a sumar las mismas unidades dejaria el deposito descuadrado.
        ''' </summary>
        Public Function Cargar() As Integer
            Datos.ListaIngresos.Clear()
            Dim maxId As Integer = 0
            Try
                If Not File.Exists(Ruta) Then Return 0
                Dim texto = File.ReadAllText(Ruta, Encoding.UTF8)
                If String.IsNullOrWhiteSpace(texto) Then Return 0

                Dim guardados = JsonSerializer.Deserialize(Of List(Of IngresoJson))(texto, Opciones)
                If guardados Is Nothing Then Return 0

                For Each g In guardados
                    Dim ingreso = TraducirDesdeJson(g)
                    If ingreso IsNot Nothing Then
                        Datos.ListaIngresos.Add(ingreso)
                        If ingreso.ID > maxId Then maxId = ingreso.ID
                    End If
                Next
            Catch
                ' Un archivo ilegible no debe impedir abrir la aplicacion.
                Datos.ListaIngresos.Clear()
                Return 0
            End Try

            ' El contador sigue desde lo mas alto guardado para no repetir numeros.
            Datos.AjustarContadorIngresos(maxId)
            Return Datos.ListaIngresos.Count
        End Function

        Private Function TraducirAJson(ingreso As IngresoStock) As IngresoJson
            Dim g As New IngresoJson With {
                .ID = ingreso.ID,
                .Fecha = ingreso.Fecha,
                .ProveedorCodigo = If(ingreso.Proveedor Is Nothing, "", ingreso.Proveedor.Codigo),
                .ProveedorNombre = If(ingreso.Proveedor Is Nothing, "", ingreso.Proveedor.Nombre),
                .Contacto = If(ingreso.Proveedor Is Nothing, "", ingreso.Proveedor.ContactoPrincipal),
                .Observaciones = ingreso.Observaciones,
                .RegistradoPor = ingreso.RegistradoPor
            }
            For Each d In ingreso.Detalles
                g.Lineas.Add(New LineaJson With {
                    .ProductoID = If(d.Producto Is Nothing, 0, d.Producto.ID),
                    .ProductoNombre = If(d.Producto Is Nothing, "", d.Producto.Nombre),
                    .Cantidad = d.Cantidad,
                    .Observacion = d.Observacion
                })
            Next
            Return g
        End Function

        Private Function TraducirDesdeJson(g As IngresoJson) As IngresoStock
            If g Is Nothing Then Return Nothing

            Dim ingreso As New IngresoStock With {
                .ID = g.ID,
                .Fecha = g.Fecha,
                .Observaciones = g.Observaciones,
                .RegistradoPor = g.RegistradoPor,
                .Proveedor = BuscarProveedor(g)
            }

            For Each l In g.Lineas
                Dim producto = BuscarProducto(l)
                ' Si el producto ya no esta en el catalogo la linea no sirve para
                ' nada: se pierde junto con el ingreso.
                If producto IsNot Nothing Then
                    ingreso.Detalles.Add(New DetalleIngreso With {
                        .Producto = producto,
                        .Cantidad = l.Cantidad,
                        .Observacion = l.Observacion
                    })
                End If
            Next

            If ingreso.Detalles.Count = 0 Then Return Nothing
            Return ingreso
        End Function

        Private Function BuscarProveedor(g As IngresoJson) As Proveedor
            If Not String.IsNullOrWhiteSpace(g.ProveedorCodigo) Then
                Dim porCodigo = Datos.ListaProveedores.
                    FirstOrDefault(Function(p) p.Codigo = g.ProveedorCodigo)
                If porCodigo IsNot Nothing Then Return porCodigo
            End If
            If Not String.IsNullOrWhiteSpace(g.ProveedorNombre) Then
                Return Datos.ListaProveedores.
                    FirstOrDefault(Function(p) p.Nombre = g.ProveedorNombre)
            End If
            Return Nothing
        End Function

        Private Function BuscarProducto(l As LineaJson) As Producto
            If l.ProductoID > 0 Then
                Dim porId = Datos.ListaProductos.FirstOrDefault(Function(p) p.ID = l.ProductoID)
                If porId IsNot Nothing Then Return porId
            End If
            If Not String.IsNullOrWhiteSpace(l.ProductoNombre) Then
                Return Datos.ListaProductos.
                    FirstOrDefault(Function(p) p.Nombre = l.ProductoNombre)
            End If
            Return Nothing
        End Function

    End Module

    ''' <summary>Forma en que un ingreso queda guardado en el archivo.</summary>
    Public Class IngresoJson
        Public Property ID As Integer
        Public Property Fecha As DateTime
        Public Property ProveedorCodigo As String
        Public Property ProveedorNombre As String
        Public Property Contacto As String
        Public Property Observaciones As String
        Public Property RegistradoPor As String
        Public Property Lineas As New List(Of LineaJson)()
    End Class

    ''' <summary>Forma en que una linea de producto queda guardada en el archivo.</summary>
    Public Class LineaJson
        Public Property ProductoID As Integer
        Public Property ProductoNombre As String
        Public Property Cantidad As Integer

        ''' <summary>
        ''' Como llego la mercaderia: por ejemplo "llego con las esquinas aplastadas,
        ''' el contenido esta bien". La mercaderia igual se acepta y suma al stock,
        ''' esto solo deja constancia.
        ''' </summary>
        Public Property Observacion As String
    End Class

End Namespace