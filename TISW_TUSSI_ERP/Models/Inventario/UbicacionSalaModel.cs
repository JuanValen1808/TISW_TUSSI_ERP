namespace TISW_TUSSI_ERP.Models.Inventario;

public class LoteSalaItemModel
{
    public int IdLote { get; set; }
    public int IdProducto { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string CodigoBarras { get; set; } = string.Empty;
    public string NombreProducto { get; set; } = string.Empty;
    public string NumeroLote { get; set; } = string.Empty;
    public DateTime FechaVencimiento { get; set; }
    public int StockTotalLote { get; set; }
    public int StockUbicadoBodega { get; set; }
    public int StockUbicadoSala { get; set; }

    public string VencimientoTexto => FechaVencimiento.ToString("dd/MM/yyyy");
    public bool EstaVencido => FechaVencimiento.Date < DateTime.Today;

    public string EstadoComercializacion => EstaVencido
        ? "VENCIDO (No exhibir)"
        : StockUbicadoBodega <= 0
            ? "Sin stock en bodega"
            : $"Disp. Bodega: {StockUbicadoBodega} un.";

    public string ColorComercializacion => EstaVencido
        ? "#B91C1C"
        : StockUbicadoBodega <= 0
            ? "#6B7280"
            : "#15803D";

    public string FondoComercializacion => EstaVencido
        ? "#FEE2E2"
        : StockUbicadoBodega <= 0
            ? "#F3F4F6"
            : "#DCFCE7";
}