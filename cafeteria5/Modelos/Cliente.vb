Imports System
Imports System.Collections.Generic
Imports System.Linq

Namespace CafeteriaOS

    Public Class ClienteRegistrado
        Public Property Nombre As String
        Public Property Telefono As String
        Public Property CantidadPedidos As Integer
        Public Property TotalGastado As Decimal
        Public Property UltimoPedido As Date
        Public Property MetodoPagoHabitual As MetodoPago
        Public Property HistorialPedidos As New List(Of Integer)()

        ''' <summary>
        ''' Clave de identificacion. El telefono manda cuando existe; si el cliente es
        ''' de mostrador se identifica por nombre. Evita fusionar personas distintas
        ''' que comparten un mismo numero.
        ''' </summary>
        Public ReadOnly Property Clave As String
            Get
                If Not String.IsNullOrWhiteSpace(Telefono) AndAlso Telefono <> SinTelefono Then
                    Return "T:" & Telefono.Trim().ToLowerInvariant()
                End If
                Return "N:" & Nombre.Trim().ToLowerInvariant()
            End Get
        End Property

        Public Const SinTelefono As String = "--"
    End Class

End Namespace