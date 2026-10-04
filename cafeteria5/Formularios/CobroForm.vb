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
        Private resultado As ResultadoCobro
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

        Public Shared Function Pedir(total As Decimal) As ResultadoCobro
            Using f As New CobroForm(total)
                If f.ShowDialog() <> DialogResult.OK OrElse Not f.cobrado Then
                    Return Nothing
                End If
                Return f.resultado
            End Using
        End Function

        Private Sub btnCobrar_Click(sender As Object, e As EventArgs) Handles btnCobrar.Click
            ' A cuenta se resuelve antes de tocar el medio de pago: no hay medio de pago
            ' que elegir ni efectivo que verificar, porque no se cobró nada.
            If chkACuenta.Checked Then
                resultado = New ResultadoCobro With {
                    .MetodoPago = MetodoPago.Efectivo,
                    .Total = total,
                    .EfectivoRecibido = 0D,
                    .CobradoAhora = False
                }
                cobrado = True
                DialogResult = DialogResult.OK
                Close()
                Return
            End If

            Dim medio = DirectCast(cmbPago.SelectedItem, String)
            Dim esEfectivo = medio = MetodoPago.Efectivo.ToString()
            Dim entregado As Decimal

            If esEfectivo Then
                If Not Decimal.TryParse(txtEntregado.Text, entregado) OrElse entregado < total Then
                    MessageBox.Show("El importe entregado no alcanza para cubrir el total.",
                                    "Cobro", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    txtEntregado.SelectAll()
                    txtEntregado.Focus()
                    Return
                End If
            End If

            ' El efectivo se lleva puesto con el medio de pago: si no queda en el
            ' resultado, la venta no puede saber cuanto entro ni cuanto se devolvio.
            resultado = New ResultadoCobro With {
                .MetodoPago = DirectCast([Enum].Parse(GetType(MetodoPago), medio), MetodoPago),
                .Total = total,
                .EfectivoRecibido = If(esEfectivo, entregado, 0D),
                .CobradoAhora = True
            }
            cobrado = True
            DialogResult = DialogResult.OK
            Close()
        End Sub

        Private Sub btnCancelar_Click(sender As Object, e As EventArgs) Handles btnCancelar.Click
            DialogResult = DialogResult.Cancel
            Close()
        End Sub

        ''' <summary>
        ''' Dejar a cuenta no es cobrar de otra forma: es NO cobrar. El medio de pago y
        ''' el efectivo entregado se apagan porque no aplican, y el total pasa a verse
        ''' como pendiente para que quede claro que la plata todavia no entró.
        '''
        ''' Un cliente que paga y se va nunca tilda esta casilla: para eso esta el cobro
        ''' normal, que no deja deuda.
        ''' </summary>
        Private Sub chkACuenta_CheckedChanged()
            Dim aCuenta = chkACuenta.Checked

            cmbPago.Enabled = Not aCuenta
            txtEntregado.Enabled = Not aCuenta AndAlso
                                 DirectCast(cmbPago.SelectedItem, String) = MetodoPago.Efectivo.ToString()

            If aCuenta Then
                txtEntregado.Text = String.Empty
                lblCambio.Text = "--"
                lblCambio.ForeColor = Tema.TextoSec
                btnCobrar.Text = "Dejar a cuenta"
            Else
                btnCobrar.Text = "Confirmar cobro"
                MostrarTotal()
            End If
        End Sub

        Private Sub cmbPago_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbPago.SelectedIndexChanged
            If cmbPago.SelectedItem Is Nothing Then Exit Sub

            Dim esEfectivo = DirectCast(cmbPago.SelectedItem, String) = MetodoPago.Efectivo.ToString()
            txtEntregado.Enabled = esEfectivo AndAlso Not chkACuenta.Checked
            If esEfectivo AndAlso Not chkACuenta.Checked Then
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