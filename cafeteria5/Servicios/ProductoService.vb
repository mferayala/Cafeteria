Imports System.Collections.Generic
Imports System.Linq

Namespace CafeteriaOS

    ''' <summary>
    ''' Alta y edicion del catalogo de productos.
    '''
    ''' Las reglas viven aca y no en el formulario: asi el mismo control que valida
    ''' desde la pantalla sirve para una importacion o una carga masiva, y no queda
    ''' ninguna forma de meter un producto invalido por la puerta de atrás.
    ''' </summary>
    Public Module ProductoService

        ''' <summary>Resultado de intentar guardar un producto.</summary>
        Public Class ResultadoProducto
            Public Property Ok As Boolean
            Public Property Mensaje As String
            Public Property Producto As Producto
        End Class

        ''' <summary>
        ''' Guarda un producto nuevo o los cambios de uno existente, segun tenga ID.
        ''' Antes de tocar el catalogo revisa todo; si algo esta mal, no se guarda nada.
        '''
        ''' El objeto recibido se toma como el estado completo del producto: lo que no
        ''' venga Loaded queda vacio. El formulario de edicion siempre arma el
        ''' producto entero desde la pantalla, asi que no hay riesgo ahi, pero quien
        ''' llame a esto desde otro lado tiene que saberlo y no pasar un producto a
        ''' medio Armar.
        '''
        ''' El stock y el ID quedan exceptuados: el stock no se edita desde el alta de
        ''' un producto sino desde las operaciones de stock, y cambiarlo por aqui
        ''' dejaria el deposito sin justificacion.
        ''' </summary>
        Public Function Guardar(p As Producto) As ResultadoProducto
            Dim resultado As New ResultadoProducto()

            If p Is Nothing Then
                resultado.Mensaje = "No hay ningun producto para guardar."
                Return resultado
            End If

            Dim nombre = If(p.Nombre, "").Trim()
            If nombre.Length = 0 Then
                resultado.Mensaje = "El nombre del producto no puede quedar vacio."
                Return resultado
            End If

            If p.Precio < 0D Then
                resultado.Mensaje = "El precio de venta no puede ser negativo."
                Return resultado
            End If

            If p.PrecioCosto < 0D Then
                resultado.Mensaje = "El precio de costo no puede ser negativo."
                Return resultado
            End If

            If p.Stock < 0 Then
                resultado.Mensaje = "El stock no puede ser negativo."
                Return resultado
            End If

            If p.StockMinimo < 0 Then
                resultado.Mensaje = "El minimo de stock no puede ser negativo."
                Return resultado
            End If

            Dim codigo = Si(p.CodigoBarra, "").Trim()
            If codigo.Length > 0 Then
                Dim repetido = BuscarPorCodigoBarra(codigo, p.ID)
                If repetido IsNot Nothing Then
                    resultado.Mensaje = "El codigo de barras " & codigo &
                                        " ya lo usa " & repetido.NombreCompleto() & "."
                    Return resultado
                End If
            End If

            ' Normaliza los textos opcionales: si vienen vacios o con espacios al
            ' borde, se guardan limpios para que la busqueda y los filtros no los
            ' confundan con un valor distinto.
            p.Nombre = nombre
            p.CodigoBarra = codigo
            p.Marca = Limpiar(p.Marca)
            p.Descripcion = Limpiar(p.Descripcion)
            p.Categoria = Limpiar(p.Categoria)
            p.Subcategoria = Limpiar(p.Subcategoria)
            p.UnidadMedida = Si(Limpiar(p.UnidadMedida), "un").ToLowerInvariant()
            p.Atajo = Si(Limpiar(p.Atajo), "")

            If p.ID = 0 Then
                p.ID = Datos.SiguienteIdProducto()
                Datos.ListaProductos.Add(p)
            Else
                Dim existente = PorId(p.ID)
                If existente Is Nothing Then
                    resultado.Mensaje = "El producto que queres editar ya no existe en el catalogo."
                    Return resultado
                End If
                If Not ReferenceEquals(existente, p) Then
                    CopiarSobre(existente, p)
                    p = existente
                End If
            End If

            resultado.Ok = True
            resultado.Producto = p
            Return resultado
        End Function

        ''' <summary>
        ''' Copia los datos editables de un producto sobre el que ya esta en el
        ''' catalogo. Se edita el objeto original y no uno nuevo para que los pedidos,
        ''' ingresos y movimientos que lo apuntan sigan viéndolo.
        ''' </summary>
        Private Sub CopiarSobre(destino As Producto, origen As Producto)
            destino.Nombre = origen.Nombre
            destino.CodigoBarra = origen.CodigoBarra
            destino.Marca = origen.Marca
            destino.UnidadMedida = origen.UnidadMedida
            destino.PrecioCosto = origen.PrecioCosto
            destino.Precio = origen.Precio
            destino.StockMinimo = origen.StockMinimo
            destino.Categoria = origen.Categoria
            destino.Subcategoria = origen.Subcategoria
            destino.Descripcion = origen.Descripcion
            destino.Atajo = origen.Atajo
        End Sub

        Public Function PorId(id As Integer) As Producto
            Return Datos.ListaProductos.FirstOrDefault(Function(p) p.ID = id)
        End Function

        ''' <summary>
        ''' Busca por codigo de barras. Ignora el producto indicado, para que al
        ''' editar uno no se choque consigo mismo.
        ''' </summary>
        Public Function BuscarPorCodigoBarra(codigo As String,
                                             Optional ignorarID As Integer = 0) As Producto

            Dim limpio = Si(codigo, "").Trim()
            If limpio.Length = 0 Then Return Nothing
            Return Datos.ListaProductos.FirstOrDefault(
                Function(p) p.ID <> ignorarID AndAlso
                            Si(p.CodigoBarra, "").Trim().Equals(limpio,
                                                               StringComparison.CurrentCultureIgnoreCase))
        End Function

        ''' <summary>
        ''' Filtra el catalogo por texto y categoria. El texto mira nombre, marca,
        ''' descripcion y codigo de barras, que es lo que se escribe cuando se busca
        ''' algo: el nombre, parte de la marca o el numero pegado al escaner.
        '''
        ''' Las categorias y subcategorias llegan vacias cuando no se quiere filtrar
        ''' por ellas. Los productos sin dato de categoria no se pierden nunca: si se
        ''' filtra, solo quedan los que coinciden.
        ''' </summary>
        Public Function Buscar(texto As String,
                               Optional categoria As String = "",
                               Optional subcategoria As String = "") As List(Of Producto)

            Dim t = Si(texto, "").Trim().ToLowerInvariant()
            Dim cat = Si(categoria, "").Trim()
            Dim subcat = Si(subcategoria, "").Trim()

            Return Datos.ListaProductos.
                Where(Function(p) CumpleFiltro(p, t, cat, subcat)).
                OrderBy(Function(p) Si(p.Nombre, "")).
                ThenBy(Function(p) Si(p.Marca, "")).
                ToList()
        End Function

        ''' <summary>
        ''' Decide si un producto entra en el resultado. Se separa del Where para no
        ''' encadenar tres condicionales dentro de la consulta: queda mas facil de
        ''' leer y de verificar.
        ''' </summary>
        Private Function CumpleFiltro(p As Producto, texto As String,
                                       categoria As String, subcategoria As String) As Boolean

            If categoria.Length > 0 Then
                If Not Si(p.Categoria, "").Trim().Equals(categoria,
                                                         StringComparison.CurrentCultureIgnoreCase) Then
                    Return False
                End If
            End If

            If subcategoria.Length > 0 Then
                If Not Si(p.Subcategoria, "").Trim().Equals(subcategoria,
                                                            StringComparison.CurrentCultureIgnoreCase) Then
                    Return False
                End If
            End If

            If texto.Length = 0 Then Return True

            Return Si(p.Nombre, "").ToLowerInvariant().Contains(texto) OrElse
                   Si(p.Marca, "").ToLowerInvariant().Contains(texto) OrElse
                   Si(p.Descripcion, "").ToLowerInvariant().Contains(texto) OrElse
                   Si(p.Categoria, "").ToLowerInvariant().Contains(texto) OrElse
                   Si(p.Subcategoria, "").ToLowerInvariant().Contains(texto) OrElse
                   Si(p.CodigoBarra, "").ToLowerInvariant().Contains(texto)
        End Function

        ''' Texto del filtro que significa "sin filtro". Vive aca y no en cada pantalla
        ''' porque POS e Ingreso usaban "Todos" y "(todas)" por separado: dos textos
        ''' distintos para lo mismo, y con el POS mostrando una categoria y el Ingreso
        ''' otra.
        ''' </summary>
        Public Const Todas As String = "(todas)"

        ''' Categorias para armar un filtro, con la opcion de ver todas primero.
        ''' </summary>
        Public Function CategoriasParaFiltrar() As List(Of String)
            Dim lista As List(Of String) = New List(Of String)
            lista.Add(Todas)
            lista.AddRange(Categorias())
            Return lista
        End Function

        ''' Traduce el texto elegido en el filtro a la categoria real, o a cadena
        ''' vacia cuando el filtro estaba en "todas".
        ''' </summary>
        Public Function CategoriaDelFiltro(texto As String) As String
            Dim cat = Si(texto, "").Trim()
            If cat.Equals(Todas, StringComparison.OrdinalIgnoreCase) Then Return ""
            Return cat
        End Function

        ''' <summary>
        ''' Un producto pertenece a la categoria del filtro. La comparacion no
        ''' distingue mayusculas: si el filtro muestra "Bebidas" y el producto tiene
        ''' "bebidas", el filtro lo escondia y el usuario creia que no existia.
        '''
        ''' El parametro no se llama categoriaDelFiltro: VB no distingue mayusculas, asi
        ''' que con ese nombre tapaba a la funcion CategoriaDelFiltro que se llama justo
        ''' adentro, y el error aparecia mas abajo, en el End Function.
        ''' </summary>
        Public Function PerteneceACategoria(producto As Producto, categoriaElegida As String) As Boolean
            Dim cat = CategoriaDelFiltro(categoriaElegida)
            If cat.Length = 0 Then Return True
            If producto Is Nothing Then Return False
            Return Si(producto.Categoria, "").Trim().Equals(cat,
                                                            StringComparison.CurrentCultureIgnoreCase)
        End Function

        ''' <summary>Categorias que efectivamente tienen productos, sin repetir.</summary>
        Public Function Categorias() As List(Of String)
            Return Datos.ListaProductos.
                Select(Function(p) Si(p.Categoria, "").Trim()).
                Where(Function(c) c.Length > 0).
                Distinct(StringComparer.CurrentCultureIgnoreCase).
                OrderBy(Function(c) c).
                ToList()
        End Function

        ''' <summary>Subcategorias de una categoria. Sin categoria, las de todas.</summary>
        Public Function Subcategorias(Optional categoria As String = "") As List(Of String)
            Dim cat = Si(categoria, "").Trim()
            Return Datos.ListaProductos.
                Where(Function(p) cat.Length = 0 OrElse
                               Si(p.Categoria, "").Trim().Equals(cat,
                                                                 StringComparison.CurrentCultureIgnoreCase)).
                Select(Function(p) Si(p.Subcategoria, "").Trim()).
                Where(Function(s) s.Length > 0).
                Distinct(StringComparer.CurrentCultureIgnoreCase).
                OrderBy(Function(s) s).
                ToList()
        End Function

        ''' <summary>
        ''' Sugerencia de precio de venta a partir del costo. Es una ayuda, no una
        ''' imposicion: el precio final siempre lo define quien carga el producto.
        ''' </summary>
        Public Function PrecioSugerido(costo As Decimal) As Decimal
            If costo <= 0D Then Return 0D
            Return Math.Round(PrecioService.CalcularPrecioVentaRecomendado(costo), 2)
        End Function

        ''' <summary>Devuelve el texto sin espacios al borde, o vacio si era nulo.</summary>
        Private Function Limpiar(texto As String) As String
            Return Si(texto, "").Trim()
        End Function

        Private Function Si(valor As String, porDefecto As String) As String
            Return If(String.IsNullOrWhiteSpace(valor), porDefecto, valor)
        End Function

    End Module

End Namespace
