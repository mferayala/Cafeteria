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
            Return RegistrarOActualizar(nombre, telefono, pedido.Total,
                                        pedido.MetodoPago, pedido.FechaHora, pedido.ID)
        End Function

        ''' <summary>
        ''' El cliente no necesita saber si lo que se le cargo fue un Pedido o una
        ''' Venta: le interesa cuanto gasto, como pago y cuando. Por eso la version de
        ''' Venta no pasa el objeto entero, sino los mismos datos sueltos.
        ''' </summary>
        Public Function RegistrarOActualizar(nombre As String,
                                             telefono As String,
                                             venta As Venta) As ClienteRegistrado
            Return RegistrarOActualizar(nombre, telefono, venta.Total,
                                        venta.MetodoPago, venta.FechaHora, venta.ID)
        End Function

        ''' <summary>
        ''' Clave con la que se identifica a un cliente. El telefono manda cuando existe;
        ''' si no, se identifica por nombre. Vive en su propia funcion para que el mismo
        ''' criterio se use al registrar una venta y al consultar las deudas: si cada uno
        ''' calculara la clave por su cuenta, la deuda de un cliente no se encontraria.
        ''' </summary>
        Public Function ClaveDe(nombre As String, telefono As String) As String
            Dim nombreLimpio = If(nombre, String.Empty).Trim()
            Dim telLimpio = NormalizarTelefono(telefono)
            Return If(telLimpio = SinTelefono,
                      "N:" & nombreLimpio.ToLowerInvariant(),
                      "T:" & telLimpio.ToLowerInvariant())
        End Function

        ''' <summary>
        ''' Cuanto debe un cliente en total. Sale de las ventas pendientes, no de un
        ''' contador propio: el contador se desincroniza apenas se anula una venta o se
        ''' salda una deuda, y la deuda que muestra el sistema tiene que ser la misma que
        ''' la suma de lo que cada venta dice que falta.
        ''' </summary>
        Public Function Deuda(clave As String) As Decimal
            Return VentaService.DeudaDe(clave)
        End Function

        Private Function RegistrarOActualizar(nombre As String,
                                              telefono As String,
                                              total As Decimal,
                                              metodoPago As MetodoPago,
                                              fecha As DateTime,
                                              referenciaID As Integer) As ClienteRegistrado

            Dim nombreLimpio = If(nombre, String.Empty).Trim()
            Dim telLimpio = NormalizarTelefono(telefono)

            If String.IsNullOrWhiteSpace(nombreLimpio) AndAlso telLimpio = SinTelefono Then
                Return Nothing
            End If

            Dim clave = ClaveDe(nombreLimpio, telLimpio)

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
            cliente.TotalGastado += total
            cliente.UltimoPedido = fecha
            cliente.MetodoPagoHabitual = metodoPago
            If Not cliente.HistorialPedidos.Contains(referenciaID) Then
                cliente.HistorialPedidos.Add(referenciaID)
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