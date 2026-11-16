
using System;
using TISW_TUSSI_ERP.Helpers;

namespace TISW_TUSSI_ERP.Models.Ventas;

public class VentaParaDevolucion
{
    public int IdVenta { get; set; }

    public string Folio { get; set; } = "";

    public DateTime Fecha { get; set; }

    public string Cliente { get; set; } = "";

    public string Rut { get; set; } = "";

    public decimal Total { get; set; }

    public string FechaTexto =>
        Fecha.ToString("dd-MM-yyyy HH:mm");

    public string RutTexto =>
        string.IsNullOrWhiteSpace(Rut)
            ? "Sin RUT"
            : Rut;

    public string TotalTexto =>
        Formato.Moneda(Total);
}
