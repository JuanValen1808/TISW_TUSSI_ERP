
using MySqlConnector;
using TISW_TUSSI_ERP.Models.Ventas;

namespace TISW_TUSSI_ERP.Services.Api;

public class CuentasPorCobrarVentasService
{
    private readonly DatabaseService _db;

    public CuentasPorCobrarVentasService(DatabaseService db)
    {
        _db = db;
    }

    public async Task<List<CuentaPorCobrarVenta>> ObtenerAsync()
    {
        const string sql = @"
            SELECT
                cx.id_cuenta_cobrar,
                v.folio_documento,
                c.razon_social_nombre,
                c.rut_cliente,
                COALESCE(cv.nombre_convenio, 'Sin convenio'),
                cx.fecha_vencimiento_pago,
                cx.monto_pendiente,
                cx.estado_cobro
            FROM cuentas_por_cobrar cx
            INNER JOIN ventas_encabezado v
                ON v.id_venta = cx.id_venta
            INNER JOIN clientes c
                ON c.id_cliente = cx.id_cliente
            LEFT JOIN convenios_salud cv
                ON cv.id_convenio = cx.id_convenio
            WHERE v.estado_documento <> 'ANULADO'
            ORDER BY
                cx.fecha_vencimiento_pago ASC,
                cx.id_venta DESC
            LIMIT 500;";

        var resultado = new List<CuentaPorCobrarVenta>();

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        await using var cmd = new MySqlCommand(sql, cn);
        await using var rd = await cmd.ExecuteReaderAsync();

        while (await rd.ReadAsync())
        {
            resultado.Add(new CuentaPorCobrarVenta
            {
                IdCuenta = rd.GetInt32(0),
                Folio = rd.IsDBNull(1) ? "" : rd.GetString(1),
                Cliente = rd.IsDBNull(2) ? "" : rd.GetString(2),
                Rut = rd.IsDBNull(3) ? "" : rd.GetString(3),
                Convenio = rd.GetString(4),
                Vencimiento = rd.GetDateTime(5),
                Saldo = rd.IsDBNull(6)
                    ? 0m
                    : Convert.ToDecimal(rd.GetValue(6)),
                Estado = rd.IsDBNull(7) ? "" : rd.GetString(7)
            });
        }

        return resultado;
    }

