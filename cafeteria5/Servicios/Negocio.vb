Imports System

Namespace CafeteriaOS

    ''' <summary>
    ''' Datos del negocio que se muestran al cliente: nombre en la pantalla de
    ''' ingreso, encabezado del ticket, datos fiscales.
    '''
    ''' Todo lo que el sistema muestra como "el negocio" sale de aca, en vez de estar
    ''' escrito en cada pantalla. Si el nombre cambia en la configuracion, cambia en
    ''' todos lados, y no queda ninguna pantalla diciendo cafeteria si no lo es.
    ''' </summary>
    Public Module Negocio

        ''' <summary>
        ''' Lineas del encabezado del ticket: nombre y, si hay, razon social y
        ''' contacto. Se omiten las vacias para no dejar huecos en el papel.
        ''' </summary>
        Public Function EncabezadoTicket() As String
            Dim cfg = Datos.ConfigNegocio
            If cfg Is Nothing Then Return "Mi Negocio"

            Dim lineas As New System.Collections.Generic.List(Of String)()
            lineas.Add(cfg.NombreParaMostrar())

            Dim razon = If(cfg.CUIT, "").Trim()
            If razon.Length > 0 Then lineas.Add("CUIT " & razon)

            Dim contacto = If(cfg.Telefono, "").Trim()
            If contacto.Length > 0 Then lineas.Add("Tel. " & contacto)

            Dim calle = If(cfg.Direccion, "").Trim()
            If calle.Length > 0 Then lineas.Add(calle)

            Return String.Join(Environment.NewLine, lineas)
        End Function

    End Module

End Namespace
