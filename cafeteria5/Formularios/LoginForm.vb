Imports System.Windows.Forms

Namespace CafeteriaOS

    ''' <summary>
    ''' Ventana de ingreso. Valida contra los usuarios en memoria, exige que el rol elegido
    ''' coincida con el del usuario y abre el dashboard cerrandose a si misma, de modo que
    ''' no queda un proceso huerfano detras de la ventana principal.
    ''' </summary>
    Partial Class LoginForm

        Public Sub New()
            IniciarInterfaz()
            ' El designer deja el texto de diseno; el nombre real del negocio viene
            ' de la configuracion, asi que se reemplaza despues de armar la pantalla.
            lblTitulo.Text = Datos.ConfigNegocio.NombreParaMostrar().ToUpperInvariant()
            lblPista.Text = DatosIniciales.PistasDeLogin()
        End Sub

        Private Sub LoginForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
            cmbRol.Items.AddRange(AutenticacionService.Roles())
            cmbRol.SelectedIndex = 0
            AppConfig.CargarLogin(txtUsuario, cmbRol, chkRecordar)
            If chkRecordar.Checked AndAlso txtUsuario.TextLength > 0 Then txtContrasena.Focus()
        End Sub

        Private Sub btnIngresar_Click(sender As Object, e As EventArgs) Handles btnIngresar.Click
            Ingresar()
        End Sub

        Private Sub btnSalir_Click(sender As Object, e As EventArgs) Handles btnSalir.Click
            Close()
        End Sub

        Private Sub txtContrasena_KeyDown(sender As Object, e As KeyEventArgs) Handles txtContrasena.KeyDown
            If e.KeyCode = Keys.Enter Then
                e.SuppressKeyPress = True
                Ingresar()
            End If
        End Sub

        Private Sub MostrarError(mensaje As String)
            lblError.Text = mensaje
        End Sub

        Private Sub Ingresar()
            MostrarError("")

            Dim identificador = txtUsuario.Text.Trim()
            Dim contrasena = txtContrasena.Text

            If identificador.Length = 0 Then
                MostrarError("Ingresa un usuario.")
                txtUsuario.Focus()
                Return
            End If
            If contrasena.Length = 0 Then
                MostrarError("Ingresa la contrasena.")
                txtContrasena.Focus()
                Return
            End If

            Dim usuario = AutenticacionService.Buscar(identificador, contrasena)
            If usuario Is Nothing Then
                MostrarError("Usuario o contrasena incorrectos.")
                txtContrasena.SelectAll()
                txtContrasena.Focus()
                Return
            End If

            If Not AutenticacionService.RolCoincide(usuario, cmbRol.SelectedItem?.ToString()) Then
                MostrarError("El rol seleccionado no corresponde a ese usuario.")
                cmbRol.Focus()
                Return
            End If

            If chkRecordar.Checked Then
                AppConfig.GuardarLogin(identificador, usuario.Rol)
            Else
                AppConfig.OlvidarLogin()
            End If

            ' Estado real de la sesion: el resto de la app depende de esto.
            Datos.UsuarioActual = usuario
            txtContrasena.Text = ""

            Using dashboard As New MainDashboardForm()
                Hide()
                If dashboard.ShowDialog() = DialogResult.OK Then
                    ' Se salio de la aplicacion: el login tambien cierra.
                    Close()
                Else
                    ' Se cerro sesion: se vuelve al login para entrar con otro usuario.
                    Show()
                    MostrarError("Sesion cerrada.")
                    txtContrasena.Text = String.Empty
                    txtUsuario.SelectAll()
                    txtUsuario.Focus()
                End If
            End Using
        End Sub
    End Class

End Namespace