Namespace CafeteriaOS

    ''' <summary>
    ''' Como termino un cobro: el medio de pago y, si fue en efectivo, cuanto entrego el
    ''' cliente.
    '''
    ''' Antes el cobro devolvia solo el medio de pago y el efectivo entregado se perdia:
    ''' el vuelto se calculaba y se mostraba, y despues se descartaba. Sin ese dato una
    ''' venta no puede decir cuanto efectivo entro ni cuanto se devolvio, que es
    ''' justamente lo que hace falta para que el cierre de caja cuadre.
    ''' </summary>
    Public Class ResultadoCobro
        Public Property MetodoPago As MetodoPago
        Public Property Total As Decimal
        Public Property EfectivoRecibido As Decimal

        ''' <summary>Lo que se devuelve al cliente. Siempre cero si no fue en efectivo.</summary>
        Public ReadOnly Property Cambio As Decimal
            Get
                If MetodoPago <> MetodoPago.Efectivo Then Return 0D
                Return Math.Max(0D, EfectivoRecibido - Total)
            End Get
        End Property

        Public ReadOnly Property EsEfectivo As Boolean
            Get
                Return MetodoPago = MetodoPago.Efectivo
            End Get
        End Property
    End Class

End Namespace
