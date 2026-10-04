Imports System.Drawing
Imports System.Globalization
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Windows.Forms

Namespace CafeteriaOS

    ''' <summary>
    ''' Listado de proveedores con busqueda, alta, edicion, baja y exportacion. Los
    ''' permisos se consultan en AutenticacionService: cocina puede ver, no puede tocar.
    ''' </summary>
    Public Class ProveedoresPanel
        Inherits PanelBase

        Private Const Comilla As String = Chr(34)

        Private WithEvents txtBuscar As TextBox
        Private WithEvents chkInactivos As CheckBox
        Private tabla As DataGridView
        Private btnNuevo As Button
        Private btnEditar As Button
        Private btnIngreso As Button
        Private btnEliminar As Button
        Private btnExportar As Button

        Public Sub New()
            MyBase.New("Proveedores", "Alta, edicion y estado de cuentas")
            ArmarLayout()
        End Sub

        Private Sub ArmarLayout()
            Dim raiz As New TableLayoutPanel With {
                .Name = "th.fondo",
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 3,
                .BackColor = Tema.BgPrincipal
            }
            raiz.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            raiz.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            raiz.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            raiz.RowStyles.Add(New RowStyle(SizeType.AutoSize))

            txtBuscar = Tema.CrearInput()
            txtBuscar.Dock = DockStyle.Fill
            txtBuscar.AccessibleName = "Buscar proveedor"
            chkInactivos = Tema.CrearCheck("Mostrar inactivos")

            Dim filtros As New TableLayoutPanel With {
                .Name = "th.fondo",
                .Dock = DockStyle.Fill,
                .ColumnCount = 2,
                .RowCount = 1,
                .BackColor = Color.Transparent,
                .Height = 34
            }
            filtros.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            filtros.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
            filtros.Controls.Add(txtBuscar, 0, 0)
            filtros.Controls.Add(chkInactivos, 1, 0)
            raiz.Controls.Add(filtros, 0, 0)

            tabla = Tema.CrearTabla()
            tabla.Dock = DockStyle.Fill
            UiKit.Columna(tabla, "Codigo", 90)
            UiKit.Columna(tabla, "Nombre", 220)
            UiKit.Columna(tabla, "CUIT", 150)
            UiKit.Columna(tabla, "Localidad", 160)
            UiKit.Columna(tabla, "Telefono", 130)
            UiKit.Columna(tabla, "Saldo", 110)
            UiKit.Columna(tabla, "Estado", 90)
            tabla.Columns(0).FillWeight = 10
            tabla.Columns(1).FillWeight = 28
            tabla.Columns(2).FillWeight = 18
            tabla.Columns(3).FillWeight = 20
            tabla.Columns(4).FillWeight = 16
            tabla.Columns(5).FillWeight = 12
            tabla.Columns(6).FillWeight = 12
            raiz.Controls.Add(tabla, 0, 1)

            btnNuevo = Tema.CrearBoton("Nuevo proveedor", EstiloBoton.Primario, 150, 38)
            btnEditar = Tema.CrearBoton("Editar", EstiloBoton.Neutro, 100, 38)
            btnEliminar = Tema.CrearBoton("Eliminar", EstiloBoton.Peligro, 110, 38)
            btnExportar = Tema.CrearBoton("Exportar CSV", EstiloBoton.Info, 140, 38)
            AddHandler btnNuevo.Click, Sub(s, e) Nuevo()
            AddHandler btnEditar.Click, Sub(s, e) Editar()
            AddHandler btnEliminar.Click, Sub(s, e) Eliminar()
            AddHandler btnExportar.Click, Sub(s, e) Exportar()

            raiz.Controls.Add(UiKit.Botonera(btnNuevo, btnEditar, btnEliminar, btnExportar), 0, 2)
            Contenido.Controls.Add(raiz)
        End Sub

        Public Overrides Sub Refrescar()
            Dim rol = If(Datos.UsuarioActual Is Nothing, RolUsuario.Cocina, Datos.UsuarioActual.Rol)
            Dim puedeEditar = AutenticacionService.PuedeEditarProveedores(rol)
            Dim puedeEliminar = AutenticacionService.PuedeEliminarProveedores(rol)
            btnNuevo.Enabled = puedeEditar
            btnEditar.Enabled = puedeEditar
            btnEliminar.Enabled = puedeEliminar

            Dim texto = If(txtBuscar.Text, String.Empty).Trim().ToLowerInvariant()
            tabla.Rows.Clear()

            Dim proveedores = Datos.ListaProveedores.
                Where(Function(p) chkInactivos.Checked OrElse p.Activo).
                Where(Function(p) texto.Length = 0 OrElse
                               p.Nombre.ToLowerInvariant().Contains(texto) OrElse
                               p.Codigo.ToLowerInvariant().Contains(texto) OrElse
                               p.CUIT.ToLowerInvariant().Contains(texto) OrElse
                               p.Localidad.ToLowerInvariant().Contains(texto)).
                OrderBy(Function(p) p.Codigo)

            For Each p In proveedores
                Dim indice = tabla.Rows.Add(p.Codigo, p.Nombre, p.CUIT, p.Localidad,
                                            If(String.IsNullOrWhiteSpace(p.TelefonoLaboral), p.Celular, p.TelefonoLaboral),
                                            p.SaldoPendiente.ToString("C2", CultureInfo.CurrentCulture),
                                            If(p.Activo, "Activo", "Inactivo"))
                tabla.Rows(indice).Tag = p
                If p.TieneDeudaVencida Then
                    tabla.Rows(indice).DefaultCellStyle.ForeColor = Tema.Rojo
                End If
            Next

            SetearSubtitulo(proveedores.Count() & " proveedor(es)")
        End Sub

        Private Function Seleccionado() As Proveedor
            If tabla.SelectedRows.Count = 0 Then Return Nothing
            Return TryCast(tabla.SelectedRows(0).Tag, Proveedor)
        End Function

        Private Sub Nuevo()
            Dim nuevo = ProveedorForm.Crear()
            If nuevo Is Nothing Then Return
            Refrescar()
        End Sub

        Private Sub Editar()
            Dim p = Seleccionado()
            If p Is Nothing Then
                Avisar("Elegi un proveedor de la lista.")
                Return
            End If
            If ProveedorForm.Editar(p) Then Refrescar()
        End Sub

        ''' <summary>
        ''' Carga la mercaderia que trajo el proveedor elegido: suma el stock de los
        ''' productos y deja registrado de que compra vino cada unidad.
        ''' </summary>
        Private Sub RegistrarIngreso()
            Dim p = Seleccionado()
            If p Is Nothing Then
                Avisar("Elegi un proveedor de la lista.")
                Return
            End If

            Dim ingreso = IngresoStockForm.Registrar(p)
            If ingreso Is Nothing Then Return
            Refrescar()

            SetearSubtitulo("Ultimo ingreso: " & IngresoService.Describir(ingreso) &
                            " (" & ingreso.CantidadTotal & " unidad(es))")
        End Sub

        Private Sub Eliminar()
            Dim p = Seleccionado()
            If p Is Nothing Then
                Avisar("Elegi un proveedor de la lista.")
                Return
            End If
            If MessageBox.Show("Eliminar a " & p.Nombre & " de forma permanente?",
                               "Eliminar proveedor", MessageBoxButtons.OKCancel,
                               MessageBoxIcon.Warning) <> DialogResult.OK Then
                Return
            End If
            Datos.ListaProveedores.Remove(p)
            Refrescar()
        End Sub

        Private Sub Exportar()
            Using sfd As New SaveFileDialog With {
                .Filter = "CSV (*.csv)|*.csv|Todos los archivos (*.*)|*.*",
                .FileName = "proveedores_" & DateTime.Today.ToString("yyyyMMdd") & ".csv"
            }
                If sfd.ShowDialog() <> DialogResult.OK Then Return

                Dim contenido As New StringBuilder()
                contenido.AppendLine("Codigo;Nombre;CUIT;Telefono;Localidad;Activo;SaldoPendiente;TotalCompras")
                For Each p In Datos.ListaProveedores.OrderBy(Function(x) x.Codigo)
                    contenido.AppendLine(String.Join(";", New String() {
                        Escapar(p.Codigo), Escapar(p.Nombre), Escapar(p.CUIT),
                        Escapar(If(String.IsNullOrWhiteSpace(p.TelefonoLaboral), p.Celular, p.TelefonoLaboral)),
                        Escapar(p.Localidad), p.Activo.ToString(),
                        p.SaldoPendiente.ToString("0.00", CultureInfo.InvariantCulture),
                        p.TotalCompras.ToString("0.00", CultureInfo.InvariantCulture)}))
                Next

                ' Con codepage explicito: File.WriteAllText usa UTF-8 y Excel lo rompe.
                ' UTF-8 con BOM: Excel abre el CSV con acentos sin pedir nada y no
                ' depende de la pagina de codigos 1252, ausente en algunos runtimes.
                File.WriteAllText(sfd.FileName, contenido.ToString(), New UTF8Encoding(True))
            End Using
        End Sub

        ''' <summary>Un valor con ";" o salto de linea romperia la fila del CSV.</summary>
        Private Shared Function Escapar(valor As String) As String
            Dim v = If(valor, String.Empty).Replace(Comilla, String.Empty)
            If v.Contains(";") OrElse v.Contains(vbLf) OrElse v.Contains(vbCr) Then
                Return Comilla & v.Replace(vbLf, " ").Replace(vbCr, " ") & Comilla
            End If
            Return v
        End Function

        Private Sub Avisar(mensaje As String)
            MessageBox.Show(mensaje, "Proveedores", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Sub

        Private Sub txtBuscar_TextChanged(sender As Object, e As EventArgs) Handles txtBuscar.TextChanged
            If tabla Is Nothing Then Return
            Refrescar()
        End Sub

        Private Sub chkInactivos_CheckedChanged(sender As Object, e As EventArgs) Handles chkInactivos.CheckedChanged
            If tabla Is Nothing Then Return
            Refrescar()
        End Sub
    End Class

End Namespace