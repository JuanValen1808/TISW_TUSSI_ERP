
using MySqlConnector;
using TISW_TUSSI_ERP.Models.Ventas;

namespace TISW_TUSSI_ERP.Services.Api;

public class ConveniosService
{
    private readonly DatabaseService _database;

    public ConveniosService(DatabaseService database)
    {
        _database = database;
    }

    // Obtener los convenios de un cliente
    public async Task<List<ConvenioSalud>> ObtenerPorClienteAsync(
        int idCliente)
    {
        var convenios = new List<ConvenioSalud>();

        await using var conexion = _database.CrearConexion();
        await conexion.OpenAsync();

        const string sql = """
            SELECT
                id_convenio,
                id_cliente,
                nombre_convenio,
                porcentaje_descuento,
                limite_credito,
                dias_credito_morosidad,
                activo
            FROM convenios_salud
            WHERE id_cliente = @id_cliente
            ORDER BY nombre_convenio;
            """;

        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@id_cliente", idCliente);

        await using var lector = await comando.ExecuteReaderAsync();

        while (await lector.ReadAsync())
        {
            convenios.Add(MapearConvenio(lector));
        }

        return convenios;
    }

    // Registrar convenio
    public async Task CrearConvenioAsync(ConvenioSalud convenio)
    {
        ValidarConvenio(convenio);

        await using var conexion = _database.CrearConexion();
        await conexion.OpenAsync();

        const string sql = """
            INSERT INTO convenios_salud
            (
                id_cliente,
                nombre_convenio,
                porcentaje_descuento,
                limite_credito,
                dias_credito_morosidad,
                activo
            )
            VALUES
            (
                @id_cliente,
                @nombre_convenio,
                @porcentaje_descuento,
                @limite_credito,
                @dias_credito_morosidad,
                @activo
            );
            """;

        await using var comando = new MySqlCommand(sql, conexion);

        AgregarParametros(comando, convenio);

        await comando.ExecuteNonQueryAsync();
    }

    // Actualizar convenio existente
    public async Task ActualizarConvenioAsync(ConvenioSalud convenio)
    {
        ValidarConvenio(convenio);

        if (convenio.IdConvenio <= 0)
            throw new ArgumentException("El convenio no es válido.");

        await using var conexion = _database.CrearConexion();
        await conexion.OpenAsync();

        const string sql = """
            UPDATE convenios_salud
            SET
                nombre_convenio = @nombre_convenio,
                porcentaje_descuento = @porcentaje_descuento,
                limite_credito = @limite_credito,
                dias_credito_morosidad = @dias_credito_morosidad,
                activo = @activo
            WHERE id_convenio = @id_convenio
              AND id_cliente = @id_cliente;
            """;

        await using var comando = new MySqlCommand(sql, conexion);

        AgregarParametros(comando, convenio);
        comando.Parameters.AddWithValue(
            "@id_convenio", convenio.IdConvenio);

        int filas = await comando.ExecuteNonQueryAsync();

        if (filas == 0)
            throw new InvalidOperationException(
                "No se encontró el convenio para actualizar.");
    }

    // Eliminar convenio
    public async Task EliminarConvenioAsync(
        int idConvenio, int idCliente)
    {
        await using var conexion = _database.CrearConexion();
        await conexion.OpenAsync();

        const string sql = """
            DELETE FROM convenios_salud
            WHERE id_convenio = @id_convenio
              AND id_cliente = @id_cliente;
            """;

        await using var comando = new MySqlCommand(sql, conexion);

        comando.Parameters.AddWithValue(
            "@id_convenio", idConvenio);

        comando.Parameters.AddWithValue(
            "@id_cliente", idCliente);

        int filas = await comando.ExecuteNonQueryAsync();

        if (filas == 0)
            throw new InvalidOperationException(
                "No se encontró el convenio para eliminar.");
    }

    // Parámetros compartidos para INSERT y UPDATE
    private static void AgregarParametros(
        MySqlCommand comando,
        ConvenioSalud convenio)
    {
        comando.Parameters.AddWithValue(
            "@id_cliente", convenio.IdCliente);

        comando.Parameters.AddWithValue(
            "@nombre_convenio", convenio.NombreConvenio.Trim());

        comando.Parameters.AddWithValue(
            "@porcentaje_descuento", convenio.PorcentajeDescuento);

        comando.Parameters.AddWithValue(
            "@limite_credito", convenio.LimiteCredito);

        comando.Parameters.AddWithValue(
            "@dias_credito_morosidad",
            convenio.DiasCreditoMorosidad);

        comando.Parameters.AddWithValue(
            "@activo", convenio.Activo);
    }

    // Convertir una fila de MySQL a ConvenioSalud
    private static ConvenioSalud MapearConvenio(
        MySqlDataReader lector)
    {
        return new ConvenioSalud
        {
            IdConvenio = lector.GetInt32("id_convenio"),
            IdCliente = lector.GetInt32("id_cliente"),
            NombreConvenio = lector.GetString("nombre_convenio"),
            PorcentajeDescuento =
                lector.GetDecimal("porcentaje_descuento"),
            LimiteCredito = lector.GetDecimal("limite_credito"),
            DiasCreditoMorosidad =
                lector.GetInt32("dias_credito_morosidad"),
            Activo = lector.GetBoolean("activo")
        };
    }

    // Validaciones básicas
    private static void ValidarConvenio(ConvenioSalud convenio)
    {
        if (convenio.IdCliente <= 0)
            throw new ArgumentException(
                "Debes seleccionar un cliente válido.");

        if (string.IsNullOrWhiteSpace(convenio.NombreConvenio))
            throw new ArgumentException(
                "El nombre del convenio es obligatorio.");

        if (convenio.PorcentajeDescuento < 0 ||
            convenio.PorcentajeDescuento > 100)
            throw new ArgumentException(
                "El descuento debe estar entre 0 y 100%.");

        if (convenio.LimiteCredito < 0)
            throw new ArgumentException(
                "El límite de crédito no puede ser negativo.");

        if (convenio.DiasCreditoMorosidad < 0)
            throw new ArgumentException(
                "Los días de crédito no pueden ser negativos.");
    }
}
