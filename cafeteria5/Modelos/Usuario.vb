Namespace CafeteriaOS

    ''' <summary>
    ''' Usuario de la version sin base de datos: vive en memoria, junto con el resto de
    ''' los datos de la sesion. Ver DatosIniciales.Usuarios().
    ''' </summary>
    Public Class Usuario
        Public Property Nombre As String
        Public Property Email As String
        Public Property Telefono As String
        Public Property Contrasena As String
        Public Property Rol As RolUsuario

        ''' <summary>El login acepta correo o telefono, indistintamente.</summary>
        Public ReadOnly Property Identificador As String
            Get
                Return If(String.IsNullOrWhiteSpace(Email), Telefono, Email)
            End Get
        End Property
    End Class

End Namespace