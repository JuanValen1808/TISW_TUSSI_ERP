namespace TISW_TUSSI_ERP.Models.Auth;

// Guarda en memoria el usuario que inició sesión durante el uso de la app y expone el middleware RBAC (RF-POR-03)
public static class Sesion
{
    public static Usuario? UsuarioActual { get; set; }

    public static bool HaySesionActiva => UsuarioActual is not null;

    public static void CerrarSesion() => UsuarioActual = null;

    // Middleware de verificación RBAC por código de permiso específico (RF-POR-03)
    public static bool TienePermiso(string codigoPermiso)
    {
        var u = UsuarioActual;
        if (u is null || !u.Activo) return false;
        if (u.EsAdministrador) return true;
        if (string.IsNullOrWhiteSpace(codigoPermiso)) return true;

        if (u.Permisos.Contains(codigoPermiso))
            return true;

        // Compatibilidad con permisos generales heredados si aún no se han desglosado
        if (codigoPermiso.StartsWith("VENTAS_", StringComparison.OrdinalIgnoreCase) && u.Permisos.Contains("VENTAS_CREAR"))
            return true;
        if (codigoPermiso.StartsWith("INVENTARIO_", StringComparison.OrdinalIgnoreCase) && u.Permisos.Contains("INVENTARIO_VER"))
            return true;
        if (codigoPermiso.StartsWith("COMPRAS_", StringComparison.OrdinalIgnoreCase) && u.Permisos.Contains("COMPRAS_CREAR"))
            return true;
        if (codigoPermiso.StartsWith("CONTAB_", StringComparison.OrdinalIgnoreCase) && u.Permisos.Contains("CONTABILIDAD_VER"))
            return true;

        return false;
    }

    // Verifica si el usuario tiene acceso a una vista específica según los poderes asignados por el Administrador
    public static bool PuedeAccederVista(string codigoPermiso, string seccion)
    {
        var u = UsuarioActual;
        if (u is null || !u.Activo) return false;
        if (u.EsAdministrador) return true;
        if (seccion == "PRINCIPAL") return true;

        if (u.Permisos.Count > 0)
        {
            if (!string.IsNullOrWhiteSpace(codigoPermiso))
                return TienePermiso(codigoPermiso);
            return PuedeAccederSeccion(seccion);
        }

        return PuedeAccederSeccionPorRolBase(u.NombreRol.Trim(), seccion);
    }

    public static bool PuedeAccederSeccion(string seccion)
    {
        var u = UsuarioActual;
        if (u is null || !u.Activo) return false;
        if (u.EsAdministrador) return true;
        if (seccion == "PRINCIPAL") return true;

        // Si el perfil tiene permisos configurados en la matriz RBAC, se respetan estrictamente los poderes asignados por el Admin
        if (u.Permisos.Count > 0)
        {
            return seccion switch
            {
                "CONTABILIDAD" => u.Permisos.Any(p => p.StartsWith("CONTAB_", StringComparison.OrdinalIgnoreCase) || p.Equals("CONTABILIDAD_VER", StringComparison.OrdinalIgnoreCase)),
                "OPERACIÓN" or "VENTAS" => u.Permisos.Any(p => p.StartsWith("VENTAS_", StringComparison.OrdinalIgnoreCase) || p.Equals("CONVENIOS_ADMIN", StringComparison.OrdinalIgnoreCase)),
                "INVENTARIOS" => u.Permisos.Any(p => p.StartsWith("INVENTARIO_", StringComparison.OrdinalIgnoreCase) || p.Equals("PRODUCTOS_CREAR", StringComparison.OrdinalIgnoreCase)),
                "COMPRAS" => u.Permisos.Any(p => p.StartsWith("COMPRAS_", StringComparison.OrdinalIgnoreCase) || p.Equals("PROVEEDORES_CREAR", StringComparison.OrdinalIgnoreCase)),
                "ADMINISTRACIÓN" => u.Permisos.Any(p => p.StartsWith("ADMIN_", StringComparison.OrdinalIgnoreCase) || p.StartsWith("SEG_", StringComparison.OrdinalIgnoreCase)),
                _ => false
            };
        }

        // Respaldo estricto por rol cuando no hay permisos explícitos cargados
        return PuedeAccederSeccionPorRolBase(u.NombreRol.Trim(), seccion);
    }

    private static bool PuedeAccederSeccionPorRolBase(string rol, string seccion)
    {
        return seccion switch
        {
            "CONTABILIDAD" => rol.Contains("Contador", StringComparison.OrdinalIgnoreCase),
            "OPERACIÓN" or "VENTAS" => rol.Contains("Vendedor", StringComparison.OrdinalIgnoreCase) || rol.Contains("Gerente", StringComparison.OrdinalIgnoreCase),
            "INVENTARIOS" => rol.Contains("Bodega", StringComparison.OrdinalIgnoreCase) || rol.Contains("Inventario", StringComparison.OrdinalIgnoreCase),
            "COMPRAS" => rol.Contains("Comprador", StringComparison.OrdinalIgnoreCase) || rol.Contains("Compras", StringComparison.OrdinalIgnoreCase),
            "ADMINISTRACIÓN" => rol.Contains("Administrador", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }
}
