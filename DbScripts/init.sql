-- ==========================================================
-- init.sql - Base de datos erp_farmaceutico
-- Se ejecuta automáticamente al levantar el contenedor MySQL
-- (docker-entrypoint-initdb.d) la primera vez que se crea el volumen.
-- ==========================================================

CREATE DATABASE IF NOT EXISTS erp_farmaceutico
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

USE erp_farmaceutico;

-- =========================
-- SEGURIDAD Y USUARIOS
-- =========================

CREATE TABLE roles (
    id_rol INT AUTO_INCREMENT PRIMARY KEY,
    nombre_rol VARCHAR(100) NOT NULL,
    descripcion TEXT
) ENGINE=InnoDB;

CREATE TABLE permisos (
    id_permiso INT AUTO_INCREMENT PRIMARY KEY,
    codigo_permiso VARCHAR(100) NOT NULL UNIQUE,
    modulo VARCHAR(100) NOT NULL,
    descripcion VARCHAR(255)
) ENGINE=InnoDB;

CREATE TABLE usuarios (
    id_usuario INT AUTO_INCREMENT PRIMARY KEY,
    rut_usuario VARCHAR(20) NOT NULL UNIQUE,
    nombre VARCHAR(150) NOT NULL,
    email VARCHAR(150) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    id_rol INT NOT NULL,
    activo BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_usuarios_rol
        FOREIGN KEY (id_rol) REFERENCES roles(id_rol)
) ENGINE=InnoDB;

CREATE TABLE rol_permisos (
    id_rol INT NOT NULL,
    id_permiso INT NOT NULL,
    PRIMARY KEY (id_rol, id_permiso),
    CONSTRAINT fk_rol_permisos_rol
        FOREIGN KEY (id_rol) REFERENCES roles(id_rol),
    CONSTRAINT fk_rol_permisos_permiso
        FOREIGN KEY (id_permiso) REFERENCES permisos(id_permiso)
) ENGINE=InnoDB;

-- =========================
-- CLIENTES Y CONVENIOS
-- =========================

CREATE TABLE clientes (
    id_cliente INT AUTO_INCREMENT PRIMARY KEY,
    rut_cliente VARCHAR(20) NOT NULL UNIQUE,
    razon_social_nombre VARCHAR(200) NOT NULL,
    email VARCHAR(150),
    telefono VARCHAR(30),
    tipo_cliente ENUM('PERSONA', 'EMPRESA', 'INSTITUCION') NOT NULL
) ENGINE=InnoDB;

CREATE TABLE convenios_salud (
    id_convenio INT AUTO_INCREMENT PRIMARY KEY,
    id_cliente INT NOT NULL,
    nombre_convenio VARCHAR(150) NOT NULL,
    porcentaje_descuento DECIMAL(5,2) NOT NULL DEFAULT 0,
    limite_credito DECIMAL(15,2) NOT NULL DEFAULT 0,
    dias_credito_morosidad INT NOT NULL DEFAULT 0,
    activo BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT fk_convenios_cliente
        FOREIGN KEY (id_cliente) REFERENCES clientes(id_cliente)
) ENGINE=InnoDB;

-- =========================
-- PRODUCTOS E INVENTARIO
-- =========================

CREATE TABLE categorias_producto (
    id_categoria INT AUTO_INCREMENT PRIMARY KEY,
    nombre_categoria VARCHAR(150) NOT NULL
) ENGINE=InnoDB;

CREATE TABLE productos (
    id_producto INT AUTO_INCREMENT PRIMARY KEY,
    sku VARCHAR(100) NOT NULL UNIQUE,
    codigo_barras VARCHAR(100) UNIQUE,
    nombre_comercial VARCHAR(200) NOT NULL,
    principio_activo VARCHAR(200),
    registro_sanitario VARCHAR(100),
    formato_presentacion VARCHAR(150),
    id_categoria INT NOT NULL,
    precio_venta_base DECIMAL(15,2) NOT NULL DEFAULT 0,
    costo_promedio_ponderado DECIMAL(15,2) NOT NULL DEFAULT 0,
    stock_minimo INT NOT NULL DEFAULT 0,
    punto_reorden INT NOT NULL DEFAULT 0,
    activo BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT fk_productos_categoria
        FOREIGN KEY (id_categoria) REFERENCES categorias_producto(id_categoria)
) ENGINE=InnoDB;

