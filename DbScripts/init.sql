-- ==========================================================
-- init.sql - Base de datos erp_farmaceutico
-- Se ejecuta automáticamente al levantar el contenedor MySQL
-- (docker-entrypoint-initdb.d) la primera vez que se crea el volumen.
-- ==========================================================

CREATE DATABASE IF NOT EXISTS erp_farmaceutico
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

USE erp_farmaceutico;
SET NAMES utf8mb4;
SET CHARACTER SET utf8mb4;

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
-- BLOQUE 1: DATOS SEMILLA ORIGINALES
-- ==========================================================

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

INSERT INTO rol_permisos (id_rol, id_permiso)
SELECT 1, id_permiso FROM permisos;

INSERT INTO usuarios (rut_usuario, nombre, email, password_hash, id_rol, activo) VALUES
    ('11.111.111-1', 'Administrador General', 'admin@erp.cl', '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', 1, TRUE);

INSERT INTO clientes (rut_cliente, razon_social_nombre, email, telefono, tipo_cliente) VALUES
    ('22.222.222-2', 'Cliente Demo Persona', 'cliente.demo@correo.cl', '+56912345678', 'PERSONA'),
    ('76.111.222-3', 'Clínica Demo SPA', 'contacto@clinicademo.cl', '+56221234567', 'INSTITUCION');

INSERT INTO convenios_salud (id_cliente, nombre_convenio, porcentaje_descuento, limite_credito, dias_credito_morosidad, activo) VALUES
    (2, 'Convenio Clínica Demo', 15.00, 5000000.00, 30, TRUE);

INSERT INTO categorias_producto (nombre_categoria) VALUES
    ('Medicamentos'),
    ('Insumos médicos'),
    ('Dermocosmética');

INSERT INTO productos (sku, codigo_barras, nombre_comercial, principio_activo, registro_sanitario, formato_presentacion, id_categoria, precio_venta_base, costo_promedio_ponderado, stock_minimo, punto_reorden, activo) VALUES
    ('MED-0001', '7801234567890', 'Paracetamol 500mg', 'Paracetamol', 'ISP-12345', 'Caja x 20 comprimidos', 1, 2500.00, 1200.00, 50, 80, TRUE);

INSERT INTO lotes (id_producto, numero_lote, fecha_vencimiento, stock_actual) VALUES
    (1, 'LOTE-2026-001', '2028-06-30', 150);

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

INSERT INTO proveedores (rut_proveedor, razon_social, nombre_contacto, telefono, email, condiciones_comerciales) VALUES
    ('77.888.999-0', 'Distribuidora Farmacéutica Demo Ltda.', 'Juan Pérez', '+56221112233', 'ventas@distribuidorademo.cl', 'Pago a 30 días');

-- ==========================================================
-- BLOQUE 2: PRIMER SET DE REGISTROS ADICIONALES
-- ==========================================================

INSERT INTO usuarios (rut_usuario, nombre, email, password_hash, id_rol, activo) VALUES
    ('12.345.678-9', 'María Vendedora', 'ventas@erp.cl', '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', 2, TRUE),
    ('13.456.789-0', 'Pedro Bodega', 'bodega@erp.cl', '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', 3, TRUE);

INSERT INTO rol_permisos (id_rol, id_permiso) VALUES
    (2, 1),
    (2, 2),
    (3, 2);

INSERT INTO productos (sku, codigo_barras, nombre_comercial, principio_activo, registro_sanitario, formato_presentacion, id_categoria, precio_venta_base, costo_promedio_ponderado, stock_minimo, punto_reorden, activo) VALUES
    ('MED-0002', '7801234567891', 'Ibuprofeno 400mg', 'Ibuprofeno', 'ISP-12346', 'Caja x 10 comprimidos', 1, 1500.00, 800.00, 100, 150, TRUE),
    ('INS-0001', '7801234567892', 'Jeringa 5ml', 'N/A', 'ISP-12347', 'Unidad', 2, 300.00, 100.00, 500, 1000, TRUE);

