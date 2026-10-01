Imports System.Collections.Generic
Imports System.Windows.Forms

Namespace CafeteriaOS

    ''' <summary>
    ''' Ventana principal: lateral de navegacion y un unico modulo visible a la vez.
    '''
    ''' Los modulos se crean una sola vez y se reutilizan, de modo que cambiar de seccion no
    ''' pierde el pedido en curso, la mesa elegida, el medio de pago, los filtros ni la
    ''' posicion de scroll. El cambio de tema tambien se aplica en el lugar, sobre los
    ''' controles existentes, sin recrear la interfaz.
    ''' </summary>
    Partial Class MainDashboardForm
        Inherits Form

        Private paneles As New Dictionary(Of SeccionDashboard, PanelBase)()
        Private botonera As New Dictionary(Of SeccionDashboard, Button)()
        Private seccionActual As SeccionDashboard = SeccionDashboard.Venta

        Public Sub New()
            IniciarInterfaz()
            ActualizarUsuario()
            Mostrar(SeccionDashboard.Venta)
        End Sub

        Private Sub MainDashboardForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
            IndexarBotones()
            ActualizarUsuario()
        End Sub

        Private Sub IndexarBotones()
            botonera.Clear()
            For Each control As Control In Controls
                IndexarBotonesDe(control)
            Next
            MarcarActivo()
        End Sub

        Private Sub IndexarBotonesDe(control As Control)
            If control.Tag IsNot Nothing AndAlso control.Tag.GetType() = GetType(SeccionDashboard) Then
                botonera(CType(control.Tag, SeccionDashboard)) = DirectCast(control, Button)
            End If
            For Each hijo As Control In control.Controls
                IndexarBotonesDe(hijo)
            Next
        End Sub

        ''' <summary>
        ''' Cambia de modulo. El panel activo anterior se quita de la pantalla sin
        ''' liberarlo: queda guardado en el diccionario y conserva todo su estado.
        ''' </summary>
        Private Sub Mostrar(seccion As SeccionDashboard)
            seccionActual = seccion

            If Not paneles.ContainsKey(seccion) Then
                paneles.Add(seccion, CrearPanel(seccion))
            End If
            Dim panel = paneles(seccion)

            ' Se quita sin liberar: la misma instancia sigue en el diccionario y conserva
            ' el pedido, los filtros y la posicion del scroll del usuario.
            While area.Controls.Count > 0
                area.Controls.Remove(area.Controls(0))
            End While
            area.Controls.Add(panel)
            ' El panel se reestiliza al mostrarlo: los que estaban ocultos mientras se
            ' cambiaba el tema no lo receberam y quedarian con los colores anteriores.
            Tema.Restilar(panel)
            panel.Refrescar()
            MarcarActivo()
        End Sub

        Private Shared Function CrearPanel(seccion As SeccionDashboard) As PanelBase
            Select Case seccion
                Case SeccionDashboard.Venta : Return New POSPanel()
                Case SeccionDashboard.Pedidos : Return New KanbanPanel()
                Case SeccionDashboard.Stock : Return New StockPanel()
                Case SeccionDashboard.Clientes : Return New ClientesPanel()
                Case SeccionDashboard.Proveedores : Return New ProveedoresPanel()
                Case SeccionDashboard.Reportes : Return New ReportesPanel()
                Case SeccionDashboard.Caja : Return New CajaPanel()
            End Select
            Throw New InvalidOperationException("Seccion desconocida: " & seccion.ToString())
        End Function

        Private Sub MarcarActivo()
            For Each par In botonera
                par.Value.Name = If(par.Key = seccionActual, "th.lateralActivo", "th.lateral")
            Next
            Tema.Restilar(lateral)
        End Sub

        ''' <summary>
        ''' Alterna el tema reestilizando los controles ya existentes. No se reconstruye
        ''' ningun panel, por lo que nada de lo que estaba cargado se pierde. Se recorren
        ''' tambien los paneles ocultos, que de otro modo quedarian con el tema anterior.
        ''' </summary>
        Private Sub btnTema_Click(sender As Object, e As EventArgs) Handles btnTema.Click
            Tema.ModoOscuro = Not Tema.ModoOscuro
            Tema.Restilar(Me)
            For Each par In paneles
                Tema.Restilar(par.Value)
            Next
            btnTema.Text = If(Tema.ModoOscuro, "Tema claro", "Tema oscuro")
            If paneles.ContainsKey(seccionActual) Then paneles(seccionActual).Refrescar()
        End Sub

        Private Sub ActualizarUsuario()
            If Datos.UsuarioActual Is Nothing Then
                lblUsuario.Text = String.Empty
                Return
            End If
            lblUsuario.Text = Datos.UsuarioActual.Nombre & Environment.NewLine &
                             Datos.UsuarioActual.Rol.ToString()
        End Sub

        ''' <summary>
        ''' Cerrar sesion devuelve el control al login: el dashboard se cierra con
        ''' DialogResult Cancel y el login vuelve a mostrarse.
        ''' </summary>
        Private Sub btnSalir_Click(sender As Object, e As EventArgs) Handles btnSalir.Click
            If MessageBox.Show("Cerrar la sesion de " & Datos.UsuarioActualNombre() & "?",
                               "Cerrar sesion", MessageBoxButtons.OKCancel,
                               MessageBoxIcon.Question) <> DialogResult.OK Then
                Return
            End If
            DialogResult = DialogResult.Cancel
            Close()
        End Sub

        Private Sub MainDashboardForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
            If DialogResult = DialogResult.OK Then
                Datos.UsuarioActual = Nothing
            End If
        End Sub

        Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
            If keyData = Keys.F5 Then
                If paneles.ContainsKey(seccionActual) Then paneles(seccionActual).Refrescar()
                Return True
            End If
            Return MyBase.ProcessCmdKey(msg, keyData)
        End Function
    End Class

End Namespace