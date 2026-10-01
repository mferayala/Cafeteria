Imports System.IO
Imports System.Text
Imports System.Windows.Forms

Namespace CafeteriaOS

    ''' <summary>
    ''' Persistencia minima del login: usuario y rol elegidos, nunca la contrasena.
    ''' Solo se usa para precompletar el formulario en el proximo arranque.
    ''' </summary>
    Public Module AppConfig

        Private ReadOnly Property Ruta As String
            Get
                Return Path.Combine(Application.StartupPath, "login_recordado.txt")
            End Get
        End Property

        Public Sub GuardarLogin(usuario As String, rol As RolUsuario)
            Try
                File.WriteAllLines(Ruta,
                                   New String() {usuario, rol.ToString()},
                                   New UTF8Encoding(False))
            Catch
                ' Recordar el login es una comodidad: si falla, el login sigue funcionando.
            End Try
        End Sub

        Public Sub OlvidarLogin()
            Try
                If File.Exists(Ruta) Then File.Delete(Ruta)
            Catch
            End Try
        End Sub

        Public Sub CargarLogin(txtUsuario As TextBox, cmbRol As ComboBox, chkRecordar As CheckBox)
            Try
                If Not File.Exists(Ruta) Then Return
                Dim datos = File.ReadAllLines(Ruta, Encoding.UTF8)
                If datos.Length > 0 Then txtUsuario.Text = datos(0)
                If datos.Length > 1 Then
                    Dim rol As RolUsuario
                    If [Enum].TryParse(datos(1), False, rol) Then cmbRol.SelectedItem = rol.ToString()
                End If
                chkRecordar.Checked = True
            Catch
            End Try
        End Sub

    End Module

End Namespace