Imports System.Data
Imports MySql.Data.MySqlClient

Namespace CafeteriaOS

    ''' <summary>
    ''' Punto unico de acceso a MySQL. Todo el resto de BaseDatos pasa por aca: abrir la
    ''' conexion, armar comandos con parametros, leer valores que pueden venir NULL y
    ''' agrupar varias escrituras en una transaccion.
    '''
    ''' Siempre se usan parametros (@nombre) y nunca se pega texto del usuario dentro del
    ''' SQL: asi un nombre como "O'Higgins" no rompe la consulta y no hay inyeccion SQL.
    ''' </summary>
    Public NotInheritable Class Db

        Private Sub New()
        End Sub

        ''' <summary>
        ''' Datos de conexion. XAMPP trae por defecto el usuario root sin contraseña en
        ''' el puerto 3306. Si le pusiste contraseña a root, completala en Pwd.
        '''
        ''' SslMode=Disabled porque el MySQL de XAMPP no trae certificados: sin esto el
        ''' conector puede intentar una conexion cifrada y fallar.
        '''
        ''' AllowUserVariables hace falta para poder usar @usuario_id (variable de
        ''' MySQL que leen los triggers de auditoria) sin que el conector la confunda
        ''' con un parametro.
        ''' </summary>
        Public Shared Property CadenaConexion As String =
            "Server=localhost;Port=3306;Database=cafeteria;Uid=root;Pwd=;" &
            "CharSet=utf8mb4;SslMode=Disabled;AllowUserVariables=True;"

        ''' <summary>
        ''' En False el programa trabaja solo en memoria, como antes de tener base de
        ''' datos. Lo prende Program.Main. Los proyectos de Pruebas no lo prenden, asi
        ''' que siguen corriendo sin necesitar XAMPP.
        ''' </summary>
        Public Shared Property Habilitada As Boolean

        ''' <summary>Abre una conexion nueva. Quien la pide la cierra con Using.</summary>
        Public Shared Function Abrir() As MySqlConnection
            Dim cx As New MySqlConnection(CadenaConexion)
            cx.Open()

            ' Los triggers de auditoria leen @usuario_id para saber quien hizo el cambio.
            Dim usuario = Datos.UsuarioActual
            If usuario IsNot Nothing AndAlso usuario.ID > 0 Then
                Using cmd As New MySqlCommand("SET @usuario_id = " & usuario.ID.ToString(), cx)
                    cmd.ExecuteNonQuery()
                End Using
            End If
            Return cx
        End Function

        ''' <summary>
        ''' Prueba la conexion al arrancar. Devuelve el mensaje del error para mostrarlo,
        ''' porque "no se pudo conectar" sin el motivo no ayuda a arreglarlo.
        ''' </summary>
        Public Shared Function Probar(ByRef mensaje As String) As Boolean
            Try
                Using cx = Abrir()
                    Using cmd As New MySqlCommand("SELECT COUNT(*) FROM productos", cx)
                        cmd.ExecuteScalar()
                    End Using
                End Using
                mensaje = String.Empty
                Return True
            Catch ex As Exception
                mensaje = ex.Message
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Corre varias escrituras como una sola operacion: o se guardan todas o no se
        ''' guarda ninguna. Si algo falla en el medio, MySQL deshace lo que ya se habia
        ''' escrito (Rollback) y el error sigue hacia arriba.
        ''' </summary>
        Public Shared Sub EnTransaccion(trabajo As Action(Of MySqlConnection, MySqlTransaction))
            If Not Habilitada Then Return
            Using cx = Abrir()
                Using tx = cx.BeginTransaction()
                    Try
                        trabajo(cx, tx)
                        tx.Commit()
                    Catch
                        Try
                            tx.Rollback()
                        Catch
                            ' Si la conexion se corto, MySQL ya descarto la transaccion.
                        End Try
                        Throw
                    End Try
                End Using
            End Using
        End Sub

        ''' <summary>
        ''' Arma un comando con sus parametros. Los parametros van de a pares:
        ''' Comando(cx, tx, "... WHERE id = @id", "@id", 5)
        ''' Nothing se manda como NULL.
        ''' </summary>
        Public Shared Function Comando(cx As MySqlConnection,
                                tx As MySqlTransaction,
                                sql As String,
                                ParamArray pares() As Object) As MySqlCommand
            Dim cmd As New MySqlCommand(sql, cx, tx)
            For i = 0 To pares.Length - 2 Step 2
                cmd.Parameters.AddWithValue(CStr(pares(i)), ValorParaDb(pares(i + 1)))
            Next
            Return cmd
        End Function

        ''' <summary>INSERT, UPDATE o DELETE. Devuelve cuantas filas toco.</summary>
        Public Shared Function Ejecutar(cx As MySqlConnection,
                                 tx As MySqlTransaction,
                                 sql As String,
                                 ParamArray pares() As Object) As Integer
            Using cmd = Comando(cx, tx, sql, pares)
                Return cmd.ExecuteNonQuery()
            End Using
        End Function

        ''' <summary>INSERT en una tabla con AUTO_INCREMENT. Devuelve el id generado.</summary>
        Public Shared Function Insertar(cx As MySqlConnection,
                                 tx As MySqlTransaction,
                                 sql As String,
                                 ParamArray pares() As Object) As Integer
            Using cmd = Comando(cx, tx, sql, pares)
                cmd.ExecuteNonQuery()
                Return CInt(cmd.LastInsertedId)
            End Using
        End Function

        ''' <summary>Consulta que devuelve un solo valor (o Nothing si no hubo filas).</summary>
        Public Shared Function Escalar(cx As MySqlConnection,
                                tx As MySqlTransaction,
                                sql As String,
                                ParamArray pares() As Object) As Object
            Using cmd = Comando(cx, tx, sql, pares)
                Dim valor = cmd.ExecuteScalar()
                If valor Is Nothing OrElse IsDBNull(valor) Then Return Nothing
                Return valor
            End Using
        End Function

        ' ------------------------------------------------------------------
        '  Conversiones
        ' ------------------------------------------------------------------

        Private Shared Function ValorParaDb(valor As Object) As Object
            If valor Is Nothing Then Return DBNull.Value
            Return valor
        End Function

        ''' <summary>Texto vacio se guarda como NULL.</summary>
        Public Shared Function NuloSiVacio(texto As String) As Object
            If String.IsNullOrWhiteSpace(texto) Then Return Nothing
            Return texto.Trim()
        End Function

        ''' <summary>Un id 0 significa "sin dato" y se guarda como NULL.</summary>
        Public Shared Function NuloSiCero(id As Integer) As Object
            If id <= 0 Then Return Nothing
            Return id
        End Function

        Public Shared Function Texto(r As IDataRecord, campo As String) As String
            Dim v = r(campo)
            If IsDBNull(v) Then Return String.Empty
            Return Convert.ToString(v)
        End Function

        Public Shared Function Entero(r As IDataRecord, campo As String) As Integer
            Dim v = r(campo)
            If IsDBNull(v) Then Return 0
            Return Convert.ToInt32(v)
        End Function

        Public Shared Function Dinero(r As IDataRecord, campo As String) As Decimal
            Dim v = r(campo)
            If IsDBNull(v) Then Return 0D
            Return Convert.ToDecimal(v)
        End Function

        Public Shared Function Booleano(r As IDataRecord, campo As String) As Boolean
            Dim v = r(campo)
            If IsDBNull(v) Then Return False
            Return Convert.ToBoolean(v)
        End Function

        Public Shared Function Fecha(r As IDataRecord, campo As String) As DateTime
            Dim v = r(campo)
            If IsDBNull(v) Then Return DateTime.MinValue
            Return Convert.ToDateTime(v)
        End Function

        Public Shared Function FechaNula(r As IDataRecord, campo As String) As DateTime?
            Dim v = r(campo)
            If IsDBNull(v) Then Return Nothing
            Return Convert.ToDateTime(v)
        End Function

        ''' <summary>
        ''' Convierte el texto de una columna ENUM en el Enum del programa. Por eso los
        ''' valores de los ENUM en MySQL se escriben igual que en Enums.vb.
        ''' </summary>
        Public Shared Function EnumDe(Of T As Structure)(texto As String) As T
            Dim resultado As T
            [Enum].TryParse(Of T)(texto, True, resultado)
            Return resultado
        End Function

        ' ------------------------------------------------------------------
        '  Quien esta operando
        ' ------------------------------------------------------------------

        ''' <summary>Id del usuario logueado, o Nothing (NULL) si no hay sesion.</summary>
        Public Shared Function UsuarioId() As Object
            If Datos.UsuarioActual Is Nothing Then Return Nothing
            Return NuloSiCero(Datos.UsuarioActual.ID)
        End Function

        ''' <summary>Id del empleado del usuario logueado, o Nothing (NULL).</summary>
        Public Shared Function EmpleadoId() As Object
            If Datos.UsuarioActual Is Nothing Then Return Nothing
            Return NuloSiCero(Datos.UsuarioActual.EmpleadoID)
        End Function

        ''' <summary>
        ''' Id del metodo de pago en la tabla metodos_pago. Se busca por nombre porque
        ''' la tabla usa los mismos nombres que el Enum MetodoPago.
        ''' </summary>
        Public Shared Function MetodoPagoId(cx As MySqlConnection,
                                     tx As MySqlTransaction,
                                     metodo As MetodoPago) As Integer
            Dim id = Escalar(cx, tx, "SELECT id FROM metodos_pago WHERE nombre = @n",
                             "@n", metodo.ToString())
            If id Is Nothing Then
                Throw New InvalidOperationException(
                    "Falta el metodo de pago '" & metodo.ToString() & "' en la tabla metodos_pago.")
            End If
            Return Convert.ToInt32(id)
        End Function

    End Class

End Namespace
