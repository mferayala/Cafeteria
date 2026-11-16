Imports System.Drawing
Imports System.Windows.Forms

Namespace CafeteriaOS

    Partial Class LoginForm
        Inherits Form

        Private lblTitulo As Label
        Private lblSubtitulo As Label
        Private lblUsuario As Label
        Private lblContrasena As Label
        Private lblRol As Label
        Private lblError As Label
        Private WithEvents chkMostrarContrasena As CheckBox
        Private WithEvents lnkOlvide As LinkLabel
        Private txtUsuario As TextBox
        Private WithEvents txtContrasena As TextBox
        Private cmbRol As ComboBox
        Private chkRecordar As CheckBox
        Private WithEvents btnIngresar As Button
        Private WithEvents btnSalir As Button

        Private Sub IniciarInterfaz()
            Text = "Sistema de gestión de ventas - Ingreso"
            StartPosition = FormStartPosition.CenterScreen
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ' Sin el bloque de "cuentas de prueba" el formulario es mas bajo.
            ClientSize = New Size(440, 500)
            ' Sin esto WinForms escala con los valores por defecto y no con los que
            ' escribe el Designer: a 125% o 150% la fuente crece y las columnas no,
            ' y los labels quedan cortados.
            AutoScaleMode = AutoScaleMode.Dpi
            AutoScaleDimensions = New SizeF(96, 96)
            BackColor = Tema.BgPrincipal
            Font = Tema.Fuente(Tema.TamNormal)
            KeyPreview = True

            Dim tarjeta As New TableLayoutPanel With {
                .Name = "th.tarjeta",
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 0,
                .GrowStyle = TableLayoutPanelGrowStyle.AddRows,
                .BackColor = Tema.Superficie,
                .Padding = New Padding(28, 26, 28, 26)
            }
            tarjeta.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))

            lblTitulo = Tema.CrearLabel("MI NEGOCIO", Tono.Acento, Tema.TamLogin, FontStyle.Bold)
            lblTitulo.Dock = DockStyle.Fill
            lblTitulo.TextAlign = ContentAlignment.MiddleCenter
            lblSubtitulo = Tema.CrearLabel("Gestion de ventas e inventario", Tono.Secundario, Tema.TamMini)
            lblSubtitulo.Dock = DockStyle.Fill
            lblSubtitulo.TextAlign = ContentAlignment.MiddleCenter

            lblUsuario = Etiqueta("Usuario o correo")
            txtUsuario = Tema.CrearInput()
            txtUsuario.Dock = DockStyle.Fill

            lblContrasena = Etiqueta("Contraseña")
            txtContrasena = Tema.CrearInput(esClave:=True)
            txtContrasena.Dock = DockStyle.Fill

            ' Debajo de la contraseña: mostrarla (a la izquierda) y recuperarla (a la
            ' derecha), en la misma fila para no alargar la ventana.
            chkMostrarContrasena = Tema.CrearCheck("Mostrar contraseña")
            chkMostrarContrasena.Anchor = AnchorStyles.Left
            chkMostrarContrasena.TabStop = False

            lnkOlvide = New LinkLabel With {
                .Name = "th.tono.Acento",
                .Text = "¿Olvidaste tu contraseña?",
                .Font = Tema.Fuente(Tema.TamMini),
                .AutoSize = True,
                .Anchor = AnchorStyles.Right,
                .BackColor = Color.Transparent,
                .LinkBehavior = LinkBehavior.HoverUnderline,
                .LinkColor = Tema.Acento2,
                .ActiveLinkColor = Tema.Acento,
                .Margin = New Padding(0)
            }

            Dim filaContrasena As New TableLayoutPanel With {
                .Dock = DockStyle.Fill,
                .Height = 24,
                .ColumnCount = 2,
                .RowCount = 1,
                .BackColor = Color.Transparent,
                .Margin = New Padding(0)
            }
            filaContrasena.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
            filaContrasena.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
            filaContrasena.Controls.Add(chkMostrarContrasena, 0, 0)
            filaContrasena.Controls.Add(lnkOlvide, 1, 0)

            lblRol = Etiqueta("Rol")
            cmbRol = Tema.CrearCombo()
            cmbRol.Dock = DockStyle.Fill

            chkRecordar = Tema.CrearCheck("Recordar este ingreso")

            lblError = Tema.CrearLabel("", Tono.Peligro, Tema.TamMini, FontStyle.Bold)
            lblError.Dock = DockStyle.Fill
            lblError.Height = 20
            lblError.TextAlign = ContentAlignment.MiddleLeft

            btnIngresar = Tema.CrearBoton("Ingresar", EstiloBoton.Primario)
            btnIngresar.Dock = DockStyle.Fill
            btnIngresar.Height = 44

            btnSalir = Tema.CrearBoton("Salir", EstiloBoton.Sutil)
            btnSalir.Dock = DockStyle.Fill
            btnSalir.Height = 36

            tarjeta.Controls.AddRange(New Control() {
                lblTitulo, lblSubtitulo, Separador(10),
                lblUsuario, txtUsuario, Separador(8),
                lblContrasena, txtContrasena, Separador(4), filaContrasena, Separador(6),
                lblRol, cmbRol, Separador(4),
                chkRecordar, Separador(2), lblError, Separador(10),
                btnIngresar, Separador(8), btnSalir
            })

            Controls.Add(tarjeta)
            AcceptButton = btnIngresar
            CancelButton = btnSalir
        End Sub

        Private Function Etiqueta(texto As String) As Label
            Dim l As Label = Tema.CrearLabel(texto, Tono.Secundario, Tema.TamMini, FontStyle.Bold)
            l.Dock = DockStyle.Fill
            l.Height = 18
            l.TextAlign = ContentAlignment.MiddleLeft
            Return l
        End Function

        Private Function Separador(alto As Integer) As Control
            Return New Panel With {
                .Name = "th.tono.Secundario",
                .Dock = DockStyle.Fill,
                .Height = alto,
                .BackColor = Color.Transparent
            }
        End Function
    End Class

End Namespace