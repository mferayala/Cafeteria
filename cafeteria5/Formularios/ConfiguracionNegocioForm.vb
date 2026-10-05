Imports System.Drawing
Imports System.Globalization
Imports System.Linq
Imports System.Windows.Forms

Namespace CafeteriaOS

    ''' <summary>
    ''' Configuracion del negocio. Solo para administradores.
    '''
    ''' Se separa en dos bloques porque son cosas de otra naturaleza. Los datos del
    ''' negocio (nombre, CUIT, direccion) salen impresos en el ticket y los ve el
    ''' cliente. Los porcentajes de calculo (margen, gastos, IVA) no los ve nadie: son
    ''' los que sirven para sugerir un precio de venta al cargar un producto, y para eso
    ''' son secretos del negocio.
    '''
    ''' Ningun campo es obligatorio salvo el nombre: se carga lo que se tenga y lo que
    ''' falte simplemente no se imprime.
    ''' </summary>
    Partial Class ConfiguracionNegocioForm
        Inherits Form

        Private config As ConfiguracionNegocio

        Private txtNombre As TextBox
        Private txtCuit As TextBox
        Private txtDireccion As TextBox
        Private txtTelefono As TextBox

        Private numMargen As NumericUpDown
        Private numGastos As NumericUpDown
        Private numIva As NumericUpDown
        Private chkGastos As CheckBox
        Private chkIva As CheckBox
        Private lblEjemplo As Label

        Private WithEvents btnGuardar As Button
        Private WithEvents btnCancelar As Button

        Public Sub New()
            ' Se trabaja sobre una copia: si se cancela, la configuracion viva
            ' queda como estaba.
            config = CopiaDe(Datos.ConfigNegocio)
            ArmarInterfaz()
            CargarDatos()
        End Sub

        ''' <summary>
        ''' Abre la configuracion. Devuelve True si se guardo. Un cajero no puede
        ''' entrar: los porcentajes son del negocio y cualquiera que los vea puede
        ''' conocer el margen.
        ''' </summary>
        Public Shared Function Editar() As Boolean
            Dim usuario = Datos.UsuarioActual
            If usuario Is Nothing OrElse
               Not AutenticacionService.PuedeEditarConfiguracion(usuario.Rol) Then
                MessageBox.Show("La configuracion del negocio es solo para administradores.",
                                "Configuracion", MessageBoxButtons.OK,
                                MessageBoxIcon.Warning)
                Return False
            End If

            Using f As New ConfiguracionNegocioForm()
                If f.ShowDialog() = DialogResult.OK Then Return True
            End Using
            Return False
        End Function

        Private Shared Function CopiaDe(original As ConfiguracionNegocio) As ConfiguracionNegocio
            If original Is Nothing Then Return New ConfiguracionNegocio()
            Dim copia As New ConfiguracionNegocio With {
                .MargenSobreCosto = original.MargenSobreCosto,
                .GastosFijosPorcentaje = original.GastosFijosPorcentaje,
                .IVAGeneral = original.IVAGeneral,
                .AplicarGastosFijos = original.AplicarGastosFijos,
                .AplicarIVA = original.AplicarIVA,
                .NombreNegocio = original.NombreNegocio,
                .CUIT = original.CUIT,
                .Direccion = original.Direccion,
                .Telefono = original.Telefono
            }
            Return copia
        End Function

        Private Sub ArmarInterfaz()
            Text = "Configuracion del negocio"
            StartPosition = FormStartPosition.CenterParent
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ClientSize = New Size(620, 560)
            ' Sin esto WinForms escala con los valores por defecto y no con los que
            ' escribe el Designer: a 125% o 150% la fuente crece y las columnas no,
            ' y los labels quedan cortados.
            AutoScaleMode = AutoScaleMode.Dpi
            AutoScaleDimensions = New SizeF(96, 96)
            BackColor = Tema.BgPrincipal
            Font = Tema.Fuente(Tema.TamNormal)

            Dim raiz As New TableLayoutPanel With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 2,
                .RowCount = 14,
                .BackColor = Tema.BgPrincipal,
                .Padding = New Padding(16)
            }
            raiz.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 210))
            raiz.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))

            txtNombre = Tema.CrearInput()
            txtCuit = Tema.CrearInput()
            txtDireccion = Tema.CrearInput()
            txtTelefono = Tema.CrearInput()

            AddRow(raiz, 0, "Nombre del negocio", txtNombre)
            AddRow(raiz, 1, "CUIT", txtCuit)
            AddRow(raiz, 2, "Direccion", txtDireccion)
            AddRow(raiz, 3, "Telefono", txtTelefono)

            AddSeparador(raiz, 4, "CALCULO DE PRECIOS")

            lblAyuda = Tema.CrearLabel(
                "Estos porcentajes se suman sobre el costo para sugerir un precio de venta." &
                Environment.NewLine &
                "No se imponen: el precio final lo define quien carga el producto.",
                Tono.Secundario, Tema.TamMini)
            lblAyuda.Dock = DockStyle.Fill
            lblAyuda.AutoSize = False
            lblAyuda.TextAlign = ContentAlignment.TopLeft
            raiz.Controls.Add(lblAyuda, 0, 5)
            raiz.SetColumnSpan(lblAyuda, 2)

            numMargen = Porcentaje(200D)
            numGastos = Porcentaje(100D)
            numIva = Porcentaje(100D)

            chkGastos = Tema.CrearCheck("Sumar gastos fijos")
            chkIva = Tema.CrearCheck("Sumar IVA")
            AddHandler chkGastos.CheckedChanged, Sub() ActualizarEjemplo()
            AddHandler chkIva.CheckedChanged, Sub() ActualizarEjemplo()
            For Each n In New NumericUpDown() {numMargen, numGastos, numIva}
                AddHandler n.ValueChanged, Sub() ActualizarEjemplo()
            Next

            AddRow(raiz, 6, "Margen sobre costo %", numMargen)
            AddRow(raiz, 7, "Gastos fijos %", numGastos)
            AddRow(raiz, 8, "Aplicar gastos fijos", chkGastos)
            AddRow(raiz, 9, "IVA %", numIva)
            AddRow(raiz, 10, "Aplicar IVA", chkIva)

            lblEjemplo = Tema.CrearLabel("", Tono.Primario, Tema.TamMini)
            lblEjemplo.Dock = DockStyle.Fill
            lblEjemplo.AutoSize = False
            lblEjemplo.TextAlign = ContentAlignment.TopLeft
            raiz.Controls.Add(lblEjemplo, 0, 11)
            raiz.SetColumnSpan(lblEjemplo, 2)

            btnGuardar = Tema.CrearBoton("Guardar", EstiloBoton.Exito, 150, 36)
            btnCancelar = Tema.CrearBoton("Cancelar", EstiloBoton.Neutro, 120, 36)
            AddHandler btnGuardar.Click, Sub() Guardar()
            AddHandler btnCancelar.Click, Sub() Cancelar()

            Dim acciones As New FlowLayoutPanel With {
                .Dock = DockStyle.Fill,
                .FlowDirection = FlowDirection.RightToLeft,
                .WrapContents = False,
                .BackColor = Color.Transparent
            }
            acciones.Controls.Add(btnCancelar)
            acciones.Controls.Add(btnGuardar)
            raiz.Controls.Add(acciones, 0, 13)
            raiz.SetColumnSpan(acciones, 2)

            For i = 0 To 3
                raiz.RowStyles.Add(New RowStyle(SizeType.Absolute, 32))
            Next
            raiz.RowStyles.Add(New RowStyle(SizeType.Absolute, 34))
            raiz.RowStyles.Add(New RowStyle(SizeType.Absolute, 56))
            For i = 5 To 10
                raiz.RowStyles.Add(New RowStyle(SizeType.Absolute, 30))
            Next
            raiz.RowStyles.Add(New RowStyle(SizeType.Absolute, 72))
            raiz.RowStyles.Add(New RowStyle(SizeType.Absolute, 44))

            Controls.Add(raiz)
        End Sub

        Private lblAyuda As Label

        ''' <summary>Campo de porcentaje: nunca negativo y con un tope razonable.</summary>
        Private Function Porcentaje(maximo As Decimal) As NumericUpDown
            Dim n As New NumericUpDown With {
                .Dock = DockStyle.Fill,
                .Minimum = 0D,
                .Maximum = maximo,
                .DecimalPlaces = 2,
                .Increment = 0.5D,
                .ThousandsSeparator = True
            }
            Return n
        End Function

        Private Sub AddRow(panel As TableLayoutPanel, fila As Integer,
                           etiqueta As String, ctrl As Control)

            Dim lbl As New Label With {
                .Text = etiqueta,
                .Dock = DockStyle.Fill,
                .TextAlign = ContentAlignment.MiddleLeft
            }
            panel.Controls.Add(lbl, 0, fila)
            panel.Controls.Add(ctrl, 1, fila)
        End Sub

        Private Sub AddSeparador(panel As TableLayoutPanel, fila As Integer, titulo As String)
            Dim lbl As New Label With {
                .Text = titulo,
                .Dock = DockStyle.Fill,
                .TextAlign = ContentAlignment.MiddleLeft,
                .ForeColor = Tema.TextoSec,
                .Font = Tema.Fuente(Tema.TamMini, FontStyle.Bold),
                .Margin = New Padding(0, 8, 0, 4)
            }
            panel.Controls.Add(lbl, 0, fila)
            panel.SetColumnSpan(lbl, 2)
        End Sub

        Private Sub CargarDatos()
            txtNombre.Text = If(config.NombreNegocio, "")
            txtCuit.Text = If(config.CUIT, "")
            txtDireccion.Text = If(config.Direccion, "")
            txtTelefono.Text = If(config.Telefono, "")
            numMargen.Value = Limitar(numMargen, config.MargenSobreCosto)
            numGastos.Value = Limitar(numGastos, config.GastosFijosPorcentaje)
            numIva.Value = Limitar(numIva, config.IVAGeneral)
            chkGastos.Checked = config.AplicarGastosFijos
            chkIva.Checked = config.AplicarIVA
            ActualizarEjemplo()
        End Sub

        Private Shared Function Limitar(campo As NumericUpDown, valor As Decimal) As Decimal
            If valor < campo.Minimum Then Return campo.Minimum
            If valor > campo.Maximum Then Return campo.Maximum
            Return valor
        End Function

        ''' <summary>
        ''' Muestra el precio que saldria para un costo de referencia. Ver el numero
        ''' con los porcentajes a la vista evita guardar un 30% donde se queria 3%.
        ''' </summary>
        Private Sub ActualizarEjemplo()
            Dim costo = 1000D
            Dim antes = Datos.ConfigNegocio
            Dim sugerencia = PrecioService.CalcularPrecioVenta(costo,
                numMargen.Value, numGastos.Value, numIva.Value,
                chkGastos.Checked, chkIva.Checked)
            lblEjemplo.Text = "Ejemplo: un producto de costo " &
                              costo.ToString("C0", CultureInfo.CurrentCulture) &
                              " se venderia a " +
                              sugerencia.ToString("C2", CultureInfo.CurrentCulture) & "."
        End Sub

        Private Sub Guardar()
            Dim nombre = If(txtNombre.Text, "").Trim()
            If nombre.Length = 0 Then
                MessageBox.Show("Carga el nombre del negocio: sale en el ticket y en la pantalla de ingreso.",
                                "Configuracion", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtNombre.Focus()
                Return
            End If

            config.NombreNegocio = nombre
            config.CUIT = If(txtCuit.Text, "").Trim()
            config.Direccion = If(txtDireccion.Text, "").Trim()
            config.Telefono = If(txtTelefono.Text, "").Trim()
            config.MargenSobreCosto = numMargen.Value
            config.GastosFijosPorcentaje = numGastos.Value
            config.IVAGeneral = numIva.Value
            config.AplicarGastosFijos = chkGastos.Checked
            config.AplicarIVA = chkIva.Checked

            Datos.ConfigNegocio = config

            DialogResult = DialogResult.OK
            Close()
        End Sub

        Private Sub Cancelar()
            DialogResult = DialogResult.Cancel
            Close()
        End Sub

    End Class

End Namespace
