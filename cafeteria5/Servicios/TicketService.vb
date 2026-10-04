Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Printing
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Windows.Forms

Namespace CafeteriaOS

    ''' <summary>
    ''' Generacion e impresion de tickets. Centraliza los tres formatos (cliente, cocina y
    ''' caja) y el paginado de la impresora, que antes estaba resuelto con un PrintDocument
    ''' vacio que imprimia una hoja en blanco.
    ''' </summary>
    Public Module TicketService

        Public Function ConstruirCabecera(pedido As Pedido) As String
            Dim s As New StringBuilder()
            s.AppendLine("PEDIDO N. " & pedido.ID)
            s.AppendLine("Fecha: " & pedido.FechaHora.ToString("dd/MM/yyyy HH:mm"))
            If pedido.TipoServicio = TipoServicio.ParaLlevar Then
                s.AppendLine("PARA LLEVAR")
            Else
                s.AppendLine("Mesa: " & If(String.IsNullOrWhiteSpace(pedido.Mesa), "sin asignar", pedido.Mesa))
            End If
            s.AppendLine("Cliente: " & If(String.IsNullOrWhiteSpace(pedido.NombreCliente), "Mostrador", pedido.NombreCliente))
            Return s.ToString()
        End Function

        Private Function LineaDetalle(item As DetallePedido) As String
            Dim s = "  " & item.Cantidad.ToString() & " x " & item.Producto.Nombre
            If Not String.IsNullOrWhiteSpace(item.Especificaciones) Then
                s &= " (" & item.Especificaciones & ")"
            End If
            Return s
        End Function

        Public Function ConstruirDetalle(pedido As Pedido) As String
            Dim s As New StringBuilder()
            For Each item In pedido.Items
                s.AppendLine(LineaDetalle(item))
            Next
            s.AppendLine("--------------------------------")
            s.AppendLine("Subtotal: " & pedido.Subtotal.ToString("C"))
            If pedido.Descuento > 0D Then
                s.AppendLine("Descuento: -" & pedido.Descuento.ToString("C"))
            End If
            s.AppendLine("TOTAL: " & pedido.Total.ToString("C"))
            s.AppendLine("Pago: " & pedido.MetodoPago.ToString())
            Return s.ToString()
        End Function

        Public Function ConstruirTicket(pedido As Pedido) As String
            Return ConstruirCabecera(pedido) & vbCrLf & ConstruirDetalle(pedido) & vbCrLf &
                   "Gracias por su visita" & vbCrLf
        End Function

        ''' <summary>Ticket de cocina: grande, sin precios.</summary>
        Public Function ConstruirTicketCocina(pedido As Pedido) As String
            Dim s As New StringBuilder()
            s.AppendLine("=== COCINA - PEDIDO N. " & pedido.ID & " ===")
            s.AppendLine(pedido.FechaHora.ToString("HH:mm"))
            s.AppendLine(If(pedido.TipoServicio = TipoServicio.ParaLlevar, "PARA LLEVAR", "Mesa: " & pedido.Mesa))
            s.AppendLine("--------------------------------")
            For Each item In pedido.Items
                s.AppendLine(LineaDetalle(item))
            Next
            s.AppendLine("--------------------------------")
            s.AppendLine("Atendido por: " & If(Datos.UsuarioActual Is Nothing, "Sistema", Datos.UsuarioActual.Nombre))
            Return s.ToString()
        End Function

        ''' <summary>Ticket de caja: con el desglose de pago.</summary>
        Public Function ConstruirTicketCaja(pedido As Pedido) As String
            Dim s As New StringBuilder()
            s.AppendLine("=== CAJA - PEDIDO N. " & pedido.ID & " ===")
            s.AppendLine(ConstruirCabecera(pedido))
            s.AppendLine("--------------------------------")
            s.AppendLine(ConstruirDetalle(pedido))
            s.AppendLine("Cajero: " & If(Datos.UsuarioActual Is Nothing, "Sistema", Datos.UsuarioActual.Nombre))
            Return s.ToString()
        End Function

        Public Sub GuardarArchivo(texto As String, suggestedName As String)
            Using sfd As New SaveFileDialog With {
                .Filter = "Archivo de texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*",
                .FileName = suggestedName
            }
                If sfd.ShowDialog() = DialogResult.OK Then
                    File.WriteAllText(sfd.FileName, texto, Encoding.UTF8)
                End If
            End Using
        End Sub

        ''' <summary>
        ''' Imprime el ticket paginando por lineas. La linea en la que se corta cada
        ''' hoja queda registrada en <paramref name="pagina"/>, de modo que la pagina
        ''' siguiente continua donde termino la anterior y no repite el encabezado.
        ''' </summary>
        Public Sub Imprimir(texto As String)
            Using fuente As New Font(Tema.FamiliaTipografica(), 9.0F)
                Using pd As New PrintDocument With {.DocumentName = "Ticket Cafeteria"}
                    pd.DefaultPageSettings.Landscape = False

                    Dim lineas = texto.Replace(vbCrLf, vbLf).Split(vbLf)
                    Dim pagina As Integer = 0

                    Dim alPintar As PrintPageEventHandler =
                        Sub(sender As Object, e As PrintPageEventArgs)
                            Dim g = e.Graphics
                            Dim margen = e.MarginBounds
                            Dim altoLinea = fuente.GetHeight(g)

                            Dim y As Single = margen.Top
                            Dim i = pagina
                            While i < lineas.Length
                                If y + altoLinea > margen.Bottom Then
                                    e.HasMorePages = True
                                    Exit While
                                End If
                                g.DrawString(lineas(i), fuente, Brushes.Black, margen.Left, y)
                                y += altoLinea
                                i += 1
                            End While
                            pagina = i
                        End Sub

                    AddHandler pd.PrintPage, alPintar

                    Using dlg As New PrintDialog With {.Document = pd}
                        If dlg.ShowDialog() = DialogResult.OK Then pd.Print()
                    End Using

                    RemoveHandler pd.PrintPage, alPintar
                End Using
            End Using
        End Sub

    End Module

End Namespace