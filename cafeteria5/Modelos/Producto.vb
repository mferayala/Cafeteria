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
        Public Property CodigoBarra As String
        Public Property Marca As String
        Public Property UnidadMedida As String
        Public Property PrecioCosto As Decimal
        Public Property Precio As Decimal
        Public Property Stock As Integer
        Public Property Categoria As String
        Public Property Subcategoria As String
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

        ''' <summary>
        ''' Como se muestra el producto al cliente: marca, nombre y descripcion pegados.
        '''
        ''' Son tres datos separados porque el mismo nombre se repite entre marcas
        ''' distintas ("Jugo en sobre" de TANG y de otra marca), pero al mostrarlos
        ''' juntos el cliente tiene que ver cual compro. Las partes que ya estarian
        ''' repetidas se omiten, para no terminar con "Cafe con leche Cafe con leche".
        ''' </summary>
        Public Function NombreCompleto() As String
            Dim texto As New System.Text.StringBuilder()
            For Each parte In New String() {Marca, Nombre, Descripcion}
                Dim limpia = If(parte, "").Trim()
                If limpia.Length = 0 Then Continue For
                If texto.ToString.IndexOf(limpia, StringComparison.CurrentCultureIgnoreCase) >= 0 Then
                    Continue For
                End If
                If texto.Length > 0 Then texto.Append(" ")
                texto.Append(limpia)
            Next
            Return If(texto.Length = 0, "", texto.ToString())
        End Function
    End Class

End Namespace