Namespace CafeteriaOS

    ''' <summary>
    ''' Categoria del catalogo, con sus subcategorias.
    '''
    ''' Existe como entidad y no como texto dentro del producto para que se pueda
    ''' crear una categoria antes de que tenga productos, renombrarla una sola vez
    ''' y borrarla sin dejar variantes del mismo nombre dando vueltas.
    '''
        ''' Las subcategorias se guardan como nombres dentro de la categoria: en la
        ''' practica son etiquetas cortas ("Sin gas", "Entera") que no se consultan
        ''' por separado, asi que una tabla propia seria complejidad sin uso.
    ''' </summary>
    Public Class Categoria
        Public Property ID As Integer
        Public Property Nombre As String

        Public Property Subcategorias As New System.Collections.Generic.List(Of String)

        Public Sub New()
        End Sub

        Public Sub New(nombre As String)
            Me.Nombre = nombre
        End Sub

        Public Function ConSubcategorias() As Integer
            Return Subcategorias.Count
        End Function
    End Class

End Namespace
