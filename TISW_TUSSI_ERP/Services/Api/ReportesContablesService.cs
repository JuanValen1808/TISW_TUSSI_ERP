using MySqlConnector;
using TISW_TUSSI_ERP.Models.Contabilidad;

namespace TISW_TUSSI_ERP.Services.Api;

// Consultas de los 5 reportes contables.
// IMPORTANTE: este servicio es de SOLO LECTURA (únicamente SELECT) para no afectar
// los datos de los demás módulos que comparten la base erp_farmaceutico.
public class ReportesContablesService
{
    private readonly DatabaseService _dbService;

    // Cuentas de efectivo: activos cuyo nombre contiene "caja" o "banco".
    private const string FiltroCuentasEfectivo =
        "p.tipo_cuenta = 'ACTIVO' AND (LOWER(p.nombre_cuenta) LIKE '%caja%' OR LOWER(p.nombre_cuenta) LIKE '%banco%')";

    public ReportesContablesService(DatabaseService dbService)
    {
        _dbService = dbService;
    }

    private static DateTime FinDelDia(DateTime fecha) => fecha.Date.AddDays(1).AddTicks(-1);
    private static string Texto(MySqlDataReader l, string col) => l.IsDBNull(l.GetOrdinal(col)) ? string.Empty : l.GetString(col);

    // ------------------------------------------------------------------
    //  REPORTE 1: Libro Diario detallado
    // ------------------------------------------------------------------
    public async Task<List<AsientoDetallado>> ObtenerLibroDiarioDetalladoAsync(DateTime desde, DateTime hasta, string? origen)
    {
        var asientos = new List<AsientoDetallado>();
        using var conexion = _dbService.CrearConexion();
        await conexion.OpenAsync();

        var query = @"
            SELECT l.id_asiento, l.numero_asiento, l.fecha_asiento, l.glosa_descripcion, l.origen,
                   IFNULL(u.nombre, '') AS usuario,
                   p.codigo_cuenta, p.nombre_cuenta, d.debe, d.haber
            FROM libro_diario_asientos l
            LEFT JOIN usuarios u ON u.id_usuario = l.id_usuario
            LEFT JOIN detalle_asiento_contable d ON d.id_asiento = l.id_asiento
            LEFT JOIN plan_cuentas p ON p.id_cuenta = d.id_cuenta
            WHERE l.fecha_asiento BETWEEN @Desde AND @Hasta";
        if (!string.IsNullOrWhiteSpace(origen))
            query += " AND l.origen = @Origen";
        query += " ORDER BY l.fecha_asiento, l.numero_asiento, (d.debe > 0) DESC, d.id_detalle_asiento;";

        using var cmd = new MySqlCommand(query, conexion);
        cmd.Parameters.AddWithValue("@Desde", desde.Date);
        cmd.Parameters.AddWithValue("@Hasta", FinDelDia(hasta));
        if (!string.IsNullOrWhiteSpace(origen))
            cmd.Parameters.AddWithValue("@Origen", origen);

        using var l = await cmd.ExecuteReaderAsync();
        AsientoDetallado? actual = null;
        while (await l.ReadAsync())
        {
            var id = l.GetInt32("id_asiento");
            if (actual == null || actual.IdAsiento != id)
            {
                actual = new AsientoDetallado
                {
                    IdAsiento = id,
                    NumeroAsiento = l.GetInt32("numero_asiento"),
                    Fecha = l.GetDateTime("fecha_asiento"),
                    Glosa = Texto(l, "glosa_descripcion"),
                    Origen = l.GetString("origen"),
                    Usuario = l.GetString("usuario")
                };
                asientos.Add(actual);
            }

            if (!l.IsDBNull(l.GetOrdinal("codigo_cuenta")))
            {
                actual.Lineas.Add(new LineaAsiento
                {
                    CodigoCuenta = l.GetString("codigo_cuenta"),
                    NombreCuenta = l.GetString("nombre_cuenta"),
                    Debe = l.GetDecimal("debe"),
                    Haber = l.GetDecimal("haber")
                });
            }
        }
        return asientos;
    }

