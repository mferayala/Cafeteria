Imports System.Collections.Generic
Imports System.Linq

Namespace CafeteriaOS

    ''' <summary>
    ''' Una cantidad de un producto que entró al deposito y de quien vino. Es el eslabon
    ''' que permite responder de donde sale cada unidad: si una caja de leches vence,
    ''' el historial muestra la fecha, el proveedor y cuantas unidades trajo.
    ''' </summary>
    Public Class DetalleIngreso
        Public Property Producto As Producto
        Public Property Cantidad As Integer

        Public Function Describe() As String
            Return Cantidad & " x " & If(Producto Is Nothing, "(sin producto)", Producto.Nombre)
        End Function
    End Class

    ''' <summary>
    ''' Ingreso de mercaderia de un proveedor, con una o varias lineas de producto. Vale
    ''' para cualquier rubro: el ingreso es generico y no asume nada de cafeteria.
    ''' </summary>
    Public Class IngresoStock
        Public Property ID As Integer
        Public Property Fecha As DateTime = DateTime.Now
        Public Property Proveedor As Proveedor
        Public Property Detalles As New List(Of DetalleIngreso)()
        Public Property Observaciones As String
        Public Property RegistradoPor As String

        Public ReadOnly Property CantidadTotal As Integer
            Get
                Return Detalles.Sum(Function(d) d.Cantidad)
            End Get
        End Property

        Public Function Contiene(producto As Producto) As Boolean
            Return Detalles.Any(Function(d) ReferenceEquals(d.Producto, producto))
        End Function

        Public Function CantidadDe(producto As Producto) As Integer
            Dim detalle = Detalles.FirstOrDefault(Function(d) ReferenceEquals(d.Producto, producto))
            If detalle Is Nothing Then Return 0
            Return detalle.Cantidad
        End Function

        ''' <summary>Fecha del ingreso formateada para las listas de historial.</summary>
        Public Function FechaCorta() As String
            Return Fecha.ToString("dd/MM/yyyy")
        End Function
    End Class

End Namespace