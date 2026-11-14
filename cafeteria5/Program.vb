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
            Db.Habilitada = True
            Dim mensaje As String = Nothing
            If Not Db.Probar(mensaje) Then
                MessageBox.Show(
                    "No se pudo conectar con la base de datos." & vbCrLf & vbCrLf &
                    mensaje & vbCrLf & vbCrLf &
                    "Revisa que en el panel de XAMPP este encendido MySQL y que hayas " &
                    "importado el archivo cafeteria.sql en phpMyAdmin.",
                    "Cafeteria", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If

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