    // ------------------------------------------------------------------
    //  REPORTE 2: Balance General (saldos acumulados a una fecha de corte)
    // ------------------------------------------------------------------
    public async Task<List<SaldoCuenta>> ObtenerSaldosAFechaAsync(DateTime fechaCorte)
    {
        var lista = new List<SaldoCuenta>();
        using var conexion = _dbService.CrearConexion();
        await conexion.OpenAsync();

        const string query = @"
            SELECT p.id_cuenta, p.codigo_cuenta, p.nombre_cuenta, p.tipo_cuenta, p.nivel,
                   IFNULL(SUM(d.debe), 0) AS total_debe,
                   IFNULL(SUM(d.haber), 0) AS total_haber
            FROM plan_cuentas p
            INNER JOIN detalle_asiento_contable d ON d.id_cuenta = p.id_cuenta
            INNER JOIN libro_diario_asientos l ON l.id_asiento = d.id_asiento
            WHERE l.fecha_asiento <= @Corte
            GROUP BY p.id_cuenta, p.codigo_cuenta, p.nombre_cuenta, p.tipo_cuenta, p.nivel
            ORDER BY p.codigo_cuenta;";

        using var cmd = new MySqlCommand(query, conexion);
        cmd.Parameters.AddWithValue("@Corte", FinDelDia(fechaCorte));
        using var l = await cmd.ExecuteReaderAsync();
        while (await l.ReadAsync())
        {
            lista.Add(new SaldoCuenta
            {
                IdCuenta = l.GetInt32("id_cuenta"),
                CodigoCuenta = l.GetString("codigo_cuenta"),
                NombreCuenta = l.GetString("nombre_cuenta"),
                TipoCuenta = l.GetString("tipo_cuenta"),
                Nivel = l.GetInt32("nivel"),
                TotalDebe = l.GetDecimal("total_debe"),
                TotalHaber = l.GetDecimal("total_haber")
            });
        }
        return lista;
    }

    // ------------------------------------------------------------------
    //  REPORTE 4: Flujo de Caja (movimientos de cuentas de efectivo)
    // ------------------------------------------------------------------
    public async Task<(decimal SaldoInicial, List<MovimientoCaja> Movimientos)> ObtenerFlujoCajaAsync(DateTime desde, DateTime hasta)
    {
        var movimientos = new List<MovimientoCaja>();
        using var conexion = _dbService.CrearConexion();
        await conexion.OpenAsync();

        // Saldo de efectivo antes del período
        decimal saldoInicial;
        var qInicial = $@"
            SELECT IFNULL(SUM(d.debe - d.haber), 0)
            FROM detalle_asiento_contable d
            INNER JOIN libro_diario_asientos l ON l.id_asiento = d.id_asiento
            INNER JOIN plan_cuentas p ON p.id_cuenta = d.id_cuenta
            WHERE {FiltroCuentasEfectivo} AND l.fecha_asiento < @Desde;";
        using (var cmd = new MySqlCommand(qInicial, conexion))
        {
            cmd.Parameters.AddWithValue("@Desde", desde.Date);
            saldoInicial = Convert.ToDecimal(await cmd.ExecuteScalarAsync() ?? 0m);
        }

        var qMov = $@"
            SELECT l.id_asiento, l.numero_asiento, l.fecha_asiento, l.glosa_descripcion, l.origen,
                   SUM(d.debe) AS entrada, SUM(d.haber) AS salida,
                   (SELECT GROUP_CONCAT(DISTINCT p2.nombre_cuenta ORDER BY p2.codigo_cuenta SEPARATOR ', ')
                      FROM detalle_asiento_contable d2
                      INNER JOIN plan_cuentas p2 ON p2.id_cuenta = d2.id_cuenta
                     WHERE d2.id_asiento = l.id_asiento
                       AND NOT ({FiltroCuentasEfectivo.Replace("p.", "p2.")})) AS contrapartida
            FROM detalle_asiento_contable d
            INNER JOIN libro_diario_asientos l ON l.id_asiento = d.id_asiento
            INNER JOIN plan_cuentas p ON p.id_cuenta = d.id_cuenta
            WHERE {FiltroCuentasEfectivo} AND l.fecha_asiento BETWEEN @Desde AND @Hasta
            GROUP BY l.id_asiento, l.numero_asiento, l.fecha_asiento, l.glosa_descripcion, l.origen
            ORDER BY l.fecha_asiento, l.numero_asiento;";

        using (var cmd = new MySqlCommand(qMov, conexion))
        {
            cmd.Parameters.AddWithValue("@Desde", desde.Date);
            cmd.Parameters.AddWithValue("@Hasta", FinDelDia(hasta));
            using var l = await cmd.ExecuteReaderAsync();
            decimal saldo = saldoInicial;
            while (await l.ReadAsync())
            {
                var entrada = l.GetDecimal("entrada");
                var salida = l.GetDecimal("salida");
                saldo += entrada - salida;
                movimientos.Add(new MovimientoCaja
                {
                    Fecha = l.GetDateTime("fecha_asiento"),
                    NumeroAsiento = l.GetInt32("numero_asiento"),
                    Glosa = Texto(l, "glosa_descripcion"),
                    Origen = l.GetString("origen"),
                    Contrapartida = Texto(l, "contrapartida"),
                    Entrada = entrada,
                    Salida = salida,
                    Saldo = saldo
                });
            }
        }

        return (saldoInicial, movimientos);
    }

