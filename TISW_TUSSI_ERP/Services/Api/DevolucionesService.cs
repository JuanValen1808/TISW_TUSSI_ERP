
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySqlConnector;
using TISW_TUSSI_ERP.Models.Ventas;

namespace TISW_TUSSI_ERP.Services.Api;

public class DevolucionesService
{
    private readonly DatabaseService _db;

    public DevolucionesService(DatabaseService db)
    {
        _db = db;
    }

    // =====================================
    // BUSCAR VENTA ORIGINAL
    // =====================================
    public async Task<DevolucionVenta?> BuscarVentaAsync(
        string folio)
    {
        if (string.IsNullOrWhiteSpace(folio))
            return null;

        const string sql = @"
            SELECT
                v.id_venta,
                v.folio_documento,
                COALESCE(
                    c.razon_social_nombre,
                    'Público general'
                )
            FROM ventas_encabezado v
            LEFT JOIN clientes c
                ON c.id_cliente = v.id_cliente
            WHERE v.folio_documento = @folio
              AND v.tipo_documento IN ('BOLETA', 'FACTURA')
              AND v.estado_documento = 'PAGADO'
            LIMIT 1;";

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        await using var cmd = new MySqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@folio", folio.Trim());

        await using var rd = await cmd.ExecuteReaderAsync();

        if (!await rd.ReadAsync())
            return null;

        return new DevolucionVenta
        {
            IdVentaOriginal = rd.GetInt32(0),
            FolioVenta = rd.GetString(1),
            Cliente = rd.GetString(2)
        };
    }

    // =====================================
    // OBTENER DETALLE DE VENTA
    // =====================================
    public async Task<List<DetalleDevolucionVenta>>
        ObtenerDetalleAsync(int idVenta)
    {
        const string sql = @"
            SELECT
                vd.id_detalle_venta,
                vd.id_producto,
                vd.id_lote,
                p.nombre_comercial,
                l.numero_lote,
                vd.cantidad,
                COALESCE((
                    SELECT SUM(dd.cantidad_devuelta)
                    FROM devoluciones_detalle dd
                    INNER JOIN devoluciones_ventas dv
                        ON dv.id_devolucion = dd.id_devolucion
                    WHERE dd.id_detalle_venta = vd.id_detalle_venta
                      AND dv.estado = 'REGISTRADA'
                ), 0),
                vd.precio_unitario,
                v.monto_total,
                (
                    SELECT COALESCE(SUM(x.subtotal), 0)
                    FROM ventas_detalle x
                    WHERE x.id_venta = vd.id_venta
                )
            FROM ventas_detalle vd
            INNER JOIN ventas_encabezado v
                ON v.id_venta = vd.id_venta
            INNER JOIN productos p
                ON p.id_producto = vd.id_producto
            INNER JOIN lotes l
                ON l.id_lote = vd.id_lote
            WHERE vd.id_venta = @venta
            ORDER BY vd.id_detalle_venta;";

        var resultado = new List<DetalleDevolucionVenta>();

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        await using var cmd = new MySqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@venta", idVenta);

        await using var rd = await cmd.ExecuteReaderAsync();

        while (await rd.ReadAsync())
        {
            decimal precioOriginal =
                Convert.ToDecimal(rd.GetValue(7));

            decimal totalCobrado =
                Convert.ToDecimal(rd.GetValue(8));

            decimal totalLineas =
                Convert.ToDecimal(rd.GetValue(9));

            decimal precioEfectivo = totalLineas > 0
                ? precioOriginal * totalCobrado / totalLineas
                : 0m;

            resultado.Add(new DetalleDevolucionVenta
            {
                IdDetalleVenta = rd.GetInt32(0),
                IdProducto = rd.GetInt32(1),
                IdLote = rd.GetInt32(2),
                NombreProducto = rd.GetString(3),
                NumeroLote = rd.GetString(4),
                CantidadVendida = rd.GetInt32(5),
                CantidadDevueltaAnterior =
                    Convert.ToInt32(rd.GetValue(6)),
                PrecioUnitario = Math.Round(
                    precioEfectivo,
                    2,
                    MidpointRounding.AwayFromZero),
                CantidadDevolver = 0,
                CondicionProducto = "CUARENTENA"
            });
        }

        return resultado;
    }

