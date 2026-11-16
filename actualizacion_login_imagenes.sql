-- =====================================================================
--  CAFETERIA - Actualizacion de una base que YA EXISTE
--
--  Que hace:
--    1. Agrega la columna para la foto de cada producto.
--    2. Agrega las tablas para recuperar la contraseña por correo.
--    3. Pasa el stock y las cantidades a numeros enteros.
--    4. Unifica los productos "local" / "para llevar" (eso se elige en el
--       punto de venta): queda uno solo con el precio de local, que suma el
--       stock del otro; el otro queda dado de baja (las ventas viejas lo
--       siguen mostrando).
--    5. Corrige nombres (tildes y abreviaturas como "c/leche" o "JYQ").
--    6. Numeracion compartida entre PCs (tabla secuencias) y fecha de
--       modificacion en ventas, movimientos e ingresos (sincronizacion).
--
--  NO borra datos y se puede ejecutar mas de una vez.
--  ANTES de ejecutarlo: cerrar el programa en todas las PCs y hacer un
--  backup (ver PASO_A_PASO_BASE_DE_DATOS.md).
--
--  Como usarlo: phpMyAdmin > base "cafeteria" > Importar > este archivo.
-- =====================================================================

SET NAMES utf8mb4;
USE cafeteria;


-- 1. Imagen de cada producto ---------------------------------------------
ALTER TABLE productos ADD COLUMN IF NOT EXISTS imagen MEDIUMBLOB NULL AFTER atajo;


-- 2.--  RECUPERACION DE CONTRASEÑA POR CORREO
-- ---------------------------------------------------------------------

-- Cuenta de correo con la que el programa manda los codigos (una sola fila).
-- Con Gmail: servidor smtp.gmail.com, puerto 587, usar_ssl 1, usuario = la
-- direccion de Gmail y contrasena = una "contraseña de aplicacion" de 16
-- letras (Cuenta de Google > Seguridad > Contraseñas de aplicaciones), NO la
-- contraseña normal de la cuenta.
CREATE TABLE IF NOT EXISTS configuracion_correo (
    id             TINYINT       NOT NULL,
    servidor_smtp  VARCHAR(150)  NOT NULL DEFAULT 'smtp.gmail.com',
    puerto         INT           NOT NULL DEFAULT 587,
    usar_ssl       TINYINT(1)    NOT NULL DEFAULT 1,
    usuario        VARCHAR(150)  NULL,
    contrasena     VARCHAR(255)  NULL,
    remitente      VARCHAR(150)  NULL,
    actualizado_en DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    CONSTRAINT ck_correo_fila_unica CHECK (id = 1)
) ENGINE=InnoDB;

-- Codigos enviados. Se guarda el hash SHA-256 del codigo, no el codigo.
-- Vence a los 15 minutos, admite 5 intentos y solo vale el ultimo enviado.
CREATE TABLE IF NOT EXISTS recuperaciones_contrasena (
    id           BIGINT     NOT NULL AUTO_INCREMENT,
    usuario_id   BIGINT     NOT NULL,
    codigo_hash  CHAR(64)   NOT NULL,
    expira_en    DATETIME   NOT NULL,
    intentos     INT        NOT NULL DEFAULT 0,
    usado        TINYINT(1) NOT NULL DEFAULT 0,
    creado_en    DATETIME   NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    KEY ix_recuperaciones_usuario (usuario_id, usado),
    CONSTRAINT fk_recuperaciones_usuario FOREIGN KEY (usuario_id) REFERENCES usuarios (id)
        ON DELETE CASCADE
) ENGINE=InnoDB;

INSERT IGNORE INTO configuracion_correo (id) VALUES (1);


-- 3. Stock en numeros enteros --------------------------------------------
-- El programa cuenta unidades enteras. Si hubiera algun valor con decimales
-- se redondearia: el paso a paso indica como revisarlo antes.
ALTER TABLE productos MODIFY stock_actual INT NOT NULL DEFAULT 0, MODIFY stock_minimo INT NOT NULL DEFAULT 0;
ALTER TABLE detalle_ingresos MODIFY cantidad INT NOT NULL;
ALTER TABLE detalle_ventas MODIFY cantidad INT NOT NULL;
ALTER TABLE detalle_pedidos MODIFY cantidad INT NOT NULL;
ALTER TABLE movimientos_stock MODIFY cantidad INT NOT NULL, MODIFY stock_anterior INT NOT NULL, MODIFY stock_posterior INT NOT NULL;


