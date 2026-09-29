using System.Data.Common;
using MySqlConnector;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Dashboard;

namespace TISW_TUSSI_ERP.Services.Api;

// Lee de MySQL todo lo que muestra el Dashboard.
public class DashboardService
{
    private readonly DatabaseService _db;
    public DashboardService(DatabaseService db) => _db = db;

    private const string VentasValidas = "estado_documento <> 'ANULADO' AND tipo_documento <> 'NOTA_CREDITO'";

    public async Task<DashboardData> ObtenerAsync()
    {
        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        var datos = new DashboardData();
        datos.Hoy = Convert.ToDateTime(await EscalarAsync(cn, "SELECT CURDATE()"));

        await CargarResumenAsync(cn, datos);
        await CargarSemanaAsync(cn, datos);
        await CargarMediosPagoAsync(cn, datos);
        await CargarTopProductosAsync(cn, datos);
        await CargarAlertasAsync(cn, datos);
        return datos;
    }

    private static async Task CargarResumenAsync(MySqlConnection cn, DashboardData datos)
    {
        var r = datos.Resumen;

        var sqlVentas = $@"
            SELECT
              COALESCE(SUM(CASE WHEN DATE(fecha_hora_emision) = CURDATE() THEN monto_total END), 0),
              COALESCE(SUM(CASE WHEN DATE(fecha_hora_emision) = CURDATE() - INTERVAL 1 DAY THEN monto_total END), 0),
              COUNT(CASE WHEN DATE(fecha_hora_emision) = CURDATE() THEN 1 END),
              COALESCE(SUM(CASE WHEN DATE(fecha_hora_emision) = CURDATE() AND forma_pago = 'EFECTIVO' THEN monto_total END), 0)
            FROM ventas_encabezado
            WHERE {VentasValidas} AND fecha_hora_emision >= CURDATE() - INTERVAL 1 DAY";
        await using (var cmd = new MySqlCommand(sqlVentas, cn))
        await using (var rd = await cmd.ExecuteReaderAsync())
        {
            if (await rd.ReadAsync())
            {
                r.IngresosHoy = Dec(rd, 0);
                r.IngresosAyer = Dec(rd, 1);
                r.DocumentosHoy = Ent(rd, 2);
                r.EfectivoHoy = Dec(rd, 3);
            }
        }

        const string sqlCxc = @"
            SELECT COALESCE(SUM(monto_pendiente), 0), COUNT(*),
              COUNT(CASE WHEN estado_cobro = 'VENCIDA' OR fecha_vencimiento_pago < CURDATE() THEN 1 END),
              COALESCE(SUM(CASE WHEN estado_cobro = 'VENCIDA' OR fecha_vencimiento_pago < CURDATE() THEN monto_pendiente END), 0)
            FROM cuentas_por_cobrar
            WHERE estado_cobro IN ('PENDIENTE', 'VENCIDA')";
        await using (var cmd = new MySqlCommand(sqlCxc, cn))
        await using (var rd = await cmd.ExecuteReaderAsync())
        {
            if (await rd.ReadAsync())
            {
                r.PorCobrarMonto = Dec(rd, 0);
                r.PorCobrarDocumentos = Ent(rd, 1);
                r.VencidasDocumentos = Ent(rd, 2);
                r.VencidasMonto = Dec(rd, 3);
            }
        }
    }

    private static async Task CargarSemanaAsync(MySqlConnection cn, DashboardData datos)
    {
        var sql = $@"
            SELECT DATE(fecha_hora_emision), SUM(monto_total)
            FROM ventas_encabezado
            WHERE {VentasValidas} AND fecha_hora_emision >= CURDATE() - INTERVAL 6 DAY
            GROUP BY DATE(fecha_hora_emision)";

        var porDia = new Dictionary<DateTime, decimal>();
        await using (var cmd = new MySqlCommand(sql, cn))
        await using (var rd = await cmd.ExecuteReaderAsync())
        {
            while (await rd.ReadAsync())
                porDia[Convert.ToDateTime(rd.GetValue(0)).Date] = Dec(rd, 1);
        }

        var montos = Enumerable.Range(0, 7)
            .Select(i => datos.Hoy.Date.AddDays(i - 6))
            .Select(d => (Dia: d, Monto: porDia.TryGetValue(d, out var m) ? m : 0m))
            .ToList();

        var max = montos.Max(x => x.Monto);
        foreach (var (dia, monto) in montos)
        {
            datos.Barras.Add(new BarraVenta
            {
                Etiqueta = Formato.Capitalizar(dia.ToString("ddd", Formato.Cultura).TrimEnd('.')),
                Monto = monto,
                Altura = max > 0 ? Math.Max((double)(monto / max) * 170, 4) : 4,
                EsHoy = dia == datos.Hoy.Date
            });
        }
    }

