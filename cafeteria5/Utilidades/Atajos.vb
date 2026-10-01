Imports System.Collections.Generic
Imports System.Windows.Forms

Namespace CafeteriaOS

    ''' <summary>
    ''' Traduce el atajo guardado en Producto.Atajo a teclas reales. No hay una segunda
    ''' tabla de atajos: si un atajo no esta escrito en el producto, ese producto no
    ''' tiene atajo, y la etiqueta que se muestra al usuario sale del mismo valor.
    ''' </summary>
    Public Module Atajos

        ''' <summary>
        ''' Devuelve la combinacion de teclas del atajo, o Keys.None si el texto no es un
        ''' atajo valido. Los modificadores se acumulan con la tecla: "Ctrl+1" es
        ''' Control Or D1, no solo Control (si se devolviera solo el modificador, todos los
        ''' atajos numericos colisionarian en la misma tecla del mapa).
        ''' </summary>
        Public Function TeclaDe(atajo As String) As Keys
            If String.IsNullOrWhiteSpace(atajo) Then Return Keys.None
            Dim resultado As Keys = Keys.None
            For Each parte In atajo.Split("+"c)
                Select Case parte.Trim().ToUpperInvariant()
                    Case "CTRL" : resultado = resultado Or Keys.Control
                    Case "SHIFT" : resultado = resultado Or Keys.Shift
                    Case "ALT" : resultado = resultado Or Keys.Alt
                    Case "D0", "0" : resultado = resultado Or Keys.D0
                    Case "D1", "1" : resultado = resultado Or Keys.D1
                    Case "D2", "2" : resultado = resultado Or Keys.D2
                    Case "D3", "3" : resultado = resultado Or Keys.D3
                    Case "D4", "4" : resultado = resultado Or Keys.D4
                    Case "D5", "5" : resultado = resultado Or Keys.D5
                    Case "D6", "6" : resultado = resultado Or Keys.D6
                    Case "D7", "7" : resultado = resultado Or Keys.D7
                    Case "D8", "8" : resultado = resultado Or Keys.D8
                    Case "D9", "9" : resultado = resultado Or Keys.D9
                    Case "Q" : resultado = resultado Or Keys.Q
                    Case "W" : resultado = resultado Or Keys.W
                    Case "E" : resultado = resultado Or Keys.E
                    Case "R" : resultado = resultado Or Keys.R
                    Case "T" : resultado = resultado Or Keys.T
                    Case "Y" : resultado = resultado Or Keys.Y
                    Case "U" : resultado = resultado Or Keys.U
                    Case "I" : resultado = resultado Or Keys.I
                    Case "O" : resultado = resultado Or Keys.O
                    Case "P" : resultado = resultado Or Keys.P
                    Case "A" : resultado = resultado Or Keys.A
                    Case "S" : resultado = resultado Or Keys.S
                    Case "D" : resultado = resultado Or Keys.D
                    Case "F" : resultado = resultado Or Keys.F
                    Case "G" : resultado = resultado Or Keys.G
                    Case "H" : resultado = resultado Or Keys.H
                    Case "J" : resultado = resultado Or Keys.J
                    Case "K" : resultado = resultado Or Keys.K
                    Case "L" : resultado = resultado Or Keys.L
                    Case "Z" : resultado = resultado Or Keys.Z
                    Case "X" : resultado = resultado Or Keys.X
                    Case "C" : resultado = resultado Or Keys.C
                    Case "V" : resultado = resultado Or Keys.V
                    Case "B" : resultado = resultado Or Keys.B
                    Case "N" : resultado = resultado Or Keys.N
                    Case "M" : resultado = resultado Or Keys.M
                End Select
            Next
            Return resultado
        End Function

        ''' <summary>
        ''' Mapa de atajo a producto. Se arma desde el catalogo cada vez que se necesita,
        ''' por lo que agregar un atajo en Producto.Atajo alcanza para que funcione.
        ''' </summary>
        Public Function Mapa() As Dictionary(Of Keys, Producto)
            Dim resultado As New Dictionary(Of Keys, Producto)()
            For Each producto In Datos.ListaProductos
                Dim tecla = TeclaDe(producto.Atajo)
                If tecla <> Keys.None AndAlso Not resultado.ContainsKey(tecla) Then
                    resultado.Add(tecla, producto)
                End If
            Next
            Return resultado
        End Function

    End Module

End Namespace