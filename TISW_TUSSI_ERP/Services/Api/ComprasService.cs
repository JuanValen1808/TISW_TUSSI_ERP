using System.Data.Common;
using MySqlConnector;
using TISW_TUSSI_ERP.Models.Compras;

namespace TISW_TUSSI_ERP.Services.Api;

public class ReporteCompraItem
{
    public string Rut { get; set; } = string.Empty;
    public string Proveedor { get; set; } = string.Empty;
    public int TotalOrdenes { get; set; }
    public decimal MontoTotal { get; set; }
    public decimal PorRecibir { get; set; }
}

public class ProductoSelector
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal Costo { get; set; }
    public string DescripcionCompleta => $"{Sku} - {Nombre} (${Costo:N0})";
}

public class CarritoItem
{
    public int IdProducto { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string NombreProducto { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal => Cantidad * PrecioUnitario;
}

public class ComprasService
{
    private readonly DatabaseService _db;
    public ComprasService(DatabaseService db) => _db = db;

    // Límite de compra que activa la aprobación jerárquica obligatoria
    public const decimal LimiteGastoAutorizacion = 1_000_000m;

    // Estado transitorio compartido entre vistas del módulo Compras para resaltar/refrescar órdenes creadas
    public int? UltimaOrdenCreadaId { get; set; }

    public async Task<ResumenCompras> ObtenerResumenAsync()
    {
        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        var r = new ResumenCompras();

        const string sql1 = @"
            SELECT COUNT(CASE WHEN estado IN ('BORRADOR','EMITIDA') THEN 1 END),
                   COUNT(CASE WHEN estado = 'PARCIAL' THEN 1 END),
                   COALESCE(SUM(CASE WHEN estado NOT IN ('ANULADA', 'PENDIENTE_AUTORIZACION') THEN monto_total END), 0),
                   COUNT(CASE WHEN estado = 'PENDIENTE_AUTORIZACION' THEN 1 END)
            FROM ordenes_compra";
        await using (var cmd = new MySqlCommand(sql1, cn))
        await using (var rd = await cmd.ExecuteReaderAsync())
        {
            if (await rd.ReadAsync())
            {
                r.OrdenesPendientes = Ent(rd, 0);
                r.RecibidasParciales = Ent(rd, 1);
                r.TotalComprometido = Dec(rd, 2);
                r.OrdenesPendientesAutorizacion = Ent(rd, 3);
            }
        }

        const string sql2 = @"
            SELECT COALESCE(SUM((d.cantidad_solicitada - d.cantidad_recibida) * d.costo_pactado_unitario), 0)
            FROM detalle_orden_compra d
            JOIN ordenes_compra o ON o.id_orden_compra = d.id_orden_compra
            WHERE o.estado IN ('EMITIDA','PARCIAL')";
        await using (var cmd = new MySqlCommand(sql2, cn))
            r.MontoPorRecibir = Convert.ToDecimal(await cmd.ExecuteScalarAsync());

        await using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM proveedores", cn))
            r.Proveedores = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        return r;
    }

    public async Task<List<OrdenCompra>> ObtenerOrdenesAsync()
    {
        const string sql = @"
            SELECT o.id_orden_compra, o.numero_oc, o.fecha_emision, o.estado,
                   o.monto_subtotal, o.monto_iva, o.monto_total,
                   p.razon_social, p.rut_proveedor, p.nombre_contacto, u.nombre,
                   o.id_proveedor, o.id_usuario_creador
            FROM ordenes_compra o
            JOIN proveedores p ON p.id_proveedor = o.id_proveedor
            JOIN usuarios u ON u.id_usuario = o.id_usuario_creador
            ORDER BY o.fecha_emision DESC, o.id_orden_compra DESC";

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, cn);
        await using var rd = await cmd.ExecuteReaderAsync();

        var lista = new List<OrdenCompra>();
        while (await rd.ReadAsync())
        {
            lista.Add(new OrdenCompra
            {
                Id = rd.GetInt32(0),
                Numero = rd.GetString(1),
                FechaEmision = Convert.ToDateTime(rd.GetValue(2)),
                Estado = rd.GetString(3),
                Subtotal = Dec(rd, 4),
                Iva = Dec(rd, 5),
                Total = Dec(rd, 6),
                Proveedor = rd.GetString(7),
                RutProveedor = rd.GetString(8),
                Contacto = rd.IsDBNull(9) ? "—" : rd.GetString(9),
                Creador = rd.GetString(10),
                IdProveedor = rd.GetInt32(11),
                IdUsuarioCreador = rd.GetInt32(12)
            });
        }
        return lista;
    }

    public async Task<List<DetalleOrdenCompra>> ObtenerDetalleAsync(int idOrden)
    {
        const string sql = @"
            SELECT d.id_detalle_oc, d.id_producto, p.sku, p.nombre_comercial, p.formato_presentacion,
                   d.cantidad_solicitada, d.cantidad_recibida, d.costo_pactado_unitario
            FROM detalle_orden_compra d
            JOIN productos p ON p.id_producto = d.id_producto
            WHERE d.id_orden_compra = @id
            ORDER BY d.id_detalle_oc";

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@id", idOrden);
        await using var rd = await cmd.ExecuteReaderAsync();

        var lista = new List<DetalleOrdenCompra>();
        while (await rd.ReadAsync())
        {
            var presentacion = rd.IsDBNull(4) ? "" : " · " + rd.GetString(4);
            lista.Add(new DetalleOrdenCompra
            {
                IdDetalleOc = rd.GetInt32(0),
                IdProducto = rd.GetInt32(1),
                Sku = rd.GetString(2),
                Descripcion = rd.GetString(3) + presentacion,
                Pedido = Ent(rd, 5),
                Recibido = Ent(rd, 6),
                Costo = Dec(rd, 7)
            });
        }
        return lista;
    }

    public async Task<List<Proveedor>> ObtenerProveedoresAsync()
    {
        const string sql = @"
            SELECT p.id_proveedor, p.rut_proveedor, p.razon_social, p.nombre_contacto,
                   p.telefono, p.email, p.condiciones_comerciales,
                   COUNT(o.id_orden_compra) AS num_ordenes,
                   COALESCE(SUM(CASE WHEN o.estado <> 'ANULADA' THEN o.monto_total END), 0) AS monto_total
            FROM proveedores p
            LEFT JOIN ordenes_compra o ON o.id_proveedor = p.id_proveedor
            GROUP BY p.id_proveedor, p.rut_proveedor, p.razon_social, p.nombre_contacto,
                     p.telefono, p.email, p.condiciones_comerciales
            ORDER BY p.razon_social";

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, cn);
        await using var rd = await cmd.ExecuteReaderAsync();

        var lista = new List<Proveedor>();
        while (await rd.ReadAsync())
        {
            lista.Add(new Proveedor
            {
                Id = rd.GetInt32(0),
                Rut = rd.GetString(1),
                RazonSocial = rd.GetString(2),
                Contacto = rd.IsDBNull(3) ? "" : rd.GetString(3),
                Telefono = rd.IsDBNull(4) ? "" : rd.GetString(4),
                Email = rd.IsDBNull(5) ? "" : rd.GetString(5),
                CondicionesComerciales = rd.IsDBNull(6) ? "" : rd.GetString(6),
                OrdenesEmitidas = Ent(rd, 7),
                MontoComprado = Dec(rd, 8)
            });
        }
        return lista;
    }

