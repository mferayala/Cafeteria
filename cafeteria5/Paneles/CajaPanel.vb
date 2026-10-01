Imports System.Drawing
Imports System.Globalization
Imports System.Linq
Imports System.Windows.Forms

Namespace CafeteriaOS

    ''' <summary>
    ''' Caja del turno. Permite abrirla, registrar egresos y cerrarla. Mientras no esta
    ''' abierta el punto de venta no anota ventas: queda una sola regla y un solo lugar.
    ''' </summary>
    Public Class CajaPanel
        Inherits PanelBase

        Private indApertura As UiKit.Indicador
        Private indVentas As UiKit.Indicador
        Private indEgresos As UiKit.Indicador
        Private indSaldo As UiKit.Indicador
        Private lblEstado As Label
        Private tabla As DataGridView
        Private WithEvents txtMonto As TextBox
        Private WithEvents txtDescripcion As TextBox

        Public Sub New()
            MyBase.New("Caja", "Apertura, egresos y cierre del turno")
            ArmarLayout()
        End Sub

        Private Sub ArmarLayout()
            Dim raiz As New TableLayoutPanel With {
                .Name = "th.fondo",
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 4,
                .BackColor = Tema.BgPrincipal
            }
            raiz.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            raiz.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            raiz.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            raiz.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            raiz.RowStyles.Add(New RowStyle(SizeType.AutoSize))

            indApertura = New UiKit.Indicador("Apertura", "--", Tema.TextoPrinc)
            indVentas = New UiKit.Indicador("Ventas del turno", "--", Tema.Verde)
            indEgresos = New UiKit.Indicador("Egresos", "--", Tema.Rojo)
            indSaldo = New UiKit.Indicador("Saldo en caja", "--", Tema.Acento)
            raiz.Controls.Add(FilaDeIndicadores(indApertura, indVentas, indEgresos, indSaldo), 0, 0)

            lblEstado = Tema.CrearLabel("", Tono.Secundario, Tema.TamMini, FontStyle.Bold)
            lblEstado.Dock = DockStyle.Fill
            lblEstado.AutoSize = False
            lblEstado.Height = 20
            raiz.Controls.Add(lblEstado, 0, 1)

            tabla = Tema.CrearTabla()
            tabla.Dock = DockStyle.Fill
            UiKit.Columna(tabla, "Fecha", 150)
            UiKit.Columna(tabla, "Tipo", 110)
            UiKit.Columna(tabla, "Descripcion", 320)
            UiKit.Columna(tabla, "Usuario", 130)
            UiKit.Columna(tabla, "Monto", 110)
            tabla.Columns(0).FillWeight = 20
            tabla.Columns(1).FillWeight = 14
            tabla.Columns(2).FillWeight = 40
            tabla.Columns(3).FillWeight = 14
            tabla.Columns(4).FillWeight = 16
            raiz.Controls.Add(tabla, 0, 2)

            txtMonto = Tema.CrearInput()
            txtMonto.Width = 120
            txtMonto.TextAlign = HorizontalAlignment.Right
            txtDescripcion = Tema.CrearInput()
            txtDescripcion.Width = 260

            Dim btnAbrir = Tema.CrearBoton("Abrir caja", EstiloBoton.Primario, 130, 38)
            Dim btnEgreso = Tema.CrearBoton("Registrar egreso", EstiloBoton.Aviso, 150, 38)
            Dim btnCorte = Tema.CrearBoton("Corte de caja", EstiloBoton.Info, 140, 38)
            Dim btnCerrar = Tema.CrearBoton("Cerrar caja", EstiloBoton.Peligro, 130, 38)

            AddHandler btnAbrir.Click, Sub(s, e) AbrirCaja()
            AddHandler btnEgreso.Click, Sub(s, e) RegistrarEgreso()
            AddHandler btnCorte.Click, Sub(s, e) VerCorte()
            AddHandler btnCerrar.Click, Sub(s, e) CerrarCaja()

            Dim acciones = UiKit.Botonera(btnAbrir, btnEgreso, btnCorte, btnCerrar)
            Dim ingreso As New TableLayoutPanel With {
                .Name = "th.fondo",
                .Dock = DockStyle.Fill,
                .ColumnCount = 3,
                .RowCount = 1,
                .BackColor = Color.Transparent,
                .AutoSize = True
            }
            ingreso.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
            ingreso.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
            ingreso.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            ingreso.Controls.Add(txtMonto, 0, 0)
            ingreso.Controls.Add(txtDescripcion, 1, 0)
            ingreso.Controls.Add(acciones, 2, 0)

            raiz.Controls.Add(ingreso, 0, 3)
            Contenido.Controls.Add(raiz)
        End Sub

        Private Shared Function FilaDeIndicadores(ParamArray indicadores As UiKit.Indicador()) As TableLayoutPanel
            Dim fila As New TableLayoutPanel With {
                .Name = "th.fondo",
                .Dock = DockStyle.Fill,
                .ColumnCount = indicadores.Length,
                .RowCount = 1,
                .BackColor = Color.Transparent,
                .Height = 74
            }
            For i = 0 To indicadores.Length - 1
                fila.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
                fila.Controls.Add(indicadores(i).Raiz, i, 0)
            Next
            Return fila
        End Function

        Public Overrides Sub Refrescar()
            Dim caja = Datos.Caja
            If CajaService.EstaAbierta() Then
                lblEstado.Text = "Caja abierta por " & caja.UsuarioApertura & " el " &
                                 caja.FechaApertura.ToString("dd/MM/yyyy HH:mm")
                lblEstado.ForeColor = Tema.Verde
                indApertura.Actualizar(caja.MontoInicial.ToString("C2"))
                indVentas.Actualizar(caja.TotalVentas.ToString("C2"))
                indEgresos.Actualizar(caja.TotalEgresos.ToString("C2"))
                indSaldo.Actualizar(caja.SaldoActual.ToString("C2"))
            Else
                lblEstado.Text = "La caja esta cerrada. Abrila para poder cobrar."
                lblEstado.ForeColor = Tema.Rojo
                indApertura.Actualizar("--")
                indVentas.Actualizar(caja.TotalVentas.ToString("C2"))
                indEgresos.Actualizar(caja.TotalEgresos.ToString("C2"))
                indSaldo.Actualizar(caja.SaldoActual.ToString("C2"))
            End If

            tabla.Rows.Clear()
            For Each m In caja.Movimientos.OrderByDescending(Function(x) x.FechaHora)
                tabla.Rows.Add(m.FechaHora.ToString("dd/MM/yyyy HH:mm"), m.Tipo, m.Descripcion,
                               m.Usuario, m.Monto.ToString("C2", CultureInfo.CurrentCulture))
            Next

            SetearSubtitulo(If(CajaService.EstaAbierta(), "Turno abierto", "Turno cerrado"))
        End Sub

        Private Sub AbrirCaja()
            If CajaService.EstaAbierta() Then
                MessageBox.Show("La caja ya esta abierta.", "Caja",
                                MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If
            Dim monto As Decimal
            If Not Decimal.TryParse(txtMonto.Text, monto) OrElse monto < 0D Then
                MessageBox.Show("Ingresa el monto con el que se abre la caja.", "Caja",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtMonto.SelectAll()
                txtMonto.Focus()
                Return
            End If

            CajaService.Abrir(monto)
            txtMonto.Text = String.Empty
            Refrescar()
        End Sub

        Private Sub RegistrarEgreso()
            If Not CajaService.EstaAbierta() Then
                MessageBox.Show("La caja esta cerrada.", "Caja",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            Dim monto As Decimal
            If Not Decimal.TryParse(txtMonto.Text, monto) OrElse monto <= 0D Then
                MessageBox.Show("Ingresa el importe del egreso.", "Caja",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtMonto.SelectAll()
                txtMonto.Focus()
                Return
            End If

            CajaService.RegistrarEgreso(monto, txtDescripcion.Text.Trim())
            txtMonto.Text = String.Empty
            txtDescripcion.Text = String.Empty
            Refrescar()
        End Sub

        Private Sub CerrarCaja()
            If Not CajaService.EstaAbierta() Then
                MessageBox.Show("La caja ya esta cerrada.", "Caja",
                                MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If
            Dim corte = Datos.Caja.SaldoActual
            If MessageBox.Show("El saldo a entregar es " & corte.ToString("C2") & ". Confirmar el cierre?",
                               "Cerrar caja", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) _
               <> DialogResult.OK Then
                Return
            End If
            CajaService.Cerrar()
            Refrescar()
        End Sub

        Private Sub VerCorte()
            Using f As New CorteCajaForm(Datos.Caja)
                f.ShowDialog()
            End Using
        End Sub
    End Class

End Namespace