    private static async Task CargarMediosPagoAsync(MySqlConnection cn, DashboardData datos)
    {
        var sql = $@"
            SELECT forma_pago, SUM(monto_total)
            FROM ventas_encabezado
            WHERE {VentasValidas} AND DATE(fecha_hora_emision) = CURDATE()
            GROUP BY forma_pago
            ORDER BY 2 DESC";

        var filas = new List<(string Pago, decimal Monto)>();
        await using (var cmd = new MySqlCommand(sql, cn))
        await using (var rd = await cmd.ExecuteReaderAsync())
        {
            while (await rd.ReadAsync())
                filas.Add((rd.GetString(0), Dec(rd, 1)));
        }

        var total = filas.Sum(f => f.Monto);
        foreach (var (pago, monto) in filas)
        {
            var (nombre, color) = pago switch
            {
                "EFECTIVO" => ("Efectivo", Color.FromArgb("#1D6FD8")),
                "TARJETA" => ("Tarjeta", Color.FromArgb("#16A34A")),
                "TRANSFERENCIA" => ("Transferencia", Color.FromArgb("#F59E0B")),
                "CREDITO" => ("Convenios / Crédito", Color.FromArgb("#DC2626")),
                _ => ("Otro", Color.FromArgb("#6B7280"))
            };
            datos.MediosPago.Add(new MedioPago
            {
                Nombre = nombre,
                Monto = monto,
                Color = color,
                Porcentaje = total > 0 ? (double)(monto / total * 100m) : 0
            });
        }
    }

    private static async Task CargarTopProductosAsync(MySqlConnection cn, DashboardData datos)
    {
        const string sql = @"
            SELECT p.nombre_comercial, SUM(d.cantidad) AS unidades
            FROM ventas_detalle d
            JOIN ventas_encabezado v ON v.id_venta = d.id_venta
            JOIN productos p ON p.id_producto = d.id_producto
            WHERE v.estado_documento <> 'ANULADO' AND v.fecha_hora_emision >= CURDATE() - INTERVAL 6 DAY
            GROUP BY p.id_producto, p.nombre_comercial
            ORDER BY unidades DESC
            LIMIT 5";

        await using var cmd = new MySqlCommand(sql, cn);
        await using var rd = await cmd.ExecuteReaderAsync();
        var lista = new List<ProductoVendido>();
        while (await rd.ReadAsync())
            lista.Add(new ProductoVendido { Nombre = rd.GetString(0), Unidades = Ent(rd, 1) });

        var max = lista.Count > 0 ? lista.Max(x => x.Unidades) : 0;
        for (int i = 0; i < lista.Count; i++)
        {
            lista[i].Posicion = i + 1;
            lista[i].Progreso = max > 0 ? (double)lista[i].Unidades / max : 0;
        }
        datos.TopProductos.AddRange(lista);
    }

    private static async Task CargarAlertasAsync(MySqlConnection cn, DashboardData datos)
    {
        const string sql = @"
            SELECT p.sku, p.nombre_comercial, p.formato_presentacion, p.stock_minimo,
                   COALESCE(SUM(l.stock_actual), 0) AS stock,
                   (SELECT l2.numero_lote FROM lotes l2
                     WHERE l2.id_producto = p.id_producto AND l2.stock_actual > 0
                     ORDER BY l2.fecha_vencimiento LIMIT 1) AS lote,
                   (SELECT MIN(l3.fecha_vencimiento) FROM lotes l3
                     WHERE l3.id_producto = p.id_producto AND l3.stock_actual > 0) AS vence
            FROM productos p
            LEFT JOIN lotes l ON l.id_producto = p.id_producto
            WHERE p.activo = 1
            GROUP BY p.id_producto, p.sku, p.nombre_comercial, p.formato_presentacion, p.stock_minimo
            HAVING stock <= p.stock_minimo
            ORDER BY stock ASC";

        await using var cmd = new MySqlCommand(sql, cn);
        await using var rd = await cmd.ExecuteReaderAsync();
        var todas = new List<AlertaStock>();
        while (await rd.ReadAsync())
        {
            todas.Add(new AlertaStock
            {
                Sku = rd.GetString(0),
                Nombre = rd.GetString(1),
                Presentacion = rd.IsDBNull(2) ? "" : rd.GetString(2),
                StockMinimo = Ent(rd, 3),
                Stock = Ent(rd, 4),
                Lote = rd.IsDBNull(5) ? "—" : rd.GetString(5),
                Vencimiento = rd.IsDBNull(6) ? null : Convert.ToDateTime(rd.GetValue(6))
            });
        }
        datos.Resumen.ProductosBajoMinimo = todas.Count;
        datos.Resumen.ProductosSinStock = todas.Count(a => a.Stock == 0);
        datos.Alertas.AddRange(todas.Take(5));
    }

    private static async Task<object?> EscalarAsync(MySqlConnection cn, string sql)
    {
        await using var cmd = new MySqlCommand(sql, cn);
        return await cmd.ExecuteScalarAsync();
    }

    private static decimal Dec(DbDataReader r, int i) => r.IsDBNull(i) ? 0m : Convert.ToDecimal(r.GetValue(i));
    private static int Ent(DbDataReader r, int i) => r.IsDBNull(i) ? 0 : Convert.ToInt32(r.GetValue(i));
}
