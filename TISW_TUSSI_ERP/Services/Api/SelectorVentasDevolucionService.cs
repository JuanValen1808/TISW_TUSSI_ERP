
using MySqlConnector;
using TISW_TUSSI_ERP.Models.Ventas;

namespace TISW_TUSSI_ERP.Services.Api;

public class SelectorVentasDevolucionService
{
    private readonly DatabaseService _db;

    public SelectorVentasDevolucionService(DatabaseService db)
    {
        _db = db;
    }

    public async Task<List<VentaParaDevolucion>>
        ListarVentasDisponiblesAsync(string? filtro = null)
    {
        var ventas = new List<VentaParaDevolucion>();

        const string sql = @"
            SELECT
                v.id_venta,
                v.folio_documento,
                v.fecha_hora_emision,
                COALESCE(c.razon_social_nombre,
                         'Público general') AS cliente,
                COALESCE(c.rut_cliente, '') AS rut,
                v.monto_total
            FROM ventas_encabezado v
            LEFT JOIN clientes c
                ON c.id_cliente = v.id_cliente
            WHERE v.estado_documento = 'PAGADO'
              AND v.tipo_documento IN ('BOLETA', 'FACTURA')
              AND (
                  @filtro = ''
                  OR v.folio_documento LIKE @busqueda
                  OR c.razon_social_nombre LIKE @busqueda
                  OR c.rut_cliente LIKE @busqueda
              )
              AND EXISTS (
                  SELECT 1
                  FROM ventas_detalle vd
                  WHERE vd.id_venta = v.id_venta
                    AND vd.cantidad > COALESCE((
                        SELECT SUM(dd.cantidad_devuelta)
                        FROM devoluciones_detalle dd
                        INNER JOIN devoluciones_ventas dv
                            ON dv.id_devolucion = dd.id_devolucion
                        WHERE dd.id_detalle_venta =
                              vd.id_detalle_venta
                          AND dv.estado = 'REGISTRADA'
                    ), 0)
              )
            ORDER BY v.fecha_hora_emision DESC,
                     v.id_venta DESC
            LIMIT 50;";

        await using var conexion = _db.CrearConexion();
        await conexion.OpenAsync();

        await using var comando =
            new MySqlCommand(sql, conexion);

        string texto = filtro?.Trim() ?? "";

        comando.Parameters.AddWithValue("@filtro", texto);
        comando.Parameters.AddWithValue(
            "@busqueda", $"%{texto}%");

        await using var lector =
            await comando.ExecuteReaderAsync();

        while (await lector.ReadAsync())
        {
            ventas.Add(new VentaParaDevolucion
            {
                IdVenta = lector.GetInt32(0),
                Folio = lector.GetString(1),
                Fecha = lector.GetDateTime(2),
                Cliente = lector.GetString(3),
                Rut = lector.GetString(4),
                Total = lector.GetDecimal(5)
            });
        }

        return ventas;
    }
}
