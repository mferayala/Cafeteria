Imports System

Namespace CafeteriaOS

    Public Enum TipoMovimientoStock
        Ingreso
        Reposicion
        Ajuste
        Venta
        Cancelacion
        Devolucion
    End Enum

    Public Class MovimientoStock
        Public Property ID As Integer
        Public Property Fecha As DateTime = DateTime.Now
        Public Property Producto As Producto
        Public Property Cantidad As Integer
        Public Property Tipo As TipoMovimientoStock
        Public Property Origen As String
        Public Property ReferenciaID As Integer
        Public Property Usuario As String
        Public Property Observacion As String
    End Class

End Namespace
