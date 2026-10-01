Imports System.Drawing
Imports System.Windows.Forms

Namespace CafeteriaOS

    ''' Piezas de interfaz que se repiten en varios modulos: tarjetas de indicador y
    ''' filas de botones. Agruparlas evita que cada panel invente su propia version.
    Public Module UiKit

        ''' <summary>Tarjeta de indicador con su valor actualizable sin recrear controles.</summary>
        Public Class Indicador
            Public ReadOnly Property Raiz As Panel
            Public ReadOnly Property Valor As Label

            Public Sub New(titulo As String, valorInicial As String, color As Color)
                Dim raiz As New Panel With {
                    .Name = "th.tarjeta",
                    .Dock = DockStyle.Fill,
                    .BackColor = Tema.Superficie,
                    .Padding = New Padding(12, 10, 12, 10)
                }

                Dim layout As New TableLayoutPanel With {
                    .Dock = DockStyle.Fill,
                    .ColumnCount = 1,
                    .RowCount = 2,
                    .BackColor = Color.Transparent
                }
                layout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
                layout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
                layout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

                Dim lblTitulo = Tema.CrearLabel(titulo, Tono.Secundario, Tema.TamMicro, FontStyle.Bold)
                lblTitulo.Dock = DockStyle.Fill
                lblTitulo.AutoSize = False
                lblTitulo.Height = 16

                Valor = New Label With {
                    .Name = "th.tono.Acento",
                    .Text = valorInicial,
                    .Font = Tema.Fuente(Tema.TamNumero, FontStyle.Bold),
                    .ForeColor = color,
                    .BackColor = Color.Transparent,
                    .Dock = DockStyle.Fill,
                    .TextAlign = ContentAlignment.MiddleLeft
                }

                layout.Controls.Add(lblTitulo, 0, 0)
                layout.Controls.Add(Valor, 0, 1)
                raiz.Controls.Add(layout)

                Raiz = raiz
            End Sub

            Public Sub Actualizar(texto As String)
                Valor.Text = texto
            End Sub
        End Class

        ''' <summary>
        ''' Agrega una columna con el ancho indicado. Se construye la columna de forma
        ''' explicita porque la sobrecarga de dos parametros de Add es ambigua al compilar
        ''' con Option Strict.
        ''' </summary>
        Public Sub Columna(tabla As DataGridView, titulo As String, ancho As Integer)
            Dim columna As New DataGridViewColumn()
            columna.HeaderText = titulo
            columna.Name = titulo
            columna.Width = ancho
            tabla.Columns.Add(columna)
        End Sub

        Public Function CampoBusqueda(placeholder As String) As TextBox
            Dim t = Tema.CrearInput()
            t.Dock = DockStyle.Fill
            t.AccessibleDescription = placeholder
            Return t
        End Function

        ''' <summary>Fila de controles de accion de ancho automatico (botones, checks).</summary>
        Public Function Botonera(ParamArray controles As Control()) As FlowLayoutPanel
            Dim p As New FlowLayoutPanel With {
                .Name = "th.tono.Secundario",
                .Dock = DockStyle.Fill,
                .AutoSize = True,
                .AutoSizeMode = AutoSizeMode.GrowAndShrink,
                .FlowDirection = FlowDirection.LeftToRight,
                .WrapContents = False,
                .BackColor = Color.Transparent
            }
            For Each c In controles
                p.Controls.Add(c)
            Next
            Return p
        End Function

    End Module

End Namespace