INSERT INTO lotes (id_producto, numero_lote, fecha_vencimiento, stock_actual) VALUES
    (2, 'LOTE-2026-002', '2027-12-31', 300),
    (3, 'LOTE-2026-003', '2029-01-01', 1500);

INSERT INTO libro_diario_asientos (numero_asiento, fecha_asiento, glosa_descripcion, origen, id_usuario) VALUES
    (1, '2026-10-15', 'Apertura de caja inicial', 'AJUSTE', 1);

INSERT INTO detalle_asiento_contable (id_asiento, id_cuenta, debe, haber) VALUES
    (1, 6, 100000.00, 0.00),
    (1, 3, 0.00, 100000.00);

INSERT INTO ordenes_compra (numero_oc, id_proveedor, fecha_emision, estado, monto_subtotal, monto_iva, monto_total, id_usuario_creador) VALUES
    ('OC-2026-001', 1, '2026-10-10', 'RECIBIDA', 100000.00, 19000.00, 119000.00, 1);

INSERT INTO detalle_orden_compra (id_orden_compra, id_producto, cantidad_solicitada, cantidad_recibida, costo_pactado_unitario) VALUES
    (1, 2, 100, 100, 1000.00);

INSERT INTO recepciones_facturas_compra (id_orden_compra, numero_factura_proveedor, fecha_factura, monto_total_facturado, id_asiento_contable) VALUES
    (1, 'FACT-PROV-998', '2026-10-12', 119000.00, NULL);

INSERT INTO detalle_recepcion_lotes (id_recepcion, id_producto, numero_lote, fecha_vencimiento, cantidad_ingresada, costo_adquisicion_unitario) VALUES
    (1, 2, 'LOTE-2026-004', '2028-10-10', 100, 1000.00);

INSERT INTO cuentas_por_pagar (id_recepcion, id_proveedor, numero_documento, fecha_emision, fecha_vencimiento, monto_total, monto_pagado, estado_pago, id_asiento_contable) VALUES
    (1, 1, 'FACT-PROV-998', '2026-10-12', '2026-11-12', 119000.00, 0.00, 'PENDIENTE', NULL);

INSERT INTO ventas_encabezado (folio_documento, tipo_documento, id_cliente, id_convenio, estado_documento, forma_pago, monto_neto, monto_iva, monto_descuento, monto_total, id_usuario_cajero, id_asiento_contable) VALUES
    ('BOL-00001', 'BOLETA', 1, NULL, 'EMITIDO', 'EFECTIVO', 2100.84, 399.16, 0.00, 2500.00, 2, NULL);

INSERT INTO ventas_detalle (id_venta, id_producto, id_lote, cantidad, precio_unitario, subtotal) VALUES
    (1, 1, 1, 1, 2500.00, 2500.00);

INSERT INTO cuentas_por_cobrar (id_venta, id_cliente, id_convenio, monto_pendiente, fecha_vencimiento_pago, dias_morosidad, estado_cobro) VALUES
    (1, 1, NULL, 0.00, '2026-10-15', 0, 'PAGADA');

INSERT INTO kardex_movimientos (id_producto, id_lote, tipo_movimiento, origen_documento, id_referencia_origen, cantidad, costo_unitario, saldo_stock_producto, id_usuario) VALUES
    (2, 4, 'ENTRADA', 'COMPRA', 1, 100, 1000.00, 400, 3),
    (1, 1, 'SALIDA', 'VENTA', 1, 1, 1200.00, 149, 2);

INSERT INTO registro_mermas (id_producto, id_lote, cantidad, motivo, costo_total_perdida, id_usuario) VALUES
    (3, 2, 5, 'DANO', 500.00, 3);

-- ==========================================================
-- BLOQUE 3: CARGA MASIVA DE DATOS DE PRUEBA (IDs 10+)
-- ==========================================================

