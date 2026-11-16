# Guía: conectar la Cafetería a MySQL (XAMPP)

Antes, el sistema guardaba todo en listas en memoria y lo perdía al cerrarse. Con estos cambios lee todo de MySQL al arrancar y guarda cada operación en el momento.

## Paso 1: Encender XAMPP
1. Abrí el **XAMPP Control Panel**.
2. Dale **Start** a **Apache** y a **MySQL**. Los dos tienen que quedar en verde.

## Paso 2: Crear la base con el script
1. Entrá a `http://localhost/phpmyadmin`.
2. Andá a la pestaña **Importar**, elegí el archivo **`cafeteria.sql`** (está en esta misma carpeta) y tocá **Importar**.
3. A la izquierda tiene que aparecer la base **cafeteria** con 26 tablas y 2 vistas.

> El script crea la base si no existe. Si ya existe, **borra las tablas y las vuelve a crear** con los datos de ejemplo. No lo vuelvas a importar cuando ya tengas ventas reales.

Para comprobarlo, en la pestaña **SQL** de la base `cafeteria` corré:
```sql
SELECT COUNT(*) FROM productos;      -- 35
SELECT * FROM vista_stock_bajo;      -- productos con poco stock
```

### Si ya tenías la base creada de antes
No la vuelvas a importar: seguí **[PASO_A_PASO_BASE_DE_DATOS.md](PASO_A_PASO_BASE_DE_DATOS.md)**. Ahí está, en orden y con verificaciones, cómo hacer el backup, actualizar la base sin perder datos, configurar el correo para recuperar contraseñas y cargar las fotos de los productos.

## Paso 3: Abrir el proyecto en Visual Studio
1. Abrí `cafeteria5.sln`.
2. El conector **MySql.Data** ya está agregado en `cafeteria5.vbproj`, y Visual Studio lo descarga solo al compilar. Si no lo hace, andá a **Proyecto → Administrar paquetes NuGet → Examinar**, buscá **MySql.Data** e instalalo.
3. Si tu usuario `root` de MySQL tiene contraseña, escribila en `BaseDatos/Db.vb`, en la línea `Pwd=;` (por ejemplo, `Pwd=miClave;`).

## Paso 4: Ejecutar y probar
Presioná **F5**. Usuarios que crea el script (podés entrar con el correo, el teléfono o el nombre). El login ya no los muestra en pantalla; **cambiá estas contraseñas** antes de usar el sistema en el negocio:

| Usuario | Contraseña | Rol |
|---|---|---|
| admin@cafe.com | 123456 | Administrador |
| cajera@cafe.com | caja123 | Cajero |
| cocina@cafe.com | cocina123 | Cocina |

Prueba recomendada:
1. Abrí la caja y hacé una venta en el punto de venta.
2. Cerrá el programa y volvé a abrirlo: la venta, el stock descontado y la caja abierta siguen ahí.
3. En phpMyAdmin, mirá las tablas `ventas`, `detalle_ventas`, `pagos`, `movimientos_stock` y `movimientos_caja`.

Si al arrancar sale "No se pudo conectar con la base de datos", revisá que MySQL esté en verde en XAMPP y que hayas importado el script.

---

## Cómo está armado el código

```
cafeteria5/
 └─ BaseDatos/
     ├─ Db.vb            Conexión, parámetros, transacciones y conversiones
     ├─ CargaBD.vb       Al arrancar, lee todas las tablas y llena las listas de Datos
     ├─ UsuarioRepo.vb   Login (UsuarioRepo) y configuración del negocio (ConfigRepo)
     ├─ CatalogoRepo.vb  Categorías, subcategorías y productos
     ├─ ProveedorRepo.vb Proveedores
     ├─ StockRepo.vb     Movimientos de stock e ingresos de mercadería
     ├─ CajaRepo.vb      Turnos de caja y sus movimientos
     └─ VentaRepo.vb     Ventas y pagos (VentaRepo), pedidos (PedidoRepo) y clientes (ClienteRepo)
```