    // =====================================
    // REGISTRAR DEVOLUCIÓN
    // =====================================
    public async Task<string> RegistrarDevolucionAsync(
        int idVenta,
        int idUsuario,
        string motivo,
        List<DetalleDevolucionVenta> detalles)
    {
        if (idVenta <= 0 || idUsuario <= 0)
        {
            throw new InvalidOperationException(
                "La venta o el usuario no son válidos.");
        }

        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new InvalidOperationException(
                "Debes indicar el motivo de la devolución.");
        }

        motivo = motivo.Trim();

        if (motivo.Length > 255)
        {
            throw new InvalidOperationException(
                "El motivo no puede superar los 255 caracteres.");
        }

        if (detalles == null)
        {
            throw new InvalidOperationException(
                "No se recibieron productos.");
        }

        var seleccionados = detalles
            .Where(d => d.CantidadDevolver > 0)
            .ToList();

        if (seleccionados.Count == 0)
        {
            throw new InvalidOperationException(
                "Selecciona al menos un producto para devolver.");
        }

        if (seleccionados
            .GroupBy(d => d.IdDetalleVenta)
            .Any(g => g.Count() > 1))
        {
            throw new InvalidOperationException(
                "Existen productos duplicados en la devolución.");
        }

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        await using var tx = await cn.BeginTransactionAsync();

