
using System.Data.Common;
using MySqlConnector;
using TISW_TUSSI_ERP.Models.Ventas;

namespace TISW_TUSSI_ERP.Services.Api;

public class VentasService
{
    private readonly DatabaseService _db;
    private readonly ContabilidadService _contabilidad;

    public VentasService(DatabaseService db, ContabilidadService contabilidad)
    {
        _db = db;
        _contabilidad = contabilidad;
    }

    // PRODUCTOS
    public async Task<ProductoPos?> BuscarProductoAsync(string codigo)
    {
        const string sql = @"
            SELECT id_producto, sku,
                   COALESCE(codigo_barras, ''),
                   nombre_comercial,
                   COALESCE(principio_activo, ''),
                   precio_venta_base
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

        return LeerProducto(rd);
    }

    // CATÁLOGO PARA EL NUEVO DISEÑO
    public async Task<List<ProductoPos>> ObtenerProductosAsync(
        string busqueda = "")
    {
        const string sql = @"
            SELECT p.id_producto, p.sku,
                   COALESCE(p.codigo_barras, ''),
                   p.nombre_comercial,
                   COALESCE(p.principio_activo, ''),
                   p.precio_venta_base
            FROM productos p
            WHERE p.activo = 1
              AND (
                  @busqueda = ''
                  OR p.sku LIKE @filtro
                  OR p.codigo_barras LIKE @filtro
                  OR p.nombre_comercial LIKE @filtro
              )
              AND EXISTS (
                  SELECT 1
                  FROM lotes l
                  WHERE l.id_producto = p.id_producto
                    AND l.stock_actual > 0
                    AND l.fecha_vencimiento >= CURDATE()
                    AND COALESCE(l.estado_cuarentena, 'LIBERADO') = 'LIBERADO'
              )
            ORDER BY p.nombre_comercial
            LIMIT 100;";

        var resultado = new List<ProductoPos>();

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        await using var cmd = new MySqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@busqueda", busqueda.Trim());
        cmd.Parameters.AddWithValue("@filtro", $"%{busqueda.Trim()}%");

        await using var rd = await cmd.ExecuteReaderAsync();

        while (await rd.ReadAsync())
            resultado.Add(LeerProducto(rd));

        return resultado;
    }

    private static ProductoPos LeerProducto(DbDataReader rd)
    {
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

    // LOTES ORDENADOS POR FEFO
    public async Task<List<LotePos>> ObtenerLotesAsync(int idProducto)
    {
        const string sql = @"
            SELECT id_lote, numero_lote,
                   fecha_vencimiento, stock_actual
            FROM lotes
            WHERE id_producto = @idProducto
              AND stock_actual > 0
              AND fecha_vencimiento >= CURDATE()
              AND COALESCE(estado_cuarentena, 'LIBERADO') = 'LIBERADO'
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

    // CLIENTES REGISTRADOS
    public async Task<List<ClientePos>> ObtenerClientesAsync()
    {
        const string sql = @"
            SELECT id_cliente, rut_cliente,
                   razon_social_nombre, tipo_cliente
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
                RutCliente = rd.IsDBNull(1) ? "" : rd.GetString(1),
                Nombre = rd.IsDBNull(2) ? "" : rd.GetString(2),
                TipoCliente = rd.IsDBNull(3) ? "" : rd.GetString(3)
            });
        }

