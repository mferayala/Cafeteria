Imports System.Windows.Forms

Namespace CafeteriaOS

    Public Module Program
        <STAThread>
        Public Sub Main()
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2)
            Application.EnableVisualStyles()
            Application.SetCompatibleTextRenderingDefault(False)
            DatosIniciales.Cargar()
            Application.Run(New LoginForm())
        End Sub
    End Module

End Namespace