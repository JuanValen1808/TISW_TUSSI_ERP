
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using MySqlConnector;
using TISW_TUSSI_ERP.Models.Ventas;

namespace TISW_TUSSI_ERP.Services.Api;

public class ReporteProductosService
{
    private readonly DatabaseService _db;

    public ReporteProductosService(DatabaseService db)
    {
        _db = db;
    }

    public async Task<List<ReporteProductoVenta>>
        ObtenerProductosAsync(
            DateTime? desde = null,
            DateTime? hasta = null)
    {
        const string sql = @"
            SELECT
                p.sku,
                p.nombre_comercial,
                c.nombre_categoria,
                SUM(vd.cantidad) AS unidades,
                COUNT(DISTINCT ve.id_venta) AS documentos,
                SUM(vd.subtotal) AS total
            FROM ventas_detalle vd
            INNER JOIN ventas_encabezado ve
                ON ve.id_venta = vd.id_venta
            INNER JOIN productos p
                ON p.id_producto = vd.id_producto
            INNER JOIN categorias_producto c
                ON c.id_categoria = p.id_categoria
            WHERE ve.estado_documento <> 'ANULADO'
                AND (
                    @desde IS NULL
                    OR ve.fecha_hora_emision >= @desde
                )
                AND (
                    @hasta IS NULL
                    OR ve.fecha_hora_emision < @hasta
                )
            GROUP BY
                p.id_producto,
                p.sku,
                p.nombre_comercial,
                c.id_categoria,
                c.nombre_categoria
            ORDER BY total DESC;";

        var resultado = new List<ReporteProductoVenta>();

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        await using var cmd = new MySqlCommand(sql, cn);

        cmd.Parameters.AddWithValue(
            "@desde",
            desde.HasValue
                ? desde.Value.Date
                : DBNull.Value);

        cmd.Parameters.AddWithValue(
            "@hasta",
            hasta.HasValue
                ? hasta.Value.Date.AddDays(1)
                : DBNull.Value);

        await using var rd = await cmd.ExecuteReaderAsync();

        while (await rd.ReadAsync())
        {
            resultado.Add(new ReporteProductoVenta
            {
                Sku = rd.IsDBNull(0) ? "" : rd.GetString(0),
                NombreProducto = rd.GetString(1),
                Categoria = rd.GetString(2),
                UnidadesVendidas =
                    Convert.ToInt32(rd.GetValue(3)),
                NumeroVentas =
                    Convert.ToInt32(rd.GetValue(4)),
                TotalVendido =
                    LeerDecimal(rd, 5)
            });
        }

        return resultado;
    }

    public async Task<List<ReporteCategoriaVenta>>
        ObtenerCategoriasAsync(
            DateTime? desde = null,
            DateTime? hasta = null)
    {
        const string sql = @"
            SELECT
                c.nombre_categoria,
                SUM(vd.cantidad) AS unidades,
                COUNT(DISTINCT p.id_producto) AS productos,
                SUM(vd.subtotal) AS total
            FROM ventas_detalle vd
            INNER JOIN ventas_encabezado ve
                ON ve.id_venta = vd.id_venta
            INNER JOIN productos p
                ON p.id_producto = vd.id_producto
            INNER JOIN categorias_producto c
                ON c.id_categoria = p.id_categoria
            WHERE ve.estado_documento <> 'ANULADO'
                AND (
                    @desde IS NULL
                    OR ve.fecha_hora_emision >= @desde
                )
                AND (
                    @hasta IS NULL
                    OR ve.fecha_hora_emision < @hasta
                )
            GROUP BY
                c.id_categoria,
                c.nombre_categoria
            ORDER BY total DESC;";

        var resultado = new List<ReporteCategoriaVenta>();

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        await using var cmd = new MySqlCommand(sql, cn);

        cmd.Parameters.AddWithValue(
            "@desde",
            desde.HasValue
                ? desde.Value.Date
                : DBNull.Value);

        cmd.Parameters.AddWithValue(
            "@hasta",
            hasta.HasValue
                ? hasta.Value.Date.AddDays(1)
                : DBNull.Value);

        await using var rd = await cmd.ExecuteReaderAsync();

        while (await rd.ReadAsync())
        {
            resultado.Add(new ReporteCategoriaVenta
            {
                Categoria = rd.GetString(0),
                UnidadesVendidas =
                    Convert.ToInt32(rd.GetValue(1)),
                ProductosDistintos =
                    Convert.ToInt32(rd.GetValue(2)),
                TotalVendido =
                    LeerDecimal(rd, 3)
            });
        }

        return resultado;
    }

    private static decimal LeerDecimal(
        DbDataReader rd,
        int indice)
    {
        return rd.IsDBNull(indice)
            ? 0m
            : Convert.ToDecimal(rd.GetValue(indice));
    }
}