    public async Task<bool> ExisteRutAsync(string rut, int? idExcluir = null)
    {
        const string sql = "SELECT COUNT(*) FROM proveedores WHERE rut_proveedor = @rut AND (@id IS NULL OR id_proveedor <> @id)";
        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@rut", rut);
        cmd.Parameters.AddWithValue("@id", (object?)idExcluir ?? DBNull.Value);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
    }

    public async Task CrearProveedorAsync(Proveedor p)
    {
        const string sql = @"
            INSERT INTO proveedores (rut_proveedor, razon_social, nombre_contacto, telefono, email, condiciones_comerciales)
            VALUES (@rut, @razon, @contacto, @telefono, @email, @condiciones)";

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, cn);
        AgregarParametrosProveedor(cmd, p);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task ActualizarProveedorAsync(Proveedor p)
    {
        const string sql = @"
            UPDATE proveedores
            SET rut_proveedor = @rut, razon_social = @razon, nombre_contacto = @contacto,
                telefono = @telefono, email = @email, condiciones_comerciales = @condiciones
            WHERE id_proveedor = @id";

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, cn);
        AgregarParametrosProveedor(cmd, p);
        cmd.Parameters.AddWithValue("@id", p.Id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task EliminarProveedorAsync(int id)
    {
        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        try
        {
            await using var cmd = new MySqlCommand("DELETE FROM proveedores WHERE id_proveedor = @id", cn);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (MySqlException ex) when (ex.Number == 1451)
        {
            throw new InvalidOperationException("No se puede eliminar: el proveedor tiene órdenes de compra asociadas. Puedes editar sus datos.");
        }
    }

    private static void AgregarParametrosProveedor(MySqlCommand cmd, Proveedor p)
    {
        cmd.Parameters.AddWithValue("@rut", p.Rut.Trim());
        cmd.Parameters.AddWithValue("@razon", p.RazonSocial.Trim());
        cmd.Parameters.AddWithValue("@contacto", string.IsNullOrWhiteSpace(p.Contacto) ? DBNull.Value : p.Contacto.Trim());
        cmd.Parameters.AddWithValue("@telefono", string.IsNullOrWhiteSpace(p.Telefono) ? DBNull.Value : p.Telefono.Trim());
        cmd.Parameters.AddWithValue("@email", string.IsNullOrWhiteSpace(p.Email) ? DBNull.Value : p.Email.Trim());
        cmd.Parameters.AddWithValue("@condiciones", string.IsNullOrWhiteSpace(p.CondicionesComerciales) ? DBNull.Value : p.CondicionesComerciales.Trim());
    }

    // =========================================================
    // CREACIÓN Y FLUJO DE AUTORIZACIÓN DE ÓRDENES
    // =========================================================

    public async Task<List<ProductoSelector>> ObtenerProductosParaCompraAsync()
    {
        const string sql = @"
            SELECT id_producto, sku, nombre_comercial, costo_promedio_ponderado 
            FROM productos 
            WHERE activo = 1 
            ORDER BY nombre_comercial ASC";

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, cn);
        await using var rd = await cmd.ExecuteReaderAsync();

        var lista = new List<ProductoSelector>();
        while (await rd.ReadAsync())
        {
            lista.Add(new ProductoSelector
            {
                Id = rd.GetInt32(0),
                Sku = rd.GetString(1),
                Nombre = rd.GetString(2),
                Costo = rd.GetDecimal(3)
            });
        }
        return lista;
    }

    public async Task<(int IdOrden, string NumeroOc, string Estado)> CrearOrdenCompraAsync(int idProveedor, int idUsuario, bool esAdminOGerente, decimal subtotal, decimal iva, decimal total, List<CarritoItem> carrito)
    {
        if (carrito == null || carrito.Count == 0 || carrito.Any(c => c.Cantidad <= 0 || c.PrecioUnitario < 0))
            throw new InvalidOperationException("La orden de compra debe incluir al menos un ítem válido con cantidad mayor a cero.");

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        await using var transaccion = await cn.BeginTransactionAsync();

        try
        {
            // Flujo de autorización jerárquica
            // Si supera el límite de gasto y el usuario no tiene rol superior, queda retenida
            string estadoInicial = (total > LimiteGastoAutorizacion && !esAdminOGerente)
                ? "PENDIENTE_AUTORIZACION"
                : "EMITIDA";

            // Generar número de orden único y libre de colisiones dentro de la transacción
            const string sqlNextSeq = "SELECT COALESCE(MAX(id_orden_compra), 0) + 1 FROM ordenes_compra";
            int seq = Convert.ToInt32(await new MySqlCommand(sqlNextSeq, cn, transaccion).ExecuteScalarAsync());
            string numeroOc;
            while (true)
            {
                numeroOc = $"OC-{DateTime.Now.Year}-{seq:D4}";
                const string sqlExisteNum = "SELECT COUNT(*) FROM ordenes_compra WHERE numero_oc = @numOC";
                await using var cmdCheckNum = new MySqlCommand(sqlExisteNum, cn, transaccion);
                cmdCheckNum.Parameters.AddWithValue("@numOC", numeroOc);
                if (Convert.ToInt32(await cmdCheckNum.ExecuteScalarAsync()) == 0)
                    break;
                seq++;
            }

            const string sqlOrden = @"
                INSERT INTO ordenes_compra (numero_oc, id_proveedor, fecha_emision, estado, monto_subtotal, monto_iva, monto_total, id_usuario_creador)
                VALUES (@numOC, @idProv, CURRENT_DATE, @estado, @sub, @iva, @tot, @idUser);
                SELECT LAST_INSERT_ID();";

            await using var cmdOrden = new MySqlCommand(sqlOrden, cn, transaccion);
            cmdOrden.Parameters.AddWithValue("@numOC", numeroOc);
            cmdOrden.Parameters.AddWithValue("@idProv", idProveedor);
            cmdOrden.Parameters.AddWithValue("@estado", estadoInicial);
            cmdOrden.Parameters.AddWithValue("@sub", subtotal);
            cmdOrden.Parameters.AddWithValue("@iva", iva);
            cmdOrden.Parameters.AddWithValue("@tot", total);
            cmdOrden.Parameters.AddWithValue("@idUser", idUsuario);

            int idOrdenGenerada = Convert.ToInt32(await cmdOrden.ExecuteScalarAsync());

            const string sqlDetalle = @"
                INSERT INTO detalle_orden_compra (id_orden_compra, id_producto, cantidad_solicitada, cantidad_recibida, costo_pactado_unitario)
                VALUES (@idOrd, @idProd, @cant, 0, @costo)";

            foreach (var item in carrito)
            {
                await using var cmdDetalle = new MySqlCommand(sqlDetalle, cn, transaccion);
                cmdDetalle.Parameters.AddWithValue("@idOrd", idOrdenGenerada);
                cmdDetalle.Parameters.AddWithValue("@idProd", item.IdProducto);
                cmdDetalle.Parameters.AddWithValue("@cant", item.Cantidad);
                cmdDetalle.Parameters.AddWithValue("@costo", item.PrecioUnitario);

                await cmdDetalle.ExecuteNonQueryAsync();
            }

            await transaccion.CommitAsync();
            UltimaOrdenCreadaId = idOrdenGenerada;
            return (idOrdenGenerada, numeroOc, estadoInicial);
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    public async Task AprobarOrdenCompraAsync(int idOrden)
    {
        const string sql = "UPDATE ordenes_compra SET estado = 'EMITIDA' WHERE id_orden_compra = @id AND estado = 'PENDIENTE_AUTORIZACION'";
        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@id", idOrden);
        int afectadas = await cmd.ExecuteNonQueryAsync();
        if (afectadas == 0)
            throw new InvalidOperationException("La orden ya fue procesada o no se encuentra pendiente de autorización.");
    }

    public async Task RechazarOrdenCompraAsync(int idOrden, string motivo)
    {
        const string sql = "UPDATE ordenes_compra SET estado = 'ANULADA' WHERE id_orden_compra = @id AND estado = 'PENDIENTE_AUTORIZACION'";
        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@id", idOrden);
        int afectadas = await cmd.ExecuteNonQueryAsync();
        if (afectadas == 0)
            throw new InvalidOperationException("La orden ya fue procesada o no se encuentra pendiente de autorización.");
    }

    public async Task AnularOrdenCompraAsync(int idOrden)
    {
        const string sql = "UPDATE ordenes_compra SET estado = 'ANULADA' WHERE id_orden_compra = @id AND estado IN ('BORRADOR', 'EMITIDA', 'PENDIENTE_AUTORIZACION')";
        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@id", idOrden);
        int afectadas = await cmd.ExecuteNonQueryAsync();
        if (afectadas == 0)
            throw new InvalidOperationException("La orden ya fue anulada, recibida o cambió de estado.");
    }

    // =========================================================
    // SUGERENCIA AUTOMATIZADA DE REABASTECIMIENTO
    // =========================================================

    public async Task<List<SugerenciaReabastecimientoItem>> ObtenerSugerenciasReabastecimientoAsync()
    {
        const string sql = @"
            SELECT 
                p.id_producto,
                p.sku,
                p.nombre_comercial,
                p.punto_reorden,
                p.stock_minimo,
                COALESCE(SUM(l.stock_actual), 0) AS stock_actual,
                p.costo_promedio_ponderado,
                GREATEST((p.punto_reorden * 2) - COALESCE(SUM(l.stock_actual), 0), p.stock_minimo) AS cantidad_sugerida
            FROM productos p
            LEFT JOIN lotes l ON l.id_producto = p.id_producto
            WHERE p.activo = 1
            GROUP BY p.id_producto, p.sku, p.nombre_comercial, p.punto_reorden, p.stock_minimo, p.costo_promedio_ponderado
            HAVING stock_actual <= p.punto_reorden
            ORDER BY (stock_actual - p.punto_reorden) ASC";

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, cn);
        await using var rd = await cmd.ExecuteReaderAsync();

        var lista = new List<SugerenciaReabastecimientoItem>();
        while (await rd.ReadAsync())
        {
            lista.Add(new SugerenciaReabastecimientoItem
            {
                IdProducto = rd.GetInt32(0),
                Sku = rd.GetString(1),
                NombreProducto = rd.GetString(2),
                PuntoReorden = rd.GetInt32(3),
                StockMinimo = rd.GetInt32(4),
                StockActual = Convert.ToInt32(rd.GetValue(5)),
                CostoEstimado = rd.GetDecimal(6),
                CantidadSugerida = Math.Max(1, Convert.ToInt32(rd.GetValue(7)))
            });
        }
        return lista;
    }

    // =========================================================
    // RECEPCIÓN Y LIQUIDACIÓN DE FACTURAS
    // =========================================================

    public async Task<List<OrdenCompra>> ObtenerOrdenesParaRecepcionAsync()
    {
        const string sql = @"
            SELECT o.id_orden_compra, o.numero_oc, o.fecha_emision, o.estado,
                   o.monto_subtotal, o.monto_iva, o.monto_total,
                   p.razon_social, p.rut_proveedor, p.nombre_contacto, u.nombre,
                   o.id_proveedor, o.id_usuario_creador
            FROM ordenes_compra o
            JOIN proveedores p ON p.id_proveedor = o.id_proveedor
            JOIN usuarios u ON u.id_usuario = o.id_usuario_creador
            WHERE o.estado IN ('EMITIDA', 'PARCIAL')
            ORDER BY o.fecha_emision DESC";

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, cn);
        await using var rd = await cmd.ExecuteReaderAsync();

        var lista = new List<OrdenCompra>();
        while (await rd.ReadAsync())
        {
            lista.Add(new OrdenCompra
            {
                Id = rd.GetInt32(0),
                Numero = rd.GetString(1),
                FechaEmision = Convert.ToDateTime(rd.GetValue(2)),
                Estado = rd.GetString(3),
                Subtotal = Dec(rd, 4),
                Iva = Dec(rd, 5),
                Total = Dec(rd, 6),
                Proveedor = rd.GetString(7),
                RutProveedor = rd.GetString(8),
                Contacto = rd.IsDBNull(9) ? "—" : rd.GetString(9),
                Creador = rd.GetString(10),
                IdProveedor = rd.GetInt32(11),
                IdUsuarioCreador = rd.GetInt32(12)
            });
        }
        return lista;
    }

    public async Task<List<LineaRecepcionFacturaItem>> ObtenerLineasParaRecepcionAsync(int idOrden)
    {
        const string sql = @"
            SELECT d.id_detalle_oc, d.id_producto, p.sku, p.nombre_comercial,
                   d.cantidad_solicitada, d.cantidad_recibida, d.costo_pactado_unitario
            FROM detalle_orden_compra d
            JOIN productos p ON p.id_producto = d.id_producto
            WHERE d.id_orden_compra = @id
            ORDER BY d.id_detalle_oc";

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@id", idOrden);
        await using var rd = await cmd.ExecuteReaderAsync();

        var lineas = new List<LineaRecepcionFacturaItem>();
        while (await rd.ReadAsync())
        {
            int solicitada = Ent(rd, 4);
            int recibida = Ent(rd, 5);
            int pendiente = Math.Max(0, solicitada - recibida);
            decimal costo = Dec(rd, 6);

            lineas.Add(new LineaRecepcionFacturaItem
            {
                IdDetalleOc = rd.GetInt32(0),
                IdProducto = rd.GetInt32(1),
                Sku = rd.GetString(2),
                NombreProducto = rd.GetString(3),
                CantidadSolicitada = solicitada,
                CantidadPreviaRecibida = recibida,
                CostoPactado = costo,
                CostoFacturado = costo,
                CantidadARecibir = pendiente, // Por defecto propone recibir el saldo pendiente
                NumeroLote = $"LOT-{DateTime.Now.Year}-{new Random().Next(100, 999)}",
                FechaVencimiento = DateTime.Today.AddYears(2),
                EnCuarentena = false
            });
        }
        return lineas;
    }

    public async Task<(int IdRecepcion, string NumeroRecepcion)> RegistrarRecepcionFacturaAsync(RecepcionFacturaInput input, int idUsuario)
    {
        string numeroFactura = input.NumeroFactura?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(numeroFactura))
            throw new InvalidOperationException("Debe ingresar el número de factura del proveedor.");

        var lineasARecibir = input.Lineas.Where(l => l.CantidadARecibir > 0).ToList();
        if (lineasARecibir.Count == 0)
            throw new InvalidOperationException("Debe ingresar al menos un ítem con cantidad a recibir mayor a cero.");

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        await using var transaccion = await cn.BeginTransactionAsync();

        try
        {
            // 0. Bloquear y validar estado actual de la orden de compra (previene liquidaciones duplicadas / fantasmas)
            string? estadoOcActual = null;
            int idProveedorOc = 0;
            const string sqlLockOc = @"
                SELECT estado, id_proveedor
                FROM ordenes_compra
                WHERE id_orden_compra = @idOc
                FOR UPDATE;";
            await using (var cmdLock = new MySqlCommand(sqlLockOc, cn, transaccion))
            {
                cmdLock.Parameters.AddWithValue("@idOc", input.IdOrdenCompra);
                await using var rdOc = await cmdLock.ExecuteReaderAsync();
                if (await rdOc.ReadAsync())
                {
                    estadoOcActual = rdOc.GetString(0);
                    idProveedorOc = rdOc.GetInt32(1);
                }
            }

            if (estadoOcActual is null)
                throw new InvalidOperationException("La orden de compra seleccionada ya no existe.");

            if (estadoOcActual != "EMITIDA" && estadoOcActual != "PARCIAL")
                throw new InvalidOperationException($"La orden de compra ya cambió a estado '{estadoOcActual}' y no admite más recepciones.");

            // Verificar factura duplicada para el proveedor y para la orden antes de insertar
            const string sqlDupCxP = "SELECT COUNT(*) FROM cuentas_por_pagar WHERE id_proveedor = @idProv AND numero_documento = @numFactura";
            await using (var cmdDupCxP = new MySqlCommand(sqlDupCxP, cn, transaccion))
            {
                cmdDupCxP.Parameters.AddWithValue("@idProv", idProveedorOc);
                cmdDupCxP.Parameters.AddWithValue("@numFactura", numeroFactura);
                if (Convert.ToInt32(await cmdDupCxP.ExecuteScalarAsync()) > 0)
                    throw new InvalidOperationException($"Ya existe una factura registrada con el número '{numeroFactura}' para este proveedor.");
            }

            const string sqlDupRec = "SELECT COUNT(*) FROM recepciones_facturas_compra WHERE id_orden_compra = @idOc AND numero_factura_proveedor = @numFactura";
            await using (var cmdDupRec = new MySqlCommand(sqlDupRec, cn, transaccion))
            {
                cmdDupRec.Parameters.AddWithValue("@idOc", input.IdOrdenCompra);
                cmdDupRec.Parameters.AddWithValue("@numFactura", numeroFactura);
                if (Convert.ToInt32(await cmdDupRec.ExecuteScalarAsync()) > 0)
                    throw new InvalidOperationException($"La factura '{numeroFactura}' ya fue liquidada anteriormente para esta orden.");
            }

            // Bloquear y validar saldos pendientes línea por línea antes de registrar movimientos
            foreach (var item in lineasARecibir)
            {
                if (string.IsNullOrWhiteSpace(item.NumeroLote))
                    throw new InvalidOperationException($"Debe ingresar el número de lote para '{item.NombreProducto}'.");

                if (item.CostoFacturado < 0)
                    throw new InvalidOperationException($"El costo facturado de '{item.NombreProducto}' no puede ser negativo.");

                const string sqlLockDet = @"
                    SELECT cantidad_solicitada, cantidad_recibida
                    FROM detalle_orden_compra
                    WHERE id_detalle_oc = @idDetOc AND id_orden_compra = @idOc
                    FOR UPDATE;";
                await using var cmdLockDet = new MySqlCommand(sqlLockDet, cn, transaccion);
                cmdLockDet.Parameters.AddWithValue("@idDetOc", item.IdDetalleOc);
                cmdLockDet.Parameters.AddWithValue("@idOc", input.IdOrdenCompra);

                await using var rdDet = await cmdLockDet.ExecuteReaderAsync();
                if (!await rdDet.ReadAsync())
                    throw new InvalidOperationException($"El ítem '{item.NombreProducto}' no pertenece a la orden seleccionada.");

                int cantSolicitada = rdDet.GetInt32(0);
                int cantRecibidaPrevia = rdDet.GetInt32(1);
                int saldoDisponible = Math.Max(0, cantSolicitada - cantRecibidaPrevia);

                if (item.CantidadARecibir > saldoDisponible)
                    throw new InvalidOperationException($"La cantidad a recibir para '{item.NombreProducto}' ({item.CantidadARecibir} un.) excede el saldo pendiente ({saldoDisponible} un.). Es posible que la orden ya haya sido liquidada.");
            }

            // 1. Insertar encabezado de recepción de factura
            const string sqlRecepcion = @"
                INSERT INTO recepciones_facturas_compra 
                    (id_orden_compra, numero_factura_proveedor, fecha_factura, fecha_recepcion, monto_total_facturado)
                VALUES 
                    (@idOc, @numFactura, @fechaFactura, CURRENT_TIMESTAMP, @montoTotal);
                SELECT LAST_INSERT_ID();";

            await using var cmdRec = new MySqlCommand(sqlRecepcion, cn, transaccion);
            cmdRec.Parameters.AddWithValue("@idOc", input.IdOrdenCompra);
            cmdRec.Parameters.AddWithValue("@numFactura", numeroFactura);
            cmdRec.Parameters.AddWithValue("@fechaFactura", input.FechaFactura.Date);
            cmdRec.Parameters.AddWithValue("@montoTotal", input.MontoTotalFacturado);

            int idRecepcion = Convert.ToInt32(await cmdRec.ExecuteScalarAsync());

            // Verificar si la tabla lotes en la base de datos activa tiene la columna estado_cuarentena
            const string sqlHasCuarentena = @"
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = 'lotes'
                  AND COLUMN_NAME = 'estado_cuarentena';";
            bool soportaEstadoCuarentena;
            await using (var cmdCol = new MySqlCommand(sqlHasCuarentena, cn, transaccion))
            {
                soportaEstadoCuarentena = Convert.ToInt32(await cmdCol.ExecuteScalarAsync()) > 0;
            }

            // 2. Procesar cada ítem recibido (Detalle, Lote, Cuarentena, Stock, Kardex)
            foreach (var item in lineasARecibir)
            {
                string codigoLote = item.NumeroLote.Trim().ToUpperInvariant();

                // 2.1 Insertar en detalle_recepcion_lotes
                const string sqlDetRec = @"
                    INSERT INTO detalle_recepcion_lotes
                        (id_recepcion, id_producto, numero_lote, fecha_vencimiento, cantidad_ingresada, costo_adquisicion_unitario)
                    VALUES
                        (@idRec, @idProd, @lote, @vence, @cant, @costo);";

                await using var cmdDetRec = new MySqlCommand(sqlDetRec, cn, transaccion);
                cmdDetRec.Parameters.AddWithValue("@idRec", idRecepcion);
                cmdDetRec.Parameters.AddWithValue("@idProd", item.IdProducto);
                cmdDetRec.Parameters.AddWithValue("@lote", codigoLote);
                cmdDetRec.Parameters.AddWithValue("@vence", item.FechaVencimiento.Date);
                cmdDetRec.Parameters.AddWithValue("@cant", item.CantidadARecibir);
                cmdDetRec.Parameters.AddWithValue("@costo", item.CostoFacturado);
                await cmdDetRec.ExecuteNonQueryAsync();

                // 2.2 Buscar o crear lote en la tabla lotes (persistiendo estado_cuarentena si la columna existe)
                int idLoteFinal;
                const string sqlCheckLote = "SELECT id_lote, stock_actual FROM lotes WHERE id_producto = @idProd AND numero_lote = @lote LIMIT 1";
                await using var cmdLote = new MySqlCommand(sqlCheckLote, cn, transaccion);
                cmdLote.Parameters.AddWithValue("@idProd", item.IdProducto);
                cmdLote.Parameters.AddWithValue("@lote", codigoLote);

                await using (var rdLote = await cmdLote.ExecuteReaderAsync())
                {
                    if (await rdLote.ReadAsync())
                    {
                        idLoteFinal = rdLote.GetInt32(0);
                    }
                    else
                    {
                        idLoteFinal = 0;
                    }
                }

                if (idLoteFinal > 0)
                {
                    string sqlUpdLote = soportaEstadoCuarentena
                        ? @"
                            UPDATE lotes
                            SET stock_actual = stock_actual + @cant,
                                estado_cuarentena = CASE WHEN @cuarentena = 1 THEN 'CUARENTENA' ELSE estado_cuarentena END
                            WHERE id_lote = @idLote"
                        : "UPDATE lotes SET stock_actual = stock_actual + @cant WHERE id_lote = @idLote";

                    await using var cmdUpdLote = new MySqlCommand(sqlUpdLote, cn, transaccion);
                    cmdUpdLote.Parameters.AddWithValue("@cant", item.CantidadARecibir);
                    if (soportaEstadoCuarentena)
                        cmdUpdLote.Parameters.AddWithValue("@cuarentena", item.EnCuarentena ? 1 : 0);
                    cmdUpdLote.Parameters.AddWithValue("@idLote", idLoteFinal);
                    await cmdUpdLote.ExecuteNonQueryAsync();
                }
                else if (soportaEstadoCuarentena)
                {
                    string estadoCuarentena = item.EnCuarentena ? "CUARENTENA" : "LIBERADO";
                    const string sqlInsLote = @"
                        INSERT INTO lotes (id_producto, numero_lote, fecha_vencimiento, stock_actual, estado_cuarentena)
                        VALUES (@idProd, @lote, @vence, @cant, @estadoCuarentena);
                        SELECT LAST_INSERT_ID();";
                    await using var cmdInsLote = new MySqlCommand(sqlInsLote, cn, transaccion);
                    cmdInsLote.Parameters.AddWithValue("@idProd", item.IdProducto);
                    cmdInsLote.Parameters.AddWithValue("@lote", codigoLote);
                    cmdInsLote.Parameters.AddWithValue("@vence", item.FechaVencimiento.Date);
                    cmdInsLote.Parameters.AddWithValue("@cant", item.CantidadARecibir);
                    cmdInsLote.Parameters.AddWithValue("@estadoCuarentena", estadoCuarentena);
                    idLoteFinal = Convert.ToInt32(await cmdInsLote.ExecuteScalarAsync());
                }
                else
                {
                    const string sqlInsLote = @"
                        INSERT INTO lotes (id_producto, numero_lote, fecha_vencimiento, stock_actual)
                        VALUES (@idProd, @lote, @vence, @cant);
                        SELECT LAST_INSERT_ID();";
                    await using var cmdInsLote = new MySqlCommand(sqlInsLote, cn, transaccion);
                    cmdInsLote.Parameters.AddWithValue("@idProd", item.IdProducto);
                    cmdInsLote.Parameters.AddWithValue("@lote", codigoLote);
                    cmdInsLote.Parameters.AddWithValue("@vence", item.FechaVencimiento.Date);
                    cmdInsLote.Parameters.AddWithValue("@cant", item.CantidadARecibir);
                    idLoteFinal = Convert.ToInt32(await cmdInsLote.ExecuteScalarAsync());
                }

                // 2.3 Obtener saldo total del producto para registrar el Kardex
                const string sqlSaldo = "SELECT COALESCE(SUM(stock_actual), 0) FROM lotes WHERE id_producto = @idProd";
                await using var cmdSaldo = new MySqlCommand(sqlSaldo, cn, transaccion);
                cmdSaldo.Parameters.AddWithValue("@idProd", item.IdProducto);
                int saldoActualizado = Convert.ToInt32(await cmdSaldo.ExecuteScalarAsync());

                // 2.4 Inyectar movimiento en Kardex (ENTRADA por COMPRA)
                const string sqlKardex = @"
                    INSERT INTO kardex_movimientos 
                        (id_producto, id_lote, tipo_movimiento, origen_documento, id_referencia_origen, cantidad, costo_unitario, saldo_stock_producto, id_usuario)
                    VALUES 
                        (@idProd, @idLote, 'ENTRADA', 'COMPRA', @idRec, @cant, @costo, @saldo, @idUser)";
                await using var cmdKardex = new MySqlCommand(sqlKardex, cn, transaccion);
                cmdKardex.Parameters.AddWithValue("@idProd", item.IdProducto);
                cmdKardex.Parameters.AddWithValue("@idLote", idLoteFinal);
                cmdKardex.Parameters.AddWithValue("@idRec", idRecepcion);
                cmdKardex.Parameters.AddWithValue("@cant", item.CantidadARecibir);
                cmdKardex.Parameters.AddWithValue("@costo", item.CostoFacturado);
                cmdKardex.Parameters.AddWithValue("@saldo", saldoActualizado);
                cmdKardex.Parameters.AddWithValue("@idUser", idUsuario);
                await cmdKardex.ExecuteNonQueryAsync();

                // 2.5 Actualizar cantidad_recibida en detalle_orden_compra
                const string sqlUpdDetOc = @"
                    UPDATE detalle_orden_compra 
                    SET cantidad_recibida = cantidad_recibida + @cant
                    WHERE id_detalle_oc = @idDetOc";
                await using var cmdUpdDetOc = new MySqlCommand(sqlUpdDetOc, cn, transaccion);
                cmdUpdDetOc.Parameters.AddWithValue("@cant", item.CantidadARecibir);
                cmdUpdDetOc.Parameters.AddWithValue("@idDetOc", item.IdDetalleOc);
                await cmdUpdDetOc.ExecuteNonQueryAsync();
            }

            // 3. Evaluar y actualizar estado de la orden de compra (PARCIAL vs RECIBIDA)
            const string sqlCheckOc = @"
                SELECT 
                    COALESCE(SUM(cantidad_solicitada), 0) AS total_solicitado,
                    COALESCE(SUM(cantidad_recibida), 0) AS total_recibido
                FROM detalle_orden_compra
                WHERE id_orden_compra = @idOc";

            int solTotal = 0, recTotal = 0;
            await using (var cmdCheck = new MySqlCommand(sqlCheckOc, cn, transaccion))
            {
                cmdCheck.Parameters.AddWithValue("@idOc", input.IdOrdenCompra);
                await using var rd = await cmdCheck.ExecuteReaderAsync();
                if (await rd.ReadAsync())
                {
                    solTotal = Ent(rd, 0);
                    recTotal = Ent(rd, 1);
                }
            }

            string nuevoEstadoOc = (recTotal >= solTotal && solTotal > 0) ? "RECIBIDA" : "PARCIAL";
            const string sqlUpdOc = "UPDATE ordenes_compra SET estado = @estado WHERE id_orden_compra = @idOc";
            await using (var cmdUpd = new MySqlCommand(sqlUpdOc, cn, transaccion))
            {
                cmdUpd.Parameters.AddWithValue("@estado", nuevoEstadoOc);
                cmdUpd.Parameters.AddWithValue("@idOc", input.IdOrdenCompra);
                await cmdUpd.ExecuteNonQueryAsync();
            }

            // 4. Generar obligación en cuentas_por_pagar (integración Compras - Contabilidad)
            const string sqlCxP = @"
                INSERT INTO cuentas_por_pagar
                    (id_recepcion, id_proveedor, numero_documento, fecha_emision, fecha_vencimiento, monto_total, monto_pagado, estado_pago)
                VALUES
                    (@idRec, @idProv, @numDoc, @fEmision, @fVence, @monto, 0, 'PENDIENTE');";

            await using var cmdCxP = new MySqlCommand(sqlCxP, cn, transaccion);
            cmdCxP.Parameters.AddWithValue("@idRec", idRecepcion);
            cmdCxP.Parameters.AddWithValue("@idProv", input.IdProveedor);
            cmdCxP.Parameters.AddWithValue("@numDoc", input.NumeroFactura.Trim());
            cmdCxP.Parameters.AddWithValue("@fEmision", input.FechaFactura.Date);
            cmdCxP.Parameters.AddWithValue("@fVence", input.FechaFactura.Date.AddDays(30)); // Plazo estándar 30 días
            cmdCxP.Parameters.AddWithValue("@monto", input.MontoTotalFacturado);
            await cmdCxP.ExecuteNonQueryAsync();

            // 5. Asiento Contable Automático en Libro Diario
            // Debe: Inventario de Mercaderías (1.1.02) + IVA Crédito Fiscal (1.1.04)
            // Haber: Cuentas por Pagar Proveedores (2.1.01)
            try
            {
                int nextAsientoNum = 1;
                const string sqlNextNum = "SELECT COALESCE(MAX(numero_asiento), 0) + 1 FROM libro_diario_asientos";
                await using var cmdNextNum = new MySqlCommand(sqlNextNum, cn, transaccion);
                nextAsientoNum = Convert.ToInt32(await cmdNextNum.ExecuteScalarAsync());

                const string sqlAsiento = @"
                    INSERT INTO libro_diario_asientos 
                        (numero_asiento, fecha_asiento, glosa_descripcion, origen, id_usuario)
                    VALUES 
                        (@num, CURRENT_DATE, @glosa, 'COMPRA', @user);
                    SELECT LAST_INSERT_ID();";

                await using var cmdAsiento = new MySqlCommand(sqlAsiento, cn, transaccion);
                cmdAsiento.Parameters.AddWithValue("@num", nextAsientoNum);
                cmdAsiento.Parameters.AddWithValue("@glosa", $"Recepción Factura {input.NumeroFactura} - Liquidación OC #{input.IdOrdenCompra}");
                cmdAsiento.Parameters.AddWithValue("@user", idUsuario);
                int idAsiento = Convert.ToInt32(await cmdAsiento.ExecuteScalarAsync());

                decimal montoNeto = Math.Round(input.MontoTotalFacturado / 1.19m, 2);
                decimal montoIva = input.MontoTotalFacturado - montoNeto;

                // Cuentas contables:
                // 7 = 1.1.02 (Inventario), 13 = 1.1.04 (IVA Crédito Fiscal), 11 = 2.1.01 (Cuentas por Pagar)
                const string sqlDetAsiento = @"
                    INSERT INTO detalle_asiento_contable (id_asiento, id_cuenta, debe, haber) VALUES
                    (@idAsiento, 7, @neto, 0),
                    (@idAsiento, 13, @iva, 0),
                    (@idAsiento, 11, 0, @total)";

                await using var cmdDetAsiento = new MySqlCommand(sqlDetAsiento, cn, transaccion);
                cmdDetAsiento.Parameters.AddWithValue("@idAsiento", idAsiento);
                cmdDetAsiento.Parameters.AddWithValue("@neto", montoNeto);
                cmdDetAsiento.Parameters.AddWithValue("@iva", montoIva);
                cmdDetAsiento.Parameters.AddWithValue("@total", input.MontoTotalFacturado);
                await cmdDetAsiento.ExecuteNonQueryAsync();

                // Asociar id_asiento_contable a la recepción
                const string sqlLinkAsiento = "UPDATE recepciones_facturas_compra SET id_asiento_contable = @idAsiento WHERE id_recepcion = @idRec";
                await using var cmdLink = new MySqlCommand(sqlLinkAsiento, cn, transaccion);
                cmdLink.Parameters.AddWithValue("@idAsiento", idAsiento);
                cmdLink.Parameters.AddWithValue("@idRec", idRecepcion);
                await cmdLink.ExecuteNonQueryAsync();
            }
            catch
            {
                // Si la tabla contable no tiene esas cuentas exactas no abortamos la recepción física
            }

            await transaccion.CommitAsync();
            return (idRecepcion, input.NumeroFactura);
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    // =========================================================
    // REPORTE: REGISTRO DE COMPRAS POR LABORATORIO
    // =========================================================

    public async Task<(ResumenReporteCompras Resumen, List<CompraPorProveedor> Filas)> ObtenerReporteComprasAsync(
        DateTime desde, DateTime hasta, string? rutFiltro)
    {
        const string sql = @"
            SELECT p.razon_social, p.rut_proveedor,
                   COUNT(o.id_orden_compra) AS num_ordenes,
                   COUNT(CASE WHEN o.estado IN ('BORRADOR','EMITIDA','PARCIAL') THEN 1 END) AS pendientes,
                   COUNT(CASE WHEN o.estado = 'RECIBIDA' THEN 1 END) AS liquidadas,
                   COALESCE(SUM(CASE WHEN o.estado <> 'ANULADA' THEN o.monto_total END), 0) AS monto_total,
                   MAX(o.fecha_emision) AS ultima_compra
            FROM proveedores p
            LEFT JOIN ordenes_compra o
                   ON o.id_proveedor = p.id_proveedor
                  AND o.fecha_emision BETWEEN @desde AND @hasta
            WHERE (@rut IS NULL OR p.rut_proveedor LIKE CONCAT('%', @rut, '%'))
            GROUP BY p.id_proveedor, p.razon_social, p.rut_proveedor
            ORDER BY monto_total DESC";

        const string sqlPorRecibir = @"
            SELECT p.id_proveedor,
                   COALESCE(SUM((d.cantidad_solicitada - d.cantidad_recibida) * d.costo_pactado_unitario), 0)
            FROM proveedores p
            JOIN ordenes_compra o ON o.id_proveedor = p.id_proveedor AND o.estado IN ('EMITIDA','PARCIAL')
            JOIN detalle_orden_compra d ON d.id_orden_compra = o.id_orden_compra
            WHERE o.fecha_emision BETWEEN @desde AND @hasta
            GROUP BY p.id_proveedor";

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        var filas = new List<CompraPorProveedor>();
        await using (var cmd = new MySqlCommand(sql, cn))
        {
            cmd.Parameters.AddWithValue("@desde", desde.Date);
            cmd.Parameters.AddWithValue("@hasta", hasta.Date.AddDays(1).AddSeconds(-1));
            cmd.Parameters.AddWithValue("@rut", string.IsNullOrWhiteSpace(rutFiltro) ? (object)DBNull.Value : rutFiltro.Trim());

            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                filas.Add(new CompraPorProveedor
                {
                    Proveedor = rd.GetString(0),
                    Rut = rd.GetString(1),
                    NumeroOrdenes = Ent(rd, 2),
                    OrdenesPendientes = Ent(rd, 3),
                    OrdenesLiquidadas = Ent(rd, 4),
                    MontoTotal = Dec(rd, 5),
                    UltimaCompra = rd.IsDBNull(6) ? null : Convert.ToDateTime(rd.GetValue(6))
                });
            }
        }

        decimal montoPorRecibirTotal = 0m;
        await using (var cmd = new MySqlCommand(sqlPorRecibir, cn))
        {
            cmd.Parameters.AddWithValue("@desde", desde.Date);
            cmd.Parameters.AddWithValue("@hasta", hasta.Date.AddDays(1).AddSeconds(-1));
            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
                montoPorRecibirTotal += Dec(rd, 1);
        }

        var resumen = new ResumenReporteCompras
        {
            ProveedoresConCompras = filas.Count(f => f.NumeroOrdenes > 0),
            TotalOrdenes = filas.Sum(f => f.NumeroOrdenes),
            MontoTotalPeriodo = filas.Sum(f => f.MontoTotal),
            MontoPorRecibir = montoPorRecibirTotal
        };

        return (resumen, filas);
    }

    // =========================================================
    // REPORTE: CUMPLIMIENTO DE ÓRDENES Y SLA DE PROVEEDORES
    // =========================================================

    public async Task<(ResumenCumplimientoOrdenes Resumen, List<ItemCumplimientoOrden> Items)> ObtenerReporteCumplimientoAsync(
        DateTime desde, DateTime hasta, string? estadoFiltro, int? idProveedorFiltro)
    {
        const string sql = @"
            SELECT 
                o.id_orden_compra,
                o.numero_oc,
                o.fecha_emision,
                p.razon_social,
                p.rut_proveedor,
                o.estado,
                COALESCE(SUM(d.cantidad_solicitada), 0) AS total_solicitado,
                COALESCE(SUM(d.cantidad_recibida), 0) AS total_recibido,
                o.monto_total,
                (SELECT MAX(r.fecha_recepcion) FROM recepciones_facturas_compra r WHERE r.id_orden_compra = o.id_orden_compra) AS ult_recepcion
            FROM ordenes_compra o
            JOIN proveedores p ON p.id_proveedor = o.id_proveedor
            LEFT JOIN detalle_orden_compra d ON d.id_orden_compra = o.id_orden_compra
            WHERE o.fecha_emision BETWEEN @desde AND @hasta
              AND (@estado IS NULL OR @estado = 'TODAS' OR o.estado = @estado)
              AND (@idProv IS NULL OR o.id_proveedor = @idProv)
            GROUP BY o.id_orden_compra, o.numero_oc, o.fecha_emision, p.razon_social, p.rut_proveedor, o.estado, o.monto_total
            ORDER BY o.fecha_emision DESC";

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@desde", desde.Date);
        cmd.Parameters.AddWithValue("@hasta", hasta.Date.AddDays(1).AddSeconds(-1));
        cmd.Parameters.AddWithValue("@estado", string.IsNullOrWhiteSpace(estadoFiltro) || estadoFiltro == "TODAS" ? (object)DBNull.Value : estadoFiltro);
        cmd.Parameters.AddWithValue("@idProv", idProveedorFiltro.HasValue && idProveedorFiltro.Value > 0 ? (object)idProveedorFiltro.Value : DBNull.Value);

        var lista = new List<ItemCumplimientoOrden>();
        await using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            lista.Add(new ItemCumplimientoOrden
            {
                IdOrdenCompra = rd.GetInt32(0),
                NumeroOc = rd.GetString(1),
                FechaEmision = Convert.ToDateTime(rd.GetValue(2)),
                Proveedor = rd.GetString(3),
                RutProveedor = rd.GetString(4),
                Estado = rd.GetString(5),
                TotalSolicitado = Ent(rd, 6),
                TotalRecibido = Ent(rd, 7),
                MontoTotal = Dec(rd, 8),
                FechaUltimaRecepcion = rd.IsDBNull(9) ? null : Convert.ToDateTime(rd.GetValue(9))
            });
        }

        var resumen = new ResumenCumplimientoOrdenes
        {
            TotalOrdenes = lista.Count,
            Liquidadas = lista.Count(x => x.Estado == "RECIBIDA"),
            Parciales = lista.Count(x => x.Estado == "PARCIAL"),
            Pendientes = lista.Count(x => x.Estado is "EMITIDA" or "BORRADOR" or "PENDIENTE_AUTORIZACION"),
            TotalUnidadesPedidas = lista.Sum(x => x.TotalSolicitado),
            TotalUnidadesRecibidas = lista.Sum(x => x.TotalRecibido)
        };

        return (resumen, lista);
    }

