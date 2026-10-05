using TISW_TUSSI_ERP.Helpers;

namespace TISW_TUSSI_ERP.Models.Compras;

// Fila del reporte "Compras por Proveedor" (RF-COM-04 / RF-COM-05)
public class CompraPorProveedor
{
    public string Proveedor { get; set; } = string.Empty;
    public string Rut { get; set; } = string.Empty;
    public int NumeroOrdenes { get; set; }
    public int OrdenesPendientes { get; set; }
    public int OrdenesLiquidadas { get; set; }
    public decimal MontoTotal { get; set; }
    public decimal MontoPorRecibir { get; set; }
    public DateTime? UltimaCompra { get; set; }

    public string MontoTotalTexto => Formato.Moneda(MontoTotal);
    public string MontoPorRecibirTexto => Formato.Moneda(MontoPorRecibir);
    public string UltimaCompraTexto => UltimaCompra.HasValue
        ? UltimaCompra.Value.ToString("dd MMM yyyy", Formato.Cultura) : "Sin compras";
    public string CumplimientoTexto => NumeroOrdenes == 0
        ? "—" : $"{OrdenesLiquidadas}/{NumeroOrdenes} liquidadas";
    public double Cumplimiento => NumeroOrdenes == 0 ? 0 : (double)OrdenesLiquidadas / NumeroOrdenes;
    public Color CumplimientoColor => Cumplimiento >= 0.8 ? Paleta.Exito
        : Cumplimiento >= 0.4 ? Paleta.Advertencia : Paleta.Peligro;
}

public class ResumenReporteCompras
{
    public int ProveedoresConCompras { get; set; }
    public int TotalOrdenes { get; set; }
    public decimal MontoTotalPeriodo { get; set; }
    public decimal MontoPorRecibir { get; set; }

    public string MontoTotalPeriodoTexto => Formato.Moneda(MontoTotalPeriodo);
    public string MontoPorRecibirTexto => Formato.Moneda(MontoPorRecibir);
}
