Imports System.Collections.Generic
Imports System.Linq

Namespace CafeteriaOS

    ''' <summary>
    ''' Autenticacion y permisos por rol. Los usuarios viven en la tabla usuarios de
    ''' MySQL: el programa no trae ninguna cuenta escrita en el codigo.
    ''' </summary>
    Public Module AutenticacionService

        ''' <summary>
        ''' Solo para el modo sin base de datos (los proyectos de Pruebas): ahi no hay
        ''' tabla usuarios, asi que cada prueba carga aca las cuentas que necesita.
        ''' La aplicacion real nunca la llena.
        ''' </summary>
        Public ReadOnly Property UsuariosEnMemoria As New List(Of Usuario)()

        ''' <summary>
        ''' Busca por correo, telefono o nombre de usuario. Devuelve Nothing si la
        ''' contrasena no coincide. Con base de datos la consulta va a la tabla usuarios.
        ''' </summary>
        Public Function Buscar(identificador As String, contrasena As String) As Usuario
            If Db.Habilitada Then Return UsuarioRepo.Autenticar(identificador, contrasena)

            Dim clave = If(identificador, String.Empty).Trim()
            Return UsuariosEnMemoria.FirstOrDefault(
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

        ''' <summary>
        ''' Margen, gastos e IVA son informacion del negocio: quien los ve puede
        ''' saber cuanto se gana con cada producto. Solo el administrador.
        ''' </summary>
        Public Function PuedeEditarConfiguracion(rol As RolUsuario) As Boolean
            Return rol = RolUsuario.Administrador
        End Function

    End Module

End Namespace