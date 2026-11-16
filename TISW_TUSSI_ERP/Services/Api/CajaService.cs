
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySqlConnector;
using TISW_TUSSI_ERP.Models.Ventas;

namespace TISW_TUSSI_ERP.Services.Api;

public class ResumenPagosCaja
{
    public decimal Efectivo { get; set; }
    public decimal Tarjeta { get; set; }
    public decimal Transferencia { get; set; }
    public decimal Credito { get; set; }
    public decimal Otros { get; set; }

    public decimal Total =>
        Efectivo + Tarjeta + Transferencia + Credito + Otros;
}

public class CajaService
{
    private readonly DatabaseService _db;

    public CajaService(DatabaseService db)
    {
        _db = db;
    }

    public async Task<List<Caja>> ObtenerCajasAsync()
    {
        var cajas = new List<Caja>();

        using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        const string sql = @"
            SELECT id_caja, nombre_caja, descripcion, activa
            FROM cajas
            WHERE activa = 1
            ORDER BY nombre_caja;";

        using var cmd = new MySqlCommand(sql, cn);
        using var rd = await cmd.ExecuteReaderAsync();

        while (await rd.ReadAsync())
        {
            cajas.Add(new Caja
            {
                IdCaja = rd.GetInt32("id_caja"),
                NombreCaja = rd.GetString("nombre_caja"),
                Descripcion = rd.IsDBNull(rd.GetOrdinal("descripcion"))
                    ? "" : rd.GetString("descripcion"),
                Activa = rd.GetBoolean("activa")
            });
        }

        return cajas;
    }

    public async Task<CajaTurno?> ObtenerTurnoAbiertoAsync(int idCaja)
    {
        using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        const string sql = @"
            SELECT *
            FROM caja_turnos
            WHERE id_caja = @idCaja AND estado = 'ABIERTA'
            ORDER BY id_turno DESC
            LIMIT 1;";

        using var cmd = new MySqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@idCaja", idCaja);

        using var rd = await cmd.ExecuteReaderAsync();

        if (!await rd.ReadAsync())
            return null;

        return new CajaTurno
        {
            IdTurno = rd.GetInt32("id_turno"),
            IdCaja = rd.GetInt32("id_caja"),
            IdUsuarioApertura = rd.GetInt32("id_usuario_apertura"),
            FechaApertura = rd.GetDateTime("fecha_apertura"),
            MontoInicial = rd.GetDecimal("monto_inicial"),
            Estado = rd.GetString("estado")
        };
    }

