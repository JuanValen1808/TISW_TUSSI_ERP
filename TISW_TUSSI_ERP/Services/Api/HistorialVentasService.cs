
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySqlConnector;
using TISW_TUSSI_ERP.Models.Ventas;

namespace TISW_TUSSI_ERP.Services.Api;

public class HistorialVentasService
{
    private readonly DatabaseService _db;

    public HistorialVentasService(DatabaseService db)
    {
        _db = db;
    }

    public async Task<List<VentaHistorial>> ObtenerVentasAsync()
    {
        var resultado = new List<VentaHistorial>();

        const string sql = """
            SELECT
                v.id_venta,
                v.folio_documento,
                v.fecha_hora_emision,
                COALESCE(c.razon_social_nombre, 'Público general'),
                COALESCE(c.rut_cliente, ''),
                COALESCE(cv.nombre_convenio, ''),
                v.tipo_documento,
                v.estado_documento,
                v.forma_pago,
                v.monto_neto,
                v.monto_iva,
                v.monto_descuento,
                v.monto_total
            FROM ventas_encabezado v
            LEFT JOIN clientes c
                ON c.id_cliente = v.id_cliente
            LEFT JOIN convenios_salud cv
                ON cv.id_convenio = v.id_convenio
            ORDER BY
                v.fecha_hora_emision DESC,
                v.id_venta DESC
            LIMIT 500;
            """;

        await using var conexion = _db.CrearConexion();
        await conexion.OpenAsync();

        await using var comando = new MySqlCommand(sql, conexion);
        await using var lector = await comando.ExecuteReaderAsync();

        while (await lector.ReadAsync())
        {
            resultado.Add(new VentaHistorial
            {
                IdVenta = lector.GetInt32(0),
                Folio = Texto(lector, 1),
                Fecha = lector.GetDateTime(2),
                Cliente = Texto(lector, 3),
                RutCliente = Texto(lector, 4),
                Convenio = Texto(lector, 5),
                TipoDocumento = Texto(lector, 6),
                Estado = Texto(lector, 7),
                FormaPago = Texto(lector, 8),
                Neto = Decimal(lector, 9),
                Iva = Decimal(lector, 10),
                Descuento = Decimal(lector, 11),
                Total = Decimal(lector, 12)
            });
        }

        return resultado;
    }

    public async Task<List<DetalleVentaHistorial>>
        ObtenerDetalleAsync(int idVenta)
    {
        var resultado = new List<DetalleVentaHistorial>();

        const string sql = """
            SELECT
                COALESCE(p.nombre_comercial, 'Producto'),
                COALESCE(p.sku, ''),
                COALESCE(l.numero_lote, ''),
                d.cantidad,
                d.precio_unitario,
                d.subtotal
            FROM ventas_detalle d
            LEFT JOIN productos p
                ON p.id_producto = d.id_producto
            LEFT JOIN lotes l
                ON l.id_lote = d.id_lote
            WHERE d.id_venta = @idVenta
            ORDER BY d.id_detalle_venta;
            """;

        await using var conexion = _db.CrearConexion();
        await conexion.OpenAsync();

        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@idVenta", idVenta);

        await using var lector = await comando.ExecuteReaderAsync();

        while (await lector.ReadAsync())
        {
            resultado.Add(new DetalleVentaHistorial
            {
                Producto = Texto(lector, 0),
                Sku = Texto(lector, 1),
                Lote = Texto(lector, 2),
                Cantidad = Convert.ToInt32(lector.GetValue(3)),
                PrecioUnitario = Decimal(lector, 4),
                Subtotal = Decimal(lector, 5)
            });
        }

        return resultado;
    }

    private static string Texto(MySqlDataReader lector, int indice)
    {
        return lector.IsDBNull(indice)
            ? ""
            : Convert.ToString(lector.GetValue(indice)) ?? "";
    }

    private static decimal Decimal(MySqlDataReader lector, int indice)
    {
        return lector.IsDBNull(indice)
            ? 0m
            : Convert.ToDecimal(lector.GetValue(indice));
    }
}
