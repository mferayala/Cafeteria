Imports MySql.Data.MySqlClient

Namespace CafeteriaOS

    ''' <summary>
    ''' Turnos de caja (sesiones_caja) y sus movimientos de dinero.
    '''
    ''' El programa maneja una sola caja; es la fila 1 de la tabla cajas. Si mas adelante
    ''' hay dos puestos de cobro, alcanza con cambiar CajaId en cada uno.
    ''' </summary>
    Public NotInheritable Class CajaRepo

        Private Sub New()
        End Sub

        Public Const CajaId As Integer = 1

        ''' <summary>
        ''' Recupera el turno que quedo abierto. Si el programa se cerro con la caja
        ''' abierta, al volver a entrar el turno sigue donde estaba, con sus movimientos.
        ''' </summary>
        Public Shared Function CargarSesionAbierta(cx As MySqlConnection) As SesionCaja
            Dim sesion As New SesionCaja()

            Dim sql =
                "SELECT s.*, u.nombre_usuario FROM sesiones_caja s " &
                "LEFT JOIN empleados e ON e.id = s.empleado_id " &
                "LEFT JOIN usuarios u ON u.id = e.usuario_id " &
                "WHERE s.caja_id = @caja AND s.estado = 'Abierta' " &
                "ORDER BY s.id DESC LIMIT 1"

            Using cmd = Db.Comando(cx, Nothing, sql, "@caja", CajaId)
                Using r = cmd.ExecuteReader()
                    If Not r.Read() Then Return sesion
                    sesion.ID = Db.Entero(r, "id")
                    sesion.Abierta = True
                    sesion.MontoInicial = Db.Dinero(r, "monto_inicial")
                    sesion.FechaApertura = Db.Fecha(r, "abierta_en")
                    sesion.UsuarioApertura = Db.Texto(r, "nombre_usuario")
                End Using
            End Using

            Dim sqlMov =
                "SELECT m.*, u.nombre_usuario FROM movimientos_caja m " &
                "LEFT JOIN empleados e ON e.id = m.empleado_id " &
                "LEFT JOIN usuarios u ON u.id = e.usuario_id " &
                "WHERE m.sesion_caja_id = @s ORDER BY m.id"

            Using cmd = Db.Comando(cx, Nothing, sqlMov, "@s", sesion.ID)
                Using r = cmd.ExecuteReader()
                    While r.Read()
                        sesion.Movimientos.Add(New MovimientoCaja With {
                            .Tipo = Db.Texto(r, "tipo"),
                            .Monto = Db.Dinero(r, "monto"),
                            .Descripcion = Db.Texto(r, "descripcion"),
                            .FechaHora = Db.Fecha(r, "creado_en"),
                            .Usuario = Db.Texto(r, "nombre_usuario"),
                            .VentaID = Db.Entero(r, "venta_id")
                        })
                    End While
                End Using
            End Using

            Return sesion
        End Function

        ''' <summary>Crea el turno en la base y le deja su ID a la sesion.</summary>
        Public Shared Sub AbrirSesion(sesion As SesionCaja)
            Db.EnTransaccion(
                Sub(cx, tx)
                    sesion.ID = Db.Insertar(cx, tx,
                        "INSERT INTO sesiones_caja (caja_id, empleado_id, estado, monto_inicial, abierta_en) " &
                        "VALUES (@caja, @emp, 'Abierta', @monto, @fecha)",
                        "@caja", CajaId,
                        "@emp", Db.EmpleadoId(),
                        "@monto", sesion.MontoInicial,
                        "@fecha", sesion.FechaApertura)
                End Sub)
        End Sub

        ''' <summary>
        ''' Escribe un movimiento de caja. Se usa suelto (egresos) o dentro de la
        ''' transaccion de una venta o de un cobro.
        ''' </summary>
        Public Shared Sub InsertarMovimiento(cx As MySqlConnection,
                                             tx As MySqlTransaction,
                                             sesionId As Integer,
                                             m As MovimientoCaja)
            If m Is Nothing OrElse sesionId <= 0 Then Return
            Db.Ejecutar(cx, tx,
                "INSERT INTO movimientos_caja (sesion_caja_id, venta_id, empleado_id, tipo, monto, descripcion, creado_en) " &
                "VALUES (@s, @venta, @emp, @tipo, @monto, @desc, @fecha)",
                "@s", sesionId,
                "@venta", Db.NuloSiCero(m.VentaID),
                "@emp", Db.EmpleadoId(),
                "@tipo", m.Tipo,
                "@monto", m.Monto,
                "@desc", Db.NuloSiVacio(m.Descripcion),
                "@fecha", m.FechaHora)
        End Sub

        Public Shared Sub GuardarMovimiento(sesionId As Integer, m As MovimientoCaja)
            Db.EnTransaccion(Sub(cx, tx) InsertarMovimiento(cx, tx, sesionId, m))
        End Sub

        ''' <summary>
        ''' Cierra el turno: anota el movimiento de cierre y guarda el saldo que deberia
        ''' haber en la caja (monto_esperado).
        ''' </summary>
        Public Shared Sub CerrarSesion(sesion As SesionCaja, cierre As MovimientoCaja)
            Db.EnTransaccion(
                Sub(cx, tx)
                    InsertarMovimiento(cx, tx, sesion.ID, cierre)
                    Db.Ejecutar(cx, tx,
                        "UPDATE sesiones_caja SET estado = 'Cerrada', monto_esperado = @esperado, " &
                        "cerrada_en = @fecha WHERE id = @id",
                        "@esperado", cierre.Monto,
                        "@fecha", cierre.FechaHora,
                        "@id", sesion.ID)
                End Sub)
        End Sub

    End Class

End Namespace