INSERT INTO roles (id_rol, nombre_rol, descripcion) VALUES
    (10, 'Gerente Comercial', 'Supervisión general de ventas y convenios'),
    (11, 'Auditor Contable', 'Acceso de solo lectura a contabilidad e inventario'),
    (12, 'Cajero', 'Operación exclusiva del punto de venta');

INSERT INTO permisos (id_permiso, codigo_permiso, modulo, descripcion) VALUES
    (10, 'PRODUCTOS_CREAR', 'Inventario', 'Crear y editar productos'),
    (11, 'PROVEEDORES_CREAR', 'Compras', 'Gestión de proveedores'),
    (12, 'CONVENIOS_ADMIN', 'Ventas', 'Administrar convenios de salud'),
    (13, 'REPORTES_VER', 'Gerencia', 'Ver reportes financieros y de ventas');

INSERT INTO rol_permisos (id_rol, id_permiso) VALUES
    (10, 12), (10, 13), (11, 13), (11, 2), (12, 1);

INSERT INTO usuarios (id_usuario, rut_usuario, nombre, email, password_hash, id_rol, activo) VALUES
    (10, '15.555.666-7', 'Carlos Gerente', 'carlos.g@erp.cl', '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', 10, TRUE),
    (11, '16.777.888-9', 'Ana Auditora', 'ana.a@erp.cl', '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', 11, TRUE),
    (12, '17.888.999-0', 'Luis Cajero', 'luis.c@erp.cl', '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', 12, TRUE),
    (13, '18.123.456-1', 'Sofía Ventas', 'sofia.v@erp.cl', '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', 2, TRUE),
    (14, '19.333.444-5', 'Roberto Bodega', 'roberto.b@erp.cl', '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', 3, TRUE);

INSERT INTO clientes (id_cliente, rut_cliente, razon_social_nombre, email, telefono, tipo_cliente) VALUES
    (10, '20.111.222-3', 'Juan Pérez Soto', 'jperez@correo.cl', '+56987654321', 'PERSONA'),
    (11, '21.222.333-4', 'Camila Rojas', 'crojas@correo.cl', '+56987654322', 'PERSONA'),
    (12, '77.333.444-5', 'Mutual de Trabajadores', 'contacto@mutualtrab.cl', '+56223334444', 'INSTITUCION'),
    (13, '78.444.555-6', 'Isapre VidaSalud', 'convenios@vidasalud.cl', '+56225556666', 'EMPRESA'),
    (14, '22.555.666-7', 'Felipe Morales', 'fmorales@correo.cl', '+56987654323', 'PERSONA'),
    (15, '23.666.777-8', 'Andrea Silva', 'asilva@correo.cl', '+56987654324', 'PERSONA');

INSERT INTO convenios_salud (id_convenio, id_cliente, nombre_convenio, porcentaje_descuento, limite_credito, dias_credito_morosidad, activo) VALUES
    (10, 12, 'Convenio Mutual Base', 20.00, 10000000.00, 45, TRUE),
    (11, 13, 'Convenio Isapre Preferente', 25.00, 15000000.00, 30, TRUE);

INSERT INTO categorias_producto (id_categoria, nombre_categoria) VALUES
    (10, 'Vitaminas y Suplementos'),
    (11, 'Cuidado Personal e Higiene'),
    (12, 'Primeros Auxilios'),
    (13, 'Antialérgicos'),
    (14, 'Cardiovasculares');

