using System.Data.Common;
using MySqlConnector;
using TISW_TUSSI_ERP.Models.Ventas;

namespace TISW_TUSSI_ERP.Services.Api;

public class VentasService
{
    private readonly DatabaseService _db;

    public VentasService(DatabaseService db) => _db = db;

    // =========================
    // PRODUCTOS
    // =========================
    public async Task<ProductoPos?> BuscarProductoAsync(string codigo)
    {
        const string sql = @"
            SELECT id_producto, sku, COALESCE(codigo_barras, ''), nombre_comercial,
                   COALESCE(principio_activo, ''), precio_venta_base
            FROM productos
            WHERE activo = 1
              AND (sku = @codigo OR codigo_barras = @codigo)
            LIMIT 1;";

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        await using var cmd = new MySqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@codigo", codigo.Trim());

        await using var rd = await cmd.ExecuteReaderAsync();

        if (!await rd.ReadAsync())
            return null;

        return new ProductoPos
        {
            IdProducto = rd.GetInt32(0),
            Sku = rd.IsDBNull(1) ? "" : rd.GetString(1),
            CodigoBarras = rd.IsDBNull(2) ? "" : rd.GetString(2),
            NombreComercial = rd.IsDBNull(3) ? "" : rd.GetString(3),
            PrincipioActivo = rd.IsDBNull(4) ? "" : rd.GetString(4),
            PrecioVenta = Dec(rd, 5)
        };
    }

    // =========================
    // LOTES
    // =========================
    public async Task<List<LotePos>> ObtenerLotesAsync(int idProducto)
    {
        const string sql = @"
            SELECT id_lote, numero_lote, fecha_vencimiento, stock_actual
            FROM lotes
            WHERE id_producto = @idProducto
              AND stock_actual > 0
            ORDER BY fecha_vencimiento ASC, id_lote ASC;";

        var resultado = new List<LotePos>();

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        await using var cmd = new MySqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@idProducto", idProducto);

        await using var rd = await cmd.ExecuteReaderAsync();

        while (await rd.ReadAsync())
        {
            resultado.Add(new LotePos
            {
                IdLote = rd.GetInt32(0),
                NumeroLote = rd.GetString(1),
                FechaVencimiento = rd.GetDateTime(2),
                StockActual = rd.GetInt32(3)
            });
        }

        return resultado;
    }

    // =========================
    // CLIENTES
    // =========================
    public async Task<List<ClientePos>> ObtenerClientesAsync()
    {
        const string sql = @"
            SELECT id_cliente, rut_cliente, razon_social_nombre, tipo_cliente
            FROM clientes
            ORDER BY razon_social_nombre;";

        var resultado = new List<ClientePos>();

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        await using var cmd = new MySqlCommand(sql, cn);
        await using var rd = await cmd.ExecuteReaderAsync();

        while (await rd.ReadAsync())
        {
            resultado.Add(new ClientePos
            {
                IdCliente = rd.GetInt32(0),
                RutCliente = rd.GetString(1),
                Nombre = rd.GetString(2),
                TipoCliente = rd.IsDBNull(3) ? "" : rd.GetString(3)
            });
        }

        return resultado;
    }

    // =========================
    // CONVENIOS
    // =========================
    public async Task<List<ConvenioPos>> ObtenerConveniosAsync(int idCliente)
    {
        const string sql = @"
            SELECT id_convenio, nombre_convenio,
                   porcentaje_descuento, dias_credito_morosidad
            FROM convenios_salud
            WHERE id_cliente = @idCliente
              AND activo = 1
            ORDER BY nombre_convenio;";

        var resultado = new List<ConvenioPos>();

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        await using var cmd = new MySqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@idCliente", idCliente);

        await using var rd = await cmd.ExecuteReaderAsync();

        while (await rd.ReadAsync())
        {
            resultado.Add(new ConvenioPos
            {
                IdConvenio = rd.GetInt32(0),
                NombreConvenio = rd.GetString(1),
                PorcentajeDescuento = Dec(rd, 2),
                DiasCredito = rd.GetInt32(3)
            });
        }

        return resultado;
    }

