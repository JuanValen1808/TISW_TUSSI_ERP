using MySqlConnector;
using System.Collections.Generic;
using System.Threading.Tasks;
using TISW_TUSSI_ERP.Models.Contabilidad;

namespace TISW_TUSSI_ERP.Services.Api;

public class ContabilidadService
{
    private readonly DatabaseService _dbService;

    public ContabilidadService(DatabaseService dbService)
    {
        _dbService = dbService;
    }

    public async Task<List<PlanCuentas>> ObtenerPlanCuentasAsync()
    {
        var lista = new List<PlanCuentas>();
        using var conexion = _dbService.CrearConexion();
        await conexion.OpenAsync();

        const string query = @"
            SELECT id_cuenta, codigo_cuenta, nombre_cuenta, tipo_cuenta, nivel, cuenta_padre_id
            FROM plan_cuentas
            ORDER BY codigo_cuenta;";

        using var comando = new MySqlCommand(query, conexion);
        using var lector = await comando.ExecuteReaderAsync();

        while (await lector.ReadAsync())
        {
            lista.Add(new PlanCuentas
            {
                IdCuenta = lector.GetInt32("id_cuenta"),
                CodigoCuenta = lector.GetString("codigo_cuenta"),
                NombreCuenta = lector.GetString("nombre_cuenta"),
                TipoCuenta = lector.GetString("tipo_cuenta"),
                Nivel = lector.GetInt32("nivel"),
                CuentaPadreId = lector.IsDBNull(lector.GetOrdinal("cuenta_padre_id")) ? null : lector.GetInt32("cuenta_padre_id")
            });
        }

        return lista;
    }

    public async Task<List<LibroDiarioAsiento>> ObtenerLibroDiarioAsync()
    {
        var lista = new List<LibroDiarioAsiento>();
        using var conexion = _dbService.CrearConexion();
        await conexion.OpenAsync();

        const string query = @"
            SELECT l.id_asiento, l.numero_asiento, l.fecha_asiento, l.glosa_descripcion, l.origen, l.id_usuario, l.created_at,
                   IFNULL(u.nombre, '') AS usuario,
                   IFNULL(SUM(d.debe), 0) AS total_debe,
                   IFNULL(SUM(d.haber), 0) AS total_haber,
                   COUNT(d.id_detalle_asiento) AS lineas
            FROM libro_diario_asientos l
            LEFT JOIN usuarios u ON u.id_usuario = l.id_usuario
            LEFT JOIN detalle_asiento_contable d ON d.id_asiento = l.id_asiento
            GROUP BY l.id_asiento, l.numero_asiento, l.fecha_asiento, l.glosa_descripcion, l.origen, l.id_usuario, l.created_at, u.nombre
            ORDER BY l.fecha_asiento DESC, l.numero_asiento DESC;";

        using var comando = new MySqlCommand(query, conexion);
        using var lector = await comando.ExecuteReaderAsync();

        while (await lector.ReadAsync())
        {
            lista.Add(new LibroDiarioAsiento
            {
                IdAsiento = lector.GetInt32("id_asiento"),
                NumeroAsiento = lector.GetInt32("numero_asiento"),
                FechaAsiento = lector.GetDateTime("fecha_asiento"),
                GlosaDescripcion = lector.IsDBNull(lector.GetOrdinal("glosa_descripcion")) ? string.Empty : lector.GetString("glosa_descripcion"),
                Origen = lector.GetString("origen"),
                IdUsuario = lector.GetInt32("id_usuario"),
                CreatedAt = lector.GetDateTime("created_at"),
                Usuario = lector.GetString("usuario"),
                TotalDebe = lector.GetDecimal("total_debe"),
                TotalHaber = lector.GetDecimal("total_haber"),
                CantidadLineas = Convert.ToInt32(lector["lineas"])
            });
        }

        return lista;
    }