CREATE TABLE lotes (
    id_lote INT AUTO_INCREMENT PRIMARY KEY,
    id_producto INT NOT NULL,
    numero_lote VARCHAR(100) NOT NULL,
    fecha_vencimiento DATE NOT NULL,
    stock_actual INT NOT NULL DEFAULT 0,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_lotes_producto
        FOREIGN KEY (id_producto) REFERENCES productos(id_producto),
    UNIQUE KEY uk_lote_producto (id_producto, numero_lote)
) ENGINE=InnoDB;

-- =========================
-- CONTABILIDAD
-- =========================

CREATE TABLE plan_cuentas (
    id_cuenta INT AUTO_INCREMENT PRIMARY KEY,
    codigo_cuenta VARCHAR(50) NOT NULL UNIQUE,
    nombre_cuenta VARCHAR(150) NOT NULL,
    tipo_cuenta ENUM('ACTIVO', 'PASIVO', 'PATRIMONIO', 'INGRESO', 'GASTO') NOT NULL,
    nivel INT NOT NULL,
    cuenta_padre_id INT NULL,
    CONSTRAINT fk_plan_cuentas_padre
        FOREIGN KEY (cuenta_padre_id) REFERENCES plan_cuentas(id_cuenta)
) ENGINE=InnoDB;

CREATE TABLE libro_diario_asientos (
    id_asiento INT AUTO_INCREMENT PRIMARY KEY,
    numero_asiento INT NOT NULL,
    fecha_asiento DATE NOT NULL,
    glosa_descripcion TEXT,
    origen ENUM('VENTA', 'COMPRA', 'RECEPCION', 'AJUSTE', 'OTRO') NOT NULL,
    id_usuario INT NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_asientos_usuario
        FOREIGN KEY (id_usuario) REFERENCES usuarios(id_usuario),
    UNIQUE KEY uk_numero_asiento (numero_asiento)
) ENGINE=InnoDB;

CREATE TABLE detalle_asiento_contable (
    id_detalle_asiento INT AUTO_INCREMENT PRIMARY KEY,
    id_asiento INT NOT NULL,
    id_cuenta INT NOT NULL,
    debe DECIMAL(15,2) NOT NULL DEFAULT 0,
    haber DECIMAL(15,2) NOT NULL DEFAULT 0,
    CONSTRAINT fk_detalle_asiento_asiento
        FOREIGN KEY (id_asiento) REFERENCES libro_diario_asientos(id_asiento),
    CONSTRAINT fk_detalle_asiento_cuenta
        FOREIGN KEY (id_cuenta) REFERENCES plan_cuentas(id_cuenta),
    CHECK (debe >= 0),
    CHECK (haber >= 0)
) ENGINE=InnoDB;

-- =========================
-- COMPRAS
-- =========================

CREATE TABLE proveedores (
    id_proveedor INT AUTO_INCREMENT PRIMARY KEY,
    rut_proveedor VARCHAR(20) NOT NULL UNIQUE,
    razon_social VARCHAR(200) NOT NULL,
    nombre_contacto VARCHAR(150),
    telefono VARCHAR(30),
    email VARCHAR(150),
    condiciones_comerciales TEXT
) ENGINE=InnoDB;

