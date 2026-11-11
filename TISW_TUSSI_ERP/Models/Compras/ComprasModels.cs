using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.ViewModels;

namespace TISW_TUSSI_ERP.Models.Compras;

public class ResumenCompras
{
    public int OrdenesPendientes { get; set; }
    public int RecibidasParciales { get; set; }
    public decimal MontoPorRecibir { get; set; }
    public int Proveedores { get; set; }
    public decimal TotalComprometido { get; set; }
    public int OrdenesPendientesAutorizacion { get; set; }

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
    public int IdProveedor { get; set; }
    public string Proveedor { get; set; } = string.Empty;
    public string RutProveedor { get; set; } = string.Empty;
    public string Contacto { get; set; } = string.Empty;
    public int IdUsuarioCreador { get; set; }
    public string Creador { get; set; } = string.Empty;
    public string? MotivoRechazo { get; set; }

    public string FechaTexto => FechaEmision.ToString("dd MMM yyyy", Formato.Cultura);
    public string TotalTexto => Formato.Moneda(Total);
    public string SubtotalTexto => Formato.Moneda(Subtotal);
    public string IvaTexto => Formato.Moneda(Iva);
    public string RutTexto => "RUT " + RutProveedor;

    public bool RequiereAutorizacion => Estado == "PENDIENTE_AUTORIZACION";
    public bool PuedeRecibirse => Estado is "EMITIDA" or "PARCIAL";
    public bool PuedeAnularse => Estado is "BORRADOR" or "EMITIDA" or "PENDIENTE_AUTORIZACION";

    public string EstadoTexto => Estado switch
    {
        "BORRADOR" => "Borrador",
        "EMITIDA" => "Pendiente",
        "PARCIAL" => "Recibida Parcial",
        "RECIBIDA" => "Liquidada",
        "ANULADA" => "Anulada",
        "PENDIENTE_AUTORIZACION" => "Pend. Autorización",
        _ => Estado
    };

    public Color EstadoFondo => Estado switch
    {
        "EMITIDA" => Color.FromArgb("#FEF3C7"),
        "PARCIAL" => Color.FromArgb("#DBEAFE"),
        "RECIBIDA" => Color.FromArgb("#DCFCE7"),
        "ANULADA" => Color.FromArgb("#FEE2E2"),
        "PENDIENTE_AUTORIZACION" => Color.FromArgb("#FFEDD5"),
        _ => Color.FromArgb("#F3F4F6")
    };

    public Color EstadoColor => Estado switch
    {
        "EMITIDA" => Color.FromArgb("#B45309"),
        "PARCIAL" => Color.FromArgb("#1D4ED8"),
        "RECIBIDA" => Color.FromArgb("#15803D"),
        "ANULADA" => Color.FromArgb("#B91C1C"),
        "PENDIENTE_AUTORIZACION" => Color.FromArgb("#C2410C"),
        _ => Paleta.Gris
    };
}

