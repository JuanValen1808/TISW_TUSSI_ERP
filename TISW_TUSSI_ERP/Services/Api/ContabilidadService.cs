using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Contabilidad;

namespace TISW_TUSSI_ERP.Services.Api;

public class ContabilidadService
{
    private readonly DatabaseService _dbService;
    public const decimal TasaIvaChile = 0.19m; // Art. 14 D.L. 825 Ley sobre Impuesto a las Ventas y Servicios

    public ContabilidadService(DatabaseService dbService)
    {
        _dbService = dbService;
    }

    // =========================================================================
    //  RF-CON-01: PLAN DE CUENTAS JERÁRQUICO (NORMATIVA SII / IFRS PYME CHILE)
    // =========================================================================

    /// <summary>
    /// Asegura que el catálogo contable cuente con las cuentas base exigidas por la normativa chilena
    /// (Caja, Banco, Clientes, IVA Crédito, Inventario, PPM, Proveedores, IVA Débito, Retenciones/F29,
    /// Capital, Ventas Netas, Convenios, Costo de Ventas, Gastos Administración y Mermas).
    /// </summary>
    public async Task AsegurarPlanCuentasChilenoAsync()
    {
        using var conexion = _dbService.CrearConexion();
        await conexion.OpenAsync();

        // Obtenemos los códigos existentes e IDs de grupos nivel 1
        var existentes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        using (var cmd = new MySqlCommand("SELECT id_cuenta, codigo_cuenta FROM plan_cuentas;", conexion))
        using (var r = await cmd.ExecuteReaderAsync())
        {
            while (await r.ReadAsync())
                existentes[r.GetString("codigo_cuenta")] = r.GetInt32("id_cuenta");
        }

        async Task<int> AsegurarCuentaAsync(string codigo, string nombre, string tipo, int nivel, string? codigoPadre)
        {
            if (existentes.TryGetValue(codigo, out var idActual))
                return idActual;

            int? idPadre = null;
            if (!string.IsNullOrEmpty(codigoPadre) && existentes.TryGetValue(codigoPadre, out var pid))
                idPadre = pid;

            const string ins = @"
                INSERT INTO plan_cuentas (codigo_cuenta, nombre_cuenta, tipo_cuenta, nivel, cuenta_padre_id)
                VALUES (@Cod, @Nom, @Tipo, @Nivel, @Padre);
                SELECT LAST_INSERT_ID();";
            using var cmd = new MySqlCommand(ins, conexion);
            cmd.Parameters.AddWithValue("@Cod", codigo);
            cmd.Parameters.AddWithValue("@Nom", nombre);
            cmd.Parameters.AddWithValue("@Tipo", tipo);
            cmd.Parameters.AddWithValue("@Nivel", nivel);
            cmd.Parameters.AddWithValue("@Padre", (object?)idPadre ?? DBNull.Value);

            var nuevoId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            existentes[codigo] = nuevoId;
            return nuevoId;
        }

        // Nivel 1: Grupos principales (PCGA / IFRS Chile)
        await AsegurarCuentaAsync("1", "ACTIVOS", "ACTIVO", 1, null);
        await AsegurarCuentaAsync("2", "PASIVOS", "PASIVO", 1, null);
        await AsegurarCuentaAsync("3", "PATRIMONIO", "PATRIMONIO", 1, null);
        await AsegurarCuentaAsync("4", "INGRESOS OPERACIONALES", "INGRESO", 1, null);
        await AsegurarCuentaAsync("5", "COSTOS Y GASTOS", "GASTO", 1, null);

        // Nivel 2: Cuentas imputables chilenas estándar
        await AsegurarCuentaAsync("1.1.01", "Caja General", "ACTIVO", 2, "1");
        await AsegurarCuentaAsync("1.1.02", "Inventario de Mercaderías", "ACTIVO", 2, "1");
        await AsegurarCuentaAsync("1.1.03", "Cuentas por Cobrar Clientes", "ACTIVO", 2, "1");
        await AsegurarCuentaAsync("1.1.04", "IVA Crédito Fiscal", "ACTIVO", 2, "1");
        await AsegurarCuentaAsync("1.1.05", "Banco Cuenta Corriente", "ACTIVO", 2, "1");
        await AsegurarCuentaAsync("1.1.06", "PPM por Recuperar (Art. 84 LIR)", "ACTIVO", 2, "1");

        await AsegurarCuentaAsync("2.1.01", "Cuentas por Pagar Proveedores", "PASIVO", 2, "2");
        await AsegurarCuentaAsync("2.1.02", "IVA Débito Fiscal", "PASIVO", 2, "2");
        await AsegurarCuentaAsync("2.1.03", "Impuestos por Pagar F29 (SII)", "PASIVO", 2, "2");

        await AsegurarCuentaAsync("3.1.01", "Capital Social", "PATRIMONIO", 2, "3");
        await AsegurarCuentaAsync("3.1.02", "Resultados Acumulados", "PATRIMONIO", 2, "3");

        await AsegurarCuentaAsync("4.1.01", "Ventas Netas Afectas", "INGRESO", 2, "4");
        await AsegurarCuentaAsync("4.1.02", "Ingresos por Convenios de Salud", "INGRESO", 2, "4");

        await AsegurarCuentaAsync("5.1.01", "Costo de Ventas", "GASTO", 2, "5");
        await AsegurarCuentaAsync("5.1.02", "Gastos de Administración y Ventas", "GASTO", 2, "5");
        await AsegurarCuentaAsync("5.1.03", "Pérdidas por Mermas y Vencimientos", "GASTO", 2, "5");
    }

    public async Task<List<PlanCuentas>> ObtenerPlanCuentasAsync()
    {
        var lista = new List<PlanCuentas>();
        using var conexion = _dbService.CrearConexion();
        await conexion.OpenAsync();

        const string query = @"
            SELECT p.id_cuenta, p.codigo_cuenta, p.nombre_cuenta, p.tipo_cuenta, p.nivel, p.cuenta_padre_id,
                   IFNULL(SUM(d.debe), 0) AS total_debe,
                   IFNULL(SUM(d.haber), 0) AS total_haber,
                   COUNT(d.id_detalle_asiento) AS cant_movs
            FROM plan_cuentas p
            LEFT JOIN detalle_asiento_contable d ON d.id_cuenta = p.id_cuenta
            GROUP BY p.id_cuenta, p.codigo_cuenta, p.nombre_cuenta, p.tipo_cuenta, p.nivel, p.cuenta_padre_id
            ORDER BY p.codigo_cuenta;";

        using var comando = new MySqlCommand(query, conexion);
        using var lector = await comando.ExecuteReaderAsync();

        while (await lector.ReadAsync())
        {
            lista.Add(new PlanCuentas
            {
                IdCuenta = lector.GetInt32("id_cuenta"),
                CodigoCuenta = lector.GetString("codigo_cuenta"),
                NombreCuenta = Formato.SanitizarTexto(lector.GetString("nombre_cuenta")),
                TipoCuenta = lector.GetString("tipo_cuenta"),
                Nivel = lector.GetInt32("nivel"),
                CuentaPadreId = lector.IsDBNull(lector.GetOrdinal("cuenta_padre_id")) ? null : lector.GetInt32("cuenta_padre_id"),
                TotalDebe = lector.GetDecimal("total_debe"),
                TotalHaber = lector.GetDecimal("total_haber"),
                CantidadMovimientos = Convert.ToInt32(lector["cant_movs"])
            });
        }

        // Acumular saldos de subcuentas hacia cuentas de agrupación (Nivel 1)
        foreach (var grupo in lista.Where(c => c.EsGrupo))
        {
            var hijos = lista.Where(h => !h.EsGrupo && (h.CuentaPadreId == grupo.IdCuenta || h.TipoCuenta == grupo.TipoCuenta)).ToList();
            grupo.TotalDebe += hijos.Sum(h => h.TotalDebe);
            grupo.TotalHaber += hijos.Sum(h => h.TotalHaber);
            grupo.CantidadMovimientos += hijos.Sum(h => h.CantidadMovimientos);
        }

        return lista;
    }