CREATE TABLE ordenes_compra (
    id_orden_compra INT AUTO_INCREMENT PRIMARY KEY,
    numero_oc VARCHAR(50) NOT NULL UNIQUE,
    id_proveedor INT NOT NULL,
    fecha_emision DATE NOT NULL,
    estado ENUM('BORRADOR', 'EMITIDA', 'PARCIAL', 'RECIBIDA', 'ANULADA') NOT NULL,
    monto_subtotal DECIMAL(15,2) NOT NULL DEFAULT 0,
    monto_iva DECIMAL(15,2) NOT NULL DEFAULT 0,
    monto_total DECIMAL(15,2) NOT NULL DEFAULT 0,
    id_usuario_creador INT NOT NULL,
    CONSTRAINT fk_ordenes_proveedor
        FOREIGN KEY (id_proveedor) REFERENCES proveedores(id_proveedor),
    CONSTRAINT fk_ordenes_usuario
        FOREIGN KEY (id_usuario_creador) REFERENCES usuarios(id_usuario)
) ENGINE=InnoDB;

CREATE TABLE detalle_orden_compra (
    id_detalle_oc INT AUTO_INCREMENT PRIMARY KEY,
    id_orden_compra INT NOT NULL,
    id_producto INT NOT NULL,
    cantidad_solicitada INT NOT NULL,
    cantidad_recibida INT NOT NULL DEFAULT 0,
    costo_pactado_unitario DECIMAL(15,2) NOT NULL,
    CONSTRAINT fk_detalle_oc_orden
        FOREIGN KEY (id_orden_compra) REFERENCES ordenes_compra(id_orden_compra),
    CONSTRAINT fk_detalle_oc_producto
        FOREIGN KEY (id_producto) REFERENCES productos(id_producto),
    CHECK (cantidad_solicitada > 0),
    CHECK (cantidad_recibida >= 0)
) ENGINE=InnoDB;

CREATE TABLE recepciones_facturas_compra (
    id_recepcion INT AUTO_INCREMENT PRIMARY KEY,
    id_orden_compra INT NOT NULL,
    numero_factura_proveedor VARCHAR(100) NOT NULL,
    fecha_factura DATE NOT NULL,
    fecha_recepcion TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    monto_total_facturado DECIMAL(15,2) NOT NULL DEFAULT 0,
    id_asiento_contable INT NULL,
    CONSTRAINT fk_recepciones_orden
        FOREIGN KEY (id_orden_compra) REFERENCES ordenes_compra(id_orden_compra),
    CONSTRAINT fk_recepciones_asiento
        FOREIGN KEY (id_asiento_contable) REFERENCES libro_diario_asientos(id_asiento)
) ENGINE=InnoDB;

CREATE TABLE detalle_recepcion_lotes (
    id_detalle_recepcion INT AUTO_INCREMENT PRIMARY KEY,
    id_recepcion INT NOT NULL,
    id_producto INT NOT NULL,
    numero_lote VARCHAR(100) NOT NULL,
    fecha_vencimiento DATE NOT NULL,
    cantidad_ingresada INT NOT NULL,
    costo_adquisicion_unitario DECIMAL(15,2) NOT NULL,
    CONSTRAINT fk_detalle_recepcion_recepcion
        FOREIGN KEY (id_recepcion) REFERENCES recepciones_facturas_compra(id_recepcion),
    CONSTRAINT fk_detalle_recepcion_producto
        FOREIGN KEY (id_producto) REFERENCES productos(id_producto),
    CHECK (cantidad_ingresada > 0)
) ENGINE=InnoDB;

-- >>> NUEVA TABLA AGREGADA AQUÍ <<<
CREATE TABLE cuentas_por_pagar (
    id_cuenta_pagar INT AUTO_INCREMENT PRIMARY KEY,
    id_recepcion INT NOT NULL,
    id_proveedor INT NOT NULL,
    numero_documento VARCHAR(100) NOT NULL,
    fecha_emision DATE NOT NULL,
    fecha_vencimiento DATE NOT NULL,
    monto_total DECIMAL(15,2) NOT NULL,
    monto_pagado DECIMAL(15,2) NOT NULL DEFAULT 0,
    estado_pago VARCHAR(50) NOT NULL DEFAULT 'PENDIENTE',
    id_asiento_contable INT NULL,
    CONSTRAINT uq_cuentas_pagar_documento
        UNIQUE (id_proveedor, numero_documento),
    CONSTRAINT fk_cuentas_pagar_recepcion
        FOREIGN KEY (id_recepcion) REFERENCES recepciones_facturas_compra(id_recepcion),
    CONSTRAINT fk_cuentas_pagar_proveedor
        FOREIGN KEY (id_proveedor) REFERENCES proveedores(id_proveedor),
    CONSTRAINT fk_cuentas_pagar_asiento
        FOREIGN KEY (id_asiento_contable) REFERENCES libro_diario_asientos(id_asiento),
    CONSTRAINT chk_cuentas_pagar_montos
        CHECK (monto_total >= 0 AND monto_pagado >= 0 AND monto_pagado <= monto_total),
    CONSTRAINT chk_cuentas_pagar_fechas
        CHECK (fecha_vencimiento >= fecha_emision)
) ENGINE=InnoDB;

