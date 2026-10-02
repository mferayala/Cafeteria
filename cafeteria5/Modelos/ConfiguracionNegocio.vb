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

        Public Sub New()
            MargenSobreCosto = 30D
            GastosFijosPorcentaje = 10D
            IVAGeneral = 21D
            NombreNegocio = "CafeteriaOS"
        End Sub
    End Class

End Namespace
