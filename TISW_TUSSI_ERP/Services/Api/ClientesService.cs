
using MySqlConnector;
using TISW_TUSSI_ERP.Models.Ventas;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.Services.Api;

public class ClientesService
{
    private readonly DatabaseService _database;

    public ClientesService(DatabaseService database)
    {
        _database = database;
    }

    // RF-VEN-01: Listar todos los clientes
    public async Task<List<Cliente>> ObtenerClientesAsync()
    {
        var clientes = new List<Cliente>();

        using var conexion = _database.CrearConexion();
        await conexion.OpenAsync();

        const string sql = @"
            SELECT id_cliente, rut_cliente,
                   razon_social_nombre, email,
                   telefono, tipo_cliente
            FROM clientes
            ORDER BY razon_social_nombre;";

        using var comando = new MySqlCommand(sql, conexion);
        using var lector = await comando.ExecuteReaderAsync();

        while (await lector.ReadAsync())
        {
            clientes.Add(MapearCliente(lector));
        }

        return clientes;
    }

    // Buscar clientes por RUT o nombre
    public async Task<List<Cliente>> BuscarClientesAsync(string texto)
    {
        var clientes = new List<Cliente>();

        using var conexion = _database.CrearConexion();
        await conexion.OpenAsync();

        const string sql = @"
            SELECT id_cliente, rut_cliente,
                   razon_social_nombre, email,
                   telefono, tipo_cliente
            FROM clientes
            WHERE rut_cliente LIKE @busqueda
               OR razon_social_nombre LIKE @busqueda
            ORDER BY razon_social_nombre;";

        using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue(
            "@busqueda", $"%{texto}%");

        using var lector = await comando.ExecuteReaderAsync();

        while (await lector.ReadAsync())
        {
            clientes.Add(MapearCliente(lector));
        }

        return clientes;
    }

    // Registrar un nuevo cliente
    public async Task<int> CrearClienteAsync(Cliente cliente)
    {
        ValidarCliente(cliente);

        using var conexion = _database.CrearConexion();
        await conexion.OpenAsync();

        const string sql = @"
            INSERT INTO clientes
            (rut_cliente, razon_social_nombre,
             email, telefono, tipo_cliente)
            VALUES
            (@rut, @nombre, @email, @telefono, @tipo);";

        using var comando = new MySqlCommand(sql, conexion);

        AgregarParametros(comando, cliente);

        await comando.ExecuteNonQueryAsync();

        return Convert.ToInt32(comando.LastInsertedId);
    }

    // Actualizar datos de un cliente existente
    public async Task<bool> ActualizarClienteAsync(Cliente cliente)
    {
        ValidarCliente(cliente);

        if (cliente.IdCliente <= 0)
            throw new ArgumentException(
                "El identificador del cliente no es válido.");

        using var conexion = _database.CrearConexion();
        await conexion.OpenAsync();

        const string sql = @"
            UPDATE clientes
            SET rut_cliente = @rut,
                razon_social_nombre = @nombre,
                email = @email,
                telefono = @telefono,
                tipo_cliente = @tipo
            WHERE id_cliente = @id;";

        using var comando = new MySqlCommand(sql, conexion);

        AgregarParametros(comando, cliente);
        comando.Parameters.AddWithValue(
            "@id", cliente.IdCliente);

        return await comando.ExecuteNonQueryAsync() > 0;
    }

    // Eliminar un cliente sin registros dependientes
    public async Task<bool> EliminarClienteAsync(int idCliente)
    {
        using var conexion = _database.CrearConexion();
        await conexion.OpenAsync();

        const string sql = @"
            DELETE FROM clientes
            WHERE id_cliente = @id;";

        using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@id", idCliente);

        // MySQL impedirá eliminar clientes referenciados
        // por convenios, ventas u otras tablas con FK.
        return await comando.ExecuteNonQueryAsync() > 0;
    }

    // Convertir un registro MySQL en un objeto Cliente
    private static Cliente MapearCliente(MySqlDataReader lector)
    {
        return new Cliente
        {
            IdCliente = lector.GetInt32("id_cliente"),
            RutCliente = lector.GetString("rut_cliente"),
            RazonSocialNombre =
                lector.GetString("razon_social_nombre"),
            Email = lector.IsDBNull(
                lector.GetOrdinal("email"))
                ? null : lector.GetString("email"),
            Telefono = lector.IsDBNull(
                lector.GetOrdinal("telefono"))
                ? null : lector.GetString("telefono"),
            TipoCliente = lector.GetString("tipo_cliente")
        };
    }

    // Parámetros reutilizados en INSERT y UPDATE
    private static void AgregarParametros(
        MySqlCommand comando, Cliente cliente)
    {
        comando.Parameters.AddWithValue(
            "@rut", cliente.RutCliente.Trim());

        comando.Parameters.AddWithValue(
            "@nombre", cliente.RazonSocialNombre.Trim());

        comando.Parameters.AddWithValue(
            "@email", string.IsNullOrWhiteSpace(cliente.Email)
                ? DBNull.Value : cliente.Email.Trim());

        comando.Parameters.AddWithValue(
            "@telefono", string.IsNullOrWhiteSpace(cliente.Telefono)
                ? DBNull.Value : cliente.Telefono.Trim());

        comando.Parameters.AddWithValue(
            "@tipo", cliente.TipoCliente);
    }

    // Validaciones básicas antes de guardar
    private static void ValidarCliente(Cliente cliente)
    {
        if (string.IsNullOrWhiteSpace(cliente.RutCliente))
            throw new ArgumentException(
                "El RUT es obligatorio.");

        if (string.IsNullOrWhiteSpace(cliente.RazonSocialNombre))
            throw new ArgumentException(
                "El nombre o razón social es obligatorio.");

        if (cliente.RutCliente.Trim().Length > 20 ||
            cliente.RazonSocialNombre.Trim().Length > 200 ||
            cliente.Email?.Trim().Length > 150 ||
            cliente.Telefono?.Trim().Length > 30)
            throw new ArgumentException(
                "Uno o más campos exceden su longitud permitida.");

        if (cliente.TipoCliente != "PERSONA" &&
            cliente.TipoCliente != "EMPRESA" &&
            cliente.TipoCliente != "INSTITUCION")
            throw new ArgumentException(
                "El tipo de cliente no es válido.");
    }
}
