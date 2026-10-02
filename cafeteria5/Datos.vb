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

        Public UsuarioActual As Usuario
        Public Caja As New SesionCaja()

        Private contadorPedidos As Integer = 1
        Private contadorIngresos As Integer = 1

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
        ''' Retoma la numeracion despues de recuperar el historial del disco, para que
        ''' un ingreso guardado no compita por numero con uno nuevo.
        ''' </summary>
        Public Sub AjustarContadorIngresos(maxId As Integer)
            If maxId >= contadorIngresos Then contadorIngresos = maxId + 1
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