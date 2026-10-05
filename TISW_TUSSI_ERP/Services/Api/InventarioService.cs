using System.Data.Common;
using MySqlConnector;
using TISW_TUSSI_ERP.Models.Inventario;

namespace TISW_TUSSI_ERP.Services.Api;

public class InventarioService
{
    private readonly DatabaseService _db;
    public InventarioService(DatabaseService db) => _db = db;

    public async Task<ResumenInventario> ObtenerResumenAsync()
    {
        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        var r = new ResumenInventario();

        const string sql1 = @"
            SELECT 
                COUNT(*),
                COALESCE(SUM(p.costo_promedio_ponderado * COALESCE(l.stock_total, 0)), 0)
            FROM productos p
            LEFT JOIN (
                SELECT id_producto, SUM(stock_actual) AS stock_total 
                FROM lotes 
                GROUP BY id_producto
            ) l ON l.id_producto = p.id_producto
            WHERE p.activo = 1";

        await using (var cmd = new MySqlCommand(sql1, cn))
        await using (var rd = await cmd.ExecuteReaderAsync())
        {
            if (await rd.ReadAsync())
            {
                r.TotalProductos = Ent(rd, 0);
                r.ValorizacionTotal = Dec(rd, 1);
            }
        }

        const string sql2 = @"
            SELECT COUNT(*) 
            FROM (
                SELECT p.id_producto, p.stock_minimo, COALESCE(SUM(l.stock_actual), 0) AS stock_actual
                FROM productos p
                LEFT JOIN lotes l ON l.id_producto = p.id_producto
                WHERE p.activo = 1
                GROUP BY p.id_producto, p.stock_minimo
                HAVING stock_actual <= p.stock_minimo
            ) t";

        await using (var cmd = new MySqlCommand(sql2, cn))
            r.ProductosStockBajo = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        const string sql3 = @"
            SELECT COUNT(*) 
            FROM lotes 
            WHERE stock_actual > 0 
              AND fecha_vencimiento <= DATE_ADD(CURDATE(), INTERVAL 30 DAY)";

        await using (var cmd = new MySqlCommand(sql3, cn))
            r.ProductosPorVencer = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        return r;
    }

    public async Task<List<ProductoModel>> ObtenerProductosAsync()
    {
        const string sql = @"
            SELECT 
                p.id_producto, p.sku, p.codigo_barras, p.nombre_comercial, 
                p.principio_activo, p.registro_sanitario, p.formato_presentacion,
                p.id_categoria, c.nombre_categoria, p.precio_venta_base, 
                p.costo_promedio_ponderado, p.stock_minimo, p.punto_reorden, p.activo,
                COALESCE(SUM(l.stock_actual), 0) AS stock_total
            FROM productos p
            INNER JOIN categorias_producto c ON c.id_categoria = p.id_categoria
            LEFT JOIN lotes l ON l.id_producto = p.id_producto
            WHERE p.activo = 1
            GROUP BY p.id_producto
            ORDER BY p.nombre_comercial ASC";

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, cn);
        await using var rd = await cmd.ExecuteReaderAsync();

        var lista = new List<ProductoModel>();
        while (await rd.ReadAsync())
        {
            lista.Add(new ProductoModel
            {
                IdProducto = rd.GetInt32(0),
                Sku = rd.GetString(1),
                CodigoBarras = rd.IsDBNull(2) ? "" : rd.GetString(2),
                NombreComercial = rd.GetString(3),
                PrincipioActivo = rd.IsDBNull(4) ? "" : rd.GetString(4),
                RegistroSanitario = rd.IsDBNull(5) ? "" : rd.GetString(5),
                FormatoPresentacion = rd.IsDBNull(6) ? "" : rd.GetString(6),
                IdCategoria = rd.GetInt32(7),
                CategoriaNombre = rd.GetString(8),
                PrecioVenta = Dec(rd, 9),
                CostoPromedio = Dec(rd, 10),
                StockMinimo = Ent(rd, 11),
                PuntoReorden = Ent(rd, 12),
                Activo = rd.GetBoolean(13),
                StockActual = Ent(rd, 14)
            });
        }
        return lista;
    }

    public async Task<List<LoteModel>> ObtenerLotesPorProductoAsync(int idProducto)
    {
        const string sql = @"
            SELECT l.id_lote, l.id_producto, p.sku, p.nombre_comercial, 
                   l.numero_lote, l.fecha_vencimiento, l.stock_actual
            FROM lotes l
            JOIN productos p ON p.id_producto = l.id_producto
            WHERE l.id_producto = @id AND l.stock_actual > 0
            ORDER BY l.fecha_vencimiento ASC";

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@id", idProducto);
        await using var rd = await cmd.ExecuteReaderAsync();

        var lista = new List<LoteModel>();
        while (await rd.ReadAsync())
        {
            lista.Add(new LoteModel
            {
                IdLote = rd.GetInt32(0),
                IdProducto = rd.GetInt32(1),
                Sku = rd.GetString(2),
                NombreProducto = rd.GetString(3),
                NumeroLote = rd.GetString(4),
                FechaVencimiento = Convert.ToDateTime(rd.GetValue(5)),
                StockActual = Ent(rd, 6)
            });
        }
        return lista;
    }

    public async Task<List<KardexMovimientoModel>> ObtenerKardexAsync(int? idProducto = null)
    {
        string sql = @"
            SELECT k.id_kardex, k.id_producto, p.nombre_comercial, p.sku, 
                   COALESCE(l.numero_lote, '—') AS numero_lote, k.tipo_movimiento, 
                   k.origen_documento, k.cantidad, k.costo_unitario, 
                   k.saldo_stock_producto, k.fecha_movimiento
            FROM kardex_movimiento k
            JOIN productos p ON p.id_producto = k.id_producto
            LEFT JOIN lotes l ON l.id_lote = k.id_lote";

        if (idProducto.HasValue)
            sql += " WHERE k.id_producto = @idProducto";

        sql += " ORDER BY k.fecha_movimiento DESC, k.id_kardex DESC LIMIT 100";

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, cn);
        if (idProducto.HasValue)
            cmd.Parameters.AddWithValue("@idProducto", idProducto.Value);

        await using var rd = await cmd.ExecuteReaderAsync();

        var lista = new List<KardexMovimientoModel>();
        while (await rd.ReadAsync())
        {
            lista.Add(new KardexMovimientoModel
            {
                IdKardex = rd.GetInt32(0),
                IdProducto = rd.GetInt32(1),
                NombreProducto = rd.GetString(2),
                Sku = rd.GetString(3),
                NumeroLote = rd.GetString(4),
                TipoMovimiento = rd.GetString(5),
                OrigenDocumento = rd.GetString(6),
                Cantidad = Ent(rd, 7),
                CostoUnitario = Dec(rd, 8),
                SaldoStockProducto = Ent(rd, 9),
                FechaMovimiento = Convert.ToDateTime(rd.GetValue(10))
            });
        }
        return lista;
    }

    private static decimal Dec(DbDataReader r, int i) => r.IsDBNull(i) ? 0m : Convert.ToDecimal(r.GetValue(i));
    private static int Ent(DbDataReader r, int i) => r.IsDBNull(i) ? 0 : Convert.ToInt32(r.GetValue(i));
}