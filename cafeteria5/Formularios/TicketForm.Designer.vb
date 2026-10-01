Imports System.Drawing
Imports System.Windows.Forms

Namespace CafeteriaOS

    Partial Class TicketForm
        Inherits Form

        Private Sub IniciarInterfaz()
            Text = "Ticket"
            StartPosition = FormStartPosition.CenterParent
            FormBorderStyle = FormBorderStyle.Sizable
            MinimizeBox = False
            ClientSize = New Size(460, 620)
            BackColor = Tema.BgPrincipal
            Font = Tema.Fuente(Tema.TamNormal)

            txtTicket = New TextBox With {
                .Name = "th.campo",
                .Dock = DockStyle.Fill,
                .Multiline = True,
                .ReadOnly = True,
                .ScrollBars = ScrollBars.Vertical,
                .BackColor = Tema.Superficie,
                .ForeColor = Tema.TextoPrinc,
                .BorderStyle = BorderStyle.None,
                .Font = Tema.FuenteMonoespaciada(),
                .WordWrap = False
            }

            btnImprimir = Tema.CrearBoton("Imprimir", EstiloBoton.Primario, 130, 38)
            btnGuardar = Tema.CrearBoton("Guardar .txt", EstiloBoton.Neutro, 130, 38)
            btnCerrar = Tema.CrearBoton("Cerrar", EstiloBoton.Sutil, 130, 38)

            Dim cuerpo As New TableLayoutPanel With {
                .Name = "th.fondo",
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 2,
                .BackColor = Tema.BgPrincipal,
                .Padding = New Padding(14)
            }
            cuerpo.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            cuerpo.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            cuerpo.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            cuerpo.Controls.Add(txtTicket, 0, 0)
            cuerpo.Controls.Add(UiKit.Botonera(btnImprimir, btnGuardar, btnCerrar), 0, 1)

            Controls.Add(cuerpo)
        End Sub
    End Class

End Namespace