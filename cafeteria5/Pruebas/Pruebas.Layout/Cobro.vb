Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Windows.Forms
Imports CafeteriaOS

Namespace Prueba

    ''' <summary>
    ''' Pruebas de las ventanas que se construyen solas y de los combos que
    ''' muestran objetos.
    '''
    ''' La del cobro importa por una razon particular: la excepcion no salia al
    ''' cobrar, sino al CONSTRUIR la ventana. Asignar el SelectedIndex del combo
    ''' dispara su handler en el acto, y ese handler tocaba controles que todavia
    ''' no existian. Dare que abortaba con NullReferenceException antes de que el
    ''' usuario pudiera elegir el medio de pago, siempre. Por eso la prueba
    ''' construye la ventana de verdad en vez de llamar a un metodo suelto.
    ''' </summary>
    Public Module PruebasCobro

        Public Sub Correr()
            Dim total As Decimal = 1000D

            ' --- El combo arranca con una seleccion y no al construirlo ---
            Using f As New CobroForm(total)
                Dim combo = PrimerCombo(f)
                Revisar("El combo de medio de pago arranca con algo elegido",
                        combo IsNot Nothing AndAlso combo.SelectedIndex = 0)
                Revisar("El medio de pago por defecto es efectivo",
                        combo.SelectedItem.ToString() = MetodoPago.Efectivo.ToString())
            End Using

            ' --- Todos los medios de pago se pueden elegir sin reventar ---
            Using f As New CobroForm(total)
                Dim combo = PrimerCombo(f)
                For Each nombre In [Enum].GetNames(GetType(MetodoPago))
                    combo.SelectedItem = nombre
                    Revisar("Se puede elegir el medio de pago " & nombre,
                            combo.SelectedItem.ToString() = nombre)
                Next
            End Using

            ' --- El campo de entregado solo aplica a efectivo ---
            Using f As New CobroForm(total)
                Dim combo = PrimerCombo(f)
                Dim entregado = PrimerInput(f)

                combo.SelectedItem = MetodoPago.Efectivo.ToString()
                Revisar("Con efectivo se puede escribir cuanto se entrega",
                        entregado.Enabled)

                combo.SelectedItem = MetodoPago.MercadoPago.ToString()
                Revisar("Con Mercado Pago no se escribe el entregado",
                        Not entregado.Enabled)
            End Using

            PruebasComboObjetos.Correr()
            PruebasStock.Correr()
        End Sub

        Private Function PrimerCombo(contenedor As Control) As ComboBox
            For Each c As Control In Arbol.Todos(contenedor)
                If TypeOf c Is ComboBox Then Return DirectCast(c, ComboBox)
            Next
            Return Nothing
        End Function

        Private Function PrimerInput(contenedor As Control) As TextBox
            For Each c As Control In Arbol.Todos(contenedor)
                If TypeOf c Is TextBox Then Return DirectCast(c, TextBox)
            Next
            Return Nothing
        End Function

    End Module

    ''' <summary>
    ''' Un combo que recibe objetos tiene que decir cual es cada uno. Sin
    ''' DisplayMember dibuja el nombre del tipo y la lista parece llena de
    ''' "CafeteriaOS.Proveedor": era lo que pasaba con los proveedores del ingreso
    ''' de stock, una lista llena pero donde no se lee ningun nombre.
    '''
    ''' La prueba usa el combo del IngresoStockForm de verdad y no uno armado aqui,
    ''' para que un arreglo en el formulario no pueda dejar el test en verde solo.
    ''' </summary>
    Public Module PruebasComboObjetos

        Public Sub Correr()

            ' Se prueba el comportamiento base de un combo con objetos...
            Dim combo = Tema.CrearCombo()
            combo.DisplayMember = NameOf(Proveedor.Nombre)

            Dim a As New Proveedor With {.Nombre = "Distribuidora del Norte"}
            Dim b As New Proveedor With {.Nombre = "Mayorista Sur"}
            combo.Items.Add("(sin proveedor)")
            combo.Items.Add(a)
            combo.Items.Add(b)

            ' Un combo con DropDownList muestra en .Text exactamente lo que dibuja
            ' en la fila elegida, asi que se comprueba lo que el usuario ve sin
            ' meterse a los metodos internos de WinForms.
            combo.SelectedIndex = 1
            Revisar("Un combo con DisplayMember dibuja el nombre del objeto",
                    combo.Text = "Distribuidora del Norte")
            Revisar("Elegir un proveedor entrega el objeto y no el texto",
                    ReferenceEquals(combo.SelectedItem, a))

            ' ...y despues el combo de proveedores del ingreso de stock real.
            Dim mio As New Proveedor With {.Nombre = "Proveedor Para La Prueba"}
            mio.Activo = True
            Dim cantidadInicial = Datos.ListaProveedores.Count
            Datos.ListaProveedores.Add(mio)

            Try
                Using f As New IngresoStockForm(Nothing)
                    Dim comboProveedor = ComboDeProveedores(f)
                    Revisar("El ingreso de stock tiene un combo de proveedores",
                            comboProveedor IsNot Nothing)

                    If comboProveedor IsNot Nothing Then
                        Dim indice = comboProveedor.Items.IndexOf(mio)
                        Revisar("El proveedor nuevo aparece en el combo",
                                indice >= 0)

                        If indice >= 0 Then
                            comboProveedor.SelectedIndex = indice
                            Revisar("El combo del ingreso muestra el nombre del proveedor",
                                    comboProveedor.Text = "Proveedor Para La Prueba")
                        End If

                        comboProveedor.SelectedIndex = 0
                        Revisar("La primera fila es la opcion sin proveedor",
                                comboProveedor.Text = "(sin proveedor)")
                    End If
                End Using
            Finally
                While Datos.ListaProveedores.Count > cantidadInicial
                    Datos.ListaProveedores.RemoveAt(Datos.ListaProveedores.Count - 1)
                End While
            End Try
        End Sub

        ''' <summary>El combo que recibe los objetos Proveedor entre los controles.</summary>
        Private Function ComboDeProveedores(contenedor As Control) As ComboBox
            For Each c As Control In Arbol.Todos(contenedor)
                If TypeOf c Is ComboBox Then
                    Dim box = DirectCast(c, ComboBox)
                    For Each item In box.Items
                        If TypeOf item Is Proveedor Then Return box
                    Next
                End If
            Next
            Return Nothing
        End Function

    End Module

    ''' <summary>
    ''' El stock entra por un solo boton: si hay un producto elegido se repone ese,
    ''' y si no hay ninguno se abre el ingreso de varias lineas. Dos botones para lo
    ''' mismo obligaban a decidir cual usar antes de saber si habia algo que reponer.
    '''
    ''' Lo que se puede comprobar sin abrir un dialogo modal es que quedo UN solo
    ''' boton de carga y que el panel se construye bien. Las dos pantallas de
    ''' destino ya tienen sus propias pruebas de layout.
    ''' </summary>
    Public Module PruebasStock

        Public Sub Correr()
            Using panel As New StockPanel()
                Dim deCarga = BotonesDeCarga(panel)

                Revisar("El stock tiene un solo boton para cargar mercaderia",
                        deCarga.Count = 1)
                Revisar("El boton unico se llama Reponer stock",
                        deCarga.Count = 1 AndAlso deCarga(0).Text = "Reponer stock")
                Revisar("No quedo el boton viejo de Ingresar stock",
                        Botones(panel).All(Function(b) b.Text <> "Ingresar stock"))
            End Using
        End Sub

        ''' <summary>Botones que sirven para meter mercaderia al deposito.</summary>
        Private Function BotonesDeCarga(contenedor As Control) As List(Of Button)
            Dim lista As New List(Of Button)
            For Each b In Botones(contenedor)
                Dim texto = b.Text.ToLowerInvariant()
                If texto.Contains("stock") Or texto.Contains("reponer") Or texto.Contains("ingresar") Then
                    lista.Add(b)
                End If
            Next
            Return lista
        End Function

        Private Function Botones(contenedor As Control) As List(Of Button)
            Dim lista As New List(Of Button)
            For Each c As Control In Arbol.Todos(contenedor)
                If TypeOf c Is Button Then lista.Add(DirectCast(c, Button))
            Next
            Return lista
        End Function

    End Module

    ''' <summary>
    ''' Todos los controles del arbol, en profundidad. No se usa Yield Return porque
    ''' VB no deja declarar iteradores dentro de un Module.
    '''
    ''' ControlCollection no implementa IEnumerable(Of Control), solo el IEnumerable
    ''' sin generico: sin el "As Control" la variable se infiere como Object y el
    ''' Option Strict del harness no deja convertirla.
    ''' </summary>
    Public Module Arbol

        Public Function Todos(contenedor As Control) As List(Of Control)
            Dim lista As New List(Of Control)
            For Each c As Control In contenedor.Controls
                lista.Add(c)
                lista.AddRange(Todos(c))
            Next
            Return lista
        End Function

    End Module

End Namespace
