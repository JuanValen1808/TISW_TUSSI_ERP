using System.Data.Common;
using MySqlConnector;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Seguridad;

namespace TISW_TUSSI_ERP.Services.Api;

public class SeguridadService
{
    private readonly DatabaseService _db;

    public SeguridadService(DatabaseService db)
    {
        _db = db;
    }

    // ==========================================
    // USUARIOS
    // ==========================================

    public async Task<List<UsuarioModel>> ObtenerUsuariosAsync(string? busqueda = null, int? idRol = null, bool? soloActivos = null)
    {
        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        var sql = @"
            SELECT u.id_usuario, u.rut_usuario, u.nombre, u.email, u.password_hash,
                   u.id_rol, r.nombre_rol, u.activo, u.created_at
            FROM usuarios u
            INNER JOIN roles r ON r.id_rol = u.id_rol
            WHERE 1=1";

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            sql += " AND (u.nombre LIKE @busq OR u.rut_usuario LIKE @busq OR u.email LIKE @busq)";
        }
        if (idRol.HasValue && idRol.Value > 0)
        {
            sql += " AND u.id_rol = @idRol";
        }
        if (soloActivos.HasValue)
        {
            sql += " AND u.activo = @soloActivos";
        }

        sql += " ORDER BY u.nombre ASC";

