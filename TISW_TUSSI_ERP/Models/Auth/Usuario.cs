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
}
