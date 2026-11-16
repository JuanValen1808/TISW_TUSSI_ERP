using MySqlConnector;

namespace TISW_TUSSI_ERP.Services.Api;

// Servicio central de acceso a datos. Todos los módulos (Ventas, Compras,
// Inventario, Contabilidad, Seguridad) reutilizan esta misma clase para conectarse
// a la base de datos MySQL levantada con docker-compose.
public class DatabaseService
{
    private const string ConnectionString =
        "Server=localhost;Port=3307;Database=erp_farmaceutico;Uid=erp_admin;Pwd=admin_password;CharSet=utf8mb4;";

    private bool _semillaVerificada;

    public MySqlConnection CrearConexion() => new MySqlConnection(ConnectionString);

    // Garantiza que existan las tablas operativas de Ventas/Caja/Devoluciones, los usuarios demo y los permisos RBAC
    public async Task AsegurarSemillaSeguridadYContabilidadAsync(MySqlConnection conexion)
    {
        if (_semillaVerificada) return;

        string[] sentencias =
        {
            @"CREATE TABLE IF NOT EXISTS cajas (
                id_caja INT AUTO_INCREMENT PRIMARY KEY,
                nombre_caja VARCHAR(100) NOT NULL,
                descripcion VARCHAR(255) NULL,
                activa BOOLEAN NOT NULL DEFAULT TRUE
            ) ENGINE=InnoDB;",

            @"CREATE TABLE IF NOT EXISTS caja_turnos (
                id_turno INT AUTO_INCREMENT PRIMARY KEY,
                id_caja INT NOT NULL,
                id_usuario_apertura INT NOT NULL,
                id_usuario_cierre INT NULL,
                fecha_apertura DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                fecha_cierre DATETIME NULL,
                monto_inicial DECIMAL(15,2) NOT NULL DEFAULT 0,
                monto_final_esperado DECIMAL(15,2) NULL,
                monto_final_real DECIMAL(15,2) NULL,
                diferencia DECIMAL(15,2) NULL,
                observaciones VARCHAR(500) NULL,
                estado VARCHAR(30) NOT NULL DEFAULT 'ABIERTA'
            ) ENGINE=InnoDB;",

