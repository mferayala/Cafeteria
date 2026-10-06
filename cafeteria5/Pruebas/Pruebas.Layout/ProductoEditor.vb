Imports System.IO
Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Threading.Tasks
Imports System.Linq
Imports System.Windows.Forms
Imports CafeteriaOS

Namespace Prueba

    ''' <summary>
    ''' Pruebas del flujo real de ProductoEditorForm, sin reflexion: se construye el
    ''' formulario, se escribe en los controles como lo haria el usuario y se mira lo
    ''' que pasa. El bug que estas pruebas persiguen es un StackOverflowException al
    ''' elegir categoria, y un StackOverflow no se puede atrapar en .NET: mata el
    ''' proceso entero. Por eso el flujo SIEMPRE corre en un proceso hijo aparte, y el
    ''' padre juzga por el codigo de salida y la salida por consola. Si se corriera en
    ''' proceso propio, un fallo mataria al runner y no habria forma de reportarlo.
    '''
    ''' El hijo se lanza a si mismo con el nombre del escenario como argumento.
    ''' </summary>
    Module PruebasProductoEditor

        Sub Correr()
            ReproducirRecursionAlElegirCategoria()
            NoSeRepiteAlElegirCategoriaEnEditar()
        End Sub

        ''' <summary>
        ''' El padre tiene que poder confirmar que el hijo hizo lo que se le pidio y
        ''' no otro camino. Si el hijo ignora el escenario y siempre arma un producto
        ''' nuevo, la prueba de "editar" pasa en verde probando el alta: un test que
        ''' miente es peor que no tener test. Por eso el hijo anuncia con que producto
        ''' abrio la pantalla y el padre lo verifica.
        ''' </summary>
        Const EtiquetaEdicion As String = "editando el producto"

        ''' <summary>
        ''' El bug: txtCategoria.TextChanged (linea 119) llama a CargarSugerencias, y
        ''' CargarSugerencias escribe txtCategoria.Text (linea 230) para restaurar lo
        ''' que el usuario eligio. Escribir el Text vuelve a disparar TextChanged, que
        ''' vuelve a llamar a CargarSugerencias. Se agota la pila y el proceso muere.
        '''
        ''' El sintoma aparece en txtCategoria.Items.Add porque ahi se profundiza la
        ''' recursion, no porque esa linea sea la culpable.
        ''' </summary>
        Sub ReproducirRecursionAlElegirCategoria()
            Dim r = CorrerEnProcesoHijo("nuevo")

            Revisar("Elegir una categoria en +Producto no cuelga el proceso " &
                    "(el hijo salio con codigo " & r.Codigo & ")",
                    r.Codigo = 0)

            If r.Codigo <> 0 Then
                Console.WriteLine("    salida del hijo: " & r.Salida.Trim())
            End If

            ' El fix es que CargarSugerencias se invoque un numero acotado de veces
            ' por cambio de categoria. Antes de arreglarlo eran cientos de llamadas
            ' y el proceso moria; ahora tiene que quedar debajo del tope.
            Dim llamadas = LeerNumeroReportado(r.Salida, "llamadas a CargarSugerencias")
            Revisar($"Elegir una categoria mantiene acotadas las llamadas a " &
                    $"CargarSugerencias (fueron {llamadas}, tope {TopeLlamadas})",
                    llamadas >= 0 AndAlso llamadas <= TopeLlamadas)
        End Sub

        ''' <summary>
        ''' El mismo bug llegaba por el otro lado: al editar un producto que ya tiene
        ''' categoría, CargarDatos escribe la categoría y eso disparaba el mismo ciclo.
        ''' </summary>
        Sub NoSeRepiteAlElegirCategoriaEnEditar()
            Dim r = CorrerEnProcesoHijo("editar")

            Revisar("Elegir una categoria en Editar Producto no cuelga el proceso " &
                    "(el hijo salio con codigo " & r.Codigo & ")",
                    r.Codigo = 0)

            If r.Codigo <> 0 Then
                Console.WriteLine("    salida del hijo: " & r.Salida.Trim())
            End If

            ' Antes de mirar el contador se verifica que el hijo haya abierto la
            ' pantalla de edicion y no la de alta. Si el escenario se ignorara,
            ' el contador dariia 1 igual y la prueba passaria sin probar nada.
            Dim abrioEdicion = r.Salida.Contains(EtiquetaEdicion)
            Revisar("Editar Producto abre la pantalla con un producto del catalogo " &
                    "y no arma uno nuevo", abrioEdicion)

            Dim llamadas = LeerNumeroReportado(r.Salida, "llamadas a CargarSugerencias")
            Revisar($"Editar un producto existente mantiene acotadas las llamadas a " &
                    $"CargarSugerencias (fueron {llamadas}, tope {TopeLlamadas})",
                    llamadas >= 0 AndAlso llamadas <= TopeLlamadas)
        End Sub

        ''' <summary>
        ''' Lanza este mismo ejecutable con un escenario como argumento y espera. Al
        ''' ser un WinExe no hay console pegado, asi que se redirige la salida y se
        ''' espera a que termine.
        ''' </summary>
        Function CorrerEnProcesoHijo(escenario As String) As ResultadoHijo
            Dim propio = Process.GetCurrentProcess().MainModule.FileName
            Dim psi = New ProcessStartInfo(propio, escenario) With {
                .UseShellExecute = False,
                .RedirectStandardOutput = True,
                .RedirectStandardError = True,
                .CreateNoWindow = True
            }
            Using p = Process.Start(psi)
                ' Los dos flujos se leen en paralelo mientras corre el hijo. Si se
                ' esperara a que termine y despues se leyera, el hijo que muere por
                ' stack overflow vuelca una traza enorme en stderr, llena el buffer
                ' del pipe, se queda bloqueado escribiendo y nunca sale: el runner
                ' cuelga en vez de reportar el fallo. Con un StackOverflowException
                ' no se puede hacer catch, asi que separar el proceso es la unica
                ' forma de survives.
                Dim salida = New System.Text.StringBuilder()
                Dim tOut = Task.Run(Sub() salida.Append(p.StandardOutput.ReadToEnd()))
                Dim tErr = Task.Run(Sub() salida.Append(p.StandardError.ReadToEnd()))

                p.WaitForExit(120000)
                If Not p.HasExited Then
                    p.Kill(True)
                    Return New ResultadoHijo(-1, "el hijo no termino en 120 segundos")
                End If
                Task.WaitAll({tOut, tErr}, 30000)
                Return New ResultadoHijo(p.ExitCode, salida.ToString())
            End Using
        End Function

        Class ResultadoHijo
            Public Sub New(codigo As Integer, salida As String)
                Me.Codigo = codigo
                Me.Salida = salida
            End Sub
            Public ReadOnly Codigo As Integer
            Public ReadOnly Salida As String
        End Class

        ''' <summary>
        ''' Saca el numero que el hijo reporto. Contar cuantas veces aparece la frase
        ''' "llamadas a CargarSugerencias" no sirve: esa frase aparece una vez por
        ''' linea, asi que el contador daba 1 siempre, el valor real fuera 1 o 500, y
        ''' la prueba pasaba igual. Hay que leer el numero.
        ''' </summary>
        Function LeerNumeroReportado(texto As String, etiqueta As String) As Integer
            Dim i = texto.IndexOf(etiqueta, StringComparison.Ordinal)
            If i < 0 Then Return -1
            i = texto.IndexOf("=", i, StringComparison.Ordinal)
            If i < 0 Then Return -1
            i += 1
            ' El hijo escribe "= 2" con espacio: sin saltearlo el primer caracter
            ' no es un digito, el While no avanza nunca y la funcion devuelve -1
            ' como si el numero no estuviera.
            While i < texto.Length AndAlso (texto(i) = " "c OrElse texto(i) = vbTab)
                i += 1
            End While
            Dim fin = i
            While fin < texto.Length AndAlso
                  (Char.IsDigit(texto(fin)) OrElse texto(fin) = "-")
                fin += 1
            End While
            If fin = i Then Return -1
            Dim n As Integer
            If Integer.TryParse(texto.Substring(i, fin - i), n) Then Return n
            Return -1
        End Function

        ''' <summary>
        ''' Cuantas entradas a CargarSugerencias se permiten por cada eleccion de
        ''' categoria.
        '''
        ''' El numero NO es 1, y no por un defecto de la aplicacion: al escribir el
        ''' Text de un ComboBox, WinForms dispara TextChanged, y despues, al resolver
        ''' el SelectedIndex, vuelve a escribir el Text y dispara TextChanged otra
        ''' vez. Son dos eventos de la misma eleccion del usuario, y cada uno entra una
        ''' vez a CargarSugerencias. El flag cargandoSugerencias evita que una entrada
        ''' se anide dentro de otra; no puede (y no debe) evitar dos entradas
        ''' sucesivas.
        '''
        ''' Lo que el arreglo garantiza es que el numero este acotado. Antes eran
        ''' cientos y el proceso moria por stack overflow; ahora son 2. Por eso el
        ''' techo es 4: holgado para no depender de detalles de WinForms, y estrecho
        ''' para seguir detectando la reentrada.
        ''' </summary>
        Const TopeLlamadas As Integer = 4

        ''' <summary>
        ''' Cuerpo del proceso hijo. Simula lo que hace el usuario de verdad: crea el
        ''' formulario, muestra, y escribe el texto de la categoria en el combo. No
        ''' usa reflexion para nada.
        ''' </summary>
        Sub MainHijo(escenario As String)
            Application.EnableVisualStyles()
            Application.SetCompatibleTextRenderingDefault(False)

            Dim carpeta = Path.Combine(Path.GetTempPath(), "CafeteriaOS-pruebas-producto")
            If Directory.Exists(carpeta) Then Directory.Delete(carpeta, True)
            Directory.CreateDirectory(carpeta)
            Environment.SetEnvironmentVariable(IngresoAlmacen.VariableCarpeta, carpeta)

            DatosIniciales.Cargar()
            Datos.UsuarioActual = AutenticacionService.Buscar("admin@cafe.com", "123456")

            ' El contador va en un archivo porque la recursion ocurre adentro del
            ' formulario y el hijo tiene que|reportar cuantas veces se entro.
            Dim archivoContador = Path.Combine(carpeta, "contador.txt")

            ' En "editar" se abre la pantalla con un producto del catalogo, que es
            ' exactamente lo que hace el usuario al tocar Editar. Con Nothing la
            ' pantalla arma un alta nueva y el camino de edicion no se ejercita.
            Dim productoAEditar As Producto = Nothing
            If escenario = "editar" Then
                productoAEditar = Datos.ListaProductos.FirstOrDefault(
                                     Function(p) Not String.IsNullOrWhiteSpace(p.Categoria))
            End If

            Using f As New ProductoEditorForm(productoAEditar)
                f.Show()
                ForzarLayout(f)

                Dim combo = BuscarComboCategoria(f)
                If combo Is Nothing Then
                    Console.WriteLine("NO SE ENCONTRO EL COMBO DE CATEGORIA")
                    Return
                End If

                If productoAEditar IsNot Nothing Then
                    Console.WriteLine(EtiquetaEdicion & " " & productoAEditar.ID &
                                      " '" & productoAEditar.Nombre & "' categoria '" &
                                      productoAEditar.Categoria & "'")
                Else
                    Console.WriteLine("armando un producto nuevo")
                End If

                Instrumentar(f, combo, archivoContador)

                ' Esto es lo que hace el usuario: elige una categoria. Se elige una
                ' DISTINTA a la que ya tiene, porque en un combo escribir el mismo
                ' texto que ya tiene no dispara TextChanged y la prueba pasaria sin
                ' haber recorrido el camino del bug.
                Dim actual = If(combo.Text, "")
                Dim categorias = ProductoService.Categorias()
                Dim elegida = categorias.FirstOrDefault(
                                  Function(c) Not String.Equals(c, actual,
                                                               StringComparison.CurrentCultureIgnoreCase))
                If elegida Is Nothing Then elegida = categorias.First()
                combo.Text = elegida

                ForzarLayout(f)

                Dim veces = If(File.Exists(archivoContador),
                               Integer.Parse(File.ReadAllText(archivoContador)), 0)
                Console.WriteLine($"llamadas a CargarSugerencias = {veces}")
            End Using
        End Sub

        ''' <summary>
        ''' Cuenta las entradas a CargarSugerencias metiendo un handler nuestro en el
        ''' combo que se dispara cada vez que se reasigna el Text. Es el mismo mecanismo
        ''' que usa el bug, asi que si la recursion vuelve, el handler la ve.
        ''' </summary>
        Sub Instrumentar(f As Form, combo As ComboBox, archivoContador As String)
            If File.Exists(archivoContador) Then File.Delete(archivoContador)
            Dim veces As Integer
            AddHandler combo.TextChanged,
                Sub()
                    veces += 1
                    Try
                        File.WriteAllText(archivoContador, veces.ToString())
                    Catch
                        ' Si la recursion es infinita el archivo se queda sin escribir.
                        ' No importa: con el proceso muerto ya se sabe que hay bug.
                    End Try
                End Sub
        End Sub

        Function BuscarComboCategoria(f As Form) As ComboBox
            For Each c In Descendientes(f)
                Dim combo = TryCast(c, ComboBox)
                If combo Is Nothing Then Continue For
                ' El de categoria es el que esta en DropDown y ya trae las
                ' categorias del catalogo.
                If combo.DropDownStyle = ComboBoxStyle.DropDown AndAlso
                   combo.Items.Count > 0 Then
                    Return combo
                End If
            Next
            Return Nothing
        End Function

        Function Descendientes(control As Control) As List(Of Control)
            Dim lista As New List(Of Control)
            For Each hijo As Control In control.Controls
                lista.Add(hijo)
                lista.AddRange(Descendientes(hijo))
            Next
            Return lista
        End Function

        Sub ForzarLayout(control As Control)
            For i = 1 To 4
                control.PerformLayout()
                For Each hijo As Control In control.Controls
                    hijo.PerformLayout()
                Next
            Next
        End Sub

    End Module

End Namespace