    // =========================
    // EMITIR VENTA
    // =========================
    public async Task<string> EmitirVentaAsync(
        int idCliente,
        int? idConvenio,
        string formaPago,
        List<ItemCarrito> items,
        decimal subtotal,
        decimal descuento,
        decimal neto,
        decimal iva,
        decimal total,
        int idCajero)
    {
        if (items.Count == 0)
            throw new InvalidOperationException("El carrito está vacío.");

        if (formaPago == "CREDITO" && idConvenio is null)
            throw new InvalidOperationException(
                "El pago a crédito requiere un convenio seleccionado.");

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        await using var tx = await cn.BeginTransactionAsync();

        try
        {
            // Tipo de cliente
            const string sqlCliente = @"
                SELECT tipo_cliente
                FROM clientes
                WHERE id_cliente = @id
                LIMIT 1;";

            string tipoCliente;

            await using (var cmd = new MySqlCommand(sqlCliente, cn, tx))
            {
                cmd.Parameters.AddWithValue("@id", idCliente);

                tipoCliente =
                    Convert.ToString(await cmd.ExecuteScalarAsync())
                    ?? "PERSONA";
            }

            var tipoDocumento =
                tipoCliente.Equals("PERSONA", StringComparison.OrdinalIgnoreCase)
                    ? "BOLETA"
                    : "FACTURA";

            var folio =
                $"V-{DateTime.Now:yyyyMMddHHmmss}-" +
                $"{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

            var estado =
                formaPago == "CREDITO"
                    ? "PENDIENTE"
                    : "PAGADO";

            // Encabezado de la venta
            const string sqlVenta = @"
                INSERT INTO ventas_encabezado
                    (folio_documento, tipo_documento, id_cliente, id_convenio,
                     estado_documento, forma_pago, monto_neto, monto_iva,
                     monto_descuento, monto_total, id_usuario_cajero)
                VALUES
                    (@folio, @tipo, @cliente, @convenio,
                     @estado, @pago, @neto, @iva,
                     @descuento, @total, @cajero);";

            long idVenta;

            await using (var cmd = new MySqlCommand(sqlVenta, cn, tx))
            {
                cmd.Parameters.AddWithValue("@folio", folio);
                cmd.Parameters.AddWithValue("@tipo", tipoDocumento);
                cmd.Parameters.AddWithValue("@cliente", idCliente);
                cmd.Parameters.AddWithValue(
                    "@convenio",
                    (object?)idConvenio ?? DBNull.Value);

                cmd.Parameters.AddWithValue("@estado", estado);
                cmd.Parameters.AddWithValue("@pago", formaPago);
                cmd.Parameters.AddWithValue("@neto", neto);
                cmd.Parameters.AddWithValue("@iva", iva);
                cmd.Parameters.AddWithValue("@descuento", descuento);
                cmd.Parameters.AddWithValue("@total", total);
                cmd.Parameters.AddWithValue("@cajero", idCajero);

                await cmd.ExecuteNonQueryAsync();
                idVenta = cmd.LastInsertedId;
            }

            // Detalle y stock
            foreach (var item in items)
            {
                const string sqlStock = @"
                    UPDATE lotes
                    SET stock_actual = stock_actual - @cantidad
                    WHERE id_lote = @lote
                      AND id_producto = @producto
                      AND stock_actual >= @cantidad
                      AND fecha_vencimiento >= CURDATE();";

                await using (var cmd = new MySqlCommand(sqlStock, cn, tx))
                {
                    cmd.Parameters.AddWithValue("@cantidad", item.Cantidad);
                    cmd.Parameters.AddWithValue("@lote", item.IdLote);
                    cmd.Parameters.AddWithValue("@producto", item.IdProducto);

                    if (await cmd.ExecuteNonQueryAsync() != 1)
                    {
                        throw new InvalidOperationException(
                            $"Stock insuficiente o lote vencido para " +
                            $"{item.NombreProducto} ({item.NumeroLote}).");
                    }
                }

                const string sqlDetalle = @"
                    INSERT INTO ventas_detalle
                        (id_venta, id_producto, id_lote,
                         cantidad, precio_unitario, subtotal)
                    VALUES
                        (@venta, @producto, @lote,
                         @cantidad, @precio, @subtotal);";

                await using var cmdDetalle =
                    new MySqlCommand(sqlDetalle, cn, tx);

                cmdDetalle.Parameters.AddWithValue("@venta", idVenta);
                cmdDetalle.Parameters.AddWithValue("@producto", item.IdProducto);
                cmdDetalle.Parameters.AddWithValue("@lote", item.IdLote);
                cmdDetalle.Parameters.AddWithValue("@cantidad", item.Cantidad);
                cmdDetalle.Parameters.AddWithValue("@precio", item.PrecioUnitario);
                cmdDetalle.Parameters.AddWithValue("@subtotal", item.Subtotal);

                await cmdDetalle.ExecuteNonQueryAsync();
            }

            // Cuenta por cobrar si es venta a crédito
            if (formaPago == "CREDITO")
            {
                const string sqlCxc = @"
                    INSERT INTO cuentas_por_cobrar
                        (id_venta, id_cliente, id_convenio,
                         monto_pendiente, fecha_vencimiento_pago,
                         dias_morosidad, estado_cobro)
                    SELECT
                        @venta, @cliente, @convenio, @total,
                        DATE_ADD(
                            CURDATE(),
                            INTERVAL dias_credito_morosidad DAY
                        ),
                        0,
                        'PENDIENTE'
                    FROM convenios_salud
                    WHERE id_convenio = @convenio
                      AND id_cliente = @cliente
                      AND activo = 1;";

                await using var cmd =
                    new MySqlCommand(sqlCxc, cn, tx);

                cmd.Parameters.AddWithValue("@venta", idVenta);
                cmd.Parameters.AddWithValue("@cliente", idCliente);
                cmd.Parameters.AddWithValue("@convenio", idConvenio!.Value);
                cmd.Parameters.AddWithValue("@total", total);

                if (await cmd.ExecuteNonQueryAsync() != 1)
                {
                    throw new InvalidOperationException(
                        "El convenio seleccionado no es válido para este cliente.");
                }
            }

            await tx.CommitAsync();

            return folio;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // =========================
    // RANKING DE VENTAS
    // =========================
    public async Task<(List<FilaRanking> Filas, decimal TotalGeneral)>
        ObtenerRankingAsync()
    {
        const string sql = @"
            SELECT c.rut_cliente,
                   c.razon_social_nombre,
                   c.tipo_cliente,
                   COALESCE(cs.nombre_convenio, 'Sin convenio') AS convenio,
                   COUNT(v.id_venta) AS documentos,
                   COALESCE(SUM(v.monto_total), 0) AS total
            FROM ventas_encabezado v
            JOIN clientes c
                ON c.id_cliente = v.id_cliente
            LEFT JOIN convenios_salud cs
                ON cs.id_convenio = v.id_convenio
            WHERE v.estado_documento <> 'ANULADO'
            GROUP BY c.id_cliente,
                     c.rut_cliente,
                     c.razon_social_nombre,
                     c.tipo_cliente,
                     cs.id_convenio,
                     cs.nombre_convenio
            ORDER BY total DESC;";

        var filas = new List<FilaRanking>();
        decimal totalGeneral = 0;

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        await using var cmd = new MySqlCommand(sql, cn);
        await using var rd = await cmd.ExecuteReaderAsync();

        while (await rd.ReadAsync())
        {
            var fila = new FilaRanking
            {
                RutCliente = rd.GetString(0),
                NombreCliente = rd.GetString(1),
                TipoCliente = rd.IsDBNull(2) ? "" : rd.GetString(2),
                NombreConvenio = rd.GetString(3),
                Documentos = Convert.ToInt32(rd.GetValue(4)),
                TotalVentas = Dec(rd, 5)
            };

            totalGeneral += fila.TotalVentas;
            filas.Add(fila);
        }

        foreach (var fila in filas)
        {
            fila.Porcentaje =
                totalGeneral > 0
                    ? fila.TotalVentas * 100m / totalGeneral
                    : 0m;
        }

        return (filas, totalGeneral);
    }

    private static decimal Dec(DbDataReader r, int i) =>
        r.IsDBNull(i)
            ? 0m
            : Convert.ToDecimal(r.GetValue(i));
}