Imports System.Collections.Generic
Imports System.Linq

Namespace CafeteriaOS

    ''' <summary>
    ''' Ingresos de mercaderia desde proveedores. Registrar un ingreso suma el stock de
    ''' cada producto y deja el asiento que explica de donde vino cada unidad.
    ''' </summary>
    Public Module IngresoService

        ''' <summary>
        ''' Registra el ingreso completo: primero valida todo y recien despues toca el
        ''' stock, para que un dato invalido no deje mercaderia a medio sumar.
        '''
        ''' El proveedor es opcional a proposito. Hay ingresos que no vienen de una compra
        ''' (una devolucion, un rescate de mercaderia, una correccion) y forzarlos a
        ''' elegir un proveedor terminaba inventando de quien era la mercaderia.
        ''' </summary>
        Public Function Registrar(proveedor As Proveedor,
                                  detalles As IEnumerable(Of DetalleIngreso),
                                  observaciones As String) As ResultadoIngreso

            Dim resultado As New ResultadoIngreso()

            Dim lineas As New List(Of DetalleIngreso)()
            For Each d In detalles
                If d Is Nothing OrElse d.Producto Is Nothing Then
                    resultado.Mensaje = "Hay una linea sin producto."
                    Return resultado
                End If
                If d.Cantidad <= 0 Then
                    resultado.Mensaje = "La cantidad de " & d.Producto.Nombre & " tiene que ser mayor a cero."
                    Return resultado
                End If
                ' Si el mismo producto viene dos veces se acumula en una sola linea.
                Dim repetido = lineas.FirstOrDefault(Function(x) ReferenceEquals(x.Producto, d.Producto))
                If repetido Is Nothing Then
                    lineas.Add(New DetalleIngreso With {
                        .Producto = d.Producto,
                        .Cantidad = d.Cantidad,
                        .Observacion = d.Observacion
                    })
                Else
                    repetido.Cantidad += d.Cantidad
                    ' Al juntar dos lineas del mismo producto las notas se suman: si
                    ' alguna viene vacia se conserva la que habia.
                    If Not String.IsNullOrWhiteSpace(d.Observacion) Then
                        repetido.Observacion = If(String.IsNullOrWhiteSpace(repetido.Observacion),
                                                  d.Observacion,
                                                  repetido.Observacion.Trim() & " / " & d.Observacion.Trim())
                    End If
                End If
            Next

            If lineas.Count = 0 Then
                resultado.Mensaje = "Carga al menos un producto."
                Return resultado
            End If

            Dim ingreso As New IngresoStock With {
                .Fecha = DateTime.Now,
                .Proveedor = proveedor,
                .Detalles = lineas,
                .Observaciones = String.Empty & observaciones,
                .RegistradoPor = Datos.UsuarioActualNombre()
            }
            Datos.RegistrarIngreso(ingreso)

            For Each d In lineas
                StockService.Ajustar(d.Producto, d.Cantidad, TipoMovimientoStock.Ingreso,
                                     Datos.UsuarioActualNombre(), d.Observacion, ingreso.ID)
            Next

            ' El proveedor es opcional, asi que sus datos de ultima compra solo se
            ' tocan cuando hay alguien a quien pertainecen.
            If proveedor IsNot Nothing Then
                proveedor.UltimaCompra = ingreso.Fecha
                proveedor.FechaUltimaOperacion = ingreso.Fecha
            End If

            If Not IngresoAlmacen.Guardar() Then
                ' El ingreso quedo aplicado igual, pero el historico no se pudo
                ' escribir. Se avisa para que no se pierda en silencio.
                resultado.Advertencia =
                    "El ingreso se registro, pero no se pudo guardar el historial en disco."
            End If

            resultado.Ok = True
            resultado.Ingreso = ingreso
            Return resultado
        End Function

        ''' <summary>
        ''' Ingresos del proveedor, del mas nuevo al mas viejo.
        ''' </summary>
        Public Function PorProveedor(proveedor As Proveedor) As List(Of IngresoStock)
            If proveedor Is Nothing Then Return New List(Of IngresoStock)()
            Return Datos.ListaIngresos.
                Where(Function(i) ReferenceEquals(i.Proveedor, proveedor)).
                OrderByDescending(Function(i) i.Fecha).
                ThenByDescending(Function(i) i.ID).
                ToList()
        End Function

        ''' <summary>
        ''' Ingresos que incluyen un producto concreto: es el historial que permite
        ''' ubicar de que compra vino una unidad que se danio o vencio.
        ''' </summary>
        Public Function PorProducto(producto As Producto) As List(Of IngresoStock)
            If producto Is Nothing Then Return New List(Of IngresoStock)()
            Return Datos.ListaIngresos.
                Where(Function(i) i.Contiene(producto)).
                OrderByDescending(Function(i) i.Fecha).
                ThenByDescending(Function(i) i.ID).
                ToList()
        End Function

        ''' <summary>Ultimo ingreso que sumo unidades de ese producto, si hubo.</summary>
        Public Function UltimoDe(producto As Producto) As IngresoStock
            Return PorProducto(producto).FirstOrDefault()
        End Function

        ''' <summary>
        ''' Unidades del producto que todavia no se consumieron, ordenadas por fecha de
        ''' ingreso. Sirve para ubicar primero lo mas viejo, que es lo que vence antes.
        ''' </summary>
        Public Function PendientesPorVencer(producto As Producto) As List(Of LoteIngresado)
            Dim resultado As New List(Of LoteIngresado)()
            Dim restantes = producto.Stock

            For Each ingreso In PorProducto(producto).OrderBy(Function(i) i.Fecha).ThenBy(Function(i) i.ID)
                Dim cantidad = ingreso.CantidadDe(producto)
                If cantidad <= 0 Then Continue For
                Dim disponible = Math.Min(cantidad, Math.Max(0, restantes))
                If disponible > 0 Then
                    resultado.Add(New LoteIngresado With {
                        .Ingreso = ingreso,
                        .Cantidad = disponible
                    })
                    restantes -= disponible
                End If
                If restantes <= 0 Then Exit For
            Next
            Return resultado
        End Function

        ''' <summary>
        ''' Una linea del resumen de origen: "12/05/2026 - Lacteos del Sur (Jorge Paz)".
        ''' </summary>
        Public Function Describir(ingreso As IngresoStock) As String
            Dim contacto = If(ingreso.Proveedor Is Nothing, "", ingreso.Proveedor.ContactoPrincipal)
            Dim texto = ingreso.FechaCorta() & " - " &
                        If(ingreso.Proveedor Is Nothing, "(sin proveedor)", ingreso.Proveedor.Nombre)
            If Not String.IsNullOrWhiteSpace(contacto) Then texto &= " (" & contacto.Trim() & ")"
            Return texto
        End Function

    End Module

    ''' <summary>Porcion de un ingreso que todavia no se consumio.</summary>
    Public Class LoteIngresado
        Public Property Ingreso As IngresoStock
        Public Property Cantidad As Integer
    End Class

    ''' <summary>
    ''' Resultado de registrar un ingreso. Devolver el dato y no un booleano permite que
    ''' quien llama muestre el numero de ingreso recien creado.
    '''
    ''' Advertencia es para los casos en que la operacion quedo aplicada pero algo en
    ''' segundo plano fallo, por ejemplo escribir el historial en disco. No es un
    ''' error: el stock ya cambio y hay que decirlo igual.
    ''' </summary>
    Public Class ResultadoIngreso
        Public Property Ok As Boolean
        Public Property Mensaje As String
        Public Property Advertencia As String
        Public Property Ingreso As IngresoStock
    End Class

End Namespace