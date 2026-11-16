Imports System.Net
Imports System.Net.Mail
Imports System.Security.Cryptography
Imports System.Text
Imports System.Threading.Tasks

Namespace CafeteriaOS

    ''' <summary>Resultado de un paso de la recuperacion de contraseña.</summary>
    Public Class ResultadoRecuperacion
        Public Property Ok As Boolean
        Public Property Mensaje As String

        ''' <summary>Usuario al que se le mando el codigo (paso 1).</summary>
        Public Property UsuarioId As Integer

        Public Shared Function Fallo(mensaje As String) As ResultadoRecuperacion
            Return New ResultadoRecuperacion With {.Ok = False, .Mensaje = mensaje}
        End Function
    End Class

    ''' <summary>
    ''' Recuperar la contraseña con un codigo enviado por correo.
    '''
    ''' Paso 1 (SolicitarCodigoAsync): se busca el usuario, se genera un codigo de 6
    ''' digitos, se guarda su hash en recuperaciones_contrasena y se manda por mail.
    ''' Paso 2 (CambiarContrasena): con el codigo correcto se guarda la clave nueva.
    '''
    ''' Reglas: el codigo vence a los 15 minutos, admite 5 intentos, solo vale el
    ''' ultimo enviado y no se puede pedir otro antes de un minuto.
    ''' </summary>
    Public Module RecuperacionService

        Public Const MinutosDeVigencia As Integer = 15
        Public Const IntentosMaximos As Integer = 5
        Public Const SegundosEntrePedidos As Integer = 60
        Public Const LargoMinimoContrasena As Integer = 6

        Public Async Function SolicitarCodigoAsync(identificador As String) As Task(Of ResultadoRecuperacion)
            Dim ident = If(identificador, String.Empty).Trim()
            If ident.Length = 0 Then
                Return ResultadoRecuperacion.Fallo("Escribí tu usuario o correo.")
            End If
            If Not Db.Habilitada Then
                Return ResultadoRecuperacion.Fallo("La recuperación necesita la base de datos.")
            End If

            Dim config = RecuperacionRepo.CargarConfiguracion()
            If Not config.EstaCompleta() Then
                Return ResultadoRecuperacion.Fallo(
                    "El envío de correos no está configurado. Pedile al administrador que " &
                    "complete la tabla configuracion_correo o que te cambie la contraseña.")
            End If

            Dim usuario = RecuperacionRepo.BuscarUsuario(ident)
            If usuario Is Nothing Then
                Return ResultadoRecuperacion.Fallo("No hay ningún usuario activo con ese dato.")
            End If
            If String.IsNullOrWhiteSpace(usuario.Email) Then
                Return ResultadoRecuperacion.Fallo(
                    "Ese usuario no tiene correo cargado. Pedile al administrador que te cambie la contraseña.")
            End If

            Dim espera = RecuperacionRepo.SegundosDesdeUltimoPedido(usuario.ID)
            If espera >= 0 AndAlso espera < SegundosEntrePedidos Then
                Return ResultadoRecuperacion.Fallo(
                    "Ya se envió un código hace un momento. Esperá " &
                    (SegundosEntrePedidos - espera) & " segundos para pedir otro.")
            End If

            Dim codigo = GenerarCodigo()
            Dim solicitudId = RecuperacionRepo.CrearSolicitud(usuario.ID, HashDe(codigo), MinutosDeVigencia)

            Try
                Await EnviarCorreoAsync(config, usuario, codigo)
            Catch ex As Exception
                ' Un codigo que nunca llego no tiene que quedar vigente.
                RecuperacionRepo.Anular(solicitudId)
                Return ResultadoRecuperacion.Fallo("No se pudo enviar el correo: " & MensajeDe(ex))
            End Try

            Return New ResultadoRecuperacion With {
                .Ok = True,
                .UsuarioId = usuario.ID,
                .Mensaje = "Te enviamos un código a " & OcultarCorreo(usuario.Email) &
                           ". Vence en " & MinutosDeVigencia & " minutos."
            }
        End Function

        Public Function CambiarContrasena(usuarioId As Integer,
                                          codigo As String,
                                          nueva As String,
                                          repeticion As String) As ResultadoRecuperacion
            Dim cod = If(codigo, String.Empty).Trim()
            If cod.Length = 0 Then Return ResultadoRecuperacion.Fallo("Escribí el código que te llegó por correo.")
            If If(nueva, String.Empty).Length < LargoMinimoContrasena Then
                Return ResultadoRecuperacion.Fallo(
                    "La contraseña nueva tiene que tener al menos " & LargoMinimoContrasena & " caracteres.")
            End If
            If nueva <> repeticion Then
                Return ResultadoRecuperacion.Fallo("Las dos contraseñas no coinciden.")
            End If

            Dim solicitud = RecuperacionRepo.SolicitudVigente(usuarioId)
            If solicitud Is Nothing Then
                Return ResultadoRecuperacion.Fallo("El código venció o ya se usó. Pedí uno nuevo.")
            End If

            If Not MismoHash(HashDe(cod), solicitud.CodigoHash) Then
                RecuperacionRepo.RegistrarIntentoFallido(solicitud.ID, IntentosMaximos)
                Dim restantes = IntentosMaximos - solicitud.Intentos - 1
                If restantes <= 0 Then
                    Return ResultadoRecuperacion.Fallo("Código incorrecto. Se agotaron los intentos: pedí uno nuevo.")
                End If
                Return ResultadoRecuperacion.Fallo("Código incorrecto. Te quedan " & restantes & " intento(s).")
            End If

            RecuperacionRepo.CambiarContrasena(usuarioId, nueva)
            Return New ResultadoRecuperacion With {.Ok = True, .UsuarioId = usuarioId,
                                                   .Mensaje = "Contraseña actualizada."}
        End Function

        ' ------------------------------------------------------------------
        '  Auxiliares
        ' ------------------------------------------------------------------

        ''' <summary>6 digitos con generador criptografico (Random es predecible).</summary>
        Private Function GenerarCodigo() As String
            Return RandomNumberGenerator.GetInt32(0, 1000000).ToString("D6")
        End Function

        ''' <summary>
        ''' En la base se guarda el hash del codigo y no el codigo: quien mire la tabla
        ''' en phpMyAdmin no puede usarlo para cambiar la clave de otro.
        ''' </summary>
        Private Function HashDe(codigo As String) As String
            Dim bytes = SHA256.HashData(Encoding.UTF8.GetBytes(codigo))
            Return Convert.ToHexString(bytes).ToLowerInvariant()
        End Function

        Private Function MismoHash(a As String, b As String) As Boolean
            Return CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(If(a, "")),
                Encoding.ASCII.GetBytes(If(b, "").ToLowerInvariant()))
        End Function

        ''' <summary>"cajera@cafe.com" se muestra como "ca****@cafe.com".</summary>
        Public Function OcultarCorreo(email As String) As String
            Dim e = If(email, String.Empty)
            Dim arroba = e.IndexOf("@"c)
            If arroba <= 0 Then Return e
            Dim visibles = Math.Min(2, arroba)
            Return e.Substring(0, visibles) & New String("*"c, Math.Max(3, arroba - visibles)) & e.Substring(arroba)
        End Function

        ''' <summary>
        ''' SmtpClient usa STARTTLS: con Gmail va el puerto 587 (no el 465) y una
        ''' "contraseña de aplicacion", no la contraseña normal de la cuenta.
        ''' </summary>
        Private Async Function EnviarCorreoAsync(config As ConfiguracionCorreo,
                                                 usuario As UsuarioARecuperar,
                                                 codigo As String) As Task
            Dim negocio = Datos.ConfigNegocio.NombreParaMostrar()
            Dim remitente = If(String.IsNullOrWhiteSpace(config.Remitente), negocio, config.Remitente)

            Using mensaje As New MailMessage()
                mensaje.From = New MailAddress(config.Usuario, remitente)
                mensaje.To.Add(New MailAddress(usuario.Email))
                mensaje.Subject = negocio & " - Código para recuperar tu contraseña"
                mensaje.SubjectEncoding = Encoding.UTF8
                mensaje.BodyEncoding = Encoding.UTF8
                mensaje.Body =
                    "Hola " & usuario.Nombre & "," & vbCrLf & vbCrLf &
                    "Tu código para cambiar la contraseña es: " & codigo & vbCrLf & vbCrLf &
                    "Vence en " & MinutosDeVigencia & " minutos. Si no lo pediste, ignorá este correo: " &
                    "tu contraseña sigue siendo la misma." & vbCrLf

                Using smtp As New SmtpClient(config.Servidor, config.Puerto)
                    smtp.EnableSsl = config.UsarSsl
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network
                    smtp.UseDefaultCredentials = False
                    smtp.Credentials = New NetworkCredential(config.Usuario, config.Contrasena)
                    smtp.Timeout = 20000
                    Await smtp.SendMailAsync(mensaje)
                End Using
            End Using
        End Function

        ''' <summary>El error de SMTP suele venir envuelto; el de adentro es el que explica.</summary>
        Private Function MensajeDe(ex As Exception) As String
            Dim actual = ex
            While actual.InnerException IsNot Nothing
                actual = actual.InnerException
            End While
            Return actual.Message
        End Function

    End Module

End Namespace
