Imports System.Drawing
Imports System.Windows.Forms

Namespace CafeteriaOS

    ''' <summary>
    ''' Aparece al arrancar si no se pudo conectar con la base. Permite escribir donde
    ''' esta (IP de la PC de XAMPP, usuario y contraseña de MySQL), probarlo y guardarlo
    ''' en conexion.ini de esta PC, sin tocar el codigo ni recompilar.
    ''' </summary>
    Public Class ConexionForm
        Inherits Form

        Private ReadOnly config As ConfiguracionConexion

        Private txtServidor As TextBox
        Private numPuerto As NumericUpDown
        Private txtBase As TextBox
        Private txtUsuario As TextBox
        Private txtContrasena As TextBox
        Private WithEvents chkMostrar As CheckBox
        Private lblMensaje As Label
        Private WithEvents btnProbar As Button
        Private WithEvents btnSalir As Button

        Public Sub New(configActual As ConfiguracionConexion, errorInicial As String)
            config = configActual
            ArmarInterfaz()
            txtServidor.Text = config.Servidor
            numPuerto.Value = Math.Max(1, Math.Min(65535, config.Puerto))
            txtBase.Text = config.Base
            txtUsuario.Text = config.Usuario
            txtContrasena.Text = config.Contrasena
            MostrarError(errorInicial)
        End Sub

        Private Sub ArmarInterfaz()
            Text = "Conexión con la base de datos"
            StartPosition = FormStartPosition.CenterScreen
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            AutoScaleMode = AutoScaleMode.Dpi
            AutoScaleDimensions = New SizeF(96, 96)
            ClientSize = New Size(520, 560)
            BackColor = Tema.BgPrincipal
            Font = Tema.Fuente(Tema.TamNormal)

            Dim raiz As New TableLayoutPanel With {
                .Name = "th.tarjeta",
                .Dock = DockStyle.Fill,
                .ColumnCount = 2,
                .RowCount = 0,
                .GrowStyle = TableLayoutPanelGrowStyle.AddRows,
                .BackColor = Tema.Superficie,
                .Padding = New Padding(24, 20, 24, 20)
            }
            raiz.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 130))
            raiz.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))

            Dim titulo = Tema.CrearLabel("No se pudo conectar con la base de datos", Tono.Peligro,
                                         Tema.TamSubtitulo, FontStyle.Bold)
            Fila(raiz, titulo, Nothing, 36)

            Dim ayuda = Tema.CrearLabel(
                "En la PC que tiene XAMPP, el servidor es localhost. En las demás PCs, escribí la " &
                "IP de la PC de XAMPP (en esa PC: tecla Windows, escribí cmd, y ejecutá ipconfig; " &
                "es la ""Dirección IPv4"", por ejemplo 192.168.0.10).",
                Tono.Secundario, Tema.TamMini)
            ayuda.AutoSize = False
            Fila(raiz, ayuda, Nothing, 64)

            txtServidor = Tema.CrearInput()
            numPuerto = New NumericUpDown With {.Minimum = 1, .Maximum = 65535, .Dock = DockStyle.Fill}
            txtBase = Tema.CrearInput()
            txtUsuario = Tema.CrearInput()
            txtContrasena = Tema.CrearInput(esClave:=True)
            chkMostrar = Tema.CrearCheck("Mostrar contraseña")

            Fila(raiz, Etiqueta("Servidor (IP)"), txtServidor, 36)
            Fila(raiz, Etiqueta("Puerto"), numPuerto, 36)
            Fila(raiz, Etiqueta("Base de datos"), txtBase, 36)
            Fila(raiz, Etiqueta("Usuario MySQL"), txtUsuario, 36)
            Fila(raiz, Etiqueta("Contraseña"), txtContrasena, 36)
            Fila(raiz, New Label With {.Text = ""}, chkMostrar, 28)

            lblMensaje = Tema.CrearLabel("", Tono.Peligro, Tema.TamMini)
            lblMensaje.AutoSize = False
            Fila(raiz, lblMensaje, Nothing, 120)

            btnProbar = Tema.CrearBoton("Probar y guardar", EstiloBoton.Primario, 180, 40)
            btnSalir = Tema.CrearBoton("Salir", EstiloBoton.Sutil, 110, 40)
            btnSalir.Margin = New Padding(8, 0, 0, 0)
            Dim botones As New FlowLayoutPanel With {
                .Dock = DockStyle.Fill,
                .FlowDirection = FlowDirection.RightToLeft,
                .BackColor = Color.Transparent
            }
            botones.Controls.Add(btnSalir)
            botones.Controls.Add(btnProbar)
            Fila(raiz, botones, Nothing, 48)

            Controls.Add(raiz)
            AcceptButton = btnProbar
            CancelButton = btnSalir
        End Sub

        ''' <summary>Una fila: etiqueta y campo, o un solo control a todo el ancho.</summary>
        Private Shared Sub Fila(raiz As TableLayoutPanel, izquierda As Control, derecha As Control, alto As Integer)
            raiz.RowStyles.Add(New RowStyle(SizeType.Absolute, alto))
            Dim fila = raiz.RowCount
            raiz.RowCount += 1
            izquierda.Dock = DockStyle.Fill
            raiz.Controls.Add(izquierda, 0, fila)
            If derecha Is Nothing Then
                raiz.SetColumnSpan(izquierda, 2)
            Else
                derecha.Dock = DockStyle.Fill
                raiz.Controls.Add(derecha, 1, fila)
            End If
        End Sub

        Private Shared Function Etiqueta(texto As String) As Label
            Dim l = Tema.CrearLabel(texto, Tono.Secundario, Tema.TamMini, FontStyle.Bold)
            l.AutoSize = False
            l.TextAlign = ContentAlignment.MiddleLeft
            Return l
        End Function

        Private Sub MostrarError(mensaje As String)
            If String.IsNullOrWhiteSpace(mensaje) Then
                lblMensaje.Text = ""
                Return
            End If
            lblMensaje.ForeColor = Tema.ColorDeTono(Tono.Peligro)
            lblMensaje.Text = ConfiguracionConexion.Explicar(mensaje)
        End Sub

        Private Sub chkMostrar_CheckedChanged(sender As Object, e As EventArgs) Handles chkMostrar.CheckedChanged
            txtContrasena.UseSystemPasswordChar = Not chkMostrar.Checked
        End Sub

        Private Sub btnProbar_Click(sender As Object, e As EventArgs) Handles btnProbar.Click
            config.Servidor = txtServidor.Text.Trim()
            config.Puerto = CInt(numPuerto.Value)
            config.Base = txtBase.Text.Trim()
            config.Usuario = txtUsuario.Text.Trim()
            config.Contrasena = txtContrasena.Text

            lblMensaje.ForeColor = Tema.ColorDeTono(Tono.Secundario)
            lblMensaje.Text = "Probando la conexión..."
            btnProbar.Enabled = False
            UseWaitCursor = True
            Application.DoEvents()

            Dim mensaje As String = Nothing
            Db.CadenaConexion = config.CadenaConexion()
            Dim ok = Db.Probar(mensaje)

            UseWaitCursor = False
            btnProbar.Enabled = True
            If Not ok Then
                MostrarError(mensaje)
                Return
            End If

            Try
                config.Guardar()
            Catch ex As Exception
                MessageBox.Show("La conexión funciona, pero no se pudo guardar en " &
                                ConfiguracionConexion.Ruta & ": " & ex.Message & vbCrLf &
                                "Vas a tener que escribirla de nuevo la próxima vez.",
                                "Conexión", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End Try
            DialogResult = DialogResult.OK
            Close()
        End Sub

        Private Sub btnSalir_Click(sender As Object, e As EventArgs) Handles btnSalir.Click
            DialogResult = DialogResult.Cancel
            Close()
        End Sub

    End Class

End Namespace
