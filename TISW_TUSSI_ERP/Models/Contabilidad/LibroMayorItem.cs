using TISW_TUSSI_ERP.Helpers;

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

    // Presentación
    public string TotalDebeTexto => Formato.Moneda(TotalDebe);
    public string TotalHaberTexto => Formato.Moneda(TotalHaber);
    public string SaldoDeudorTexto => SaldoDeudor > 0 ? Formato.Moneda(SaldoDeudor) : "–";
    public string SaldoAcreedorTexto => SaldoAcreedor > 0 ? Formato.Moneda(SaldoAcreedor) : "–";
    public string TipoTexto => Formato.Capitalizar(TipoCuenta.ToLowerInvariant());
    public Color TipoFondo => EstiloContable.TipoCuenta(TipoCuenta).Fondo;
    public Color TipoColor => EstiloContable.TipoCuenta(TipoCuenta).Texto;
}
