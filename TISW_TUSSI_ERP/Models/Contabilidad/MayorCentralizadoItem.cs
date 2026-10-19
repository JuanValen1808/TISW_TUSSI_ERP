using System;

namespace TISW_TUSSI_ERP.Models.Contabilidad;

public class MayorCentralizadoItem
{
    public DateTime Fecha { get; set; }
    public int NumeroAsiento { get; set; }
    public string Glosa { get; set; } = string.Empty;
    public string CodigoCuenta { get; set; } = string.Empty;
    public string NombreCuenta { get; set; } = string.Empty;
    public decimal Debe { get; set; }
    public decimal Haber { get; set; }
    public decimal SaldoAcumulado { get; set; } // Calculado en memoria
}
