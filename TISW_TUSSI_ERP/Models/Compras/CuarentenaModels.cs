using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.ViewModels;

namespace TISW_TUSSI_ERP.Models.Compras;

// ==========================================================
// RF-COM-09: GESTIÓN DE CUARENTENA DE LOTES
// ==========================================================

public class LoteCuarentena
{
    public int IdLote { get; set; }
    public int IdProducto { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string NombreProducto { get; set; } = string.Empty;
    public string NumeroLote { get; set; } = string.Empty;
    public DateTime FechaVencimiento { get; set; }
    public int StockActual { get; set; }
    public string EstadoCuarentena { get; set; } = string.Empty;

    public string FechaVencimientoTexto => FechaVencimiento.ToString("dd/MM/yyyy", Formato.Cultura);
    public string StockActualTexto => $"{StockActual} un.";

    public bool EstaEnCuarentena => EstadoCuarentena == "CUARENTENA";

    public string EstadoTexto => EstaEnCuarentena ? "En Cuarentena" : "Liberado";

    public Color EstadoColor => EstaEnCuarentena
        ? Color.FromArgb("#B45309")
        : Color.FromArgb("#15803D");

    public Color EstadoFondo => EstaEnCuarentena
        ? Color.FromArgb("#FEF3C7")
        : Color.FromArgb("#DCFCE7");
}