-- =========================
-- VENTAS Y COBRANZAS
-- =========================

CREATE TABLE ventas_encabezado (
    id_venta INT AUTO_INCREMENT PRIMARY KEY,
    folio_documento VARCHAR(50) NOT NULL,
    tipo_documento ENUM('BOLETA', 'FACTURA', 'NOTA_CREDITO', 'OTRO') NOT NULL,
    fecha_hora_emision TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    id_cliente INT NOT NULL,
    id_convenio INT NULL,
    estado_documento ENUM('EMITIDO', 'PAGADO', 'ANULADO', 'PENDIENTE') NOT NULL,
    forma_pago ENUM('EFECTIVO', 'TARJETA', 'TRANSFERENCIA', 'CREDITO', 'OTRO') NOT NULL,
    monto_neto DECIMAL(15,2) NOT NULL DEFAULT 0,
    monto_iva DECIMAL(15,2) NOT NULL DEFAULT 0,
    monto_descuento DECIMAL(15,2) NOT NULL DEFAULT 0,
    monto_total DECIMAL(15,2) NOT NULL DEFAULT 0,
    id_usuario_cajero INT NOT NULL,
    id_asiento_contable INT NULL,
    CONSTRAINT fk_ventas_cliente
        FOREIGN KEY (id_cliente) REFERENCES clientes(id_cliente),
    CONSTRAINT fk_ventas_convenio
        FOREIGN KEY (id_convenio) REFERENCES convenios_salud(id_convenio),
    CONSTRAINT fk_ventas_usuario
        FOREIGN KEY (id_usuario_cajero) REFERENCES usuarios(id_usuario),
    CONSTRAINT fk_ventas_asiento
        FOREIGN KEY (id_asiento_contable) REFERENCES libro_diario_asientos(id_asiento)
) ENGINE=InnoDB;

CREATE TABLE ventas_detalle (
    id_detalle_venta INT AUTO_INCREMENT PRIMARY KEY,
    id_venta INT NOT NULL,
    id_producto INT NOT NULL,
    id_lote INT NOT NULL,
    cantidad INT NOT NULL,
    precio_unitario DECIMAL(15,2) NOT NULL,
    subtotal DECIMAL(15,2) NOT NULL,
    CONSTRAINT fk_ventas_detalle_venta
        FOREIGN KEY (id_venta) REFERENCES ventas_encabezado(id_venta),
    CONSTRAINT fk_ventas_detalle_producto
        FOREIGN KEY (id_producto) REFERENCES productos(id_producto),
    CONSTRAINT fk_ventas_detalle_lote
        FOREIGN KEY (id_lote) REFERENCES lotes(id_lote),
    CHECK (cantidad > 0)
) ENGINE=InnoDB;

