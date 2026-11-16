Imports System.Collections.Generic
Imports System.Text.Json
Imports MySql.Data.MySqlClient

Namespace CafeteriaOS

    ''' <summary>
    ''' Listas de la ficha del proveedor que se guardan juntas en la columna JSON
    ''' "extras". Son datos que la pantalla carga como texto libre y que no se consultan
    ''' por separado; si mas adelante hace falta, cada lista puede pasar a su propia tabla.
    ''' </summary>
    Public Class ProveedorExtras
        Public Property Contactos As New List(Of ContactoProveedor)()
        Public Property Telefonos As New List(Of TelefonoProveedor)()
        Public Property Archivos As New List(Of ArchivoProveedor)()
        Public Property Movimientos As New List(Of MovimientoProveedor)()
        Public Property HistorialCambios As New List(Of String)()
    End Class

    Public NotInheritable Class ProveedorRepo

        Private Sub New()
        End Sub

        Private Shared ReadOnly OpcionesJson As New JsonSerializerOptions With {
            .PropertyNameCaseInsensitive = True
        }

        Public Shared Function CargarTodos(cx As MySqlConnection) As Dictionary(Of Integer, Proveedor)
            Dim porId As New Dictionary(Of Integer, Proveedor)()

            Using cmd = Db.Comando(cx, Nothing, "SELECT * FROM proveedores ORDER BY codigo")
                Using r = cmd.ExecuteReader()
                    While r.Read()
                        Dim p As New Proveedor With {
                            .ID = Db.Entero(r, "id"),
                            .Codigo = Db.Texto(r, "codigo"),
                            .Nombre = Db.Texto(r, "razon_social"),
                            .CUIT = Db.Texto(r, "cuit"),
                            .TipoDocumento = Db.Texto(r, "tipo_documento"),
                            .NumeroDocumento = Db.Texto(r, "documento"),
                            .TipoIVA = Db.Texto(r, "tipo_iva"),
                            .TelefonoLaboral = Db.Texto(r, "telefono"),
                            .TelefonoParticular = Db.Texto(r, "telefono_particular"),
                            .Celular = Db.Texto(r, "celular"),
                            .Fax = Db.Texto(r, "fax"),
                            .Email = Db.Texto(r, "email"),
                            .SitioWeb = Db.Texto(r, "sitio_web"),
                            .Domicilio = Db.Texto(r, "direccion"),
                            .CodigoPostal = Db.Texto(r, "codigo_postal"),
                            .Localidad = Db.Texto(r, "localidad"),
                            .Provincia = Db.Texto(r, "provincia"),
                            .Pais = Db.Texto(r, "pais"),
                            .ContactoPrincipal = Db.Texto(r, "contacto_principal"),
                            .Actividad = Db.Texto(r, "actividad"),
                            .Categoria = Db.Texto(r, "categoria"),
                            .CondicionComercial = Db.Texto(r, "condicion_comercial"),
                            .FormaPagoHabitual = Db.Texto(r, "forma_pago"),
                            .Banco = Db.Texto(r, "banco"),
                            .CBU = Db.Texto(r, "cbu"),
                            .Alias = Db.Texto(r, "alias"),
                            .DescuentoGeneral = Db.Dinero(r, "descuento_general"),
                            .DescuentoPorVolumen = Db.Dinero(r, "descuento_volumen"),
                            .ObservacionesRapidas = Db.Texto(r, "observaciones_rapidas"),
                            .Observaciones = Db.Texto(r, "observaciones"),
                            .ObservacionesComerciales = Db.Texto(r, "observaciones_comerciales"),
                            .NotasInternas = Db.Texto(r, "notas_internas"),
                            .TotalCompras = Db.Dinero(r, "total_compras"),
                            .CantidadOrdenes = Db.Entero(r, "cantidad_ordenes"),
                            .SaldoPendiente = Db.Dinero(r, "saldo_pendiente"),
                            .UltimaCompra = Db.FechaNula(r, "ultima_compra"),
                            .FechaUltimaOperacion = Db.FechaNula(r, "fecha_ultima_operacion"),
                            .FechaAlta = Db.Fecha(r, "fecha_alta"),
                            .CreadoPor = Db.Texto(r, "creado_por"),
                            .ModificadoPor = Db.Texto(r, "modificado_por"),
                            .FechaCreacion = Db.Fecha(r, "creado_en"),
                            .FechaModificacion = Db.Fecha(r, "actualizado_en"),
                            .Activo = Db.Booleano(r, "activo")
                        }
                        AplicarExtras(p, Db.Texto(r, "extras"))
                        porId(p.ID) = p
                    End While
                End Using
            End Using

            Return porId
        End Function

        ''' <summary>Alta si el proveedor no tiene ID; si no, actualiza sus datos.</summary>
        Public Shared Sub Guardar(p As Proveedor)
            Dim valores = Parametros(p)
            ' El id nuevo se asigna recien cuando la transaccion se confirmo: si se
            ' reintenta (o falla), el proveedor no queda con un id que no existe.
            Dim esNuevo = p.ID = 0
            Dim nuevoId = 0

            Db.EnTransaccion(
                Sub(cx, tx)
                    If esNuevo Then
                        nuevoId = Db.Insertar(cx, tx,
                            "INSERT INTO proveedores (" & String.Join(", ", Columnas) & ", creado_por) " &
                            "VALUES (@" & String.Join(", @", Columnas) & ", @creado_por)",
                            Concatenar(valores, "@creado_por", Db.NuloSiVacio(p.CreadoPor)))
                    Else
                        Dim asignaciones As New List(Of String)()
                        For Each col In Columnas
                            asignaciones.Add(col & " = @" & col)
                        Next
                        Db.Ejecutar(cx, tx,
                            "UPDATE proveedores SET " & String.Join(", ", asignaciones) & " WHERE id = @id",
                            Concatenar(valores, "@id", p.ID))
                    End If
                End Sub)
            If esNuevo AndAlso nuevoId > 0 Then p.ID = nuevoId
        End Sub

        ''' <summary>
        ''' Borra el proveedor. Los productos e ingresos que lo nombraban quedan sin
        ''' proveedor (ON DELETE SET NULL), no se pierden.
        ''' </summary>
        Public Shared Sub Eliminar(p As Proveedor)
            If p Is Nothing OrElse p.ID = 0 Then Return
            Db.EnTransaccion(
                Sub(cx, tx)
                    Db.Ejecutar(cx, tx, "DELETE FROM proveedores WHERE id = @id", "@id", p.ID)
                End Sub)
        End Sub

        ' ------------------------------------------------------------------

        ''' <summary>Columnas que se escriben en el alta y en la edicion.</summary>
        Private Shared ReadOnly Columnas As String() = {
            "codigo", "razon_social", "cuit", "tipo_documento", "documento", "tipo_iva",
            "telefono", "telefono_particular", "celular", "fax", "email", "sitio_web",
            "direccion", "codigo_postal", "localidad", "provincia", "pais",
            "contacto_principal", "actividad", "categoria", "condicion_comercial",
            "forma_pago", "banco", "cbu", "alias", "descuento_general", "descuento_volumen",
            "observaciones_rapidas", "observaciones", "observaciones_comerciales",
            "notas_internas", "total_compras", "cantidad_ordenes", "saldo_pendiente",
            "ultima_compra", "fecha_ultima_operacion", "extras", "fecha_alta",
            "modificado_por", "activo"
        }

        ''' <summary>Valores en el mismo orden que Columnas, de a pares @columna / valor.</summary>
        Private Shared Function Parametros(p As Proveedor) As Object()
            Dim extras As New ProveedorExtras With {
                .Contactos = If(p.Contactos, New List(Of ContactoProveedor)()),
                .Telefonos = If(p.Telefonos, New List(Of TelefonoProveedor)()),
                .Archivos = If(p.Archivos, New List(Of ArchivoProveedor)()),
                .Movimientos = If(p.Movimientos, New List(Of MovimientoProveedor)()),
                .HistorialCambios = If(p.HistorialCambios, New List(Of String)())
            }

            Dim valores As Object() = {
                p.Codigo, p.Nombre, Db.NuloSiVacio(p.CUIT), Db.NuloSiVacio(p.TipoDocumento),
                Db.NuloSiVacio(p.NumeroDocumento), Db.NuloSiVacio(p.TipoIVA),
                Db.NuloSiVacio(p.TelefonoLaboral), Db.NuloSiVacio(p.TelefonoParticular),
                Db.NuloSiVacio(p.Celular), Db.NuloSiVacio(p.Fax), Db.NuloSiVacio(p.Email),
                Db.NuloSiVacio(p.SitioWeb), Db.NuloSiVacio(p.Domicilio),
                Db.NuloSiVacio(p.CodigoPostal), Db.NuloSiVacio(p.Localidad),
                Db.NuloSiVacio(p.Provincia), Db.NuloSiVacio(p.Pais),
                Db.NuloSiVacio(p.ContactoPrincipal), Db.NuloSiVacio(p.Actividad),
                Db.NuloSiVacio(p.Categoria), Db.NuloSiVacio(p.CondicionComercial),
                Db.NuloSiVacio(p.FormaPagoHabitual), Db.NuloSiVacio(p.Banco),
                Db.NuloSiVacio(p.CBU), Db.NuloSiVacio(p.Alias),
                p.DescuentoGeneral, p.DescuentoPorVolumen,
                Db.NuloSiVacio(p.ObservacionesRapidas), Db.NuloSiVacio(p.Observaciones),
                Db.NuloSiVacio(p.ObservacionesComerciales), Db.NuloSiVacio(p.NotasInternas),
                p.TotalCompras, p.CantidadOrdenes, p.SaldoPendiente,
                p.UltimaCompra, p.FechaUltimaOperacion,
                JsonSerializer.Serialize(extras),
                p.FechaAlta.Date,
                Db.NuloSiVacio(p.ModificadoPor), p.Activo
            }

            Dim pares As New List(Of Object)()
            For i = 0 To Columnas.Length - 1
                pares.Add("@" & Columnas(i))
                pares.Add(valores(i))
            Next
            Return pares.ToArray()
        End Function

        Private Shared Function Concatenar(pares As Object(), nombre As String, valor As Object) As Object()
            Dim lista As New List(Of Object)(pares)
            lista.Add(nombre)
            lista.Add(valor)
            Return lista.ToArray()
        End Function

        Private Shared Sub AplicarExtras(p As Proveedor, json As String)
            If String.IsNullOrWhiteSpace(json) Then Return
            Try
                Dim extras = JsonSerializer.Deserialize(Of ProveedorExtras)(json, OpcionesJson)
                If extras Is Nothing Then Return
                p.Contactos = If(extras.Contactos, New List(Of ContactoProveedor)())
                p.Telefonos = If(extras.Telefonos, New List(Of TelefonoProveedor)())
                p.Archivos = If(extras.Archivos, New List(Of ArchivoProveedor)())
                p.Movimientos = If(extras.Movimientos, New List(Of MovimientoProveedor)())
                p.HistorialCambios = If(extras.HistorialCambios, New List(Of String)())
            Catch ex As JsonException
                ' Un JSON mal escrito a mano en phpMyAdmin no tiene que impedir abrir el
                ' programa: el proveedor se carga igual, sin esas listas.
            End Try
        End Sub

    End Class

End Namespace