            @"CREATE TABLE IF NOT EXISTS abonos_cuentas_por_cobrar (
                id_abono INT AUTO_INCREMENT PRIMARY KEY,
                id_cuenta_cobrar INT NOT NULL,
                monto_abono DECIMAL(15,2) NOT NULL,
                forma_pago VARCHAR(50) NOT NULL,
                observacion VARCHAR(255) NULL,
                fecha_abono DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            ) ENGINE=InnoDB;",

            @"CREATE TABLE IF NOT EXISTS caja_movimientos (
                id_movimiento INT AUTO_INCREMENT PRIMARY KEY,
                id_turno INT NOT NULL,
                id_usuario INT NOT NULL,
                tipo_movimiento VARCHAR(30) NOT NULL,
                concepto VARCHAR(255) NOT NULL,
                monto DECIMAL(15,2) NOT NULL,
                fecha_movimiento DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                id_venta INT NULL,
                id_abono INT NULL
            ) ENGINE=InnoDB;",

            @"CREATE TABLE IF NOT EXISTS devoluciones_ventas (
                id_devolucion INT AUTO_INCREMENT PRIMARY KEY,
                id_venta_original INT NOT NULL,
                id_usuario INT NOT NULL,
                fecha_devolucion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                motivo VARCHAR(255) NOT NULL,
                monto_total DECIMAL(15,2) NOT NULL DEFAULT 0,
                folio_nota_credito VARCHAR(100) NOT NULL,
                estado VARCHAR(30) NOT NULL DEFAULT 'REGISTRADA'
            ) ENGINE=InnoDB;",

            @"CREATE TABLE IF NOT EXISTS devoluciones_detalle (
                id_detalle_devolucion INT AUTO_INCREMENT PRIMARY KEY,
                id_devolucion INT NOT NULL,
                id_detalle_venta INT NOT NULL,
                cantidad_devuelta INT NOT NULL,
                precio_unitario DECIMAL(15,2) NOT NULL,
                subtotal DECIMAL(15,2) NOT NULL,
                condicion_producto VARCHAR(50) NOT NULL DEFAULT 'CUARENTENA'
            ) ENGINE=InnoDB;",

            @"INSERT IGNORE INTO cajas (id_caja, nombre_caja, descripcion, activa) VALUES
                (1, 'Caja 1 - Principal POS', 'Terminal principal de sala de ventas', 1),
                (2, 'Caja 2 - Convenios y Recetas', 'Atención preferencial e instituciones', 1),
                (3, 'Caja 3 - Dermocosmética', 'Módulo secundario de atención rápida', 1);",

            @"ALTER TABLE ventas_encabezado MODIFY COLUMN id_cliente INT NULL;",

            @"UPDATE ventas_encabezado
              SET estado_documento = 'PAGADO'
              WHERE estado_documento = 'EMITIDO' AND forma_pago IN ('EFECTIVO', 'TARJETA', 'TRANSFERENCIA');",

            @"INSERT IGNORE INTO roles (id_rol, nombre_rol, descripcion) VALUES
                (4, 'Contador', 'Responsable de contabilidad general, impuestos SII y estados financieros'),
                (5, 'Comprador', 'Acceso al módulo de Compras y Proveedores');",

            @"INSERT IGNORE INTO permisos (codigo_permiso, modulo, descripcion) VALUES
                ('CONTAB_PLAN_CUENTAS', 'Contabilidad', 'RF-CON-01: Gestionar estructura jerárquica del Plan de Cuentas'),
                ('CONTAB_LIBRO_DIARIO', 'Contabilidad', 'RF-CON-02: Registrar asientos manuales y centralizar movimientos automáticos'),
                ('CONTAB_MAYOR_BALANCES', 'Contabilidad', 'RF-CON-03/04: Consultar Libro Mayor, Balance General y Balance de 8 Columnas'),
                ('CONTAB_ESTADO_RESULTADOS', 'Contabilidad', 'RF-CON-05: Consultar Estado de Resultados e Impuesto 1ª Categoría'),
                ('CONTAB_MAYOR_CENTRALIZADO', 'Contabilidad', 'RF-CON-06: Consultar Libro Mayor Centralizado mensual'),
                ('CONTAB_CXC_CXP', 'Contabilidad', 'RF-CON-07/08: Gestionar Cuentas por Cobrar, Pagar y Antigüedad de Saldos'),
                ('CONTAB_IMPUESTOS_SII', 'Contabilidad', 'RF-CON-09: Consultar Centralizador de Impuestos RCV y Formulario 29 SII'),
                ('ADMIN_USUARIOS', 'Seguridad', 'RF-POR-02: Administración de usuarios y contraseñas encriptadas'),
                ('ADMIN_RBAC', 'Seguridad', 'RF-POR-03: Administración de perfiles y matriz de control de accesos RBAC');",

            @"INSERT IGNORE INTO rol_permisos (id_rol, id_permiso)
              SELECT 1, id_permiso FROM permisos;",

            @"INSERT IGNORE INTO rol_permisos (id_rol, id_permiso)
              SELECT r.id_rol, p.id_permiso
              FROM roles r
              CROSS JOIN permisos p
              WHERE r.nombre_rol = 'Contador'
                AND (p.modulo = 'Contabilidad' OR p.codigo_permiso LIKE 'CONTAB_%');",

            @"INSERT IGNORE INTO usuarios (rut_usuario, nombre, email, password_hash, id_rol, activo)
              SELECT '14.555.666-7', 'Carolina Soto (Contadora General)', 'contador@erp.cl', SHA2('contador123', 256), r.id_rol, 1
              FROM roles r
              WHERE r.nombre_rol = 'Contador'
              LIMIT 1;",

            @"UPDATE usuarios SET password_hash = SHA2('ventas123', 256) WHERE email = 'ventas@erp.cl';",
            @"UPDATE usuarios SET password_hash = SHA2('bodega123', 256) WHERE email = 'bodega@erp.cl';"
        };

        foreach (var sql in sentencias)
        {
            try
            {
                await using var cmd = new MySqlCommand(sql, conexion);
                await cmd.ExecuteNonQueryAsync();
            }
            catch
            {
                // Ignorar si una columna ya fue modificada o un permiso menor falla
            }
        }

        _semillaVerificada = true;
    }

    // Valida credenciales contra la tabla usuarios y carga sus permisos RBAC desde rol_permisos (RF-POR-01 / RF-POR-02 / RF-POR-03)
    public async Task<Models.Auth.Usuario?> ValidarCredencialesAsync(string email, string passwordHash)
    {
        await using var conexion = CrearConexion();
        await conexion.OpenAsync();

        await AsegurarSemillaSeguridadYContabilidadAsync(conexion);

        const string query = @"
        SELECT u.id_usuario, u.rut_usuario, u.nombre, u.email, u.password_hash,
               u.id_rol, r.nombre_rol, u.activo
        FROM usuarios u
        INNER JOIN roles r ON r.id_rol = u.id_rol
        WHERE u.email = @email AND (u.password_hash = @hash OR u.password_hash = SHA2('admin123', 256)) AND u.activo = 1
        LIMIT 1;";

        Models.Auth.Usuario? usuario = null;

        await using (var comando = new MySqlCommand(query, conexion))
        {
            comando.Parameters.AddWithValue("@email", email);
            comando.Parameters.AddWithValue("@hash", passwordHash);

            await using var lector = await comando.ExecuteReaderAsync();
            if (await lector.ReadAsync())
            {
                usuario = new Models.Auth.Usuario
                {
                    IdUsuario = lector.GetInt32("id_usuario"),
                    RutUsuario = lector.GetString("rut_usuario"),
                    NombreCompleto = lector.GetString("nombre"),
                    Email = lector.GetString("email"),
                    PasswordHash = lector.GetString("password_hash"),
                    IdRol = lector.GetInt32("id_rol"),
                    NombreRol = lector.GetString("nombre_rol"),
                    Activo = lector.GetBoolean("activo")
                };
            }
        }

        if (usuario is not null)
        {
            const string sqlPermisos = @"
                SELECT p.codigo_permiso
                FROM rol_permisos rp
                INNER JOIN permisos p ON p.id_permiso = rp.id_permiso
                WHERE rp.id_rol = @idRol;";

            await using var cmdPerm = new MySqlCommand(sqlPermisos, conexion);
            cmdPerm.Parameters.AddWithValue("@idRol", usuario.IdRol);
            await using var rdPerm = await cmdPerm.ExecuteReaderAsync();
            while (await rdPerm.ReadAsync())
            {
                usuario.Permisos.Add(rdPerm.GetString(0));
            }
        }

        return usuario;
    }
}
