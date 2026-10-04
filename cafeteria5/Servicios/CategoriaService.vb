Imports System.Collections.Generic
Imports System.Linq
Imports System.Globalization

Namespace CafeteriaOS

    ''' <summary>
    ''' Alta, renombrado y borrado de categorias y subcategorias.
    '''
    ''' Acá viven las reglas para que el editor de productos no tenga que repetirlas y
    ''' una pantalla de administracion no pueda romper lo que el editor garantiza.
    '''
    ''' La comparacion de nombres no distingue mayusculas: "Bebidas" y "bebidas" son la
    ''' misma categoria. Guardarlas como dos distintas llenaba los filtros de dos
    ''' opciones que en realidad significaban lo mismo.
    ''' </summary>
    Public Module CategoriaService

        ''' <summary>Resultado de intentar modificar el catalogo de categorias.</summary>
        Public Class ResultadoCategoria
            Public Property Ok As Boolean
            Public Property Mensaje As String

            Public Shared Function Exito() As ResultadoCategoria
                Return New ResultadoCategoria With {.Ok = True}
            End Function

            ' "Error" es palabra clave, asi que el metodo se llama Falla.
            Public Shared Function Falla(mensaje As String) As ResultadoCategoria
                Return New ResultadoCategoria With {.Ok = False, .Mensaje = mensaje}
            End Function
        End Class

        Public Function Todas() As List(Of Categoria)
            Return Datos.ListaCategorias.OrderBy(Function(c) c.Nombre).ToList()
        End Function

        ''' <summary>
        ''' Busca una categoria por nombre, sin distinguir mayusculas ni espacios.
        ''' Devuelve Nothing si no existe.
        ''' </summary>
        Public Function Buscar(nombre As String) As Categoria
            Dim texto = Limpiar(nombre)
            If texto.Length = 0 Then Return Nothing
            Return Datos.ListaCategorias.FirstOrDefault(
                Function(c) c.Nombre.Equals(texto, StringComparison.CurrentCultureIgnoreCase))
        End Function

        Public Function Existe(nombre As String) As Boolean
            Return Buscar(nombre) IsNot Nothing
        End Function

        ''' <summary>
        ''' Crea una categoria. Si ya existe una con ese nombre no crea otra: la
        ''' devuelve, para que volver a cargar el mismo nombre no duplique la lista.
        ''' </summary>
        Public Function Agregar(nombre As String) As ResultadoCategoria
            Dim texto = Limpiar(nombre)
            If texto.Length = 0 Then
                Return ResultadoCategoria.Falla("La categoria no puede quedar vacia.")
            End If

            Dim existente = Buscar(texto)
            If existente IsNot Nothing Then
                Return ResultadoCategoria.Falla("Ya existe la categoria """ & texto & """.")
            End If

            Dim nueva As New Categoria With {
                .ID = Datos.SiguienteIdCategoria(),
                .Nombre = texto
            }
            Datos.ListaCategorias.Add(nueva)
            Return ResultadoCategoria.Exito()
        End Function

        ''' <summary>
        ''' Renombra una categoria y actualiza todos los productos que la usan. Sin
        ''' esto habia que cambiar el nombre producto por producto, y alguno se
        ''' olvida y queda con una categoria que ya no existe en el catalogo.
        ''' </summary>
        Public Function Renombrar(nombreViejo As String, nombreNuevo As String) As ResultadoCategoria
            Dim categoria = Buscar(nombreViejo)
            If categoria Is Nothing Then
                Return ResultadoCategoria.Falla("La categoria """ & nombreViejo & """ no existe.")
            End If

            Dim texto = Limpiar(nombreNuevo)
            If texto.Length = 0 Then
                Return ResultadoCategoria.Falla("La categoria no puede quedar vacia.")
            End If

            Dim destino = Buscar(texto)
            If destino IsNot Nothing AndAlso Not ReferenceEquals(destino, categoria) Then
                Return ResultadoCategoria.Falla(
                    "Ya existe la categoria """ & texto & """. Renombrarla fusionaria las dos.")
            End If

            Dim viejo = categoria.Nombre
            categoria.Nombre = texto
            For Each p In Datos.ListaProductos
                If Limpiar(p.Categoria).Equals(viejo, StringComparison.CurrentCultureIgnoreCase) Then
                    p.Categoria = texto
                End If
            Next
            Return ResultadoCategoria.Exito()
        End Function

        ''' <summary>
        ''' Borra una categoria. Los productos que la usaban quedan sin categoria, o
        ''' pasan a otra si se indica: no se borran productos porque se haya borrado
        ''' una etiqueta.
        ''' </summary>
        Public Function Eliminar(nombre As String, Optional categoriaReemplazo As String = "") As ResultadoCategoria
            Dim categoria = Buscar(nombre)
            If categoria Is Nothing Then
                Return ResultadoCategoria.Falla("La categoria """ & nombre & """ no existe.")
            End If

            Dim destino As Categoria = Nothing
            Dim reemplazo = Limpiar(categoriaReemplazo)
            If reemplazo.Length > 0 Then
                destino = Buscar(reemplazo)
                If destino Is Nothing Then
                    Return ResultadoCategoria.Falla("La categoria de reemplazo no existe.")
                End If
                If ReferenceEquals(destino, categoria) Then
                    Return ResultadoCategoria.Falla("Una categoria no puede reemplazarse a si misma.")
                End If
            End If

            Dim viejo = categoria.Nombre
            For Each p In Datos.ListaProductos
                If Limpiar(p.Categoria).Equals(viejo, StringComparison.CurrentCultureIgnoreCase) Then
                    p.Categoria = If(destino Is Nothing, "", destino.Nombre)
                End If
            Next

            Datos.ListaCategorias.Remove(categoria)
            Return ResultadoCategoria.Exito()
        End Function

        ''' <summary>Agrega una subcategoria a una categoria, sin repetir.</summary>
        Public Function AgregarSubcategoria(nombreCategoria As String,
                                           nombreSubcategoria As String) As ResultadoCategoria
            Dim categoria = Buscar(nombreCategoria)
            If categoria Is Nothing Then
                Return ResultadoCategoria.Falla("La categoria """ & nombreCategoria & """ no existe.")
            End If

            Dim texto = Limpiar(nombreSubcategoria)
            If texto.Length = 0 Then
                Return ResultadoCategoria.Falla("La subcategoria no puede quedar vacia.")
            End If

            If categoria.Subcategorias.Any(
                Function(s) s.Equals(texto, StringComparison.CurrentCultureIgnoreCase)) Then
                Return ResultadoCategoria.Falla(
                    "La categoria """ & categoria.Nombre & """ ya tiene la subcategoria """ & texto & """.")
            End If

            categoria.Subcategorias.Add(texto)
            Return ResultadoCategoria.Exito()
        End Function

        ''' <summary>
        ''' Borra una subcategoria y actualiza los productos que la usaban con esa
        ''' categoria. Si la usan productos de otra categoria no la toca.
        ''' </summary>
        Public Function EliminarSubcategoria(nombreCategoria As String,
                                            nombreSubcategoria As String) As ResultadoCategoria
            Dim categoria = Buscar(nombreCategoria)
            If categoria Is Nothing Then
                Return ResultadoCategoria.Falla("La categoria """ & nombreCategoria & """ no existe.")
            End If

            Dim texto = Limpiar(nombreSubcategoria)
            Dim existente = categoria.Subcategorias.FirstOrDefault(
                Function(s) s.Equals(texto, StringComparison.CurrentCultureIgnoreCase))
            If existente Is Nothing Then
                Return ResultadoCategoria.Falla(
                    "La categoria """ & categoria.Nombre & """ no tiene la subcategoria """ & texto & """.")
            End If

            categoria.Subcategorias.Remove(existente)
            For Each p In Datos.ListaProductos
                If Limpiar(p.Categoria).Equals(categoria.Nombre, StringComparison.CurrentCultureIgnoreCase) AndAlso
                   Limpiar(p.Subcategoria).Equals(texto, StringComparison.CurrentCultureIgnoreCase) Then
                    p.Subcategoria = String.Empty
                End If
            Next
            Return ResultadoCategoria.Exito()
        End Function

        ''' <summary>
        ''' Subcategorias de una categoria, mas las que usan los productos aunque no
        ''' esten registradas. Un producto cargado antes de que existiera el panel no
        ''' puede dejar de aparecer en el filtro.
        ''' </summary>
        Public Function SubcategoriasDe(nombreCategoria As String) As List(Of String)
            Dim cat = Limpiar(nombreCategoria)
            Dim resultado As New List(Of String)()

            Dim categoria = Buscar(cat)
            If categoria IsNot Nothing Then
                resultado.AddRange(categoria.Subcategorias)
            End If

            For Each p In Datos.ListaProductos
                If cat.Length = 0 OrElse
                   Limpiar(p.Categoria).Equals(cat, StringComparison.CurrentCultureIgnoreCase) Then
                    ' "sub" es palabra clave en VB.
                    Dim subcat = Limpiar(p.Subcategoria)
                    If subcat.Length > 0 AndAlso
                       Not resultado.Any(Function(s) s.Equals(subcat, StringComparison.CurrentCultureIgnoreCase)) Then
                        resultado.Add(subcat)
                    End If
                End If
            Next

            Return resultado.OrderBy(Function(s) s).ToList()
        End Function

        ''' <summary>
        ''' La forma canonica de escribir un nombre de categoria: si ya existe, se
        ''' devuelve con la capitalizacion que ya usa el catalogo. Asi "bebidas" y
        ''' "BEBIDAS" terminan guardados como "Bebidas".
        ''' </summary>
        Public Function Normalizar(nombre As String) As String
            Dim texto = Limpiar(nombre)
            If texto.Length = 0 Then Return String.Empty

            Dim categoria = Buscar(texto)
            If categoria Is Nothing Then
                ' Una categoria nueva queda con la primera letra en mayuscula y el
                ' resto en minuscula, que es como se ven en el catalogo.
                Return FormatoDeCatalogo(texto)
            End If

            ' Si ya existe se devuelve el nombre como esta guardado, no el texto
            ' recibido reformateado: reformatear aqui volveria a crear la variante
            ' que Normalizar existe para evitar. Si el catalogo tiene
            ' "BEBIDAS SIN GAS" y el usuario escribe "bebidas sin gas", tiene que
            ' quedar "BEBIDAS SIN GAS".
            Return categoria.Nombre
        End Function

        Public Function NormalizarSubcategoria(nombreCategoria As String, nombre As String) As String
            Dim texto = Limpiar(nombre)
            If texto.Length = 0 Then Return String.Empty

            For Each s In SubcategoriasDe(nombreCategoria)
                If s.Equals(texto, StringComparison.CurrentCultureIgnoreCase) Then Return s
            Next
            Return texto
        End Function

        ''' <summary>
        ''' Deja un nombre con la forma del catalogo: la primera letra en mayuscula y
        ''' el resto en minuscula.
        '''
        ''' No se usa StrConv(ProperCase) porque depende del codepage ANSI del sistema
        ''' y en .NET Core ese proveedor no esta registrado: la app compile bien y
        ''' revienta al arrancar. Aparte StrConv pondria "Sin Gas" con mayuscula en
        ''' medio de la frase, que no es como se lee un nombre de categoria.
        ''' </summary>
        Public Function FormatoDeCatalogo(texto As String) As String
            Dim limpio = Limpiar(texto)
            If limpio.Length = 0 Then Return String.Empty

            Return Char.ToUpper(limpio(0), CultureInfo.CurrentCulture) &
                   limpio.Substring(1).ToLower(CultureInfo.CurrentCulture)
        End Function

        Private Function Limpiar(texto As String) As String
            Return If(texto, "").Trim()
        End Function

    End Module

End Namespace
