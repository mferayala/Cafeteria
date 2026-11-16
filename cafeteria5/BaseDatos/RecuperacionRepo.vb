Imports MySql.Data.MySqlClient

Namespace CafeteriaOS

    ''' <summary>Datos del servidor de correo con el que se mandan los codigos.</summary>
    Public Class ConfiguracionCorreo
        Public Property Servidor As String
        Public Property Puerto As Integer
        Public Property UsarSsl As Boolean
        Public Property Usuario As String
        Public Property Contrasena As String
        Public Property Remitente As String

        ''' <summary>Sin servidor, usuario y contraseña no se puede mandar nada.</summary>
        Public Function EstaCompleta() As Boolean
            Return Not String.IsNullOrWhiteSpace(Servidor) AndAlso
                   Puerto > 0 AndAlso
                   Not String.IsNullOrWhiteSpace(Usuario) AndAlso
                   Not String.IsNullOrWhiteSpace(Contrasena)
        End Function
    End Class

    ''' <summary>Usuario que pidio recuperar la contraseña.</summary>
    Public Class UsuarioARecuperar
        Public Property ID As Integer
        Public Property Nombre As String
        Public Property Email As String
    End Class

    ''' <summary>Pedido de recuperacion vigente (sin usar y sin vencer).</summary>
    Public Class SolicitudRecuperacion
        Public Property ID As Integer
        Public Property CodigoHash As String
        Public Property Intentos As Integer
    End Class

    ''' <summary>
    ''' Acceso a la base para recuperar contraseñas: la configuracion del correo, los
    ''' codigos enviados y el cambio de la contraseña.
    '''
    ''' Las fechas de vencimiento las calcula MySQL con NOW(): asi no importa si el
    ''' reloj de la PC de la caja esta corrido respecto del servidor.
    ''' </summary>
    Public NotInheritable Class RecuperacionRepo

        Private Sub New()
        End Sub

        ''' <summary>
        ''' Se lee cada vez que se pide un codigo (no al arrancar), para que un cambio
        ''' hecho en phpMyAdmin sirva sin reiniciar el programa.
        ''' </summary>
        Public Shared Function CargarConfiguracion() As ConfiguracionCorreo
            Dim c As New ConfiguracionCorreo()
            Using cx = Db.Abrir()
                Using cmd = Db.Comando(cx, Nothing, "SELECT * FROM configuracion_correo WHERE id = 1")
                    Using r = cmd.ExecuteReader()
                        If r.Read() Then
                            c.Servidor = Db.Texto(r, "servidor_smtp")
                            c.Puerto = Db.Entero(r, "puerto")
                            c.UsarSsl = Db.Booleano(r, "usar_ssl")
                            c.Usuario = Db.Texto(r, "usuario")
                            c.Contrasena = Db.Texto(r, "contrasena")
                            c.Remitente = Db.Texto(r, "remitente")
                        End If
                    End Using
                End Using
            End Using
            Return c
        End Function

        ''' <summary>Usuario activo por nombre, correo o telefono (igual que el login).</summary>
        Public Shared Function BuscarUsuario(identificador As String) As UsuarioARecuperar
            Using cx = Db.Abrir()
                Using cmd = Db.Comando(cx, Nothing,
                    "SELECT id, nombre_usuario, email FROM usuarios " &
                    "WHERE activo = 1 AND (email = @i OR telefono = @i OR nombre_usuario = @i) " &
                    "LIMIT 1",
                    "@i", identificador)
                    Using r = cmd.ExecuteReader()
                        If Not r.Read() Then Return Nothing
                        Return New UsuarioARecuperar With {
                            .ID = Db.Entero(r, "id"),
                            .Nombre = Db.Texto(r, "nombre_usuario"),
                            .Email = Db.Texto(r, "email")
                        }
                    End Using
                End Using
            End Using
        End Function

        ''' <summary>Segundos desde el ultimo codigo pedido por este usuario, o -1 si nunca pidio.</summary>
        Public Shared Function SegundosDesdeUltimoPedido(usuarioId As Integer) As Integer
            Using cx = Db.Abrir()
                Dim v = Db.Escalar(cx, Nothing,
                    "SELECT TIMESTAMPDIFF(SECOND, MAX(creado_en), NOW()) " &
                    "FROM recuperaciones_contrasena WHERE usuario_id = @u",
                    "@u", usuarioId)
                If v Is Nothing Then Return -1
                Return Convert.ToInt32(v)
            End Using
        End Function

        ''' <summary>
        ''' Guarda un codigo nuevo y anula los anteriores: solo vale el ultimo que llego
        ''' al correo. Devuelve el id de la solicitud.
        ''' </summary>
        Public Shared Function CrearSolicitud(usuarioId As Integer,
                                              codigoHash As String,
                                              minutosDeVigencia As Integer) As Integer
            Dim id = 0
            Db.EnTransaccion(
                Sub(cx, tx)
                    Db.Ejecutar(cx, tx,
                        "UPDATE recuperaciones_contrasena SET usado = 1 " &
                        "WHERE usuario_id = @u AND usado = 0",
                        "@u", usuarioId)
                    id = Db.Insertar(cx, tx,
                        "INSERT INTO recuperaciones_contrasena (usuario_id, codigo_hash, expira_en) " &
                        "VALUES (@u, @h, NOW() + INTERVAL @m MINUTE)",
                        "@u", usuarioId, "@h", codigoHash, "@m", minutosDeVigencia)
                End Sub)
            Return id
        End Function

        ''' <summary>Si el correo no salio, el codigo no tiene que quedar vigente.</summary>
        Public Shared Sub Anular(solicitudId As Integer)
            Db.EnTransaccion(
                Sub(cx, tx)
                    Db.Ejecutar(cx, tx,
                        "UPDATE recuperaciones_contrasena SET usado = 1 WHERE id = @id",
                        "@id", solicitudId)
                End Sub)
        End Sub

        ''' <summary>La solicitud vigente del usuario, o Nothing si vencio o ya se uso.</summary>
        Public Shared Function SolicitudVigente(usuarioId As Integer) As SolicitudRecuperacion
            Using cx = Db.Abrir()
                Using cmd = Db.Comando(cx, Nothing,
                    "SELECT id, codigo_hash, intentos FROM recuperaciones_contrasena " &
                    "WHERE usuario_id = @u AND usado = 0 AND expira_en > NOW() " &
                    "ORDER BY id DESC LIMIT 1",
                    "@u", usuarioId)
                    Using r = cmd.ExecuteReader()
                        If Not r.Read() Then Return Nothing
                        Return New SolicitudRecuperacion With {
                            .ID = Db.Entero(r, "id"),
                            .CodigoHash = Db.Texto(r, "codigo_hash"),
                            .Intentos = Db.Entero(r, "intentos")
                        }
                    End Using
                End Using
            End Using
        End Function

        ''' <summary>Suma un intento fallido; al llegar al maximo la solicitud se anula.</summary>
        Public Shared Sub RegistrarIntentoFallido(solicitudId As Integer, maximo As Integer)
            Db.EnTransaccion(
                Sub(cx, tx)
                    Db.Ejecutar(cx, tx,
                        "UPDATE recuperaciones_contrasena " &
                        "SET intentos = intentos + 1, usado = IF(intentos + 1 >= @max, 1, usado) " &
                        "WHERE id = @id",
                        "@id", solicitudId, "@max", maximo)
                End Sub)
        End Sub

        ''' <summary>
        ''' Cambia la contraseña y da por usada la solicitud, las dos cosas juntas: no
        ''' puede quedar la clave cambiada con el codigo todavia vigente.
        ''' El hash se calcula igual que en el login (SHA2 de MySQL).
        ''' </summary>
        Public Shared Sub CambiarContrasena(usuarioId As Integer, nueva As String)
            Db.EnTransaccion(
                Sub(cx, tx)
                    Db.Ejecutar(cx, tx,
                        "UPDATE usuarios SET password_hash = SHA2(@clave, 256) WHERE id = @u",
                        "@clave", nueva, "@u", usuarioId)
                    Db.Ejecutar(cx, tx,
                        "UPDATE recuperaciones_contrasena SET usado = 1 WHERE usuario_id = @u AND usado = 0",
                        "@u", usuarioId)
                End Sub)
        End Sub

    End Class

End Namespace
