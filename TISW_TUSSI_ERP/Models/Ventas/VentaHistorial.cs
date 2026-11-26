
using System;
using System.Collections.Generic;
using System.Globalization;

namespace TISW_TUSSI_ERP.Models.Ventas;

public class VentaHistorial
{
    private static readonly CultureInfo Chile =
        CultureInfo.GetCultureInfo("es-CL");

    public int IdVenta { get; set; }
    public string Folio { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string Cliente { get; set; } = "";
    public string RutCliente { get; set; } = "";
    public string Convenio { get; set; } = "";
    public string TipoDocumento { get; set; } = "";
    public string Estado { get; set; } = "";
    public string FormaPago { get; set; } = "";

    public decimal Neto { get; set; }
    public decimal Iva { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }

    public string FechaTexto => Fecha.ToString("dd/MM/yyyy HH:mm");
    public string TotalTexto => Total.ToString("C0", Chile);
    public string NetoTexto => Neto.ToString("C0", Chile);
    public string IvaTexto => Iva.ToString("C0", Chile);
    public string DescuentoTexto => Descuento.ToString("C0", Chile);
}

public class DetalleVentaHistorial
{
    private static readonly CultureInfo Chile =
        CultureInfo.GetCultureInfo("es-CL");

    public string Producto { get; set; } = "";
    public string Sku { get; set; } = "";
    public string Lote { get; set; } = "";
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }

    public string PrecioTexto => PrecioUnitario.ToString("C0", Chile);
    public string SubtotalTexto => Subtotal.ToString("C0", Chile);
}