    // Líneas (cuenta / debe / haber) de un asiento. Primero el Debe y luego el Haber, como en el formato clásico.
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
                NombreCuenta = lector.GetString("nombre_cuenta"),
                Debe = lector.GetDecimal("debe"),
                Haber = lector.GetDecimal("haber")
            });
        }

        return lista;
    }

    // Cuentas por cobrar reales (excluye anuladas). Solo lectura.
    public async Task<List<CuentaPorCobrar>> ObtenerCuentasPorCobrarAsync()
    {
        var lista = new List<CuentaPorCobrar>();
        using var conexion = _dbService.CrearConexion();
        await conexion.OpenAsync();

        const string query = @"
            SELECT c.id_cuenta_cobrar, v.folio_documento, v.tipo_documento,
                   cl.razon_social_nombre, cl.rut_cliente,
                   IFNULL(cv.nombre_convenio, '') AS nombre_convenio,
                   DATE(v.fecha_hora_emision) AS fecha_emision,
                   c.fecha_vencimiento_pago, v.monto_total, c.monto_pendiente
            FROM cuentas_por_cobrar c
            INNER JOIN ventas_encabezado v ON v.id_venta = c.id_venta
            INNER JOIN clientes cl ON cl.id_cliente = c.id_cliente
            LEFT JOIN convenios_salud cv ON cv.id_convenio = c.id_convenio
            WHERE c.estado_cobro <> 'ANULADA'
            ORDER BY c.fecha_vencimiento_pago, v.folio_documento;";

        using var comando = new MySqlCommand(query, conexion);
        using var lector = await comando.ExecuteReaderAsync();

        while (await lector.ReadAsync())
        {
            var total = lector.GetDecimal("monto_total");
            var pendiente = lector.GetDecimal("monto_pendiente");
            lista.Add(new CuentaPorCobrar
            {
                IdDocumento = lector.GetInt32("id_cuenta_cobrar"),
                NumeroDocumento = lector.GetString("folio_documento"),
                TipoDocumento = lector.GetString("tipo_documento"),
                Cliente = lector.GetString("razon_social_nombre"),
                RutCliente = lector.GetString("rut_cliente"),
                Convenio = lector.GetString("nombre_convenio"),
                FechaEmision = lector.GetDateTime("fecha_emision"),
                FechaVencimiento = lector.GetDateTime("fecha_vencimiento_pago"),
                MontoTotal = total,
                MontoPagado = Math.Max(0, total - pendiente)
            });
        }

        return lista;
    }

    // Cuentas por pagar reales a proveedores. Solo lectura.
    public async Task<List<CuentaPorPagar>> ObtenerCuentasPorPagarAsync()
    {
        var lista = new List<CuentaPorPagar>();
        using var conexion = _dbService.CrearConexion();
        await conexion.OpenAsync();

        const string query = @"
            SELECT p.id_cuenta_pagar, p.numero_documento, pr.razon_social, pr.rut_proveedor,
                   p.fecha_emision, p.fecha_vencimiento, p.monto_total, p.monto_pagado
            FROM cuentas_por_pagar p
            INNER JOIN proveedores pr ON pr.id_proveedor = p.id_proveedor
            ORDER BY p.fecha_vencimiento, p.numero_documento;";

        using var comando = new MySqlCommand(query, conexion);
        using var lector = await comando.ExecuteReaderAsync();

        while (await lector.ReadAsync())
        {
            lista.Add(new CuentaPorPagar
            {
                IdDocumento = lector.GetInt32("id_cuenta_pagar"),
                NumeroDocumento = lector.GetString("numero_documento"),
                Proveedor = lector.GetString("razon_social"),
                RutProveedor = lector.GetString("rut_proveedor"),
                FechaEmision = lector.GetDateTime("fecha_emision"),
                FechaVencimiento = lector.GetDateTime("fecha_vencimiento"),
                MontoTotal = lector.GetDecimal("monto_total"),
                MontoPagado = lector.GetDecimal("monto_pagado")
            });
        }

        return lista;
    }

    public async Task<List<LibroMayorItem>> ObtenerLibroMayorAsync()
    {
        var lista = new List<LibroMayorItem>();
        using var conexion = _dbService.CrearConexion();
        await conexion.OpenAsync();

        // Consulta que agrupa los movimientos por cuenta contable
        const string query = @"
            SELECT 
                p.id_cuenta, p.codigo_cuenta, p.nombre_cuenta, p.tipo_cuenta,
                IFNULL(SUM(d.debe), 0) AS total_debe,
                IFNULL(SUM(d.haber), 0) AS total_haber
            FROM plan_cuentas p
            LEFT JOIN detalle_asiento_contable d ON p.id_cuenta = d.id_cuenta
            GROUP BY p.id_cuenta, p.codigo_cuenta, p.nombre_cuenta, p.tipo_cuenta
            HAVING total_debe > 0 OR total_haber > 0
            ORDER BY p.codigo_cuenta;";

        using var comando = new MySqlCommand(query, conexion);
        using var lector = await comando.ExecuteReaderAsync();

        while (await lector.ReadAsync())
        {
            var item = new LibroMayorItem
            {
                IdCuenta = lector.GetInt32("id_cuenta"),
                CodigoCuenta = lector.GetString("codigo_cuenta"),
                NombreCuenta = lector.GetString("nombre_cuenta"),
                TipoCuenta = lector.GetString("tipo_cuenta"),
                TotalDebe = lector.GetDecimal("total_debe"),
                TotalHaber = lector.GetDecimal("total_haber")
            };

            // Cálculo del saldo basado en Debe y Haber (norma contable básica de cuadratura)
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
            else
            {
                item.SaldoDeudor = 0;
                item.SaldoAcreedor = 0;
            }

            lista.Add(item);
        }

        return lista;
    }

    public async Task<List<BalanceOchoColumnasItem>> ObtenerBalanceOchoColumnasAsync()
    {
        var lista = new List<BalanceOchoColumnasItem>();
        using var conexion = _dbService.CrearConexion();
        await conexion.OpenAsync();

        const string query = @"
            SELECT 
                p.id_cuenta, p.codigo_cuenta, p.nombre_cuenta, p.tipo_cuenta,
                IFNULL(SUM(d.debe), 0) AS total_debe,
                IFNULL(SUM(d.haber), 0) AS total_haber
            FROM plan_cuentas p
            LEFT JOIN detalle_asiento_contable d ON p.id_cuenta = d.id_cuenta
            GROUP BY p.id_cuenta, p.codigo_cuenta, p.nombre_cuenta, p.tipo_cuenta
            HAVING total_debe > 0 OR total_haber > 0
            ORDER BY p.codigo_cuenta;";

        using var comando = new MySqlCommand(query, conexion);
        using var lector = await comando.ExecuteReaderAsync();

        while (await lector.ReadAsync())
        {
            var item = new BalanceOchoColumnasItem
            {
                IdCuenta = lector.GetInt32("id_cuenta"),
                CodigoCuenta = lector.GetString("codigo_cuenta"),
                NombreCuenta = lector.GetString("nombre_cuenta"),
                TipoCuenta = lector.GetString("tipo_cuenta"),
                TotalDebe = lector.GetDecimal("total_debe"),
                TotalHaber = lector.GetDecimal("total_haber")
            };

            // Cálculo de saldos (Deudor / Acreedor)
            decimal diferencia = item.TotalDebe - item.TotalHaber;
            if (diferencia > 0)
                item.SaldoDeudor = diferencia;
            else if (diferencia < 0)
                item.SaldoAcreedor = Math.Abs(diferencia);

            // Clasificación en 8 columnas basada en el tipo de cuenta chileno
            switch (item.TipoCuenta.ToUpper())
            {
                case "ACTIVO":
                    // El activo toma el saldo deudor. Si tuviera saldo acreedor, iría restando (aquí simplificamos asignando el saldo deudor)
                    item.Activo = item.SaldoDeudor;
                    break;
                case "PASIVO":
                case "PATRIMONIO":
                    item.Pasivo = item.SaldoAcreedor;
                    break;
                case "GASTO":
                    item.Perdida = item.SaldoDeudor;
                    break;
                case "INGRESO":
                    item.Ganancia = item.SaldoAcreedor;
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
                l.glosa_descripcion,
                p.codigo_cuenta,
                p.nombre_cuenta,
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
        // Hasta el final del dia
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
            
            // Reiniciar saldo acumulado si cambia de cuenta (para que el saldo sea por cuenta)
            if (codigoCuenta != cuentaActual)
            {
                saldoAcumulado = 0;
                cuentaActual = codigoCuenta;
            }

            var debe = lector.GetDecimal("debe");
            var haber = lector.GetDecimal("haber");
            
            // Calculo simple matematico de saldo para el kardex de la cuenta
            saldoAcumulado += (debe - haber);

            lista.Add(new MayorCentralizadoItem
            {
                Fecha = lector.GetDateTime("fecha_asiento"),
                NumeroAsiento = lector.GetInt32("numero_asiento"),
                Glosa = lector.IsDBNull(lector.GetOrdinal("glosa_descripcion")) ? "" : lector.GetString("glosa_descripcion"),
                CodigoCuenta = codigoCuenta,
                NombreCuenta = lector.GetString("nombre_cuenta"),
                Debe = debe,
                Haber = haber,
                SaldoAcumulado = saldoAcumulado
            });
        }

        return lista;
    }
}