-- 4. Unificar "local" / "para llevar" -------------------------------------
-- Solo actua si el producto a dar de baja sigue activo y con su nombre de
-- siempre, asi que correrlo dos veces no suma el stock dos veces.
-- Cada pase de stock queda registrado en movimientos_stock como Ajuste.

START TRANSACTION;

-- Licuado comun para llevar -> producto 30
INSERT INTO movimientos_stock (producto_id, tipo, cantidad, stock_anterior, stock_posterior, referencia_tipo, motivo)
SELECT a.id, 'Ajuste', b.stock_actual, a.stock_actual, a.stock_actual + b.stock_actual, 'Unificacion',
       CONCAT('Suma el stock de "', b.nombre, '" (producto unificado)')
FROM productos a JOIN productos b
WHERE a.id = 30 AND b.id = 31 AND b.activo = 1 AND b.nombre IN ('Licuado comun para llevar', 'Licuado común (para llevar)') AND b.stock_actual > 0;
INSERT INTO movimientos_stock (producto_id, tipo, cantidad, stock_anterior, stock_posterior, referencia_tipo, motivo)
SELECT b.id, 'Ajuste', -b.stock_actual, b.stock_actual, 0, 'Unificacion',
       CONCAT('Stock pasado al producto #', a.id, ' (producto unificado)')
FROM productos a JOIN productos b
WHERE a.id = 30 AND b.id = 31 AND b.activo = 1 AND b.nombre IN ('Licuado comun para llevar', 'Licuado común (para llevar)') AND b.stock_actual > 0;
UPDATE productos a JOIN productos b ON a.id = 30 AND b.id = 31 AND b.activo = 1 AND b.nombre IN ('Licuado comun para llevar', 'Licuado común (para llevar)')
   SET a.stock_actual = a.stock_actual + b.stock_actual;
UPDATE productos b SET b.stock_actual = 0, b.atajo = NULL, b.activo = 0
 WHERE b.id = 31 AND b.activo = 1 AND b.nombre IN ('Licuado comun para llevar', 'Licuado común (para llevar)');

-- Licuado comun + tostado llevar -> producto 32
INSERT INTO movimientos_stock (producto_id, tipo, cantidad, stock_anterior, stock_posterior, referencia_tipo, motivo)
SELECT a.id, 'Ajuste', b.stock_actual, a.stock_actual, a.stock_actual + b.stock_actual, 'Unificacion',
       CONCAT('Suma el stock de "', b.nombre, '" (producto unificado)')
FROM productos a JOIN productos b
WHERE a.id = 32 AND b.id = 33 AND b.activo = 1 AND b.nombre IN ('Licuado comun + tostado llevar', 'Licuado común + tostado (para llevar)') AND b.stock_actual > 0;
INSERT INTO movimientos_stock (producto_id, tipo, cantidad, stock_anterior, stock_posterior, referencia_tipo, motivo)
SELECT b.id, 'Ajuste', -b.stock_actual, b.stock_actual, 0, 'Unificacion',
       CONCAT('Stock pasado al producto #', a.id, ' (producto unificado)')
FROM productos a JOIN productos b
WHERE a.id = 32 AND b.id = 33 AND b.activo = 1 AND b.nombre IN ('Licuado comun + tostado llevar', 'Licuado común + tostado (para llevar)') AND b.stock_actual > 0;
UPDATE productos a JOIN productos b ON a.id = 32 AND b.id = 33 AND b.activo = 1 AND b.nombre IN ('Licuado comun + tostado llevar', 'Licuado común + tostado (para llevar)')
   SET a.stock_actual = a.stock_actual + b.stock_actual;
UPDATE productos b SET b.stock_actual = 0, b.atajo = NULL, b.activo = 0
 WHERE b.id = 33 AND b.activo = 1 AND b.nombre IN ('Licuado comun + tostado llevar', 'Licuado común + tostado (para llevar)');

-- Licuado especial para llevar -> producto 34
INSERT INTO movimientos_stock (producto_id, tipo, cantidad, stock_anterior, stock_posterior, referencia_tipo, motivo)
SELECT a.id, 'Ajuste', b.stock_actual, a.stock_actual, a.stock_actual + b.stock_actual, 'Unificacion',
       CONCAT('Suma el stock de "', b.nombre, '" (producto unificado)')
FROM productos a JOIN productos b
WHERE a.id = 34 AND b.id = 35 AND b.activo = 1 AND b.nombre IN ('Licuado especial para llevar', 'Licuado especial (para llevar)') AND b.stock_actual > 0;
INSERT INTO movimientos_stock (producto_id, tipo, cantidad, stock_anterior, stock_posterior, referencia_tipo, motivo)
SELECT b.id, 'Ajuste', -b.stock_actual, b.stock_actual, 0, 'Unificacion',
       CONCAT('Stock pasado al producto #', a.id, ' (producto unificado)')