    // =========================================================
    // REPORTE: HISTORIAL DE PRECIOS Y COSTOS DE ADQUISICIÓN
    // =========================================================

    public async Task<(ResumenHistorialPrecios Resumen, List<ItemHistorialPrecio> Items)> ObtenerReporteHistorialPreciosAsync(
        int? idProductoFiltro, DateTime desde, DateTime hasta)
    {
        const string sql = @"
            SELECT 
                r.fecha_factura AS fecha,
                r.numero_factura_proveedor AS documento,
                p.sku,
                p.nombre_comercial,
                pr.razon_social AS proveedor,
                dr.cantidad_ingresada AS cantidad,
                dr.costo_adquisicion_unitario AS costo
            FROM detalle_recepcion_lotes dr
            JOIN recepciones_facturas_compra r ON r.id_recepcion = dr.id_recepcion
            JOIN ordenes_compra o ON o.id_orden_compra = r.id_orden_compra
            JOIN proveedores pr ON pr.id_proveedor = o.id_proveedor
            JOIN productos p ON p.id_producto = dr.id_producto
            WHERE r.fecha_factura BETWEEN @desde AND @hasta
              AND (@idProd IS NULL OR dr.id_producto = @idProd)

            UNION ALL

            SELECT 
                o.fecha_emision AS fecha,
                o.numero_oc AS documento,
                p.sku,
                p.nombre_comercial,
                pr.razon_social AS proveedor,
                d.cantidad_solicitada AS cantidad,
                d.costo_pactado_unitario AS costo
            FROM detalle_orden_compra d
            JOIN ordenes_compra o ON o.id_orden_compra = d.id_orden_compra
            JOIN proveedores pr ON pr.id_proveedor = o.id_proveedor
            JOIN productos p ON p.id_producto = d.id_producto
            WHERE o.fecha_emision BETWEEN @desde AND @hasta
              AND (@idProd IS NULL OR d.id_producto = @idProd)
              AND o.id_orden_compra NOT IN (SELECT r2.id_orden_compra FROM recepciones_facturas_compra r2)

            ORDER BY fecha ASC";

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@desde", desde.Date);
        cmd.Parameters.AddWithValue("@hasta", hasta.Date.AddDays(1).AddSeconds(-1));
        cmd.Parameters.AddWithValue("@idProd", idProductoFiltro.HasValue && idProductoFiltro.Value > 0 ? (object)idProductoFiltro.Value : DBNull.Value);

        var temp = new List<ItemHistorialPrecio>();
        await using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            temp.Add(new ItemHistorialPrecio
            {
                Fecha = Convert.ToDateTime(rd.GetValue(0)),
                Documento = rd.GetString(1),
                Sku = rd.GetString(2),
                NombreProducto = rd.GetString(3),
                Proveedor = rd.GetString(4),
                Cantidad = Ent(rd, 5),
                CostoUnitario = Dec(rd, 6)
            });
        }

