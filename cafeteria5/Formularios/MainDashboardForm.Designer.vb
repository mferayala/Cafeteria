Imports System.Drawing
Imports System.Windows.Forms

Namespace CafeteriaOS

    Partial Class MainDashboardForm
        Inherits Form

        Private lateral As TableLayoutPanel
        Private area As Panel
        Private lblMarca As Label
        Private lblUsuario As Label
        Private WithEvents btnTema As Button
        Private WithEvents btnSalir As Button

        Private Sub IniciarInterfaz()
            Text = "Sistema de gestión de ventas"
            StartPosition = FormStartPosition.CenterScreen
            WindowState = FormWindowState.Maximized
            MinimumSize = New Size(1024, 640)
            BackColor = Tema.BgPrincipal
            Font = Tema.Fuente(Tema.TamNormal)
            KeyPreview = True

            Dim raiz As New TableLayoutPanel With {
                .Name = "th.fondo",
                .Dock = DockStyle.Fill,
                .ColumnCount = 2,
                .RowCount = 1,
                .BackColor = Tema.BgPrincipal
            }
            raiz.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 214))
            raiz.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))

            raiz.Controls.Add(ArmarLateral(), 0, 0)

            area = New Panel With {
                .Name = "th.fondo",
                .Dock = DockStyle.Fill,
                .BackColor = Tema.BgPrincipal
            }
            raiz.Controls.Add(area, 1, 0)
            Controls.Add(raiz)
        End Sub

        Private Function ArmarLateral() As Control
            lateral = New TableLayoutPanel With {
                .Name = "th.lateral",
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 4,
                .BackColor = Tema.LateralBg,
                .Padding = New Padding(10)
            }
            lateral.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            lateral.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            lateral.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            lateral.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            lateral.RowStyles.Add(New RowStyle(SizeType.AutoSize))

            lblMarca = New Label With {
                .Name = "th.tono.Lateral",
                .Text = "CAFETERIA",
                .Font = Tema.Fuente(Tema.TamSeccion, FontStyle.Bold),
                .ForeColor = Tema.Acento3,
                .BackColor = Color.Transparent,
                .Dock = DockStyle.Top,
                .Height = 40,
                .TextAlign = ContentAlignment.MiddleLeft
            }
            lateral.Controls.Add(lblMarca, 0, 0)

            Dim navegacion As New FlowLayoutPanel With {
                .Name = "th.lateral",
                .Dock = DockStyle.Fill,
                .AutoSize = True,
                .AutoSizeMode = AutoSizeMode.GrowAndShrink,
                .FlowDirection = FlowDirection.TopDown,
                .WrapContents = False,
                .BackColor = Color.Transparent
            }
            For Each seccion As SeccionDashboard In DirectCast(
                [Enum].GetValues(GetType(SeccionDashboard)), SeccionDashboard())
                Dim boton As Button = Tema.CrearBoton(EtiquetaDe(seccion), EstiloBoton.Sutil, 194, 40)
                boton.Name = "th.lateral"
                boton.FlatAppearance.BorderSize = 0
                boton.TextAlign = ContentAlignment.MiddleLeft
                boton.Padding = New Padding(10, 0, 0, 0)
                boton.Tag = seccion
                AddHandler boton.Click, Sub(s, e) Mostrar(seccion)
                navegacion.Controls.Add(boton)
            Next
            lateral.Controls.Add(navegacion, 0, 1)

            Dim relleno = New Panel With {
                .Name = "th.lateral",
                .Dock = DockStyle.Fill,
                .BackColor = Color.Transparent
            }
            lateral.Controls.Add(relleno, 0, 2)

            lblUsuario = New Label With {
                .Name = "th.tono.Lateral",
                .Text = String.Empty,
                .Font = Tema.Fuente(Tema.TamMini),
                .ForeColor = Tema.Acento3,
                .BackColor = Color.Transparent,
                .Dock = DockStyle.Fill,
                .Height = 40
            }
            btnTema = Tema.CrearBoton("Tema oscuro", EstiloBoton.Neutro, 194, 36)
            btnSalir = Tema.CrearBoton("Cerrar sesion", EstiloBoton.Peligro, 194, 36)
            lateral.Controls.Add(lblUsuario, 0, 3)
            lateral.Controls.Add(btnTema)
            lateral.Controls.Add(btnSalir)
            Return lateral
        End Function

        Private Shared Function EtiquetaDe(seccion As SeccionDashboard) As String
            Select Case seccion
                Case SeccionDashboard.Venta : Return "Punto de venta"
                Case SeccionDashboard.Pedidos : Return "Pedidos"
                Case SeccionDashboard.Stock : Return "Stock"
                Case SeccionDashboard.Clientes : Return "Clientes"
                Case SeccionDashboard.Proveedores : Return "Proveedores"
                Case SeccionDashboard.Reportes : Return "Reportes"
                Case SeccionDashboard.Caja : Return "Caja"
            End Select
            Return seccion.ToString()
        End Function
    End Class

End Namespace