Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Runtime.CompilerServices

Namespace CafeteriaOS

    ''' <summary>
    ''' Fotos de los productos.
    '''
    ''' Al cargar una foto se achica a LadoMaximo px y se guarda como JPEG: una foto de
    ''' celular pesa varios MB y en la base alcanza con unos 30-60 KB. Asi el catalogo
    ''' entero entra en memoria al arrancar y el punto de venta no se traba.
    ''' </summary>
    Public Module ImagenProducto

        Public Const LadoMaximo As Integer = 400
        Public Const CalidadJpeg As Long = 85L

        ''' <summary>Fotos mas grandes que esto se rechazan antes de abrirlas.</summary>
        Public Const BytesMaximosArchivo As Long = 20L * 1024 * 1024

        Public Const FiltroArchivos As String =
            "Imágenes (*.jpg; *.jpeg; *.png; *.bmp; *.gif)|*.jpg;*.jpeg;*.png;*.bmp;*.gif"

        ''' <summary>
        ''' Una imagen por arreglo de bytes. Cuando el producto cambia de foto, el arreglo
        ''' viejo deja de usarse y su entrada desaparece sola (tabla debil): no hace falta
        ''' invalidar nada a mano. El punto de venta rearma las tarjetas en cada tecla
        ''' del buscador, y sin esto decodificaria todas las fotos cada vez.
        ''' </summary>
        Private ReadOnly cache As New ConditionalWeakTable(Of Byte(), Image)()

        ''' <summary>La imagen lista para mostrar, o Nothing si no tiene o esta dañada.</summary>
        Public Function ComoImagen(bytes As Byte()) As Image
            If bytes Is Nothing OrElse bytes.Length = 0 Then Return Nothing
            Dim imagen As Image = Nothing
            If cache.TryGetValue(bytes, imagen) Then Return imagen
            Try
                Using ms As New MemoryStream(bytes)
                    Using original = Image.FromStream(ms)
                        ' Copia: Image.FromStream necesita el stream abierto mientras viva.
                        imagen = New Bitmap(original)
                    End Using
                End Using
            Catch ex As ArgumentException
                Return Nothing
            End Try
            cache.AddOrUpdate(bytes, imagen)
            Return imagen
        End Function

        ''' <summary>
        ''' Lee un archivo de imagen y lo deja listo para guardar: derecho (segun la
        ''' orientacion EXIF de los celulares), achicado y en JPEG. Tira una excepcion
        ''' con un mensaje para el usuario si el archivo no sirve.
        ''' </summary>
        Public Function DesdeArchivo(ruta As String) As Byte()
            Dim info As New FileInfo(ruta)
            If Not info.Exists Then Throw New InvalidOperationException("El archivo no existe.")
            If info.Length > BytesMaximosArchivo Then
                Throw New InvalidOperationException("La imagen pesa más de 20 MB. Elegí una más chica.")
            End If

            Dim original As Image
            Try
                Using ms As New MemoryStream(File.ReadAllBytes(ruta))
                    Using leida = Image.FromStream(ms)
                        original = New Bitmap(leida)
                        CopiarOrientacion(leida, original)
                    End Using
                End Using
            Catch ex As ArgumentException
                Throw New InvalidOperationException("El archivo no es una imagen válida.")
            End Try

            Using original
                Enderezar(original)
                Return Reducir(original)
            End Using
        End Function

        Private Sub CopiarOrientacion(desde As Image, hacia As Image)
            If Array.IndexOf(desde.PropertyIdList, OrientacionExif) >= 0 Then
                hacia.SetPropertyItem(desde.GetPropertyItem(OrientacionExif))
            End If
        End Sub

        Private Const OrientacionExif As Integer = &H112

        ''' <summary>Las fotos de celular vienen "acostadas" con una marca de como girarlas.</summary>
        Private Sub Enderezar(img As Image)
            If Array.IndexOf(img.PropertyIdList, OrientacionExif) < 0 Then Return
            Dim valor = img.GetPropertyItem(OrientacionExif).Value
            If valor Is Nothing OrElse valor.Length = 0 Then Return
            Select Case valor(0)
                Case 2 : img.RotateFlip(RotateFlipType.RotateNoneFlipX)
                Case 3 : img.RotateFlip(RotateFlipType.Rotate180FlipNone)
                Case 4 : img.RotateFlip(RotateFlipType.Rotate180FlipX)
                Case 5 : img.RotateFlip(RotateFlipType.Rotate90FlipX)
                Case 6 : img.RotateFlip(RotateFlipType.Rotate90FlipNone)
                Case 7 : img.RotateFlip(RotateFlipType.Rotate270FlipX)
                Case 8 : img.RotateFlip(RotateFlipType.Rotate270FlipNone)
            End Select
        End Sub

        Private Function Reducir(original As Image) As Byte()
            Dim escala = Math.Min(1.0, LadoMaximo / Math.Max(original.Width, original.Height))
            Dim ancho = Math.Max(1, CInt(Math.Round(original.Width * escala)))
            Dim alto = Math.Max(1, CInt(Math.Round(original.Height * escala)))

            Using destino As New Bitmap(ancho, alto)
                Using g = Graphics.FromImage(destino)
                    ' JPEG no tiene transparencia: un PNG con fondo transparente
                    ' quedaria negro si no se pinta el fondo de blanco antes.
                    g.Clear(Color.White)
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic
                    g.SmoothingMode = SmoothingMode.HighQuality
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality
                    g.DrawImage(original, 0, 0, ancho, alto)
                End Using

                Dim codec = ImageCodecInfo.GetImageEncoders().First(Function(c) c.FormatID = ImageFormat.Jpeg.Guid)
                Using parametros As New EncoderParameters(1)
                    parametros.Param(0) = New EncoderParameter(Encoder.Quality, CalidadJpeg)
                    Using ms As New MemoryStream()
                        destino.Save(ms, codec, parametros)
                        Return ms.ToArray()
                    End Using
                End Using
            End Using
        End Function

        Private ReadOnly Conectores As New HashSet(Of String) From {
            "con", "de", "del", "la", "el", "los", "las", "y", "o", "en", "para", "sin", "a", "al"}

        ''' <summary>Iniciales para la tarjeta de un producto sin foto ("Café con leche" -> "CL").</summary>
        Public Function Iniciales(nombre As String) As String
            Dim palabras = If(nombre, "").Split(New Char() {" "c, "/"c, "+"c, "("c, ")"c},
                                                StringSplitOptions.RemoveEmptyEntries).
                           Where(Function(w) Not Conectores.Contains(w.ToLowerInvariant())).ToList()
            If palabras.Count = 0 Then Return "?"
            If palabras.Count = 1 Then Return palabras(0).Substring(0, 1).ToUpperInvariant()
            Return (palabras(0).Substring(0, 1) & palabras(1).Substring(0, 1)).ToUpperInvariant()
        End Function

    End Module

End Namespace
