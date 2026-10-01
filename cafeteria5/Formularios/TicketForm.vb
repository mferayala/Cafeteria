Imports System.Windows.Forms

Namespace CafeteriaOS

    ''' <summary>
    ''' Muestra el ticket generado y ofrece imprimirlo o guardarlo como texto.
    ''' Reemplaza la vista previa que antes imprimia una hoja en blanco.
    ''' </summary>
    Partial Class TicketForm
        Inherits Form

        Private contenido As String
        Private nombreSugerido As String

        Private txtTicket As TextBox
        Private WithEvents btnImprimir As Button
        Private WithEvents btnGuardar As Button
        Private WithEvents btnCerrar As Button

        Public Sub New(contenido As String, Optional titulo As String = "Ticket")
            Me.contenido = contenido
            Me.nombreSugerido = "ticket_" & DateTime.Now.ToString("yyyyMMdd_HHmmss") & ".txt"
            Text = titulo
            StartPosition = FormStartPosition.CenterParent
            IniciarInterfaz()
            txtTicket.Text = contenido
        End Sub

        Private Sub btnImprimir_Click(sender As Object, e As EventArgs) Handles btnImprimir.Click
            TicketService.Imprimir(contenido)
        End Sub

        Private Sub btnGuardar_Click(sender As Object, e As EventArgs) Handles btnGuardar.Click
            TicketService.GuardarArchivo(contenido, nombreSugerido)
        End Sub

        Private Sub btnCerrar_Click(sender As Object, e As EventArgs) Handles btnCerrar.Click
            Close()
        End Sub
    End Class

End Namespace