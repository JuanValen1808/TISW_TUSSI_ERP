using System.Text.Json.Serialization;

namespace TISW_TUSSI_ERP.Models.Inventario;

public class MermaModel
{
    [JsonPropertyName("idMerma")]
    public int IdMerma { get; set; }

    [JsonPropertyName("idLote")]
    public int IdLote { get; set; }

    [JsonPropertyName("numeroLote")]
    public string NumeroLote { get; set; } = string.Empty;

    [JsonPropertyName("sku")]
    public string Sku { get; set; } = string.Empty;

    [JsonPropertyName("nombreProducto")]
    public string NombreProducto { get; set; } = string.Empty;

    [JsonPropertyName("cantidadBaja")]
    public int CantidadBaja { get; set; }

    [JsonPropertyName("motivo")]
    public string Motivo { get; set; } = string.Empty; // VENCIMIENTO, ROTURA, DAÑO, OTRO

    [JsonPropertyName("fechaRegistro")]
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    [JsonPropertyName("usuarioResponsable")]
    public string UsuarioResponsable { get; set; } = string.Empty;

    // Formato visual para la UI
    public string FechaTexto => FechaRegistro.ToString("dd/MM/yyyy HH:mm");

    public string ColorMotivo => Motivo switch
    {
        "VENCIMIENTO" => "#D97706", // Ámbar
        "ROTURA" or "DAÑO" => "#B91C1C", // Rojo
        _ => "#4B5563"
    };

    public string FondoMotivo => Motivo switch
    {
        "VENCIMIENTO" => "#FEF3C7",
        "ROTURA" or "DAÑO" => "#FEE2E2",
        _ => "#F3F4F6"
    };
}