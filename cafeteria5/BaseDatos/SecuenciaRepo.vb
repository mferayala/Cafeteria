Namespace CafeteriaOS

    ''' <summary>
    ''' Numeracion compartida entre todas las PCs.
    '''
    ''' Ventas, pedidos, movimientos de stock e ingresos necesitan su numero ANTES de
    ''' guardarse (la venta se arma, se cobra y recien despues se confirma, y el ticket
    ''' y los movimientos ya usan ese numero). Antes cada PC llevaba su propio contador
    ''' desde que se abria el programa: con dos cajas abiertas, las dos daban el mismo.
    '''
    ''' La tabla secuencias guarda el ultimo numero entregado de cada cosa. El UPDATE con
    ''' LAST_INSERT_ID(valor + 1) suma y devuelve en un solo paso atomico: MySQL bloquea
    ''' la fila mientras tanto, asi que dos PCs pidiendo a la vez reciben numeros
    ''' distintos. Si un numero se pide y la venta se descarta, queda un hueco: es
    ''' preferible a repetir un numero.
    ''' </summary>
    Public NotInheritable Class SecuenciaRepo

        Public Const Ventas As String = "ventas"
        Public Const Pedidos As String = "pedidos"
        Public Const MovimientosStock As String = "movimientos_stock"
        Public Const IngresosStock As String = "ingresos_stock"

        Private Sub New()
        End Sub

        Public Shared Function Siguiente(nombre As String) As Integer
            Using cx = Db.Abrir()
                Dim filas = Db.Ejecutar(cx, Nothing,
                    "UPDATE secuencias SET valor = LAST_INSERT_ID(valor + 1) WHERE nombre = @n",
                    "@n", nombre)
                If filas <> 1 Then
                    Throw New InvalidOperationException(
                        "Falta la secuencia '" & nombre & "' en la tabla secuencias. " &
                        "Ejecuta actualizacion_login_imagenes.sql en phpMyAdmin.")
                End If
                ' LAST_INSERT_ID es por conexion: devuelve el valor que acaba de poner
                ' ESTE UPDATE, aunque otra PC haya pedido otro numero en el medio.
                Return Convert.ToInt32(Db.Escalar(cx, Nothing, "SELECT LAST_INSERT_ID()"))
            End Using
        End Function

    End Class

End Namespace