FROM productos a JOIN productos b
WHERE a.id = 34 AND b.id = 35 AND b.activo = 1 AND b.nombre IN ('Licuado especial para llevar', 'Licuado especial (para llevar)') AND b.stock_actual > 0;
UPDATE productos a JOIN productos b ON a.id = 34 AND b.id = 35 AND b.activo = 1 AND b.nombre IN ('Licuado especial para llevar', 'Licuado especial (para llevar)')
   SET a.stock_actual = a.stock_actual + b.stock_actual;
UPDATE productos b SET b.stock_actual = 0, b.atajo = NULL, b.activo = 0
 WHERE b.id = 35 AND b.activo = 1 AND b.nombre IN ('Licuado especial para llevar', 'Licuado especial (para llevar)');

-- Licuado especial + tostado llevar -> producto 36
INSERT INTO movimientos_stock (producto_id, tipo, cantidad, stock_anterior, stock_posterior, referencia_tipo, motivo)
SELECT a.id, 'Ajuste', b.stock_actual, a.stock_actual, a.stock_actual + b.stock_actual, 'Unificacion',
       CONCAT('Suma el stock de "', b.nombre, '" (producto unificado)')
FROM productos a JOIN productos b
WHERE a.id = 36 AND b.id = 37 AND b.activo = 1 AND b.nombre IN ('Licuado especial + tostado llevar', 'Licuado especial + tostado (para llevar)') AND b.stock_actual > 0;
INSERT INTO movimientos_stock (producto_id, tipo, cantidad, stock_anterior, stock_posterior, referencia_tipo, motivo)
SELECT b.id, 'Ajuste', -b.stock_actual, b.stock_actual, 0, 'Unificacion',
       CONCAT('Stock pasado al producto #', a.id, ' (producto unificado)')
FROM productos a JOIN productos b
WHERE a.id = 36 AND b.id = 37 AND b.activo = 1 AND b.nombre IN ('Licuado especial + tostado llevar', 'Licuado especial + tostado (para llevar)') AND b.stock_actual > 0;
UPDATE productos a JOIN productos b ON a.id = 36 AND b.id = 37 AND b.activo = 1 AND b.nombre IN ('Licuado especial + tostado llevar', 'Licuado especial + tostado (para llevar)')
   SET a.stock_actual = a.stock_actual + b.stock_actual;
UPDATE productos b SET b.stock_actual = 0, b.atajo = NULL, b.activo = 0
 WHERE b.id = 37 AND b.activo = 1 AND b.nombre IN ('Licuado especial + tostado llevar', 'Licuado especial + tostado (para llevar)');

-- Limonada vaso para llevar -> producto 38
INSERT INTO movimientos_stock (producto_id, tipo, cantidad, stock_anterior, stock_posterior, referencia_tipo, motivo)
SELECT a.id, 'Ajuste', b.stock_actual, a.stock_actual, a.stock_actual + b.stock_actual, 'Unificacion',
       CONCAT('Suma el stock de "', b.nombre, '" (producto unificado)')
FROM productos a JOIN productos b
WHERE a.id = 38 AND b.id = 39 AND b.activo = 1 AND b.nombre IN ('Limonada vaso para llevar', 'Limonada en vaso (para llevar)') AND b.stock_actual > 0;
INSERT INTO movimientos_stock (producto_id, tipo, cantidad, stock_anterior, stock_posterior, referencia_tipo, motivo)
SELECT b.id, 'Ajuste', -b.stock_actual, b.stock_actual, 0, 'Unificacion',
       CONCAT('Stock pasado al producto #', a.id, ' (producto unificado)')
FROM productos a JOIN productos b
WHERE a.id = 38 AND b.id = 39 AND b.activo = 1 AND b.nombre IN ('Limonada vaso para llevar', 'Limonada en vaso (para llevar)') AND b.stock_actual > 0;
UPDATE productos a JOIN productos b ON a.id = 38 AND b.id = 39 AND b.activo = 1 AND b.nombre IN ('Limonada vaso para llevar', 'Limonada en vaso (para llevar)')
   SET a.stock_actual = a.stock_actual + b.stock_actual;
UPDATE productos b SET b.stock_actual = 0, b.atajo = NULL, b.activo = 0
 WHERE b.id = 39 AND b.activo = 1 AND b.nombre IN ('Limonada vaso para llevar', 'Limonada en vaso (para llevar)');

COMMIT;


