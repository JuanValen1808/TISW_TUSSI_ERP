
using System;
using TISW_TUSSI_ERP.Helpers;

namespace TISW_TUSSI_ERP.Models.Ventas;

// ======================================
// VENTA ORIGINAL PARA DEVOLUCIÓN
// ======================================

public class DevolucionVenta
{
    public int IdVentaOriginal { get; set; }

    public string FolioVenta { get; set; } = "";

    public string Cliente { get; set; } = "";
}

// ======================================
// PRODUCTOS DE LA VENTA
// ======================================

public class DetalleDevolucionVenta
{
    public int IdDetalleVenta { get; set; }

    public int IdProducto { get; set; }

    public int IdLote { get; set; }

    public string NombreProducto { get; set; } = "";

    public string NumeroLote { get; set; } = "";

    public int CantidadVendida { get; set; }

    public int CantidadDevueltaAnterior { get; set; }

    public int CantidadDisponible =>
        Math.Max(
            0,
            CantidadVendida - CantidadDevueltaAnterior);

    public decimal PrecioUnitario { get; set; }

    public int CantidadDevolver { get; set; }

    public string CondicionProducto { get; set; }
        = "CUARENTENA";

    public decimal SubtotalDevolucion =>
        Math.Round(
            PrecioUnitario * CantidadDevolver,
            2,
            MidpointRounding.AwayFromZero);

    public string PrecioUnitarioTexto =>
        Formato.Moneda(PrecioUnitario);

    public string SubtotalDevolucionTexto =>
        Formato.Moneda(SubtotalDevolucion);
}