INSERT INTO productos (id_producto, sku, codigo_barras, nombre_comercial, principio_activo, registro_sanitario, formato_presentacion, id_categoria, precio_venta_base, costo_promedio_ponderado, stock_minimo, punto_reorden, activo) VALUES
    (10, 'VIT-0001', '7801111111111', 'Vitamina C 1000mg', 'Ácido Ascórbico', 'ISP-V111', 'Frasco x 30 cap', 10, 4500.00, 2000.00, 20, 40, TRUE),
    (11, 'VIT-0002', '7802222222222', 'Complejo B Forte', 'Vitaminas B', 'ISP-V222', 'Caja x 30 comp', 10, 5000.00, 2200.00, 15, 30, TRUE),
    (12, 'HIG-0001', '7803333333333', 'Shampoo Anticaspa', 'Ketoconazol', 'ISP-H111', 'Frasco 200ml', 11, 6500.00, 3000.00, 10, 20, TRUE),
    (13, 'PAX-0001', '7804444444444', 'Gasas Estériles', 'Algodón', 'ISP-P111', 'Sobre x 5 un', 12, 1200.00, 400.00, 100, 150, TRUE),
    (14, 'PAX-0002', '7805555555555', 'Povidona Yodada 10%', 'Povidona', 'ISP-P222', 'Frasco 100ml', 12, 2800.00, 1100.00, 30, 50, TRUE),
    (15, 'ALE-0001', '7806666666666', 'Loratadina 10mg', 'Loratadina', 'ISP-A111', 'Caja x 30 comp', 13, 3500.00, 1500.00, 40, 80, TRUE),
    (16, 'ALE-0002', '7807777777777', 'Desloratadina 5mg', 'Desloratadina', 'ISP-A222', 'Caja x 30 comp', 13, 5500.00, 2500.00, 30, 60, TRUE),
    (17, 'CAR-0001', '7808888888888', 'Losartán 50mg', 'Losartán Potásico', 'ISP-C111', 'Caja x 30 comp', 14, 2500.00, 800.00, 50, 100, TRUE),
    (18, 'CAR-0002', '7809999999999', 'Atenolol 50mg', 'Atenolol', 'ISP-C222', 'Caja x 20 comp', 14, 1800.00, 600.00, 40, 80, TRUE),
    (19, 'DER-0001', '7801010101010', 'Crema Hidratante Urea 10%', 'Urea', 'ISP-D111', 'Pote 250g', 3, 12000.00, 6000.00, 10, 25, TRUE);

INSERT INTO lotes (id_lote, id_producto, numero_lote, fecha_vencimiento, stock_actual) VALUES
    (10, 10, 'LOT-VIT-01', '2027-05-30', 200),
    (11, 11, 'LOT-VIT-02', '2028-01-15', 150),
    (12, 12, 'LOT-HIG-01', '2026-12-30', 80),
    (13, 13, 'LOT-PAX-01', '2029-08-01', 500),
    (14, 14, 'LOT-PAX-02', '2028-11-30', 120),
    (15, 15, 'LOT-ALE-01', '2027-04-20', 300),
    (16, 16, 'LOT-ALE-02', '2027-09-10', 250),
    (17, 17, 'LOT-CAR-01', '2028-02-28', 400),
    (18, 18, 'LOT-CAR-02', '2027-06-30', 350),
    (19, 19, 'LOT-DER-01', '2026-10-31', 60);

INSERT INTO plan_cuentas (id_cuenta, codigo_cuenta, nombre_cuenta, tipo_cuenta, nivel, cuenta_padre_id) VALUES
    (10, '1.1.03', 'Cuentas por Cobrar Clientes', 'ACTIVO', 2, 1),
    (11, '2.1.01', 'Cuentas por Pagar Proveedores', 'PASIVO', 2, 2),
    (12, '2.1.02', 'IVA Débito Fiscal', 'PASIVO', 2, 2),
    (13, '1.1.04', 'IVA Crédito Fiscal', 'ACTIVO', 2, 1),
    (14, '5.1.02', 'Gastos de Administración', 'GASTO', 2, 5),
    (15, '4.1.02', 'Ingresos por Convenios', 'INGRESO', 2, 4);

