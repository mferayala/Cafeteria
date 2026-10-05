Imports System.Drawing
Imports System.Globalization
Imports System.Linq
Imports System.Windows.Forms

Namespace CafeteriaOS

    ''' <summary>
    ''' Administracion de categorias y subcategorias del catalogo.
    '''
    ''' Sin esta pantalla una categoria existe recien cuando un producto la usa, y
    ''' cambiar un nombre obliga a editar producto por producto. Aca se crea una
    ''' categoria vacia, se renombra una sola vez y se borra reasignando los
    ''' productos que la usaban.
    '''
    ''' Borrar una categoria nunca borra productos: pide a que categoria pasan, o los
    ''' deja sin categoria si no hay a cual moverlos.
    '''
    ''' Devuelve True si se toco algo, para que el panel de Stock se refresque.
    ''' </summary>
    Partial Class CategoriasForm
        Inherits Form

        Private WithEvents txtBuscar As TextBox
        Private WithEvents tabla As DataGridView
        Private WithEvents lblDetalle As Label

        Private WithEvents txtNombre As TextBox
        Private WithEvents btnAgregar As Button
        Private WithEvents btnRenombrar As Button
        Private WithEvents btnBorrar As Button
        Private WithEvents btnCerrar As Button

        Private WithEvents cmbSubcategoria As ComboBox
        Private WithEvents txtSubcategoria As TextBox
        Private WithEvents btnAgregarSub As Button
        Private WithEvents btnBorrarSub As Button

        Private filtradas As New System.Collections.Generic.List(Of Categoria)()

        Public Sub New()
            Text = "Categorias del catalogo"
            StartPosition = FormStartPosition.CenterParent
            ' Redimensionable a proposito: es la unica de esta tanda que se usa para
            ' dar de alta Providers de verdad y la lista crece. Maximizar no.
            MaximizeBox = False
            ClientSize = New Size(880, 620)
            ' Sin esto WinForms escala con los valores por defecto y no con los que
            ' escribe el Designer: a 125% o 150% la fuente crece y las columnas no,
            ' y los labels quedan cortados.
            AutoScaleMode = AutoScaleMode.Dpi
            AutoScaleDimensions = New SizeF(96, 96)
            MinimumSize = New Size(880, 620)
            BackColor = Tema.BgPrincipal
            Font = Tema.Fuente(Tema.TamNormal)

            ArmarLayout()
            Refrescar()
        End Sub

        Public Shared Function Editar() As Boolean
            Using f As New CategoriasForm()
                f.ShowDialog()
            End Using
            Return True
        End Function

        Private Sub ArmarLayout()
            Dim raiz As New TableLayoutPanel With {
                .Name = "th.fondo",
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 4,
                .BackColor = Tema.BgPrincipal,
                .Padding = New Padding(12)
            }
            raiz.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            raiz.RowStyles.Add(New RowStyle(SizeType.Absolute, 34))
            raiz.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            raiz.RowStyles.Add(New RowStyle(SizeType.Absolute, 30))
            raiz.RowStyles.Add(New RowStyle(SizeType.Absolute, 150))

            txtBuscar = Tema.CrearInput()
            txtBuscar.Dock = DockStyle.Fill
            txtBuscar.AccessibleName = "Buscar categoria"
            AddHandler txtBuscar.TextChanged, Sub() Refrescar()
            raiz.Controls.Add(txtBuscar, 0, 0)

            tabla = Tema.CrearTabla()
            tabla.Dock = DockStyle.Fill
            tabla.ReadOnly = True
            tabla.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            tabla.MultiSelect = False
            tabla.RowHeadersVisible = False
            UiKit.Columna(tabla, "Categoria", 260)
            UiKit.Columna(tabla, "Subcategorias", 420)
            UiKit.Columna(tabla, "Productos", 120)
            AddHandler tabla.SelectionChanged, Sub() AlCambiarLaFila()
            raiz.Controls.Add(tabla, 0, 1)

            lblDetalle = Tema.CrearLabel("", Tono.Secundario, Tema.TamMini)
            lblDetalle.Dock = DockStyle.Fill
            lblDetalle.TextAlign = ContentAlignment.MiddleLeft
            raiz.Controls.Add(lblDetalle, 0, 2)

            raiz.Controls.Add(ArmarZonaDeEdicion(), 0, 3)

            Controls.Add(raiz)
        End Sub

        ''' <summary>
        ''' Abajo, a la izquierda, se agrega o renombra una categoria; a la derecha se
        ''' administra la subcategoria de la que esta seleccionada. Estan separadas
        ''' porque la subcategoria siempre depende de una categoria elegida.
        ''' </summary>
        Private Function ArmarZonaDeEdicion() As Control
            Dim zona As New TableLayoutPanel With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 2,
                .RowCount = 1,
                .BackColor = Color.Transparent
            }
            zona.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 52))
            zona.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 48))
            zona.Controls.Add(ArmarBloqueCategorias(), 0, 0)
            zona.Controls.Add(ArmarBloqueSubcategorias(), 1, 0)
            Return zona
        End Function

        Private Function ArmarBloqueCategorias() As Control
            Dim caja As New TableLayoutPanel With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 2,
                .RowCount = 3,
                .BackColor = Tema.Superficie,
                .Padding = New Padding(10)
            }
            caja.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            caja.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 200))
            caja.RowStyles.Add(New RowStyle(SizeType.Absolute, 26))
            caja.RowStyles.Add(New RowStyle(SizeType.Absolute, 44))
            caja.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

            Dim titulo = Tema.CrearLabel("CATEGORIAS", Tono.Primario, Tema.TamMini, FontStyle.Bold)
            titulo.Dock = DockStyle.Fill
            titulo.TextAlign = ContentAlignment.MiddleLeft
            caja.Controls.Add(titulo, 0, 0)
            caja.SetColumnSpan(titulo, 2)

            txtNombre = Tema.CrearInput()
            txtNombre.Dock = DockStyle.Fill
            txtNombre.AccessibleName = "Nombre de la categoria"
            AddHandler txtNombre.KeyDown, Sub(sender, ev) SiEsEnter(sender, ev, Sub() Agregar())
            caja.Controls.Add(txtNombre, 0, 1)

            Dim botones As New FlowLayoutPanel With {
                .Dock = DockStyle.Fill,
                .FlowDirection = FlowDirection.LeftToRight,
                .WrapContents = False,
                .BackColor = Color.Transparent,
                .Padding = New Padding(8, 6, 0, 0)
            }
            btnAgregar = Tema.CrearBoton("Agregar", EstiloBoton.Primario, 92, 32)
            btnRenombrar = Tema.CrearBoton("Renombrar", EstiloBoton.Neutro, 96, 32)
            btnBorrar = Tema.CrearBoton("Borrar", EstiloBoton.Peligro, 84, 32)
            AddHandler btnAgregar.Click, Sub() Agregar()
            AddHandler btnRenombrar.Click, Sub() Renombrar()
            AddHandler btnBorrar.Click, Sub() Borrar()
            botones.Controls.AddRange(New Control() {btnAgregar, btnRenombrar, btnBorrar})
            caja.Controls.Add(botones, 1, 1)

            Dim ayuda = Tema.CrearLabel(
                "Renombrar actualiza todos los productos que usan la categoria." &
                Environment.NewLine &
                "Borrar pide a que categoria pasan esos productos; no borra productos.",
                Tono.Secundario, Tema.TamMini)
            ayuda.Dock = DockStyle.Fill
            ayuda.AutoSize = False
            ayuda.TextAlign = ContentAlignment.TopLeft
            caja.Controls.Add(ayuda, 0, 2)
            caja.SetColumnSpan(ayuda, 2)

            Return caja
        End Function

        Private Function ArmarBloqueSubcategorias() As Control
            Dim caja As New TableLayoutPanel With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 2,
                .RowCount = 4,
                .BackColor = Tema.Superficie,
                .Padding = New Padding(10)
            }
            caja.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            caja.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 200))
            caja.RowStyles.Add(New RowStyle(SizeType.Absolute, 26))
            caja.RowStyles.Add(New RowStyle(SizeType.Absolute, 30))
            caja.RowStyles.Add(New RowStyle(SizeType.Absolute, 44))
            caja.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

            Dim titulo = Tema.CrearLabel("SUBCATEGORIAS DE LA ELEGIDA", Tono.Primario,
                                          Tema.TamMini, FontStyle.Bold)
            titulo.Dock = DockStyle.Fill
            titulo.TextAlign = ContentAlignment.MiddleLeft
            caja.Controls.Add(titulo, 0, 0)
            caja.SetColumnSpan(titulo, 2)

            cmbSubcategoria = Tema.CrearCombo()
            cmbSubcategoria.DropDownStyle = ComboBoxStyle.DropDown
            AddHandler cmbSubcategoria.SelectedIndexChanged,
                Sub() CargarSubcategoriaSeleccionada()
            caja.Controls.Add(cmbSubcategoria, 0, 1)
            caja.SetColumnSpan(cmbSubcategoria, 2)

            txtSubcategoria = Tema.CrearInput()
            txtSubcategoria.Dock = DockStyle.Fill
            txtSubcategoria.AccessibleName = "Nombre de la subcategoria"
            AddHandler txtSubcategoria.KeyDown,
                Sub(sender, ev) SiEsEnter(sender, ev, Sub() AgregarSubcategoria())
            caja.Controls.Add(txtSubcategoria, 0, 2)

            Dim botones As New FlowLayoutPanel With {
                .Dock = DockStyle.Fill,
                .FlowDirection = FlowDirection.LeftToRight,
                .WrapContents = False,
                .BackColor = Color.Transparent,
                .Padding = New Padding(8, 6, 0, 0)
            }
            btnAgregarSub = Tema.CrearBoton("Agregar", EstiloBoton.Primario, 92, 32)
            btnBorrarSub = Tema.CrearBoton("Quitar", EstiloBoton.Peligro, 84, 32)
            btnCerrar = Tema.CrearBoton("Cerrar", EstiloBoton.Neutro, 92, 32)
            AddHandler btnAgregarSub.Click, Sub() AgregarSubcategoria()
            AddHandler btnBorrarSub.Click, Sub() BorrarSubcategoria()
            AddHandler btnCerrar.Click, Sub() Close()
            botones.Controls.AddRange(New Control() {btnCerrar, btnBorrarSub, btnAgregarSub})
            caja.Controls.Add(botones, 1, 2)

            Dim ayuda = Tema.CrearLabel(
                "Las subcategorias son opcionales y viven dentro de una categoria.",
                Tono.Secundario, Tema.TamMini)
            ayuda.Dock = DockStyle.Fill
            ayuda.TextAlign = ContentAlignment.TopLeft
            caja.Controls.Add(ayuda, 0, 3)
            caja.SetColumnSpan(ayuda, 2)

            Return caja
        End Function

        Private Sub SiEsEnter(sender As Object, ev As KeyEventArgs, accion As Action)
            If ev.KeyCode <> Keys.Enter Then Return
            ev.SuppressKeyPress = True
            ev.Handled = True
            accion()
        End Sub

        Private Sub Refrescar()
            Dim texto = If(txtBuscar.Text, "").Trim()

            filtradas = CategoriaService.Todas().
                Where(Function(c) texto.Length = 0 OrElse
                               c.Nombre.IndexOf(texto, StringComparison.CurrentCultureIgnoreCase) >= 0).
                ToList()

            tabla.Rows.Clear()
            For Each c In filtradas
                Dim productos = Datos.ListaProductos.Where(
                    Function(p) If(p.Categoria, "").Trim().Equals(
                        c.Nombre, StringComparison.CurrentCultureIgnoreCase)).Count()
                tabla.Rows.Add(c.Nombre, c.Subcategorias.Count.ToString(), productos.ToString())
            Next

            If tabla.Rows.Count > 0 Then tabla.Rows(0).Selected = True
            ActualizarDetalle()
        End Sub

        Private Function Seleccionada() As Categoria
            If tabla.CurrentRow Is Nothing Then Return Nothing
            Dim nombre = If(tabla.CurrentRow.Cells(0).Value, "").ToString()
            Return CategoriaService.Buscar(nombre)
        End Function

        Private Sub AlCambiarLaFila()
            ActualizarDetalle()
            CargarSubcategoriaSeleccionada()
        End Sub

        Private Sub ActualizarDetalle()
            Dim c = Seleccionada()
            If c Is Nothing Then
                lblDetalle.Text = "No hay categorias que mostrar."
                HabilitarEdicionDeSubcategoria(False)
                Return
            End If

            Dim productos = Datos.ListaProductos.Where(
                Function(p) If(p.Categoria, "").Trim().Equals(
                    c.Nombre, StringComparison.CurrentCultureIgnoreCase)).Count()
            lblDetalle.Text = c.Subcategorias.Count.ToString() & " subcategoria(s), " &
                              productos.ToString() & " producto(s)."
            HabilitarEdicionDeSubcategoria(True)
        End Sub

        Private Sub HabilitarEdicionDeSubcategoria(habilitar As Boolean)
            If Not habilitar Then
                btnAgregarSub.Enabled = False
                btnBorrarSub.Enabled = False
                txtSubcategoria.Enabled = False
                cmbSubcategoria.Enabled = False
                Return
            End If

            Dim c = Seleccionada()
            Dim hay = c IsNot Nothing
            btnAgregarSub.Enabled = hay
            btnBorrarSub.Enabled = hay
            txtSubcategoria.Enabled = hay
            cmbSubcategoria.Enabled = hay
        End Sub

        Private Sub Agregar()
            Dim resultado = CategoriaService.Agregar(txtNombre.Text)
            If Not resultado.Ok Then
                Avisar(resultado.Mensaje)
                Return
            End If
            txtNombre.Clear()
            Refrescar()
        End Sub

        Private Sub Renombrar()
            Dim c = Seleccionada()
            If c Is Nothing Then
                Avisar("Elegi la categoria a renombrar.")
                Return
            End If

            Dim nuevo = If(txtNombre.Text, "").Trim()
            If nuevo.Length = 0 Then
                Avisar("Escribi el nombre nuevo.")
                txtNombre.Focus()
                Return
            End If

            Dim resultado = CategoriaService.Renombrar(c.Nombre, nuevo)
            If Not resultado.Ok Then
                Avisar(resultado.Mensaje)
                Return
            End If
            txtNombre.Clear()
            Refrescar()
        End Sub

        Private Sub Borrar()
            Dim c = Seleccionada()
            If c Is Nothing Then
                Avisar("Elegi la categoria a borrar.")
                Return
            End If

            Dim productos = Datos.ListaProductos.Where(
                Function(p) If(p.Categoria, "").Trim().Equals(
                    c.Nombre, StringComparison.CurrentCultureIgnoreCase)).Count()

            Dim otras = CategoriaService.Todas().
                Where(Function(x) Not ReferenceEquals(x, c)).ToList()

            Dim detalle = "Borrar la categoria """ & c.Nombre & """?"
            If productos > 0 Then
                detalle &= Environment.NewLine & Environment.NewLine &
                           productos.ToString() & " producto(s) la usan." & Environment.NewLine
                If otras.Count = 0 Then
                    detalle &= "No hay otra categoria a la que mudarlos: quedan sin categoria."
                Else
                    detalle &= "Elegi a que categoria pasan. Cancelar los deja sin categoria."
                End If
            End If

            Dim reemplazo = ""
            If otras.Count > 0 Then
                Dim nombres = otras.Select(Function(x) x.Nombre).ToArray()
                Dim elegido = ElegirOpcional("A que categoria pasan esos productos?", nombres)
                If elegido IsNot Nothing Then reemplazo = elegido
            End If

            Dim confirmado = MessageBox.Show(detalle, "Borrar categoria",
                                             MessageBoxButtons.OKCancel,
                                             MessageBoxIcon.Warning)
            If confirmado <> DialogResult.OK Then Return

            Dim resultado = CategoriaService.Eliminar(c.Nombre, reemplazo)
            If Not resultado.Ok Then
                Avisar(resultado.Mensaje)
                Return
            End If
            Refrescar()
        End Sub

        ''' <summary>
        ''' ChoiceBox sin la X de cancelar: es un elegir-reemplazo opcional, y el
        ''' boton de cancelar del MessageBox ya cubre dejar los productos sin
        ''' categoria.
        ''' </summary>
        Private Function ElegirOpcional(titulo As String, opciones As String()) As String
            Using f As New Form With {
                .Text = titulo,
                .StartPosition = FormStartPosition.CenterParent,
                .FormBorderStyle = FormBorderStyle.FixedDialog,
                .MinimizeBox = False,
                .MaximizeBox = False,
                .ClientSize = New Size(420, 140),
                .BackColor = Tema.BgPrincipal,
                .Font = Tema.Fuente(Tema.TamNormal)
            }
                Dim combo = Tema.CrearCombo()
                combo.Dock = DockStyle.Top
                combo.Items.AddRange(opciones)
                combo.SelectedIndex = 0

                Dim aceptar = Tema.CrearBoton("Aceptar", EstiloBoton.Primario, 110, 34)
                Dim barra As New FlowLayoutPanel With {
                    .Dock = DockStyle.Bottom,
                    .Height = 46,
                    .FlowDirection = FlowDirection.RightToLeft,
                    .BackColor = Color.Transparent
                }
                barra.Controls.Add(aceptar)
                AddHandler aceptar.Click, Sub() f.DialogResult = DialogResult.OK

                f.Controls.Add(combo)
                f.Controls.Add(barra)
                f.AcceptButton = aceptar

                If f.ShowDialog() = DialogResult.OK Then
                    Return If(combo.SelectedItem, "").ToString()
                End If
            End Using
            Return Nothing
        End Function

        Private Sub CargarSubcategoriaSeleccionada()
            Dim c = Seleccionada()
            Dim actual = If(cmbSubcategoria.SelectedItem, "").ToString()

            cmbSubcategoria.Items.Clear()
            If c IsNot Nothing Then
                For Each s In CategoriaService.SubcategoriasDe(c.Nombre)
                    cmbSubcategoria.Items.Add(s)
                Next
            End If

            Dim indice = cmbSubcategoria.Items.IndexOf(actual)
            If indice >= 0 Then
                cmbSubcategoria.SelectedIndex = indice
            ElseIf cmbSubcategoria.Items.Count > 0 Then
                cmbSubcategoria.SelectedIndex = 0
            Else
                cmbSubcategoria.SelectedIndex = -1
            End If
        End Sub

        Private Sub AgregarSubcategoria()
            Dim c = Seleccionada()
            If c Is Nothing Then
                Avisar("Elegi una categoria primero.")
                Return
            End If

            Dim resultado = CategoriaService.AgregarSubcategoria(c.Nombre, txtSubcategoria.Text)
            If Not resultado.Ok Then
                Avisar(resultado.Mensaje)
                Return
            End If
            txtSubcategoria.Clear()
            Refrescar()
            CargarSubcategoriaSeleccionada()
        End Sub

        Private Sub BorrarSubcategoria()
            Dim c = Seleccionada()
            If c Is Nothing Then
                Avisar("Elegi una categoria primero.")
                Return
            End If

            ' "sub" es palabra clave en VB.
            Dim elegida = If(cmbSubcategoria.SelectedItem, "").ToString()
            If elegida.Length = 0 Then
                Avisar("Elegi la subcategoria a quitar.")
                Return
            End If

            Dim resultado = CategoriaService.EliminarSubcategoria(c.Nombre, elegida)
            If Not resultado.Ok Then
                Avisar(resultado.Mensaje)
                Return
            End If
            Refrescar()
            CargarSubcategoriaSeleccionada()
        End Sub

        Private Sub Avisar(mensaje As String)
            MessageBox.Show(mensaje, "Categorias", MessageBoxButtons.OK,
                            MessageBoxIcon.Warning)
        End Sub

    End Class

End Namespace
