# Paso a paso: actualizar la base de datos

Esta guía cubre todo lo que hay que hacer en MySQL (phpMyAdmin) para la nueva versión del programa:
fotos de productos, recuperación de contraseña por correo, productos unificados, stock en números enteros y numeración compartida entre varias PCs.

Seguí los pasos **en orden** y marcá cada casilla. Si algún resultado no coincide con lo que dice la guía, **pará ahí** y no sigas con el próximo paso.

**¿Qué parte te toca?**
- **Ya tenés la base `cafeteria` con datos** (ventas, stock, etc.) → seguí desde el **Paso 1**.
- **Es una instalación nueva, sin datos** → andá directo a la **[Instalación desde cero](#instalación-desde-cero)** y después seguí desde el **Paso 6**.

> En todos los pasos, "la pestaña **SQL**" significa: en phpMyAdmin hacé clic en la base **cafeteria** (columna izquierda) y después en la pestaña **SQL** de arriba. Pegá la consulta y tocá **Continuar**.

---

## Paso 0 — Preparación

- [ ] **Cerrá el programa en TODAS las PCs** (caja, cocina, etc.).
  El programa numera ventas y movimientos de stock al abrirse. Si queda abierto mientras corre la actualización, el próximo movimiento que registre puede chocar con los que agrega el script.
- [ ] En el **XAMPP Control Panel**, **Apache** y **MySQL** están en verde.
- [ ] Entrá a `http://localhost/phpmyadmin`.

---

## Paso 1 — Copia de seguridad (backup)

Si algo sale mal, con esta copia la base vuelve exactamente a como estaba.

1. Hacé clic en la base **cafeteria** (columna izquierda).
2. Pestaña **Exportar**.
3. En **Método de exportación**, elegí **Personalizado**.
4. Bajá hasta **Opciones de creación de objetos** y marcá **"Añadir sentencia DROP TABLE / VIEW / PROCEDURE / FUNCTION / EVENT / TRIGGER"**.
   (Sin esta opción, el backup no se puede volver a importar sobre la base existente).
5. Abajo de todo, tocá **Continuar**. Se descarga un archivo `cafeteria.sql`.
6. Renombralo con la fecha, por ejemplo `backup_cafeteria_2026-11-16.sql`, y guardalo **fuera** de la carpeta del proyecto (en un pendrive o en Documentos).

- [ ] El archivo de backup existe y pesa más de 50 KB.

---

## Paso 2 — Revisar que el stock no tenga decimales

El programa ahora cuenta el stock en unidades enteras. Si en la base hay alguna cantidad con decimales (por ejemplo 2,5), se redondearía. Esta consulta lo revisa **sin cambiar nada**.

En la pestaña **SQL**, pegá y ejecutá:

```sql
SELECT 'productos' AS tabla, COUNT(*) AS con_decimales FROM productos
 WHERE stock_actual <> ROUND(stock_actual) OR stock_minimo <> ROUND(stock_minimo)
UNION ALL SELECT 'detalle_ventas',   COUNT(*) FROM detalle_ventas   WHERE cantidad <> ROUND(cantidad)
UNION ALL SELECT 'detalle_pedidos',  COUNT(*) FROM detalle_pedidos  WHERE cantidad <> ROUND(cantidad)
UNION ALL SELECT 'detalle_ingresos', COUNT(*) FROM detalle_ingresos WHERE cantidad <> ROUND(cantidad)
UNION ALL SELECT 'movimientos_stock', COUNT(*) FROM movimientos_stock
 WHERE cantidad <> ROUND(cantidad) OR stock_anterior <> ROUND(stock_anterior) OR stock_posterior <> ROUND(stock_posterior);
```

- [ ] Las 5 filas dicen **0** en `con_decimales`.

Si alguna dice más de 0: **no sigas**. Avisame qué tabla es, porque hay que decidir qué hacer con esos valores antes de actualizar.

---

## Paso 3 — Ejecutar la actualización

1. Hacé clic en la base **cafeteria**.
2. Pestaña **Importar**.
3. **Seleccionar archivo** → elegí **`actualizacion_login_imagenes.sql`** (está en la carpeta del proyecto, junto a `cafeteria.sql`).
4. **Conjunto de caracteres del archivo:** `utf-8` (es el que viene por defecto).
5. Abajo de todo, tocá **Importar** (o **Continuar**).

- [ ] Aparece un mensaje verde: *"La importación se ejecutó exitosamente"*.

Qué hace este archivo:
1. Agrega la columna `imagen` a `productos`.
2. Crea las tablas `configuracion_correo` y `recuperaciones_contrasena`.
3. Pasa el stock y las cantidades a números enteros.
4. **Unifica los productos "local" / "para llevar":**
   - Queda uno solo por par, con el precio de local, y suma el stock del otro.
   - El otro queda dado de baja (oculto). Las ventas viejas lo siguen mostrando.
   - Cada pase de stock queda registrado como un **Ajuste** en los movimientos de stock.
5. Corrige los nombres de los productos (tildes y abreviaturas como "c/leche" o "JYQ").
6. Crea la tabla `secuencias`, de donde todas las PCs sacan el próximo número de venta, pedido, movimiento de stock e ingreso. Así dos cajas nunca dan el mismo número.
7. Agrega a ventas, movimientos e ingresos una columna con la hora de la última modificación, que usa cada PC para enterarse de lo que hicieron las otras.

> **No borra nada** y **se puede ejecutar de nuevo** sin problema: lo que ya está hecho no se repite (el stock no se suma dos veces).

Si sale un **error en rojo**: copiá el mensaje completo y mandámelo. No borres nada. Como el script se puede repetir, una vez corregido el problema se vuelve a importar.

---

## Paso 4 — Verificar la actualización

En la pestaña **SQL**, ejecutá cada consulta y compará con el resultado esperado.

**4.1 — La columna y las tablas nuevas existen**
```sql
SELECT
 (SELECT COUNT(*) FROM information_schema.columns
   WHERE table_schema = 'cafeteria' AND table_name = 'productos' AND column_name = 'imagen') AS columna_imagen,
 (SELECT COUNT(*) FROM information_schema.tables
   WHERE table_schema = 'cafeteria'
     AND table_name IN ('configuracion_correo', 'recuperaciones_contrasena')) AS tablas_nuevas;
```
- [ ] `columna_imagen` = **1** y `tablas_nuevas` = **2**.

**4.2 — Los productos quedaron unificados**
```sql
SELECT id, nombre, stock_actual, activo FROM productos WHERE id BETWEEN 30 AND 39 ORDER BY id;
```
- [ ] Los ids **30, 32, 34, 36 y 38** tienen `activo` = **1** y nombres sin "local" (por ejemplo, "Licuado común").
- [ ] Los ids **31, 33, 35, 37 y 39** tienen `activo` = **0** y `stock_actual` = **0**.

**4.3 — Ningún producto a la venta dice "local" o "llevar"**
```sql
SELECT id, nombre FROM productos
 WHERE activo = 1 AND (nombre LIKE '%local%' OR nombre LIKE '%llevar%');
```
- [ ] **No devuelve ninguna fila.**
  Si aparece alguno, es un producto que cargaste vos con otro nombre. Corregilo a mano desde el programa (**Stock → Editar producto**).

**4.4 — El pase de stock quedó registrado**
```sql
SELECT producto_id, cantidad, stock_anterior, stock_posterior, motivo
  FROM movimientos_stock WHERE referencia_tipo = 'Unificacion' ORDER BY id;
```
- [ ] Hay **dos filas por cada par**: una positiva en el producto que queda y una negativa en el que se da de baja.
  Si el que se dio de baja tenía stock 0, ese par no tiene filas, y está bien.

**4.5 — La numeración compartida arranca bien**
```sql
SELECT s.nombre, s.valor,
       CASE s.nombre
         WHEN 'ventas'            THEN (SELECT COALESCE(MAX(id), 0) FROM ventas)
         WHEN 'pedidos'           THEN (SELECT COALESCE(MAX(id), 0) FROM pedidos)
         WHEN 'movimientos_stock' THEN (SELECT COALESCE(MAX(id), 0) FROM movimientos_stock)
         WHEN 'ingresos_stock'    THEN (SELECT COALESCE(MAX(id), 0) FROM ingresos_stock)
       END AS mayor_usado
  FROM secuencias s;
```
- [ ] Hay **4 filas**, y en cada una `valor` es **igual o mayor** que `mayor_usado`.

**4.6 — El stock quedó en enteros**
```sql
SELECT table_name, column_name, data_type FROM information_schema.columns
 WHERE table_schema = 'cafeteria'
   AND column_name IN ('stock_actual', 'stock_minimo', 'cantidad', 'stock_anterior', 'stock_posterior')
   AND table_name IN ('productos', 'detalle_ventas', 'detalle_pedidos', 'detalle_ingresos', 'movimientos_stock');
```
- [ ] Todas las filas dicen **int**.

---

## Paso 5 — Abrir el programa nuevo

1. En Visual Studio, abrí `cafeteria5.sln` y compilá con **Compilar → Recompilar solución**.
2. Si el programa corre en otras PCs, copiales **esta versión nueva**. Una versión vieja no conoce las columnas nuevas.
3. Ejecutá con **F5**.

- [ ] Se abre el login **sin** el aviso *"A la base de datos le faltan las tablas nuevas"*.
  Si aparece ese aviso, el Paso 3 no se ejecutó sobre la base `cafeteria`: repetilo.
- [ ] En el login **no** aparecen las cuentas de prueba, y están "Mostrar contraseña" y "¿Olvidaste tu contraseña?".
- [ ] En el **Punto de venta**, cada licuado y la limonada en vaso aparecen **una sola vez**. Las tarjetas sin foto muestran las iniciales del producto.

**Si usan más de una PC** (por ejemplo, caja y cocina):
- [ ] Hacé una venta con "Preparar" en la caja. En menos de 15 segundos tiene que aparecer en el tablero de **Pedidos** de la otra PC, sin reiniciar. Si estás apurado, tocá **F5**.
- [ ] Si el título de la ventana dice **"SIN CONEXION CON LA BASE DE DATOS"**, esa PC perdió contacto con MySQL: revisá la red y que MySQL siga en verde. Se reconecta sola.

---

## Paso 6 — Configurar el correo que manda los códigos

Sin esto, "¿Olvidaste tu contraseña?" muestra un aviso y no manda nada.

> Usá una cuenta de Gmail **del negocio**, no una personal. La contraseña que cargues queda guardada en la base y la puede ver cualquiera que entre a phpMyAdmin.

**6.1 — Activar la verificación en 2 pasos** (Google la exige para el paso 6.2)
1. Entrá a `https://myaccount.google.com` con la cuenta del negocio.
2. **Seguridad** → **Verificación en 2 pasos** → activala y seguí las instrucciones.

- [ ] La verificación en 2 pasos figura como **activada**.

**6.2 — Crear una "contraseña de aplicación"**
1. Entrá a `https://myaccount.google.com/apppasswords`.
2. En el nombre de la app escribí `Cafeteria` y tocá **Crear**.
3. Google muestra una clave de **16 letras** (por ejemplo `abcd efgh ijkl mnop`). Copiala **sin los espacios**.
   Solo se muestra una vez; si la perdés, borrala y creá otra.

- [ ] Tengo anotada la clave de 16 letras.

> La contraseña **normal** de Gmail **no funciona** acá. Tiene que ser la de aplicación.

**6.3 — Cargarla en la base**

En la pestaña **SQL**, reemplazá los tres datos y ejecutá:
```sql
UPDATE configuracion_correo
   SET usuario    = 'cuentadelnegocio@gmail.com',
       contrasena = 'abcdefghijklmnop',
       remitente  = 'Cafetería'
 WHERE id = 1;
```
No hace falta tocar `servidor_smtp` (`smtp.gmail.com`), `puerto` (**587**) ni `usar_ssl` (**1**): ya vienen bien para Gmail. **No uses el puerto 465**: el programa no lo soporta.

- [ ] La consulta dice *"1 fila afectada"*.

---

## Paso 7 — Cargar el correo de cada usuario

El código de recuperación llega al correo que el usuario tiene cargado en la base.

1. Mirá los usuarios:
   ```sql
   SELECT id, nombre_usuario, email, telefono FROM usuarios;
   ```
2. Por cada usuario, poné su correo **real** (cambiá el correo y el número de `id`):
   ```sql
   UPDATE usuarios SET email = 'correo.real@gmail.com' WHERE id = 1;
   ```
   No puede haber dos usuarios con el mismo correo; si lo intentás, MySQL da un error de duplicado.

- [ ] Todos los usuarios que van a usar el sistema tienen su correo real.

> Ojo: el login también acepta el correo como usuario. Desde ahora, quien entraba con `admin@cafe.com` entra con el correo nuevo, o con el nombre de usuario o el teléfono.

---

## Paso 8 — Probar la recuperación de contraseña

1. Abrí el programa → **¿Olvidaste tu contraseña?**
2. Escribí tu usuario o correo → **Enviar código**.
3. Revisá tu correo. Si no aparece en la bandeja de entrada, mirá en **Spam**.
4. Escribí el código, la contraseña nueva dos veces → **Cambiar contraseña**.
5. Ingresá con la contraseña nueva.

- [ ] Llegó el correo y pude entrar con la contraseña nueva.

Si algo falla, el programa muestra el motivo:

| Mensaje | Qué revisar |
|---|---|
| *"El envío de correos no está configurado"* | El Paso 6.3: falta `usuario` o `contrasena`. |
| *"...Authentication Required"* o *"5.7.0"* | La clave no es una contraseña de aplicación, tiene espacios, o falta la verificación en 2 pasos (Pasos 6.1 y 6.2). |
| *"...timed out"* o *"No se puede conectar"* | Que haya internet, que el puerto sea **587** y que el antivirus o firewall no bloquee el programa. |
| *"Ese usuario no tiene correo cargado"* | El Paso 7. |
| *"Ya se envió un código hace un momento"* | Esperá un minuto antes de pedir otro. |
| *"El código venció o ya se usó"* | Pasaron más de 15 minutos o ya se usó: pedí uno nuevo. |

---

## Paso 9 — Cambiar las contraseñas de ejemplo

Las cuentas que crea el script tienen contraseñas conocidas (`123456`, `caja123`, `cocina123`). Hay que cambiarlas antes de usar el sistema en el negocio.

- **Forma recomendada:** con el Paso 8 ("¿Olvidaste tu contraseña?"), cada uno desde su usuario.
- **Forma manual** (si todavía no está el correo): en la pestaña **SQL**, una vez por usuario:
  ```sql
  UPDATE usuarios SET password_hash = SHA2('LaClaveNueva', 256) WHERE nombre_usuario = 'Administrador';
  ```
  phpMyAdmin puede guardar esta consulta en su historial, con la clave a la vista. Si usás esta forma, después cambiá la clave de nuevo desde el programa.

- [ ] Ningún usuario sigue con su contraseña de ejemplo.

---

## Paso 10 — Cargar las fotos de los productos

**10.1 — Fotos del menú (de una sola vez)**

Las fotos del documento "Menú de imágenes Cafetería" ya están preparadas en el archivo **`imagenes_productos.sql`**, uno por producto.

1. phpMyAdmin → clic en la base **cafeteria** → pestaña **Importar**.
2. Elegí **`imagenes_productos.sql`** → **Importar**.

Solo carga la foto en los productos que **todavía no tienen una**: si a alguno ya le pusieron foto desde el programa, la respeta. Se puede ejecutar más de una vez.

Para verificar, en la pestaña **SQL**:
```sql
SELECT id, nombre FROM productos WHERE activo = 1 AND imagen IS NULL;
```
- [ ] Solo aparece **"Exprimido chico"**, que no tiene foto en el documento.

Las mismas fotos, ya achicadas, están en la carpeta **`imagenes_productos/`** del proyecto, por si hay que volver a cargar alguna a mano.

**10.2 — Cambiar o agregar una foto**

Esto se hace desde el programa, no desde phpMyAdmin.

1. Abrí **Stock**, elegí un producto y tocá **Editar producto**.
2. A la derecha, tocá **Elegir imagen...** y elegí la foto (JPG, PNG, BMP o GIF, hasta 20 MB).
3. Tocá **Guardar**.

El programa achica la foto (unos 400 px, 30–60 KB) antes de guardarla, así que podés usar fotos sacadas con el celular. Si la foto está de costado por cómo se sacó, se endereza sola.

- [ ] Las fotos aparecen en las tarjetas del **Punto de venta**.

---

## Si algo sale mal: volver al backup

1. phpMyAdmin → clic en la base **cafeteria** → pestaña **Importar**.
2. Elegí el backup del Paso 1 (`backup_cafeteria_...sql`) → **Importar**.
3. La base vuelve a como estaba antes del Paso 3. Usá la versión anterior del programa hasta resolver el problema.

---

## Instalación desde cero

**Solo para una base nueva o de prueba.** Si la base `cafeteria` ya existe, este paso **la borra entera**, con ventas, stock y usuarios incluidos.

1. phpMyAdmin → pestaña **Importar** (sin elegir ninguna base) → archivo **`cafeteria.sql`** → **Importar**.
2. Verificá en la pestaña **SQL**:
   ```sql
   SELECT
    (SELECT COUNT(*) FROM information_schema.tables
      WHERE table_schema = 'cafeteria' AND table_type = 'BASE TABLE') AS tablas,
    (SELECT COUNT(*) FROM cafeteria.productos) AS productos;
   ```
   - [ ] `tablas` = **26** y `productos` = **35**.
3. `cafeteria.sql` ya trae todo lo de los Pasos 2 a 4, así que **no** hace falta importar `actualizacion_login_imagenes.sql`.
4. Seguí desde el **Paso 5**.
