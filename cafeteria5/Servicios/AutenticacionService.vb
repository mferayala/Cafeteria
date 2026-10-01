Imports System.Collections.Generic
Imports System.Linq

Namespace CafeteriaOS

    ''' <summary>
    ''' Autenticacion en memoria y permisos por rol. Reemplaza la consulta SQL que
    ''' existía en la version con base de datos.
    ''' </summary>
    Public Module AutenticacionService

        Private ReadOnly usuarios As List(Of Usuario) = DatosIniciales.Usuarios()

        ''' <summary>Busca por correo o telefono. Devuelve Nothing si la contrasena no coincide.</summary>
        Public Function Buscar(identificador As String, contrasena As String) As Usuario
            Dim clave = If(identificador, String.Empty).Trim()
            Return usuarios.FirstOrDefault(
                Function(u) u.Identificador.Equals(clave, StringComparison.OrdinalIgnoreCase) AndAlso
                            u.Contrasena = contrasena)
        End Function

        Public Function Roles() As String()
            Return [Enum].GetNames(GetType(RolUsuario))
        End Function

        ''' <summary>
        ''' El rol elegido en el login debe corresponder al usuario: es lo que habilita
        ''' o bloquea cada modulo, no un dato decorativo.
        ''' </summary>
        Public Function RolCoincide(usuario As Usuario, rolElegido As String) As Boolean
            Return usuario IsNot Nothing AndAlso
                   rolElegido IsNot Nothing AndAlso
                   usuario.Rol.ToString() = rolElegido
        End Function

        ''' <summary>Cocina puede consultar el listado, pero no cargarlo ni modificarlo.</summary>
        Public Function PuedeEditarProveedores(rol As RolUsuario) As Boolean
            Return rol = RolUsuario.Administrador OrElse rol = RolUsuario.Cajero
        End Function

        Public Function PuedeEliminarProveedores(rol As RolUsuario) As Boolean
            Return rol = RolUsuario.Administrador
        End Function

    End Module

End Namespace