    public async Task RegistrarAbonoAsync(
        int idCuenta,
        decimal monto,
        string formaPago,
        int idUsuario,
        int? idTurno = null,
        string? observacion = null)
    {
        if (idCuenta <= 0)
            throw new ArgumentException(
                "La cuenta por cobrar no es válida.");

        if (idUsuario <= 0)
            throw new ArgumentException(
                "Debes iniciar sesión para registrar el cobro.");

        if (monto <= 0 || decimal.Round(monto, 2) != monto)
            throw new ArgumentException(
                "El monto debe ser positivo y tener máximo dos decimales.");

        formaPago = (formaPago ?? "")
            .Trim()
            .ToUpperInvariant();

        if (formaPago != "EFECTIVO" &&
            formaPago != "TARJETA" &&
            formaPago != "TRANSFERENCIA")
        {
            throw new ArgumentException(
                "La forma de pago no es válida.");
        }

        if (idTurno == null)
        {
            throw new InvalidOperationException(
                "Debes tener una caja con turno abierto para registrar cobros.");
        }

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        await using var tx = await cn.BeginTransactionAsync();

        try
        {
            decimal saldoActual;
            string estadoActual;
            int idVenta;

            const string sqlCuenta = @"
                SELECT
                    cx.monto_pendiente,
                    cx.estado_cobro,
                    cx.id_venta
                FROM cuentas_por_cobrar cx
                INNER JOIN ventas_encabezado v
                    ON v.id_venta = cx.id_venta
                WHERE cx.id_cuenta_cobrar = @idCuenta
                  AND v.estado_documento <> 'ANULADO'
                FOR UPDATE;";

            await using (var cmd = new MySqlCommand(
                sqlCuenta, cn, tx))
            {
                cmd.Parameters.AddWithValue(
                    "@idCuenta", idCuenta);

                await using var rd =
                    await cmd.ExecuteReaderAsync();

                if (!await rd.ReadAsync())
                    throw new InvalidOperationException(
                        "No se encontró la cuenta por cobrar.");

                saldoActual = rd.GetDecimal(0);
                estadoActual = rd.GetString(1);
                idVenta = rd.GetInt32(2);
            }

            if (estadoActual == "PAGADA" ||
                estadoActual == "ANULADA" ||
                saldoActual <= 0)
            {
                throw new InvalidOperationException(
                    "Esta cuenta no permite nuevos abonos.");
            }

            if (monto > saldoActual)
            {
                throw new InvalidOperationException(
                    $"El abono supera el saldo pendiente de {saldoActual:N0}.");
            }

            // Validar el turno abierto dentro de la transacción para cualquier forma de pago.
            {
                const string sqlTurno = @"
                    SELECT estado
                    FROM caja_turnos
                    WHERE id_turno = @idTurno
                    FOR UPDATE;";

                await using var cmd = new MySqlCommand(
                    sqlTurno, cn, tx);

                cmd.Parameters.AddWithValue(
                    "@idTurno", idTurno!.Value);

                string estadoTurno =
                    Convert.ToString(
                        await cmd.ExecuteScalarAsync()) ?? "";

                if (estadoTurno != "ABIERTA")
                {
                    throw new InvalidOperationException(
                        "El turno de caja no está abierto.");
                }
            }

            // Guardar el abono.
            const string sqlAbono = @"
                INSERT INTO abonos_cuentas_por_cobrar
                (
                    id_cuenta_cobrar,
                    monto_abono,
                    forma_pago,
                    observacion
                )
                VALUES
                (
                    @idCuenta,
                    @monto,
                    @formaPago,
                    @observacion
                );";

            long idAbono;

            await using (var cmd = new MySqlCommand(
                sqlAbono, cn, tx))
            {
                cmd.Parameters.AddWithValue(
                    "@idCuenta", idCuenta);

                cmd.Parameters.AddWithValue(
                    "@monto", monto);

                cmd.Parameters.AddWithValue(
                    "@formaPago", formaPago);

                cmd.Parameters.AddWithValue(
                    "@observacion",
                    string.IsNullOrWhiteSpace(observacion)
                        ? DBNull.Value
                        : observacion.Trim());

                await cmd.ExecuteNonQueryAsync();

                idAbono = cmd.LastInsertedId;
            }

            // Actualizar saldo.
            decimal nuevoSaldo = saldoActual - monto;

            string nuevoEstado = nuevoSaldo == 0
                ? "PAGADA"
                : estadoActual;

            const string sqlActualizar = @"
                UPDATE cuentas_por_cobrar
                SET
                    monto_pendiente = @saldo,
                    estado_cobro = @estado
                WHERE id_cuenta_cobrar = @idCuenta;";

            await using (var cmd = new MySqlCommand(
                sqlActualizar, cn, tx))
            {
                cmd.Parameters.AddWithValue(
                    "@saldo", nuevoSaldo);

                cmd.Parameters.AddWithValue(
                    "@estado", nuevoEstado);

                cmd.Parameters.AddWithValue(
                    "@idCuenta", idCuenta);

                int filas = await cmd.ExecuteNonQueryAsync();

                if (filas != 1)
                    throw new InvalidOperationException(
                        "No se pudo actualizar el saldo.");
            }

            // Si la deuda quedó completamente saldada, marcar también la venta como PAGADO.
            if (nuevoSaldo == 0)
            {
                const string sqlVentaPagada = @"
                    UPDATE ventas_encabezado
                    SET estado_documento = 'PAGADO'
                    WHERE id_venta = @idVenta
                      AND estado_documento = 'PENDIENTE';";

                await using var cmdVenta = new MySqlCommand(
                    sqlVentaPagada, cn, tx);
                cmdVenta.Parameters.AddWithValue("@idVenta", idVenta);
                await cmdVenta.ExecuteNonQueryAsync();
            }

            // Solo los abonos en efectivo afectan
            // el saldo físico del turno de caja.
            if (formaPago == "EFECTIVO")
            {
                const string sqlMovimiento = @"
                    INSERT INTO caja_movimientos
                    (
                        id_turno,
                        id_usuario,
                        tipo_movimiento,
                        concepto,
                        monto,
                        id_venta,
                        id_abono
                    )
                    VALUES
                    (
                        @idTurno,
                        @idUsuario,
                        'INGRESO',
                        @concepto,
                        @monto,
                        @idVenta,
                        @idAbono
                    );";

                await using var cmd = new MySqlCommand(
                    sqlMovimiento, cn, tx);

                cmd.Parameters.AddWithValue(
                    "@idTurno", idTurno!.Value);

                cmd.Parameters.AddWithValue(
                    "@idUsuario", idUsuario);

                cmd.Parameters.AddWithValue(
                    "@concepto",
                    $"Abono cuenta por cobrar #{idCuenta}");

                cmd.Parameters.AddWithValue(
                    "@monto", monto);

                cmd.Parameters.AddWithValue(
                    "@idVenta", idVenta);

                cmd.Parameters.AddWithValue(
                    "@idAbono", idAbono);

                await cmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
