using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.ViewModels;

namespace TISW_TUSSI_ERP.Models.Compras;

// ==========================================================
// RF-COM-07: DEVOLUCIONES A PROVEEDOR
// ==========================================================

public class LoteDevolucionItem : BaseViewModel
{
    public int IdLote { get; set; }
    public int IdProducto { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string NombreProducto { get; set; } = string.Empty;
    public string NumeroLote { get; set; } = string.Empty;
    public DateTime FechaVencimiento { get; set; }
    public int StockActual { get; set; }
    public decimal CostoUnitario { get; set; }

    private int _cantidadADevolver;
    public int CantidadADevolver
    {
        get => _cantidadADevolver;
        set
        {
            if (SetProperty(ref _cantidadADevolver, value))
            {
                OnPropertyChanged(nameof(Subtotal));
                OnPropertyChanged(nameof(SubtotalTexto));
            }
        }
    }

    public decimal Subtotal => CantidadADevolver * CostoUnitario;

    public string FechaVencimientoTexto => FechaVencimiento.ToString("dd/MM/yyyy", Formato.Cultura);
    public string StockActualTexto => $"{StockActual} un.";
    public string CostoUnitarioTexto => Formato.Moneda(CostoUnitario);
    public string SubtotalTexto => Formato.Moneda(Subtotal);
}

public class DevolucionProveedorInput
{
    public int IdProveedor { get; set; }
    public int? IdRecepcion { get; set; }
    public string NumeroNotaCredito { get; set; } = string.Empty;
    public DateTime FechaDevolucion { get; set; } = DateTime.Today;
    public string Motivo { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;
    public List<LoteDevolucionItem> Lineas { get; set; } = new();
}

public class HistorialDevolucionProveedor
{
    public int Id { get; set; }
    public string NumeroNotaCredito { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string Proveedor { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public decimal MontoTotal { get; set; }
    public string Estado { get; set; } = string.Empty;

    public string FechaTexto => Fecha.ToString("dd/MM/yyyy", Formato.Cultura);
    public string MontoTotalTexto => Formato.Moneda(MontoTotal);

    public string MotivoTexto => Motivo switch
    {
        "MERMA" => "Merma",
        "VENCIMIENTO_PROXIMO" => "Vencimiento Próximo",
        "DAÑO" => "Daño",
        "OTRO" => "Otro",
        _ => Motivo
    };

    public Color EstadoColor => Estado switch
    {
        "REGISTRADA" => Color.FromArgb("#B45309"),
        "PROCESADA" => Color.FromArgb("#15803D"),
        "ANULADA" => Color.FromArgb("#B91C1C"),
        _ => Paleta.Gris
    };

    public Color EstadoFondo => Estado switch
    {
        "REGISTRADA" => Color.FromArgb("#FEF3C7"),
        "PROCESADA" => Color.FromArgb("#DCFCE7"),
        "ANULADA" => Color.FromArgb("#FEE2E2"),
        _ => Color.FromArgb("#F3F4F6")
    };

    public string EstadoTexto => Estado switch
    {
        "REGISTRADA" => "Registrada",
        "PROCESADA" => "Procesada",
        "ANULADA" => "Anulada",
        _ => Estado
    };
}
