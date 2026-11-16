-- =====================================================================
--  CAFETERIA - Base de datos para XAMPP (MariaDB / MySQL)
--
--  Como usarlo:
--    1. Encender Apache y MySQL en el panel de XAMPP.
--    2. Abrir http://localhost/phpmyadmin
--    3. Pestaña "Importar" -> elegir este archivo -> "Importar".
--
--  ATENCION: si la base "cafeteria" ya existe, este script BORRA sus
--  tablas y las vuelve a crear con los datos de ejemplo. Usalo para
--  instalar desde cero, no sobre una base con datos reales.
-- =====================================================================

SET NAMES utf8mb4;

CREATE DATABASE IF NOT EXISTS cafeteria
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_general_ci;

USE cafeteria;

SET FOREIGN_KEY_CHECKS = 0;
DROP VIEW  IF EXISTS vista_pedidos_operativos;
DROP VIEW  IF EXISTS vista_stock_bajo;
DROP TABLE IF EXISTS auditoria, movimientos_caja, sesiones_caja, cajas,
                     pagos, detalle_ventas, detalle_pedidos, pedidos, ventas,
                     metodos_pago, detalle_ingresos, ingresos_stock,
                     movimientos_stock, producto_opciones, productos,
                     subcategorias, categorias, proveedores, clientes,
                     empleados, usuarios, roles, configuracion_negocio,
                     recuperaciones_contrasena, configuracion_correo, secuencias;
SET FOREIGN_KEY_CHECKS = 1;


-- ---------------------------------------------------------------------
--  SEGURIDAD: roles, usuarios, empleados, auditoria
-- ---------------------------------------------------------------------

-- Los nombres de los roles tienen que coincidir EXACTAMENTE con el
-- Enum RolUsuario del programa (Administrador, Cajero, Cocina).
CREATE TABLE roles (
    id          BIGINT       NOT NULL AUTO_INCREMENT,
    nombre      VARCHAR(50)  NOT NULL,
    descripcion VARCHAR(255) NULL,
    activo      TINYINT(1)   NOT NULL DEFAULT 1,
    creado_en   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uq_roles_nombre (nombre)
) ENGINE=InnoDB;

-- El login acepta nombre de usuario, correo o telefono.
-- La contraseña se guarda como hash SHA-256, nunca en texto plano.
CREATE TABLE usuarios (
    id              BIGINT       NOT NULL AUTO_INCREMENT,
    nombre_usuario  VARCHAR(80)  NOT NULL,
    email           VARCHAR(150) NULL,
    telefono        VARCHAR(40)  NULL,
    password_hash   VARCHAR(255) NOT NULL,
    rol_id          BIGINT       NOT NULL,
    activo          TINYINT(1)   NOT NULL DEFAULT 1,
    ultimo_acceso   DATETIME     NULL,
    creado_en       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    actualizado_en  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uq_usuarios_nombre (nombre_usuario),
    UNIQUE KEY uq_usuarios_email (email),
    KEY ix_usuarios_telefono (telefono),
    CONSTRAINT fk_usuarios_rol FOREIGN KEY (rol_id) REFERENCES roles (id)
) ENGINE=InnoDB;

CREATE TABLE empleados (
    id              BIGINT       NOT NULL AUTO_INCREMENT,
    usuario_id      BIGINT       NULL,
    nombre          VARCHAR(100) NOT NULL,
    apellido        VARCHAR(100) NULL,
    documento       VARCHAR(40)  NULL,
    telefono        VARCHAR(40)  NULL,
    fecha_ingreso   DATE         NULL,
    activo          TINYINT(1)   NOT NULL DEFAULT 1,
    creado_en       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    actualizado_en  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uq_empleados_usuario (usuario_id),
    CONSTRAINT fk_empleados_usuario FOREIGN KEY (usuario_id) REFERENCES usuarios (id)
        ON DELETE SET NULL
) ENGINE=InnoDB;

-- La llenan los triggers del final. El programa avisa quien esta
-- operando con  SET @usuario_id = ...  al abrir cada conexion.
CREATE TABLE auditoria (
    id                BIGINT       NOT NULL AUTO_INCREMENT,
    usuario_id        BIGINT       NULL,
    entidad           VARCHAR(80)  NOT NULL,
    entidad_id        BIGINT       NOT NULL,
    accion            ENUM('INSERT','UPDATE','DELETE') NOT NULL,
    datos_anteriores  JSON         NULL,
    datos_nuevos      JSON         NULL,
    creado_en         DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    KEY ix_auditoria_entidad (entidad, entidad_id),
    CONSTRAINT fk_auditoria_usuario FOREIGN KEY (usuario_id) REFERENCES usuarios (id)
        ON DELETE SET NULL
) ENGINE=InnoDB;


-- ---------------------------------------------------------------------
--  CONFIGURACION DEL NEGOCIO (una sola fila, id = 1)
-- ---------------------------------------------------------------------
CREATE TABLE configuracion_negocio (
    id                       TINYINT       NOT NULL,
    nombre_negocio           VARCHAR(150)  NOT NULL DEFAULT '',
    cuit                     VARCHAR(20)   NULL,
    direccion                VARCHAR(255)  NULL,
    telefono                 VARCHAR(40)   NULL,
    margen_sobre_costo       DECIMAL(6,2)  NOT NULL DEFAULT 30.00,
    gastos_fijos_porcentaje  DECIMAL(6,2)  NOT NULL DEFAULT 10.00,
    iva_general              DECIMAL(6,2)  NOT NULL DEFAULT 21.00,
    aplicar_gastos_fijos     TINYINT(1)    NOT NULL DEFAULT 1,
    aplicar_iva              TINYINT(1)    NOT NULL DEFAULT 1,
    actualizado_en           DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    CONSTRAINT ck_config_fila_unica CHECK (id = 1)
) ENGINE=InnoDB;


