Imports System.Drawing
Imports System.Windows.Forms

Namespace CafeteriaOS

    ''' <summary>
    ''' Responsabilidad unica: paleta, tipografia, estilos reutilizables de controles
    ''' y la aplicacion del tema. No contiene reglas de negocio.
    '''
    ''' Los controles se identifican por una convencion de nombre ("th.*") para poder
    ''' reestilizarse en el lugar cuando cambia el tema, sin reconstruir la interfaz.
    ''' </summary>
    Public Module Tema

        ' ── Identificacion visual de controles (ver Restilar) ──
        Private Const Prefijo As String = "th."
        Private Const PrefijoBoton As String = "th.btn."
        Private Const PrefijoTono As String = "th.tono."

        Public Const TamMicro As Single = 8
        Public Const TamMini As Single = 9
        Public Const TamNormal As Single = 10
        Public Const TamMedio As Single = 11
        Public Const TamSubtitulo As Single = 13
        Public Const TamSeccion As Single = 15
        Public Const TamNumero As Single = 19
        Public Const TamLogin As Single = 27

        Private ReadOnly fuentes As New Dictionary(Of String, Font)()
        Private ReadOnly familia As String = DetectarFamilia()

        Public ModoOscuro As Boolean

        #Region "Paleta"

        Public ReadOnly Property BgPrincipal As Color
            Get
                Return If(ModoOscuro, Color.FromArgb(18, 18, 18), Color.FromArgb(245, 240, 232))
            End Get
        End Property

        Public ReadOnly Property Superficie As Color
            Get
                Return If(ModoOscuro, Color.FromArgb(30, 30, 30), Color.FromArgb(255, 255, 255))
            End Get
        End Property

        Public ReadOnly Property Superficie2 As Color
            Get
                Return If(ModoOscuro, Color.FromArgb(40, 40, 40), Color.FromArgb(250, 247, 242))
            End Get
        End Property

        Public ReadOnly Property Superficie3 As Color
            Get
                Return If(ModoOscuro, Color.FromArgb(55, 55, 55), Color.FromArgb(240, 234, 216))
            End Get
        End Property

        Public ReadOnly Property TextoPrinc As Color
            Get
                Return If(ModoOscuro, Color.FromArgb(230, 220, 210), Color.FromArgb(42, 26, 10))
            End Get
        End Property

        Public ReadOnly Property TextoSec As Color
            Get
                Return If(ModoOscuro, Color.FromArgb(170, 150, 130), Color.FromArgb(107, 80, 64))
            End Get
        End Property

        Public ReadOnly Property TextoTer As Color
            Get
                Return If(ModoOscuro, Color.FromArgb(120, 100, 80), Color.FromArgb(160, 128, 96))
            End Get
        End Property

        Public ReadOnly Property Borde As Color
            Get
                Return If(ModoOscuro, Color.FromArgb(60, 50, 40), Color.FromArgb(221, 208, 184))
            End Get
        End Property

        Public ReadOnly Property LateralBg As Color
            Get
                Return If(ModoOscuro, Color.FromArgb(10, 8, 5), Color.FromArgb(28, 18, 8))
            End Get
        End Property

        ' Paleta de marca: no cambia con el tema.
        Public ReadOnly Acento As Color = Color.FromArgb(107, 58, 31)
        Public ReadOnly Acento2 As Color = Color.FromArgb(193, 127, 58)
        Public ReadOnly Acento3 As Color = Color.FromArgb(232, 201, 122)
        Public ReadOnly Verde As Color = Color.FromArgb(46, 125, 82)
        Public ReadOnly Rojo As Color = Color.FromArgb(192, 57, 43)
        Public ReadOnly Azul As Color = Color.FromArgb(36, 113, 163)
        Public ReadOnly Naranja As Color = Color.FromArgb(214, 137, 16)

        Public Function ColorDeBoton(estilo As EstiloBoton) As Color
            Select Case estilo
                Case EstiloBoton.Primario : Return Acento
                Case EstiloBoton.Exito : Return Verde
                Case EstiloBoton.Peligro : Return Rojo
                Case EstiloBoton.Aviso : Return Naranja
                Case EstiloBoton.Info : Return Azul
                Case EstiloBoton.Neutro : Return Superficie3
                Case EstiloBoton.Sutil : Return Borde
            End Select
            Return Acento
        End Function

        Public Function TextoDeBoton(estilo As EstiloBoton) As Color
            Select Case estilo
                Case EstiloBoton.Neutro : Return TextoSec
                Case EstiloBoton.Sutil : Return TextoSec
            End Select
            Return Color.White
        End Function

        Public Function ColorDeTono(tono As Tono) As Color
            Select Case tono
                Case Tono.Primario : Return TextoPrinc
                Case Tono.Secundario : Return TextoSec
                Case Tono.Terciario : Return TextoTer
                Case Tono.Acento : Return Acento
                Case Tono.AcentoSuave : Return Acento2
                Case Tono.Exito : Return Verde
                Case Tono.Peligro : Return Rojo
                Case Tono.Atencion : Return Naranja
                Case Tono.Info : Return Azul
                Case Tono.SobreAcento : Return Color.White
                Case Tono.SobreAcentoSuave : Return Acento3
                Case Tono.Lateral : Return Color.FromArgb(180, 155, 130)
            End Select
            Return TextoSec
        End Function

        #End Region

        #Region "Tipografia"

        ''' <summary>
        ''' San Francisco no esta disponible en Windows. Se resuelve la variante de la
        ''' misma familia sans geometrica mas cercana y se usa una unica familia en toda
        ''' la aplicacion, con jerarquia por tamano, peso y color.
        ''' </summary>
        Private Function DetectarFamilia() As String
            Dim candidatas = {"Segoe UI Variable Text", "Segoe UI Variable", "Segoe UI"}
            For Each nombre In candidatas
                Try
                    Using prueba As New Font(nombre, 10F)
                        If prueba.Name.Equals(nombre, StringComparison.OrdinalIgnoreCase) Then
                            Return nombre
                        End If
                    End Using
                Catch
                End Try
            Next
            Return SystemFonts.DefaultFont.FontFamily.Name
        End Function

        Public Function Fuente(tam As Single, Optional estilo As FontStyle = FontStyle.Regular,
                               Optional monoespaciada As Boolean = False) As Font
            Dim clave = tam.ToString("0.##") & "|" & estilo.ToString() & "|" & monoespaciada
            Dim f As Font = Nothing
            If fuentes.TryGetValue(clave, f) Then Return f
            f = If(monoespaciada,
                   New Font(FontFamily.GenericMonospace, tam, estilo),
                   New Font(familia, tam, estilo))
            fuentes(clave) = f
            Return f
        End Function

        Public Function FamiliaTipografica() As String
            Return familia
        End Function

        ''' <summary>
        ''' Fuente monoespaciada para los tickets y el cierre de caja, donde las columnas
        ''' tienen que alinearse. Va por la misma cache que el resto: los controles no
        ''' liberan la fuente que se les asigna, asi que crearla por uso la dejaria viva.
        ''' </summary>
        Public Function FuenteMonoespaciada(Optional tam As Single = 10F) As Font
            Return Fuente(tam, FontStyle.Regular, True)
        End Function

        #End Region

        #Region "Fabricas de controles"

        Public Function CrearBoton(texto As String,
                                    estilo As EstiloBoton,
                                    Optional ancho As Integer = 200,
                                    Optional alto As Integer = 40) As Button

            Dim b As New Button With {
                .Text = texto,
                .Name = PrefijoBoton & estilo.ToString(),
                .Size = New Size(ancho, alto),
                .Font = Fuente(TamNormal, FontStyle.Bold),
                .FlatStyle = FlatStyle.Flat,
                .Cursor = Cursors.Hand,
                .TextAlign = ContentAlignment.MiddleCenter,
                .UseVisualStyleBackColor = False,
                .Margin = New Padding(0)
            }
            b.FlatAppearance.BorderSize = 0
            AplicarEstiloBoton(b, estilo)
            Return b
        End Function

        Private Sub AplicarEstiloBoton(b As Button, estilo As EstiloBoton)
            Dim baseColor = ColorDeBoton(estilo)
            b.BackColor = baseColor
            b.ForeColor = TextoDeBoton(estilo)
            b.FlatAppearance.MouseOverBackColor = AjustarLuminosidad(baseColor, -22)
            b.FlatAppearance.MouseDownBackColor = AjustarLuminosidad(baseColor, -38)
        End Sub

        Private Function EstiloDeBoton(b As Button) As EstiloBoton
            If b.Name.StartsWith(PrefijoBoton, StringComparison.Ordinal) Then
                Dim valor As EstiloBoton
                If [Enum].TryParse(b.Name.Substring(PrefijoBoton.Length), False, valor) Then
                    Return valor
                End If
            End If
            Return EstiloBoton.Primario
        End Function

        Public Function AjustarLuminosidad(c As Color, delta As Integer) As Color
            Return Color.FromArgb(c.A,
                                  Math.Clamp(c.R + delta, 0, 255),
                                  Math.Clamp(c.G + delta, 0, 255),
                                  Math.Clamp(c.B + delta, 0, 255))
        End Function

        Public Function CrearLabel(texto As String,
                                    Optional tono As Tono = Tono.Secundario,
                                    Optional tam As Single = TamNormal,
                                    Optional estilo As FontStyle = FontStyle.Regular) As Label
            Return New Label With {
                .Text = texto,
                .Name = PrefijoTono & tono.ToString(),
                .ForeColor = ColorDeTono(tono),
                .Font = Fuente(tam, estilo),
                .BackColor = Color.Transparent,
                .AutoSize = True,
                .Margin = New Padding(0)
            }
        End Function

        Public Function CrearInput(Optional esClave As Boolean = False) As TextBox
            Dim t As New TextBox With {
                .Name = "th.campo",
                .Font = Fuente(TamNormal),
                .BackColor = Superficie2,
                .ForeColor = TextoPrinc,
                .BorderStyle = BorderStyle.FixedSingle,
                .Height = 30,
                .Margin = New Padding(0)
            }
            If esClave Then t.UseSystemPasswordChar = True
            Return t
        End Function

        Public Function CrearCombo() As ComboBox
            Return New ComboBox With {
                .Name = "th.campo",
                .Font = Fuente(TamNormal),
                .BackColor = Superficie2,
                .ForeColor = TextoPrinc,
                .FlatStyle = FlatStyle.Flat,
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Height = 30,
                .Margin = New Padding(0)
            }
        End Function

        Public Function CrearCheck(texto As String, Optional tono As Tono = Tono.Secundario) As CheckBox
            Return New CheckBox With {
                .Text = texto,
                .Name = PrefijoTono & tono.ToString(),
                .ForeColor = ColorDeTono(tono),
                .Font = Fuente(TamMini),
                .BackColor = Color.Transparent,
                .AutoSize = True,
                .Margin = New Padding(0)
            }
        End Function

        Public Function CrearSeparador(ancho As Integer) As Panel
            Return New Panel With {
                .Name = "th.borde",
                .Size = New Size(ancho, 1),
                .BackColor = Borde,
                .Margin = New Padding(0)
            }
        End Function

        Public Function CrearTarjeta(ancho As Integer, alto As Integer) As Panel
            Return New Panel With {
                .Name = "th.tarjeta",
                .Size = New Size(ancho, alto),
                .BackColor = Superficie,
                .Margin = New Padding(0)
            }
        End Function

        Public Function CrearTabla() As DataGridView
            Dim d As New DataGridView With {
                .Name = "th.tabla",
                .ReadOnly = True,
                .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False,
                .AllowUserToResizeRows = False,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .MultiSelect = False,
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                .RowHeadersVisible = False,
                .BorderStyle = BorderStyle.None,
                .Font = Fuente(TamMini),
                .ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                .ColumnHeadersHeight = 32
            }
            EstilarTabla(d)
            Return d
        End Function

        Public Sub EstilarTabla(d As DataGridView)
            d.EnableHeadersVisualStyles = False
            d.BackgroundColor = Superficie
            d.GridColor = Borde
            d.DefaultCellStyle.BackColor = Superficie
            d.DefaultCellStyle.ForeColor = TextoPrinc
            d.DefaultCellStyle.SelectionBackColor = Acento
            d.DefaultCellStyle.SelectionForeColor = Color.White
            d.DefaultCellStyle.Font = Fuente(TamMini)
            d.DefaultCellStyle.Padding = New Padding(6, 2, 6, 2)
            d.AlternatingRowsDefaultCellStyle.BackColor = Superficie2
            d.ColumnHeadersDefaultCellStyle.BackColor = Superficie3
            d.ColumnHeadersDefaultCellStyle.ForeColor = TextoPrinc
            d.ColumnHeadersDefaultCellStyle.Font = Fuente(TamMini, FontStyle.Bold)
            d.ColumnHeadersDefaultCellStyle.Padding = New Padding(6, 0, 6, 0)
        End Sub

        Public Function CrearLista() As ListBox
            Return New ListBox With {
                .Name = "th.lista",
                .Font = Fuente(TamMini),
                .BackColor = Superficie2,
                .ForeColor = TextoPrinc,
                .BorderStyle = BorderStyle.None,
                .IntegralHeight = False
            }
        End Function

        ''' <summary>Cabecera de modulo, dockeada arriba, con el color de marca.</summary>
        Public Function CrearCabecera(titulo As String, Optional alto As Integer = 56) As Panel
            Dim lbl As Label = CrearLabel(titulo, Tono.SobreAcento, TamSeccion, FontStyle.Bold)
            lbl.Dock = DockStyle.Fill
            lbl.TextAlign = ContentAlignment.MiddleLeft

            Dim p As New Panel With {
                .Name = "th.cabecera",
                .Dock = DockStyle.Top,
                .Height = alto,
                .BackColor = Acento,
                .Padding = New Padding(18, 0, 18, 0),
                .Margin = New Padding(0)
            }
            p.Controls.Add(lbl)
            Return p
        End Function

        #End Region

        #Region "Aplicacion del tema"

        ''' <summary>
        ''' Reestiliza el arbol de controles en el lugar. No recrea controles, no vuelve a
        ''' registrar eventos ni a crear fuentes, y por lo tanto no pierde el estado de la
        ''' interfaz (pedido en curso, mesa elegida, metodo de pago, filtros, scroll).
        ''' </summary>
        Public Sub Restilar(raiz As Control)
            For Each hijo In HijosDe(raiz)
                Restilar(hijo)
            Next
            AplicarRol(raiz)
        End Sub

        Private Function HijosDe(c As Control) As Control()
            If Not c.HasChildren Then Return Array.Empty(Of Control)()
            Dim lista As New List(Of Control)(c.Controls.Count)
            For Each hijo As Control In c.Controls
                lista.Add(hijo)
            Next
            Return lista.ToArray()
        End Function

        Private Sub AplicarRol(c As Control)
            If Not c.Name.StartsWith(Prefijo, StringComparison.Ordinal) Then Exit Sub
            Dim rol = c.Name.Substring(Prefijo.Length)

            If rol.StartsWith("btn.", StringComparison.Ordinal) Then
                If TypeOf c Is Button Then AplicarEstiloBoton(DirectCast(c, Button), EstiloDeBoton(DirectCast(c, Button)))
                Exit Sub
            End If

            If rol.StartsWith("tono.", StringComparison.Ordinal) Then
                Dim valor As Tono
                If [Enum].TryParse(rol.Substring(5), False, valor) Then
                    c.ForeColor = ColorDeTono(valor)
                End If
                Exit Sub
            End If

            Select Case rol
                Case "fondo" : c.BackColor = BgPrincipal
                Case "tarjeta" : c.BackColor = Superficie
                Case "tarjeta2" : c.BackColor = Superficie2
                Case "tarjeta3" : c.BackColor = Superficie3
                Case "campo"
                    c.BackColor = Superficie2
                    c.ForeColor = TextoPrinc
                Case "borde" : c.BackColor = Borde
                Case "cabecera" : c.BackColor = Acento
                Case "cabeceraSuave"
                    c.BackColor = Acento
                    c.ForeColor = Acento3
                Case "lateral"
                    c.BackColor = LateralBg
                    c.ForeColor = Color.FromArgb(180, 155, 130)
                Case "lateralActivo"
                    c.BackColor = Acento
                    c.ForeColor = Color.White
                Case "tabla" : EstilarTabla(DirectCast(c, DataGridView))
                Case "lista"
                    c.BackColor = Superficie2
                    c.ForeColor = TextoPrinc
                Case "pestanas"
                    EstilarPestanas(DirectCast(c, TabControl))
            End Select
        End Sub

        ''' <summary>
        ''' Unifica el color de las pestañas con el fondo de la ventana. OwnerDraw queda
        ''' apagado: el color de las cabeceras lo define el sistema y por eso un tema
        ''' oscuro mostraba las pestañas claras.
        ''' </summary>
        Public Sub EstilarPestanas(tabs As TabControl)
            tabs.Appearance = TabAppearance.Normal
            tabs.DrawMode = TabDrawMode.Normal
            tabs.SizeMode = TabSizeMode.Fixed
            tabs.Padding = New Point(14, 6)
            tabs.ForeColor = TextoPrinc
            tabs.BackColor = Superficie
            For Each pagina As TabPage In tabs.TabPages
                pagina.BackColor = Superficie
                pagina.ForeColor = TextoPrinc
            Next
        End Sub

        #End Region

    End Module

    Public Enum Tono
        Primario
        Secundario
        Terciario
        Acento
        AcentoSuave
        Exito
        Peligro
        Atencion
        Info
        SobreAcento
        SobreAcentoSuave
        Lateral
    End Enum

End Namespace