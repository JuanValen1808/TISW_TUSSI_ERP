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

// Modelo ligero para el selector de productos en la nueva orden
public class ProductoSelector
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal Costo { get; set; }
    public string DescripcionCompleta => $"{Sku} - {Nombre} (${Costo:N0})";
}

// Modelo para el carrito de compras
public class CarritoItem
{
    public int IdProducto { get; set; }
    public string NombreProducto { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal => Cantidad * PrecioUnitario;
}

public class ComprasService
{
    private readonly DatabaseService _db;
    public ComprasService(DatabaseService db) => _db = db;

    public async Task<ResumenCompras> ObtenerResumenAsync()
    {
        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        var r = new ResumenCompras();

        const string sql1 = @"
            SELECT COUNT(CASE WHEN estado IN ('BORRADOR','EMITIDA') THEN 1 END),
                   COUNT(CASE WHEN estado = 'PARCIAL' THEN 1 END),
                   COALESCE(SUM(CASE WHEN estado <> 'ANULADA' THEN monto_total END), 0)
            FROM ordenes_compra";
        await using (var cmd = new MySqlCommand(sql1, cn))
        await using (var rd = await cmd.ExecuteReaderAsync())
        {
            if (await rd.ReadAsync())
            {
                r.OrdenesPendientes = Ent(rd, 0);
                r.RecibidasParciales = Ent(rd, 1);
                r.TotalComprometido = Dec(rd, 2);
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
                   p.razon_social, p.rut_proveedor, p.nombre_contacto, u.nombre
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
                Creador = rd.GetString(10)
            });
        }
        return lista;
    }

    public async Task<List<DetalleOrdenCompra>> ObtenerDetalleAsync(int idOrden)
    {
        const string sql = @"
            SELECT p.sku, p.nombre_comercial, p.formato_presentacion,
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
            var presentacion = rd.IsDBNull(2) ? "" : " · " + rd.GetString(2);
            lista.Add(new DetalleOrdenCompra
            {
                Sku = rd.GetString(0),
                Descripcion = rd.GetString(1) + presentacion,
                Pedido = Ent(rd, 3),
                Recibido = Ent(rd, 4),
                Costo = Dec(rd, 5)
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
            throw new InvalidOperationException("No se puede eliminar: el proveedor tiene órdenes de compra registradas. Puedes editarlo en su lugar.");
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
            ProveedoresConCompras = filas.Count,
            TotalOrdenes = filas.Sum(f => f.NumeroOrdenes),
            MontoTotalPeriodo = filas.Sum(f => f.MontoTotal),
            MontoPorRecibir = montoPorRecibirTotal
        };

        return (resumen, filas);
    }

    // =========================================================
    // NUEVA ORDEN DE COMPRA (PANTALLA 1)
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

    public async Task CrearOrdenCompraAsync(int idProveedor, int idUsuario, decimal subtotal, decimal iva, decimal total, List<CarritoItem> carrito)
    {
        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        await using var transaccion = await cn.BeginTransactionAsync();

        try
        {
            const string sqlOrden = @"
                INSERT INTO ordenes_compra (numero_oc, id_proveedor, fecha_emision, estado, monto_subtotal, monto_iva, monto_total, id_usuario_creador)
                VALUES (@numOC, @idProv, CURRENT_DATE, 'EMITIDA', @sub, @iva, @tot, @idUser);
                SELECT LAST_INSERT_ID();";

            await using var cmdOrden = new MySqlCommand(sqlOrden, cn, transaccion);
            cmdOrden.Parameters.AddWithValue("@numOC", $"OC-{DateTime.Now.Year}-{new Random().Next(1000, 9999)}");
            cmdOrden.Parameters.AddWithValue("@idProv", idProveedor);
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
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    private static decimal Dec(DbDataReader r, int i) => r.IsDBNull(i) ? 0m : Convert.ToDecimal(r.GetValue(i));
    private static int Ent(DbDataReader r, int i) => r.IsDBNull(i) ? 0 : Convert.ToInt32(r.GetValue(i));
}