        return resultado;
    }

    // CONVENIOS ACTIVOS
    public async Task<List<ConvenioPos>> ObtenerConveniosAsync(
        int idCliente)
    {
        const string sql = @"
            SELECT id_convenio, nombre_convenio,
                   porcentaje_descuento,
                   dias_credito_morosidad
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

    // VERIFICAR SI LA CAJA PRINCIPAL DEL POS ESTÁ ABIERTA
    public async Task<bool> TieneCajaAbiertaAsync()
    {
        const string sql = @"
            SELECT 1
            FROM caja_turnos
            WHERE id_caja = 1
              AND estado = 'ABIERTA'
            LIMIT 1;";

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        await using var cmd = new MySqlCommand(sql, cn);
        var resultado = await cmd.ExecuteScalarAsync();

        return resultado != null && resultado != DBNull.Value;
    }

    // EMITIR VENTA
    // idCliente NULL = PÚBLICO GENERAL
    public async Task<string> EmitirVentaAsync(
        int? idCliente,
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
        if (items == null || items.Count == 0)
            throw new InvalidOperationException(
                "El carrito está vacío.");

        if (idCajero <= 0)
            throw new InvalidOperationException(
                "No existe un cajero con sesión activa.");

        if (formaPago != "EFECTIVO" &&
            formaPago != "TARJETA" &&
            formaPago != "TRANSFERENCIA" &&
            formaPago != "CREDITO")
        {
            throw new InvalidOperationException(
                "La forma de pago no es válida.");
        }

        if (formaPago == "CREDITO" &&
            (!idCliente.HasValue || !idConvenio.HasValue))
        {
            throw new InvalidOperationException(
                "Las ventas a crédito requieren " +
                "un cliente registrado y un convenio.");
        }

        if (idConvenio.HasValue && !idCliente.HasValue)
            throw new InvalidOperationException(
                "No puedes asociar un convenio a Público general.");

        if (items.Any(i =>
            i.Cantidad <= 0 ||
            i.PrecioUnitario < 0 ||
            i.IdProducto <= 0 ||
            i.IdLote <= 0))
        {
            throw new InvalidOperationException(
                "El carrito contiene productos inválidos.");
        }

        decimal subtotalCalculado =
            items.Sum(i => i.Subtotal);

        if (subtotal != subtotalCalculado ||
            descuento < 0 ||
            descuento > subtotal ||
            total != subtotal - descuento ||
            neto < 0 ||
            iva < 0 ||
            neto + iva != total)
        {
            throw new InvalidOperationException(
                "Los importes de la venta no son consistentes.");
        }

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        await using var tx = await cn.BeginTransactionAsync();

        try
        {
            // VALIDAR QUE EXISTA UNA CAJA ABIERTA PARA CUALQUIER FORMA DE PAGO
            const string sqlTurno = @"
                SELECT id_turno
                FROM caja_turnos
                WHERE id_caja = 1
                  AND estado = 'ABIERTA'
                ORDER BY id_turno DESC
                LIMIT 1
                FOR UPDATE;";

            long idTurno;

            await using (var cmdTurno =
                new MySqlCommand(sqlTurno, cn, tx))
            {
                var resultadoTurno =
                    await cmdTurno.ExecuteScalarAsync();

                if (resultadoTurno == null ||
                    resultadoTurno == DBNull.Value)
                {
                    throw new InvalidOperationException(
                        "No hay una caja abierta. " +
                        "Abre Control de Caja antes " +
                        "de realizar una venta.");
                }

                idTurno = Convert.ToInt64(resultadoTurno);
            }

            string tipoDocumento = "BOLETA";

            if (idCliente.HasValue)
            {
                const string sqlCliente = @"
                    SELECT tipo_cliente
                    FROM clientes
                    WHERE id_cliente = @id
                    LIMIT 1;";

                await using var cmdCliente =
                    new MySqlCommand(sqlCliente, cn, tx);

                cmdCliente.Parameters.AddWithValue(
                    "@id", idCliente.Value);

                var resultado =
                    await cmdCliente.ExecuteScalarAsync();

                if (resultado == null ||
                    resultado == DBNull.Value)
                {
                    throw new InvalidOperationException(
                        "El cliente seleccionado no existe.");
                }

                string tipoCliente =
                    Convert.ToString(resultado) ?? "PERSONA";

                tipoDocumento =
                    tipoCliente.Equals(
                        "PERSONA",
                        StringComparison.OrdinalIgnoreCase)
                    ? "BOLETA"
                    : "FACTURA";
            }

            // Validar que el convenio pertenezca al cliente y esté activo (aplica en cualquier forma de pago).
            if (idConvenio.HasValue)
            {
                const string sqlValidarConvenio = @"
                    SELECT 1
                    FROM convenios_salud
                    WHERE id_convenio = @convenio
                      AND id_cliente = @cliente
                      AND activo = 1
                    LIMIT 1;";

                await using var cmdConvenio =
                    new MySqlCommand(sqlValidarConvenio, cn, tx);

                cmdConvenio.Parameters.AddWithValue(
                    "@convenio", idConvenio.Value);
                cmdConvenio.Parameters.AddWithValue(
                    "@cliente", idCliente!.Value);

                var convenioValido =
                    await cmdConvenio.ExecuteScalarAsync();

                if (convenioValido == null ||
                    convenioValido == DBNull.Value)
                {
                    throw new InvalidOperationException(
                        "El convenio seleccionado no es válido para este cliente.");
                }
            }

            string folio =
                $"V-{DateTime.Now:yyyyMMddHHmmss}-" +
                $"{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

            string estado =
                formaPago == "CREDITO"
                ? "PENDIENTE"
                : "PAGADO";

            const string sqlVenta = @"
                INSERT INTO ventas_encabezado
                (
                    folio_documento,
                    tipo_documento,
                    id_cliente,
                    id_convenio,
                    estado_documento,
                    forma_pago,
                    monto_neto,
                    monto_iva,
                    monto_descuento,
                    monto_total,
                    id_usuario_cajero
                )
                VALUES
                (
                    @folio, @tipo, @cliente, @convenio,
                    @estado, @pago, @neto, @iva,
                    @descuento, @total, @cajero
                );";

            long idVenta;

            await using (var cmd =
                new MySqlCommand(sqlVenta, cn, tx))
            {
                cmd.Parameters.AddWithValue("@folio", folio);
                cmd.Parameters.AddWithValue("@tipo", tipoDocumento);
                cmd.Parameters.AddWithValue(
                    "@cliente",
                    (object?)idCliente ?? DBNull.Value);
                cmd.Parameters.AddWithValue(
                    "@convenio",
                    (object?)idConvenio ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@estado", estado);
                cmd.Parameters.AddWithValue("@pago", formaPago);
                cmd.Parameters.AddWithValue("@neto", neto);
                cmd.Parameters.AddWithValue("@iva", iva);
                cmd.Parameters.AddWithValue(
                    "@descuento", descuento);
                cmd.Parameters.AddWithValue("@total", total);
                cmd.Parameters.AddWithValue("@cajero", idCajero);

                await cmd.ExecuteNonQueryAsync();
                idVenta = cmd.LastInsertedId;
            }

            // DESCONTAR STOCK Y REGISTRAR DETALLE
            foreach (var item in items)
            {
                const string sqlStock = @"
                    UPDATE lotes
                    SET stock_actual = stock_actual - @cantidad
                    WHERE id_lote = @lote
                      AND id_producto = @producto
                      AND stock_actual >= @cantidad
                      AND fecha_vencimiento >= CURDATE()
                      AND COALESCE(estado_cuarentena, 'LIBERADO') = 'LIBERADO';";

                await using (var cmd =
                    new MySqlCommand(sqlStock, cn, tx))
                {
                    cmd.Parameters.AddWithValue(
                        "@cantidad", item.Cantidad);
                    cmd.Parameters.AddWithValue(
                        "@lote", item.IdLote);
                    cmd.Parameters.AddWithValue(
                        "@producto", item.IdProducto);

                    if (await cmd.ExecuteNonQueryAsync() != 1)
                    {
                        throw new InvalidOperationException(
                            $"Stock insuficiente o lote vencido: " +
                            $"{item.NombreProducto} " +
                            $"({item.NumeroLote}).");
                    }
                }

                const string sqlDetalle = @"
                    INSERT INTO ventas_detalle
                    (
                        id_venta,
                        id_producto,
                        id_lote,
                        cantidad,
                        precio_unitario,
                        subtotal
                    )
                    VALUES
                    (
                        @venta, @producto, @lote,
                        @cantidad, @precio, @subtotal
                    );";

                await using var cmdDetalle =
                    new MySqlCommand(sqlDetalle, cn, tx);

                cmdDetalle.Parameters.AddWithValue(
                    "@venta", idVenta);
                cmdDetalle.Parameters.AddWithValue(
                    "@producto", item.IdProducto);
                cmdDetalle.Parameters.AddWithValue(
                    "@lote", item.IdLote);
                cmdDetalle.Parameters.AddWithValue(
                    "@cantidad", item.Cantidad);
                cmdDetalle.Parameters.AddWithValue(
                    "@precio", item.PrecioUnitario);
                cmdDetalle.Parameters.AddWithValue(
                    "@subtotal", item.Subtotal);

                await cmdDetalle.ExecuteNonQueryAsync();
            }

            // CUENTA POR COBRAR Y VALIDACIÓN DE LÍMITE DE CRÉDITO
            if (formaPago == "CREDITO")
            {
                const string sqlCupo = @"
                    SELECT
                        cs.limite_credito,
                        COALESCE((
                            SELECT SUM(cx.monto_pendiente)
                            FROM cuentas_por_cobrar cx
                            INNER JOIN ventas_encabezado ve
                                ON ve.id_venta = cx.id_venta
                            WHERE cx.id_convenio = cs.id_convenio
                              AND cx.estado_cobro <> 'ANULADA'
                              AND ve.estado_documento <> 'ANULADO'
                        ), 0) AS deuda_actual
                    FROM convenios_salud cs
                    WHERE cs.id_convenio = @convenio
                      AND cs.id_cliente = @cliente
                      AND cs.activo = 1
                    FOR UPDATE;";

                await using (var cmdCupo =
                    new MySqlCommand(sqlCupo, cn, tx))
                {
                    cmdCupo.Parameters.AddWithValue(
                        "@convenio", idConvenio!.Value);
                    cmdCupo.Parameters.AddWithValue(
                        "@cliente", idCliente!.Value);

                    await using var rdCupo =
                        await cmdCupo.ExecuteReaderAsync();

                    if (!await rdCupo.ReadAsync())
                    {
                        throw new InvalidOperationException(
                            "El convenio no es válido para este cliente.");
                    }

                    decimal limiteCredito = rdCupo.GetDecimal(0);
                    decimal deudaActual = rdCupo.GetDecimal(1);

                    if (limiteCredito > 0 &&
                        deudaActual + total > limiteCredito)
                    {
                        decimal disponible =
                            Math.Max(0, limiteCredito - deudaActual);
                        throw new InvalidOperationException(
                            $"La venta supera el cupo disponible del convenio " +
                            $"(${disponible:N0} disponibles de ${limiteCredito:N0}).");
                    }
                }

                const string sqlCxc = @"
                    INSERT INTO cuentas_por_cobrar
                    (
                        id_venta,
                        id_cliente,
                        id_convenio,
                        monto_pendiente,
                        fecha_vencimiento_pago,
                        dias_morosidad,
                        estado_cobro
                    )
                    SELECT
                        @venta,
                        @cliente,
                        @convenio,
                        @total,
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

                await using var cmdCxc =
                    new MySqlCommand(sqlCxc, cn, tx);

                cmdCxc.Parameters.AddWithValue(
                    "@venta", idVenta);
                cmdCxc.Parameters.AddWithValue(
                    "@cliente", idCliente!.Value);
                cmdCxc.Parameters.AddWithValue(
                    "@convenio", idConvenio!.Value);
                cmdCxc.Parameters.AddWithValue(
                    "@total", total);

                if (await cmdCxc.ExecuteNonQueryAsync() != 1)
                {
                    throw new InvalidOperationException(
                        "El convenio no es válido para este cliente.");
                }
            }

            // REGISTRO EN CONTROL DE CAJA (SOLO EFECTIVO AFECTA EL SALDO FÍSICO)
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
                        id_venta
                    )
                    VALUES
                    (
                        @turno,
                        @usuario,
                        'INGRESO',
                        @concepto,
                        @monto,
                        @venta
                    );";

                await using var cmdMovimiento =
                    new MySqlCommand(sqlMovimiento, cn, tx);

                cmdMovimiento.Parameters.AddWithValue(
                    "@turno", idTurno);
                cmdMovimiento.Parameters.AddWithValue(
                    "@usuario", idCajero);
                cmdMovimiento.Parameters.AddWithValue(
                    "@concepto", $"Venta POS - {folio}");
                cmdMovimiento.Parameters.AddWithValue(
                    "@monto", total);
                cmdMovimiento.Parameters.AddWithValue(
                    "@venta", idVenta);

                await cmdMovimiento.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();

            try
            {
                await _contabilidad.CentralizarMovimientosPendientesAsync(idCajero);
            }
            catch { }

            return folio;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // RANKING DE VENTAS
    // LEFT JOIN incluye las ventas a Público general.
    public async Task<(List<FilaRanking> Filas, decimal TotalGeneral)>
        ObtenerRankingAsync()
    {
        const string sql = @"
            SELECT
                COALESCE(c.rut_cliente, ''),
                COALESCE(
                    c.razon_social_nombre,
                    'Público general'
                ),
                COALESCE(c.tipo_cliente, 'PUBLICO'),
                COALESCE(
                    cs.nombre_convenio,
                    'Sin convenio'
                ),
                COUNT(v.id_venta),
                COALESCE(SUM(v.monto_total), 0)
            FROM ventas_encabezado v
            LEFT JOIN clientes c
                ON c.id_cliente = v.id_cliente
            LEFT JOIN convenios_salud cs
                ON cs.id_convenio = v.id_convenio
            WHERE v.estado_documento <> 'ANULADO'
            GROUP BY
                c.id_cliente,
                c.rut_cliente,
                c.razon_social_nombre,
                c.tipo_cliente,
                cs.id_convenio,
                cs.nombre_convenio
            ORDER BY
                SUM(v.monto_total) DESC;";

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
                TipoCliente = rd.GetString(2),
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

    private static decimal Dec(DbDataReader rd, int indice)
    {
        return rd.IsDBNull(indice)
            ? 0m
            : Convert.ToDecimal(rd.GetValue(indice));
    }
}
