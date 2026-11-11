namespace TISW_TUSSI_ERP.Models.Auth;

// Guarda en memoria el usuario que inició sesión durante el uso de la app y expone el middleware RBAC (RF-POR-03)
public static class Sesion
{
    public static Usuario? UsuarioActual { get; set; }

    public static bool HaySesionActiva => UsuarioActual is not null;

    public static void CerrarSesion() => UsuarioActual = null;

    // Middleware de verificación RBAC por permiso o módulo (RF-POR-03)
    public static bool TienePermiso(string codigoPermiso)
    {
        var u = UsuarioActual;
        if (u is null || !u.Activo) return false;
        if (u.EsAdministrador) return true;
        return u.Permisos.Contains(codigoPermiso);
    }

    public static bool PuedeAccederSeccion(string seccion)
    {
        var u = UsuarioActual;
        if (u is null || !u.Activo) return false;
        if (u.EsAdministrador) return true;

        if (seccion == "PRINCIPAL") return true;

        var rol = u.NombreRol.Trim();

        // Si el perfil tiene permisos explícitos en base de datos, validamos contra ellos
        if (u.Permisos.Count > 0)
        {
            return seccion switch
            {
                "CONTABILIDAD" => rol.Equals("Contador", StringComparison.OrdinalIgnoreCase)
                                  || u.Permisos.Any(p => p.StartsWith("CONTAB_", StringComparison.OrdinalIgnoreCase) || p.StartsWith("CON_", StringComparison.OrdinalIgnoreCase)),
                "OPERACIÓN" => rol.Equals("Vendedor", StringComparison.OrdinalIgnoreCase)
                               || u.Permisos.Any(p => p.StartsWith("VENTAS_", StringComparison.OrdinalIgnoreCase)),
                "INVENTARIOS" => rol.Equals("Bodega", StringComparison.OrdinalIgnoreCase)
                                 || rol.Equals("Vendedor", StringComparison.OrdinalIgnoreCase)
                                 || u.Permisos.Any(p => p.StartsWith("INV_", StringComparison.OrdinalIgnoreCase)),
                "COMPRAS" => rol.Equals("Comprador", StringComparison.OrdinalIgnoreCase)
                             || rol.Equals("Bodega", StringComparison.OrdinalIgnoreCase)
                             || u.Permisos.Any(p => p.StartsWith("COMPRAS_", StringComparison.OrdinalIgnoreCase)),
                "ADMINISTRACIÓN" => u.Permisos.Any(p => p.StartsWith("ADMIN_", StringComparison.OrdinalIgnoreCase) || p.StartsWith("SEG_", StringComparison.OrdinalIgnoreCase)),
                _ => false
            };
        }

        // Respaldo por nombre de rol
        return seccion switch
        {
            "CONTABILIDAD" => rol.Equals("Contador", StringComparison.OrdinalIgnoreCase),
            "OPERACIÓN" => rol.Equals("Vendedor", StringComparison.OrdinalIgnoreCase),
            "INVENTARIOS" => rol.Equals("Bodega", StringComparison.OrdinalIgnoreCase) || rol.Equals("Vendedor", StringComparison.OrdinalIgnoreCase),
            "COMPRAS" => rol.Equals("Comprador", StringComparison.OrdinalIgnoreCase) || rol.Equals("Bodega", StringComparison.OrdinalIgnoreCase),
            "ADMINISTRACIÓN" => rol.Equals("Administrador", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }
}
