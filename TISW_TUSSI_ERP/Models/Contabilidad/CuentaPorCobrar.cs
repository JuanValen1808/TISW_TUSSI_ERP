namespace TISW_TUSSI_ERP.Models.Contabilidad;

public class CuentaPorCobrar
{
    public int IdDocumento { get; set; }
    public string NumeroDocumento { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public decimal MontoTotal { get; set; }
    public decimal MontoPagado { get; set; }
    public decimal SaldoPendiente => MontoTotal - MontoPagado;
    public string Estado => SaldoPendiente <= 0 ? "Pagado" : (DateTime.Now > FechaVencimiento ? "Vencido" : "Pendiente");
}
