
namespace TISW_TUSSI_ERP.Models.Ventas;

public class Cliente
{
    public int IdCliente { get; set; }

    public string RutCliente { get; set; } = string.Empty;

    public string RazonSocialNombre { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Telefono { get; set; }

    public string TipoCliente { get; set; } = "PERSONA";
}
