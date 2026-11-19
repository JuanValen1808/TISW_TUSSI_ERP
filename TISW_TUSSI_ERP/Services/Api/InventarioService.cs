using MySqlConnector;
using System.Data.Common;
using System.Text.RegularExpressions;
using TISW_TUSSI_ERP.Models.Inventario;

namespace TISW_TUSSI_ERP.Services.Api;

public class InventarioService
{
    private readonly DatabaseService _db;
    public InventarioService(DatabaseService db) => _db = db;

    public async Task<ResumenInventario> ObtenerResumenAsync()
    {
        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        var r = new ResumenInventario();

        const string sql1 = @"
            SELECT 
                COUNT(*),
                COALESCE(SUM(p.costo_promedio_ponderado * COALESCE(l.stock_total, 0)), 0)
            FROM productos p
            LEFT JOIN (
                SELECT id_producto, SUM(stock_actual) AS stock_total 
                FROM lotes 
                GROUP BY id_producto
            ) l ON l.id_producto = p.id_producto
            WHERE p.activo = 1";

        await using (var cmd = new MySqlCommand(sql1, cn))
        await using (var rd = await cmd.ExecuteReaderAsync())
        {
            if (await rd.ReadAsync())
            {
                r.TotalProductos = Ent(rd, 0);
                r.ValorizacionTotal = Dec(rd, 1);
            }
        }

        const string sql2 = @"
            SELECT COUNT(*) 
            FROM (
                SELECT p.id_producto, p.stock_minimo, COALESCE(SUM(l.stock_actual), 0) AS stock_actual
                FROM productos p
                LEFT JOIN lotes l ON l.id_producto = p.id_producto
                WHERE p.activo = 1
                GROUP BY p.id_producto, p.stock_minimo
                HAVING stock_actual <= p.stock_minimo
            ) t";

        await using (var cmd = new MySqlCommand(sql2, cn))
            r.ProductosStockBajo = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        const string sql3 = @"
            SELECT COUNT(*) 
            FROM lotes 
            WHERE stock_actual > 0 
              AND fecha_vencimiento <= DATE_ADD(CURDATE(), INTERVAL 30 DAY)";

        await using (var cmd = new MySqlCommand(sql3, cn))
            r.ProductosPorVencer = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        return r;
    }

    public async Task<List<ProductoModel>> ObtenerProductosAsync()
    {
        const string sql = @"
            SELECT 
                p.id_producto, p.sku, p.codigo_barras, p.nombre_comercial, 
                p.principio_activo, p.registro_sanitario, p.formato_presentacion,
                p.id_categoria, c.nombre_categoria, p.precio_venta_base, 
                p.costo_promedio_ponderado, p.stock_minimo, p.punto_reorden, p.activo,
                COALESCE(SUM(l.stock_actual), 0) AS stock_total
            FROM productos p
            INNER JOIN categorias_producto c ON c.id_categoria = p.id_categoria
            LEFT JOIN lotes l ON l.id_producto = p.id_producto
            WHERE p.activo = 1
            GROUP BY p.id_producto
            ORDER BY p.nombre_comercial ASC";

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, cn);
        await using var rd = await cmd.ExecuteReaderAsync();

