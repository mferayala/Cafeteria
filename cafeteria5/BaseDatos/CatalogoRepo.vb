Imports MySql.Data.MySqlClient

Namespace CafeteriaOS

    ''' <summary>
    ''' Categorias, subcategorias y productos.
    '''
    ''' Los ids de categorias y productos los asigna el programa (Datos.SiguienteId...)
    ''' y se guardan tal cual, para que el id en memoria y el de MySQL sean el mismo.
    ''' </summary>
    Public NotInheritable Class CatalogoRepo

        Private Sub New()
        End Sub

        ' ------------------------------------------------------------------
        '  Categorias
        ' ------------------------------------------------------------------

        Public Shared Sub InsertarCategoria(c As Categoria)
            Db.EnTransaccion(
                Sub(cx, tx)
                    Db.Ejecutar(cx, tx, "INSERT INTO categorias (id, nombre) VALUES (@id, @nombre)",
                                "@id", c.ID, "@nombre", c.Nombre)
                End Sub)
        End Sub

        ''' <summary>
        ''' Los productos apuntan a la categoria por id, asi que renombrarla es un solo
        ''' UPDATE: no hace falta tocar los productos como en la version en memoria.
        ''' </summary>
        Public Shared Sub RenombrarCategoria(id As Integer, nombreNuevo As String)
            Db.EnTransaccion(
                Sub(cx, tx)
                    Db.Ejecutar(cx, tx, "UPDATE categorias SET nombre = @nombre WHERE id = @id",
                                "@nombre", nombreNuevo, "@id", id)
                End Sub)
        End Sub

        ''' <summary>
        ''' Borra la categoria. Si hay reemplazo, sus productos pasan a esa; si no,
        ''' quedan sin categoria (la clave foranea los pone en NULL). Las subcategorias
        ''' se borran solas por ON DELETE CASCADE.
        ''' </summary>
        Public Shared Sub EliminarCategoria(id As Integer, reemplazoId As Integer)
            Db.EnTransaccion(
                Sub(cx, tx)
                    If reemplazoId > 0 Then
                        Db.Ejecutar(cx, tx,
                            "UPDATE productos SET categoria_id = @nueva, subcategoria_id = NULL " &
                            "WHERE categoria_id = @vieja",
                            "@nueva", reemplazoId, "@vieja", id)
                    End If
                    Db.Ejecutar(cx, tx, "DELETE FROM categorias WHERE id = @id", "@id", id)
                End Sub)
        End Sub

        Public Shared Sub InsertarSubcategoria(categoriaId As Integer, nombre As String)
            Db.EnTransaccion(
                Sub(cx, tx)
                    Db.Ejecutar(cx, tx,
                        "INSERT INTO subcategorias (categoria_id, nombre) VALUES (@cat, @nombre)",
                        "@cat", categoriaId, "@nombre", nombre)
                End Sub)
        End Sub

        ''' <summary>Los productos que la usaban quedan sin subcategoria (ON DELETE SET NULL).</summary>
        Public Shared Sub EliminarSubcategoria(categoriaId As Integer, nombre As String)
            Db.EnTransaccion(
                Sub(cx, tx)
                    Db.Ejecutar(cx, tx,
                        "DELETE FROM subcategorias WHERE categoria_id = @cat AND nombre = @nombre",
                        "@cat", categoriaId, "@nombre", nombre)
                End Sub)
        End Sub

        ' ------------------------------------------------------------------
        '  Productos
        ' ------------------------------------------------------------------

        ''' <summary>Alta de un producto con sus opciones. El ID ya viene asignado.</summary>
        Public Shared Sub InsertarProducto(p As Producto)
            Db.EnTransaccion(
                Sub(cx, tx)
                    Dim categoriaId = IdCategoria(p.Categoria)
                    Db.Ejecutar(cx, tx,
                        "INSERT INTO productos " &
                        "  (id, categoria_id, subcategoria_id, codigo, nombre, marca, descripcion, " &
                        "   precio_venta, costo, stock_actual, stock_minimo, unidad_medida, atajo) " &
                        "VALUES (@id, @cat, @sub, @codigo, @nombre, @marca, @desc, " &
                        "        @precio, @costo, @stock, @minimo, @unidad, @atajo)",
                        "@id", p.ID,
                        "@cat", categoriaId,
                        "@sub", IdSubcategoria(cx, tx, categoriaId, p.Subcategoria),
                        "@codigo", Db.NuloSiVacio(p.CodigoBarra),
                        "@nombre", p.Nombre,
                        "@marca", Db.NuloSiVacio(p.Marca),
                        "@desc", Db.NuloSiVacio(p.Descripcion),
                        "@precio", p.Precio,
                        "@costo", p.PrecioCosto,
                        "@stock", Math.Max(0, p.Stock),
                        "@minimo", p.StockMinimo,
                        "@unidad", If(String.IsNullOrWhiteSpace(p.UnidadMedida), "un", p.UnidadMedida),
                        "@atajo", Db.NuloSiVacio(p.Atajo))

                    Dim orden = 0
                    For Each grupo In p.Specs
                        For Each opcion In grupo.Opciones
                            orden += 1
                            Db.Ejecutar(cx, tx,
                                "INSERT INTO producto_opciones (producto_id, grupo, opcion, orden) " &
                                "VALUES (@p, @g, @o, @n)",
                                "@p", p.ID, "@g", grupo.Etiqueta, "@o", opcion, "@n", orden)
                        Next
                    Next
                End Sub)
        End Sub

        ''' <summary>
        ''' Guarda los cambios de un producto. El stock NO se toca: solo cambia a traves
        ''' de movimientos de stock (ventas, ingresos, ajustes), igual que en el programa.
        ''' </summary>
        Public Shared Sub ActualizarProducto(p As Producto)
            Db.EnTransaccion(
                Sub(cx, tx)
                    Dim categoriaId = IdCategoria(p.Categoria)
                    Db.Ejecutar(cx, tx,
                        "UPDATE productos SET categoria_id = @cat, subcategoria_id = @sub, " &
                        "  codigo = @codigo, nombre = @nombre, marca = @marca, descripcion = @desc, " &
                        "  precio_venta = @precio, costo = @costo, stock_minimo = @minimo, " &
                        "  unidad_medida = @unidad, atajo = @atajo " &
                        "WHERE id = @id",
                        "@cat", categoriaId,
                        "@sub", IdSubcategoria(cx, tx, categoriaId, p.Subcategoria),
                        "@codigo", Db.NuloSiVacio(p.CodigoBarra),
                        "@nombre", p.Nombre,
                        "@marca", Db.NuloSiVacio(p.Marca),
                        "@desc", Db.NuloSiVacio(p.Descripcion),
                        "@precio", p.Precio,
                        "@costo", p.PrecioCosto,
                        "@minimo", p.StockMinimo,
                        "@unidad", If(String.IsNullOrWhiteSpace(p.UnidadMedida), "un", p.UnidadMedida),
                        "@atajo", Db.NuloSiVacio(p.Atajo),
                        "@id", p.ID)
                End Sub)
        End Sub

        ''' <summary>Id de la categoria por nombre, o Nothing (NULL) si no tiene.</summary>
        Private Shared Function IdCategoria(nombre As String) As Object
            Dim c = CategoriaService.Buscar(nombre)
            If c Is Nothing Then Return Nothing
            Return c.ID
        End Function

        Private Shared Function IdSubcategoria(cx As MySqlConnection,
                                               tx As MySqlTransaction,
                                               categoriaId As Object,
                                               nombre As String) As Object
            If categoriaId Is Nothing OrElse String.IsNullOrWhiteSpace(nombre) Then Return Nothing
            Return Db.Escalar(cx, tx,
                "SELECT id FROM subcategorias WHERE categoria_id = @cat AND nombre = @nombre",
                "@cat", categoriaId, "@nombre", nombre.Trim())
        End Function

    End Class

End Namespace
