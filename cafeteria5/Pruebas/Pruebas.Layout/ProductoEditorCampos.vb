Imports System.IO
Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.Linq
Imports System.Windows.Forms
Imports CafeteriaOS

Namespace Prueba

    ''' <summary>
    ''' Bug B: al editar un producto, Nombre, Marca, Codigo de barras y Descripcion
    ''' no se pueden escribir.
    '''
    ''' La causa no estaba en los campos: estan habilitados, no son de solo lectura y
    ''' aceptan la escritura (la primera prueba lo verifica). Lo que pasaba es que el
    ''' guardado se rechazaba siempre con la alerta "El precio de venta tiene que ser
    ''' un numero", y el usuario lo:"-atribuia a los campos que habia tocado.
    '''
    ''' Motivo del rechazo: la pantalla escribe el precio con ToString("N2"), que en
    ''' es-AR sale "3.000,00", y Guardar le hacia Replace(".", ",") antes de
    ''' parsearlo, dejando "3,000,00", que no es un numero. Con precios de 1000 para
    ''' arriba el guardado no pasaba nunca; con precios mas chicos si, y por eso
    ''' parecia que le pasaba a algunos productos y no a todos.
    '''
    ''' El guardado se prueba en un proceso hijo, como el bug A: al rechazarse
    ''' aparece un MessageBox, y si el proceso principal lo esperara se quedaria
    ''' colgado en vez de reportar el fallo.
    ''' </summary>
    Module PruebasProductoEditorPrecios

        ' Los valores esperados viven aqui y no en cada prueba: el hijo los escribe
        ' y el padre los compara, asi los dos lados hablan del mismo dato.
        Const NombreNuevo As String = "Cafe de prueba editado"
        Const MarcaNueva As String = "Marca de prueba"
        Const CodigoNuevo As String = "7799000000017"
        Const DescripcionNueva As String = "Descripcion de prueba"

        Sub Correr()
            LosCamposAceptanEscrituraAlEditar()
            GuardarProductoConPrecioDeMilNoPierdeLosCampos()
        End Sub

        ''' <summary>
        ''' Los cuatro campos del reporte tienen que estar habilitados y aceptar que
        ''' se escriba y se borre. Se los busca por la fila en que estan, no por un
        ''' nombre de control: todos se llaman "th.campo" y ese nombre no dice nada.
        ''' </summary>
        Sub LosCamposAceptanEscrituraAlEditar()
            PrepararDatos()

            Using f As New ProductoEditorForm(ProductoConPrecioAlto())
                f.Show()
                ForzarLayout(f)

                Dim campos = CamposDelBugB(f)
                Revisar("Los cuatro campos del bug se encuentran en la pantalla " &
                        "(se hallaron " & campos.Count & " de 4)", campos.Count = 4)

                ' Antes de probar los campos hay que fijar que fila es cual. Si la
                ' fila 2 no fuera la del codigo de barras, la prueba estaria
                ' escribiendo en el campo equivocado y diria que funciona.
                Revisar("La fila " & FilaNombre & " es la del nombre",
                        EtiquetaDe(f, FilaNombre) = "Nombre")
                Revisar("La fila " & FilaMarca & " es la de la marca",
                        EtiquetaDe(f, FilaMarca) = "Marca")
                Revisar("La fila " & FilaCodigoBarra & " es la del codigo de barras",
                        EtiquetaDe(f, FilaCodigoBarra) = "C" & ChrW(243) & "digo de barras")
                Revisar("La fila " & FilaDescripcion & " es la de la descripcion",
                        EtiquetaDe(f, FilaDescripcion) = "Descripci" & ChrW(243) & "n")

                For i = 0 To campos.Count - 1
                    Dim campo = campos(i)
                    Revisar($"Editar Producto: el campo de la fila {i} no es de solo lectura",
                            Not campo.ReadOnly)
                    Revisar($"Editar Producto: el campo de la fila {i} esta habilitado",
                            campo.Enabled)
                    Revisar($"Editar Producto: el campo de la fila {i} se puede escribir y borrar",
                            AceptaEscrituraYBorrado(campo))
                Next
            End Using
        End Sub

        ''' <summary>
        ''' Edita un producto cuyo precio tiene separador de miles y comprueba que lo
        ''' escrito en los cuatro campos queda en el catalogo. Antes del arreglo el
        ''' guardado terminaba en la alerta del precio y el catalogo quedaba igual:
        ''' el usuario perdia los cambios sin ver por que.
        ''' </summary>
        Sub GuardarProductoConPrecioDeMilNoPierdeLosCampos()
            Dim r = PruebasProductoEditor.CorrerEnProcesoHijo("guardar-precio")

            Revisar("Editar un producto con precio de 1000 o mas no se cuelga en " &
                    "una alerta (el hijo salio con codigo " & r.Codigo & ")",
                    r.Codigo = 0)

            If r.Codigo <> 0 Then
                Console.WriteLine("    salida del hijo: " & r.Salida.Trim())
            End If

            ' Esta comprobacion exige que el hijo haya llegado a terminar. Si solo
            ' mirara que no aparece "ALERTA:", pasaria en vacio cuando el hijo se
            ' cuelga en el MessageBox y muere sin imprimir nada, que es justo lo
            ' que hace cuando el bug esta presente.
            Revisar("Guardar un producto con precio de 1000 o mas no rebate el " &
                    "precio por alerta",
                    r.Codigo = 0 AndAlso Not Contiene(r.Salida, "ALERTA:"))
            Revisar("El formulario se cierra con OK al guardar bien",
                    Contiene(r.Salida, "CERRADO OK"))

            Revisar("Guardar deja el nombre escrito en el catalogo",
                    CampoEnCatalogo(r.Salida, "nombre") = NombreNuevo)
            Revisar("Guardar deja la marca escrita en el catalogo",
                    CampoEnCatalogo(r.Salida, "marca") = MarcaNueva)
            Revisar("Guardar deja el codigo de barras escrito en el catalogo",
                    CampoEnCatalogo(r.Salida, "codigo") = CodigoNuevo)
            Revisar("Guardar deja la descripcion escrita en el catalogo",
                    CampoEnCatalogo(r.Salida, "descripcion") = DescripcionNueva)
        End Sub

        ''' <summary>
        ''' El hijo: abre Editar Producto con un producto real cuyo precio es de 1000
        ''' o mas, escribe en los cuatro campos, aprieta Guardar y reporta que paso.
        ''' Sin reflexion: el boton se busca por su texto y los campos por su fila.
        ''' </summary>
        Sub MainHijoGuardar()
            Application.EnableVisualStyles()
            Application.SetCompatibleTextRenderingDefault(False)

            PrepararDatos()

            Dim producto = ProductoConPrecioAlto()
            Dim id = producto.ID

            Using f As New ProductoEditorForm(producto)
                f.Show()
                ForzarLayout(f)

                Dim campos = CamposDelBugB(f)
                Escribir(campos, FilaNombre, NombreNuevo)
                Escribir(campos, FilaMarca, MarcaNueva)
                Escribir(campos, FilaCodigoBarra, CodigoNuevo)
                Escribir(campos, FilaDescripcion, DescripcionNueva)

                Dim boton = BotonGuardar(f)
                If boton Is Nothing Then
                    Console.WriteLine("ALERTA: no hay boton Guardar en la pantalla")
                    Return
                End If

                boton.PerformClick()
                ForzarLayout(f)

                ' Si el precio no se pudo leer, Guardar vuelve antes de cerrar y la
                ' alerta que ve el usuario es la del precio, no sobre los campos.
                If f.DialogResult = DialogResult.OK Then
                    Console.WriteLine("CERRADO OK")
                Else
                    Console.WriteLine("ALERTA: el guardado se reboto con DialogResult " &
                                      f.DialogResult)
                End If
            End Using

            Dim guardado = ProductoService.PorId(id)
            Console.WriteLine("CAT nombre=" & If(guardado.Nombre, ""))
            Console.WriteLine("CAT marca=" & If(guardado.Marca, ""))
            Console.WriteLine("CAT codigo=" & If(guardado.CodigoBarra, ""))
            Console.WriteLine("CAT descripcion=" & If(guardado.Descripcion, ""))
        End Sub

        ''' <summary>
        ''' Un producto del catalogo cuyo precio tiene separador de miles, que es
        ''' justo el caso que rompia. Sin esto la prueba pasaria con cualquier
        ''' producto y no tocaria el bug.
        ''' </summary>
        Function ProductoConPrecioAlto() As Producto
            Return Datos.ListaProductos.FirstOrDefault(
                       Function(x) x.Precio >= 1000D AndAlso
                                 Not String.IsNullOrWhiteSpace(x.Nombre))
        End Function

        Sub PrepararDatos()
            Dim carpeta = Path.Combine(Path.GetTempPath(), "CafeteriaOS-pruebas-precios")
            If Directory.Exists(carpeta) Then Directory.Delete(carpeta, True)
            Directory.CreateDirectory(carpeta)
            Environment.SetEnvironmentVariable(IngresoAlmacen.VariableCarpeta, carpeta)

            DatosIniciales.Cargar()
            Datos.UsuarioActual = AutenticacionService.Buscar("admin@cafe.com", "123456")
        End Sub

        ''' <summary>Escribe y borra, como hace el usuario con el teclado.</summary>
        Function AceptaEscrituraYBorrado(campo As TextBox) As Boolean
            Dim original = campo.Text
            campo.Text = original & "-escrito"
            Dim escrita = campo.Text
            campo.Text = String.Empty
            Dim borrado = campo.Text
            campo.Text = original
            Return escrita.Contains("-escrito") AndAlso borrado.Length = 0
        End Function

        Sub Escribir(campos As List(Of TextBox), indice As Integer, valor As String)
            campos(indice).Text = valor
        End Sub

        Const FilaNombre As Integer = 0
        Const FilaMarca As Integer = 1
        Const FilaCodigoBarra As Integer = 2
        Const FilaDescripcion As Integer = 3

        ''' <summary>
        ''' Los cuatro campos del bug, por numero de fila.
        '''
        ''' Se los ubica por posicion y no por el texto de la etiqueta a proposito:
        ''' la etiqueta real lleva acentos ("Codigo de barras", "Descripcion") y
        ''' buscarla por nombre obliga a escribir esos acentos en el test, que es
        ''' justo la clase de error que hace que una prueba no pruebe nada.
        ''' Que la fila sea la correcta lo verifica EtiquetaDe.
        ''' </summary>
        Function CamposDelBugB(f As Form) As List(Of TextBox)
            Dim raiz = f.Controls.OfType(Of TableLayoutPanel)().FirstOrDefault()
            Dim lista As New List(Of TextBox)
            For Each fila As Integer In New Integer() {FilaNombre, FilaMarca,
                                                    FilaCodigoBarra, FilaDescripcion}
                Dim campo = TryCast(raiz.GetControlFromPosition(1, fila), TextBox)
                If campo IsNot Nothing Then lista.Add(campo)
            Next
            Return lista
        End Function

        ''' <summary>El texto de la etiqueta de una fila, para fijar que fila es cual.</summary>
        Function EtiquetaDe(f As Form, fila As Integer) As String
            Dim raiz = f.Controls.OfType(Of TableLayoutPanel)().FirstOrDefault()
            Dim etiqueta = TryCast(raiz.GetControlFromPosition(0, fila), Label)
            If etiqueta Is Nothing Then Return String.Empty
            Return etiqueta.Text
        End Function

        Function BotonGuardar(f As Form) As Button
            Return Descendientes(f).OfType(Of Button)().
                FirstOrDefault(Function(b) b.Text = "Guardar")
        End Function

        Function Descendientes(control As Control) As List(Of Control)
            Dim lista As New List(Of Control)
            For Each hijo As Control In control.Controls
                lista.Add(hijo)
                lista.AddRange(Descendientes(hijo))
            Next
            Return lista
        End Function

        Function Contiene(texto As String, aguja As String) As Boolean
            Return texto.IndexOf(aguja, StringComparison.Ordinal) >= 0
        End Function

        ''' <summary>
        ''' Lee "clave=valor" de la linea "catalogo ..." que reporta el hijo. Si la
        ''' clave no aparece devuelve cadena vacia, que no es igual a ningun valor
        ''' esperado: una asercion que no llego a comprobar nada tiene que fallar.
        ''' </summary>
        Function CampoEnCatalogo(salida As String, clave As String) As String
            Dim prefijo = "CAT " & clave & "="
            For Each linea As String In salida.Replace(vbCrLf, vbLf).Split(vbLf)
                If linea.StartsWith(prefijo, StringComparison.Ordinal) Then
                    Return linea.Substring(prefijo.Length).Trim()
                End If
            Next
            Return String.Empty
        End Function

        Sub ForzarLayout(control As Control)
            For i = 1 To 4
                control.PerformLayout()
                For Each hijo As Control In control.Controls
                    hijo.PerformLayout()
                Next
                Application.DoEvents()
            Next
        End Sub

    End Module

End Namespace