INSERT INTO proveedores (id_proveedor, rut_proveedor, razon_social, nombre_contacto, telefono, email, condiciones_comerciales) VALUES
    (10, '76.555.444-3', 'Laboratorios Andinos S.A.', 'Marta López', '+56227778888', 'ventas@labandinos.cl', 'Pago a 60 días'),
    (11, '77.666.555-4', 'Insumos Médicos Sur', 'Pedro Castro', '+56412223333', 'contacto@insumossur.cl', 'Pago al contado'),
    (12, '78.777.666-5', 'Droguería Central', 'Carla Ruiz', '+56229990000', 'despachos@drogueriacentral.cl', 'Pago a 30 días');

INSERT INTO ordenes_compra (id_orden_compra, numero_oc, id_proveedor, fecha_emision, estado, monto_subtotal, monto_iva, monto_total, id_usuario_creador) VALUES
    (10, 'OC-2026-002', 10, '2026-10-10', 'RECIBIDA', 160000.00, 30400.00, 190400.00, 1),
    (11, 'OC-2026-003', 11, '2026-10-11', 'RECIBIDA', 50000.00, 9500.00, 59500.00, 1),
    (12, 'OC-2026-004', 12, '2026-10-14', 'EMITIDA', 300000.00, 57000.00, 357000.00, 1);

INSERT INTO detalle_orden_compra (id_detalle_oc, id_orden_compra, id_producto, cantidad_solicitada, cantidad_recibida, costo_pactado_unitario) VALUES
    (10, 10, 17, 200, 200, 800.00),
    (11, 11, 13, 100, 100, 400.00),
    (12, 11, 14, 10, 10, 1000.00),
    (13, 12, 10, 150, 0, 2000.00);

INSERT INTO recepciones_facturas_compra (id_recepcion, id_orden_compra, numero_factura_proveedor, fecha_factura, monto_total_facturado, id_asiento_contable) VALUES
    (10, 10, 'FACT-PROV-1100', '2026-10-12', 190400.00, NULL),
    (11, 11, 'FACT-PROV-3321', '2026-10-13', 59500.00, NULL);

INSERT INTO detalle_recepcion_lotes (id_detalle_recepcion, id_recepcion, id_producto, numero_lote, fecha_vencimiento, cantidad_ingresada, costo_adquisicion_unitario) VALUES
    (10, 10, 17, 'LOT-CAR-01', '2028-02-28', 200, 800.00),
    (11, 11, 13, 'LOT-PAX-01', '2029-08-01', 100, 400.00),
    (12, 11, 14, 'LOT-PAX-02', '2028-11-30', 10, 1000.00);

INSERT INTO cuentas_por_pagar (id_cuenta_pagar, id_recepcion, id_proveedor, numero_documento, fecha_emision, fecha_vencimiento, monto_total, monto_pagado, estado_pago, id_asiento_contable) VALUES
    (10, 10, 10, 'FACT-PROV-1100', '2026-10-12', '2026-12-12', 190400.00, 0.00, 'PENDIENTE', NULL),
    (11, 11, 11, 'FACT-PROV-3321', '2026-10-13', '2026-10-13', 59500.00, 59500.00, 'PAGADO', NULL);

INSERT INTO ventas_encabezado (id_venta, folio_documento, tipo_documento, id_cliente, id_convenio, estado_documento, forma_pago, monto_neto, monto_iva, monto_descuento, monto_total, id_usuario_cajero, id_asiento_contable) VALUES
    (10, 'BOL-00002', 'BOLETA', 10, NULL, 'EMITIDO', 'TARJETA', 5882.35, 1117.65, 0.00, 7000.00, 12, NULL),
    (11, 'BOL-00003', 'BOLETA', 11, NULL, 'EMITIDO', 'EFECTIVO', 8403.36, 1596.64, 0.00, 10000.00, 12, NULL),
    (12, 'FACT-00001', 'FACTURA', 12, 10, 'EMITIDO', 'CREDITO', 21008.40, 3991.60, 5000.00, 20000.00, 13, NULL),
    (13, 'FACT-00002', 'FACTURA', 13, 11, 'EMITIDO', 'CREDITO', 42016.81, 7983.19, 12500.00, 37500.00, 13, NULL),
    (14, 'BOL-00004', 'BOLETA', 14, NULL, 'ANULADO', 'EFECTIVO', 2100.84, 399.16, 0.00, 2500.00, 12, NULL);

