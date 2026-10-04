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
        Private indDeuda As UiKit.Indicador
        Private indPendientes As UiKit.Indicador
        Private indCobradoHoy As UiKit.Indicador
        Private lblDeuda As Label
        Private tablaDeudas As DataGridView
        Private WithEvents txtMontoCobro As TextBox
        Private cmbMedioCobro As ComboBox

        Public Sub New()
            MyBase.New("Caja", "Apertura, egresos, deudas y cierre del turno")
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
            raiz.Controls.Add(UiKit.FilaDeIndicadores(indApertura, indVentas, indEgresos, indSaldo), 0, 0)

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

            Dim pestanas As New TabControl With {
                .Dock = DockStyle.Fill,
                .Name = "th.pestanas",
                .Font = Tema.Fuente(Tema.TamNormal)
            }
            Dim pagMovimientos = New TabPage("Movimientos de caja") With {.BackColor = Tema.BgPrincipal}
            Dim pagDeudas = New TabPage("Cuentas por cobrar") With {.BackColor = Tema.BgPrincipal}
            pagMovimientos.Controls.Add(raiz)
            pagDeudas.Controls.Add(ArmarPestanaDeudas())
            pestanas.TabPages.Add(pagMovimientos)
            pestanas.TabPages.Add(pagDeudas)
            Tema.EstilarPestanas(pestanas)
            Contenido.Controls.Add(pestanas)
        End Sub

        ''' <summary>
        ''' Pestana de cuentas por cobrar. Va dentro de Caja y no como modulo aparte a
        ''' proposito: cobrar una deuda es un cobro de plata, asi que pertenece al lugar
        ''' donde ya se registran los ingresos del turno y no a un modulo mas.
        '''
        ''' La lista muestra solo lo que falta pagar, no un historial de clientes: el que
        ''' esta en la pantalla es porque todavia debe. Lo que ya se saldio desaparece.
        ''' </summary>
        Private Function ArmarPestanaDeudas() As Control
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

            indDeuda = New UiKit.Indicador("Por cobrar", "0", Tema.Acento)
            indPendientes = New UiKit.Indicador("Ventas pendientes", "0", Tema.Verde)
            indCobradoHoy = New UiKit.Indicador("Cobrado hoy", "0", Tema.TextoPrinc)
            raiz.Controls.Add(UiKit.FilaDeIndicadores(indDeuda, indPendientes, indCobradoHoy), 0, 0)

            lblDeuda = Tema.CrearLabel("", Tono.Secundario, Tema.TamMini, FontStyle.Bold)
            lblDeuda.Dock = DockStyle.Fill
            lblDeuda.AutoSize = False
            lblDeuda.Height = 20
            raiz.Controls.Add(lblDeuda, 0, 1)

            tablaDeudas = Tema.CrearTabla()
            tablaDeudas.Dock = DockStyle.Fill
            tablaDeudas.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            tablaDeudas.MultiSelect = False
            UiKit.Columna(tablaDeudas, "Fecha", 140)
            UiKit.Columna(tablaDeudas, "Venta", 90)
            UiKit.Columna(tablaDeudas, "Cliente", 220)
            UiKit.Columna(tablaDeudas, "Telefono", 130)
            UiKit.Columna(tablaDeudas, "Total", 100)
            UiKit.Columna(tablaDeudas, "Pagado", 100)
            UiKit.Columna(tablaDeudas, "Saldo", 100)
            tablaDeudas.Columns(2).FillWeight = 30
            tablaDeudas.Columns(4).FillWeight = 12
            tablaDeudas.Columns(5).FillWeight = 12
            tablaDeudas.Columns(6).FillWeight = 12
            AddHandler tablaDeudas.SelectionChanged, Sub(s, e) PrepararCobro()
            raiz.Controls.Add(tablaDeudas, 0, 2)

            txtMontoCobro = Tema.CrearInput()
            txtMontoCobro.Width = 120
            txtMontoCobro.TextAlign = HorizontalAlignment.Right
            cmbMedioCobro = Tema.CrearCombo()
            cmbMedioCobro.Width = 150
            cmbMedioCobro.Items.AddRange([Enum].GetNames(GetType(MetodoPago)).Cast(Of Object)().ToArray())
            cmbMedioCobro.SelectedIndex = 0

            Dim btnCobrar = Tema.CrearBoton("Registrar cobro", EstiloBoton.Primario, 140, 38)
            AddHandler btnCobrar.Click, Sub(s, e) RegistrarCobro()

            Dim acciones = UiKit.Botonera(btnCobrar)
            Dim cobro As New TableLayoutPanel With {
                .Name = "th.fondo",
                .Dock = DockStyle.Fill,
                .ColumnCount = 3,
                .RowCount = 1,
                .BackColor = Color.Transparent,
                .AutoSize = True
            }
            cobro.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
            cobro.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
            cobro.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            cobro.Controls.Add(txtMontoCobro, 0, 0)
            cobro.Controls.Add(cmbMedioCobro, 1, 0)
            cobro.Controls.Add(acciones, 2, 0)
            raiz.Controls.Add(cobro, 0, 3)

            Return raiz
        End Function

        ''' <summary>La venta de la fila elegida, o Nothing si no hay ninguna elegida.</summary>
        Private Function VentaElegida() As Venta
            If tablaDeudas.CurrentRow Is Nothing Then Return Nothing
            Dim tag = tablaDeudas.CurrentRow.Cells(1).Tag
            If Not (TypeOf tag Is Integer) Then Return Nothing
            Return VentaService.Buscar(DirectCast(tag, Integer))
        End Function

        ''' <summary>
        ''' Al elegir una fila se ofrece el saldo completo. Es lo que se quiere casi
        ''' siempre y evita que haya que ir a la calculadora para tipear el mismo numero
        ''' que ya esta escrito en la fila.
        ''' </summary>
        Private Sub PrepararCobro()
            Dim venta = VentaElegida()
            If venta Is Nothing OrElse Not venta.TieneSaldo Then
                txtMontoCobro.Text = String.Empty
                Return
            End If
            txtMontoCobro.Text = venta.Saldo.ToString("N2", CultureInfo.CurrentCulture)
        End Sub

        Private Sub RegistrarCobro()
            Dim venta = VentaElegida()
            If venta Is Nothing Then
                MessageBox.Show("Elegi una venta de la lista para cobrar.", "Cuentas por cobrar",
                                MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If
            If Not venta.TieneSaldo Then
                MessageBox.Show("Esa venta ya esta saldada.", "Cuentas por cobrar",
                                MessageBoxButtons.OK, MessageBoxIcon.Information)
                Refrescar()
                Return
            End If
            If Not CajaService.EstaAbierta() Then
                MessageBox.Show("La caja esta cerrada. Abri la para poder cobrar.", "Cuentas por cobrar",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim monto As Decimal
            If Not Decimal.TryParse(txtMontoCobro.Text, monto) OrElse monto <= 0D Then
                MessageBox.Show("Ingresa cuanto se esta cobrando.", "Cuentas por cobrar",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtMontoCobro.SelectAll()
                txtMontoCobro.Focus()
                Return
            End If
            If monto > venta.Saldo Then
                MessageBox.Show("El importe supera el saldo de " & venta.Saldo.ToString("C2") & ".",
                                "Cuentas por cobrar", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim quien = If(venta.ClienteNombre, "(sin nombre)")
            Dim texto = If(monto < venta.Saldo,
                           "Cobrar " & monto.ToString("C2") & " de los " & venta.Saldo.ToString("C2") &
                           " que debe " & quien & " (" & venta.Numero & ")? Quedan " &
                           (venta.Saldo - monto).ToString("C2") & " pendientes.",
                           "Cobrar los " & venta.Saldo.ToString("C2") & " que debe " & quien &
                           " (" & venta.Numero & ") y saldarla?")
            If MessageBox.Show(texto, "Registrar cobro", MessageBoxButtons.OKCancel,
                               MessageBoxIcon.Question) <> DialogResult.OK Then
                Return
            End If

            Dim medio = DirectCast(cmbMedioCobro.SelectedItem, String)
            Try
                VentaService.RegistrarPago(venta, monto,
                                           DirectCast([Enum].Parse(GetType(MetodoPago), medio), MetodoPago))
                txtMontoCobro.Text = String.Empty
                Refrescar()
            Catch ex As Exception
                MessageBox.Show(ex.Message, "Cuentas por cobrar",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End Try
        End Sub

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

            RefrescarDeudas()

            SetearSubtitulo(If(CajaService.EstaAbierta(), "Turno abierto", "Turno cerrado"))
        End Sub

        Private Sub RefrescarDeudas()
            Dim pendientes = VentaService.Pendientes()
            Dim cobradoHoy = Datos.Caja.Movimientos.
                Where(Function(m) m.Tipo = MovimientoCaja.TipoCobroDeuda AndAlso
                               m.FechaHora.Date = Date.Today).
                Sum(Function(m) m.Monto)

            indDeuda.Actualizar(pendientes.Sum(Function(v) v.Saldo).ToString("C2"))
            indPendientes.Actualizar(pendientes.Count.ToString())
            indCobradoHoy.Actualizar(cobradoHoy.ToString("C2"))

            If pendientes.Count = 0 Then
                lblDeuda.Text = "No hay ventas pendientes de cobro."
                lblDeuda.ForeColor = Tema.Verde
            Else
                lblDeuda.Text = pendientes.Count & " venta(s) por cobrar. Elegi una fila: el importe se completa solo."
                lblDeuda.ForeColor = Tema.Acento
            End If

            ' El id de la fila elegida se anota antes de vaciar la tabla. Con la tabla
            ' vacia CurrentRow es Nothing, y una fila recien creada no tiene columnas:
            ' preguntar el Tag a ciegas revienta al abrir el modulo sin deudas.
            Dim idARestaurar As Integer? = Nothing
            If tablaDeudas.CurrentRow IsNot Nothing AndAlso
               TypeOf tablaDeudas.CurrentRow.Cells(1).Tag Is Integer Then
                idARestaurar = DirectCast(tablaDeudas.CurrentRow.Cells(1).Tag, Integer)
            End If

            tablaDeudas.Rows.Clear()
            For Each v In pendientes.OrderBy(Function(x) x.FechaHora)
                Dim i = tablaDeudas.Rows.Add(
                    v.FechaHora.ToString("dd/MM/yyyy HH:mm"), v.Numero,
                    If(v.ClienteNombre, "(sin nombre)"), If(v.ClienteTelefono, "--"),
                    v.Total.ToString("C2", CultureInfo.CurrentCulture),
                    v.MontoPagado.ToString("C2", CultureInfo.CurrentCulture),
                    v.Saldo.ToString("C2", CultureInfo.CurrentCulture))
                ' El id viaja en la columna Venta y no en la primera porque las columnas
                ' se reordenan y el usuario puede cambiar el orden: el indice no es fijo.
                tablaDeudas.Rows(i).Cells(1).Tag = v.ID
            Next

            ' Volver a poner el cursor donde estaba: si no, cada cobro salta al
            ' principio de la lista y el que cobra veinte deudas de un tiro tiene
            ' que buscar con el mouse la fila siguiente.
            If idARestaurar.HasValue Then
                For Each fila As DataGridViewRow In tablaDeudas.Rows
                    If TypeOf fila.Cells(1).Tag Is Integer AndAlso
                       DirectCast(fila.Cells(1).Tag, Integer) = idARestaurar Then
                        fila.Selected = True
                        tablaDeudas.CurrentCell = fila.Cells(1)
                        Exit For
                    End If
                Next
            End If
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