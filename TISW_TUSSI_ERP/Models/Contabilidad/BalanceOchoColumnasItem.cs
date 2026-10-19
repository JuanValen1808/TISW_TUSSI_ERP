namespace TISW_TUSSI_ERP.Models.Contabilidad;

public class BalanceOchoColumnasItem
{
    public int IdCuenta { get; set; }
    public string CodigoCuenta { get; set; } = string.Empty;
    public string NombreCuenta { get; set; } = string.Empty;
    public string TipoCuenta { get; set; } = string.Empty;
    
    // Sumas (Columnas 1 y 2)
    public decimal TotalDebe { get; set; }
    public decimal TotalHaber { get; set; }
    
    // Saldos (Columnas 3 y 4)
    public decimal SaldoDeudor { get; set; }
    public decimal SaldoAcreedor { get; set; }

    // Inventario (Columnas 5 y 6)
    public decimal Activo { get; set; }
    public decimal Pasivo { get; set; }

    // Resultados (Columnas 7 y 8)
    public decimal Perdida { get; set; }
    public decimal Ganancia { get; set; }
}
