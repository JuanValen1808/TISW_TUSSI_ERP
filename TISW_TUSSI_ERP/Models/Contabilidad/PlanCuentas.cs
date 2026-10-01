namespace TISW_TUSSI_ERP.Models.Contabilidad;

public class PlanCuentas
{
    public int IdCuenta { get; set; }
    public string CodigoCuenta { get; set; } = string.Empty;
    public string NombreCuenta { get; set; } = string.Empty;
    public string TipoCuenta { get; set; } = string.Empty;
    public int Nivel { get; set; }
    public int? CuentaPadreId { get; set; }
}
