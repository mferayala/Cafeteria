Imports System.Drawing
Imports System.Windows.Forms

Namespace CafeteriaOS

    ''' <summary>
    ''' Base comun de los modulos del dashboard: armonica (cabecera con titulo) y area de
    ''' contenido. Existe para que los siete modulos no repitan el mismo esqueleto ni el
    ''' mismo manejo de recarga.
    ''' </summary>
    Public MustInherit Class PanelBase
        Inherits UserControl

        Private lblTitulo As Label
        Private lblSubtitulo As Label
        Private area As Panel

        Protected ReadOnly Property Contenido As Panel
            Get
                Return area
            End Get
        End Property

        Public Sub New(titulo As String, subtitulo As String)
            Me.Name = "th.fondo"
            Me.Dock = DockStyle.Fill
            Me.BackColor = Tema.BgPrincipal
            Me.Font = Tema.Fuente(Tema.TamNormal)

            Dim cabecera As New TableLayoutPanel With {
                .Name = "th.cabecera",
                .Dock = DockStyle.Top,
                .ColumnCount = 2,
                .RowCount = 1,
                .Height = 62,
                .BackColor = Tema.Acento,
                .Padding = New Padding(18, 8, 18, 8)
            }
            cabecera.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            cabecera.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))

            lblTitulo = Tema.CrearLabel(titulo, Tono.SobreAcento, Tema.TamSeccion, FontStyle.Bold)
            lblTitulo.Dock = DockStyle.Fill
            lblTitulo.TextAlign = ContentAlignment.MiddleLeft

            lblSubtitulo = Tema.CrearLabel(subtitulo, Tono.SobreAcentoSuave, Tema.TamMini)
            lblSubtitulo.Dock = DockStyle.Fill
            lblSubtitulo.TextAlign = ContentAlignment.MiddleRight
            lblSubtitulo.AutoSize = False

            cabecera.Controls.Add(lblTitulo, 0, 0)
            cabecera.Controls.Add(lblSubtitulo, 1, 0)

            area = New Panel With {
                .Name = "th.fondo",
                .Dock = DockStyle.Fill,
                .BackColor = Tema.BgPrincipal,
                .Padding = New Padding(14)
            }

            Controls.Add(area)
            Controls.Add(cabecera)
        End Sub

        Protected Sub SetearSubtitulo(texto As String)
            lblSubtitulo.Text = texto
        End Sub

        ''' <summary>
        ''' Vuelve a leer el estado y a pintar los controles. Se llama al abrir el modulo y
        ''' tambien despues de cambiar el tema, para que ningun dato se quede desactualizado.
        ''' </summary>
        Public Overridable Sub Refrescar()
        End Sub
    End Class

End Namespace