Imports System.Collections.Generic

Namespace CafeteriaOS

    ''' <summary>Grupo de opciones de una especificacion (por ejemplo "Leche").</summary>
    Public Class EspecificacionGrupo
        Public Property Etiqueta As String
        Public Property Opciones As String()
    End Class

    Public Class Producto
        Public Property ID As Integer
        Public Property Nombre As String
        Public Property Precio As Decimal
        Public Property Stock As Integer
        Public Property Categoria As String
        Public Property Descripcion As String

        ''' <summary>
        ''' Unica fuente de verdad de los atajos de teclado del POS, en formato "Ctrl+N".
        ''' Vacio cuando el producto no tiene atajo. La etiqueta y el manejo del teclado
        ''' leen este mismo valor, de modo que nunca pueden discrepar.
        ''' </summary>
        Public Property Atajo As String

        Public Property Specs As New List(Of EspecificacionGrupo)()

        Public Function TienePrecio() As Boolean
            Return Precio > 0D
        End Function
    End Class

End Namespace