    // ------------------------------------------------------------------
    //  REPORTE 5: Análisis por Cuenta (incluye subcuentas directas)
    // ------------------------------------------------------------------
    public async Task<(decimal DebeAnterior, decimal HaberAnterior, List<MovimientoCuenta> Movimientos)> ObtenerAnalisisCuentaAsync(
        int idCuenta, DateTime desde, DateTime hasta)
    {
        var movimientos = new List<MovimientoCuenta>();
        using var conexion = _dbService.CrearConexion();
        await conexion.OpenAsync();

        const string filtroCuenta = "(p.id_cuenta = @IdCuenta OR p.cuenta_padre_id = @IdCuenta)";

        decimal debeAnt = 0, haberAnt = 0;
        var qInicial = $@"
            SELECT IFNULL(SUM(d.debe), 0) AS debe, IFNULL(SUM(d.haber), 0) AS haber
            FROM detalle_asiento_contable d
            INNER JOIN libro_diario_asientos l ON l.id_asiento = d.id_asiento
            INNER JOIN plan_cuentas p ON p.id_cuenta = d.id_cuenta
            WHERE {filtroCuenta} AND l.fecha_asiento < @Desde;";
        using (var cmd = new MySqlCommand(qInicial, conexion))
        {
            cmd.Parameters.AddWithValue("@IdCuenta", idCuenta);
            cmd.Parameters.AddWithValue("@Desde", desde.Date);
            using var l = await cmd.ExecuteReaderAsync();
            if (await l.ReadAsync())
            {
                debeAnt = l.GetDecimal("debe");
                haberAnt = l.GetDecimal("haber");
            }
        }

        var qMov = $@"
            SELECT l.fecha_asiento, l.numero_asiento, l.glosa_descripcion, l.origen, p.codigo_cuenta, d.debe, d.haber,
                   (SELECT GROUP_CONCAT(DISTINCT p2.nombre_cuenta ORDER BY p2.codigo_cuenta SEPARATOR ', ')
                      FROM detalle_asiento_contable d2
                      INNER JOIN plan_cuentas p2 ON p2.id_cuenta = d2.id_cuenta
                     WHERE d2.id_asiento = l.id_asiento AND d2.id_cuenta <> d.id_cuenta) AS contrapartida
            FROM detalle_asiento_contable d
            INNER JOIN libro_diario_asientos l ON l.id_asiento = d.id_asiento
            INNER JOIN plan_cuentas p ON p.id_cuenta = d.id_cuenta
            WHERE {filtroCuenta} AND l.fecha_asiento BETWEEN @Desde AND @Hasta
            ORDER BY l.fecha_asiento, l.numero_asiento, d.id_detalle_asiento;";
        using (var cmd = new MySqlCommand(qMov, conexion))
        {
            cmd.Parameters.AddWithValue("@IdCuenta", idCuenta);
            cmd.Parameters.AddWithValue("@Desde", desde.Date);
            cmd.Parameters.AddWithValue("@Hasta", FinDelDia(hasta));
            using var l = await cmd.ExecuteReaderAsync();
            while (await l.ReadAsync())
            {
                movimientos.Add(new MovimientoCuenta
                {
                    Fecha = l.GetDateTime("fecha_asiento"),
                    NumeroAsiento = l.GetInt32("numero_asiento"),
                    Glosa = Texto(l, "glosa_descripcion"),
                    Origen = l.GetString("origen"),
                    CodigoCuenta = l.GetString("codigo_cuenta"),
                    Contrapartida = Texto(l, "contrapartida"),
                    Debe = l.GetDecimal("debe"),
                    Haber = l.GetDecimal("haber")
                });
            }
        }

        return (debeAnt, haberAnt, movimientos);
    }
}
