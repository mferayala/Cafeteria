Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms

Namespace CafeteriaOS

    ''' <summary>
    ''' Eleccion de especificaciones de un producto (por ejemplo "Leche: Entera").
    ''' Devuelve el texto ya resuelto, que es lo que se guarda en el pedido y se imprime
    ''' en la cocina. Si el producto no tiene especificaciones, no llega a abrirse.
    ''' </summary>
    Partial Class EspecificacionesForm
        Inherits Form

        Private ProductoActual As Producto
        Private resultado As String

        Private WithEvents btnAgregar As Button
        Private WithEvents btnCancelar As Button

        Public Sub New(producto As Producto)
            ProductoActual = producto
            Text = "Especificaciones - " & producto.Nombre
            StartPosition = FormStartPosition.CenterParent
            FormBorderStyle = FormBorderStyle.Sizable
            MinimizeBox = False
            ClientSize = New Size(380, 120 + 62 * producto.Specs.Count)
            ' Sin esto WinForms escala con los valores por defecto y no con los que
            ' escribe el Designer: a 125% o 150% la fuente crece y las columnas no,
            ' y los labels quedan cortados.
            AutoScaleMode = AutoScaleMode.Dpi
            AutoScaleDimensions = New SizeF(96, 96)
            BackColor = Tema.BgPrincipal
            Font = Tema.Fuente(Tema.TamNormal)
            MinimumSize = Size
            IniciarInterfaz()
        End Sub

        ''' <summary>Devuelve el texto elegido, o Nothing si el usuario cancelo.</summary>
        Public Shared Function Pedir(producto As Producto) As String
            If producto Is Nothing OrElse producto.Specs.Count = 0 Then Return String.Empty
            Using f As New EspecificacionesForm(producto)
                Return If(f.ShowDialog() = DialogResult.OK, f.resultado, Nothing)
            End Using
        End Function

        Private Sub btnAgregar_Click(sender As Object, e As EventArgs) Handles btnAgregar.Click
            Aceptar()
        End Sub

        Private Sub btnCancelar_Click(sender As Object, e As EventArgs) Handles btnCancelar.Click
            DialogResult = DialogResult.Cancel
            Close()
        End Sub

        Private Sub Aceptar()
            Dim partes As New List(Of String)()
            For Each grupo In ProductoActual.Specs
                Dim elegido = controles(grupo.Etiqueta).SelectedItem?.ToString()
                If Not String.IsNullOrWhiteSpace(elegido) Then
                    partes.Add(grupo.Etiqueta & ": " & elegido)
                End If
            Next
            resultado = String.Join(" | ", partes)
            DialogResult = DialogResult.OK
            Close()
        End Sub
    End Class

End Namespace