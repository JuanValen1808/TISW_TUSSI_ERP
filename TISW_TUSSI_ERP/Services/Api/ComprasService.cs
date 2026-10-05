using System.Data.Common;
using MySqlConnector;
using TISW_TUSSI_ERP.Models.Compras;

namespace TISW_TUSSI_ERP.Services.Api;

// Lee de MySQL las órdenes de compra, su detalle y los indicadores del módulo Compras.
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

    private static decimal Dec(DbDataReader r, int i) => r.IsDBNull(i) ? 0m : Convert.ToDecimal(r.GetValue(i));
    private static int Ent(DbDataReader r, int i) => r.IsDBNull(i) ? 0 : Convert.ToInt32(r.GetValue(i));
}
