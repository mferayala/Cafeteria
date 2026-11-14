Imports MySql.Data.MySqlClient

Namespace CafeteriaOS

    ''' <summary>Usuarios y configuracion del negocio.</summary>
    Public NotInheritable Class UsuarioRepo

        Private Sub New()
        End Sub

        ''' <summary>
        ''' Busca el usuario por nombre, correo o telefono y compara la contraseña.
        '''
        ''' La comparacion la hace MySQL con SHA2(): la contraseña escrita se convierte en
        ''' hash y se compara contra el hash guardado. La contraseña real nunca se guarda
        ''' ni se trae de la base.
        ''' </summary>
        Public Shared Function Autenticar(identificador As String, contrasena As String) As Usuario
            Dim usuario As Usuario = Nothing

            Dim sql =
                "SELECT u.id, u.nombre_usuario, u.email, u.telefono, r.nombre AS rol, " &
                "       e.id AS empleado_id " &
                "FROM usuarios u " &
                "JOIN roles r ON r.id = u.rol_id " &
                "LEFT JOIN empleados e ON e.usuario_id = u.id " &
                "WHERE u.activo = 1 " &
                "  AND (u.email = @ident OR u.telefono = @ident OR u.nombre_usuario = @ident) " &
                "  AND u.password_hash = SHA2(@clave, 256) " &
                "LIMIT 1"

            Using cx = Db.Abrir()
                Using cmd = Db.Comando(cx, Nothing, sql,
                                       "@ident", If(identificador, String.Empty).Trim(),
                                       "@clave", If(contrasena, String.Empty))
                    Using r = cmd.ExecuteReader()
                        If Not r.Read() Then Return Nothing
                        usuario = New Usuario With {
                            .ID = Db.Entero(r, "id"),
                            .EmpleadoID = Db.Entero(r, "empleado_id"),
                            .Nombre = Db.Texto(r, "nombre_usuario"),
                            .Email = Db.Texto(r, "email"),
                            .Telefono = Db.Texto(r, "telefono"),
                            .Rol = Db.EnumDe(Of RolUsuario)(Db.Texto(r, "rol"))
                        }
                    End Using
                End Using

                Db.Ejecutar(cx, Nothing, "UPDATE usuarios SET ultimo_acceso = NOW() WHERE id = @id",
                            "@id", usuario.ID)
            End Using

            Return usuario
        End Function

    End Class

    Public NotInheritable Class ConfigRepo

        Private Sub New()
        End Sub

        Public Shared Function Cargar(cx As MySqlConnection) As ConfiguracionNegocio
            Dim config As New ConfiguracionNegocio()
            Using cmd = Db.Comando(cx, Nothing, "SELECT * FROM configuracion_negocio WHERE id = 1")
                Using r = cmd.ExecuteReader()
                    If r.Read() Then
                        config.NombreNegocio = Db.Texto(r, "nombre_negocio")
                        config.CUIT = Db.Texto(r, "cuit")
                        config.Direccion = Db.Texto(r, "direccion")
                        config.Telefono = Db.Texto(r, "telefono")
                        config.MargenSobreCosto = Db.Dinero(r, "margen_sobre_costo")
                        config.GastosFijosPorcentaje = Db.Dinero(r, "gastos_fijos_porcentaje")
                        config.IVAGeneral = Db.Dinero(r, "iva_general")
                        config.AplicarGastosFijos = Db.Booleano(r, "aplicar_gastos_fijos")
                        config.AplicarIVA = Db.Booleano(r, "aplicar_iva")
                    End If
                End Using
            End Using
            Return config
        End Function

        ''' <summary>
        ''' Guarda la unica fila de configuracion. INSERT ... ON DUPLICATE KEY UPDATE
        ''' la crea si alguien la borro y la actualiza si ya existe.
        ''' </summary>
        Public Shared Sub Guardar(c As ConfiguracionNegocio)
            Db.EnTransaccion(
                Sub(cx, tx)
                    Db.Ejecutar(cx, tx,
                        "INSERT INTO configuracion_negocio " &
                        "  (id, nombre_negocio, cuit, direccion, telefono, margen_sobre_costo, " &
                        "   gastos_fijos_porcentaje, iva_general, aplicar_gastos_fijos, aplicar_iva) " &
                        "VALUES (1, @nombre, @cuit, @dir, @tel, @margen, @gastos, @iva, @aplGastos, @aplIva) " &
                        "ON DUPLICATE KEY UPDATE nombre_negocio = @nombre, cuit = @cuit, direccion = @dir, " &
                        "  telefono = @tel, margen_sobre_costo = @margen, gastos_fijos_porcentaje = @gastos, " &
                        "  iva_general = @iva, aplicar_gastos_fijos = @aplGastos, aplicar_iva = @aplIva",
                        "@nombre", If(c.NombreNegocio, String.Empty),
                        "@cuit", Db.NuloSiVacio(c.CUIT),
                        "@dir", Db.NuloSiVacio(c.Direccion),
                        "@tel", Db.NuloSiVacio(c.Telefono),
                        "@margen", c.MargenSobreCosto,
                        "@gastos", c.GastosFijosPorcentaje,
                        "@iva", c.IVAGeneral,
                        "@aplGastos", c.AplicarGastosFijos,
                        "@aplIva", c.AplicarIVA)
                End Sub)
        End Sub

    End Class

End Namespace
