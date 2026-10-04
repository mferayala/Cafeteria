Imports System.Collections.Generic

Namespace CafeteriaOS

    ''' <summary>
    ''' Estado compartido de la sesion. Solo contiene el estado: la carga inicial vive en
    ''' DatosIniciales y las reglas estan en Servicios.
    ''' </summary>
    Public Module Datos

        Public ListaProductos As New List(Of Producto)
        Public ListaPedidos As New List(Of Pedido)
        Public ListaClientes As New List(Of ClienteRegistrado)
        Public ListaProveedores As New List(Of Proveedor)
        Public ListaIngresos As New List(Of IngresoStock)()
        Public ListaMovimientos As New List(Of MovimientoStock)()

        ''' <summary>
        ''' Catalogo de categorias. Vive aparte de los productos para que se pueda
        ''' crear una categoria antes de que tenga algo cargado, y para renombrarla
        ''' una sola vez en lugar de producto por producto.
        ''' </summary>
        Public ListaCategorias As New List(Of Categoria)()

        '''Configuracion global del negocio (accesible para administradores).'''
        Public ConfigNegocio As New ConfiguracionNegocio()

        '''Usuario actualmente logueado (o Nothing si no hay sesion).'''
        Public UsuarioActual As Usuario
        Public Caja As New SesionCaja()

        Private contadorPedidos As Integer = 1
        Private contadorIngresos As Integer = 1
        Private contadorMovimientos As Integer = 1

        ''' <summary>
        ''' Secuencia independiente de la cantidad de pedidos: no se repiten numeros si
        ''' algun pedido llegara a eliminarse de la lista.
        ''' </summary>
        Public Function SiguienteIdPedido() As Integer
            Return contadorPedidos
        End Function

        Public Sub RegistrarPedido(p As Pedido)
            p.ID = contadorPedidos
            contadorPedidos += 1
            ListaPedidos.Add(p)
        End Sub

        ''' <summary>
        ''' Secuencia propia de los ingresos: los numeros no se repiten aunque se borre
        ''' alguno de la lista.
        ''' </summary>
        Public Function SiguienteIdIngreso() As Integer
            Return contadorIngresos
        End Function

        ''' <summary>
        ''' Id para un producto nuevo: uno mas del mayor que exista. No se usa un
        ''' contador aparte porque el catalogo arranca con datos de ejemplo que ya
        ''' traen sus propios numeros, y un contador independiente terminaria
        ''' chocando con alguno de ellos.
        ''' </summary>
        Public Function SiguienteIdProducto() As Integer
            Dim mayor = 0
            For Each p In ListaProductos
                If p.ID > mayor Then mayor = p.ID
            Next
            Return mayor + 1
        End Function

        ''' <summary>
        ''' Retoma la numeracion despues de recuperar el historial del disco, para que
        ''' un ingreso guardado no compita por numero con uno nuevo.
        ''' </summary>
        Public Sub AjustarContadorIngresos(maxId As Integer)
            If maxId >= contadorIngresos Then contadorIngresos = maxId + 1
        End Sub

        Public Function SiguienteIdMovimiento() As Integer
            Return contadorMovimientos
        End Function

        ''' <summary>
        ''' Id para una categoria nueva. Uno mas del mayor que exista, igual que los
        ''' productos, porque el catalogo tambien se arma con datos de ejemplo.
        ''' </summary>
        Public Function SiguienteIdCategoria() As Integer
            Dim mayor = 0
            For Each c In ListaCategorias
                If c.ID > mayor Then mayor = c.ID
            Next
            Return mayor + 1
        End Function

        ''' <summary>
        ''' Crea el catalogo de categorias a partir de los productos que ya existen.
        '''
        ''' No se duplican las que difieren solo en mayusculas y cada producto queda
        ''' con la misma capitalizacion que la categoria creada, para que el filtro no
        ''' muestre "Bebidas" y "bebidas" como dos cosas distintas.
        ''' </summary>
        Public Sub SembrarCategorias()
            ListaCategorias.Clear()
            For Each p In ListaProductos
                Dim nombre = If(p.Categoria, "").Trim()
                If nombre.Length = 0 Then Continue For

                Dim categoria = CategoriaService.Buscar(nombre)
                If categoria Is Nothing Then
                    categoria = New Categoria With {
                        .ID = SiguienteIdCategoria(),
                        .Nombre = CategoriaService.FormatoDeCatalogo(nombre)
                    }
                    ListaCategorias.Add(categoria)
                End If

                If Not categoria.Nombre.Equals(p.Categoria.Trim(),
                                               StringComparison.CurrentCultureIgnoreCase) Then
                    p.Categoria = categoria.Nombre
                End If

                ' "sub" es palabra clave en VB: como nombre de variable rompe
                ' el parseo con un error que no apunta a esta linea.
                Dim subcat = If(p.Subcategoria, "").Trim()
                If subcat.Length > 0 AndAlso
                   Not categoria.Subcategorias.Any(
                       Function(s) s.Equals(subcat, StringComparison.CurrentCultureIgnoreCase)) Then
                    categoria.Subcategorias.Add(subcat)
                End If
            Next
        End Sub

        Public Sub RegistrarMovimiento(m As MovimientoStock)
            If m Is Nothing Then Return
            m.ID = contadorMovimientos
            contadorMovimientos += 1
            ListaMovimientos.Add(m)
        End Sub



        Public Sub RegistrarIngreso(i As IngresoStock)
            i.ID = contadorIngresos
            contadorIngresos += 1
            ListaIngresos.Add(i)
        End Sub

        Public Function UsuarioActualNombre() As String
            If UsuarioActual Is Nothing Then Return "Sistema"
            Return UsuarioActual.Nombre
        End Function

    End Module

End Namespace