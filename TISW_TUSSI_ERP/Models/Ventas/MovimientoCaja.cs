
namespace TISW_TUSSI_ERP.Models.Ventas;

public class MovimientoCaja
{
    public int IdMovimiento { get; set; }
    public int IdTurno { get; set; }
    public int IdUsuario { get; set; }

    public string TipoMovimiento { get; set; } = "INGRESO";
    public string Concepto { get; set; } = "";

    public decimal Monto { get; set; }
    public DateTime FechaMovimiento { get; set; }

    public int? IdVenta { get; set; }

    public bool EsIngreso => TipoMovimiento == "INGRESO";
}