    public async Task<int> AbrirCajaAsync(
        int idCaja, int idUsuario, decimal montoInicial)
    {
        if (montoInicial < 0)
            throw new Exception("El monto inicial no puede ser negativo.");

        using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        using var tx = await cn.BeginTransactionAsync();

        try
        {
            const string sqlCaja = @"
                SELECT id_caja FROM cajas
                WHERE id_caja = @idCaja AND activa = 1
                FOR UPDATE;";

            using (var cmd = new MySqlCommand(sqlCaja, cn, tx))
            {
                cmd.Parameters.AddWithValue("@idCaja", idCaja);

                if (await cmd.ExecuteScalarAsync() == null)
                    throw new Exception("La caja no existe o está inactiva.");
            }

            const string sqlVerificar = @"
                SELECT COUNT(*) FROM caja_turnos
                WHERE id_caja = @idCaja AND estado = 'ABIERTA';";

            using (var cmd = new MySqlCommand(sqlVerificar, cn, tx))
            {
                cmd.Parameters.AddWithValue("@idCaja", idCaja);

                int cantidad = Convert.ToInt32(
                    await cmd.ExecuteScalarAsync());

                if (cantidad > 0)
                    throw new Exception("Esta caja ya tiene un turno abierto.");
            }

            const string sql = @"
                INSERT INTO caja_turnos
                    (id_caja, id_usuario_apertura, monto_inicial, estado)
                VALUES
                    (@idCaja, @idUsuario, @montoInicial, 'ABIERTA');";

            using var insertar = new MySqlCommand(sql, cn, tx);
            insertar.Parameters.AddWithValue("@idCaja", idCaja);
            insertar.Parameters.AddWithValue("@idUsuario", idUsuario);
            insertar.Parameters.AddWithValue("@montoInicial", montoInicial);

            await insertar.ExecuteNonQueryAsync();
            int idTurno = Convert.ToInt32(insertar.LastInsertedId);

            await tx.CommitAsync();
            return idTurno;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task RegistrarMovimientoAsync(
        int idTurno,
        int idUsuario,
        string tipo,
        string concepto,
        decimal monto)
    {
        tipo = tipo?.Trim().ToUpperInvariant() ?? "";
        concepto = concepto?.Trim() ?? "";

        if (tipo != "INGRESO" && tipo != "EGRESO")
            throw new Exception("El tipo debe ser INGRESO o EGRESO.");

        if (string.IsNullOrWhiteSpace(concepto))
            throw new Exception("Debes ingresar un concepto.");

        if (monto <= 0)
            throw new Exception("El monto debe ser mayor que cero.");

        using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        using var tx = await cn.BeginTransactionAsync();

        try
        {
            const string sqlTurno = @"
                SELECT estado FROM caja_turnos
                WHERE id_turno = @idTurno FOR UPDATE;";

            using (var cmd = new MySqlCommand(sqlTurno, cn, tx))
            {
                cmd.Parameters.AddWithValue("@idTurno", idTurno);

                string estado = Convert.ToString(
                    await cmd.ExecuteScalarAsync()) ?? "";

                if (estado != "ABIERTA")
                    throw new Exception("El turno no existe o está cerrado.");
            }

            const string sql = @"
                INSERT INTO caja_movimientos
                    (id_turno, id_usuario, tipo_movimiento, concepto, monto)
                VALUES
                    (@idTurno, @idUsuario, @tipo, @concepto, @monto);";

            using var cmdMovimiento = new MySqlCommand(sql, cn, tx);
            cmdMovimiento.Parameters.AddWithValue("@idTurno", idTurno);
            cmdMovimiento.Parameters.AddWithValue("@idUsuario", idUsuario);
            cmdMovimiento.Parameters.AddWithValue("@tipo", tipo);
            cmdMovimiento.Parameters.AddWithValue("@concepto", concepto);
            cmdMovimiento.Parameters.AddWithValue("@monto", monto);

            await cmdMovimiento.ExecuteNonQueryAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<List<MovimientoCaja>> ObtenerMovimientosAsync(
        int idTurno)
    {
        var movimientos = new List<MovimientoCaja>();

        using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        const string sql = @"
            SELECT * FROM caja_movimientos
            WHERE id_turno = @idTurno
            ORDER BY fecha_movimiento DESC, id_movimiento DESC;";

        using var cmd = new MySqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@idTurno", idTurno);

        using var rd = await cmd.ExecuteReaderAsync();

        while (await rd.ReadAsync())
        {
            int ordinalVenta = rd.GetOrdinal("id_venta");

            movimientos.Add(new MovimientoCaja
            {
                IdMovimiento = rd.GetInt32("id_movimiento"),
                IdTurno = rd.GetInt32("id_turno"),
                IdUsuario = rd.GetInt32("id_usuario"),
                TipoMovimiento = rd.GetString("tipo_movimiento"),
                Concepto = rd.GetString("concepto"),
                Monto = rd.GetDecimal("monto"),
                FechaMovimiento = rd.GetDateTime("fecha_movimiento"),
                IdVenta = rd.IsDBNull(ordinalVenta)
                    ? null : rd.GetInt32(ordinalVenta)
            });
        }

        return movimientos;
    }

    public async Task<decimal> CalcularSaldoAsync(int idTurno)
    {
        using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        const string sql = @"
            SELECT t.monto_inicial +
                COALESCE(SUM(
                    CASE
                        WHEN m.tipo_movimiento = 'INGRESO'
                            THEN m.monto
                        WHEN m.tipo_movimiento = 'EGRESO'
                            THEN -m.monto
                        ELSE 0
                    END
                ), 0)
            FROM caja_turnos t
            LEFT JOIN caja_movimientos m
                ON m.id_turno = t.id_turno
            WHERE t.id_turno = @idTurno
            GROUP BY t.id_turno, t.monto_inicial;";

        using var cmd = new MySqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@idTurno", idTurno);

        var resultado = await cmd.ExecuteScalarAsync();

        if (resultado == null || resultado == DBNull.Value)
            throw new Exception("No se encontró el turno de caja.");

        return Convert.ToDecimal(resultado);
    }

    // Resumen informativo de ventas por período de apertura.
    // No constituye una conciliación exclusiva de la caja seleccionada.
    public async Task<ResumenPagosCaja> ObtenerResumenPagosAsync(
        int idTurno)
    {
        var resumen = new ResumenPagosCaja();

        using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        const string sql = @"
            SELECT v.forma_pago,
                   COALESCE(SUM(v.monto_total), 0) AS total
            FROM caja_turnos t
            INNER JOIN ventas_encabezado v
                ON v.fecha_hora_emision >= t.fecha_apertura
               AND v.fecha_hora_emision <=
                   COALESCE(t.fecha_cierre, CURRENT_TIMESTAMP)
            WHERE t.id_turno = @idTurno
              AND v.estado_documento <> 'ANULADO'
            GROUP BY v.forma_pago;";

        using var cmd = new MySqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@idTurno", idTurno);

        using var rd = await cmd.ExecuteReaderAsync();

        while (await rd.ReadAsync())
        {
            string forma = rd.GetString(0).ToUpperInvariant();
            decimal monto = Convert.ToDecimal(rd.GetValue(1));

            switch (forma)
            {
                case "EFECTIVO":
                    resumen.Efectivo += monto;
                    break;
                case "TARJETA":
                    resumen.Tarjeta += monto;
                    break;
                case "TRANSFERENCIA":
                    resumen.Transferencia += monto;
                    break;
                case "CREDITO":
                    resumen.Credito += monto;
                    break;
                default:
                    resumen.Otros += monto;
                    break;
            }
        }

        return resumen;
    }

    public async Task<decimal> CerrarCajaAsync(
        int idTurno,
        int idUsuario,
        decimal montoFinalReal,
        string observaciones)
    {
        if (montoFinalReal < 0)
            throw new Exception("El monto final no puede ser negativo.");

        using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        using var tx = await cn.BeginTransactionAsync();

        try
        {
            const string sqlTurno = @"
                SELECT monto_inicial, estado
                FROM caja_turnos
                WHERE id_turno = @idTurno
                FOR UPDATE;";

            decimal montoInicial;

            using (var cmd = new MySqlCommand(sqlTurno, cn, tx))
            {
                cmd.Parameters.AddWithValue("@idTurno", idTurno);

                using var rd = await cmd.ExecuteReaderAsync();

                if (!await rd.ReadAsync())
                    throw new Exception("No se encontró el turno.");

                if (rd.GetString("estado") != "ABIERTA")
                    throw new Exception("Este turno ya está cerrado.");

                montoInicial = rd.GetDecimal("monto_inicial");
            }

            const string sqlSaldo = @"
                SELECT COALESCE(SUM(
                    CASE
                        WHEN tipo_movimiento = 'INGRESO' THEN monto
                        WHEN tipo_movimiento = 'EGRESO' THEN -monto
                        ELSE 0
                    END
                ), 0)
                FROM caja_movimientos
                WHERE id_turno = @idTurno;";

            decimal movimientos;

            using (var cmd = new MySqlCommand(sqlSaldo, cn, tx))
            {
                cmd.Parameters.AddWithValue("@idTurno", idTurno);
                movimientos = Convert.ToDecimal(
                    await cmd.ExecuteScalarAsync());
            }

            decimal esperado = montoInicial + movimientos;
            decimal diferencia = montoFinalReal - esperado;

            const string sqlCerrar = @"
                UPDATE caja_turnos
                SET id_usuario_cierre = @idUsuario,
                    fecha_cierre = CURRENT_TIMESTAMP,
                    monto_final_esperado = @esperado,
                    monto_final_real = @real,
                    diferencia = @diferencia,
                    observaciones = @observaciones,
                    estado = 'CERRADA'
                WHERE id_turno = @idTurno
                  AND estado = 'ABIERTA';";

            using var cerrar = new MySqlCommand(sqlCerrar, cn, tx);
            cerrar.Parameters.AddWithValue("@idUsuario", idUsuario);
            cerrar.Parameters.AddWithValue("@esperado", esperado);
            cerrar.Parameters.AddWithValue("@real", montoFinalReal);
            cerrar.Parameters.AddWithValue("@diferencia", diferencia);
            cerrar.Parameters.AddWithValue(
                "@observaciones", observaciones ?? "");
            cerrar.Parameters.AddWithValue("@idTurno", idTurno);

            if (await cerrar.ExecuteNonQueryAsync() != 1)
                throw new Exception("No se pudo cerrar el turno.");

            await tx.CommitAsync();
            return diferencia;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