-- 5. Nombres corregidos ----------------------------------------------------
-- Cada UPDATE solo cambia el dato si todavia tiene el texto viejo: si ya lo
-- cambiaste a mano desde el programa, se respeta lo tuyo.
-- Las ventas viejas guardan el nombre con el que se vendieron y no cambian.

UPDATE categorias SET nombre = 'Cafés' WHERE id = 1 AND nombre = 'Cafes';

UPDATE productos SET nombre = 'Café negro' WHERE id = 1 AND nombre IN ('Cafe negro');
UPDATE productos SET descripcion = 'Café solo' WHERE id = 1 AND descripcion = 'Cafe solo';
UPDATE productos SET nombre = 'Café con leche' WHERE id = 2 AND nombre IN ('Cafe con leche');
UPDATE productos SET descripcion = 'Café con leche' WHERE id = 2 AND descripcion = 'Cafe con leche';
UPDATE productos SET nombre = 'Té o mate cocido solo' WHERE id = 3 AND nombre IN ('Te/mate cocido solo');
UPDATE productos SET descripcion = 'Infusión sola' WHERE id = 3 AND descripcion = 'Infusion sola';
UPDATE productos SET nombre = 'Té o mate cocido con leche' WHERE id = 4 AND nombre IN ('Te/mate cocido con leche');
UPDATE productos SET descripcion = 'Infusión con leche' WHERE id = 4 AND descripcion = 'Infusion con leche';
UPDATE productos SET nombre = 'Capuchino' WHERE id = 5 AND nombre IN ('Capuccino');
UPDATE productos SET nombre = 'Chocolatada fría o caliente' WHERE id = 7 AND nombre IN ('Chocolatada fria/caliente');
UPDATE productos SET descripcion = 'Chocolatada a elección' WHERE id = 7 AND descripcion = 'Chocolatada a eleccion';
UPDATE productos SET nombre = 'Frapuchino' WHERE id = 8 AND nombre IN ('Frapuccino');
UPDATE productos SET descripcion = 'Café frío batido' WHERE id = 8 AND descripcion = 'Cafe frio batido';
UPDATE productos SET nombre = 'Café con leche + 2 medialunas' WHERE id = 9 AND nombre IN ('Cafe c/leche + 2 medialunas');
UPDATE productos SET nombre = 'Café con leche + chipas' WHERE id = 10 AND nombre IN ('Cafe c/leche + chipas');
UPDATE productos SET nombre = 'Café con leche + tostado de jamón y queso' WHERE id = 11 AND nombre IN ('Cafe c/leche + tostado JYQ');
UPDATE productos SET nombre = 'Café con leche + 2 medialunas + jugo' WHERE id = 12 AND nombre IN ('Cafe c/leche + 2 medialunas + jugo');
UPDATE productos SET nombre = 'Cocido quemado + mbejú' WHERE id = 14 AND nombre IN ('Cocido quemado + mbeju');
UPDATE productos SET nombre = 'Frapuchino + 2 medialunas' WHERE id = 15 AND nombre IN ('Frapuccino + 2 medialunas');
UPDATE productos SET descripcion = 'Promo fría' WHERE id = 15 AND descripcion = 'Promo fria';
UPDATE productos SET nombre = 'Chipitas 100 g' WHERE id = 18 AND nombre IN ('Chipitas 100g');
UPDATE productos SET descripcion = 'Chipitas por 100 g' WHERE id = 18 AND descripcion = 'Chipitas por 100g';
UPDATE productos SET nombre = 'Chipitas 250 g' WHERE id = 19 AND nombre IN ('Chipitas 250g');
UPDATE productos SET descripcion = 'Chipitas por 250 g' WHERE id = 19 AND descripcion = 'Chipitas por 250g';
UPDATE productos SET nombre = 'Mbejú' WHERE id = 20 AND nombre IN ('Mbeju');
UPDATE productos SET descripcion = 'Mbejú' WHERE id = 20 AND descripcion = 'Mbeju';
UPDATE productos SET nombre = 'Medialuna con jamón y queso' WHERE id = 22 AND nombre IN ('Medialuna c/JYQ');
UPDATE productos SET descripcion = 'Medialuna con jamón y queso' WHERE id = 22 AND descripcion = 'Medialuna con jamon y queso';
UPDATE productos SET nombre = 'Sándwich de miga de jamón y queso' WHERE id = 24 AND nombre IN ('Sandwich de miga JYQ');
UPDATE productos SET descripcion = 'Sándwich de miga' WHERE id = 24 AND descripcion = 'Sandwich de miga';
UPDATE productos SET nombre = 'Tostado de jamón y queso' WHERE id = 25 AND nombre IN ('Tostado de JYQ');
UPDATE productos SET descripcion = 'Tostado de jamón y queso' WHERE id = 25 AND descripcion = 'Tostado de jamon y queso';
UPDATE productos SET descripcion = 'Hamburguesa simple con acompañamiento' WHERE id = 28 AND descripcion = 'Hamburguesa simple con acompanamiento';
UPDATE productos SET nombre = 'Porción de papas' WHERE id = 29 AND nombre IN ('Porcion de papas');
UPDATE productos SET nombre = 'Licuado común' WHERE id = 30 AND nombre IN ('Licuado comun local', 'Licuado común (local)');
UPDATE productos SET descripcion = 'Licuado de fruta' WHERE id = 30 AND descripcion = 'Licuado para consumir en local';
UPDATE productos SET nombre = 'Licuado común + tostado' WHERE id = 32 AND nombre IN ('Licuado comun + tostado local', 'Licuado común + tostado (local)');
UPDATE productos SET descripcion = 'Licuado común con tostado' WHERE id = 32 AND descripcion = 'Licuado comun con tostado';
UPDATE productos SET nombre = 'Licuado especial' WHERE id = 34 AND nombre IN ('Licuado especial local', 'Licuado especial (local)');
UPDATE productos SET nombre = 'Licuado especial + tostado' WHERE id = 36 AND nombre IN ('Licuado especial + tostado local', 'Licuado especial + tostado (local)');
UPDATE productos SET nombre = 'Limonada en vaso' WHERE id = 38 AND nombre IN ('Limonada vaso local', 'Limonada en vaso (local)');
UPDATE productos SET nombre = 'Limonada en jarra' WHERE id = 40 AND nombre IN ('Limonada jarra');

