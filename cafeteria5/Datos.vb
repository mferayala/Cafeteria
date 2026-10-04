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