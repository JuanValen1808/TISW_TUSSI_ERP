namespace TISW_TUSSI_ERP.Models.Inventario;

public class UbicacionModel
{
    public int IdUbicacion { get; set; }
    public string TipoZona { get; set; } = "BODEGA"; // BODEGA | SALA_VENTAS
    public string CodigoUbicacion { get; set; } = string.Empty;
    public string SectorPasillo { get; set; } = string.Empty;
    public string EstanteMueble { get; set; } = string.Empty;
    public string RepisaNivel { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    public string RutaCompleta => string.IsNullOrWhiteSpace(RepisaNivel)
        ? $"{SectorPasillo} › {EstanteMueble}"
        : $"{SectorPasillo} › {EstanteMueble} › {RepisaNivel}";

    public string DisplayPicker => $"[{CodigoUbicacion}] {RutaCompleta}";
}

public class LoteUbicacionDetalleModel
{
    public int IdLoteUbicacion { get; set; }
    public int IdLote { get; set; }
    public int IdUbicacion { get; set; }
    public string CodigoUbicacion { get; set; } = string.Empty;
    public string RutaCompleta { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public DateTime FechaAsignacion { get; set; }
    public string UsuarioResponsable { get; set; } = "Sistema";

    public string FechaTexto => FechaAsignacion.ToString("dd/MM/yyyy HH:mm");
}

public class LoteBodegaItemModel
{
    public int IdLote { get; set; }
    public int IdProducto { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string CodigoBarras { get; set; } = string.Empty;
    public string NombreProducto { get; set; } = string.Empty;
    public string NumeroLote { get; set; } = string.Empty;
    public DateTime FechaVencimiento { get; set; }
    public int StockTotalLote { get; set; }
    public int StockUbicadoBodega { get; set; }
    public int StockUbicadoSala { get; set; }

    public int StockSinUbicar => Math.Max(0, StockTotalLote - (StockUbicadoBodega + StockUbicadoSala));
    public string VencimientoTexto => FechaVencimiento.ToString("dd/MM/yyyy");
    public bool TieneSinUbicar => StockSinUbicar > 0;
    public string EstadoUbicacionTexto => StockSinUbicar == 0 ? "100% Ubicado" : $"{StockSinUbicar} un. sin ubicar";
    public string ColorEstado => StockSinUbicar == 0 ? "#15803D" : "#D97706";
    public string FondoEstado => StockSinUbicar == 0 ? "#DCFCE7" : "#FEF3C7";
}