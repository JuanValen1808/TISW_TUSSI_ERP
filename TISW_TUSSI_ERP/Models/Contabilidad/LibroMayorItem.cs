namespace TISW_TUSSI_ERP.Models.Contabilidad;

public class LibroMayorItem
{
    public int IdCuenta { get; set; }
    public string CodigoCuenta { get; set; } = string.Empty;
    public string NombreCuenta { get; set; } = string.Empty;
    public string TipoCuenta { get; set; } = string.Empty;
    
    // Sumatorias de movimientos
    public decimal TotalDebe { get; set; }
    public decimal TotalHaber { get; set; }
    
    // Saldos calculados
    public decimal SaldoDeudor { get; set; }
    public decimal SaldoAcreedor { get; set; }
}
