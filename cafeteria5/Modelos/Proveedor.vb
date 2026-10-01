Imports System
Imports System.Collections.Generic
Imports System.Linq

Namespace CafeteriaOS

    Public Class ContactoProveedor
        Public Property Nombre As String
        Public Property Cargo As String
        Public Property Email As String
        Public Property Telefono As String
    End Class

    Public Class TelefonoProveedor
        Public Property Tipo As String
        Public Property Numero As String
    End Class

    Public Class ArchivoProveedor
        Public Property Nombre As String
        Public Property Ruta As String
        Public Property Fecha As DateTime = DateTime.Now
    End Class

    Public Class MovimientoProveedor
        Public Property Fecha As DateTime = DateTime.Now
        Public Property Tipo As String
        Public Property Descripcion As String
        Public Property Monto As Decimal
        Public Property Vencimiento As DateTime?
        Public Property Pagado As Boolean
    End Class

    Public Class Proveedor
        Public Property Codigo As String
        Public Property Nombre As String
        Public Property Domicilio As String
        Public Property CodigoPostal As String
        Public Property TelefonoLaboral As String
        Public Property TelefonoParticular As String
        Public Property Celular As String
        Public Property Fax As String
        Public Property ContactoPrincipal As String
        Public Property TipoIVA As String
        Public Property CUIT As String
        Public Property TipoDocumento As String
        Public Property NumeroDocumento As String
        Public Property Pais As String = "Argentina"
        Public Property Provincia As String
        Public Property Localidad As String
        Public Property Actividad As String
        Public Property Categoria As String
        Public Property ObservacionesRapidas As String
        Public Property Activo As Boolean = True
        Public Property Email As String
        Public Property SitioWeb As String
        Public Property FechaAlta As DateTime = DateTime.Now
        Public Property CondicionComercial As String
        Public Property FormaPagoHabitual As String
        Public Property Banco As String
        Public Property CBU As String
        Public Property [Alias] As String
        Public Property Observaciones As String
        Public Property NotasInternas As String
        Public Property DescuentoGeneral As Decimal
        Public Property DescuentoPorVolumen As Decimal
        Public Property ObservacionesComerciales As String
        Public Property TotalCompras As Decimal
        Public Property CantidadOrdenes As Integer
        Public Property UltimaCompra As DateTime?
        Public Property SaldoPendiente As Decimal
        Public Property FechaUltimaOperacion As DateTime?
        Public Property CreadoPor As String
        Public Property ModificadoPor As String
        Public Property FechaCreacion As DateTime = DateTime.Now
        Public Property FechaModificacion As DateTime = DateTime.Now
        Public Property HistorialCambios As New List(Of String)()
        Public Property Contactos As New List(Of ContactoProveedor)()
        Public Property Telefonos As New List(Of TelefonoProveedor)()
        Public Property Archivos As New List(Of ArchivoProveedor)()
        Public Property Movimientos As New List(Of MovimientoProveedor)()

        Public ReadOnly Property TieneDeudaVencida As Boolean
            Get
                Return Movimientos.Any(
                    Function(m) Not m.Pagado AndAlso
                              m.Vencimiento.HasValue AndAlso
                              m.Vencimiento.Value.Date < Date.Today)
            End Get
        End Property
    End Class

End Namespace