-- ---------------------------------------------------------------------
--  RECUPERACION DE CONTRASEÑA POR CORREO
-- ---------------------------------------------------------------------

-- Cuenta de correo con la que el programa manda los codigos (una sola fila).
-- Con Gmail: servidor smtp.gmail.com, puerto 587, usar_ssl 1, usuario = la
-- direccion de Gmail y contrasena = una "contraseña de aplicacion" de 16
-- letras (Cuenta de Google > Seguridad > Contraseñas de aplicaciones), NO la
-- contraseña normal de la cuenta.
CREATE TABLE configuracion_correo (
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
CREATE TABLE recuperaciones_contrasena (
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


-- Numeracion compartida entre todas las PCs: ventas, pedidos, movimientos de
-- stock e ingresos piden aca su proximo numero (ver SecuenciaRepo.vb). Un
-- contador en cada PC daba el mismo numero en dos cajas abiertas a la vez.
CREATE TABLE secuencias (
    nombre  VARCHAR(40) NOT NULL,
    valor   BIGINT      NOT NULL DEFAULT 0,
    PRIMARY KEY (nombre)
) ENGINE=InnoDB;


-- ---------------------------------------------------------------------
--  PERSONAS: clientes y proveedores
-- ---------------------------------------------------------------------
CREATE TABLE clientes (
    id              BIGINT       NOT NULL AUTO_INCREMENT,
    usuario_id      BIGINT       NULL,
    nombre          VARCHAR(100) NOT NULL,
    apellido        VARCHAR(100) NULL,
    documento       VARCHAR(40)  NULL,
    telefono        VARCHAR(40)  NULL,
    email           VARCHAR(150) NULL,
    activo          TINYINT(1)   NOT NULL DEFAULT 1,
    creado_en       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    actualizado_en  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    KEY ix_clientes_telefono (telefono),
    KEY ix_clientes_nombre (nombre),
    CONSTRAINT fk_clientes_usuario FOREIGN KEY (usuario_id) REFERENCES usuarios (id)
        ON DELETE SET NULL
) ENGINE=InnoDB;

-- Los datos principales van en columnas. Las listas que la ficha del
-- proveedor maneja como texto libre (contactos, telefonos extra,
-- archivos, cuenta corriente e historial) van juntas en "extras" (JSON).
CREATE TABLE proveedores (
    id                         BIGINT        NOT NULL AUTO_INCREMENT,
    codigo                     VARCHAR(20)   NOT NULL,
    razon_social               VARCHAR(150)  NOT NULL,
    cuit                       VARCHAR(20)   NULL,
    tipo_documento             VARCHAR(20)   NULL,
    documento                  VARCHAR(40)   NULL,
    tipo_iva                   VARCHAR(40)   NULL,
    telefono                   VARCHAR(40)   NULL,
    telefono_particular        VARCHAR(40)   NULL,
    celular                    VARCHAR(40)   NULL,
    fax                        VARCHAR(40)   NULL,
    email                      VARCHAR(150)  NULL,
    sitio_web                  VARCHAR(150)  NULL,
    direccion                  VARCHAR(255)  NULL,
    codigo_postal              VARCHAR(15)   NULL,
    localidad                  VARCHAR(100)  NULL,
    provincia                  VARCHAR(100)  NULL,
    pais                       VARCHAR(60)   NULL DEFAULT 'Argentina',
    contacto_principal         VARCHAR(100)  NULL,
    actividad                  VARCHAR(100)  NULL,
    categoria                  VARCHAR(100)  NULL,
    condicion_comercial        VARCHAR(150)  NULL,
    forma_pago                 VARCHAR(100)  NULL,
    banco                      VARCHAR(100)  NULL,
    cbu                        VARCHAR(30)   NULL,
    alias                      VARCHAR(60)   NULL,
    descuento_general          DECIMAL(5,2)  NOT NULL DEFAULT 0,
    descuento_volumen          DECIMAL(5,2)  NOT NULL DEFAULT 0,
    observaciones_rapidas      VARCHAR(255)  NULL,
    observaciones              TEXT          NULL,
    observaciones_comerciales  TEXT          NULL,
    notas_internas             TEXT          NULL,
    total_compras              DECIMAL(14,2) NOT NULL DEFAULT 0,
    cantidad_ordenes           INT           NOT NULL DEFAULT 0,
    saldo_pendiente            DECIMAL(14,2) NOT NULL DEFAULT 0,
    ultima_compra              DATETIME      NULL,
    fecha_ultima_operacion     DATETIME      NULL,
    extras                     JSON          NULL,
    fecha_alta                 DATE          NULL,
    creado_por                 VARCHAR(80)   NULL,
    modificado_por             VARCHAR(80)   NULL,
    activo                     TINYINT(1)    NOT NULL DEFAULT 1,
    creado_en                  DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    actualizado_en             DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uq_proveedores_codigo (codigo)
) ENGINE=InnoDB;


-- ---------------------------------------------------------------------
--  CATALOGO: categorias, subcategorias, productos y sus opciones
-- ---------------------------------------------------------------------
CREATE TABLE categorias (
    id          BIGINT       NOT NULL AUTO_INCREMENT,
    nombre      VARCHAR(100) NOT NULL,
    descripcion VARCHAR(255) NULL,
    activo      TINYINT(1)   NOT NULL DEFAULT 1,
    creado_en   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uq_categorias_nombre (nombre)
) ENGINE=InnoDB;

-- El programa permite subcategorias dentro de cada categoria
-- ("Sin gas", "Entera"). No estaban en el diagrama.
CREATE TABLE subcategorias (
    id            BIGINT       NOT NULL AUTO_INCREMENT,
    categoria_id  BIGINT       NOT NULL,
    nombre        VARCHAR(100) NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_subcategorias (categoria_id, nombre),
    CONSTRAINT fk_subcategorias_categoria FOREIGN KEY (categoria_id) REFERENCES categorias (id)
        ON DELETE CASCADE
) ENGINE=InnoDB;

-- codigo = codigo de barras. Puede quedar NULL; si se carga, no se repite.
-- unidad_medida es texto libre porque el formulario lo deja escribir.
CREATE TABLE productos (
    id                    BIGINT        NOT NULL AUTO_INCREMENT,
    categoria_id          BIGINT        NULL,
    subcategoria_id       BIGINT        NULL,
    proveedor_id          BIGINT        NULL,
    codigo                VARCHAR(60)   NULL,
    nombre                VARCHAR(150)  NOT NULL,
    marca                 VARCHAR(100)  NULL,
    descripcion           VARCHAR(500)  NULL,
    precio_venta          DECIMAL(12,2) NOT NULL DEFAULT 0,
    costo                 DECIMAL(12,2) NOT NULL DEFAULT 0,
    stock_actual          INT           NOT NULL DEFAULT 0,
    stock_minimo          INT           NOT NULL DEFAULT 0,
    unidad_medida         VARCHAR(20)   NOT NULL DEFAULT 'un',
    atajo                 VARCHAR(20)   NULL,
    -- Foto ya reducida por el programa (JPEG de ~400px, unos 30-60 KB).
    imagen                MEDIUMBLOB    NULL,
    requiere_vencimiento  TINYINT(1)    NOT NULL DEFAULT 0,
    fecha_vencimiento     DATE          NULL,
    disponible_venta      TINYINT(1)    NOT NULL DEFAULT 1,
    activo                TINYINT(1)    NOT NULL DEFAULT 1,
    creado_en             DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    actualizado_en        DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uq_productos_codigo (codigo),
    KEY ix_productos_nombre (nombre),
    CONSTRAINT fk_productos_categoria    FOREIGN KEY (categoria_id)    REFERENCES categorias (id)    ON DELETE SET NULL,
    CONSTRAINT fk_productos_subcategoria FOREIGN KEY (subcategoria_id) REFERENCES subcategorias (id) ON DELETE SET NULL,
    CONSTRAINT fk_productos_proveedor    FOREIGN KEY (proveedor_id)    REFERENCES proveedores (id)   ON DELETE SET NULL,
    CONSTRAINT ck_productos_precio CHECK (precio_venta >= 0 AND costo >= 0),
    CONSTRAINT ck_productos_stock  CHECK (stock_actual >= 0 AND stock_minimo >= 0)
) ENGINE=InnoDB;

-- Opciones que se eligen al vender (Leche: Entera / Descremada...).
-- Una fila por opcion; "grupo" es la etiqueta y "orden" respeta el orden en pantalla.
CREATE TABLE producto_opciones (
    id           BIGINT      NOT NULL AUTO_INCREMENT,
    producto_id  BIGINT      NOT NULL,
    grupo        VARCHAR(60) NOT NULL,
    opcion       VARCHAR(80) NOT NULL,
    orden        INT         NOT NULL DEFAULT 0,
    PRIMARY KEY (id),
    KEY ix_opciones_producto (producto_id, orden),
    CONSTRAINT fk_opciones_producto FOREIGN KEY (producto_id) REFERENCES productos (id)
        ON DELETE CASCADE
) ENGINE=InnoDB;


-- ---------------------------------------------------------------------
--  STOCK: ingresos de mercaderia y movimientos
-- ---------------------------------------------------------------------

-- Reemplaza el archivo ingresos_stock.json que usaba el programa.
CREATE TABLE ingresos_stock (
    id             BIGINT       NOT NULL AUTO_INCREMENT,
    proveedor_id   BIGINT       NULL,
    usuario_id     BIGINT       NULL,
    fecha          DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    observaciones  VARCHAR(500) NULL,
    -- Lo pone el servidor; la sincronizacion entre PCs lo usa para ver que cambio.
    actualizado_en   DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    KEY ix_ingresos_fecha (fecha),
    CONSTRAINT fk_ingresos_proveedor FOREIGN KEY (proveedor_id) REFERENCES proveedores (id) ON DELETE SET NULL,
    CONSTRAINT fk_ingresos_usuario   FOREIGN KEY (usuario_id)   REFERENCES usuarios (id)    ON DELETE SET NULL
) ENGINE=InnoDB;

CREATE TABLE detalle_ingresos (
    id           BIGINT        NOT NULL AUTO_INCREMENT,
    ingreso_id   BIGINT        NOT NULL,
    producto_id  BIGINT        NOT NULL,
    cantidad     INT           NOT NULL,
    observacion  VARCHAR(255)  NULL,
    PRIMARY KEY (id),
    CONSTRAINT fk_detalle_ingresos_ingreso  FOREIGN KEY (ingreso_id)  REFERENCES ingresos_stock (id) ON DELETE CASCADE,
    CONSTRAINT fk_detalle_ingresos_producto FOREIGN KEY (producto_id) REFERENCES productos (id),
    CONSTRAINT ck_detalle_ingresos_cantidad CHECK (cantidad > 0)
) ENGINE=InnoDB;

-- Cada cambio de stock deja una fila. cantidad lleva signo:
-- positiva si entro mercaderia, negativa si salio.
-- referencia_tipo + referencia_id dicen que operacion lo causo
-- (POS + id de venta, IngresoStock + id de ingreso, etc.).
CREATE TABLE movimientos_stock (
    id               BIGINT        NOT NULL AUTO_INCREMENT,
    producto_id      BIGINT        NOT NULL,
    usuario_id       BIGINT        NULL,
    tipo             ENUM('Ingreso','Reposicion','Ajuste','Venta','Cancelacion','Devolucion') NOT NULL,
    cantidad         INT           NOT NULL,
    stock_anterior   INT           NOT NULL,
    stock_posterior  INT           NOT NULL,
    referencia_tipo  VARCHAR(50)   NULL,
    referencia_id    BIGINT        NULL,
    motivo           VARCHAR(255)  NULL,
    creado_en        DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    -- Lo pone el servidor; la sincronizacion entre PCs lo usa para ver que cambio.
    actualizado_en   DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    KEY ix_movstock_producto (producto_id, creado_en),
    KEY ix_movstock_referencia (referencia_tipo, referencia_id),
    CONSTRAINT fk_movstock_producto FOREIGN KEY (producto_id) REFERENCES productos (id),
    CONSTRAINT fk_movstock_usuario  FOREIGN KEY (usuario_id)  REFERENCES usuarios (id) ON DELETE SET NULL
) ENGINE=InnoDB;


-- ---------------------------------------------------------------------
--  VENTAS, PAGOS Y PEDIDOS
-- ---------------------------------------------------------------------

-- Los nombres coinciden con el Enum MetodoPago del programa.
CREATE TABLE metodos_pago (
    id                   BIGINT      NOT NULL AUTO_INCREMENT,
    nombre               VARCHAR(50) NOT NULL,
    requiere_referencia  TINYINT(1)  NOT NULL DEFAULT 0,
    activo               TINYINT(1)  NOT NULL DEFAULT 1,
    PRIMARY KEY (id),
    UNIQUE KEY uq_metodos_pago_nombre (nombre)
) ENGINE=InnoDB;

-- La venta es el hecho economico: se confirma y despues solo se puede anular.
-- cliente_nombre / cliente_telefono y el detalle guardan una "foto" de como
-- era todo al momento de vender, para que cambiar un precio no altere el pasado.
CREATE TABLE ventas (
    id                 BIGINT        NOT NULL AUTO_INCREMENT,
    numero             VARCHAR(20)   NOT NULL,
    cliente_id         BIGINT        NULL,
    empleado_id        BIGINT        NULL,
    cliente_nombre     VARCHAR(150)  NULL,
    cliente_telefono   VARCHAR(40)   NULL,
    tipo_servicio      ENUM('EnElLocal','ParaLlevar') NOT NULL DEFAULT 'EnElLocal',
    mesa               VARCHAR(20)   NULL,
    metodo_pago_id     BIGINT        NOT NULL,
    estado             ENUM('Confirmada','Anulada') NOT NULL DEFAULT 'Confirmada',
    estado_pago        ENUM('Pagada','Pendiente')   NOT NULL DEFAULT 'Pagada',
    fue_a_cuenta       TINYINT(1)    NOT NULL DEFAULT 0,
    subtotal           DECIMAL(12,2) NOT NULL DEFAULT 0,
    descuento          DECIMAL(12,2) NOT NULL DEFAULT 0,
    impuesto           DECIMAL(12,2) NOT NULL DEFAULT 0,
    total              DECIMAL(12,2) NOT NULL DEFAULT 0,
    monto_pagado       DECIMAL(12,2) NOT NULL DEFAULT 0,
    efectivo_recibido  DECIMAL(12,2) NOT NULL DEFAULT 0,
    stock_descontado   TINYINT(1)    NOT NULL DEFAULT 0,
    fecha_venta        DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    anulada_en         DATETIME      NULL,
    anulada_por        BIGINT        NULL,
    motivo_anulacion   VARCHAR(255)  NULL,
    -- Lo pone el servidor; la sincronizacion entre PCs lo usa para ver que cambio.
    actualizado_en   DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uq_ventas_numero (numero),
    KEY ix_ventas_fecha (fecha_venta),
    KEY ix_ventas_estado_pago (estado_pago),
    CONSTRAINT fk_ventas_cliente     FOREIGN KEY (cliente_id)     REFERENCES clientes (id)     ON DELETE SET NULL,
    CONSTRAINT fk_ventas_empleado    FOREIGN KEY (empleado_id)    REFERENCES empleados (id)    ON DELETE SET NULL,
    CONSTRAINT fk_ventas_metodo_pago FOREIGN KEY (metodo_pago_id) REFERENCES metodos_pago (id),
    CONSTRAINT fk_ventas_anulada_por FOREIGN KEY (anulada_por)    REFERENCES empleados (id)    ON DELETE SET NULL,
    CONSTRAINT ck_ventas_montos CHECK (total >= 0 AND monto_pagado >= 0 AND descuento >= 0)
) ENGINE=InnoDB;

CREATE TABLE detalle_ventas (
    id                BIGINT        NOT NULL AUTO_INCREMENT,
    venta_id          BIGINT        NOT NULL,
    producto_id       BIGINT        NOT NULL,
    producto_nombre   VARCHAR(150)  NOT NULL,
    cantidad          INT           NOT NULL,
    precio_unitario   DECIMAL(12,2) NOT NULL,
    subtotal          DECIMAL(12,2) NOT NULL,
    especificaciones  VARCHAR(255)  NULL,
    PRIMARY KEY (id),
    CONSTRAINT fk_detalle_ventas_venta    FOREIGN KEY (venta_id)    REFERENCES ventas (id) ON DELETE CASCADE,
    CONSTRAINT fk_detalle_ventas_producto FOREIGN KEY (producto_id) REFERENCES productos (id),
    CONSTRAINT ck_detalle_ventas_cantidad CHECK (cantidad > 0)
) ENGINE=InnoDB;

-- Cada ingreso de plata contra una venta. Una venta pagada en el momento
-- tiene un pago; una venta "a cuenta" puede tener varios (pagos parciales).
CREATE TABLE pagos (
    id                 BIGINT        NOT NULL AUTO_INCREMENT,
    venta_id           BIGINT        NOT NULL,
    metodo_pago_id     BIGINT        NOT NULL,
    empleado_id        BIGINT        NULL,
    monto              DECIMAL(12,2) NOT NULL,
    efectivo_recibido  DECIMAL(12,2) NULL,
    referencia         VARCHAR(120)  NULL,
    pagado_en          DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    KEY ix_pagos_venta (venta_id),
    CONSTRAINT fk_pagos_venta       FOREIGN KEY (venta_id)       REFERENCES ventas (id) ON DELETE CASCADE,
    CONSTRAINT fk_pagos_metodo_pago FOREIGN KEY (metodo_pago_id) REFERENCES metodos_pago (id),
    CONSTRAINT fk_pagos_empleado    FOREIGN KEY (empleado_id)    REFERENCES empleados (id) ON DELETE SET NULL,
    CONSTRAINT ck_pagos_monto CHECK (monto > 0)
) ENGINE=InnoDB;

-- En este sistema el pedido de cocina nace DESPUES de la venta
-- (PedidoService.CrearDesdeVenta), por eso la clave foranea va
-- de pedidos hacia ventas (venta_id) y no al reves.
CREATE TABLE pedidos (
    id                  BIGINT        NOT NULL AUTO_INCREMENT,
    venta_id            BIGINT        NULL,
    cliente_id          BIGINT        NULL,
    empleado_id         BIGINT        NULL,
    cliente_nombre      VARCHAR(150)  NULL,
    cliente_telefono    VARCHAR(40)   NULL,
    estado              ENUM('EnPreparacion','Listo','Entregado','Cancelado') NOT NULL DEFAULT 'EnPreparacion',
    tipo                ENUM('EnElLocal','ParaLlevar') NOT NULL DEFAULT 'EnElLocal',
    mesa                VARCHAR(20)   NULL,
    metodo_pago_id      BIGINT        NULL,
    observaciones       VARCHAR(500)  NULL,
    subtotal            DECIMAL(12,2) NOT NULL DEFAULT 0,
    descuento           DECIMAL(12,2) NOT NULL DEFAULT 0,
    total               DECIMAL(12,2) NOT NULL DEFAULT 0,
    motivo_cancelacion  VARCHAR(255)  NULL,
    creado_en           DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    confirmado_en       DATETIME      NULL,
    cancelado_en        DATETIME      NULL,
    actualizado_en      DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    KEY ix_pedidos_estado (estado),
    KEY ix_pedidos_venta (venta_id),
    CONSTRAINT fk_pedidos_venta       FOREIGN KEY (venta_id)       REFERENCES ventas (id)       ON DELETE SET NULL,
    CONSTRAINT fk_pedidos_cliente     FOREIGN KEY (cliente_id)     REFERENCES clientes (id)     ON DELETE SET NULL,
    CONSTRAINT fk_pedidos_empleado    FOREIGN KEY (empleado_id)    REFERENCES empleados (id)    ON DELETE SET NULL,
    CONSTRAINT fk_pedidos_metodo_pago FOREIGN KEY (metodo_pago_id) REFERENCES metodos_pago (id)
) ENGINE=InnoDB;

CREATE TABLE detalle_pedidos (
    id               BIGINT        NOT NULL AUTO_INCREMENT,
    pedido_id        BIGINT        NOT NULL,
    producto_id      BIGINT        NOT NULL,
    cantidad         INT           NOT NULL,
    precio_unitario  DECIMAL(12,2) NOT NULL DEFAULT 0,
    subtotal         DECIMAL(12,2) NOT NULL DEFAULT 0,
    observaciones    VARCHAR(255)  NULL,
    creado_en        DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    CONSTRAINT fk_detalle_pedidos_pedido   FOREIGN KEY (pedido_id)   REFERENCES pedidos (id) ON DELETE CASCADE,
    CONSTRAINT fk_detalle_pedidos_producto FOREIGN KEY (producto_id) REFERENCES productos (id)
) ENGINE=InnoDB;


-- ---------------------------------------------------------------------
--  CAJA: cajas fisicas, turnos (sesiones) y movimientos de dinero
-- ---------------------------------------------------------------------
CREATE TABLE cajas (
    id         BIGINT       NOT NULL AUTO_INCREMENT,
    nombre     VARCHAR(80)  NOT NULL,
    ubicacion  VARCHAR(120) NULL,
    activo     TINYINT(1)   NOT NULL DEFAULT 1,
    creado_en  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id)
) ENGINE=InnoDB;

CREATE TABLE sesiones_caja (
    id              BIGINT        NOT NULL AUTO_INCREMENT,
    caja_id         BIGINT        NOT NULL,
    empleado_id     BIGINT        NULL,
    estado          ENUM('Abierta','Cerrada') NOT NULL DEFAULT 'Abierta',
    monto_inicial   DECIMAL(12,2) NOT NULL DEFAULT 0,
    monto_esperado  DECIMAL(12,2) NULL,
    monto_real      DECIMAL(12,2) NULL,
    diferencia      DECIMAL(12,2) NULL,
    abierta_en      DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    cerrada_en      DATETIME      NULL,
    observaciones   VARCHAR(500)  NULL,
    PRIMARY KEY (id),
    KEY ix_sesiones_estado (caja_id, estado),
    CONSTRAINT fk_sesiones_caja     FOREIGN KEY (caja_id)     REFERENCES cajas (id),
    CONSTRAINT fk_sesiones_empleado FOREIGN KEY (empleado_id) REFERENCES empleados (id) ON DELETE SET NULL
) ENGINE=InnoDB;

CREATE TABLE movimientos_caja (
    id              BIGINT        NOT NULL AUTO_INCREMENT,
    sesion_caja_id  BIGINT        NOT NULL,
    venta_id        BIGINT        NULL,
    empleado_id     BIGINT        NULL,
    tipo            ENUM('Apertura','Venta','Egreso','Cierre','CobroDeuda') NOT NULL,
    monto           DECIMAL(12,2) NOT NULL,
    descripcion     VARCHAR(255)  NULL,
    creado_en       DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    KEY ix_movcaja_sesion (sesion_caja_id),
    KEY ix_movcaja_venta (venta_id),
    CONSTRAINT fk_movcaja_sesion   FOREIGN KEY (sesion_caja_id) REFERENCES sesiones_caja (id) ON DELETE CASCADE,
    CONSTRAINT fk_movcaja_venta    FOREIGN KEY (venta_id)       REFERENCES ventas (id)        ON DELETE SET NULL,
    CONSTRAINT fk_movcaja_empleado FOREIGN KEY (empleado_id)    REFERENCES empleados (id)     ON DELETE SET NULL
) ENGINE=InnoDB;


-- ---------------------------------------------------------------------
--  VISTAS
-- ---------------------------------------------------------------------

-- Pedidos que la cocina todavia tiene que atender.
CREATE VIEW vista_pedidos_operativos AS
SELECT  p.id                                        AS pedido_id,
        p.venta_id,
        v.numero                                    AS venta_numero,
        p.estado,
        p.tipo,
        p.mesa,
        COALESCE(c.nombre, p.cliente_nombre)        AS cliente,
        COUNT(d.id)                                 AS lineas,
        COALESCE(SUM(d.cantidad), 0)                AS unidades,
        p.total,
        p.creado_en,
        TIMESTAMPDIFF(MINUTE, p.creado_en, NOW())   AS minutos_espera
FROM pedidos p
LEFT JOIN ventas          v ON v.id = p.venta_id
LEFT JOIN clientes        c ON c.id = p.cliente_id
LEFT JOIN detalle_pedidos d ON d.pedido_id = p.id
WHERE p.estado IN ('EnPreparacion', 'Listo')
GROUP BY p.id, p.venta_id, v.numero, p.estado, p.tipo, p.mesa,
         c.nombre, p.cliente_nombre, p.total, p.creado_en;

-- Productos en o por debajo de su minimo. Si el minimo es 0 se usa 10,
-- igual que StockService.UmbralCritico en el programa.
CREATE VIEW vista_stock_bajo AS
SELECT  p.id                                                   AS producto_id,
        p.nombre,
        cat.nombre                                             AS categoria,
        p.stock_actual,
        IF(p.stock_minimo > 0, p.stock_minimo, 10)             AS stock_minimo,
        IF(p.stock_minimo > 0, p.stock_minimo, 10) - p.stock_actual AS faltante,
        p.unidad_medida
FROM productos p
LEFT JOIN categorias cat ON cat.id = p.categoria_id
WHERE p.activo = 1
  AND p.stock_actual <= IF(p.stock_minimo > 0, p.stock_minimo, 10);


-- ---------------------------------------------------------------------
--  TRIGGERS DE AUDITORIA
-- ---------------------------------------------------------------------
DELIMITER $$

CREATE TRIGGER trg_productos_auditoria
AFTER UPDATE ON productos
FOR EACH ROW
BEGIN
    IF OLD.precio_venta <> NEW.precio_venta OR OLD.costo <> NEW.costo THEN
        INSERT INTO auditoria (usuario_id, entidad, entidad_id, accion,
                               datos_anteriores, datos_nuevos)
        VALUES (@usuario_id, 'productos', NEW.id, 'UPDATE',
                JSON_OBJECT('precio_venta', OLD.precio_venta, 'costo', OLD.costo),
                JSON_OBJECT('precio_venta', NEW.precio_venta, 'costo', NEW.costo));
    END IF;
END$$

CREATE TRIGGER trg_ventas_auditoria
AFTER UPDATE ON ventas
FOR EACH ROW
BEGIN
    IF OLD.estado <> NEW.estado THEN
        INSERT INTO auditoria (usuario_id, entidad, entidad_id, accion,
                               datos_anteriores, datos_nuevos)
        VALUES (@usuario_id, 'ventas', NEW.id, 'UPDATE',
                JSON_OBJECT('estado', OLD.estado, 'total', OLD.total),
                JSON_OBJECT('estado', NEW.estado, 'motivo', NEW.motivo_anulacion));
    END IF;
END$$

DELIMITER ;
-- ---------------------------------------------------------------------
--  DATOS INICIALES (el programa ya no trae productos ni usuarios en el codigo)
-- ---------------------------------------------------------------------

INSERT INTO roles (id, nombre, descripcion) VALUES
  (1, 'Administrador', 'Acceso total, configuracion y precios'),
  (2, 'Cajero',        'Ventas, caja, clientes y proveedores'),
  (3, 'Cocina',        'Tablero de pedidos');

-- Contraseñas de prueba: 123456 / caja123 / cocina123
INSERT INTO usuarios (id, nombre_usuario, email, telefono, password_hash, rol_id) VALUES
  (1, 'Administrador', 'admin@cafe.com',  '12345678', SHA2('123456', 256),    1),
  (2, 'Cajera',        'cajera@cafe.com', '87654321', SHA2('caja123', 256),   2),
  (3, 'Cocinera',      'cocina@cafe.com', '11111111', SHA2('cocina123', 256), 3);

INSERT INTO empleados (id, usuario_id, nombre, telefono, fecha_ingreso) VALUES
  (1, 1, 'Administrador', '12345678', CURDATE()),
  (2, 2, 'Cajera',        '87654321', CURDATE()),
  (3, 3, 'Cocinera',      '11111111', CURDATE());

INSERT INTO metodos_pago (id, nombre, requiere_referencia) VALUES
  (1, 'Efectivo',      0),
  (2, 'Debito',        1),
  (3, 'Credito',       1),
  (4, 'Transferencia', 1),
  (5, 'MercadoPago',   1);

INSERT INTO cajas (id, nombre, ubicacion) VALUES (1, 'Caja principal', 'Mostrador');

INSERT INTO configuracion_negocio (id, nombre_negocio) VALUES (1, '');

-- Completar usuario y contrasena para poder recuperar contraseñas por correo.
INSERT INTO configuracion_correo (id) VALUES (1);

INSERT INTO categorias (id, nombre) VALUES
  (1, 'Cafés'),
  (2, 'Promos'),
  (3, 'Panificados'),
  (4, 'Hamburguesas'),
  (5, 'Licuados'),
  (6, 'Limonadas');

INSERT INTO productos (id, categoria_id, nombre, descripcion, precio_venta, stock_actual, atajo) VALUES
  (1, 1, 'Café negro', 'Café solo', 3000, 45, 'Ctrl+1'),
  (2, 1, 'Café con leche', 'Café con leche', 3500, 40, 'Ctrl+2'),
  (3, 1, 'Té o mate cocido solo', 'Infusión sola', 3000, 60, 'Ctrl+3'),
  (4, 1, 'Té o mate cocido con leche', 'Infusión con leche', 3500, 55, 'Ctrl+4'),
  (5, 1, 'Capuchino', 'Con espuma de leche', 4000, 35, 'Ctrl+5'),
  (6, 1, 'Submarino', 'Leche caliente con chocolate', 4500, 25, 'Ctrl+6'),
  (7, 1, 'Chocolatada fría o caliente', 'Chocolatada a elección', 4000, 30, 'Ctrl+7'),
  (8, 1, 'Frapuchino', 'Café frío batido', 4000, 28, 'Ctrl+8'),
  (9, 2, 'Café con leche + 2 medialunas', 'Promo desayuno', 5000, 25, NULL),
  (10, 2, 'Café con leche + chipas', 'Promo con chipas', 5000, 25, NULL),
  (11, 2, 'Café con leche + tostado de jamón y queso', 'Promo con tostado', 6000, 20, NULL),
  (12, 2, 'Café con leche + 2 medialunas + jugo', 'Promo completa', 6000, 20, NULL),
  (13, 2, 'Exprimido chico', 'Precio pendiente de cargar', 0, 20, NULL),
  (14, 2, 'Cocido quemado + mbejú', 'Promo regional', 6000, 20, NULL),
  (15, 2, 'Frapuchino + 2 medialunas', 'Promo fría', 5500, 20, NULL),
  (16, 3, 'Chipa', 'Chipa por unidad', 100, 100, 'Ctrl+9'),
  (17, 3, 'Chipa 4 unidades', 'Combo 4 chipas', 3500, 50, NULL),
  (18, 3, 'Chipitas 100 g', 'Chipitas por 100 g', 2000, 40, NULL),
  (19, 3, 'Chipitas 250 g', 'Chipitas por 250 g', 4500, 30, NULL),
  (20, 3, 'Mbejú', 'Mbejú', 3500, 20, 'Ctrl+0'),
  (21, 3, 'Medialuna', 'Medialuna', 1000, 40, 'Ctrl+Q'),
  (22, 3, 'Medialuna con jamón y queso', 'Medialuna con jamón y queso', 1500, 30, NULL),
  (23, 3, 'Alfajor de maicena', 'Alfajor de maicena', 1000, 30, NULL),
  (24, 3, 'Sándwich de miga de jamón y queso', 'Sándwich de miga', 3000, 25, NULL),
  (25, 3, 'Tostado de jamón y queso', 'Tostado de jamón y queso', 3500, 20, 'Ctrl+W'),
  (26, 4, 'Hamburguesa simple', 'Hamburguesa simple', 4000, 20, NULL),
  (27, 4, 'Hamburguesa doble', 'Hamburguesa doble', 5000, 20, NULL),
  (28, 4, 'Combo hamburguesa simple', 'Hamburguesa simple con acompañamiento', 9000, 15, NULL),
  (29, 4, 'Porción de papas', 'Papas fritas', 3500, 25, NULL),
  (30, 5, 'Licuado común', 'Licuado de fruta', 3500, 60, NULL),
  (32, 5, 'Licuado común + tostado', 'Licuado común con tostado', 6500, 40, NULL),
  (34, 5, 'Licuado especial', 'Licuado especial', 4000, 50, NULL),
  (36, 5, 'Licuado especial + tostado', 'Licuado especial con tostado', 7000, 40, NULL),
  (38, 6, 'Limonada en vaso', 'Limón, menta y jengibre', 2000, 60, NULL),
  (40, 6, 'Limonada en jarra', 'Jarra de limonada', 600, 15, NULL);

INSERT INTO producto_opciones (producto_id, grupo, opcion, orden) VALUES
  (2, 'Leche', 'Entera', 1),
  (2, 'Leche', 'Descremada', 2),
  (2, 'Leche', 'Vegetal', 3),
  (2, 'Azúcar', 'Sin azúcar', 4),
  (2, 'Azúcar', 'Normal', 5),
  (2, 'Azúcar', 'Edulcorante', 6),
  (3, 'Infusión', 'Té', 1),
  (3, 'Infusión', 'Mate cocido', 2),
  (7, 'Temperatura', 'Fría', 1),
  (7, 'Temperatura', 'Caliente', 2),
  (30, 'Fruta', 'Frutilla', 1),
  (30, 'Fruta', 'Banana', 2),
  (30, 'Fruta', 'Durazno', 3),
  (30, 'Fruta', 'Ananá', 4),
  (30, 'Fruta', 'Melón', 5),
  (30, 'Fruta', 'Mango', 6),
  (30, 'Fruta', 'Guayaba', 7),
  (30, 'Fruta', 'Naranja', 8),
  (34, 'Sabor', 'Super frutilla', 1),
  (34, 'Sabor', 'Banana con leche', 2),
  (34, 'Sabor', 'Durazno con leche', 3),
  (34, 'Sabor', 'Naranja-mango', 4),
  (34, 'Sabor', 'Mango-durazno', 5),
  (34, 'Sabor', 'Ananá-naranja', 6),
  (34, 'Sabor', 'Frutos rojos', 7),
  (34, 'Sabor', 'Frutos del bosque', 8),
  (34, 'Sabor', 'Arándanos', 9),
  (34, 'Sabor', 'Kiwi', 10),
  (34, 'Sabor', 'Frutilla-ananá', 11),
  (34, 'Sabor', 'Frambuesa', 12),
  (34, 'Sabor', 'Mora', 13),
  (34, 'Sabor', 'Frambuesa-frutilla', 14),
  (34, 'Sabor', 'Maracuyá', 15),
  (38, 'Temperatura', 'Fría', 1),
  (38, 'Temperatura', 'Caliente', 2);

INSERT INTO proveedores
  (id, codigo, razon_social, cuit, tipo_iva, telefono, celular, email, direccion, codigo_postal,
   localidad, provincia, pais, contacto_principal, actividad, categoria, forma_pago,
   total_compras, cantidad_ordenes, saldo_pendiente, ultima_compra, fecha_ultima_operacion,
   extras, fecha_alta, creado_por, modificado_por)
VALUES
  (1, 'PRV001', 'Distribuidora Argentina', '30-12345678-9', 'Resp. Inscripto', '4444-3333', '3764-111111',
   'ventas@distribuidora.com', 'Cordoba 234', '1824', 'Lanus Oeste', 'Buenos Aires', 'Argentina',
   'Perez', 'Distribuidora', 'Mayorista', 'Transferencia',
   250000, 8, 15000, CURDATE() - INTERVAL 4 DAY, CURDATE() - INTERVAL 4 DAY,
   CONCAT('{"Contactos":[{"Nombre":"Perez","Cargo":"Ventas","Email":"ventas@distribuidora.com","Telefono":"4444-3333"}],',
          '"Telefonos":[{"Tipo":"Laboral","Numero":"4444-3333"}],',
          '"Archivos":[],',
          '"Movimientos":[{"Fecha":"', DATE_FORMAT(NOW(), '%Y-%m-%dT%H:%i:%s'),
          '","Tipo":"Factura","Descripcion":"Compra de cafe e insumos","Monto":15000,"Vencimiento":"',
          DATE_FORMAT(CURDATE() - INTERVAL 2 DAY, '%Y-%m-%dT00:00:00'), '","Pagado":false}],',
          '"HistorialCambios":[]}'),
   CURDATE(), 'Sistema', 'Sistema'),
  (2, 'PRV002', 'Panificados Mitre', '27-87654321-5', NULL, '3764-222222', '3764-333333',
   'pedidos@panificadosmitre.com', 'Mitre 890', NULL, 'Posadas', 'Misiones', 'Argentina',
   'Laura Benitez', 'Panaderia', 'Panificados', 'Efectivo',
   120000, 14, 0, CURDATE() - INTERVAL 1 DAY, NULL,
   '{"Contactos":[],"Telefonos":[],"Archivos":[],"Movimientos":[],"HistorialCambios":[]}',
   CURDATE(), 'Sistema', 'Sistema'),
  (3, 'PRV003', 'Bebidas del Norte', '20-11223344-6', NULL, '3764-555555', NULL,
   'admin@bebidasnorte.com', 'Ruta 12 km 8', NULL, 'Garupa', 'Misiones', 'Argentina',
   'Carlos Diaz', 'Bebidas', 'Bebidas', 'Cuenta corriente',
   98000, 5, 8500, CURDATE() - INTERVAL 12 DAY, NULL,
   '{"Contactos":[],"Telefonos":[],"Archivos":[],"Movimientos":[],"HistorialCambios":[]}',
   CURDATE(), 'Sistema', 'Sistema');

-- Las secuencias arrancan desde el mayor numero ya usado (con la base nueva, 0).
INSERT INTO secuencias (nombre, valor)
SELECT 'ventas', COALESCE(MAX(id), 0) FROM ventas
UNION ALL SELECT 'pedidos', COALESCE(MAX(id), 0) FROM pedidos
UNION ALL SELECT 'movimientos_stock', COALESCE(MAX(id), 0) FROM movimientos_stock
UNION ALL SELECT 'ingresos_stock', COALESCE(MAX(id), 0) FROM ingresos_stock
ON DUPLICATE KEY UPDATE valor = GREATEST(valor, VALUES(valor));
