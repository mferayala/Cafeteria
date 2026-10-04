Imports System
Imports System.Globalization
Imports System.Linq
Imports CafeteriaOS

Namespace Prueba

    ''' <summary>
    ''' Pruebas del catalogo de categorias.
    ''' 
    ''' Van en su propio modulo y no dentro de Nucleo.Main porque Main ya
    ''' tiene casi mil lineas de alcance plano: dos pruebas que usen el
    ''' mismo nombre de variable se pisan y el compilador reporta el error
    ''' en la linea del uso, muy lejos de la declaracion que lo provoco.
    ''' </summary>
    Public Module PruebasCategoria

        Public Sub Correr()
        ' --- Categorias ---
        Dim catsOriginales = Datos.ListaCategorias.Select(Function(c) c.Nombre).ToList()
        Dim productosOriginales = Datos.ListaProductos.Count
        Dim p1 As New Producto With {
            .Nombre = "Prod A", .Categoria = "Prueba", .Subcategoria = "Uno",
            .PrecioCosto = 100D, .Precio = 200D}
        Dim p2 As New Producto With {
            .Nombre = "Prod B", .Categoria = "prueba", .Subcategoria = "DOS",
            .PrecioCosto = 100D, .Precio = 200D}
        ProductoService.Guardar(p1)
        ProductoService.Guardar(p2)
        Try
            Revisar("Guardar un producto crea su categoria si no existe",
                    CategoriaService.Existe("Prueba"))
            Revisar("Una categoria que solo cambia en mayusculas no se duplica",
                    CategoriaService.Todas().Where(
                        Function(c) c.Nombre.Equals("Prueba",
                                                    StringComparison.CurrentCultureIgnoreCase)).Count() = 1)
            Revisar("El segundo producto adopta las mayusculas del catalogo",
                    p2.Categoria = "Prueba")
            Revisar("La subcategoria queda registrada",
                    CategoriaService.SubcategoriasDe("Prueba").Count = 2)

            Revisar("No se puede crear una categoria con nombre vacio",
                    Not CategoriaService.Agregar("   ").Ok)
            Revisar("No se puede crear una categoria que ya existe",
                    Not CategoriaService.Agregar("prueba").Ok)

            Revisar("Renombrar cambia el nombre en el catalogo",
                    CategoriaService.Renombrar("Prueba", "Lacteos").Ok AndAlso
                    CategoriaService.Existe("Lacteos") AndAlso
                    Not CategoriaService.Existe("Prueba"))
            Revisar("Renombrar actualiza todos los productos que la usaban",
                    p1.Categoria = "Lacteos" AndAlso p2.Categoria = "Lacteos")
            Revisar("Renombrar deja las subcategorias en la categoria nueva",
                    CategoriaService.SubcategoriasDe("Lacteos").Count = 2)
            Revisar("No se puede renombrar a una categoria existente",
                    Not CategoriaService.Renombrar("Lacteos", Datos.ListaCategorias.Where(
                        Function(c) Not c.Nombre.Equals("Lacteos",
                                                        StringComparison.CurrentCultureIgnoreCase)
                    ).First().Nombre).Ok)
            Revisar("No se puede renombrar a vacio",
                    Not CategoriaService.Renombrar("Lacteos", " ").Ok)
            Revisar("Renombrar una categoria inexistente falla",
                    Not CategoriaService.Renombrar("NoExiste", "X").Ok)

            Dim otra = CategoriaService.Agregar("Congelados").Ok
            Revisar("Se puede crear una categoria vacia, sin productos", otra AndAlso
                    CategoriaService.Existe("Congelados"))
            Revisar("Una categoria sin productos aparece en el filtro",
                    ProductoService.Categorias().Contains("Congelados"))

            Revisar("Borrar sin reemplazo deja los productos sin categoria",
                    CategoriaService.Eliminar("Lacteos").Ok AndAlso
                    p1.Categoria = "" AndAlso p2.Categoria = "" AndAlso
                    Not CategoriaService.Existe("Lacteos"))
            Revisar("Borrar una categoria no borra productos",
                    Datos.ListaProductos.Contains(p1) AndAlso Datos.ListaProductos.Contains(p2))
            Revisar("No se puede borrar una categoria inexistente",
                    Not CategoriaService.Eliminar("NoExiste").Ok)

            Dim p3 As New Producto With {
                .Nombre = "Prod C", .Categoria = "Lacteos2", .Subcategoria = "Tres",
                .PrecioCosto = 100D, .Precio = 200D}
            ProductoService.Guardar(p3)
            CategoriaService.Agregar("Destino")
            Revisar("Borrar con reemplazo mueve los productos",
                    CategoriaService.Eliminar("Lacteos2", "Destino").Ok AndAlso
                    p3.Categoria = "Destino")
            Revisar("No se puede reemplazar por una categoria inexistente",
                    Not CategoriaService.Eliminar("Destino", "NoExiste").Ok)
            Revisar("Una categoria no puede reemplazarse a si misma",
                    Not CategoriaService.Eliminar("Destino", "destino").Ok)
            Datos.ListaProductos.Remove(p3)

            Revisar("Agregar subcategoria la registra",
                    CategoriaService.AgregarSubcategoria("Congelados", "Helados").Ok AndAlso
                    CategoriaService.SubcategoriasDe("Congelados").Contains("Helados"))
            Revisar("No se agrega dos veces la misma subcategoria",
                    Not CategoriaService.AgregarSubcategoria("Congelados", "helados").Ok)
            Revisar("No se agrega subcategoria a una categoria inexistente",
                    Not CategoriaService.AgregarSubcategoria("NoExiste", "X").Ok)

            Dim p4 As New Producto With {
                .Nombre = "Prod D", .Categoria = "Congelados", .Subcategoria = "Helados",
                .PrecioCosto = 100D, .Precio = 200D}
            ProductoService.Guardar(p4)
            Revisar("Quitar subcategoria la saca de los productos de esa categoria",
                    CategoriaService.EliminarSubcategoria("Congelados", "Helados").Ok AndAlso
                    p4.Subcategoria = "")
            Revisar("Quitar una subcategoria que no existe falla",
                    Not CategoriaService.EliminarSubcategoria("Congelados", "Helados").Ok)
            Datos.ListaProductos.Remove(p4)

            Dim otroProducto As New Producto With {
                .Nombre = "Prod E", .Categoria = "Lacteos3", .Subcategoria = "Compartida",
                .PrecioCosto = 100D, .Precio = 200D}
            ProductoService.Guardar(otroProducto)

            ' El caso que importa: la MISMA subcategoria existe en dos categorias.
            ' Quitarla de una tiene que tocar solo los productos de esa. Si el
            ' servicio no mirara la categoria del producto, el de Lacteos3 perderia
            ' la subcategoria tambien.
            Dim altaEnCongelados = CategoriaService.AgregarSubcategoria("Congelados", "Compartida")
            Dim bajaDeUna = CategoriaService.EliminarSubcategoria("Congelados", "Compartida")
            Revisar("Se puede quitar una subcategoria que esta en esa categoria",
                    altaEnCongelados.Ok AndAlso bajaDeUna.Ok)
            Revisar("Quitar de una categoria deja intacta la de la otra",
                    otroProducto.Subcategoria = "Compartida")
            Revisar("La subcategoria sigue en la categoria que no se toco",
                    CategoriaService.SubcategoriasDe("Lacteos3").Contains("Compartida"))
            Revisar("La subcategoria ya no esta en la categoria de la que se quito",
                    Not CategoriaService.SubcategoriasDe("Congelados").Contains("Compartida"))

            Dim altaRepetida = CategoriaService.AgregarSubcategoria("Lacteos3", "Compartida")
            Revisar("Agregar dos veces la misma subcategoria en la misma categoria falla",
                    Not altaRepetida.Ok)
            Revisar("Una subcategoria de una categoria que no la tiene no se puede quitar",
                    Not CategoriaService.EliminarSubcategoria("Congelados", "Compartida").Ok)
            Datos.ListaProductos.Remove(otroProducto)

            ' El formato de catalogo: primera letra en mayuscula, resto en
            ' minuscula, sin depender del codepage del sistema.
            Revisar("Un nombre en mayusculas queda con formato de catalogo",
                    CategoriaService.FormatoDeCatalogo("BEBIDAS") = "Bebidas")
            Revisar("Un nombre en minusculas queda con formato de catalogo",
                    CategoriaService.FormatoDeCatalogo("bebidas") = "Bebidas")
            Revisar("Un nombre con espacios al borde se limpia",
                    CategoriaService.FormatoDeCatalogo("  bebidas  ") = "Bebidas")
            Revisar("El formato no rompe las vocales acentuadas",
                    CategoriaService.FormatoDeCatalogo("PANADERÍA") = "Panadería")
            Revisar("El formato deja las palabras internas en minuscula",
                    CategoriaService.FormatoDeCatalogo("BEBIDAS SIN GAS") = "Bebidas sin gas")

            ' Normalizar tiene que devolver el nombre COMO ESTA GUARDADO cuando la
            ' categoria ya existe. Si reformateara el texto recibido volveria a
            ' crear la variante que existe para evitar: con el catalogo en
            ' "BEBIDAS SIN GAS", escribir "bebidas sin gas" no puede guardar otra
            ' vez en minusculas.
            Dim altaRara = CategoriaService.Agregar("BEBIDAS SIN GAS").Ok
            Revisar("Se puede crear una categoria con mayusculas",
                    altaRara AndAlso
                    CategoriaService.Buscar("BEBIDAS SIN GAS").Nombre = "BEBIDAS SIN GAS")
            Revisar("Normalizar devuelve el nombre guardado, no el texto recibido",
                    CategoriaService.Normalizar("bebidas sin gas") = "BEBIDAS SIN GAS")
            Revisar("Guardar un producto con esa categoria no crea una variante",
                    ProductoService.Guardar(New Producto With {
                        .Nombre = "Prod F", .Categoria = "bebidas sin gas",
                        .Subcategoria = "Con Gas", .PrecioCosto = 100D,
                        .Precio = 200D}).Ok)
            Revisar("El producto queda con la categoria tal como estaba en el catalogo",
                    Datos.ListaProductos.Last().Categoria = "BEBIDAS SIN GAS")
            Revisar("La categoria rara no se duplica por escribirla distinto",
                    CategoriaService.Todas().Where(
                        Function(c) c.Nombre.Equals("bebidas sin gas",
                                                    StringComparison.CurrentCultureIgnoreCase)).Count() = 1)
            Revisar("Se puede borrar la categoria de prueba",
                    CategoriaService.Eliminar("BEBIDAS SIN GAS").Ok)
            Datos.ListaProductos.Remove(Datos.ListaProductos.Last())
        Finally
            ' Datos.SembrarCategorias rehace el catalogo desde los productos, que
            ' es justo el estado en que estaba antes de estas pruebas. Hace falta
            ' volver a sembrar y no solo borrar lo agregado: renombrar y borrar
            ' dejaron productos con otra categoria.
            While Datos.ListaProductos.Count > productosOriginales
                Datos.ListaProductos.RemoveAt(Datos.ListaProductos.Count - 1)
            End While
            Datos.SembrarCategorias()
        End Try


        End Sub

    End Module
End Namespace