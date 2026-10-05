using TISW_TUSSI_ERP.Helpers;

namespace TISW_TUSSI_ERP.Models.Compras;

// Proveedor / laboratorio (RF-COM-01)
public class Proveedor
{
    public int Id { get; set; }
    public string Rut { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string Contacto { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CondicionesComerciales { get; set; } = string.Empty;

    // Datos calculados desde ordenes_compra, solo para la lista (no se guardan)
    public int OrdenesEmitidas { get; set; }
    public decimal MontoComprado { get; set; }
    public string MontoCompradoTexto => Formato.Moneda(MontoComprado);
    public string Iniciales => Formato.Iniciales(RazonSocial);
}
