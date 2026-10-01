Imports System.Globalization
Imports System.Linq
Imports System.Text.RegularExpressions
Imports System.Windows.Forms

Namespace CafeteriaOS

    ''' <summary>
    ''' Alta y edicion de proveedores. Todos los campos editables se guardan: las listas de
    ''' contactos, telefonos, archivos y movimientos se leen del texto y vuelven a las
    ''' listas del proveedor, y cada guardado deja una linea en el historial de cambios.
    ''' </summary>
    Partial Class ProveedorForm
        Inherits Form

        Private proveedor As Proveedor
        Private esNuevo As Boolean

        Private WithEvents btnGuardar As Button
        Private WithEvents btnNuevo As Button
        Private WithEvents btnCancelar As Button

        Private txtCodigo As TextBox
        Private txtNombre As TextBox
        Private txtDomicilio As TextBox
        Private txtCodigoPostal As TextBox
        Private cmbTipoIVA As ComboBox
        Private txtCUIT As TextBox
        Private cmbTipoDocumento As ComboBox
        Private txtNumeroDocumento As TextBox
        Private txtPais As TextBox
        Private txtProvincia As TextBox
        Private txtLocalidad As TextBox
        Private txtActividad As TextBox
        Private txtCategoria As TextBox
        Private chkActivo As CheckBox
        Private dtAlta As DateTimePicker

        Private txtContactoPrincipal As TextBox
        Private txtEmail As TextBox
        Private txtSitioWeb As TextBox
        Private txtTelLaboral As TextBox
        Private txtTelParticular As TextBox
        Private txtCelular As TextBox
        Private txtFax As TextBox
        Private txtContactos As TextBox
        Private txtTelefonos As TextBox

        Private txtCondicion As TextBox
        Private txtFormaPago As TextBox
        Private txtBanco As TextBox
        Private txtCBU As TextBox
        Private txtAlias As TextBox
        Private numDescuentoGeneral As NumericUpDown
        Private numDescuentoVolumen As NumericUpDown
        Private txtObservacionesComerciales As TextBox
        Private txtObservacionesRapidas As TextBox

        Private txtObservaciones As TextBox
        Private txtNotasInternas As TextBox
        Private txtArchivos As TextBox
        Private txtMovimientos As TextBox
        Private lstHistorial As ListBox

        Public Sub New(proveedor As Proveedor)
            Me.proveedor = proveedor
            esNuevo = proveedor Is Nothing
            If esNuevo Then
                Me.proveedor = New Proveedor With {
                    .Pais = "Argentina",
                    .Activo = True,
                    .FechaAlta = DateTime.Today
                }
            End If
            IniciarInterfaz()
            Cargar()
        End Sub

        Public Shared Function Editar(proveedor As Proveedor) As Boolean
            If proveedor Is Nothing Then Return False
            Using f As New ProveedorForm(proveedor)
                Return f.ShowDialog() = DialogResult.OK
            End Using
        End Function

        Public Shared Function Crear() As Proveedor
            Using f As New ProveedorForm(Nothing)
                If f.ShowDialog() <> DialogResult.OK Then Return Nothing
                Return f.proveedor
            End Using
        End Function

        #Region "Carga y guardado"

        Private Sub Cargar()
            With Me.proveedor
                txtCodigo.Text = .Codigo
                txtNombre.Text = .Nombre
                txtDomicilio.Text = .Domicilio
                txtCodigoPostal.Text = .CodigoPostal
                cmbTipoIVA.SelectedItem = .TipoIVA
                txtCUIT.Text = .CUIT
                cmbTipoDocumento.SelectedItem = .TipoDocumento
                txtNumeroDocumento.Text = .NumeroDocumento
                txtPais.Text = .Pais
                txtProvincia.Text = .Provincia
                txtLocalidad.Text = .Localidad
                txtActividad.Text = .Actividad
                txtCategoria.Text = .Categoria
                chkActivo.Checked = .Activo
                dtAlta.Value = If(.FechaAlta = DateTime.MinValue, DateTime.Today, .FechaAlta)

                txtContactoPrincipal.Text = .ContactoPrincipal
                txtEmail.Text = .Email
                txtSitioWeb.Text = .SitioWeb
                txtTelLaboral.Text = .TelefonoLaboral
                txtTelParticular.Text = .TelefonoParticular
                txtCelular.Text = .Celular
                txtFax.Text = .Fax
                txtContactos.Text = ProveedorTexto.ContactosATexto(.Contactos)
                txtTelefonos.Text = ProveedorTexto.TelefonosATexto(.Telefonos)

                txtCondicion.Text = .CondicionComercial
                txtFormaPago.Text = .FormaPagoHabitual
                txtBanco.Text = .Banco
                txtCBU.Text = .CBU
                txtAlias.Text = .Alias
                numDescuentoGeneral.Value = Limitar(CDec(.DescuentoGeneral))
                numDescuentoVolumen.Value = Limitar(CDec(.DescuentoPorVolumen))
                txtObservacionesComerciales.Text = .ObservacionesComerciales
                txtObservacionesRapidas.Text = .ObservacionesRapidas

                txtObservaciones.Text = .Observaciones
                txtNotasInternas.Text = .NotasInternas
                txtArchivos.Text = ProveedorTexto.ArchivosATexto(.Archivos)
                txtMovimientos.Text = ProveedorTexto.MovimientosATexto(.Movimientos)
            End With

            lstHistorial.Items.Clear()
            lstHistorial.Items.AddRange(Me.proveedor.HistorialCambios.ToArray())
            If lstHistorial.Items.Count > 0 Then
                lstHistorial.TopIndex = lstHistorial.Items.Count - 1
            End If

            If esNuevo Then
                txtCodigo.Text = ProximoCodigo()
                txtCodigo.ReadOnly = True
            End If
            txtNombre.Focus()
        End Sub

        Private Shared Function Limitar(valor As Decimal) As Decimal
            Return Math.Max(0D, Math.Min(100D, valor))
        End Function

        Private Sub Guardar()
            If String.IsNullOrWhiteSpace(txtNombre.Text) Then
                MessageBox.Show("El nombre del proveedor es obligatorio.", "Proveedor",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtNombre.Focus()
                Return
            End If

            Dim codigo = If(String.IsNullOrWhiteSpace(txtCodigo.Text), ProximoCodigo(), txtCodigo.Text.Trim())
            Dim repetido = Datos.ListaProveedores.Any(
                Function(p) p.Codigo.Equals(codigo, StringComparison.OrdinalIgnoreCase) AndAlso
                            Not ReferenceEquals(p, Me.proveedor))
            If repetido Then
                MessageBox.Show("Ya existe un proveedor con el codigo " & codigo & ".", "Proveedor",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            With Me.proveedor
                .Codigo = codigo
                .Nombre = txtNombre.Text.Trim()
                .Domicilio = txtDomicilio.Text.Trim()
                .CodigoPostal = txtCodigoPostal.Text.Trim()
                .TipoIVA = Elegido(cmbTipoIVA)
                .CUIT = txtCUIT.Text.Trim()
                .TipoDocumento = Elegido(cmbTipoDocumento)
                .NumeroDocumento = txtNumeroDocumento.Text.Trim()
                .Pais = txtPais.Text.Trim()
                .Provincia = txtProvincia.Text.Trim()
                .Localidad = txtLocalidad.Text.Trim()
                .Actividad = txtActividad.Text.Trim()
                .Categoria = txtCategoria.Text.Trim()
                .Activo = chkActivo.Checked
                .FechaAlta = dtAlta.Value.Date

                .ContactoPrincipal = txtContactoPrincipal.Text.Trim()
                .Email = txtEmail.Text.Trim()
                .SitioWeb = txtSitioWeb.Text.Trim()
                .TelefonoLaboral = txtTelLaboral.Text.Trim()
                .TelefonoParticular = txtTelParticular.Text.Trim()
                .Celular = txtCelular.Text.Trim()
                .Fax = txtFax.Text.Trim()
                .Contactos = ProveedorTexto.ContactosDesdeTexto(txtContactos.Text)
                .Telefonos = ProveedorTexto.TelefonosDesdeTexto(txtTelefonos.Text)

                .CondicionComercial = txtCondicion.Text.Trim()
                .FormaPagoHabitual = txtFormaPago.Text.Trim()
                .Banco = txtBanco.Text.Trim()
                .CBU = NormalizarCbu(txtCBU.Text)
                .Alias = txtAlias.Text.Trim()
                .DescuentoGeneral = numDescuentoGeneral.Value
                .DescuentoPorVolumen = numDescuentoVolumen.Value
                .ObservacionesComerciales = txtObservacionesComerciales.Text.Trim()
                .ObservacionesRapidas = txtObservacionesRapidas.Text.Trim()

                .Observaciones = txtObservaciones.Text.Trim()
                .NotasInternas = txtNotasInternas.Text.Trim()
                .Archivos = ProveedorTexto.ArchivosDesdeTexto(txtArchivos.Text)
                .Movimientos = ProveedorTexto.MovimientosDesdeTexto(txtMovimientos.Text)

                .SaldoPendiente = .Movimientos.Where(Function(m) Not m.Pagado).Sum(Function(m) m.Monto)
                .TotalCompras = .Movimientos.Sum(Function(m) m.Monto)
                .CantidadOrdenes = .Movimientos.Count
                .UltimaCompra = If(.Movimientos.Count = 0, Nothing,
                                   CDate(.Movimientos.Max(Function(m) m.Fecha)))
                .FechaUltimaOperacion = .UltimaCompra
                .ModificadoPor = Datos.UsuarioActualNombre()
                .FechaModificacion = DateTime.Now
            End With

            If esNuevo Then
                proveedor.CreadoPor = Datos.UsuarioActualNombre()
                proveedor.FechaCreacion = DateTime.Now
                Datos.ListaProveedores.Add(proveedor)
                esNuevo = False
            End If

            proveedor.HistorialCambios.Add(
                DateTime.Now.ToString("dd/MM/yyyy HH:mm") & " - " &
                Datos.UsuarioActualNombre() & " actualizo los datos del proveedor")
            PintarHistorial()

            DialogResult = DialogResult.OK
            Close()
        End Sub

        Private Sub PintarHistorial()
            lstHistorial.Items.Clear()
            lstHistorial.Items.AddRange(proveedor.HistorialCambios.ToArray())
            If lstHistorial.Items.Count > 0 Then
                lstHistorial.TopIndex = lstHistorial.Items.Count - 1
            End If
        End Sub

        Private Shared Function Elegido(combo As ComboBox) As String
            If combo.SelectedItem Is Nothing Then Return String.Empty
            Return combo.SelectedItem.ToString()
        End Function

        ''' <summary>El CBU son 22 digitos: se limpia para no guardar espacios ni guiones.</summary>
        Private Shared Function NormalizarCbu(texto As String) As String
            Return Regex.Replace(If(texto, String.Empty), "[^0-9]", "")
        End Function

        Private Shared Function ProximoCodigo() As String
            Dim maximo = Datos.ListaProveedores.
                Where(Function(p) Regex.IsMatch(If(p.Codigo, String.Empty), "^PRV\d+$")).
                Select(Function(p) Integer.Parse(Regex.Match(p.Codigo, "\d+").Value)).
                DefaultIfEmpty(0).
                Max()
            Return "PRV" & (maximo + 1).ToString("000")
        End Function

        #End Region

        #Region "Botones"

        Private Sub btnGuardar_Click(sender As Object, e As EventArgs) Handles btnGuardar.Click
            Guardar()
        End Sub

        ''' <summary>"Nuevo" limpia el formulario para cargar otro proveedor desde cero.</summary>
        Private Sub btnNuevo_Click(sender As Object, e As EventArgs) Handles btnNuevo.Click
            If Not Confirmar("Se perderan los datos escritos en el formulario. Continuar?") Then Return
            proveedor = New Proveedor With {
                .Pais = "Argentina",
                .Activo = True,
                .FechaAlta = DateTime.Today
            }
            esNuevo = True
            Cargar()
        End Sub

        Private Sub btnCancelar_Click(sender As Object, e As EventArgs) Handles btnCancelar.Click
            DialogResult = DialogResult.Cancel
            Close()
        End Sub

        Private Shared Function Confirmar(mensaje As String) As Boolean
            Return MessageBox.Show(mensaje, "Proveedor", MessageBoxButtons.OKCancel,
                                   MessageBoxIcon.Question) = DialogResult.OK
        End Function

        #End Region
    End Class

End Namespace