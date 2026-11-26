using TISW_TUSSI_ERP.Helpers;

namespace TISW_TUSSI_ERP.Models.Compras;

// ==========================================================
// RF-COM-11: GESTIÓN DE EQUIVALENCIAS DE PRESENTACIÓN
// ==========================================================
public class EquivalenciaProducto
{
    public int IdEquivalencia { get; set; }
    public int IdProducto { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string NombreProducto { get; set; } = string.Empty;
    public string UnidadCompra { get; set; } = string.Empty;
    public string UnidadVenta { get; set; } = string.Empty;
    public decimal FactorConversion { get; set; } = 1.0m;
    public bool Activo { get; set; } = true;

    public string DescripcionConversion => $"1 {UnidadCompra} = {FactorConversion:0.##} {UnidadVenta}";
    public string FactorTexto => $"× {FactorConversion:0.##}";
}
