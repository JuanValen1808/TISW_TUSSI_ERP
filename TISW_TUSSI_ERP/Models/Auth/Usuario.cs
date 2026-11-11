namespace TISW_TUSSI_ERP.Models.Auth;

public class Usuario
{
    public int IdUsuario { get; set; }
    public string RutUsuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int IdRol { get; set; }
    public string NombreRol { get; set; } = string.Empty;
    public bool Activo { get; set; }

    // Permisos RBAC cargados desde rol_permisos (RF-POR-03)
    public HashSet<string> Permisos { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public bool EsAdministrador =>
        NombreRol.Equals("Administrador", StringComparison.OrdinalIgnoreCase) || IdRol == 1;
}
