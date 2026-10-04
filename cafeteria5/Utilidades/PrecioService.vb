Imports System

Namespace CafeteriaOS

    ''' <summary>
    ''' Servicio para calcular precios recomendados (Opcion 1: sobre costo).
    ''' </summary>
    Public Module PrecioService

        Public Function CalcularPrecioVentaRecomendado(precioCosto As Decimal) As Decimal
            Dim cfg As ConfiguracionNegocio = Datos.ConfigNegocio
            If cfg Is Nothing Then Return 0D

            Return CalcularPrecioVenta(precioCosto,
                                       cfg.MargenSobreCosto,
                                       cfg.GastosFijosPorcentaje,
                                       cfg.IVAGeneral,
                                       cfg.AplicarGastosFijos,
                                       cfg.AplicarIVA)
        End Function

        ''' <summary>
        ''' Precio de venta a partir del costo: costo mas el margen, mas los gastos
        ''' fijos y mas el IVA, cada uno como porcentaje del costo.
        '''
        ''' Los porcentajes van como argumentos y no se leen de la configuracion
        ''' porque la pantalla de configuracion los muestra mientras los cambia:
        ''' si el calculo leyera la configuracion viva, no habria forma de ver el
        ''' efecto de un porcentaje todavia no guardado.
        ''' </summary>
        Public Function CalcularPrecioVenta(precioCosto As Decimal,
                                            margen As Decimal,
                                            gastosFijos As Decimal,
                                            iva As Decimal,
                                            aplicarGastos As Boolean,
                                            aplicarIVA As Boolean) As Decimal

            If precioCosto <= 0D Then Return 0D

            Dim mult As Decimal = 1D + (margen / 100D)
            If aplicarGastos Then mult += (gastosFijos / 100D)
            If aplicarIVA Then mult += (iva / 100D)

            Dim recomendado = Math.Round(precioCosto * mult, 2, MidpointRounding.AwayFromZero)

            ''' Jamas un precio por debajo del costo: seria vender a perdida, y
            ''' una sugerencia asi no sirve para nada.
            '''
            Return Math.Max(recomendado, precioCosto)
        End Function

    End Module

End Namespace