**Idea general.** Las pantallas y los servicios siguen trabajando con las listas de `Datos` como antes. Lo nuevo es lo siguiente:
- Al arrancar, `CargaBD.CargarTodo()` llena esas listas desde MySQL (antes se usaba `DatosIniciales.Cargar()`).
- Cada vez que un servicio cambia algo, llama al repositorio correspondiente, que lo escribe en MySQL.
- Las operaciones que tocan varias tablas (por ejemplo, una venta: venta + detalle + pago + stock + caja) van dentro de una **transacción** (`Db.EnTransaccion`): se guardan todas o ninguna.
- Si MySQL rechaza la operación, el servicio la deshace también en memoria y muestra el error.

**Archivos que se modificaron:**
`Program.vb`, `Datos.vb`, los modelos `Usuario`, `Proveedor`, `Cliente` y `Caja` (se les agregó el `ID`), los servicios `Autenticacion`, `Producto`, `Categoria`, `Venta`, `Caja`, `Pedido`, `Stock`, `MovimientoStock`, `Ingreso` y `Cliente`, y las pantallas `KanbanPanel`, `POSPanel`, `ProveedoresPanel`, `ProveedorForm` y `ConfiguracionNegocioForm`.

**Los proyectos de Pruebas** siguen andando sin XAMPP: ahí `Db.Habilitada` queda en `False` y todo funciona en memoria, como antes.

---

## Ajustes respecto al diagrama original

| Tabla | Cambio | Por qué |
|---|---|---|
| `pedidos` / `ventas` | La FK va en `pedidos.venta_id`. Se quitó `ventas.pedido_id` | En el código el pedido de cocina se crea **después** de la venta (`PedidoService.CrearDesdeVenta`), y puede haber ventas sin pedido |
| `ventas` | Se agregaron `numero`, `cliente_nombre`, `cliente_telefono`, `tipo_servicio`, `mesa`, `metodo_pago_id`, `estado_pago`, `fue_a_cuenta`, `monto_pagado`, `efectivo_recibido`, `stock_descontado` y `anulada_por` | Son datos que la clase `Venta` maneja (ventas a cuenta, vuelto, ticket) |
| `detalle_ventas` | Se agregaron `producto_nombre` y `especificaciones` | La venta guarda una "foto" del producto: si mañana cambia el nombre o el precio, el historial no se altera |
| `usuarios` | Se agregaron `email` y `telefono` | El login acepta correo o teléfono |
| `productos` | Se agregaron `marca`, `atajo` y `subcategoria_id`. `unidad_medida` pasó de ENUM a VARCHAR | El formulario de producto los usa, y la unidad se escribe libremente |
| **nuevas** | `subcategorias`, `producto_opciones` | Subcategorías y opciones al vender (Leche: Entera / Descremada…) |
| **nuevas** | `ingresos_stock`, `detalle_ingresos` | Reemplazan el archivo `ingresos_stock.json` |
| **nueva** | `configuracion_negocio` | Nombre, CUIT, margen e IVA de la pantalla de configuración |
| `proveedores` | Más columnas y un campo `extras` (JSON) con contactos, teléfonos, archivos y cuenta corriente | La ficha del proveedor tiene muchos datos que no estaban en el diagrama |
| ENUMs | Los valores se escriben igual que en `Enums.vb` (`EnPreparacion`, `MercadoPago`…) | Así se convierten directo al Enum de VB sin una tabla de traducción |

**Triggers de auditoría:** cada cambio de precio o costo de un producto y cada anulación de una venta queda registrado en `auditoria`, con el usuario que lo hizo.

## Limitaciones a tener en cuenta
- Está pensado para **una sola caja**. Si dos PCs usan la misma base al mismo tiempo, los números de venta pueden chocar, porque los asigna el programa.
- Al arrancar se cargan todas las ventas y los movimientos. Con años de historial conviene cargar solo un período (por ejemplo, los últimos 90 días).
- Las contraseñas usan SHA-256. Para un sistema en producción conviene usar BCrypt (paquete `BCrypt.Net-Next`).
