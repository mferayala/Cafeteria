Imports System.Drawing
Imports System.Windows.Forms

Namespace CafeteriaOS

    ''' <summary>
    ''' Recuperar la contraseña en dos pasos, en la misma ventana:
    '''   1. Se escribe el usuario o correo y llega un codigo por mail.
    '''   2. Se escribe el codigo y la contraseña nueva (dos veces).
    ''' Las reglas (vencimiento, intentos, largo minimo) viven en RecuperacionService.
    ''' </summary>
    Public Class RecuperarContrasenaForm
        Inherits Form

        Private usuarioId As Integer

        Private panelPedir As TableLayoutPanel
        Private panelCambiar As TableLayoutPanel

        Private txtIdentificador As TextBox
        Private WithEvents btnEnviar As Button

        Private lblEnviado As Label
        Private txtCodigo As TextBox
        Private txtNueva As TextBox
        Private txtRepetir As TextBox
        Private WithEvents chkMostrar As CheckBox
        Private WithEvents lnkReenviar As LinkLabel
        Private WithEvents btnCambiar As Button

        Private lblMensaje As Label
        Private WithEvents btnCancelar As Button

        ''' <summary>Lo que se escribio en el paso 1, para precompletar el login.</summary>
        Public ReadOnly Property IdentificadorUsado As String
            Get
                Return txtIdentificador.Text.Trim()
            End Get
        End Property

        Public Sub New(Optional identificador As String = "")
            ArmarInterfaz()
            txtIdentificador.Text = If(identificador, String.Empty)
            MostrarPaso(1)
        End Sub

        ' ------------------------------------------------------------------
        '  Interfaz
        ' ------------------------------------------------------------------

        Private Sub ArmarInterfaz()
            Text = "Recuperar contraseña"
            StartPosition = FormStartPosition.CenterParent
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            AutoScaleMode = AutoScaleMode.Dpi
            AutoScaleDimensions = New SizeF(96, 96)
            ClientSize = New Size(420, 470)
            BackColor = Tema.BgPrincipal
            Font = Tema.Fuente(Tema.TamNormal)

            Dim raiz As New TableLayoutPanel With {
                .Name = "th.tarjeta",
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 5,
                .BackColor = Tema.Superficie,
                .Padding = New Padding(24, 20, 24, 20)
            }
            raiz.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            raiz.RowStyles.Add(New RowStyle(SizeType.AutoSize))       ' titulo
            raiz.RowStyles.Add(New RowStyle(SizeType.Percent, 100))   ' paso actual
            raiz.RowStyles.Add(New RowStyle(SizeType.Absolute, 44))   ' mensaje
            raiz.RowStyles.Add(New RowStyle(SizeType.Absolute, 40))   ' boton principal
            raiz.RowStyles.Add(New RowStyle(SizeType.Absolute, 40))   ' cancelar

            Dim titulo = Tema.CrearLabel("Recuperar contraseña", Tono.Acento, Tema.TamSubtitulo, FontStyle.Bold)
            titulo.Margin = New Padding(0, 0, 0, 10)
            raiz.Controls.Add(titulo, 0, 0)

            ' --- Paso 1 ---
            panelPedir = Columna()
            Dim explicacion = Tema.CrearLabel(
                "Escribí tu usuario, correo o teléfono. Te vamos a mandar un código " &
                "al correo registrado.", Tono.Secundario, Tema.TamMini)
            explicacion.AutoSize = False
            explicacion.Dock = DockStyle.Fill
            explicacion.Height = 40
            txtIdentificador = Tema.CrearInput()
            AgregarFilas(panelPedir, explicacion, Separador(6),
                         Etiqueta("Usuario o correo"), txtIdentificador)

            ' --- Paso 2 ---
            panelCambiar = Columna()
            lblEnviado = Tema.CrearLabel("", Tono.Exito, Tema.TamMini)
            lblEnviado.AutoSize = False
            lblEnviado.Dock = DockStyle.Fill
            lblEnviado.Height = 36

            txtCodigo = Tema.CrearInput()
            txtCodigo.MaxLength = 6
            txtNueva = Tema.CrearInput(esClave:=True)
            txtRepetir = Tema.CrearInput(esClave:=True)
            chkMostrar = Tema.CrearCheck("Mostrar contraseñas")

            lnkReenviar = New LinkLabel With {
                .Text = "Reenviar código",
                .Font = Tema.Fuente(Tema.TamMini),
                .AutoSize = True,
                .BackColor = Color.Transparent,
                .LinkBehavior = LinkBehavior.HoverUnderline,
                .LinkColor = Tema.Acento2,
                .ActiveLinkColor = Tema.Acento,
                .Margin = New Padding(0, 4, 0, 0)
            }

            AgregarFilas(panelCambiar, lblEnviado,
                         Etiqueta("Código"), txtCodigo, Separador(6),
                         Etiqueta("Contraseña nueva (mínimo " & RecuperacionService.LargoMinimoContrasena & ")"),
                         txtNueva, Separador(6),
                         Etiqueta("Repetir contraseña nueva"), txtRepetir, Separador(4),
                         chkMostrar, lnkReenviar)

            Dim contenedor As New Panel With {.Dock = DockStyle.Fill, .BackColor = Color.Transparent,
                                              .Margin = New Padding(0)}
            contenedor.Controls.Add(panelPedir)
            contenedor.Controls.Add(panelCambiar)
            raiz.Controls.Add(contenedor, 0, 1)

            lblMensaje = Tema.CrearLabel("", Tono.Peligro, Tema.TamMini, FontStyle.Bold)
            lblMensaje.AutoSize = False
            lblMensaje.Dock = DockStyle.Fill
            lblMensaje.TextAlign = ContentAlignment.MiddleLeft
            raiz.Controls.Add(lblMensaje, 0, 2)

            ' Los dos botones principales ocupan la misma celda: se ve el del paso actual.
            btnEnviar = Tema.CrearBoton("Enviar código", EstiloBoton.Primario)
            btnEnviar.Dock = DockStyle.Fill
            btnCambiar = Tema.CrearBoton("Cambiar contraseña", EstiloBoton.Exito)
            btnCambiar.Dock = DockStyle.Fill
            Dim celdaBoton As New Panel With {.Dock = DockStyle.Fill, .Margin = New Padding(0),
                                              .BackColor = Color.Transparent}
            celdaBoton.Controls.Add(btnEnviar)
            celdaBoton.Controls.Add(btnCambiar)
            raiz.Controls.Add(celdaBoton, 0, 3)

            btnCancelar = Tema.CrearBoton("Cancelar", EstiloBoton.Sutil)
            btnCancelar.Dock = DockStyle.Fill
            btnCancelar.Margin = New Padding(0, 4, 0, 0)
            raiz.Controls.Add(btnCancelar, 0, 4)

            Controls.Add(raiz)
            CancelButton = btnCancelar
        End Sub

        Private Shared Function Columna() As TableLayoutPanel
            Dim t As New TableLayoutPanel With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 0,
                .GrowStyle = TableLayoutPanelGrowStyle.AddRows,
                .BackColor = Color.Transparent,
                .Margin = New Padding(0)
            }
            t.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            Return t
        End Function

        Private Shared Sub AgregarFilas(panel As TableLayoutPanel, ParamArray controles As Control())
            For Each c In controles
                panel.RowStyles.Add(New RowStyle(SizeType.AutoSize))
                panel.Controls.Add(c)
            Next
        End Sub

        Private Shared Function Etiqueta(texto As String) As Label
            Dim l = Tema.CrearLabel(texto, Tono.Secundario, Tema.TamMini, FontStyle.Bold)
            l.AutoSize = False
            l.Dock = DockStyle.Fill
            l.Height = 18
            l.TextAlign = ContentAlignment.MiddleLeft
            Return l
        End Function

        Private Shared Function Separador(alto As Integer) As Control
            Return New Panel With {.Dock = DockStyle.Fill, .Height = alto,
                                   .BackColor = Color.Transparent, .Margin = New Padding(0)}
        End Function

        Private Sub MostrarPaso(paso As Integer)
            panelPedir.Visible = (paso = 1)
            btnEnviar.Visible = (paso = 1)
            panelCambiar.Visible = (paso = 2)
            btnCambiar.Visible = (paso = 2)
            AcceptButton = If(paso = 1, btnEnviar, btnCambiar)
            If paso = 1 Then
                ActiveControl = txtIdentificador
            Else
                txtCodigo.Text = String.Empty
                ActiveControl = txtCodigo
            End If
        End Sub

        Private Sub MostrarError(texto As String)
            lblMensaje.ForeColor = Tema.ColorDeTono(Tono.Peligro)
            lblMensaje.Text = texto
        End Sub

        ' ------------------------------------------------------------------
        '  Acciones
        ' ------------------------------------------------------------------

        Private Async Sub btnEnviar_Click(sender As Object, e As EventArgs) Handles btnEnviar.Click
            Await PedirCodigoAsync()
        End Sub

        Private Async Sub lnkReenviar_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) _
            Handles lnkReenviar.LinkClicked
            Await PedirCodigoAsync()
        End Sub

        ''' <summary>
        ''' Mandar el mail tarda unos segundos: se hace sin trabar la ventana y con los
        ''' botones apagados, para que un doble click no mande dos codigos.
        ''' </summary>
        Private Async Function PedirCodigoAsync() As Threading.Tasks.Task
            MostrarError("")
            Ocupado(True, "Enviando código...")
            Dim resultado As ResultadoRecuperacion
            Try
                resultado = Await RecuperacionService.SolicitarCodigoAsync(txtIdentificador.Text)
            Catch ex As Exception
                resultado = ResultadoRecuperacion.Fallo("No se pudo consultar la base de datos: " & ex.Message)
            Finally
                Ocupado(False, "")
            End Try
            If IsDisposed Then Return

            If Not resultado.Ok Then
                MostrarError(resultado.Mensaje)
                Return
            End If

            usuarioId = resultado.UsuarioId
            lblEnviado.Text = resultado.Mensaje
            MostrarPaso(2)
        End Function

        Private Sub btnCambiar_Click(sender As Object, e As EventArgs) Handles btnCambiar.Click
            MostrarError("")
            Dim resultado As ResultadoRecuperacion
            Try
                resultado = RecuperacionService.CambiarContrasena(
                    usuarioId, txtCodigo.Text, txtNueva.Text, txtRepetir.Text)
            Catch ex As Exception
                MostrarError("No se pudo guardar la contraseña: " & ex.Message)
                Return
            End Try

            If Not resultado.Ok Then
                MostrarError(resultado.Mensaje)
                Return
            End If

            DialogResult = DialogResult.OK
            Close()
        End Sub

        Private Sub chkMostrar_CheckedChanged(sender As Object, e As EventArgs) Handles chkMostrar.CheckedChanged
            txtNueva.UseSystemPasswordChar = Not chkMostrar.Checked
            txtRepetir.UseSystemPasswordChar = Not chkMostrar.Checked
        End Sub

        Private Sub btnCancelar_Click(sender As Object, e As EventArgs) Handles btnCancelar.Click
            DialogResult = DialogResult.Cancel
            Close()
        End Sub

        Private Sub Ocupado(ocupado As Boolean, texto As String)
            btnEnviar.Enabled = Not ocupado
            lnkReenviar.Enabled = Not ocupado
            btnCancelar.Enabled = Not ocupado
            UseWaitCursor = ocupado
            If ocupado Then
                lblMensaje.ForeColor = Tema.ColorDeTono(Tono.Secundario)
                lblMensaje.Text = texto
            ElseIf lblMensaje.Text = "Enviando código..." Then
                lblMensaje.Text = String.Empty
            End If
        End Sub

    End Class

End Namespace
