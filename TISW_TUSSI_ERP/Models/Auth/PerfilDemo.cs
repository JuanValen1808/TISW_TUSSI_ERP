namespace TISW_TUSSI_ERP.Models.Auth;

// Tarjeta de "Acceso rápido de demostración" del Login.
public class PerfilDemo
{
    public string Iniciales { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Clave { get; set; } = string.Empty;
    public string ClaveTexto => $"Clave: {Clave}";
}
