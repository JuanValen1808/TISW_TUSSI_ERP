using TISW_TUSSI_ERP.Helpers;

namespace TISW_TUSSI_ERP.Models.Ventas;

// Modelos del módulo Ventas (RF-VEN-01, RF-VEN-02)
public class ProductoPos
{
    public int IdProducto { get; set; }
    public string Sku { get; set; } = "";
    public string CodigoBarras { get; set; } = "";
    public string NombreComercial { get; set; } = "";
    public string PrincipioActivo { get; set; } = "";
    public decimal PrecioVenta { get; set; }
    public string PrecioTexto => Formato.Moneda(PrecioVenta);
}

public class LotePos
{
    public int IdLote { get; set; }
    public string NumeroLote { get; set; } = "";
    public DateTime FechaVencimiento { get; set; }
    public int StockActual { get; set; }
    public string Descripcion =>
        $"{NumeroLote}  ·  vence {FechaVencimiento:dd/MM/yyyy}  ·  stock {StockActual}";
}

public class ItemCarrito
{
    public int IdProducto { get; set; }
    public int IdLote { get; set; }
    public string NombreProducto { get; set; } = "";
    public string NumeroLote { get; set; } = "";
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal => Cantidad * PrecioUnitario;
    public string SubtotalTexto => Formato.Moneda(Subtotal);
}

public class ClientePos
{
    public int IdCliente { get; set; }
    public string RutCliente { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string TipoCliente { get; set; } = "";
    public override string ToString() => $"{Nombre} ({RutCliente})";
}

public class ConvenioPos
{
    public int IdConvenio { get; set; }
    public string NombreConvenio { get; set; } = "";
    public decimal PorcentajeDescuento { get; set; }
    public int DiasCredito { get; set; }
    public override string ToString() => $"{NombreConvenio} (-{PorcentajeDescuento:N0}%)";
}

// Fila del Reporte 1: ranking de ventas por cliente y convenio (RF-VEN-04)
public class FilaRanking
{
    public string RutCliente { get; set; } = "";
    public string NombreCliente { get; set; } = "";
    public string TipoCliente { get; set; } = "";
    public string NombreConvenio { get; set; } = "";
    public int Documentos { get; set; }
    public decimal TotalVentas { get; set; }
    public decimal Porcentaje { get; set; }
    public string TotalTexto => Formato.Moneda(TotalVentas);
    public string PorcentajeTexto => $"{Porcentaje:N1}%";
}
