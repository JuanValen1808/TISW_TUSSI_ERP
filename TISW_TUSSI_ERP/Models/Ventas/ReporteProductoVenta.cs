
using System;
using System.Globalization;

namespace TISW_TUSSI_ERP.Models.Ventas;

public class ReporteProductoVenta
{
    private static readonly CultureInfo Cultura =
        CultureInfo.GetCultureInfo("es-CL");

    public string Sku { get; set; } = "";
    public string NombreProducto { get; set; } = "";
    public string Categoria { get; set; } = "";

    public int UnidadesVendidas { get; set; }
    public int NumeroVentas { get; set; }
    public decimal TotalVendido { get; set; }

    public string TotalTexto =>
        TotalVendido.ToString("C0", Cultura);

    public string UnidadesTexto =>
        UnidadesVendidas.ToString("N0", Cultura);
}

public class ReporteCategoriaVenta
{
    private static readonly CultureInfo Cultura =
        CultureInfo.GetCultureInfo("es-CL");

    public string Categoria { get; set; } = "";
    public int UnidadesVendidas { get; set; }
    public int ProductosDistintos { get; set; }
    public decimal TotalVendido { get; set; }

    public string TotalTexto =>
        TotalVendido.ToString("C0", Cultura);

    public string UnidadesTexto =>
        UnidadesVendidas.ToString("N0", Cultura);
}
