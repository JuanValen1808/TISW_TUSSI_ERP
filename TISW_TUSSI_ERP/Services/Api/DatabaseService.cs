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

    // Garantiza que existan el usuario demo Contador (contador@erp.cl / contador123) y los permisos RBAC de Contabilidad y Portal
    private async Task AsegurarSemillaSeguridadYContabilidadAsync(MySqlConnection conexion)
    {
        if (_semillaVerificada) return;

        try
        {
            const string sqlSemilla = @"
                INSERT IGNORE INTO roles (id_rol, nombre_rol, descripcion) VALUES
                (5, 'Contador', 'Responsable de contabilidad general, impuestos SII y estados financieros');

                INSERT IGNORE INTO permisos (codigo_permiso, modulo, descripcion) VALUES
                ('CONTAB_PLAN_CUENTAS', 'Contabilidad', 'RF-CON-01: Gestionar estructura jerárquica del Plan de Cuentas'),
                ('CONTAB_LIBRO_DIARIO', 'Contabilidad', 'RF-CON-02: Registrar asientos manuales y centralizar movimientos automáticos'),
                ('CONTAB_MAYOR_BALANCES', 'Contabilidad', 'RF-CON-03/04: Consultar Libro Mayor, Balance General y Balance de 8 Columnas'),
                ('CONTAB_ESTADO_RESULTADOS', 'Contabilidad', 'RF-CON-05: Consultar Estado de Resultados e Impuesto 1ª Categoría'),
                ('CONTAB_MAYOR_CENTRALIZADO', 'Contabilidad', 'RF-CON-06: Consultar Libro Mayor Centralizado mensual'),
                ('CONTAB_CXC_CXP', 'Contabilidad', 'RF-CON-07/08: Gestionar Cuentas por Cobrar, Pagar y Antigüedad de Saldos'),
                ('CONTAB_IMPUESTOS_SII', 'Contabilidad', 'RF-CON-09: Consultar Centralizador de Impuestos RCV y Formulario 29 SII'),
                ('ADMIN_USUARIOS', 'Seguridad', 'RF-POR-02: Administración de usuarios y contraseñas encriptadas'),
                ('ADMIN_RBAC', 'Seguridad', 'RF-POR-03: Administración de perfiles y matriz de control de accesos RBAC');

                INSERT IGNORE INTO rol_permisos (id_rol, id_permiso)
                SELECT 1, id_permiso FROM permisos;

                INSERT IGNORE INTO rol_permisos (id_rol, id_permiso)
                SELECT r.id_rol, p.id_permiso
                FROM roles r
                CROSS JOIN permisos p
                WHERE r.nombre_rol = 'Contador'
                  AND (p.modulo = 'Contabilidad' OR p.codigo_permiso LIKE 'CONTAB_%');

                INSERT IGNORE INTO usuarios (rut_usuario, nombre, email, password_hash, id_rol, activo)
                SELECT '14.555.666-7', 'Carolina Soto (Contadora General)', 'contador@erp.cl', SHA2('contador123', 256), r.id_rol, 1
                FROM roles r
                WHERE r.nombre_rol = 'Contador'
                LIMIT 1;";

            await using var cmd = new MySqlCommand(sqlSemilla, conexion);
            await cmd.ExecuteNonQueryAsync();
            _semillaVerificada = true;
        }
        catch
        {
            // Si la BD está en modo solo lectura o falla un permiso menor, no interrumpir el login
        }
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
        WHERE u.email = @email AND u.password_hash = @hash AND u.activo = 1
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