        try
        {
            decimal totalCobrado;
            decimal totalLineas;
            decimal totalYaDevuelto;

            // Bloquear venta para evitar devoluciones
            // simultáneas sobre las mismas unidades.
            const string sqlVenta = @"
                SELECT
                    v.monto_total,
                    (
                        SELECT COALESCE(SUM(d.subtotal), 0)
                        FROM ventas_detalle d
                        WHERE d.id_venta = v.id_venta
                    ),
                    (
                        SELECT COALESCE(SUM(dv.monto_total), 0)
                        FROM devoluciones_ventas dv
                        WHERE dv.id_venta_original = v.id_venta
                          AND dv.estado = 'REGISTRADA'
                    )
                FROM ventas_encabezado v
                WHERE v.id_venta = @venta
                  AND v.tipo_documento IN ('BOLETA', 'FACTURA')
                  AND v.estado_documento = 'PAGADO'
                FOR UPDATE;";

            await using (var cmd = new MySqlCommand(
                sqlVenta, cn, tx))
            {
                cmd.Parameters.AddWithValue("@venta", idVenta);

                await using var rd =
                    await cmd.ExecuteReaderAsync();

                if (!await rd.ReadAsync())
                {
                    throw new InvalidOperationException(
                        "La venta original no existe o no está pagada.");
                }

                totalCobrado =
                    Convert.ToDecimal(rd.GetValue(0));

                totalLineas =
                    Convert.ToDecimal(rd.GetValue(1));

                totalYaDevuelto =
                    Convert.ToDecimal(rd.GetValue(2));
            }

            if (totalCobrado <= 0 || totalLineas <= 0)
            {
                throw new InvalidOperationException(
                    "La venta tiene importes inválidos.");
            }

            decimal saldoRestante =
                totalCobrado - totalYaDevuelto;

            if (saldoRestante <= 0)
            {
                throw new InvalidOperationException(
                    "La venta ya fue devuelta completamente.");
            }

            var lineas = new List<(
                int IdDetalle,
                int Cantidad,
                decimal Precio,
                decimal Subtotal)>();

            foreach (var detalle in seleccionados)
            {
                const string sqlDetalle = @"
                    SELECT
                        vd.cantidad,
                        vd.precio_unitario,
                        COALESCE((
                            SELECT SUM(dd.cantidad_devuelta)
                            FROM devoluciones_detalle dd
                            INNER JOIN devoluciones_ventas dv
                                ON dv.id_devolucion = dd.id_devolucion
                            WHERE dd.id_detalle_venta =
                                vd.id_detalle_venta
                              AND dv.estado = 'REGISTRADA'
                        ), 0)
                    FROM ventas_detalle vd
                    WHERE vd.id_detalle_venta = @detalle
                      AND vd.id_venta = @venta;";

                int cantidadVendida;
                int cantidadDevuelta;
                decimal precioOriginal;

                await using (var cmd = new MySqlCommand(
                    sqlDetalle, cn, tx))
                {
                    cmd.Parameters.AddWithValue(
                        "@detalle",
                        detalle.IdDetalleVenta);

                    cmd.Parameters.AddWithValue(
                        "@venta",
                        idVenta);

                    await using var rd =
                        await cmd.ExecuteReaderAsync();

                    if (!await rd.ReadAsync())
                    {
                        throw new InvalidOperationException(
                            "Uno de los productos no pertenece a la venta.");
                    }

                    cantidadVendida = rd.GetInt32(0);

                    precioOriginal =
                        Convert.ToDecimal(rd.GetValue(1));

                    cantidadDevuelta =
                        Convert.ToInt32(rd.GetValue(2));
                }

                int cantidadDisponible =
                    cantidadVendida - cantidadDevuelta;

                if (detalle.CantidadDevolver <= 0 ||
                    detalle.CantidadDevolver > cantidadDisponible)
                {
                    throw new InvalidOperationException(
                        "La cantidad solicitada supera las " +
                        "unidades disponibles para devolución.");
                }

                decimal precioEfectivo =
                    precioOriginal * totalCobrado / totalLineas;

                decimal precioRedondeado = Math.Round(
                    precioEfectivo,
                    2,
                    MidpointRounding.AwayFromZero);

                decimal subtotal = Math.Round(
                    precioEfectivo * detalle.CantidadDevolver,
                    2,
                    MidpointRounding.AwayFromZero);

                lineas.Add((
                    detalle.IdDetalleVenta,
                    detalle.CantidadDevolver,
                    precioRedondeado,
                    subtotal));
            }

            decimal montoTotal =
                lineas.Sum(x => x.Subtotal);

            // No permitir que la suma de devoluciones
            // supere el total efectivamente cobrado.
            if (montoTotal > saldoRestante)
            {
                decimal exceso =
                    montoTotal - saldoRestante;

                int ultima = lineas.Count - 1;

                var linea = lineas[ultima];

                if (linea.Subtotal < exceso)
                {
                    throw new InvalidOperationException(
                        "El monto supera el saldo disponible.");
                }

                // Una tupla se reemplaza con otra tupla.
                // No utilizar la sintaxis 'with'.
                lineas[ultima] = (
                    linea.IdDetalle,
                    linea.Cantidad,
                    linea.Precio,
                    linea.Subtotal - exceso
                );

                montoTotal = saldoRestante;
            }

            if (montoTotal <= 0)
            {
                throw new InvalidOperationException(
                    "El monto de devolución debe ser positivo.");
            }

            // Folio interno, no tributario.
            string folio =
                $"NC-INT-{DateTime.Now:yyyyMMddHHmmss}-" +
                Guid.NewGuid()
                    .ToString("N")[..6]
                    .ToUpperInvariant();

            const string sqlInsert = @"
                INSERT INTO devoluciones_ventas
                    (
                        id_venta_original,
                        id_usuario,
                        motivo,
                        monto_total,
                        folio_nota_credito
                    )
                VALUES
                    (
                        @venta,
                        @usuario,
                        @motivo,
                        @total,
                        @folio
                    );";

            long idDevolucion;

            await using (var cmd = new MySqlCommand(
                sqlInsert, cn, tx))
            {
                cmd.Parameters.AddWithValue(
                    "@venta", idVenta);

                cmd.Parameters.AddWithValue(
                    "@usuario", idUsuario);

                cmd.Parameters.AddWithValue(
                    "@motivo", motivo);

                cmd.Parameters.AddWithValue(
                    "@total", montoTotal);

                cmd.Parameters.AddWithValue(
                    "@folio", folio);

                await cmd.ExecuteNonQueryAsync();

                idDevolucion = cmd.LastInsertedId;
            }

            const string sqlInsertDetalle = @"
                INSERT INTO devoluciones_detalle
                    (
                        id_devolucion,
                        id_detalle_venta,
                        cantidad_devuelta,
                        precio_unitario,
                        subtotal,
                        condicion_producto
                    )
                VALUES
                    (
                        @devolucion,
                        @detalle,
                        @cantidad,
                        @precio,
                        @subtotal,
                        'CUARENTENA'
                    );";

            foreach (var linea in lineas)
            {
                await using var cmd = new MySqlCommand(
                    sqlInsertDetalle, cn, tx);

                cmd.Parameters.AddWithValue(
                    "@devolucion",
                    idDevolucion);

                cmd.Parameters.AddWithValue(
                    "@detalle",
                    linea.IdDetalle);

                cmd.Parameters.AddWithValue(
                    "@cantidad",
                    linea.Cantidad);

                cmd.Parameters.AddWithValue(
                    "@precio",
                    linea.Precio);

                cmd.Parameters.AddWithValue(
                    "@subtotal",
                    linea.Subtotal);

                await cmd.ExecuteNonQueryAsync();
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
}