CREATE TABLE cuentas_por_cobrar (
    id_cuenta_cobrar INT AUTO_INCREMENT PRIMARY KEY,
    id_venta INT NOT NULL,
    id_cliente INT NOT NULL,
    id_convenio INT NULL,
    monto_pendiente DECIMAL(15,2) NOT NULL DEFAULT 0,
    fecha_vencimiento_pago DATE NOT NULL,
    dias_morosidad INT NOT NULL DEFAULT 0,
    estado_cobro ENUM('PENDIENTE', 'PAGADA', 'VENCIDA', 'ANULADA') NOT NULL,
    CONSTRAINT fk_cxc_venta
        FOREIGN KEY (id_venta) REFERENCES ventas_encabezado(id_venta),
    CONSTRAINT fk_cxc_cliente
        FOREIGN KEY (id_cliente) REFERENCES clientes(id_cliente),
    CONSTRAINT fk_cxc_convenio
        FOREIGN KEY (id_convenio) REFERENCES convenios_salud(id_convenio),
    UNIQUE KEY uk_cxc_venta (id_venta)
) ENGINE=InnoDB;

-- =========================
-- MOVIMIENTOS DE INVENTARIO
-- =========================

CREATE TABLE kardex_movimientos (
    id_kardex BIGINT AUTO_INCREMENT PRIMARY KEY,
    id_producto INT NOT NULL,
    id_lote INT NOT NULL,
    tipo_movimiento ENUM('ENTRADA', 'SALIDA', 'AJUSTE', 'MERMA') NOT NULL,
    origen_documento ENUM('COMPRA', 'VENTA', 'MERMA', 'AJUSTE', 'OTRO') NOT NULL,
    id_referencia_origen INT NULL,
    cantidad INT NOT NULL,
    costo_unitario DECIMAL(15,2) NOT NULL DEFAULT 0,
    saldo_stock_producto INT NOT NULL DEFAULT 0,
    fecha_movimiento TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    id_usuario INT NOT NULL,
    CONSTRAINT fk_kardex_producto
        FOREIGN KEY (id_producto) REFERENCES productos(id_producto),
    CONSTRAINT fk_kardex_lote
        FOREIGN KEY (id_lote) REFERENCES lotes(id_lote),
    CONSTRAINT fk_kardex_usuario
        FOREIGN KEY (id_usuario) REFERENCES usuarios(id_usuario)
) ENGINE=InnoDB;

CREATE TABLE registro_mermas (
    id_merma INT AUTO_INCREMENT PRIMARY KEY,
    id_producto INT NOT NULL,
    id_lote INT NOT NULL,
    cantidad INT NOT NULL,
    motivo ENUM('VENCIMIENTO', 'DANO', 'PERDIDA', 'DEVOLUCION', 'OTRO') NOT NULL,
    costo_total_perdida DECIMAL(15,2) NOT NULL DEFAULT 0,
    fecha_merma TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    id_usuario INT NOT NULL,
    CONSTRAINT fk_mermas_producto
        FOREIGN KEY (id_producto) REFERENCES productos(id_producto),
    CONSTRAINT fk_mermas_lote
        FOREIGN KEY (id_lote) REFERENCES lotes(id_lote),
    CONSTRAINT fk_mermas_usuario
        FOREIGN KEY (id_usuario) REFERENCES usuarios(id_usuario),
    CHECK (cantidad > 0)
) ENGINE=InnoDB;

-- =========================
-- ÍNDICES ADICIONALES
-- =========================

CREATE INDEX idx_cuentas_pagar_vencimiento
    ON cuentas_por_pagar(fecha_vencimiento);

CREATE INDEX idx_cuentas_pagar_estado
    ON cuentas_por_pagar(estado_pago);

CREATE INDEX idx_cuentas_cobrar_estado
    ON cuentas_por_cobrar(estado_cobro);

-- ==========================================================
-- DATOS SEMILLA
-- Todos los integrantes parten con la misma data de prueba.
-- ==========================================================

-- ---- Roles y permisos base ----
INSERT INTO roles (nombre_rol, descripcion) VALUES
    ('Administrador', 'Acceso total al sistema'),
    ('Vendedor', 'Acceso al módulo de Ventas y consulta de Inventario'),
    ('Bodega', 'Acceso a Inventario, Kardex y Recepción de compras'),
    ('Contador', 'Acceso al módulo de Contabilidad'),
    ('Comprador', 'Acceso al módulo de Compras y Proveedores');

