using TISW_TUSSI_ERP.Helpers;

namespace TISW_TUSSI_ERP.Models.Compras;

public class ResumenCompras
{
    public int OrdenesPendientes { get; set; }
    public int RecibidasParciales { get; set; }
    public decimal MontoPorRecibir { get; set; }
    public int Proveedores { get; set; }
    public decimal TotalComprometido { get; set; }

    public string MontoPorRecibirTexto => Formato.Moneda(MontoPorRecibir);
    public string TotalComprometidoTexto => Formato.Moneda(TotalComprometido);
}

public class OrdenCompra
{
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public string Estado { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal Iva { get; set; }
    public decimal Total { get; set; }
    public string Proveedor { get; set; } = string.Empty;
    public string RutProveedor { get; set; } = string.Empty;
    public string Contacto { get; set; } = string.Empty;
    public string Creador { get; set; } = string.Empty;

    public string FechaTexto => FechaEmision.ToString("dd MMM yyyy", Formato.Cultura);
    public string TotalTexto => Formato.Moneda(Total);
    public string SubtotalTexto => Formato.Moneda(Subtotal);
    public string IvaTexto => Formato.Moneda(Iva);
    public string RutTexto => "RUT " + RutProveedor;

    public string EstadoTexto => Estado switch
    {
        "BORRADOR" => "Borrador",
        "EMITIDA" => "Pendiente",
        "PARCIAL" => "Recibida Parcial",
        "RECIBIDA" => "Liquidada",
        "ANULADA" => "Anulada",
        _ => Estado
    };
    public Color EstadoFondo => Estado switch
    {
        "EMITIDA" => Color.FromArgb("#FEF3C7"),
        "PARCIAL" => Color.FromArgb("#DBEAFE"),
        "RECIBIDA" => Color.FromArgb("#DCFCE7"),
        "ANULADA" => Color.FromArgb("#FEE2E2"),
        _ => Color.FromArgb("#F3F4F6")
    };
    public Color EstadoColor => Estado switch
    {
        "EMITIDA" => Color.FromArgb("#B45309"),
        "PARCIAL" => Color.FromArgb("#1D4ED8"),
        "RECIBIDA" => Color.FromArgb("#15803D"),
        "ANULADA" => Color.FromArgb("#B91C1C"),
        _ => Paleta.Gris
    };
}

public class DetalleOrdenCompra
{
    public string Sku { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int Pedido { get; set; }
    public int Recibido { get; set; }
    public decimal Costo { get; set; }

    public int Pendiente => Math.Max(Pedido - Recibido, 0);
    public decimal Subtotal => Pedido * Costo;
    public string CostoTexto => Formato.Moneda(Costo);
    public string SubtotalTexto => Formato.Moneda(Subtotal);
    public Color RecibidoColor => Recibido > 0 ? Paleta.Exito : Paleta.Gris;
    public Color PendienteColor => Pendiente > 0 ? Paleta.Advertencia : Paleta.Gris;
}
