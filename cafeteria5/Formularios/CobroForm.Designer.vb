Imports System.Drawing
Imports System.Windows.Forms

Namespace CafeteriaOS

    Partial Class CobroForm
        Inherits Form

        Private lblTotal As Label
        Private WithEvents cmbPago As ComboBox
        Private WithEvents txtEntregado As TextBox
        Private lblCambio As Label
        Private chkACuenta As CheckBox

        Private Sub IniciarInterfaz()
            Text = "Cobrar pedido"
            StartPosition = FormStartPosition.CenterParent
            FormBorderStyle = FormBorderStyle.FixedDialog
            MinimizeBox = False
            MaximizeBox = False
            ClientSize = New Size(400, 330)
            BackColor = Tema.BgPrincipal
            Font = Tema.Fuente(Tema.TamNormal)
            KeyPreview = True

            Dim cuerpo As New TableLayoutPanel With {
                .Name = "th.tarjeta",
                .Dock = DockStyle.Fill,
                .ColumnCount = 2,
                .RowCount = 0,
                .GrowStyle = TableLayoutPanelGrowStyle.AddRows,
                .BackColor = Tema.Superficie,
                .Padding = New Padding(20)
            }
            cuerpo.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
            cuerpo.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))

            lblTotal = Tema.CrearLabel("", Tono.Acento, Tema.TamNumero, FontStyle.Bold)
            lblTotal.Dock = DockStyle.Fill
            lblTotal.TextAlign = ContentAlignment.MiddleRight

            cmbPago = Tema.CrearCombo()
            cmbPago.Dock = DockStyle.Fill
            cmbPago.Items.AddRange([Enum].GetNames(GetType(MetodoPago)))

            txtEntregado = Tema.CrearInput()
            txtEntregado.Dock = DockStyle.Fill
            txtEntregado.TextAlign = HorizontalAlignment.Right

            lblCambio = Tema.CrearLabel("", Tono.Exito, Tema.TamSubtitulo, FontStyle.Bold)
            lblCambio.Dock = DockStyle.Fill
            lblCambio.TextAlign = ContentAlignment.MiddleRight

            chkACuenta = New CheckBox With {
                .Name = "chk.aCuenta",
                .Dock = DockStyle.Fill,
                .Text = "Dejar a cuenta (paga despues)",
                .AutoSize = True,
                .UseVisualStyleBackColor = True
            }
            chkACuenta.TabStop = True
            chkACuenta.ForeColor = Tema.TextoPrinc
            AddHandler chkACuenta.CheckedChanged, Sub() chkACuenta_CheckedChanged()

            cuerpo.Controls.AddRange(New Control() {
                Etiqueta("Total a cobrar"), lblTotal,
                Etiqueta("Medio de pago"), cmbPago,
                Etiqueta("Entregado"), txtEntregado,
                Etiqueta("Cambio"), lblCambio,
                New Label(), chkACuenta
            })

            btnCobrar = Tema.CrearBoton("Confirmar cobro", EstiloBoton.Exito, 150, 40)
            btnCancelar = Tema.CrearBoton("Volver", EstiloBoton.Sutil, 150, 40)

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
            raiz.Controls.Add(UiKit.Botonera(btnCobrar, btnCancelar), 0, 1)

            Controls.Add(raiz)
            AcceptButton = btnCobrar
            CancelButton = btnCancelar

            ' SelectedIndex va AL ULTIMO, y no por estetica: asignarlo dispara
            ' SelectedIndexChanged en el acto, y ese handler usa txtEntregado y
            ' lblCambio para habilitar el campo de efectivo y calcular el vuelto.
            ' Si se selecciona antes de que existan, el cobro revienta con
            ' NullReferenceException siempre, antes de que el usuario elija nada.
            cmbPago.SelectedIndex = 0
        End Sub

        Private Function Etiqueta(texto As String) As Label
            Dim l As Label = Tema.CrearLabel(texto, Tono.Secundario, Tema.TamNormal)
            l.Dock = DockStyle.Fill
            l.AutoSize = False
            l.Width = 130
            l.TextAlign = ContentAlignment.MiddleLeft
            l.Margin = New Padding(0, 4, 12, 4)
            Return l
        End Function
    End Class

End Namespace