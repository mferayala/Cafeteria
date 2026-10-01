Imports System
Imports System.Collections.Generic
Imports System.Linq

Namespace CafeteriaOS

    ''' <summary>
    ''' Registro de clientes. Una misma persona no se duplica: se busca por telefono y, si
    ''' no tiene, por nombre, en lugar de crear un registro nuevo por cada pedido.
    ''' </summary>
    Public Module ClienteService

        Public Const SinTelefono As String = "--"

        ''' <summary>
        ''' Devuelve el cliente existente que coincide con los datos, o lo agrega. Si ya
        ''' existe, actualiza el nombre, el medio de pago habitual y el total gastado.
        ''' </summary>
        Public Function RegistrarOActualizar(nombre As String,
                                             telefono As String,
                                             pedido As Pedido) As ClienteRegistrado

            Dim nombreLimpio = If(nombre, String.Empty).Trim()
            Dim telLimpio = NormalizarTelefono(telefono)

            If String.IsNullOrWhiteSpace(nombreLimpio) AndAlso telLimpio = SinTelefono Then
                Return Nothing
            End If

            Dim clave = If(telLimpio = SinTelefono,
                           "N:" & nombreLimpio.ToLowerInvariant(),
                           "T:" & telLimpio.ToLowerInvariant())

            Dim cliente = Datos.ListaClientes.FirstOrDefault(Function(c) c.Clave = clave)

            If cliente Is Nothing Then
                cliente = New ClienteRegistrado With {
                    .Nombre = If(String.IsNullOrWhiteSpace(nombreLimpio), "Cliente sin nombre", nombreLimpio),
                    .Telefono = telLimpio
                }
                Datos.ListaClientes.Add(cliente)
            ElseIf Not String.IsNullOrWhiteSpace(nombreLimpio) Then
                cliente.Nombre = nombreLimpio
            End If

            cliente.CantidadPedidos += 1
            cliente.TotalGastado += pedido.Total
            cliente.UltimoPedido = pedido.FechaHora
            cliente.MetodoPagoHabitual = pedido.MetodoPago
            If Not cliente.HistorialPedidos.Contains(pedido.ID) Then
                cliente.HistorialPedidos.Add(pedido.ID)
            End If
            Return cliente
        End Function

        Public Function NormalizarTelefono(telefono As String) As String
            Dim t = If(telefono, String.Empty).Trim()
            If t.Length = 0 OrElse t = SinTelefono OrElse t.All(Function(c) Not Char.IsDigit(c) AndAlso c <> "+") Then
                Return SinTelefono
            End If
            Return t
        End Function

        Public Function Buscar(texto As String) As List(Of ClienteRegistrado)
            Dim t = If(texto, String.Empty).Trim().ToLowerInvariant()
            If t.Length = 0 Then Return Datos.ListaClientes.ToList()
            Return Datos.ListaClientes.
                Where(Function(c) c.Nombre.ToLowerInvariant().Contains(t) OrElse
                                 c.Telefono.ToLowerInvariant().Contains(t)).
                ToList()
        End Function

    End Module

End Namespace