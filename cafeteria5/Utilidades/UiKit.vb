Imports System.Drawing
Imports System.Windows.Forms

Namespace CafeteriaOS

    ''' Piezas de interfaz que se repiten en varios modulos: tarjetas de indicador y
    ''' filas de botones. Agruparlas evita que cada panel invente su propia version.
    Public Module UiKit

        ''' <summary>Tarjeta de indicador con su valor actualizable sin recrear controles.</summary>
        Public Class Indicador
            ''' <summary>
            ''' Alto de la tarjeta. La fila de indicadores tiene que medir lo mismo que
            ''' la tarjeta: si la fila queda mas baja, el borde de abajo de la tarjeta
            ''' pisa el numero grande y lo deja cortado.
            ''' </summary>
            Public Const Alto As Integer = 88

            Public ReadOnly Property Raiz As Panel
            Public ReadOnly Property Valor As Label

            Public Sub New(titulo As String, valorInicial As String, color As Color)
                ' El Panel local no se llama "raiz" a proposito: en VB los nombres no
                ' distinguen mayusculas, asi que chocaria con la propiedad Raiz y la
                ' asignacion "Raiz = raiz" se resolveria a si misma dejando la
                ' propiedad en Nothing.
                Dim contenedor As New Panel With {
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
                contenedor.Controls.Add(layout)

                Raiz = contenedor
            End Sub

            Public Sub Actualizar(texto As String)
                Valor.Text = texto
            End Sub
        End Class

        ''' <summary>
        ''' Fila horizontal de tarjetas de indicador, una por columna y todas del mismo
        ''' ancho. Vive aca porque Reportes y Caja la usaban igual pero cada una con su
        ''' propia copia.
        ''' </summary>
        Public Function FilaDeIndicadores(ParamArray indicadores As Indicador()) As TableLayoutPanel
            Dim fila As New TableLayoutPanel With {
                .Name = "th.fondo",
                .Dock = DockStyle.Top,
                .ColumnCount = indicadores.Length,
                .RowCount = 1,
                .BackColor = Color.Transparent,
                .Height = Indicador.Alto
            }
            ' Sin RowStyle la fila unica queda con el alto por defecto del panel y las
            ' tarjetas se salen del borde de abajo.
            fila.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            For i = 0 To indicadores.Length - 1
                fila.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
                fila.Controls.Add(indicadores(i).Raiz, i, 0)
            Next
            Return fila
        End Function

        ''' <summary>
        ''' Reparte el ancho sobrante entre las columnas indicadas y deja el resto con
        ''' su ancho fijo. Las cabeceras de un ListView no se estiran solas, asi que sin
        ''' esto al lado de la ultima columna queda el rectangulo vacio del fondo.
        ''' </summary>
        Public Sub EstirarColumnas(lista As ListView, ParamArray indices As Integer())
            Dim fijas As Integer
            For i = 0 To lista.Columns.Count - 1
                If Not indices.Contains(i) Then fijas += lista.Columns(i).Width
            Next

            Dim libre = indices.Length
            If libre < 1 Then Exit Sub

            Dim ancho = (lista.ClientSize.Width - fijas) \ libre
            For Each i In indices
                If i >= 0 AndAlso i < lista.Columns.Count Then
                    lista.Columns(i).Width = Math.Max(40, ancho)
                End If
            Next
        End Sub

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
            ' El constructor sin parametros deja CellTemplate en Nothing, y despues la
            ' tabla tira "Al menos una de las columnas no tiene ninguna plantilla de
            ' celda" apenas se le agrega la primera fila. Se asigna a mano la celda de
            ' texto para que toda tabla creada por aca pueda llenarse.
            columna.CellTemplate = New DataGridViewTextBoxCell()
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