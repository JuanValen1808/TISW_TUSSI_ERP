using MySqlConnector;

namespace TISW_TUSSI_ERP.Services.Api;

// Servicio central de acceso a datos. Todos los módulos (Ventas, Compras,
// Inventario, Contabilidad) reutilizan esta misma clase para conectarse
// a la base de datos MySQL levantada con docker-compose.
public class DatabaseService
{
    // En desarrollo local (emulador Android / Windows) usa "localhost".
    // Si usas el emulador de Android, cambia "localhost" por "10.0.2.2".
    private const string ConnectionString =
        "Server=localhost;Port=3307;Database=erp_farmaceutico;Uid=erp_admin;Pwd=admin_password;";

    public MySqlConnection CrearConexion() => new MySqlConnection(ConnectionString);

    // Valida credenciales contra la tabla usuarios (login por email, según el esquema real)
    // Valida credenciales contra la tabla usuarios (login por email, según el esquema real)
    public async Task<Models.Auth.Usuario?> ValidarCredencialesAsync(string email, string passwordHash)
    {
        using var conexion = CrearConexion();
        await conexion.OpenAsync();

        const string query = @"
        SELECT u.id_usuario, u.rut_usuario, u.nombre, u.email, u.password_hash,
               u.id_rol, r.nombre_rol, u.activo
        FROM usuarios u
        INNER JOIN roles r ON r.id_rol = u.id_rol
        WHERE u.email = @email AND u.password_hash = @hash AND u.activo = 1
        LIMIT 1;";

        using var comando = new MySqlCommand(query, conexion);
        comando.Parameters.AddWithValue("@email", email);
        comando.Parameters.AddWithValue("@hash", passwordHash); // <-- Recibe el hash directo del ViewModel

        using var lector = await comando.ExecuteReaderAsync();
        if (await lector.ReadAsync())
        {
            return new Models.Auth.Usuario
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
        return null;
    }
}
