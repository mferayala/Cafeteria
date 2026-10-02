Imports System

Namespace CafeteriaOS

    ''' <summary>
    ''' Servicio para calcular precios recomendados (Opcion 1: sobre costo).
    ''' </summary>
    Public Module PrecioService

        Public Function CalcularPrecioVentaRecomendado(precioCosto As Decimal) As Decimal
            If precioCosto <= 0D Then
                Return 0D
            End If
            Dim cfg As ConfiguracionNegocio = Datos.ConfigNegocio
            Dim mult As Decimal = 1D
            mult = 1D + (cfg.MargenSobreCosto / 100D)
            If cfg.AplicarGastosFijos Then
                mult += (cfg.GastosFijosPorcentaje / 100D)
            End If
            If cfg.AplicarIVA Then
                mult += (cfg.IVAGeneral / 100D)
            End If
            Dim recomendado = Math.Round(precioCosto * mult, 2, MidpointRounding.AwayFromZero)
            If recomendado < precioCosto Then
                recomendado = precioCosto
            End If
            Return recomendado
        End Function

    End Module

End Namespace