        var lista = new List<ProductoModel>();
        while (await rd.ReadAsync())
        {
            lista.Add(new ProductoModel
            {
                IdProducto = rd.GetInt32(0),
                Sku = rd.GetString(1),
                CodigoBarras = rd.IsDBNull(2) ? "" : rd.GetString(2),
                NombreComercial = rd.GetString(3),
                PrincipioActivo = rd.IsDBNull(4) ? "" : rd.GetString(4),
                RegistroSanitario = rd.IsDBNull(5) ? "" : rd.GetString(5),
                FormatoPresentacion = rd.IsDBNull(6) ? "" : rd.GetString(6),
                IdCategoria = rd.GetInt32(7),
                CategoriaNombre = rd.GetString(8),
                PrecioVenta = Dec(rd, 9),
                CostoPromedio = Dec(rd, 10),
                StockMinimo = Ent(rd, 11),
                PuntoReorden = Ent(rd, 12),
                Activo = rd.GetBoolean(13),
                StockActual = Ent(rd, 14)
            });
        }
        return lista;
    }

    public async Task<List<LoteModel>> ObtenerLotesPorProductoAsync(int idProducto)
    {
        const string sql = @"
        SELECT l.id_lote, l.id_producto, p.sku, p.nombre_comercial, 
               l.numero_lote, l.fecha_vencimiento, l.stock_actual, p.costo_promedio_ponderado
        FROM lotes l
        JOIN productos p ON p.id_producto = l.id_producto
        WHERE l.id_producto = @id AND l.stock_actual > 0
        ORDER BY l.fecha_vencimiento ASC";

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@id", idProducto);
        await using var rd = await cmd.ExecuteReaderAsync();

        var lista = new List<LoteModel>();
        while (await rd.ReadAsync())
        {
            lista.Add(new LoteModel
            {
                IdLote = rd.GetInt32(0),
                IdProducto = rd.GetInt32(1),
                Sku = rd.GetString(2),
                NombreProducto = rd.GetString(3),
                NumeroLote = rd.GetString(4),
                FechaVencimiento = Convert.ToDateTime(rd.GetValue(5)),
                StockActual = Ent(rd, 6),
                CostoUnitario = Dec(rd, 7)
            });
        }
        return lista;
    }

    public async Task<List<KardexMovimientoModel>> ObtenerKardexAsync(int? idProducto = null)
    {
        string sql = @"
            SELECT k.id_kardex, k.id_producto, p.nombre_comercial, p.sku, 
                   COALESCE(l.numero_lote, '—') AS numero_lote, k.tipo_movimiento, 
                   k.origen_documento, k.cantidad, k.costo_unitario, 
                   k.saldo_stock_producto, k.fecha_movimiento
            FROM kardex_movimientos k
            JOIN productos p ON p.id_producto = k.id_producto
            LEFT JOIN lotes l ON l.id_lote = k.id_lote";

        if (idProducto.HasValue && idProducto.Value > 0)
            sql += " WHERE k.id_producto = @idProducto";

        sql += " ORDER BY k.fecha_movimiento DESC, k.id_kardex DESC LIMIT 150";

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, cn);
        if (idProducto.HasValue && idProducto.Value > 0)
            cmd.Parameters.AddWithValue("@idProducto", idProducto.Value);

        await using var rd = await cmd.ExecuteReaderAsync();
        var lista = new List<KardexMovimientoModel>();

        while (await rd.ReadAsync())
        {
            lista.Add(new KardexMovimientoModel
            {
                IdKardex = rd.GetInt64(0),
                IdProducto = rd.GetInt32(1),
                NombreProducto = rd.GetString(2),
                Sku = rd.GetString(3),
                NumeroLote = rd.GetString(4),
                TipoMovimiento = rd.GetString(5),
                OrigenDocumento = rd.GetString(6),
                Cantidad = rd.IsDBNull(7) ? 0 : rd.GetInt32(7),
                CostoUnitario = rd.IsDBNull(8) ? 0m : rd.GetDecimal(8),
                SaldoStockProducto = rd.IsDBNull(9) ? 0 : rd.GetInt32(9),
                FechaMovimiento = rd.GetDateTime(10)
            });
        }
        return lista;
    }

    public async Task<List<MermaModel>> ObtenerMermasAsync()
    {
        const string sql = @"
        SELECT m.id_merma, m.id_lote, l.numero_lote, p.sku, p.nombre_comercial, 
               m.cantidad, m.motivo, m.fecha_merma, m.costo_total_perdida, COALESCE(u.nombre, 'Sistema') AS usuario
        FROM registro_mermas m
        JOIN lotes l ON l.id_lote = m.id_lote
        JOIN productos p ON p.id_producto = m.id_producto
        LEFT JOIN usuarios u ON u.id_usuario = m.id_usuario
        ORDER BY m.fecha_merma DESC LIMIT 100";

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, cn);
        await using var rd = await cmd.ExecuteReaderAsync();

        var lista = new List<MermaModel>();
        while (await rd.ReadAsync())
        {
            lista.Add(new MermaModel
            {
                IdMerma = rd.GetInt32(0),
                IdLote = rd.GetInt32(1),
                NumeroLote = rd.GetString(2),
                Sku = rd.GetString(3),
                NombreProducto = rd.GetString(4),
                CantidadBaja = rd.GetInt32(5),
                Motivo = rd.GetString(6),
                FechaRegistro = rd.GetDateTime(7),
                UsuarioResponsable = rd.GetString(9)
            });
        }
        return lista;
    }

    public async Task<(bool Exito, string Mensaje)> GuardarProductoAsync(ProductoModel producto)
    {
        producto.Sku = producto.Sku?.Trim().ToUpperInvariant() ?? string.Empty;
        producto.CodigoBarras = string.IsNullOrWhiteSpace(producto.CodigoBarras) ? string.Empty : producto.CodigoBarras.Trim();
        producto.NombreComercial = producto.NombreComercial?.Trim() ?? string.Empty;
        producto.PrincipioActivo = producto.PrincipioActivo?.Trim() ?? string.Empty;
        producto.RegistroSanitario = producto.RegistroSanitario?.Trim() ?? string.Empty;
        producto.FormatoPresentacion = producto.FormatoPresentacion?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(producto.Sku) || string.IsNullOrWhiteSpace(producto.NombreComercial) || string.IsNullOrWhiteSpace(producto.RegistroSanitario))
        {
            return (false, "Los campos SKU, Nombre Comercial y Registro Sanitario son obligatorios.");
        }

        if (!Regex.IsMatch(producto.Sku, @"^[A-Z0-9\-]{3,20}$"))
        {
            return (false, "El formato del SKU no es válido. Debe usar letras mayúsculas, números y guiones (Ej: MED-0001).");
        }

        if (!string.IsNullOrEmpty(producto.CodigoBarras))
        {
            if (!Regex.IsMatch(producto.CodigoBarras, @"^\d{8,14}$"))
            {
                return (false, "El código de barras debe ser exclusivamente numérico y tener entre 8 y 14 dígitos.");
            }
        }

        if (!Regex.IsMatch(producto.RegistroSanitario, @"^[A-Z0-9\-/]{4,25}$"))
        {
            return (false, "El formato del Registro Sanitario no es válido (Ej: ISP-12345 o J-123/456).");
        }

        if (producto.CostoPromedio <= 0 || producto.PrecioVenta <= 0)
        {
            return (false, "El costo promedio y el precio de venta deben ser estrictamente mayores a cero.");
        }

        if (producto.PrecioVenta < producto.CostoPromedio)
        {
            return (false, "Validación comercial: El precio de venta no puede ser menor al costo promedio del producto.");
        }

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        const string sqlValida = @"
        SELECT COUNT(*) FROM productos 
        WHERE (sku = @sku OR (codigo_barras IS NOT NULL AND codigo_barras != '' AND codigo_barras = @cb)) 
          AND (@id = 0 OR id_producto <> @id)";

        await using (var cmdVal = new MySqlCommand(sqlValida, cn))
        {
            cmdVal.Parameters.AddWithValue("@sku", producto.Sku);
            cmdVal.Parameters.AddWithValue("@cb", string.IsNullOrEmpty(producto.CodigoBarras) ? (object)DBNull.Value : producto.CodigoBarras);
            cmdVal.Parameters.AddWithValue("@id", producto.IdProducto);

            long count = Convert.ToInt64(await cmdVal.ExecuteScalarAsync());
            if (count > 0)
            {
                return (false, "El SKU o el código de barras ya se encuentran registrados en otro producto existente.");
            }
        }

        try
        {
            if (producto.IdProducto == 0)
            {
                const string sqlInsert = @"
                INSERT INTO productos (sku, codigo_barras, nombre_comercial, principio_activo, registro_sanitario, 
                                       formato_presentacion, id_categoria, precio_venta_base, costo_promedio_ponderado, 
                                       stock_minimo, punto_reorden, activo)
                VALUES (@sku, @cb, @nombre, @pa, @rs, @formato, @cat, @pv, @costo, @min, @reorden, 1)";

                await using var cmdIns = new MySqlCommand(sqlInsert, cn);
                AsignarParametrosProducto(cmdIns, producto);
                await cmdIns.ExecuteNonQueryAsync();
                return (true, "Producto registrado correctamente en el catálogo.");
            }
            else
            {
                const string sqlUpdate = @"
                UPDATE productos 
                SET sku = @sku, codigo_barras = @cb, nombre_comercial = @nombre, principio_activo = @pa, 
                    registro_sanitario = @rs, formato_presentacion = @formato, id_categoria = @cat, 
                    precio_venta_base = @pv, costo_promedio_ponderado = @costo, stock_minimo = @min, punto_reorden = @reorden
                WHERE id_producto = @id";

                await using var cmdUpd = new MySqlCommand(sqlUpdate, cn);
                AsignarParametrosProducto(cmdUpd, producto);
                cmdUpd.Parameters.AddWithValue("@id", producto.IdProducto);
                await cmdUpd.ExecuteNonQueryAsync();
                return (true, "Producto actualizado correctamente.");
            }
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            return (false, "El SKU o el código de barras ya se encuentran registrados en el sistema. Verifique los datos.");
        }
    }

    public async Task<(bool Exito, string Mensaje)> InactivarProductoAsync(int idProducto)
    {
        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        const string sqlCheck = "SELECT COALESCE(SUM(stock_actual), 0) FROM lotes WHERE id_producto = @id";
        await using var cmdCheck = new MySqlCommand(sqlCheck, cn);
        cmdCheck.Parameters.AddWithValue("@id", idProducto);
        int stockLotes = Convert.ToInt32(await cmdCheck.ExecuteScalarAsync());

        if (stockLotes > 0)
        {
            return (false, "No se puede inactivar el producto porque cuenta con stock en lotes activos.");
        }

        const string sqlInactivar = "UPDATE productos SET activo = 0 WHERE id_producto = @id";
        await using var cmdInc = new MySqlCommand(sqlInactivar, cn);
        cmdInc.Parameters.AddWithValue("@id", idProducto);
        await cmdInc.ExecuteNonQueryAsync();

        return (true, "El producto ha sido inactivado exitosamente.");
    }

    #region CU-INV-07 y CU-INV-08: Gestión de Ubicaciones (Bodega y Sala de Ventas)

    public async Task<List<UbicacionModel>> ObtenerUbicacionesAsync(string tipoZona = "BODEGA", bool soloActivas = true)
    {
        string sql = @"
            SELECT id_ubicacion, tipo_zona, codigo_ubicacion, sector_pasillo, estante_mueble, repisa_nivel, descripcion, activo
            FROM ubicaciones
            WHERE tipo_zona = @zona";

        if (soloActivas) sql += " AND activo = 1";
        sql += " ORDER BY sector_pasillo ASC, estante_mueble ASC, repisa_nivel ASC";

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@zona", tipoZona);
        await using var rd = await cmd.ExecuteReaderAsync();

        var lista = new List<UbicacionModel>();
        while (await rd.ReadAsync())
        {
            lista.Add(new UbicacionModel
            {
                IdUbicacion = rd.GetInt32(0),
                TipoZona = rd.GetString(1),
                CodigoUbicacion = rd.GetString(2),
                SectorPasillo = rd.GetString(3),
                EstanteMueble = rd.GetString(4),
                RepisaNivel = rd.IsDBNull(5) ? string.Empty : rd.GetString(5),
                Descripcion = rd.IsDBNull(6) ? string.Empty : rd.GetString(6),
                Activo = rd.GetBoolean(7)
            });
        }
        return lista;
    }

    public async Task<List<LoteBodegaItemModel>> ObtenerLotesParaUbicacionAsync()
    {
        const string sql = @"
            SELECT 
                l.id_lote, p.id_producto, p.sku, COALESCE(p.codigo_barras, ''), p.nombre_comercial,
                l.numero_lote, l.fecha_vencimiento, l.stock_actual,
                COALESCE(SUM(CASE WHEN u.tipo_zona = 'BODEGA' THEN lu.cantidad ELSE 0 END), 0) AS ubicado_bodega,
                COALESCE(SUM(CASE WHEN u.tipo_zona = 'SALA_VENTAS' THEN lu.cantidad ELSE 0 END), 0) AS ubicado_sala
            FROM lotes l
            INNER JOIN productos p ON p.id_producto = l.id_producto
            LEFT JOIN lote_ubicaciones lu ON lu.id_lote = l.id_lote
            LEFT JOIN ubicaciones u ON u.id_ubicacion = lu.id_ubicacion
            WHERE p.activo = 1 AND l.stock_actual > 0
            GROUP BY l.id_lote
            ORDER BY p.nombre_comercial ASC, l.fecha_vencimiento ASC";

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, cn);
        await using var rd = await cmd.ExecuteReaderAsync();

        var lista = new List<LoteBodegaItemModel>();
        while (await rd.ReadAsync())
        {
            lista.Add(new LoteBodegaItemModel
            {
                IdLote = rd.GetInt32(0),
                IdProducto = rd.GetInt32(1),
                Sku = rd.GetString(2),
                CodigoBarras = rd.GetString(3),
                NombreProducto = rd.GetString(4),
                NumeroLote = rd.GetString(5),
                FechaVencimiento = Convert.ToDateTime(rd.GetValue(6)),
                StockTotalLote = rd.GetInt32(7),
                StockUbicadoBodega = rd.GetInt32(8),
                StockUbicadoSala = rd.GetInt32(9)
            });
        }
        return lista;
    }

    public async Task<List<LoteUbicacionDetalleModel>> ObtenerUbicacionesDeLoteAsync(int idLote, string tipoZona = "BODEGA")
    {
        const string sql = @"
            SELECT lu.id_lote_ubicacion, lu.id_lote, lu.id_ubicacion, u.codigo_ubicacion,
                   CONCAT(u.sector_pasillo, ' › ', u.estante_mueble, IF(u.repisa_nivel IS NULL OR u.repisa_nivel = '', '', CONCAT(' › ', u.repisa_nivel))) AS ruta,
                   lu.cantidad, lu.fecha_asignacion, COALESCE(usr.nombre, 'Sistema') AS usuario
            FROM lote_ubicaciones lu
            INNER JOIN ubicaciones u ON u.id_ubicacion = lu.id_ubicacion
            LEFT JOIN usuarios usr ON usr.id_usuario = lu.id_usuario
            WHERE lu.id_lote = @idLote AND u.tipo_zona = @zona AND lu.cantidad > 0
            ORDER BY u.codigo_ubicacion ASC";

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@idLote", idLote);
        cmd.Parameters.AddWithValue("@zona", tipoZona);
        await using var rd = await cmd.ExecuteReaderAsync();

        var lista = new List<LoteUbicacionDetalleModel>();
        while (await rd.ReadAsync())
        {
            lista.Add(new LoteUbicacionDetalleModel
            {
                IdLoteUbicacion = rd.GetInt32(0),
                IdLote = rd.GetInt32(1),
                IdUbicacion = rd.GetInt32(2),
                CodigoUbicacion = rd.GetString(3),
                RutaCompleta = rd.GetString(4),
                Cantidad = rd.GetInt32(5),
                FechaAsignacion = rd.GetDateTime(6),
                UsuarioResponsable = rd.GetString(7)
            });
        }
        return lista;
    }

    public async Task<(bool Exito, string Mensaje)> TraspasarBodegaASalaAsync(
        int idLote,
        int idUbicacionBodegaOrigen,
        int idUbicacionSalaDestino,
        int cantidad,
        int? idUsuario = null)
    {
        if (cantidad <= 0)
            return (false, "La cantidad a exhibir debe ser mayor a cero.");

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        await using var tx = await cn.BeginTransactionAsync();

        try
        {
            const string sqlLote = "SELECT fecha_vencimiento, stock_actual FROM lotes WHERE id_lote = @idL";
            await using (var cmdL = new MySqlCommand(sqlLote, cn, tx))
            {
                cmdL.Parameters.AddWithValue("@idL", idLote);
                await using var rdL = await cmdL.ExecuteReaderAsync();
                if (!await rdL.ReadAsync())
                    return (false, "El lote seleccionado no existe.");

                DateTime venc = Convert.ToDateTime(rdL.GetValue(0));
                if (venc.Date < DateTime.Today)
                    return (false, $"No se puede exhibir: El lote venció el {venc:dd/MM/yyyy}.");
            }

            const string sqlOrig = @"
                SELECT lu.cantidad
                FROM lote_ubicaciones lu
                INNER JOIN ubicaciones u ON u.id_ubicacion = lu.id_ubicacion
                WHERE lu.id_lote = @idL AND lu.id_ubicacion = @idOrig AND u.tipo_zona = 'BODEGA'";

            int disponibleBodega = 0;
            await using (var cmdO = new MySqlCommand(sqlOrig, cn, tx))
            {
                cmdO.Parameters.AddWithValue("@idL", idLote);
                cmdO.Parameters.AddWithValue("@idOrig", idUbicacionBodegaOrigen);
                var obj = await cmdO.ExecuteScalarAsync();
                if (obj == null || obj == DBNull.Value)
                    return (false, "El lote no posee stock en la ubicación de bodega seleccionada.");
                disponibleBodega = Convert.ToInt32(obj);
            }

            if (cantidad > disponibleBodega)
                return (false, $"Stock insuficiente en esa ubicación de bodega (Disponible: {disponibleBodega} un.).");

            if (cantidad == disponibleBodega)
            {
                const string sqlDel = "DELETE FROM lote_ubicaciones WHERE id_lote = @idL AND id_ubicacion = @idOrig";
                await using var cmdDel = new MySqlCommand(sqlDel, cn, tx);
                cmdDel.Parameters.AddWithValue("@idL", idLote);
                cmdDel.Parameters.AddWithValue("@idOrig", idUbicacionBodegaOrigen);
                await cmdDel.ExecuteNonQueryAsync();
            }
            else
            {
                const string sqlDec = @"
                    UPDATE lote_ubicaciones 
                    SET cantidad = cantidad - @cant, fecha_asignacion = NOW(), id_usuario = @usr 
                    WHERE id_lote = @idL AND id_ubicacion = @idOrig";
                await using var cmdDec = new MySqlCommand(sqlDec, cn, tx);
                cmdDec.Parameters.AddWithValue("@cant", cantidad);
                cmdDec.Parameters.AddWithValue("@usr", idUsuario.HasValue ? idUsuario.Value : DBNull.Value);
                cmdDec.Parameters.AddWithValue("@idL", idLote);
                cmdDec.Parameters.AddWithValue("@idOrig", idUbicacionBodegaOrigen);
                await cmdDec.ExecuteNonQueryAsync();
            }

            const string sqlUpsertSala = @"
                INSERT INTO lote_ubicaciones (id_lote, id_ubicacion, cantidad, fecha_asignacion, id_usuario)
                VALUES (@idL, @idDest, @cant, NOW(), @usr)
                ON DUPLICATE KEY UPDATE 
                    cantidad = cantidad + @cant,
                    fecha_asignacion = NOW(),
                    id_usuario = @usr";

            await using (var cmdIns = new MySqlCommand(sqlUpsertSala, cn, tx))
            {
                cmdIns.Parameters.AddWithValue("@idL", idLote);
                cmdIns.Parameters.AddWithValue("@idDest", idUbicacionSalaDestino);
                cmdIns.Parameters.AddWithValue("@cant", cantidad);
                cmdIns.Parameters.AddWithValue("@usr", idUsuario.HasValue ? idUsuario.Value : DBNull.Value);
                await cmdIns.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
            return (true, $"Se trasladaron {cantidad} unidades a exhibición en Sala de Ventas.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return (false, "Error al traspasar a sala de ventas: " + ex.Message);
        }
    }

    public async Task<(bool Exito, string Mensaje)> RetirarSalaABodegaAsync(
        int idLote,
        int idUbicacionSalaOrigen,
        int idUbicacionBodegaDestino,
        int cantidad,
        int? idUsuario = null)
    {
        if (cantidad <= 0)
            return (false, "La cantidad a retirar debe ser mayor a cero.");

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        await using var tx = await cn.BeginTransactionAsync();

        try
        {
            const string sqlOrig = @"
                SELECT lu.cantidad
                FROM lote_ubicaciones lu
                INNER JOIN ubicaciones u ON u.id_ubicacion = lu.id_ubicacion
                WHERE lu.id_lote = @idL AND lu.id_ubicacion = @idOrig AND u.tipo_zona = 'SALA_VENTAS'";

            int disponibleSala = 0;
            await using (var cmdO = new MySqlCommand(sqlOrig, cn, tx))
            {
                cmdO.Parameters.AddWithValue("@idL", idLote);
                cmdO.Parameters.AddWithValue("@idOrig", idUbicacionSalaOrigen);
                var obj = await cmdO.ExecuteScalarAsync();
                if (obj == null || obj == DBNull.Value)
                    return (false, "El lote no tiene existencias en esa ubicación de sala.");
                disponibleSala = Convert.ToInt32(obj);
            }

            if (cantidad > disponibleSala)
                return (false, $"Cantidad mayor a lo exhibido en sala ({disponibleSala} un.).");

            if (cantidad == disponibleSala)
            {
                const string sqlDel = "DELETE FROM lote_ubicaciones WHERE id_lote = @idL AND id_ubicacion = @idOrig";
                await using var cmdDel = new MySqlCommand(sqlDel, cn, tx);
                cmdDel.Parameters.AddWithValue("@idL", idLote);
                cmdDel.Parameters.AddWithValue("@idOrig", idUbicacionSalaOrigen);
                await cmdDel.ExecuteNonQueryAsync();
            }
            else
            {
                const string sqlDec = @"
                    UPDATE lote_ubicaciones 
                    SET cantidad = cantidad - @cant, fecha_asignacion = NOW(), id_usuario = @usr 
                    WHERE id_lote = @idL AND id_ubicacion = @idOrig";
                await using var cmdDec = new MySqlCommand(sqlDec, cn, tx);
                cmdDec.Parameters.AddWithValue("@cant", cantidad);
                cmdDec.Parameters.AddWithValue("@usr", idUsuario.HasValue ? idUsuario.Value : DBNull.Value);
                cmdDec.Parameters.AddWithValue("@idL", idLote);
                cmdDec.Parameters.AddWithValue("@idOrig", idUbicacionSalaOrigen);
                await cmdDec.ExecuteNonQueryAsync();
            }

            const string sqlUpsertBodega = @"
                INSERT INTO lote_ubicaciones (id_lote, id_ubicacion, cantidad, fecha_asignacion, id_usuario)
                VALUES (@idL, @idDest, @cant, NOW(), @usr)
                ON DUPLICATE KEY UPDATE 
                    cantidad = cantidad + @cant,
                    fecha_asignacion = NOW(),
                    id_usuario = @usr";

            await using (var cmdIns = new MySqlCommand(sqlUpsertBodega, cn, tx))
            {
                cmdIns.Parameters.AddWithValue("@idL", idLote);
                cmdIns.Parameters.AddWithValue("@idDest", idUbicacionBodegaDestino);
                cmdIns.Parameters.AddWithValue("@cant", cantidad);
                cmdIns.Parameters.AddWithValue("@usr", idUsuario.HasValue ? idUsuario.Value : DBNull.Value);
                await cmdIns.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
            return (true, $"Se retiraron {cantidad} unidades desde Sala de Ventas hacia Bodega.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return (false, "Error al retirar de sala de ventas: " + ex.Message);
        }
    }

    public async Task<(bool Exito, string Mensaje)> CrearUbicacionAsync(UbicacionModel ubicacion, string rolUsuario)
    {
        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        try
        {
            const string sql = @"
                INSERT INTO ubicaciones (tipo_zona, codigo_ubicacion, sector_pasillo, estante_mueble, repisa_nivel, descripcion, activo)
                VALUES (@zona, @cod, @sec, @est, @rep, @desc, 1)";

            await using var cmd = new MySqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@zona", ubicacion.TipoZona);
            cmd.Parameters.AddWithValue("@cod", ubicacion.CodigoUbicacion.Trim().ToUpperInvariant());
            cmd.Parameters.AddWithValue("@sec", ubicacion.SectorPasillo.Trim());
            cmd.Parameters.AddWithValue("@est", ubicacion.EstanteMueble.Trim());
            cmd.Parameters.AddWithValue("@rep", string.IsNullOrWhiteSpace(ubicacion.RepisaNivel) ? DBNull.Value : ubicacion.RepisaNivel.Trim());
            cmd.Parameters.AddWithValue("@desc", string.IsNullOrWhiteSpace(ubicacion.Descripcion) ? DBNull.Value : ubicacion.Descripcion.Trim());
            await cmd.ExecuteNonQueryAsync();

            return (true, "Ubicación creada correctamente.");
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            return (false, "Ya existe una ubicación registrada con ese código.");
        }
    }

    public async Task<(bool Exito, string Mensaje)> ActualizarUbicacionAsync(UbicacionModel ubicacion, string rolUsuario)
    {
        bool tienePermiso = rolUsuario.Contains("Encargado", StringComparison.OrdinalIgnoreCase) ||
                            rolUsuario.Contains("Administrador", StringComparison.OrdinalIgnoreCase);

        if (!tienePermiso)
            return (false, "Acceso denegado (CU-04): Solo el Encargado de Inventario puede editar ubicaciones.");

        if (string.IsNullOrWhiteSpace(ubicacion.CodigoUbicacion) || string.IsNullOrWhiteSpace(ubicacion.SectorPasillo) || string.IsNullOrWhiteSpace(ubicacion.EstanteMueble))
            return (false, "Código, Pasillo/Sector y Estante son obligatorios.");

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        try
        {
            const string sql = @"
                UPDATE ubicaciones 
                SET codigo_ubicacion = @cod,
                    sector_pasillo = @sec,
                    estante_mueble = @est,
                    repisa_nivel = @rep,
                    descripcion = @desc
                WHERE id_ubicacion = @id";

            await using var cmd = new MySqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@cod", ubicacion.CodigoUbicacion.Trim().ToUpperInvariant());
            cmd.Parameters.AddWithValue("@sec", ubicacion.SectorPasillo.Trim());
            cmd.Parameters.AddWithValue("@est", ubicacion.EstanteMueble.Trim());
            cmd.Parameters.AddWithValue("@rep", string.IsNullOrWhiteSpace(ubicacion.RepisaNivel) ? DBNull.Value : ubicacion.RepisaNivel.Trim());
            cmd.Parameters.AddWithValue("@desc", string.IsNullOrWhiteSpace(ubicacion.Descripcion) ? DBNull.Value : ubicacion.Descripcion.Trim());
            cmd.Parameters.AddWithValue("@id", ubicacion.IdUbicacion);
            await cmd.ExecuteNonQueryAsync();

            return (true, "Ubicación actualizada correctamente.");
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            return (false, "Ya existe otra ubicación con ese código.");
        }
    }

    public async Task<(bool Exito, string Mensaje)> ActualizarUbicacionSalaAsync(UbicacionModel ubicacion, string rolUsuario)
    {
        bool tienePermiso = rolUsuario.Contains("Encargado", StringComparison.OrdinalIgnoreCase) ||
                            rolUsuario.Contains("Administrador", StringComparison.OrdinalIgnoreCase);

        if (!tienePermiso)
            return (false, "Acceso denegado (CU-04): Solo el Encargado de Inventario puede editar ubicaciones de sala.");

        if (string.IsNullOrWhiteSpace(ubicacion.CodigoUbicacion) || string.IsNullOrWhiteSpace(ubicacion.SectorPasillo) || string.IsNullOrWhiteSpace(ubicacion.EstanteMueble))
            return (false, "Código, Zona/Góndola y Sección son obligatorios.");

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        try
        {
            const string sql = @"
                UPDATE ubicaciones 
                SET codigo_ubicacion = @cod,
                    sector_pasillo = @sec,
                    estante_mueble = @est,
                    repisa_nivel = @rep,
                    descripcion = @desc
                WHERE id_ubicacion = @id AND tipo_zona = 'SALA_VENTAS'";

            await using var cmd = new MySqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@cod", ubicacion.CodigoUbicacion.Trim().ToUpperInvariant());
            cmd.Parameters.AddWithValue("@sec", ubicacion.SectorPasillo.Trim());
            cmd.Parameters.AddWithValue("@est", ubicacion.EstanteMueble.Trim());
            cmd.Parameters.AddWithValue("@rep", string.IsNullOrWhiteSpace(ubicacion.RepisaNivel) ? DBNull.Value : ubicacion.RepisaNivel.Trim());
            cmd.Parameters.AddWithValue("@desc", string.IsNullOrWhiteSpace(ubicacion.Descripcion) ? DBNull.Value : ubicacion.Descripcion.Trim());
            cmd.Parameters.AddWithValue("@id", ubicacion.IdUbicacion);
            await cmd.ExecuteNonQueryAsync();

            return (true, "Ubicación de sala actualizada correctamente.");
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            return (false, "Ya existe otra ubicación de sala con ese código.");
        }
    }

    public async Task<(bool Exito, string Mensaje)> CambiarEstadoUbicacionAsync(int idUbicacion, bool nuevoEstado, string rolUsuario)
    {
        bool tienePermiso = rolUsuario.Contains("Encargado", StringComparison.OrdinalIgnoreCase) ||
                            rolUsuario.Contains("Administrador", StringComparison.OrdinalIgnoreCase);

        if (!tienePermiso)
            return (false, "Acceso denegado (CU-04): Solo el Encargado de Inventario puede activar o inactivar ubicaciones.");

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        if (!nuevoEstado)
        {
            const string sqlStock = "SELECT COALESCE(SUM(cantidad), 0) FROM lote_ubicaciones WHERE id_ubicacion = @id";
            await using var cmdChk = new MySqlCommand(sqlStock, cn);
            cmdChk.Parameters.AddWithValue("@id", idUbicacion);
            int stockEnUbicacion = Convert.ToInt32(await cmdChk.ExecuteScalarAsync());

            if (stockEnUbicacion > 0)
                return (false, $"No se puede inactivar la ubicación porque aún tiene {stockEnUbicacion} unidades asignadas. Reubíquelas primero.");
        }

        const string sqlUpd = "UPDATE ubicaciones SET activo = @activo WHERE id_ubicacion = @id";
        await using var cmd = new MySqlCommand(sqlUpd, cn);
        cmd.Parameters.AddWithValue("@activo", nuevoEstado ? 1 : 0);
        cmd.Parameters.AddWithValue("@id", idUbicacion);
        await cmd.ExecuteNonQueryAsync();

        return (true, nuevoEstado ? "Ubicación activada correctamente." : "Ubicación inactivada correctamente.");
    }

    public async Task<(bool Exito, string Mensaje)> AsignarUbicacionBodegaAsync(int idLote, int idUbicacion, int cantidad, int? idUsuario = null)
    {
        if (cantidad <= 0)
            return (false, "La cantidad a asignar debe ser mayor a cero.");

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        await using var tx = await cn.BeginTransactionAsync();

        try
        {
            const string sqlUbic = "SELECT activo, tipo_zona FROM ubicaciones WHERE id_ubicacion = @idU";
            await using (var cmdU = new MySqlCommand(sqlUbic, cn, tx))
            {
                cmdU.Parameters.AddWithValue("@idU", idUbicacion);
                await using var rdU = await cmdU.ExecuteReaderAsync();
                if (!await rdU.ReadAsync())
                    return (false, "La ubicación seleccionada no existe.");
                if (!rdU.GetBoolean(0))
                    return (false, "La ubicación seleccionada se encuentra inactiva.");
                if (rdU.GetString(1) != "BODEGA")
                    return (false, "La ubicación seleccionada no pertenece a Bodega.");
            }

            int stockTotal = 0;
            int yaUbicado = 0;
            const string sqlStock = @"
                SELECT l.stock_actual, COALESCE(SUM(lu.cantidad), 0)
                FROM lotes l
                LEFT JOIN lote_ubicaciones lu ON lu.id_lote = l.id_lote
                WHERE l.id_lote = @idL
                GROUP BY l.id_lote";

            await using (var cmdS = new MySqlCommand(sqlStock, cn, tx))
            {
                cmdS.Parameters.AddWithValue("@idL", idLote);
                await using var rdS = await cmdS.ExecuteReaderAsync();
                if (!await rdS.ReadAsync())
                    return (false, "El lote seleccionado no existe.");
                stockTotal = Ent(rdS, 0);
                yaUbicado = Ent(rdS, 1);
            }

            int sinUbicar = stockTotal - yaUbicado;
            if (cantidad > sinUbicar)
                return (false, $"La cantidad ({cantidad}) supera el stock disponible sin ubicar ({sinUbicar} un.).");

            const string sqlUpsert = @"
                INSERT INTO lote_ubicaciones (id_lote, id_ubicacion, cantidad, fecha_asignacion, id_usuario)
                VALUES (@idL, @idU, @cant, NOW(), @usr)
                ON DUPLICATE KEY UPDATE 
                    cantidad = cantidad + @cant,
                    fecha_asignacion = NOW(),
                    id_usuario = @usr";

            await using (var cmdIns = new MySqlCommand(sqlUpsert, cn, tx))
            {
                cmdIns.Parameters.AddWithValue("@idL", idLote);
                cmdIns.Parameters.AddWithValue("@idU", idUbicacion);
                cmdIns.Parameters.AddWithValue("@cant", cantidad);
                cmdIns.Parameters.AddWithValue("@usr", idUsuario.HasValue ? idUsuario.Value : DBNull.Value);
                await cmdIns.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
            return (true, $"Se asignaron {cantidad} unidades a la ubicación correctamente.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return (false, "Error al registrar la ubicación: " + ex.Message);
        }
    }

    public async Task<(bool Exito, string Mensaje)> ReubicarLoteBodegaAsync(int idLote, int idUbicacionOrigen, int idUbicacionDestino, int cantidad, int? idUsuario = null)
    {
        if (idUbicacionOrigen == idUbicacionDestino)
            return (false, "La ubicación de destino debe ser distinta a la de origen.");
        if (cantidad <= 0)
            return (false, "La cantidad a reubicar debe ser mayor a cero.");

        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        await using var tx = await cn.BeginTransactionAsync();

        try
        {
            const string sqlOrig = "SELECT cantidad FROM lote_ubicaciones WHERE id_lote = @idL AND id_ubicacion = @idOrig";
            int cantOrigen = 0;
            await using (var cmdO = new MySqlCommand(sqlOrig, cn, tx))
            {
                cmdO.Parameters.AddWithValue("@idL", idLote);
                cmdO.Parameters.AddWithValue("@idOrig", idUbicacionOrigen);
                var obj = await cmdO.ExecuteScalarAsync();
                if (obj == null || obj == DBNull.Value)
                    return (false, "El lote no tiene stock en la ubicación de origen.");
                cantOrigen = Convert.ToInt32(obj);
            }

            if (cantidad > cantOrigen)
                return (false, $"Stock insuficiente en origen. Disponible en esa ubicación: {cantOrigen} un.");

            if (cantidad == cantOrigen)
            {
                const string sqlDel = "DELETE FROM lote_ubicaciones WHERE id_lote = @idL AND id_ubicacion = @idOrig";
                await using var cmdD = new MySqlCommand(sqlDel, cn, tx);
                cmdD.Parameters.AddWithValue("@idL", idLote);
                cmdD.Parameters.AddWithValue("@idOrig", idUbicacionOrigen);
                await cmdD.ExecuteNonQueryAsync();
            }
            else
            {
                const string sqlDec = "UPDATE lote_ubicaciones SET cantidad = cantidad - @cant, fecha_asignacion = NOW(), id_usuario = @usr WHERE id_lote = @idL AND id_ubicacion = @idOrig";
                await using var cmdDec = new MySqlCommand(sqlDec, cn, tx);
                cmdDec.Parameters.AddWithValue("@cant", cantidad);
                cmdDec.Parameters.AddWithValue("@usr", idUsuario.HasValue ? idUsuario.Value : DBNull.Value);
                cmdDec.Parameters.AddWithValue("@idL", idLote);
                cmdDec.Parameters.AddWithValue("@idOrig", idUbicacionOrigen);
                await cmdDec.ExecuteNonQueryAsync();
            }

            const string sqlDest = @"
                INSERT INTO lote_ubicaciones (id_lote, id_ubicacion, cantidad, fecha_asignacion, id_usuario)
                VALUES (@idL, @idDest, @cant, NOW(), @usr)
                ON DUPLICATE KEY UPDATE 
                    cantidad = cantidad + @cant,
                    fecha_asignacion = NOW(),
                    id_usuario = @usr";

            await using (var cmdDest = new MySqlCommand(sqlDest, cn, tx))
            {
                cmdDest.Parameters.AddWithValue("@idL", idLote);
                cmdDest.Parameters.AddWithValue("@idDest", idUbicacionDestino);
                cmdDest.Parameters.AddWithValue("@cant", cantidad);
                cmdDest.Parameters.AddWithValue("@usr", idUsuario.HasValue ? idUsuario.Value : DBNull.Value);
                await cmdDest.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
            return (true, $"Reubicación exitosa de {cantidad} unidades.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return (false, "Error al reubicar el lote: " + ex.Message);
        }
    }

    #endregion

    private void AsignarParametrosProducto(MySqlCommand cmd, ProductoModel p)
    {
        cmd.Parameters.AddWithValue("@sku", p.Sku);
        cmd.Parameters.AddWithValue("@cb", string.IsNullOrEmpty(p.CodigoBarras) ? (object)DBNull.Value : p.CodigoBarras);
        cmd.Parameters.AddWithValue("@nombre", p.NombreComercial);
        cmd.Parameters.AddWithValue("@pa", string.IsNullOrEmpty(p.PrincipioActivo) ? (object)DBNull.Value : p.PrincipioActivo);
        cmd.Parameters.AddWithValue("@rs", p.RegistroSanitario);
        cmd.Parameters.AddWithValue("@formato", string.IsNullOrEmpty(p.FormatoPresentacion) ? (object)DBNull.Value : p.FormatoPresentacion);
        cmd.Parameters.AddWithValue("@cat", p.IdCategoria);
        cmd.Parameters.AddWithValue("@pv", p.PrecioVenta);
        cmd.Parameters.AddWithValue("@costo", p.CostoPromedio);
        cmd.Parameters.AddWithValue("@min", p.StockMinimo);
        cmd.Parameters.AddWithValue("@reorden", p.PuntoReorden);
    }

    private static decimal Dec(DbDataReader r, int i) => r.IsDBNull(i) ? 0m : Convert.ToDecimal(r.GetValue(i));
    private static int Ent(DbDataReader r, int i) => r.IsDBNull(i) ? 0 : Convert.ToInt32(r.GetValue(i));
}