        await using var cmd = new MySqlCommand(sql, cn);
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            cmd.Parameters.AddWithValue("@busq", $"%{busqueda.Trim()}%");
        }
        if (idRol.HasValue && idRol.Value > 0)
        {
            cmd.Parameters.AddWithValue("@idRol", idRol.Value);
        }
        if (soloActivos.HasValue)
        {
            cmd.Parameters.AddWithValue("@soloActivos", soloActivos.Value);
        }

        await using var rd = await cmd.ExecuteReaderAsync();
        var lista = new List<UsuarioModel>();
        while (await rd.ReadAsync())
        {
            lista.Add(new UsuarioModel
            {
                IdUsuario = rd.GetInt32(0),
                RutUsuario = rd.GetString(1),
                Nombre = Formato.SanitizarTexto(rd.GetString(2)),
                Email = rd.GetString(3),
                PasswordHash = rd.GetString(4),
                IdRol = rd.GetInt32(5),
                NombreRol = Formato.SanitizarTexto(rd.GetString(6)),
                Activo = rd.GetBoolean(7),
                CreatedAt = Convert.ToDateTime(rd.GetValue(8))
            });
        }
        return lista;
    }

    public async Task<bool> ExisteRutAsync(string rut, int? idUsuarioExcluir = null)
    {
        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        var sql = "SELECT COUNT(*) FROM usuarios WHERE rut_usuario = @rut";
        if (idUsuarioExcluir.HasValue && idUsuarioExcluir.Value > 0)
        {
            sql += " AND id_usuario <> @idExcluir";
        }

        await using var cmd = new MySqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@rut", rut.Trim());
        if (idUsuarioExcluir.HasValue && idUsuarioExcluir.Value > 0)
        {
            cmd.Parameters.AddWithValue("@idExcluir", idUsuarioExcluir.Value);
        }

        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        return count > 0;
    }

    public async Task<bool> ExisteEmailAsync(string email, int? idUsuarioExcluir = null)
    {
        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        var sql = "SELECT COUNT(*) FROM usuarios WHERE email = @email";
        if (idUsuarioExcluir.HasValue && idUsuarioExcluir.Value > 0)
        {
            sql += " AND id_usuario <> @idExcluir";
        }

        await using var cmd = new MySqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@email", email.Trim());
        if (idUsuarioExcluir.HasValue && idUsuarioExcluir.Value > 0)
        {
            cmd.Parameters.AddWithValue("@idExcluir", idUsuarioExcluir.Value);
        }

        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        return count > 0;
    }

    public async Task GuardarUsuarioAsync(UsuarioModel usuario, string? passwordPlana)
    {
        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        if (usuario.IdUsuario == 0)
        {
            // Insertar nuevo usuario
            const string sql = @"
                INSERT INTO usuarios (rut_usuario, nombre, email, password_hash, id_rol, activo)
                VALUES (@rut, @nombre, @email, @hash, @idRol, @activo);
                SELECT LAST_INSERT_ID();";

            var hash = Formato.EncriptarSHA256(passwordPlana ?? "1234");
            await using var cmd = new MySqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@rut", usuario.RutUsuario.Trim());
            cmd.Parameters.AddWithValue("@nombre", usuario.Nombre.Trim());
            cmd.Parameters.AddWithValue("@email", usuario.Email.Trim());
            cmd.Parameters.AddWithValue("@hash", hash);
            cmd.Parameters.AddWithValue("@idRol", usuario.IdRol);
            cmd.Parameters.AddWithValue("@activo", usuario.Activo);

            usuario.IdUsuario = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }
        else
        {
            // Actualizar usuario existente
            string sql;
            if (!string.IsNullOrWhiteSpace(passwordPlana))
            {
                sql = @"
                    UPDATE usuarios
                    SET rut_usuario = @rut,
                        nombre = @nombre,
                        email = @email,
                        password_hash = @hash,
                        id_rol = @idRol,
                        activo = @activo
                    WHERE id_usuario = @id;";
            }
            else
            {
                sql = @"
                    UPDATE usuarios
                    SET rut_usuario = @rut,
                        nombre = @nombre,
                        email = @email,
                        id_rol = @idRol,
                        activo = @activo
                    WHERE id_usuario = @id;";
            }

            await using var cmd = new MySqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@id", usuario.IdUsuario);
            cmd.Parameters.AddWithValue("@rut", usuario.RutUsuario.Trim());
            cmd.Parameters.AddWithValue("@nombre", usuario.Nombre.Trim());
            cmd.Parameters.AddWithValue("@email", usuario.Email.Trim());
            cmd.Parameters.AddWithValue("@idRol", usuario.IdRol);
            cmd.Parameters.AddWithValue("@activo", usuario.Activo);

            if (!string.IsNullOrWhiteSpace(passwordPlana))
            {
                cmd.Parameters.AddWithValue("@hash", Formato.EncriptarSHA256(passwordPlana.Trim()));
            }

            await cmd.ExecuteNonQueryAsync();
        }
    }

    public async Task CambiarEstadoUsuarioAsync(int idUsuario, bool activo)
    {
        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        const string sql = "UPDATE usuarios SET activo = @activo WHERE id_usuario = @id";
        await using var cmd = new MySqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@activo", activo);
        cmd.Parameters.AddWithValue("@id", idUsuario);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task EliminarUsuarioAsync(int idUsuario)
    {
        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        // Validar si tiene registros relacionados en el ERP
        const string checkSql = @"
            SELECT
                (SELECT COUNT(*) FROM ventas_encabezado WHERE id_usuario_cajero = @id) +
                (SELECT COUNT(*) FROM ordenes_compra WHERE id_usuario_creador = @id) +
                (SELECT COUNT(*) FROM libro_diario_asientos WHERE id_usuario = @id) +
                (SELECT COUNT(*) FROM kardex_movimientos WHERE id_usuario = @id) AS total_movimientos;";

        await using var checkCmd = new MySqlCommand(checkSql, cn);
        checkCmd.Parameters.AddWithValue("@id", idUsuario);
        var total = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

        if (total > 0)
        {
            throw new InvalidOperationException(
                "No es posible eliminar este usuario porque registra movimientos contables, de ventas o compras asociadas. Para mantener la integridad y auditoría, use la opción 'Desactivar'.");
        }

        const string deleteSql = "DELETE FROM usuarios WHERE id_usuario = @id";
        await using var deleteCmd = new MySqlCommand(deleteSql, cn);
        deleteCmd.Parameters.AddWithValue("@id", idUsuario);
        await deleteCmd.ExecuteNonQueryAsync();
    }

    // ==========================================
    // PERFILES (ROLES)
    // ==========================================

    public async Task<List<RolModel>> ObtenerRolesAsync(string? busqueda = null)
    {
        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        var sql = @"
            SELECT r.id_rol, r.nombre_rol, COALESCE(r.descripcion, ''),
                   (SELECT COUNT(*) FROM usuarios u WHERE u.id_rol = r.id_rol) AS cant_usuarios,
                   (SELECT COUNT(*) FROM rol_permisos rp WHERE rp.id_rol = r.id_rol) AS cant_permisos
            FROM roles r";

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            sql += " WHERE r.nombre_rol LIKE @busq OR r.descripcion LIKE @busq";
        }

        sql += " ORDER BY r.id_rol ASC";

        await using var cmd = new MySqlCommand(sql, cn);
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            cmd.Parameters.AddWithValue("@busq", $"%{busqueda.Trim()}%");
        }

        await using var rd = await cmd.ExecuteReaderAsync();
        var lista = new List<RolModel>();
        while (await rd.ReadAsync())
        {
            lista.Add(new RolModel
            {
                IdRol = rd.GetInt32(0),
                NombreRol = Formato.SanitizarTexto(rd.GetString(1)),
                Descripcion = Formato.SanitizarTexto(rd.GetString(2)),
                CantidadUsuarios = rd.GetInt32(3),
                CantidadPermisos = rd.GetInt32(4)
            });
        }
        return lista;
    }

    public async Task GuardarRolAsync(RolModel rol)
    {
        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        if (rol.IdRol == 0)
        {
            const string sql = @"
                INSERT INTO roles (nombre_rol, descripcion)
                VALUES (@nombre, @descripcion);
                SELECT LAST_INSERT_ID();";

            await using var cmd = new MySqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@nombre", rol.NombreRol.Trim());
            cmd.Parameters.AddWithValue("@descripcion", rol.Descripcion.Trim());
            rol.IdRol = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }
        else
        {
            const string sql = @"
                UPDATE roles
                SET nombre_rol = @nombre,
                    descripcion = @descripcion
                WHERE id_rol = @id;";

            await using var cmd = new MySqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@id", rol.IdRol);
            cmd.Parameters.AddWithValue("@nombre", rol.NombreRol.Trim());
            cmd.Parameters.AddWithValue("@descripcion", rol.Descripcion.Trim());
            await cmd.ExecuteNonQueryAsync();
        }
    }

    public async Task EliminarRolAsync(int idRol)
    {
        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        // Verificar si tiene usuarios asignados
        const string checkSql = "SELECT COUNT(*) FROM usuarios WHERE id_rol = @id";
        await using var checkCmd = new MySqlCommand(checkSql, cn);
        checkCmd.Parameters.AddWithValue("@id", idRol);
        var usuarios = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

        if (usuarios > 0)
        {
            throw new InvalidOperationException(
                $"No se puede eliminar este perfil porque tiene {usuarios} usuario(s) asignado(s). Reasigne primero los usuarios a otro perfil.");
        }

        // Eliminar permisos asociados y el rol
        await using var tx = await cn.BeginTransactionAsync();
        try
        {
            const string delPermisosSql = "DELETE FROM rol_permisos WHERE id_rol = @id";
            await using var delPermisosCmd = new MySqlCommand(delPermisosSql, cn, tx);
            delPermisosCmd.Parameters.AddWithValue("@id", idRol);
            await delPermisosCmd.ExecuteNonQueryAsync();

            const string delRolSql = "DELETE FROM roles WHERE id_rol = @id";
            await using var delRolCmd = new MySqlCommand(delRolSql, cn, tx);
            delRolCmd.Parameters.AddWithValue("@id", idRol);
            await delRolCmd.ExecuteNonQueryAsync();

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ==========================================
    // ACCESOS Y PERMISOS
    // ==========================================

    public async Task<List<PermisoModel>> ObtenerPermisosPorRolAsync(int idRol)
    {
        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        const string sql = @"
            SELECT p.id_permiso, p.codigo_permiso, p.modulo, COALESCE(p.descripcion, ''),
                   (CASE WHEN rp.id_rol IS NOT NULL THEN 1 ELSE 0 END) AS asignado
            FROM permisos p
            LEFT JOIN rol_permisos rp ON rp.id_permiso = p.id_permiso AND rp.id_rol = @idRol
            ORDER BY p.modulo ASC, p.codigo_permiso ASC;";

        await using var cmd = new MySqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@idRol", idRol);
        await using var rd = await cmd.ExecuteReaderAsync();

        var lista = new List<PermisoModel>();
        while (await rd.ReadAsync())
        {
            lista.Add(new PermisoModel
            {
                IdPermiso = rd.GetInt32(0),
                CodigoPermiso = rd.GetString(1),
                Modulo = Formato.SanitizarTexto(rd.GetString(2)),
                Descripcion = Formato.SanitizarTexto(rd.GetString(3)),
                Asignado = rd.GetInt32(4) == 1
            });
        }
        return lista;
    }

    public async Task GuardarPermisosRolAsync(int idRol, IEnumerable<int> idsPermisosHabilitados)
    {
        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();
        await using var tx = await cn.BeginTransactionAsync();

        try
        {
            // Limpiar permisos actuales del rol
            const string delSql = "DELETE FROM rol_permisos WHERE id_rol = @idRol";
            await using var delCmd = new MySqlCommand(delSql, cn, tx);
            delCmd.Parameters.AddWithValue("@idRol", idRol);
            await delCmd.ExecuteNonQueryAsync();

            // Insertar los nuevos permisos
            var ids = idsPermisosHabilitados.Distinct().ToList();
            if (ids.Count > 0)
            {
                foreach (var idPermiso in ids)
                {
                    const string insSql = "INSERT INTO rol_permisos (id_rol, id_permiso) VALUES (@idRol, @idPermiso)";
                    await using var insCmd = new MySqlCommand(insSql, cn, tx);
                    insCmd.Parameters.AddWithValue("@idRol", idRol);
                    insCmd.Parameters.AddWithValue("@idPermiso", idPermiso);
                    await insCmd.ExecuteNonQueryAsync();
                }
            }

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<int> CrearPermisoAsync(string codigoPermiso, string modulo, string descripcion)
    {
        await using var cn = _db.CrearConexion();
        await cn.OpenAsync();

        const string sql = @"
            INSERT INTO permisos (codigo_permiso, modulo, descripcion)
            VALUES (@codigo, @modulo, @descripcion);
            SELECT LAST_INSERT_ID();";

        await using var cmd = new MySqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@codigo", codigoPermiso.Trim().ToUpperInvariant());
        cmd.Parameters.AddWithValue("@modulo", modulo.Trim());
        cmd.Parameters.AddWithValue("@descripcion", descripcion.Trim());

        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }
}
