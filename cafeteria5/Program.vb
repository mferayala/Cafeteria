Imports System.Windows.Forms

Namespace CafeteriaOS

    Public Module Program
        <STAThread>
        Public Sub Main()
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2)
            Application.EnableVisualStyles()
            Application.SetCompatibleTextRenderingDefault(False)

            ' Cualquier error que no se haya atajado en una pantalla (por ejemplo, MySQL
            ' apagado en medio del dia) se muestra en un mensaje en vez de cerrar el
            ' programa de golpe.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException)
            AddHandler Application.ThreadException,
                Sub(origen, e)
                    MessageBox.Show(e.Exception.Message, "Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Sub

            ' Antes los datos se armaban en memoria con DatosIniciales.Cargar() y se
            ' perdian al cerrar. Ahora se leen de MySQL.
            ' Donde esta la base lo dice conexion.ini de esta PC (en la PC de XAMPP es
            ' localhost; en las demas, la IP de esa PC). Si no conecta, se pide ahi mismo
            ' en una ventana, en vez de cerrar con un error.
            Db.Habilitada = True
            Dim conexion = ConfiguracionConexion.Cargar()
            Db.CadenaConexion = conexion.CadenaConexion()
            Dim mensaje As String = Nothing
            If Not Db.Probar(mensaje) Then
                Using f As New ConexionForm(conexion, mensaje)
                    If f.ShowDialog() <> DialogResult.OK Then Return
                End Using
            End If

            Try
                If Not Db.EsquemaActualizado(mensaje) Then
                    MessageBox.Show(mensaje, "Cafeteria - actualizar base de datos",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return
                End If
            Catch ex As Exception
                MessageBox.Show("No se pudo revisar la base de datos:" & vbCrLf & vbCrLf & ex.Message,
                                "Cafeteria", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End Try

            Try
                CargaBD.CargarTodo()
            Catch ex As Exception
                MessageBox.Show("Se conecto a MySQL pero no se pudieron leer los datos:" &
                                vbCrLf & vbCrLf & ex.Message,
                                "Cafeteria", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End Try

            Application.Run(New LoginForm())
        End Sub
    End Module

End Namespace