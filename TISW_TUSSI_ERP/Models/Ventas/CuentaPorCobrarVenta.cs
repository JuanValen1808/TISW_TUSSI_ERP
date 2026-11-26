
using TISW_TUSSI_ERP.Helpers;

namespace TISW_TUSSI_ERP.Models.Ventas;

public class CuentaPorCobrarVenta
{
    public int IdCuenta { get; set; }

    public string Folio { get; set; } = "";

    public string Cliente { get; set; } = "";

    public string Rut { get; set; } = "";

    public string Convenio { get; set; } = "";

    public DateTime Vencimiento { get; set; }

    public decimal Saldo { get; set; }

    public string SaldoTexto => Formato.Moneda(Saldo);

    public string VencimientoTexto =>
        Vencimiento.ToString("dd/MM/yyyy");

    public string Estado { get; set; } = "";
}