public class DetalleOrdenCompra
{
    public int IdDetalleOc { get; set; }
    public int IdProducto { get; set; }
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

// ==========================================================
// MODELOS PARA RECEPCIÓN Y LIQUIDACIÓN DE FACTURAS
// ==========================================================

public class LineaRecepcionFacturaItem : BaseViewModel
{
    public int IdDetalleOc { get; set; }
    public int IdProducto { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string NombreProducto { get; set; } = string.Empty;
    public int CantidadSolicitada { get; set; }
    public int CantidadPreviaRecibida { get; set; }
    public int CantidadPendiente => Math.Max(0, CantidadSolicitada - CantidadPreviaRecibida);
    public decimal CostoPactado { get; set; }

    private int _cantidadARecibir;
    public int CantidadARecibir
    {
        get => _cantidadARecibir;
        set
        {
            if (SetProperty(ref _cantidadARecibir, value))
            {
                OnPropertyChanged(nameof(SubtotalLinea));
                OnPropertyChanged(nameof(SubtotalLineaTexto));
            }
        }
    }

    private string _numeroLote = string.Empty;
    public string NumeroLote
    {
        get => _numeroLote;
        set => SetProperty(ref _numeroLote, value);
    }

    private DateTime _fechaVencimiento = DateTime.Today.AddYears(2);
    public DateTime FechaVencimiento
    {
        get => _fechaVencimiento;
        set => SetProperty(ref _fechaVencimiento, value);
    }

    private decimal _costoFacturado;
    public decimal CostoFacturado
    {
        get => _costoFacturado;
        set
        {
            if (SetProperty(ref _costoFacturado, value))
            {
                OnPropertyChanged(nameof(SubtotalLinea));
                OnPropertyChanged(nameof(SubtotalLineaTexto));
            }
        }
    }

    private bool _enCuarentena;
    public bool EnCuarentena
    {
        get => _enCuarentena;
        set => SetProperty(ref _enCuarentena, value);
    }

    public decimal SubtotalLinea => CantidadARecibir * CostoFacturado;
    public string SubtotalLineaTexto => Formato.Moneda(SubtotalLinea);
    public string CostoPactadoTexto => Formato.Moneda(CostoPactado);
}

public class RecepcionFacturaInput
{
    public int IdOrdenCompra { get; set; }
    public int IdProveedor { get; set; }
    public string NumeroFactura { get; set; } = string.Empty;
    public DateTime FechaFactura { get; set; } = DateTime.Today;
    public decimal MontoTotalFacturado { get; set; }
    public List<LineaRecepcionFacturaItem> Lineas { get; set; } = new();
}

// ==========================================================
// SUGERENCIA AUTOMATIZADA DE REABASTECIMIENTO
// ==========================================================

public class SugerenciaReabastecimientoItem
{
    public int IdProducto { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string NombreProducto { get; set; } = string.Empty;
    public int PuntoReorden { get; set; }
    public int StockMinimo { get; set; }
    public int StockActual { get; set; }
    public int CantidadSugerida { get; set; }
    public decimal CostoEstimado { get; set; }
    public decimal SubtotalEstimado => CantidadSugerida * CostoEstimado;

    public string StockActualTexto => $"{StockActual} un.";
    public string PuntoReordenTexto => $"{PuntoReorden} un.";
    public string CantidadSugeridaTexto => $"{CantidadSugerida} un.";
    public string CostoEstimadoTexto => Formato.Moneda(CostoEstimado);
    public string SubtotalEstimadoTexto => Formato.Moneda(SubtotalEstimado);
}

// ==========================================================
// REPORTE 2: CUMPLIMIENTO DE ÓRDENES Y NIVEL DE SERVICIO
// ==========================================================

public class ItemCumplimientoOrden
{
    public int IdOrdenCompra { get; set; }
    public string NumeroOc { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public string Proveedor { get; set; } = string.Empty;
    public string RutProveedor { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public int TotalSolicitado { get; set; }
    public int TotalRecibido { get; set; }
    public int Discrepancia => Math.Max(0, TotalSolicitado - TotalRecibido);
    public decimal MontoTotal { get; set; }
    public DateTime? FechaUltimaRecepcion { get; set; }

    public double PorcentajeCumplimiento => TotalSolicitado == 0 ? 1.0 : (double)TotalRecibido / TotalSolicitado;
    public string PorcentajeTexto => $"{PorcentajeCumplimiento * 100:0.0}%";
    public string MontoTotalTexto => Formato.Moneda(MontoTotal);
    public string FechaEmisionTexto => FechaEmision.ToString("dd/MM/yyyy", Formato.Cultura);
    public string FechaRecepcionTexto => FechaUltimaRecepcion.HasValue ? FechaUltimaRecepcion.Value.ToString("dd/MM/yyyy", Formato.Cultura) : "—";

    public string NivelServicio => PorcentajeCumplimiento >= 0.99 ? "Excelente (100%)"
        : PorcentajeCumplimiento >= 0.7 ? "Aceptable"
        : "Crítico / Retraso";

    public Color NivelServicioColor => PorcentajeCumplimiento >= 0.99 ? Paleta.Exito
        : PorcentajeCumplimiento >= 0.7 ? Paleta.Advertencia
        : Paleta.Peligro;

    public Color NivelServicioFondo => PorcentajeCumplimiento >= 0.99 
        ? Color.FromArgb("#DCFCE7") 
        : (PorcentajeCumplimiento >= 0.7 
            ? Color.FromArgb("#FEF3C7") 
            : Color.FromArgb("#FEE2E2"));

    public Color NivelServicioBorde => PorcentajeCumplimiento >= 0.99 
        ? Color.FromArgb("#86EFAC") 
        : (PorcentajeCumplimiento >= 0.7 
            ? Color.FromArgb("#FDE68A") 
            : Color.FromArgb("#FCA5A5"));

    public Color NivelServicioTextoColor => PorcentajeCumplimiento >= 0.99 
        ? Color.FromArgb("#15803D") 
        : (PorcentajeCumplimiento >= 0.7 
            ? Color.FromArgb("#B45309") 
            : Color.FromArgb("#B91C1C"));
}

public class ResumenCumplimientoOrdenes
{
    public int TotalOrdenes { get; set; }
    public int Liquidadas { get; set; }
    public int Parciales { get; set; }
    public int Pendientes { get; set; }
    public int TotalUnidadesPedidas { get; set; }
    public int TotalUnidadesRecibidas { get; set; }
    public double TasaCumplimientoGlobal => TotalUnidadesPedidas == 0 ? 100.0 : ((double)TotalUnidadesRecibidas / TotalUnidadesPedidas) * 100.0;
    public string TasaCumplimientoTexto => $"{TasaCumplimientoGlobal:0.0}%";
}

// ==========================================================
// REPORTE 3: HISTORIAL DE PRECIOS Y COSTOS DE ADQUISICIÓN
// ==========================================================

public class ItemHistorialPrecio
{
    public DateTime Fecha { get; set; }
    public string Documento { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string NombreProducto { get; set; } = string.Empty;
    public string Proveedor { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal? CostoAnterior { get; set; }

    public decimal VariacionMonto => CostoAnterior.HasValue ? CostoUnitario - CostoAnterior.Value : 0m;
    public double VariacionPorcentaje => CostoAnterior.HasValue && CostoAnterior.Value > 0
        ? (double)((CostoUnitario - CostoAnterior.Value) / CostoAnterior.Value) * 100.0
        : 0.0;

    public string FechaTexto => Fecha.ToString("dd/MM/yyyy", Formato.Cultura);
    public string CostoUnitarioTexto => Formato.Moneda(CostoUnitario);
    public string CostoAnteriorTexto => CostoAnterior.HasValue ? Formato.Moneda(CostoAnterior.Value) : "—";
    public string VariacionTexto => CostoAnterior.HasValue ? $"{VariacionPorcentaje:+0.0;-0.0;0.0}% ({Formato.Moneda(VariacionMonto)})" : "Base inicial";

    public Color VariacionColor => VariacionMonto > 0 ? Paleta.Peligro : (VariacionMonto < 0 ? Paleta.Exito : Paleta.Gris);

    public string IndicadorTendencia => VariacionMonto > 0 ? "Alza" : (VariacionMonto < 0 ? "Baja" : "Estable");

    public Color TendenciaFondo => VariacionMonto > 0 
        ? Color.FromArgb("#FEE2E2") 
        : (VariacionMonto < 0 
            ? Color.FromArgb("#DCFCE7") 
            : Color.FromArgb("#F1F5F9"));

    public Color TendenciaBorde => VariacionMonto > 0 
        ? Color.FromArgb("#FCA5A5") 
        : (VariacionMonto < 0 
            ? Color.FromArgb("#86EFAC") 
            : Color.FromArgb("#CBD5E1"));

    public Color TendenciaTextoColor => VariacionMonto > 0 
        ? Color.FromArgb("#B91C1C") 
        : (VariacionMonto < 0 
            ? Color.FromArgb("#15803D") 
            : Color.FromArgb("#475569"));
}

public class ResumenHistorialPrecios
{
    public int TotalRegistros { get; set; }
    public decimal CostoPromedio { get; set; }
    public decimal CostoMinimo { get; set; }
    public decimal CostoMaximo { get; set; }
    public int TotalAlzas { get; set; }

    public string CostoPromedioTexto => Formato.Moneda(CostoPromedio);
    public string CostoMinimoTexto => Formato.Moneda(CostoMinimo);
    public string CostoMaximoTexto => Formato.Moneda(CostoMaximo);
}