UPDATE producto_opciones SET grupo = 'Azúcar' WHERE grupo = 'Azucar';
UPDATE producto_opciones SET grupo = 'Infusión' WHERE grupo = 'Infusion';
UPDATE producto_opciones SET opcion = 'Sin azúcar' WHERE opcion = 'Sin azucar';
UPDATE producto_opciones SET opcion = 'Té' WHERE opcion = 'Te';
UPDATE producto_opciones SET opcion = 'Fría' WHERE opcion = 'Fria';
UPDATE producto_opciones SET opcion = 'Ananá' WHERE opcion = 'Anana';
UPDATE producto_opciones SET opcion = 'Melón' WHERE opcion = 'Melon';
UPDATE producto_opciones SET opcion = 'Banana con leche' WHERE opcion = 'Banana c/leche';
UPDATE producto_opciones SET opcion = 'Durazno con leche' WHERE opcion = 'Durazno c/leche';
UPDATE producto_opciones SET opcion = 'Ananá-naranja' WHERE opcion = 'Anana-naranja';
UPDATE producto_opciones SET opcion = 'Arándanos' WHERE opcion = 'Arandanos';
UPDATE producto_opciones SET opcion = 'Frutilla-ananá' WHERE opcion = 'Frutilla-anana';
UPDATE producto_opciones SET opcion = 'Maracuyá' WHERE opcion = 'Maracuya';


-- 6. Numeracion compartida entre PCs y fecha de modificacion -------------
-- Va al final: las secuencias tienen que arrancar despues de los movimientos
-- que agrego el paso 4.
ALTER TABLE ventas ADD COLUMN IF NOT EXISTS actualizado_en DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP;
ALTER TABLE movimientos_stock ADD COLUMN IF NOT EXISTS actualizado_en DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP;
ALTER TABLE ingresos_stock ADD COLUMN IF NOT EXISTS actualizado_en DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP;

-- Numeracion compartida entre todas las PCs: ventas, pedidos, movimientos de
-- stock e ingresos piden aca su proximo numero (ver SecuenciaRepo.vb). Un
-- contador en cada PC daba el mismo numero en dos cajas abiertas a la vez.
CREATE TABLE IF NOT EXISTS secuencias (
    nombre  VARCHAR(40) NOT NULL,
    valor   BIGINT      NOT NULL DEFAULT 0,
    PRIMARY KEY (nombre)
) ENGINE=InnoDB;

INSERT INTO secuencias (nombre, valor)
SELECT 'ventas', COALESCE(MAX(id), 0) FROM ventas
UNION ALL SELECT 'pedidos', COALESCE(MAX(id), 0) FROM pedidos
UNION ALL SELECT 'movimientos_stock', COALESCE(MAX(id), 0) FROM movimientos_stock
UNION ALL SELECT 'ingresos_stock', COALESCE(MAX(id), 0) FROM ingresos_stock
ON DUPLICATE KEY UPDATE valor = GREATEST(valor, VALUES(valor));