INSERT INTO permisos (codigo_permiso, modulo, descripcion) VALUES
    ('VENTAS_CREAR', 'Ventas', 'Emitir boletas/facturas'),
    ('INVENTARIO_VER', 'Inventario', 'Consultar stock y kardex'),
    ('COMPRAS_CREAR', 'Compras', 'Emitir órdenes de compra'),
    ('CONTABILIDAD_VER', 'Contabilidad', 'Consultar libro diario y plan de cuentas');

-- Asigna todos los permisos al rol Administrador (id_rol = 1)
INSERT INTO rol_permisos (id_rol, id_permiso)
SELECT 1, id_permiso FROM permisos;

-- ---- Usuario de prueba ----
-- Login: admin@erp.cl / admin123
-- (el hash corresponde a SHA-256 de "admin123"; usar BCrypt en producción)
INSERT INTO usuarios (rut_usuario, nombre, email, password_hash, id_rol, activo) VALUES
    ('11.111.111-1', 'Administrador General', 'admin@erp.cl',
     '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', 1, TRUE);

-- ---- Clientes y convenios ----
INSERT INTO clientes (rut_cliente, razon_social_nombre, email, telefono, tipo_cliente) VALUES
    ('22.222.222-2', 'Cliente Demo Persona', 'cliente.demo@correo.cl', '+56912345678', 'PERSONA'),
    ('76.111.222-3', 'Clínica Demo SPA', 'contacto@clinicademo.cl', '+56221234567', 'INSTITUCION');

INSERT INTO convenios_salud (id_cliente, nombre_convenio, porcentaje_descuento, limite_credito, dias_credito_morosidad, activo) VALUES
    (2, 'Convenio Clínica Demo', 15.00, 5000000.00, 30, TRUE);

-- ---- Productos e inventario ----
INSERT INTO categorias_producto (nombre_categoria) VALUES
    ('Medicamentos'),
    ('Insumos médicos'),
    ('Dermocosmética');

INSERT INTO productos
    (sku, codigo_barras, nombre_comercial, principio_activo, registro_sanitario,
     formato_presentacion, id_categoria, precio_venta_base, costo_promedio_ponderado,
     stock_minimo, punto_reorden, activo)
VALUES
    ('MED-0001', '7801234567890', 'Paracetamol 500mg', 'Paracetamol', 'ISP-12345',
     'Caja x 20 comprimidos', 1, 2500.00, 1200.00, 50, 80, TRUE);

INSERT INTO lotes (id_producto, numero_lote, fecha_vencimiento, stock_actual) VALUES
    (1, 'LOTE-2026-001', '2028-06-30', 150);

-- ---- Plan de cuentas (nivel 1: cuentas mayores) ----
INSERT INTO plan_cuentas (codigo_cuenta, nombre_cuenta, tipo_cuenta, nivel, cuenta_padre_id) VALUES
    ('1', 'ACTIVOS', 'ACTIVO', 1, NULL),
    ('2', 'PASIVOS', 'PASIVO', 1, NULL),
    ('3', 'PATRIMONIO', 'PATRIMONIO', 1, NULL),
    ('4', 'INGRESOS', 'INGRESO', 1, NULL),
    ('5', 'GASTOS', 'GASTO', 1, NULL),
    ('1.1.01', 'Caja', 'ACTIVO', 2, 1),
    ('1.1.02', 'Inventario de Mercaderías', 'ACTIVO', 2, 1),
    ('4.1.01', 'Ventas Netas', 'INGRESO', 2, 4),
    ('5.1.01', 'Costo de Ventas', 'GASTO', 2, 5);

-- ---- Proveedores ----
INSERT INTO proveedores (rut_proveedor, razon_social, nombre_contacto, telefono, email, condiciones_comerciales) VALUES
    ('77.888.999-0', 'Distribuidora Farmacéutica Demo Ltda.', 'Juan Pérez', '+56221112233',
     'ventas@distribuidorademo.cl', 'Pago a 30 días');