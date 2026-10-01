Imports System.Drawing
Imports System.Windows.Forms

Namespace CafeteriaOS

    Partial Class ProveedorForm
        Inherits Form

        Private Const Columnas As Integer = 4

        Private Function CrearHoja() As TableLayoutPanel
            Dim p As New TableLayoutPanel With {
                .Name = "th.tarjeta",
                .Dock = DockStyle.Fill,
                .ColumnCount = Columnas,
                .RowCount = 0,
                .GrowStyle = TableLayoutPanelGrowStyle.AddRows,
                .BackColor = Tema.Superficie,
                .Padding = New Padding(12),
                .AutoScroll = True
            }
            For i = 0 To Columnas - 1
                p.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
            Next
            Return p
        End Function

        Private Sub AgregarFila(hoja As TableLayoutPanel, rotulo As String, control As Control)
            hoja.Controls.Add(Etiqueta(rotulo))
            hoja.Controls.Add(control)
        End Sub

        Private Shared Function Etiqueta(texto As String) As Label
            Dim l As Label = Tema.CrearLabel(texto, Tono.Secundario, Tema.TamMini, FontStyle.Bold)
            l.Dock = DockStyle.Fill
            l.AutoSize = False
            l.Width = 120
            l.Height = 28
            l.TextAlign = ContentAlignment.MiddleLeft
            l.Margin = New Padding(0, 3, 8, 3)
            Return l
        End Function

        Private Shared Function CampoAlto(alto As Integer) As TextBox
            Dim t As TextBox = Tema.CrearInput()
            t.Multiline = True
            t.ScrollBars = ScrollBars.Vertical
            t.Dock = DockStyle.Fill
            t.Height = alto
            Return t
        End Function

        Private Sub IniciarInterfaz()
            Text = "Proveedor"
            StartPosition = FormStartPosition.CenterParent
            ClientSize = New Size(920, 680)
            MinimumSize = New Size(760, 560)
            BackColor = Tema.BgPrincipal
            Font = Tema.Fuente(Tema.TamNormal)

            Dim tabs As New TabControl With {
                .Name = "th.pestanas",
                .Dock = DockStyle.Fill,
                .Font = Tema.Fuente(Tema.TamMini, FontStyle.Bold)
            }
            tabs.TabPages.Add(ArmarDatos())
            tabs.TabPages.Add(ArmarContacto())
            tabs.TabPages.Add(ArmarComercial())
            tabs.TabPages.Add(ArmarNotas())
            Tema.EstilarPestanas(tabs)

            btnGuardar = Tema.CrearBoton("Guardar", EstiloBoton.Primario, 130, 40)
            btnNuevo = Tema.CrearBoton("Nuevo", EstiloBoton.Neutro, 120, 40)
            btnCancelar = Tema.CrearBoton("Cancelar", EstiloBoton.Sutil, 120, 40)

            Dim raiz As New TableLayoutPanel With {
                .Name = "th.fondo",
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 2,
                .BackColor = Tema.BgPrincipal,
                .Padding = New Padding(12)
            }
            raiz.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            raiz.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            raiz.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            raiz.Controls.Add(tabs, 0, 0)
            raiz.Controls.Add(UiKit.Botonera(btnNuevo, btnGuardar, btnCancelar), 0, 1)
            Controls.Add(raiz)
        End Sub

        Private Function ArmarDatos() As TabPage
            Dim hoja = CrearHoja()

            txtCodigo = Tema.CrearInput() : txtCodigo.MaxLength = 12
            txtNombre = Tema.CrearInput()
            txtDomicilio = Tema.CrearInput()
            txtCodigoPostal = Tema.CrearInput() : txtCodigoPostal.MaxLength = 12
            cmbTipoIVA = Tema.CrearCombo()
            cmbTipoIVA.Items.AddRange(New String() {"Responsable Inscripto", "Monotributo",
                                                     "No Responsable", "Consumidor Final", "Exento"})
            txtCUIT = Tema.CrearInput() : txtCUIT.MaxLength = 20
            cmbTipoDocumento = Tema.CrearCombo()
            cmbTipoDocumento.Items.AddRange(New String() {"CUIT", "CUIL", "DNI", "Pasaporte"})
            txtNumeroDocumento = Tema.CrearInput() : txtNumeroDocumento.MaxLength = 20
            txtPais = Tema.CrearInput()
            txtProvincia = Tema.CrearInput()
            txtLocalidad = Tema.CrearInput()
            txtActividad = Tema.CrearInput()
            txtCategoria = Tema.CrearInput()
            chkActivo = Tema.CrearCheck("Proveedor activo", Tono.Secundario)
            chkActivo.Checked = True
            dtAlta = New DateTimePicker With {
                .Name = "th.campo",
                .Format = DateTimePickerFormat.Short,
                .Font = Tema.Fuente(Tema.TamNormal),
                .Dock = DockStyle.Fill
            }

            AgregarFila(hoja, "Codigo", txtCodigo)
            AgregarFila(hoja, "Nombre", txtNombre)
            AgregarFila(hoja, "Domicilio", txtDomicilio)
            AgregarFila(hoja, "Codigo postal", txtCodigoPostal)
            AgregarFila(hoja, "Tipo de IVA", cmbTipoIVA)
            AgregarFila(hoja, "CUIT", txtCUIT)
            AgregarFila(hoja, "Tipo de documento", cmbTipoDocumento)
            AgregarFila(hoja, "Numero de documento", txtNumeroDocumento)
            AgregarFila(hoja, "Pais", txtPais)
            AgregarFila(hoja, "Provincia", txtProvincia)
            AgregarFila(hoja, "Localidad", txtLocalidad)
            AgregarFila(hoja, "Actividad", txtActividad)
            AgregarFila(hoja, "Categoria", txtCategoria)
            AgregarFila(hoja, "Fecha de alta", dtAlta)
            hoja.Controls.Add(Etiqueta("Estado"))
            hoja.Controls.Add(chkActivo)
            return HojaDe(hoja, "Datos")
        End Function

        Private Function ArmarContacto() As TabPage
            Dim hoja = CrearHoja()

            txtContactoPrincipal = Tema.CrearInput()
            txtEmail = Tema.CrearInput()
            txtSitioWeb = Tema.CrearInput()
            txtTelLaboral = Tema.CrearInput()
            txtTelParticular = Tema.CrearInput()
            txtCelular = Tema.CrearInput()
            txtFax = Tema.CrearInput()
            txtContactos = CampoAlto(90)
            txtTelefonos = CampoAlto(90)

            AgregarFila(hoja, "Contacto principal", txtContactoPrincipal)
            AgregarFila(hoja, "Email", txtEmail)
            AgregarFila(hoja, "Sitio web", txtSitioWeb)
            AgregarFila(hoja, "Telefono laboral", txtTelLaboral)
            AgregarFila(hoja, "Telefono particular", txtTelParticular)
            AgregarFila(hoja, "Celular", txtCelular)
            AgregarFila(hoja, "Fax", txtFax)
            hoja.Controls.Add(Etiqueta("Contactos"))
            hoja.Controls.Add(txtContactos)
            hoja.Controls.Add(Etiqueta("Telefonos"))
            hoja.Controls.Add(txtTelefonos)
            return HojaDe(hoja, "Contacto")
        End Function

        Private Function ArmarComercial() As TabPage
            Dim hoja = CrearHoja()

            txtCondicion = Tema.CrearInput()
            txtFormaPago = Tema.CrearInput()
            txtBanco = Tema.CrearInput()
            txtCBU = Tema.CrearInput() : txtCBU.MaxLength = 30
            txtAlias = Tema.CrearInput() : txtAlias.MaxLength = 40
            numDescuentoGeneral = DecimalBox()
            numDescuentoVolumen = DecimalBox()
            txtObservacionesComerciales = CampoAlto(80)
            txtObservacionesRapidas = CampoAlto(80)

            AgregarFila(hoja, "Condicion comercial", txtCondicion)
            AgregarFila(hoja, "Forma de pago habitual", txtFormaPago)
            AgregarFila(hoja, "Banco", txtBanco)
            AgregarFila(hoja, "CBU", txtCBU)
            AgregarFila(hoja, "Alias", txtAlias)
            AgregarFila(hoja, "Descuento general %", numDescuentoGeneral)
            AgregarFila(hoja, "Descuento por volumen %", numDescuentoVolumen)
            hoja.Controls.Add(Etiqueta("Observaciones rapidas"))
            hoja.Controls.Add(txtObservacionesRapidas)
            hoja.Controls.Add(Etiqueta("Observaciones comerciales"))
            hoja.Controls.Add(txtObservacionesComerciales)
            return HojaDe(hoja, "Comercial")
        End Function

        Private Function ArmarNotas() As TabPage
            Dim hoja = CrearHoja()

            txtObservaciones = CampoAlto(70)
            txtNotasInternas = CampoAlto(70)
            txtArchivos = CampoAlto(70)
            txtMovimientos = CampoAlto(110)

            lstHistorial = Tema.CrearLista()
            lstHistorial.Dock = DockStyle.Fill

            hoja.Controls.Add(Etiqueta("Observaciones"))
            hoja.Controls.Add(txtObservaciones)
            hoja.Controls.Add(Etiqueta("Notas internas"))
            hoja.Controls.Add(txtNotasInternas)
            hoja.Controls.Add(Etiqueta("Archivos"))
            hoja.Controls.Add(txtArchivos)
            hoja.Controls.Add(Etiqueta("Movimientos"))
            hoja.Controls.Add(txtMovimientos)
            hoja.Controls.Add(Etiqueta("Historial de cambios"))
            hoja.Controls.Add(lstHistorial)
            return HojaDe(hoja, "Notas y movimientos")
        End Function

        Private Shared Function DecimalBox() As NumericUpDown
            Return New NumericUpDown With {
                .Name = "th.campo",
                .Dock = DockStyle.Fill,
                .DecimalPlaces = 2,
                .Minimum = 0D,
                .Maximum = 100D,
                .Font = Tema.Fuente(Tema.TamNormal),
                .BackColor = Tema.Superficie2,
                .ForeColor = Tema.TextoPrinc,
                .TextAlign = HorizontalAlignment.Right
            }
        End Function

        Private Shared Function HojaDe(hoja As TableLayoutPanel, titulo As String) As TabPage
            Dim pagina As New TabPage With {
                .Name = "th.tarjeta",
                .Text = titulo,
                .BackColor = Tema.Superficie,
                .UseVisualStyleBackColor = False
            }
            pagina.Controls.Add(hoja)
            Return pagina
        End Function
    End Class

End Namespace