INSERT INTO ventas_detalle (id_detalle_venta, id_venta, id_producto, id_lote, cantidad, precio_unitario, subtotal) VALUES
    (10, 10, 15, 15, 2, 3500.00, 7000.00),
    (11, 11, 10, 10, 1, 4500.00, 4500.00),
    (12, 11, 16, 16, 1, 5500.00, 5500.00),
    (13, 12, 17, 17, 10, 2500.00, 25000.00),
    (14, 13, 11, 11, 10, 5000.00, 50000.00);

INSERT INTO cuentas_por_cobrar (id_cuenta_cobrar, id_venta, id_cliente, id_convenio, monto_pendiente, fecha_vencimiento_pago, dias_morosidad, estado_cobro) VALUES
    (10, 10, 10, NULL, 0.00, '2026-10-15', 0, 'PAGADA'),
    (11, 11, 11, NULL, 0.00, '2026-10-15', 0, 'PAGADA'),
    (12, 12, 12, 10, 20000.00, '2026-11-29', 0, 'PENDIENTE'),
    (13, 13, 13, 11, 37500.00, '2026-11-14', 0, 'PENDIENTE'),
    (14, 14, 14, NULL, 0.00, '2026-10-15', 0, 'ANULADA');

INSERT INTO kardex_movimientos (id_kardex, id_producto, id_lote, tipo_movimiento, origen_documento, id_referencia_origen, cantidad, costo_unitario, saldo_stock_producto, id_usuario) VALUES
    (10, 17, 17, 'ENTRADA', 'COMPRA', 10, 200, 800.00, 200, 14),
    (11, 13, 13, 'ENTRADA', 'COMPRA', 11, 100, 400.00, 100, 14),
    (12, 14, 14, 'ENTRADA', 'COMPRA', 11, 10, 1000.00, 10, 14),
    (13, 15, 15, 'SALIDA', 'VENTA', 10, 2, 1500.00, 298, 12),
    (14, 10, 10, 'SALIDA', 'VENTA', 11, 1, 2000.00, 199, 12),
    (15, 16, 16, 'SALIDA', 'VENTA', 11, 1, 2500.00, 249, 12),
    (16, 17, 17, 'SALIDA', 'VENTA', 12, 10, 800.00, 190, 13),
    (17, 11, 11, 'SALIDA', 'VENTA', 13, 10, 2200.00, 140, 13);

INSERT INTO registro_mermas (id_merma, id_producto, id_lote, cantidad, motivo, costo_total_perdida, id_usuario) VALUES
    (10, 19, 19, 2, 'DANO', 12000.00, 14),
    (11, 12, 12, 1, 'PERDIDA', 3000.00, 14),
    (12, 14, 14, 3, 'VENCIMIENTO', 3300.00, 14);

INSERT INTO libro_diario_asientos (id_asiento, numero_asiento, fecha_asiento, glosa_descripcion, origen, id_usuario) VALUES
    (10, 2, '2026-10-12', 'Centralización Compras Octubre (Parcial)', 'COMPRA', 11),
    (11, 3, '2026-10-15', 'Centralización Ventas Diarias', 'VENTA', 11);

INSERT INTO detalle_asiento_contable (id_detalle_asiento, id_asiento, id_cuenta, debe, haber) VALUES
    (10, 10, 7, 200000.00, 0.00), 
    (11, 10, 13, 38000.00, 0.00), 
    (12, 10, 11, 0.00, 238000.00), 
    (13, 11, 6, 17000.00, 0.00), 
    (14, 11, 10, 57500.00, 0.00), 
    (15, 11, 8, 0.00, 62605.04), 
    (16, 11, 12, 0.00, 11894.96);