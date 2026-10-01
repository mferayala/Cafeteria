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
        Private lblPista As Label
        Private txtUsuario As TextBox
        Private WithEvents txtContrasena As TextBox
        Private cmbRol As ComboBox
        Private chkRecordar As CheckBox
        Private WithEvents btnIngresar As Button
        Private WithEvents btnSalir As Button

        Private Sub IniciarInterfaz()
            Text = "Cafeteria - Ingreso"
            StartPosition = FormStartPosition.CenterScreen
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ClientSize = New Size(440, 520)
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

            lblTitulo = Tema.CrearLabel("CAFETERIA", Tono.Acento, Tema.TamLogin, FontStyle.Bold)
            lblTitulo.Dock = DockStyle.Fill
            lblTitulo.TextAlign = ContentAlignment.MiddleCenter
            lblSubtitulo = Tema.CrearLabel("Sistema de pedidos y gestion", Tono.Secundario, Tema.TamMini)
            lblSubtitulo.Dock = DockStyle.Fill
            lblSubtitulo.TextAlign = ContentAlignment.MiddleCenter

            lblUsuario = Etiqueta("Usuario o correo")
            txtUsuario = Tema.CrearInput()
            txtUsuario.Dock = DockStyle.Fill

            lblContrasena = Etiqueta("Contrasena")
            txtContrasena = Tema.CrearInput(esClave:=True)
            txtContrasena.Dock = DockStyle.Fill

            lblRol = Etiqueta("Rol")
            cmbRol = Tema.CrearCombo()
            cmbRol.Dock = DockStyle.Fill

            chkRecordar = Tema.CrearCheck("Recordar este ingreso")

            lblError = Tema.CrearLabel("", Tono.Peligro, Tema.TamMini, FontStyle.Bold)
            lblError.Dock = DockStyle.Fill
            lblError.Height = 20
            lblError.TextAlign = ContentAlignment.MiddleLeft

            ' El texto real lo completa LoginForm con DatosIniciales.PistasDeLogin.
            lblPista = Tema.CrearLabel(String.Empty, Tono.Terciario, Tema.TamMicro)
            lblPista.Dock = DockStyle.Fill
            lblPista.Height = 62
            lblPista.TextAlign = ContentAlignment.TopLeft

            btnIngresar = Tema.CrearBoton("Ingresar", EstiloBoton.Primario)
            btnIngresar.Dock = DockStyle.Fill
            btnIngresar.Height = 44

            btnSalir = Tema.CrearBoton("Salir", EstiloBoton.Sutil)
            btnSalir.Dock = DockStyle.Fill
            btnSalir.Height = 36

            tarjeta.Controls.AddRange(New Control() {
                lblTitulo, lblSubtitulo, Separador(10),
                lblUsuario, txtUsuario, Separador(8),
                lblContrasena, txtContrasena, Separador(8),
                lblRol, cmbRol, Separador(4),
                chkRecordar, Separador(2), lblError, Separador(2),
                lblPista, Separador(10),
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