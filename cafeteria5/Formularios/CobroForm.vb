Imports System.Drawing
Imports System.Windows.Forms

Namespace CafeteriaOS

    ''' <summary>
    ''' Paso de cobro del punto de venta. Solo aplica a efectivo: con tarjeta, transferencia
    ''' o Mercado Pago el vuelto no existe y el campo queda deshabilitado.
    ''' </summary>
    Partial Class CobroForm
        Inherits Form

        Private total As Decimal
        Private resultado As MetodoPago
        Private cobrado As Boolean

        Private WithEvents btnCobrar As Button
        Private WithEvents btnCancelar As Button

        Public Sub New(total As Decimal)
            Me.total = total
            Text = "Cobrar pedido"
            StartPosition = FormStartPosition.CenterParent
            FormBorderStyle = FormBorderStyle.FixedDialog
            MinimizeBox = False
            MaximizeBox = False
            ClientSize = New Size(400, 330)
            BackColor = Tema.BgPrincipal
            Font = Tema.Fuente(Tema.TamNormal)
            KeyPreview = True
            IniciarInterfaz()
            MostrarTotal()
        End Sub

        Public Shared Function Pedir(total As Decimal) As MetodoPago
            Using f As New CobroForm(total)
                If f.ShowDialog() <> DialogResult.OK OrElse Not f.cobrado Then
                    Return Nothing
                End If
                Return f.resultado
            End Using
        End Function

        Private Sub btnCobrar_Click(sender As Object, e As EventArgs) Handles btnCobrar.Click
            Dim medio = DirectCast(cmbPago.SelectedItem, String)

            If medio = MetodoPago.Efectivo.ToString() Then
                Dim entregado As Decimal
                If Not Decimal.TryParse(txtEntregado.Text, entregado) OrElse entregado < total Then
                    MessageBox.Show("El importe entregado no alcanza para cubrir el total.",
                                    "Cobro", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    txtEntregado.SelectAll()
                    txtEntregado.Focus()
                    Return
                End If
            End If

            resultado = DirectCast([Enum].Parse(GetType(MetodoPago), medio), MetodoPago)
            cobrado = True
            DialogResult = DialogResult.OK
            Close()
        End Sub

        Private Sub btnCancelar_Click(sender As Object, e As EventArgs) Handles btnCancelar.Click
            DialogResult = DialogResult.Cancel
            Close()
        End Sub

        Private Sub cmbPago_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbPago.SelectedIndexChanged
            Dim esEfectivo = DirectCast(cmbPago.SelectedItem, String) = MetodoPago.Efectivo.ToString()
            txtEntregado.Enabled = esEfectivo
            If esEfectivo Then
                txtEntregado.SelectAll()
                txtEntregado.Focus()
            Else
                txtEntregado.Text = String.Empty
            End If
            MostrarTotal()
        End Sub

        Private Sub txtEntregado_TextChanged(sender As Object, e As EventArgs) Handles txtEntregado.TextChanged
            MostrarTotal()
        End Sub

        Private Sub MostrarTotal()
            lblTotal.Text = total.ToString("C2")
            Dim entregado As Decimal
            If Decimal.TryParse(txtEntregado.Text, entregado) AndAlso entregado >= total Then
                lblCambio.Text = (entregado - total).ToString("C2")
                lblCambio.ForeColor = Tema.Verde
            Else
                lblCambio.Text = "--"
                lblCambio.ForeColor = Tema.TextoSec
            End If
        End Sub

    End Class

End Namespace