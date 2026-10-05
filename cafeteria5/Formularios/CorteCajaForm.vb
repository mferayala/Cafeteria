Imports System.Drawing
Imports System.Linq
Imports System.Text
Imports System.Windows.Forms

Namespace CafeteriaOS

    ''' <summary>
    ''' Corte de caja: como se llego al saldo actual y como se reparte por metodo de pago.
    ''' Es de solo lectura y se puede imprimir o guardar como texto.
    ''' </summary>
    Partial Class CorteCajaForm
        Inherits Form

        Private caja As SesionCaja
        Private contenido As String

        Private WithEvents btnImprimir As Button
        Private WithEvents btnGuardar As Button
        Private WithEvents btnCerrar As Button

        Public Sub New(caja As SesionCaja)
            Me.caja = caja
            Text = "Corte de caja"
            StartPosition = FormStartPosition.CenterParent
            ClientSize = New Size(520, 660)
            ' Sin esto WinForms escala con los valores por defecto y no con los que
            ' escribe el Designer: a 125% o 150% la fuente crece y las columnas no,
            ' y los labels quedan cortados.
            AutoScaleMode = AutoScaleMode.Dpi
            AutoScaleDimensions = New SizeF(96, 96)
            BackColor = Tema.BgPrincipal
            Font = Tema.Fuente(Tema.TamNormal)
            IniciarInterfaz()
            contenido = Construir()
            txtCorte.Text = contenido
        End Sub

        Private Sub btnImprimir_Click(sender As Object, e As EventArgs) Handles btnImprimir.Click
            TicketService.Imprimir(contenido)
        End Sub

        Private Sub btnGuardar_Click(sender As Object, e As EventArgs) Handles btnGuardar.Click
            TicketService.GuardarArchivo(contenido,
                "corte_caja_" & DateTime.Now.ToString("yyyyMMdd_HHmmss") & ".txt")
        End Sub

        Private Sub btnCerrar_Click(sender As Object, e As EventArgs) Handles btnCerrar.Click
            Close()
        End Sub

        Private Function Construir() As String
            Dim s As New StringBuilder()
            s.AppendLine("CORTE DE CAJA - " & DateTime.Now.ToString("dd/MM/yyyy HH:mm"))
            s.AppendLine("========================================")
            s.AppendLine("Estado:        " & If(caja.Abierta, "ABIERTA", "CERRADA"))
            If caja.Abierta Then
                s.AppendLine("Abrio:         " & caja.UsuarioApertura & " (" &
                             caja.FechaApertura.ToString("dd/MM/yyyy HH:mm") & ")")
            End If
            s.AppendLine("Monto inicial: " & caja.MontoInicial.ToString("C2"))
            s.AppendLine("Ventas:        " & caja.TotalVentas.ToString("C2"))
            s.AppendLine("Egresos:       " & caja.TotalEgresos.ToString("C2"))
            s.AppendLine("SALDO:         " & caja.SaldoActual.ToString("C2"))
            s.AppendLine()
            s.AppendLine("POR METODO DE PAGO (hoy)")
            s.AppendLine("----------------------------------------")
            For Each kv In ReporteService.VentasPorMetodoPago().OrderByDescending(Function(k) k.Value)
                s.AppendLine(kv.Key.ToString().PadRight(16) & kv.Value.ToString("C2"))
            Next
            s.AppendLine()
            s.AppendLine("MOVIMIENTOS DEL TURNO")
            s.AppendLine("----------------------------------------")
            If caja.Movimientos.Count = 0 Then
                s.AppendLine("Sin movimientos.")
            Else
                For Each m In caja.Movimientos.OrderBy(Function(x) x.FechaHora)
                    s.AppendLine(m.FechaHora.ToString("dd/MM HH:mm") & "  " &
                                 m.Tipo.PadRight(9) & m.Monto.ToString("C2").PadLeft(12) & "  " &
                                 m.Descripcion)
                Next
            End If
            Return s.ToString()
        End Function
    End Class

End Namespace