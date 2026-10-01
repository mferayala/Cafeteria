Imports System.Drawing
Imports System.Windows.Forms

Namespace CafeteriaOS

    Partial Class EspecificacionesForm
        Inherits Form

        Private controles As New Dictionary(Of String, ComboBox)()

        Private Sub IniciarInterfaz()
            Dim cuerpo As New TableLayoutPanel With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 2,
                .RowCount = 0,
                .GrowStyle = TableLayoutPanelGrowStyle.AddRows,
                .BackColor = Tema.Superficie,
                .Padding = New Padding(18)
            }
            cuerpo.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
            cuerpo.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))

            For Each grupo In ProductoActual.Specs
                Dim lbl As Label = Tema.CrearLabel(grupo.Etiqueta, Tono.Secundario, Tema.TamNormal)
                lbl.Dock = DockStyle.Fill
                lbl.TextAlign = ContentAlignment.MiddleLeft
                lbl.Margin = New Padding(0, 0, 12, 8)

                Dim cmb As ComboBox = Tema.CrearCombo()
                cmb.Dock = DockStyle.Fill
                cmb.Items.AddRange(grupo.Opciones)
                If cmb.Items.Count > 0 Then cmb.SelectedIndex = 0
                cmb.Margin = New Padding(0, 0, 0, 8)

                controles(grupo.Etiqueta) = cmb
                cuerpo.Controls.Add(lbl)
                cuerpo.Controls.Add(cmb)
            Next

            btnAgregar = Tema.CrearBoton("Agregar", EstiloBoton.Primario, 120, 36)
            btnCancelar = Tema.CrearBoton("Cancelar", EstiloBoton.Sutil, 120, 36)

            Dim raiz As New TableLayoutPanel With {
                .Name = "th.fondo",
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 2,
                .BackColor = Tema.BgPrincipal
            }
            raiz.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            raiz.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            raiz.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            raiz.Controls.Add(cuerpo, 0, 0)
            raiz.Controls.Add(UiKit.Botonera(btnAgregar, btnCancelar), 0, 1)

            Controls.Add(raiz)
            AcceptButton = btnAgregar
            CancelButton = btnCancelar
        End Sub
    End Class

End Namespace