    public async Task GuardarCuentaContableAsync(PlanCuentas cuenta)
    {
        using var conexion = _dbService.CrearConexion();
        await conexion.OpenAsync();

        if (cuenta.IdCuenta == 0)
        {
            const string sql = @"
                INSERT INTO plan_cuentas (codigo_cuenta, nombre_cuenta, tipo_cuenta, nivel, cuenta_padre_id)
                VALUES (@Cod, @Nom, @Tipo, @Nivel, @Padre);";
            using var cmd = new MySqlCommand(sql, conexion);
            cmd.Parameters.AddWithValue("@Cod", cuenta.CodigoCuenta.Trim());
            cmd.Parameters.AddWithValue("@Nom", cuenta.NombreCuenta.Trim());
            cmd.Parameters.AddWithValue("@Tipo", cuenta.TipoCuenta.Trim().ToUpperInvariant());
            cmd.Parameters.AddWithValue("@Nivel", cuenta.Nivel);
            cmd.Parameters.AddWithValue("@Padre", (object?)cuenta.CuentaPadreId ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }
        else
        {
            const string sql = @"
                UPDATE plan_cuentas
                SET codigo_cuenta = @Cod,
                    nombre_cuenta = @Nom,
                    tipo_cuenta = @Tipo,
                    nivel = @Nivel,
                    cuenta_padre_id = @Padre
                WHERE id_cuenta = @Id;";
            using var cmd = new MySqlCommand(sql, conexion);
            cmd.Parameters.AddWithValue("@Id", cuenta.IdCuenta);
            cmd.Parameters.AddWithValue("@Cod", cuenta.CodigoCuenta.Trim());
            cmd.Parameters.AddWithValue("@Nom", cuenta.NombreCuenta.Trim());
            cmd.Parameters.AddWithValue("@Tipo", cuenta.TipoCuenta.Trim().ToUpperInvariant());
            cmd.Parameters.AddWithValue("@Nivel", cuenta.Nivel);
            cmd.Parameters.AddWithValue("@Padre", (object?)cuenta.CuentaPadreId ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    public async Task EliminarCuentaContableAsync(int idCuenta)
    {
        using var conexion = _dbService.CrearConexion();
        await conexion.OpenAsync();

        const string checkSql = @"
            SELECT
                (SELECT COUNT(*) FROM detalle_asiento_contable WHERE id_cuenta = @Id) +
                (SELECT COUNT(*) FROM plan_cuentas WHERE cuenta_padre_id = @Id);";
        using var checkCmd = new MySqlCommand(checkSql, conexion);
        checkCmd.Parameters.AddWithValue("@Id", idCuenta);
        var uso = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

        if (uso > 0)
            throw new InvalidOperationException("No se puede eliminar la cuenta porque posee asientos contables imputados o subcuentas dependientes (Normativa de Integridad SII).");

        using var delCmd = new MySqlCommand("DELETE FROM plan_cuentas WHERE id_cuenta = @Id;", conexion);
        delCmd.Parameters.AddWithValue("@Id", idCuenta);
        await delCmd.ExecuteNonQueryAsync();
    }

    // =========================================================================
    //  RF-CON-02: LIBRO DIARIO (ASIENTOS MANUALES Y AUTOMÁTICOS DESDE MÓDULOS)
    // =========================================================================

    public async Task<List<LibroDiarioAsiento>> ObtenerLibroDiarioAsync(DateTime? desde = null, DateTime? hasta = null, string? origen = null)
    {
        var lista = new List<LibroDiarioAsiento>();
        var mapa = new Dictionary<int, LibroDiarioAsiento>();

        using var conexion = _dbService.CrearConexion();
        await conexion.OpenAsync();

        var query = @"
            SELECT l.id_asiento, l.numero_asiento, l.fecha_asiento, l.glosa_descripcion, l.origen, l.id_usuario, l.created_at,
                   IFNULL(u.nombre, 'Sistema ERP') AS usuario,
                   IFNULL(SUM(d.debe), 0) AS total_debe,
                   IFNULL(SUM(d.haber), 0) AS total_haber,
                   COUNT(d.id_detalle_asiento) AS lineas
            FROM libro_diario_asientos l
            LEFT JOIN usuarios u ON u.id_usuario = l.id_usuario
            LEFT JOIN detalle_asiento_contable d ON d.id_asiento = l.id_asiento
            WHERE 1=1";

        if (desde.HasValue) query += " AND l.fecha_asiento >= @Desde";
        if (hasta.HasValue) query += " AND l.fecha_asiento <= @Hasta";
        if (!string.IsNullOrWhiteSpace(origen) && origen != "Todos") query += " AND l.origen = @Origen";

        query += @"
            GROUP BY l.id_asiento, l.numero_asiento, l.fecha_asiento, l.glosa_descripcion, l.origen, l.id_usuario, l.created_at, u.nombre
            ORDER BY l.fecha_asiento DESC, l.numero_asiento DESC;";

        using (var comando = new MySqlCommand(query, conexion))
        {
            if (desde.HasValue) comando.Parameters.AddWithValue("@Desde", desde.Value.Date);
            if (hasta.HasValue) comando.Parameters.AddWithValue("@Hasta", hasta.Value.Date.AddDays(1).AddTicks(-1));
            if (!string.IsNullOrWhiteSpace(origen) && origen != "Todos") comando.Parameters.AddWithValue("@Origen", origen);

            using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
            {
                var item = new LibroDiarioAsiento
                {
                    IdAsiento = lector.GetInt32("id_asiento"),
                    NumeroAsiento = lector.GetInt32("numero_asiento"),
                    FechaAsiento = lector.GetDateTime("fecha_asiento"),
                    GlosaDescripcion = lector.IsDBNull(lector.GetOrdinal("glosa_descripcion")) ? string.Empty : Formato.SanitizarTexto(lector.GetString("glosa_descripcion")),
                    Origen = lector.GetString("origen"),
                    IdUsuario = lector.GetInt32("id_usuario"),
                    CreatedAt = lector.GetDateTime("created_at"),
                    Usuario = Formato.SanitizarTexto(lector.GetString("usuario")),
                    TotalDebe = lector.GetDecimal("total_debe"),
                    TotalHaber = lector.GetDecimal("total_haber"),
                    CantidadLineas = Convert.ToInt32(lector["lineas"])
                };
                lista.Add(item);
                mapa[item.IdAsiento] = item;
            }
        }

        if (lista.Count > 0)
        {
            const string qDet = @"
                SELECT d.id_asiento, p.codigo_cuenta, p.nombre_cuenta, d.debe, d.haber
                FROM detalle_asiento_contable d
                INNER JOIN plan_cuentas p ON p.id_cuenta = d.id_cuenta
                ORDER BY d.id_asiento, (d.debe > 0) DESC, d.id_detalle_asiento;";
            using var cmdDet = new MySqlCommand(qDet, conexion);
            using var rDet = await cmdDet.ExecuteReaderAsync();
            while (await rDet.ReadAsync())
            {
                var idAsiento = rDet.GetInt32("id_asiento");
                if (mapa.TryGetValue(idAsiento, out var asiento))
                {
                    asiento.Lineas.Add(new LineaAsiento
                    {
                        CodigoCuenta = rDet.GetString("codigo_cuenta"),
                        NombreCuenta = Formato.SanitizarTexto(rDet.GetString("nombre_cuenta")),
                        Debe = rDet.GetDecimal("debe"),
                        Haber = rDet.GetDecimal("haber")
                    });
                }
            }
        }

        return lista;
    }

    public async Task<List<LineaAsiento>> ObtenerDetalleAsientoAsync(int idAsiento)
    {
        var lista = new List<LineaAsiento>();
        using var conexion = _dbService.CrearConexion();
        await conexion.OpenAsync();

        const string query = @"
            SELECT p.codigo_cuenta, p.nombre_cuenta, d.debe, d.haber
            FROM detalle_asiento_contable d
            INNER JOIN plan_cuentas p ON p.id_cuenta = d.id_cuenta
            WHERE d.id_asiento = @IdAsiento
            ORDER BY (d.debe > 0) DESC, d.id_detalle_asiento;";

        using var comando = new MySqlCommand(query, conexion);
        comando.Parameters.AddWithValue("@IdAsiento", idAsiento);
        using var lector = await comando.ExecuteReaderAsync();

        while (await lector.ReadAsync())
        {
            lista.Add(new LineaAsiento
            {
                CodigoCuenta = lector.GetString("codigo_cuenta"),
                NombreCuenta = Formato.SanitizarTexto(lector.GetString("nombre_cuenta")),
                Debe = lector.GetDecimal("debe"),
                Haber = lector.GetDecimal("haber")
            });
        }

        return lista;
    }

    /// <summary>
    /// Registra un asiento contable en el Libro Diario validando el principio de Partida Doble (Debe = Haber).
    /// </summary>
    public async Task<int> RegistrarAsientoManualAsync(DateTime fecha, string glosa, string origen, int idUsuario, List<LineaAsientoInput> lineas)
    {
        if (lineas == null || lineas.Count < 2)
            throw new InvalidOperationException("Un asiento contable requiere al menos 2 líneas de imputación (Partida Doble).");

        var totalDebe = Math.Round(lineas.Sum(l => l.Debe), 2);
        var totalHaber = Math.Round(lineas.Sum(l => l.Haber), 2);

        if (totalDebe <= 0 || totalDebe != totalHaber)
            throw new InvalidOperationException($"El asiento está descuadrado. Total Debe ({Formato.Moneda(totalDebe)}) debe ser exactamente igual a Total Haber ({Formato.Moneda(totalHaber)}).");

        using var conexion = _dbService.CrearConexion();
        await conexion.OpenAsync();
        using var tx = await conexion.BeginTransactionAsync();

        try
        {
            var idAsiento = await InsertarAsientoTransaccionalAsync(conexion, tx, fecha, glosa, origen, idUsuario,
                lineas.Select(l => (l.IdCuenta, l.Debe, l.Haber)).ToList());

            await tx.CommitAsync();
            return idAsiento;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private static async Task<int> InsertarAsientoTransaccionalAsync(
        MySqlConnection conexion,
        MySqlTransaction tx,
        DateTime fecha,
        string glosa,
        string origen,
        int idUsuario,
        List<(int IdCuenta, decimal Debe, decimal Haber)> lineas)
    {
        int siguienteNumero;
        using (var cmdNum = new MySqlCommand("SELECT COALESCE(MAX(numero_asiento), 0) + 1 FROM libro_diario_asientos FOR UPDATE;", conexion, tx))
        {
            siguienteNumero = Convert.ToInt32(await cmdNum.ExecuteScalarAsync());
        }

        int usuarioValido = idUsuario > 0 ? idUsuario : 1;
        const string insEnc = @"
            INSERT INTO libro_diario_asientos (numero_asiento, fecha_asiento, glosa_descripcion, origen, id_usuario)
            VALUES (@Num, @Fecha, @Glosa, @Origen, @Usuario);
            SELECT LAST_INSERT_ID();";

        int idAsiento;
        using (var cmdEnc = new MySqlCommand(insEnc, conexion, tx))
        {
            cmdEnc.Parameters.AddWithValue("@Num", siguienteNumero);
            cmdEnc.Parameters.AddWithValue("@Fecha", fecha.Date);
            cmdEnc.Parameters.AddWithValue("@Glosa", glosa.Trim());
            cmdEnc.Parameters.AddWithValue("@Origen", origen);
            cmdEnc.Parameters.AddWithValue("@Usuario", usuarioValido);
            idAsiento = Convert.ToInt32(await cmdEnc.ExecuteScalarAsync());
        }

        const string insDet = @"
            INSERT INTO detalle_asiento_contable (id_asiento, id_cuenta, debe, haber)
            VALUES (@IdAsiento, @IdCuenta, @Debe, @Haber);";

        foreach (var (idCuenta, debe, haber) in lineas.Where(l => l.Debe > 0 || l.Haber > 0))
        {
            using var cmdDet = new MySqlCommand(insDet, conexion, tx);
            cmdDet.Parameters.AddWithValue("@IdAsiento", idAsiento);
            cmdDet.Parameters.AddWithValue("@IdCuenta", idCuenta);
            cmdDet.Parameters.AddWithValue("@Debe", Math.Round(debe, 2));
            cmdDet.Parameters.AddWithValue("@Haber", Math.Round(haber, 2));
            await cmdDet.ExecuteNonQueryAsync();
        }

        return idAsiento;
    }

    /// <summary>
    /// Consulta cuántas ventas, recepciones de compra y mermas aún no tienen asiento contable generado.
    /// </summary>
    public async Task<ResumenPendientesContables> ObtenerPendientesCentralizarAsync()
    {
        using var conexion = _dbService.CrearConexion();
        await conexion.OpenAsync();

        const string sql = @"
            SELECT
                (SELECT COUNT(*) FROM ventas_encabezado WHERE id_asiento_contable IS NULL AND estado_documento <> 'ANULADO') AS ventas_pend,
                (SELECT COUNT(*) FROM recepciones_facturas_compra WHERE id_asiento_contable IS NULL) AS compras_pend,
                (SELECT COUNT(*) FROM registro_mermas m
                  WHERE NOT EXISTS (
                      SELECT 1 FROM libro_diario_asientos l
                      WHERE l.glosa_descripcion LIKE CONCAT('%[MERMA #', m.id_merma, ']%')
                  )) AS mermas_pend;";

        using var cmd = new MySqlCommand(sql, conexion);
        using var r = await cmd.ExecuteReaderAsync();
        if (await r.ReadAsync())
        {
            return new ResumenPendientesContables
            {
                VentasSinCentralizar = r.GetInt32("ventas_pend"),
                ComprasSinCentralizar = r.GetInt32("compras_pend"),
                MermasSinCentralizar = r.GetInt32("mermas_pend")
            };
        }
        return new ResumenPendientesContables();
    }

    /// <summary>
    /// RF-CON-02: Genera automáticamente los asientos contables desde Ventas, Compras e Inventario (Mermas)
    /// aplicando normativa chilena (IVA Débito/Crédito 19%, Costo de Venta y Partida Doble).
    /// </summary>
    public async Task<(int AsientosVentas, int AsientosCompras, int AsientosMermas)> CentralizarMovimientosPendientesAsync(int idUsuario)
    {
        await AsegurarPlanCuentasChilenoAsync();

        using var conexion = _dbService.CrearConexion();
        await conexion.OpenAsync();

        // Mapear IDs de cuentas contables clave por código
        var cuentas = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        using (var cmdC = new MySqlCommand("SELECT id_cuenta, codigo_cuenta FROM plan_cuentas;", conexion))
        using (var rC = await cmdC.ExecuteReaderAsync())
        {
            while (await rC.ReadAsync())
                cuentas[rC.GetString("codigo_cuenta")] = rC.GetInt32("id_cuenta");
        }

        int idCaja = cuentas.GetValueOrDefault("1.1.01", 6);
        int idBanco = cuentas.GetValueOrDefault("1.1.05", idCaja);
        int idInventario = cuentas.GetValueOrDefault("1.1.02", 7);
        int idClientes = cuentas.GetValueOrDefault("1.1.03", idCaja);
        int idIvaCredito = cuentas.GetValueOrDefault("1.1.04", idInventario);
        int idProveedores = cuentas.GetValueOrDefault("2.1.01", 2);
        int idIvaDebito = cuentas.GetValueOrDefault("2.1.02", idProveedores);
        int idVentasNetas = cuentas.GetValueOrDefault("4.1.01", 8);
        int idIngresosConvenios = cuentas.GetValueOrDefault("4.1.02", idVentasNetas);
        int idCostoVentas = cuentas.GetValueOrDefault("5.1.01", 9);
        int idGastoMermas = cuentas.GetValueOrDefault("5.1.03", cuentas.GetValueOrDefault("5.1.02", idCostoVentas));

        int contVentas = 0, contCompras = 0, contMermas = 0;

        using var tx = await conexion.BeginTransactionAsync();
        try
        {
            // 1. CENTRALIZAR VENTAS PENDIENTES (Con IVA Débito Fiscal y Costo de Ventas)
            var ventasPend = new List<(int IdVenta, string Folio, string TipoDoc, DateTime Fecha, string FormaPago, int? IdConvenio, decimal Neto, decimal Iva, decimal Total, string Cliente, decimal CostoVenta)>();
            const string qVentas = @"
                SELECT v.id_venta, v.folio_documento, v.tipo_documento, DATE(v.fecha_hora_emision) AS fecha,
                       v.forma_pago, v.id_convenio, v.monto_neto, v.monto_iva, v.monto_total,
                       cl.razon_social_nombre,
                       IFNULL((SELECT SUM(vd.cantidad * pr.costo_promedio_ponderado)
                               FROM ventas_detalle vd
                               INNER JOIN productos pr ON pr.id_producto = vd.id_producto
                               WHERE vd.id_venta = v.id_venta), 0) AS costo_total
                FROM ventas_encabezado v
                INNER JOIN clientes cl ON cl.id_cliente = v.id_cliente
                WHERE v.id_asiento_contable IS NULL AND v.estado_documento <> 'ANULADO'
                ORDER BY v.fecha_hora_emision ASC;";

            using (var cmdV = new MySqlCommand(qVentas, conexion, tx))
            using (var rV = await cmdV.ExecuteReaderAsync())
            {
                while (await rV.ReadAsync())
                {
                    ventasPend.Add((
                        rV.GetInt32("id_venta"),
                        rV.GetString("folio_documento"),
                        rV.GetString("tipo_documento"),
                        rV.GetDateTime("fecha"),
                        rV.GetString("forma_pago"),
                        rV.IsDBNull(rV.GetOrdinal("id_convenio")) ? null : rV.GetInt32("id_convenio"),
                        rV.GetDecimal("monto_neto"),
                        rV.GetDecimal("monto_iva"),
                        rV.GetDecimal("monto_total"),
                        Formato.SanitizarTexto(rV.GetString("razon_social_nombre")),
                        rV.GetDecimal("costo_total")
                    ));
                }
            }

            foreach (var v in ventasPend)
            {
                decimal total = Math.Round(v.Total, 2);
                if (total <= 0) continue;

                // En Chile: Total Bruto = Neto + IVA 19%. Si por descuento el neto+iva difiere del total cobrado, ajustamos proporcionalmente para cuadrar partida doble.
                decimal neto = Math.Round(total / (1m + TasaIvaChile), 2);
                decimal iva = Math.Round(total - neto, 2);
                if (Math.Abs((v.Neto + v.Iva) - total) <= 1m)
                {
                    neto = Math.Round(v.Neto, 2);
                    iva = Math.Round(total - neto, 2);
                }

                int cuentaCobro = v.FormaPago == "CREDITO" ? idClientes : (v.FormaPago is "TARJETA" or "TRANSFERENCIA" ? idBanco : idCaja);
                int cuentaIngreso = v.IdConvenio.HasValue ? idIngresosConvenios : idVentasNetas;

                var lineas = new List<(int IdCuenta, decimal Debe, decimal Haber)>();
                if (v.TipoDoc == "NOTA_CREDITO")
                {
                    lineas.Add((cuentaIngreso, neto, 0));
                    lineas.Add((idIvaDebito, iva, 0));
                    lineas.Add((cuentaCobro, 0, total));
                }
                else
                {
                    lineas.Add((cuentaCobro, total, 0));
                    lineas.Add((cuentaIngreso, 0, neto));
                    if (iva > 0) lineas.Add((idIvaDebito, 0, iva));

                    if (v.CostoVenta > 0)
                    {
                        lineas.Add((idCostoVentas, Math.Round(v.CostoVenta, 2), 0));
                        lineas.Add((idInventario, 0, Math.Round(v.CostoVenta, 2)));
                    }
                }

                string glosa = $"Centralización {v.TipoDoc} {v.Folio} · {v.Cliente} ({v.FormaPago})";
                int idAsiento = await InsertarAsientoTransaccionalAsync(conexion, tx, v.Fecha, glosa, "VENTA", idUsuario, lineas);

                using var cmdUpd = new MySqlCommand("UPDATE ventas_encabezado SET id_asiento_contable = @IdAsiento WHERE id_venta = @IdVenta;", conexion, tx);
                cmdUpd.Parameters.AddWithValue("@IdAsiento", idAsiento);
                cmdUpd.Parameters.AddWithValue("@IdVenta", v.IdVenta);
                await cmdUpd.ExecuteNonQueryAsync();

                contVentas++;
            }

            // 2. CENTRALIZAR RECEPCIONES DE FACTURAS DE COMPRA PENDIENTES (Neto Inventario + IVA Crédito 19% vs Proveedores)
            var comprasPend = new List<(int IdRecepcion, string NumFactura, DateTime Fecha, decimal Total, string Proveedor, string NumOc)>();
            const string qCompras = @"
                SELECT r.id_recepcion, r.numero_factura_proveedor, r.fecha_factura, r.monto_total_facturado,
                       pr.razon_social, oc.numero_oc
                FROM recepciones_facturas_compra r
                INNER JOIN ordenes_compra oc ON oc.id_orden_compra = r.id_orden_compra
                INNER JOIN proveedores pr ON pr.id_proveedor = oc.id_proveedor
                WHERE r.id_asiento_contable IS NULL
                ORDER BY r.fecha_factura ASC;";

            using (var cmdC = new MySqlCommand(qCompras, conexion, tx))
            using (var rC = await cmdC.ExecuteReaderAsync())
            {
                while (await rC.ReadAsync())
                {
                    comprasPend.Add((
                        rC.GetInt32("id_recepcion"),
                        rC.GetString("numero_factura_proveedor"),
                        rC.GetDateTime("fecha_factura"),
                        rC.GetDecimal("monto_total_facturado"),
                        Formato.SanitizarTexto(rC.GetString("razon_social")),
                        rC.GetString("numero_oc")
                    ));
                }
            }

            foreach (var c in comprasPend)
            {
                decimal total = Math.Round(c.Total, 2);
                if (total <= 0) continue;

                decimal neto = Math.Round(total / (1m + TasaIvaChile), 2);
                decimal ivaCredito = Math.Round(total - neto, 2);

                var lineas = new List<(int IdCuenta, decimal Debe, decimal Haber)>
                {
                    (idInventario, neto, 0),
                    (idIvaCredito, ivaCredito, 0),
                    (idProveedores, 0, total)
                };

                string glosa = $"Factura Compra {c.NumFactura} ({c.NumOc}) · {c.Proveedor}";
                int idAsiento = await InsertarAsientoTransaccionalAsync(conexion, tx, c.Fecha, glosa, "COMPRA", idUsuario, lineas);

                using (var cmdUpdR = new MySqlCommand("UPDATE recepciones_facturas_compra SET id_asiento_contable = @IdAsiento WHERE id_recepcion = @IdRec;", conexion, tx))
                {
                    cmdUpdR.Parameters.AddWithValue("@IdAsiento", idAsiento);
                    cmdUpdR.Parameters.AddWithValue("@IdRec", c.IdRecepcion);
                    await cmdUpdR.ExecuteNonQueryAsync();
                }

                using (var cmdUpdP = new MySqlCommand("UPDATE cuentas_por_pagar SET id_asiento_contable = @IdAsiento WHERE id_recepcion = @IdRec AND id_asiento_contable IS NULL;", conexion, tx))
                {
                    cmdUpdP.Parameters.AddWithValue("@IdAsiento", idAsiento);
                    cmdUpdP.Parameters.AddWithValue("@IdRec", c.IdRecepcion);
                    await cmdUpdP.ExecuteNonQueryAsync();
                }

                contCompras++;
            }

            // 3. CENTRALIZAR MERMAS DE INVENTARIO NO CONTABILIZADAS
            var mermasPend = new List<(int IdMerma, DateTime Fecha, string Motivo, decimal Costo, string Producto, string Lote)>();
            const string qMermas = @"
                SELECT m.id_merma, DATE(m.fecha_merma) AS fecha, m.motivo, m.costo_total_perdida,
                       p.nombre_comercial, l.numero_lote
                FROM registro_mermas m
                INNER JOIN productos p ON p.id_producto = m.id_producto
                INNER JOIN lotes l ON l.id_lote = m.id_lote
                WHERE NOT EXISTS (
                    SELECT 1 FROM libro_diario_asientos a
                    WHERE a.glosa_descripcion LIKE CONCAT('%[MERMA #', m.id_merma, ']%')
                )
                ORDER BY m.fecha_merma ASC;";

            using (var cmdM = new MySqlCommand(qMermas, conexion, tx))
            using (var rM = await cmdM.ExecuteReaderAsync())
            {
                while (await rM.ReadAsync())
                {
                    mermasPend.Add((
                        rM.GetInt32("id_merma"),
                        rM.GetDateTime("fecha"),
                        rM.GetString("motivo"),
                        rM.GetDecimal("costo_total_perdida"),
                        Formato.SanitizarTexto(rM.GetString("nombre_comercial")),
                        rM.GetString("numero_lote")
                    ));
                }
            }

            foreach (var m in mermasPend)
            {
                decimal costo = Math.Round(m.Costo, 2);
                if (costo <= 0) continue;

                var lineas = new List<(int IdCuenta, decimal Debe, decimal Haber)>
                {
                    (idGastoMermas, costo, 0),
                    (idInventario, 0, costo)
                };

                string glosa = $"Castigo Inventario por {m.Motivo}: {m.Producto} Lote {m.Lote} [MERMA #{m.IdMerma}]";
                await InsertarAsientoTransaccionalAsync(conexion, tx, m.Fecha, glosa, "AJUSTE", idUsuario, lineas);
                contMermas++;
            }

            await tx.CommitAsync();
            return (contVentas, contCompras, contMermas);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // =========================================================================
    //  RF-CON-07: GESTIÓN DE CUENTAS POR COBRAR Y PAGAR (CON PAGOS Y ASIENTOS)
    // =========================================================================

    public async Task<List<CuentaPorCobrar>> ObtenerCuentasPorCobrarAsync()
    {
        var lista = new List<CuentaPorCobrar>();
        using var conexion = _dbService.CrearConexion();
        await conexion.OpenAsync();

        const string query = @"
            SELECT c.id_cuenta_cobrar, c.id_venta, c.id_cliente,
                   v.folio_documento, v.tipo_documento,
                   cl.razon_social_nombre, cl.rut_cliente,
                   IFNULL(cv.nombre_convenio, '') AS nombre_convenio,
                   DATE(v.fecha_hora_emision) AS fecha_emision,
                   c.fecha_vencimiento_pago, v.monto_total, c.monto_pendiente
            FROM cuentas_por_cobrar c
            INNER JOIN ventas_encabezado v ON v.id_venta = c.id_venta
            INNER JOIN clientes cl ON cl.id_cliente = c.id_cliente
            LEFT JOIN convenios_salud cv ON cv.id_convenio = c.id_convenio
            WHERE c.estado_cobro <> 'ANULADA'
            ORDER BY (c.monto_pendiente > 0) DESC, c.fecha_vencimiento_pago ASC, v.folio_documento;";

        using var comando = new MySqlCommand(query, conexion);
        using var lector = await comando.ExecuteReaderAsync();

        while (await lector.ReadAsync())
        {
            var total = lector.GetDecimal("monto_total");
            var pendiente = lector.GetDecimal("monto_pendiente");
            lista.Add(new CuentaPorCobrar
            {
                IdDocumento = lector.GetInt32("id_cuenta_cobrar"),
                IdVenta = lector.GetInt32("id_venta"),
                IdCliente = lector.GetInt32("id_cliente"),
                NumeroDocumento = lector.GetString("folio_documento"),
                TipoDocumento = lector.GetString("tipo_documento"),
                Cliente = Formato.SanitizarTexto(lector.GetString("razon_social_nombre")),
                RutCliente = lector.GetString("rut_cliente"),
                Convenio = Formato.SanitizarTexto(lector.GetString("nombre_convenio")),
                FechaEmision = lector.GetDateTime("fecha_emision"),
                FechaVencimiento = lector.GetDateTime("fecha_vencimiento_pago"),
                MontoTotal = total,
                MontoPagado = Math.Max(0, total - pendiente)
            });
        }

        return lista;
    }

    public async Task<List<CuentaPorPagar>> ObtenerCuentasPorPagarAsync()
    {
        var lista = new List<CuentaPorPagar>();
        using var conexion = _dbService.CrearConexion();
        await conexion.OpenAsync();

        const string query = @"
            SELECT p.id_cuenta_pagar, p.id_recepcion, p.id_proveedor,
                   p.numero_documento, pr.razon_social, pr.rut_proveedor,
                   IFNULL(pr.condiciones_comerciales, '') AS condiciones,
                   p.fecha_emision, p.fecha_vencimiento, p.monto_total, p.monto_pagado
            FROM cuentas_por_pagar p
            INNER JOIN proveedores pr ON pr.id_proveedor = p.id_proveedor
            ORDER BY (p.monto_total - p.monto_pagado > 0) DESC, p.fecha_vencimiento ASC, p.numero_documento;";

        using var comando = new MySqlCommand(query, conexion);
        using var lector = await comando.ExecuteReaderAsync();

        while (await lector.ReadAsync())
        {
            lista.Add(new CuentaPorPagar
            {
                IdDocumento = lector.GetInt32("id_cuenta_pagar"),
                IdRecepcion = lector.GetInt32("id_recepcion"),
                IdProveedor = lector.GetInt32("id_proveedor"),
                NumeroDocumento = lector.GetString("numero_documento"),
                Proveedor = Formato.SanitizarTexto(lector.GetString("razon_social")),
                RutProveedor = lector.GetString("rut_proveedor"),
                CondicionesComerciales = Formato.SanitizarTexto(lector.GetString("condiciones")),
                FechaEmision = lector.GetDateTime("fecha_emision"),
                FechaVencimiento = lector.GetDateTime("fecha_vencimiento"),
                MontoTotal = lector.GetDecimal("monto_total"),
                MontoPagado = lector.GetDecimal("monto_pagado")
            });
        }

        return lista;
    }

    /// <summary>
    /// RF-CON-07: Registra un cobro (total o parcial) de una Cuenta por Cobrar, actualiza el saldo en BD
    /// y genera automáticamente el asiento contable de ingreso en Caja/Banco contra Cuentas por Cobrar.
    /// </summary>
    public async Task RegistrarCobroClienteAsync(int idCuentaCobrar, decimal montoAbono, string medioPago, int idUsuario)
    {
        if (montoAbono <= 0)
            throw new InvalidOperationException("El monto a cobrar debe ser mayor a $0.");

        await AsegurarPlanCuentasChilenoAsync();

        using var conexion = _dbService.CrearConexion();
        await conexion.OpenAsync();
        using var tx = await conexion.BeginTransactionAsync();

        try
        {
            decimal pendienteActual;
            int idVenta;
            string folio, cliente;

            const string qDoc = @"
                SELECT c.monto_pendiente, c.id_venta, v.folio_documento, cl.razon_social_nombre
                FROM cuentas_por_cobrar c
                INNER JOIN ventas_encabezado v ON v.id_venta = c.id_venta
                INNER JOIN clientes cl ON cl.id_cliente = c.id_cliente
                WHERE c.id_cuenta_cobrar = @Id FOR UPDATE;";

            using (var cmd = new MySqlCommand(qDoc, conexion, tx))
            {
                cmd.Parameters.AddWithValue("@Id", idCuentaCobrar);
                using var r = await cmd.ExecuteReaderAsync();
                if (!await r.ReadAsync())
                    throw new InvalidOperationException("No se encontró el documento por cobrar.");

                pendienteActual = r.GetDecimal("monto_pendiente");
                idVenta = r.GetInt32("id_venta");
                folio = r.GetString("folio_documento");
                cliente = Formato.SanitizarTexto(r.GetString("razon_social_nombre"));
            }

            if (montoAbono > pendienteActual)
                throw new InvalidOperationException($"El abono ({Formato.Moneda(montoAbono)}) supera el saldo pendiente ({Formato.Moneda(pendienteActual)}).");

            decimal nuevoPendiente = Math.Max(0, pendienteActual - montoAbono);
            string nuevoEstado = nuevoPendiente == 0 ? "PAGADA" : "PENDIENTE";

            const string updCxc = @"
                UPDATE cuentas_por_cobrar
                SET monto_pendiente = @NuevoPend,
                    estado_cobro = @Estado,
                    dias_morosidad = CASE WHEN @NuevoPend = 0 THEN 0 ELSE dias_morosidad END
                WHERE id_cuenta_cobrar = @Id;";
            using (var cmdUpd = new MySqlCommand(updCxc, conexion, tx))
            {
                cmdUpd.Parameters.AddWithValue("@NuevoPend", nuevoPendiente);
                cmdUpd.Parameters.AddWithValue("@Estado", nuevoEstado);
                cmdUpd.Parameters.AddWithValue("@Id", idCuentaCobrar);
                await cmdUpd.ExecuteNonQueryAsync();
            }

            if (nuevoPendiente == 0)
            {
                using var cmdV = new MySqlCommand("UPDATE ventas_encabezado SET estado_documento = 'PAGADO' WHERE id_venta = @IdVenta;", conexion, tx);
                cmdV.Parameters.AddWithValue("@IdVenta", idVenta);
                await cmdV.ExecuteNonQueryAsync();
            }

            // Obtener cuentas para el asiento automático
            int idCuentaTes = await ObtenerIdCuentaPorCodigoAsync(conexion, tx, medioPago is "TRANSFERENCIA" or "TARJETA" ? "1.1.05" : "1.1.01");
            int idCuentaCxc = await ObtenerIdCuentaPorCodigoAsync(conexion, tx, "1.1.03");

            string glosa = $"Cobro cliente {cliente} · Doc. {folio} ({medioPago})";
            await InsertarAsientoTransaccionalAsync(conexion, tx, DateTime.Today, glosa, "VENTA", idUsuario,
                new List<(int, decimal, decimal)>
                {
                    (idCuentaTes, montoAbono, 0),
                    (idCuentaCxc, 0, montoAbono)
                });

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// RF-CON-07: Registra un pago (total o abono parcial) a un proveedor en Cuentas por Pagar,
    /// actualiza automáticamente el saldo y genera el asiento contable en el Libro Diario.
    /// </summary>
    public async Task RegistrarPagoProveedorAsync(int idCuentaPagar, decimal montoAbono, string medioPago, int idUsuario)
    {
        if (montoAbono <= 0)
            throw new InvalidOperationException("El monto a pagar debe ser mayor a $0.");

        await AsegurarPlanCuentasChilenoAsync();

        using var conexion = _dbService.CrearConexion();
        await conexion.OpenAsync();
        using var tx = await conexion.BeginTransactionAsync();

        try
        {
            decimal montoTotal, montoPagado;
            string numeroDoc, proveedor;

            const string qDoc = @"
                SELECT p.monto_total, p.monto_pagado, p.numero_documento, pr.razon_social
                FROM cuentas_por_pagar p
                INNER JOIN proveedores pr ON pr.id_proveedor = p.id_proveedor
                WHERE p.id_cuenta_pagar = @Id FOR UPDATE;";

            using (var cmd = new MySqlCommand(qDoc, conexion, tx))
            {
                cmd.Parameters.AddWithValue("@Id", idCuentaPagar);
                using var r = await cmd.ExecuteReaderAsync();
                if (!await r.ReadAsync())
                    throw new InvalidOperationException("No se encontró la obligación por pagar.");

                montoTotal = r.GetDecimal("monto_total");
                montoPagado = r.GetDecimal("monto_pagado");
                numeroDoc = r.GetString("numero_documento");
                proveedor = Formato.SanitizarTexto(r.GetString("razon_social"));
            }

            decimal saldoPendiente = montoTotal - montoPagado;
            if (montoAbono > saldoPendiente)
                throw new InvalidOperationException($"El monto a pagar ({Formato.Moneda(montoAbono)}) supera el saldo pendiente ({Formato.Moneda(saldoPendiente)}).");

            decimal nuevoPagado = montoPagado + montoAbono;
            string nuevoEstado = nuevoPagado >= montoTotal ? "PAGADO" : "PENDIENTE";

            const string updCxp = @"
                UPDATE cuentas_por_pagar
                SET monto_pagado = @NuevoPagado,
                    estado_pago = @Estado
                WHERE id_cuenta_pagar = @Id;";
            using (var cmdUpd = new MySqlCommand(updCxp, conexion, tx))
            {
                cmdUpd.Parameters.AddWithValue("@NuevoPagado", nuevoPagado);
                cmdUpd.Parameters.AddWithValue("@Estado", nuevoEstado);
                cmdUpd.Parameters.AddWithValue("@Id", idCuentaPagar);
                await cmdUpd.ExecuteNonQueryAsync();
            }

            int idCuentaCxp = await ObtenerIdCuentaPorCodigoAsync(conexion, tx, "2.1.01");
            int idCuentaTes = await ObtenerIdCuentaPorCodigoAsync(conexion, tx, medioPago is "TRANSFERENCIA" or "CHEQUE" ? "1.1.05" : "1.1.01");

            string glosa = $"Pago a proveedor {proveedor} · Factura {numeroDoc} ({medioPago})";
            await InsertarAsientoTransaccionalAsync(conexion, tx, DateTime.Today, glosa, "COMPRA", idUsuario,
                new List<(int, decimal, decimal)>
                {
                    (idCuentaCxp, montoAbono, 0),
                    (idCuentaTes, 0, montoAbono)
                });

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private static async Task<int> ObtenerIdCuentaPorCodigoAsync(MySqlConnection cn, MySqlTransaction tx, string codigo)
    {
        using var cmd = new MySqlCommand("SELECT id_cuenta FROM plan_cuentas WHERE codigo_cuenta = @Cod LIMIT 1;", cn, tx);
        cmd.Parameters.AddWithValue("@Cod", codigo);
        var val = await cmd.ExecuteScalarAsync();
        if (val != null && val != DBNull.Value)
            return Convert.ToInt32(val);

        using var cmdFallback = new MySqlCommand("SELECT id_cuenta FROM plan_cuentas WHERE codigo_cuenta = '1.1.01' LIMIT 1;", cn, tx);
        return Convert.ToInt32(await cmdFallback.ExecuteScalarAsync() ?? 1);
    }

    // =========================================================================
    //  RF-CON-03, RF-CON-04, RF-CON-06: LIBRO MAYOR, BALANCE 8 COLUMNAS, MAYOR CENTRALIZADO
    // =========================================================================

    public async Task<List<LibroMayorItem>> ObtenerLibroMayorAsync(DateTime? desde = null, DateTime? hasta = null)
    {
        var lista = new List<LibroMayorItem>();
        using var conexion = _dbService.CrearConexion();
        await conexion.OpenAsync();

        var query = @"
            SELECT 
                p.id_cuenta, p.codigo_cuenta, p.nombre_cuenta, p.tipo_cuenta,
                IFNULL(SUM(d.debe), 0) AS total_debe,
                IFNULL(SUM(d.haber), 0) AS total_haber
            FROM plan_cuentas p
            INNER JOIN detalle_asiento_contable d ON p.id_cuenta = d.id_cuenta
            INNER JOIN libro_diario_asientos l ON l.id_asiento = d.id_asiento
            WHERE 1=1";

        if (desde.HasValue) query += " AND l.fecha_asiento >= @Desde";
        if (hasta.HasValue) query += " AND l.fecha_asiento <= @Hasta";

        query += @"
            GROUP BY p.id_cuenta, p.codigo_cuenta, p.nombre_cuenta, p.tipo_cuenta
            HAVING total_debe > 0 OR total_haber > 0
            ORDER BY p.codigo_cuenta;";

        using var comando = new MySqlCommand(query, conexion);
        if (desde.HasValue) comando.Parameters.AddWithValue("@Desde", desde.Value.Date);
        if (hasta.HasValue) comando.Parameters.AddWithValue("@Hasta", hasta.Value.Date.AddDays(1).AddTicks(-1));

        using var lector = await comando.ExecuteReaderAsync();

        while (await lector.ReadAsync())
        {
            var item = new LibroMayorItem
            {
                IdCuenta = lector.GetInt32("id_cuenta"),
                CodigoCuenta = lector.GetString("codigo_cuenta"),
                NombreCuenta = Formato.SanitizarTexto(lector.GetString("nombre_cuenta")),
                TipoCuenta = lector.GetString("tipo_cuenta"),
                TotalDebe = lector.GetDecimal("total_debe"),
                TotalHaber = lector.GetDecimal("total_haber")
            };

            decimal diferencia = item.TotalDebe - item.TotalHaber;
            if (diferencia > 0)
            {
                item.SaldoDeudor = diferencia;
                item.SaldoAcreedor = 0;
            }
            else if (diferencia < 0)
            {
                item.SaldoDeudor = 0;
                item.SaldoAcreedor = Math.Abs(diferencia);
            }

            lista.Add(item);
        }

        return lista;
    }

    public async Task<List<BalanceOchoColumnasItem>> ObtenerBalanceOchoColumnasAsync(DateTime? desde = null, DateTime? hasta = null)
    {
        var lista = new List<BalanceOchoColumnasItem>();
        using var conexion = _dbService.CrearConexion();
        await conexion.OpenAsync();

        var query = @"
            SELECT 
                p.id_cuenta, p.codigo_cuenta, p.nombre_cuenta, p.tipo_cuenta,
                IFNULL(SUM(d.debe), 0) AS total_debe,
                IFNULL(SUM(d.haber), 0) AS total_haber
            FROM plan_cuentas p
            INNER JOIN detalle_asiento_contable d ON p.id_cuenta = d.id_cuenta
            INNER JOIN libro_diario_asientos l ON l.id_asiento = d.id_asiento
            WHERE 1=1";

        if (desde.HasValue) query += " AND l.fecha_asiento >= @Desde";
        if (hasta.HasValue) query += " AND l.fecha_asiento <= @Hasta";

        query += @"
            GROUP BY p.id_cuenta, p.codigo_cuenta, p.nombre_cuenta, p.tipo_cuenta
            HAVING total_debe > 0 OR total_haber > 0
            ORDER BY p.codigo_cuenta;";

        using var comando = new MySqlCommand(query, conexion);
        if (desde.HasValue) comando.Parameters.AddWithValue("@Desde", desde.Value.Date);
        if (hasta.HasValue) comando.Parameters.AddWithValue("@Hasta", hasta.Value.Date.AddDays(1).AddTicks(-1));

        using var lector = await comando.ExecuteReaderAsync();

        while (await lector.ReadAsync())
        {
            var item = new BalanceOchoColumnasItem
            {
                IdCuenta = lector.GetInt32("id_cuenta"),
                CodigoCuenta = lector.GetString("codigo_cuenta"),
                NombreCuenta = Formato.SanitizarTexto(lector.GetString("nombre_cuenta")),
                TipoCuenta = lector.GetString("tipo_cuenta"),
                TotalDebe = lector.GetDecimal("total_debe"),
                TotalHaber = lector.GetDecimal("total_haber")
            };

            decimal diferencia = item.TotalDebe - item.TotalHaber;
            if (diferencia > 0)
                item.SaldoDeudor = diferencia;
            else if (diferencia < 0)
                item.SaldoAcreedor = Math.Abs(diferencia);

            // Clasificación tributaria chilena de 8 Columnas (SII)
            switch (item.TipoCuenta.ToUpperInvariant())
            {
                case "ACTIVO":
                    item.Activo = item.SaldoDeudor > 0 ? item.SaldoDeudor : 0;
                    item.Pasivo = item.SaldoAcreedor > 0 ? item.SaldoAcreedor : 0;
                    break;
                case "PASIVO":
                case "PATRIMONIO":
                    item.Pasivo = item.SaldoAcreedor > 0 ? item.SaldoAcreedor : 0;
                    item.Activo = item.SaldoDeudor > 0 ? item.SaldoDeudor : 0;
                    break;
                case "GASTO":
                    item.Perdida = item.SaldoDeudor > 0 ? item.SaldoDeudor : 0;
                    item.Ganancia = item.SaldoAcreedor > 0 ? item.SaldoAcreedor : 0;
                    break;
                case "INGRESO":
                    item.Ganancia = item.SaldoAcreedor > 0 ? item.SaldoAcreedor : 0;
                    item.Perdida = item.SaldoDeudor > 0 ? item.SaldoDeudor : 0;
                    break;
            }

            lista.Add(item);
        }

        return lista;
    }

    public async Task<List<MayorCentralizadoItem>> ObtenerMayorCentralizadoAsync(int? idCuenta, DateTime fechaDesde, DateTime fechaHasta)
    {
        var lista = new List<MayorCentralizadoItem>();
        using var conexion = _dbService.CrearConexion();
        await conexion.OpenAsync();

        string query = @"
            SELECT 
                l.fecha_asiento,
                l.numero_asiento,
                l.origen,
                l.glosa_descripcion,
                p.codigo_cuenta,
                p.nombre_cuenta,
                p.tipo_cuenta,
                d.debe,
                d.haber
            FROM detalle_asiento_contable d
            INNER JOIN libro_diario_asientos l ON d.id_asiento = l.id_asiento
            INNER JOIN plan_cuentas p ON d.id_cuenta = p.id_cuenta
            WHERE l.fecha_asiento >= @FechaDesde 
              AND l.fecha_asiento <= @FechaHasta ";

        if (idCuenta.HasValue && idCuenta.Value > 0)
        {
            query += " AND p.id_cuenta = @IdCuenta ";
        }

        query += " ORDER BY p.codigo_cuenta ASC, l.fecha_asiento ASC, l.numero_asiento ASC;";

        using var comando = new MySqlCommand(query, conexion);
        comando.Parameters.AddWithValue("@FechaDesde", fechaDesde.Date);
        comando.Parameters.AddWithValue("@FechaHasta", fechaHasta.Date.AddDays(1).AddTicks(-1));
        
        if (idCuenta.HasValue && idCuenta.Value > 0)
        {
            comando.Parameters.AddWithValue("@IdCuenta", idCuenta.Value);
        }

        using var lector = await comando.ExecuteReaderAsync();
        
        decimal saldoAcumulado = 0;
        string cuentaActual = "";

        while (await lector.ReadAsync())
        {
            var codigoCuenta = lector.GetString("codigo_cuenta");
            var tipoCuenta = lector.GetString("tipo_cuenta");
            
            if (codigoCuenta != cuentaActual)
            {
                saldoAcumulado = 0;
                cuentaActual = codigoCuenta;
            }

            var debe = lector.GetDecimal("debe");
            var haber = lector.GetDecimal("haber");
            bool esDeudora = EstiloContable.EsNaturalezaDeudora(tipoCuenta);

            saldoAcumulado += esDeudora ? (debe - haber) : (haber - debe);

            lista.Add(new MayorCentralizadoItem
            {
                Fecha = lector.GetDateTime("fecha_asiento"),
                NumeroAsiento = lector.GetInt32("numero_asiento"),
                Origen = lector.GetString("origen"),
                Glosa = lector.IsDBNull(lector.GetOrdinal("glosa_descripcion")) ? "" : Formato.SanitizarTexto(lector.GetString("glosa_descripcion")),
                CodigoCuenta = codigoCuenta,
                NombreCuenta = Formato.SanitizarTexto(lector.GetString("nombre_cuenta")),
                TipoCuenta = tipoCuenta,
                Debe = debe,
                Haber = haber,
                SaldoAcumulado = saldoAcumulado
            });
        }

        return lista;
    }

    // =========================================================================
    //  RF-CON-09: REPORTE CENTRALIZADOR DE IMPUESTOS (LIBRO COMPRAS/VENTAS + F29)
    // =========================================================================

    public async Task<(List<DocumentoLibroVentasItem> Ventas, List<DocumentoLibroComprasItem> Compras)> ObtenerLibrosImpuestosAsync(DateTime desde, DateTime hasta)
    {
        var ventas = new List<DocumentoLibroVentasItem>();
        var compras = new List<DocumentoLibroComprasItem>();

        using var conexion = _dbService.CrearConexion();
        await conexion.OpenAsync();

        // 1. Libro de Ventas (excluye documentos anulados del cálculo tributario)
        const string qVentas = @"
            SELECT v.id_venta, v.folio_documento, v.tipo_documento, v.fecha_hora_emision,
                   cl.rut_cliente, cl.razon_social_nombre,
                   v.monto_descuento, v.monto_neto, v.monto_iva, v.monto_total,
                   v.forma_pago, v.estado_documento
            FROM ventas_encabezado v
            INNER JOIN clientes cl ON cl.id_cliente = v.id_cliente
            WHERE v.fecha_hora_emision BETWEEN @Desde AND @Hasta
              AND v.estado_documento <> 'ANULADO'
            ORDER BY v.fecha_hora_emision ASC, v.folio_documento ASC;";

        using (var cmdV = new MySqlCommand(qVentas, conexion))
        {
            cmdV.Parameters.AddWithValue("@Desde", desde.Date);
            cmdV.Parameters.AddWithValue("@Hasta", hasta.Date.AddDays(1).AddTicks(-1));
            using var r = await cmdV.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                decimal total = r.GetDecimal("monto_total");
                decimal neto = r.GetDecimal("monto_neto");
                decimal iva = r.GetDecimal("monto_iva");

                // Normalizar neto + IVA sobre monto total efectivamente facturado (post-descuento convenio)
                if (Math.Abs((neto + iva) - total) > 1m && total > 0)
                {
                    neto = Math.Round(total / (1m + TasaIvaChile), 0);
                    iva = Math.Round(total - neto, 0);
                }

                ventas.Add(new DocumentoLibroVentasItem
                {
                    IdVenta = r.GetInt32("id_venta"),
                    Folio = r.GetString("folio_documento"),
                    TipoDocumento = r.GetString("tipo_documento"),
                    FechaEmision = r.GetDateTime("fecha_hora_emision"),
                    RutCliente = r.GetString("rut_cliente"),
                    RazonSocial = Formato.SanitizarTexto(r.GetString("razon_social_nombre")),
                    MontoDescuento = r.GetDecimal("monto_descuento"),
                    MontoNeto = neto,
                    MontoIva = iva,
                    MontoTotal = total,
                    FormaPago = r.GetString("forma_pago"),
                    Estado = r.GetString("estado_documento")
                });
            }
        }

        // 2. Libro de Compras (Facturas de proveedores recepcionadas en el período)
        const string qCompras = @"
            SELECT r.id_recepcion, r.numero_factura_proveedor, r.fecha_factura, r.monto_total_facturado,
                   oc.numero_oc, pr.rut_proveedor, pr.razon_social,
                   IFNULL(cp.estado_pago, 'PENDIENTE') AS estado_pago
            FROM recepciones_facturas_compra r
            INNER JOIN ordenes_compra oc ON oc.id_orden_compra = r.id_orden_compra
            INNER JOIN proveedores pr ON pr.id_proveedor = oc.id_proveedor
            LEFT JOIN cuentas_por_pagar cp ON cp.id_recepcion = r.id_recepcion
            WHERE r.fecha_factura BETWEEN @Desde AND @Hasta
            ORDER BY r.fecha_factura ASC, r.numero_factura_proveedor ASC;";

        using (var cmdC = new MySqlCommand(qCompras, conexion))
        {
            cmdC.Parameters.AddWithValue("@Desde", desde.Date);
            cmdC.Parameters.AddWithValue("@Hasta", hasta.Date.AddDays(1).AddTicks(-1));
            using var r = await cmdC.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                decimal total = r.GetDecimal("monto_total_facturado");
                decimal neto = Math.Round(total / (1m + TasaIvaChile), 0);
                decimal iva = Math.Round(total - neto, 0);

                compras.Add(new DocumentoLibroComprasItem
                {
                    IdRecepcion = r.GetInt32("id_recepcion"),
                    NumeroFactura = r.GetString("numero_factura_proveedor"),
                    FechaFactura = r.GetDateTime("fecha_factura"),
                    NumeroOrdenCompra = r.GetString("numero_oc"),
                    RutProveedor = r.GetString("rut_proveedor"),
                    RazonSocial = Formato.SanitizarTexto(r.GetString("razon_social")),
                    MontoNeto = neto,
                    MontoIvaCredito = iva,
                    MontoTotal = total,
                    EstadoPago = r.GetString("estado_pago")
                });
            }
        }

        return (ventas, compras);
    }
}
