Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.Linq

Namespace CafeteriaOS

    ''' <summary>
    ''' Convierte entre las listas del proveedor y los campos de texto editables del
    ''' formulario. Antes estos campos se perdian al guardar; ahora son la via para
    ''' consultar lo que ya esta cargado, y lo que se escriba vuelve a las listas.
    '''
    ''' El formato es una linea por registro con separador "|", y el analisis es tolerante:
    ''' acepta lineas vacias y campos faltantes.
    ''' </summary>
    Public Module ProveedorTexto

        Private Const Separador As String = "|"

        Private Function Partes(linea As String) As String()
            Return linea.Split(Separador.ToCharArray()).Select(Function(p) p.Trim()).ToArray()
        End Function

        Private Function Unir(valores As IEnumerable(Of String)) As String
            Return String.Join(Separador, valores)
        End Function

        Public Function ContactosDesdeTexto(texto As String) As List(Of ContactoProveedor)
            Dim lista As New List(Of ContactoProveedor)()
            For Each linea In SplitLineas(texto)
                Dim p = Partes(linea)
                If p.Length = 0 Then Continue For
                lista.Add(New ContactoProveedor With {
                    .Nombre = Si(p, 0),
                    .Cargo = Si(p, 1),
                    .Email = Si(p, 2),
                    .Telefono = Si(p, 3)
                })
            Next
            Return lista
        End Function

        Public Function ContactosATexto(contactos As IEnumerable(Of ContactoProveedor)) As String
            Return String.Join(Environment.NewLine,
                contactos.Select(Function(c) Unir(New String() {c.Nombre, c.Cargo, c.Email, c.Telefono})))
        End Function

        Public Function TelefonosDesdeTexto(texto As String) As List(Of TelefonoProveedor)
            Dim lista As New List(Of TelefonoProveedor)()
            For Each linea In SplitLineas(texto)
                Dim p = Partes(linea)
                If p.Length = 0 Then Continue For
                lista.Add(New TelefonoProveedor With {
                    .Tipo = Si(p, 0),
                    .Numero = Si(p, 1)
                })
            Next
            Return lista
        End Function

        Public Function TelefonosATexto(telefonos As IEnumerable(Of TelefonoProveedor)) As String
            Return String.Join(Environment.NewLine,
                telefonos.Select(Function(t) Unir(New String() {t.Tipo, t.Numero})))
        End Function

        Public Function ArchivosDesdeTexto(texto As String) As List(Of ArchivoProveedor)
            Dim lista As New List(Of ArchivoProveedor)()
            For Each linea In SplitLineas(texto)
                Dim p = Partes(linea)
                If p.Length = 0 Then Continue For
                Dim fecha As DateTime = DateTime.Today
                If Not DateTime.TryParse(Si(p, 2), fecha) Then fecha = DateTime.Today
                lista.Add(New ArchivoProveedor With {
                    .Nombre = Si(p, 0),
                    .Ruta = Si(p, 1),
                    .Fecha = fecha
                })
            Next
            Return lista
        End Function

        Public Function ArchivosATexto(archivos As IEnumerable(Of ArchivoProveedor)) As String
            Return String.Join(Environment.NewLine,
                archivos.Select(Function(a) Unir(New String() {
                    a.Nombre, a.Ruta, a.Fecha.ToString("dd/MM/yyyy")})))
        End Function

        Public Function MovimientosDesdeTexto(texto As String) As List(Of MovimientoProveedor)
            Dim lista As New List(Of MovimientoProveedor)()
            For Each linea In SplitLineas(texto)
                Dim p = Partes(linea)
                If p.Length = 0 Then Continue For
                Dim fecha As DateTime = DateTime.Today
                If Not DateTime.TryParse(Si(p, 0), fecha) Then fecha = DateTime.Today
                Dim monto As Decimal
                Decimal.TryParse(Si(p, 3), NumberStyles.Any, CultureInfo.CurrentCulture, monto)
                Dim vencimiento As DateTime?
                Dim vencimientoTexto = Si(p, 4)
                If vencimientoTexto.Length > 0 Then
                    Dim v As DateTime
                    If DateTime.TryParse(vencimientoTexto, v) Then vencimiento = v
                End If
                lista.Add(New MovimientoProveedor With {
                    .Fecha = fecha,
                    .Tipo = Si(p, 1),
                    .Descripcion = Si(p, 2),
                    .Monto = monto,
                    .Vencimiento = vencimiento,
                    .Pagado = Si(p, 5).Equals("Si", StringComparison.OrdinalIgnoreCase)
                })
            Next
            Return lista
        End Function

        Public Function MovimientosATexto(movimientos As IEnumerable(Of MovimientoProveedor)) As String
            Return String.Join(Environment.NewLine,
                movimientos.Select(Function(m) Unir(New String() {
                    m.Fecha.ToString("dd/MM/yyyy"),
                    m.Tipo,
                    m.Descripcion,
                    m.Monto.ToString("0.00", CultureInfo.CurrentCulture),
                    If(m.Vencimiento.HasValue, m.Vencimiento.Value.ToString("dd/MM/yyyy"), String.Empty),
                    If(m.Pagado, "Si", "No")})))
        End Function

        Private Function SplitLineas(texto As String) As IEnumerable(Of String)
            If String.IsNullOrWhiteSpace(texto) Then Return Enumerable.Empty(Of String)()
            Return texto.Replace(vbCrLf, vbLf).Split(vbLf).
                Where(Function(l) l.Trim().Length > 0)
        End Function

        Private Function Si(partes As String(), indice As Integer) As String
            If indice >= partes.Length Then Return String.Empty
            Return partes(indice)
        End Function
    End Module

End Namespace