        // Calcular costo anterior y variación agrupado por producto
        var items = new List<ItemHistorialPrecio>();
        var grupos = temp.GroupBy(x => x.Sku);

        foreach (var grupo in grupos)
        {
            decimal? previo = null;
            foreach (var item in grupo.OrderBy(x => x.Fecha))
            {
                item.CostoAnterior = previo;
                previo = item.CostoUnitario;
                items.Add(item);
            }
        }

        // Orden descendente por fecha para la presentación en pantalla
        var listaFinal = items.OrderByDescending(x => x.Fecha).ToList();

        var resumen = new ResumenHistorialPrecios
        {
            TotalRegistros = listaFinal.Count,
            CostoPromedio = listaFinal.Count > 0 ? listaFinal.Average(x => x.CostoUnitario) : 0m,
            CostoMinimo = listaFinal.Count > 0 ? listaFinal.Min(x => x.CostoUnitario) : 0m,
            CostoMaximo = listaFinal.Count > 0 ? listaFinal.Max(x => x.CostoUnitario) : 0m,
            TotalAlzas = listaFinal.Count(x => x.VariacionMonto > 0)
        };

        return (resumen, listaFinal);
    }

    private static decimal Dec(DbDataReader r, int i) => r.IsDBNull(i) ? 0m : Convert.ToDecimal(r.GetValue(i));
    private static int Ent(DbDataReader r, int i) => r.IsDBNull(i) ? 0 : Convert.ToInt32(r.GetValue(i));
}
