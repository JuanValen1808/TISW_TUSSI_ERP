
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TISW_TUSSI_ERP.Models.Ventas;

namespace TISW_TUSSI_ERP.Helpers;

public static class ComprobantePos
{
    private static readonly CultureInfo Cultura =
        CultureInfo.GetCultureInfo("es-CL");

    private static string Dinero(decimal monto) =>
        monto.ToString("C0", Cultura);

    public static string Generar(
        string folio,
        DateTime fecha,
        string cliente,
        string formaPago,
        IEnumerable<ItemCarrito> productos,
        decimal subtotal,
        decimal descuento,
        decimal neto,
        decimal iva,
        decimal total,
        string motivoDescuento)
    {
        var sb = new StringBuilder();

        sb.AppendLine("          FARMSALUD ERP");
        sb.AppendLine("       COMPROBANTE DE VENTA");
        sb.AppendLine("--------------------------------");
        sb.AppendLine($"Folio: {folio}");
        sb.AppendLine($"Fecha: {fecha:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Cliente: {cliente}");
        sb.AppendLine($"Pago: {formaPago}");
        sb.AppendLine("--------------------------------");
        sb.AppendLine("PRODUCTOS");

        foreach (var item in productos)
        {
            sb.AppendLine(item.NombreProducto);
            sb.AppendLine(
                $"{item.Cantidad} x {Dinero(item.PrecioUnitario)}" +
                $" = {Dinero(item.Subtotal)}");
        }

        sb.AppendLine("--------------------------------");
        sb.AppendLine($"Subtotal: {Dinero(subtotal)}");

        if (descuento > 0)
        {
            sb.AppendLine($"Descuento: -{Dinero(descuento)}");
            sb.AppendLine($"Motivo: {motivoDescuento}");
        }

        sb.AppendLine($"Neto: {Dinero(neto)}");
        sb.AppendLine($"IVA incluido (19%): {Dinero(iva)}");
        sb.AppendLine($"TOTAL: {Dinero(total)}");
        sb.AppendLine("--------------------------------");
        sb.AppendLine("Gracias por su compra.");
        sb.AppendLine("Comprobante interno - No válido");
        sb.AppendLine("como documento tributario SII.");

        return sb.ToString();
    }
}
