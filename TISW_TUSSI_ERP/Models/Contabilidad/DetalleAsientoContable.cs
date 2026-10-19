namespace TISW_TUSSI_ERP.Models.Contabilidad;

public class DetalleAsientoContable
{
    public int IdDetalleAsiento { get; set; }
    public int IdAsiento { get; set; }
    public int IdCuenta { get; set; }
    public decimal Debe { get; set; }
    public decimal Haber { get; set; }
}
