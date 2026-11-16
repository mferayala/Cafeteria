Imports System.Collections
Imports System.Collections.Concurrent
Imports System.Linq
Imports System.Reflection

Namespace CafeteriaOS

    ''' <summary>
    ''' Copia los datos de un objeto recien leido de la base sobre el que ya esta en
    ''' memoria, en lugar de reemplazarlo.
    '''
    ''' Las pantallas, el pedido en curso y otros objetos guardan referencias a los
    ''' productos, ventas y proveedores que ya existen. Si la sincronizacion los cambiara
    ''' por objetos nuevos, esas referencias quedarian apuntando a copias viejas.
    ''' </summary>
    Public Module Copiador

        Private ReadOnly propiedades As New ConcurrentDictionary(Of Type, PropertyInfo())()

        ''' <summary>
        ''' Copia cada propiedad publica escribible y devuelve True si alguna cambio.
        ''' Las listas se reemplazan siempre (vienen armadas de la base) pero no cuentan
        ''' como cambio si tienen la misma cantidad de elementos; los arreglos de bytes
        ''' (fotos) se comparan por contenido y, si son iguales, se deja el original
        ''' para no perder la imagen ya decodificada.
        ''' </summary>
        Public Function CopiarSobre(Of T As Class)(destino As T, origen As T) As Boolean
            If destino Is Nothing OrElse origen Is Nothing OrElse ReferenceEquals(destino, origen) Then Return False

            Dim cambio = False
            For Each p In propiedades.GetOrAdd(GetType(T), AddressOf Escribibles)
                Dim nuevo = p.GetValue(origen)
                Dim actual = p.GetValue(destino)

                If TypeOf nuevo Is Byte() OrElse TypeOf actual Is Byte() Then
                    Dim a = TryCast(actual, Byte()), b = TryCast(nuevo, Byte())
                    Dim iguales = (a Is Nothing AndAlso b Is Nothing) OrElse
                                  (a IsNot Nothing AndAlso b IsNot Nothing AndAlso a.SequenceEqual(b))
                    If Not iguales Then
                        p.SetValue(destino, nuevo)
                        cambio = True
                    End If

                ElseIf TypeOf nuevo Is IList AndAlso Not TypeOf nuevo Is Array Then
                    Dim listaActual = TryCast(actual, IList)
                    If listaActual Is Nothing OrElse listaActual.Count <> DirectCast(nuevo, IList).Count Then cambio = True
                    p.SetValue(destino, nuevo)

                ElseIf Not Equals(actual, nuevo) Then
                    ' Texto vacio y Nothing son lo mismo para la pantalla.
                    If TypeOf nuevo Is String OrElse TypeOf actual Is String Then
                        If If(CStr(actual), "") = If(CStr(nuevo), "") Then Continue For
                    End If
                    p.SetValue(destino, nuevo)
                    cambio = True
                End If
            Next
            Return cambio
        End Function

        Private Function Escribibles(tipo As Type) As PropertyInfo()
            Return tipo.GetProperties(BindingFlags.Public Or BindingFlags.Instance).
                Where(Function(p) p.CanRead AndAlso p.CanWrite AndAlso p.GetIndexParameters().Length = 0).
                ToArray()
        End Function

    End Module

End Namespace
