Imports System

Namespace CafeteriaOS

    ''' <summary>
    ''' Configuracion global del negocio. Solo accesible para administradores.
    ''' </summary>
    Public Class ConfiguracionNegocio
        Public Property MargenSobreCosto As Decimal
        Public Property GastosFijosPorcentaje As Decimal
        Public Property IVAGeneral As Decimal
        Public Property AplicarGastosFijos As Boolean = True
        Public Property AplicarIVA As Boolean = True
        Public Property NombreNegocio As String
        Public Property CUIT As String
        Public Property Direccion As String
        Public Property Telefono As String

        ''' <summary>
        ''' Nombre tal como se muestra al cliente. Si no se cargo ninguno devuelve uno
        ''' neutro: antes de tocar la configuracion el sistema tiene que mostrar
        ''' algo, y mostrar una palabra inventada seria peor que un texto generico.
        ''' </summary>
        Public Function NombreParaMostrar() As String
            Dim cargado = If(NombreNegocio, "").Trim()
            If cargado.Length = 0 Then Return "Mi Negocio"
            Return cargado
        End Function

        Public Sub New()
            MargenSobreCosto = 30D
            GastosFijosPorcentaje = 10D
            IVAGeneral = 21D
            NombreNegocio = "CafeteriaOS"
        End Sub
    End Class

End Namespace
