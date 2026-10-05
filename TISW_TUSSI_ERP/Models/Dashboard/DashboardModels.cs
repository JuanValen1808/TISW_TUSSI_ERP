using TISW_TUSSI_ERP.Helpers;

namespace TISW_TUSSI_ERP.Models.Dashboard;

public class DashboardData
{
    public DateTime Hoy { get; set; } = DateTime.Today;
    public ResumenDashboard Resumen { get; set; } = new();
    public List<BarraVenta> Barras { get; set; } = new();
    public List<MedioPago> MediosPago { get; set; } = new();
    public List<ProductoVendido> TopProductos { get; set; } = new();
    public List<AlertaStock> Alertas { get; set; } = new();
}

public class ResumenDashboard
{
    public decimal IngresosHoy { get; set; }
    public decimal IngresosAyer { get; set; }
    public int DocumentosHoy { get; set; }
    public decimal EfectivoHoy { get; set; }
    public int ProductosBajoMinimo { get; set; }
    public int ProductosSinStock { get; set; }
    public decimal PorCobrarMonto { get; set; }
    public int PorCobrarDocumentos { get; set; }
    public int VencidasDocumentos { get; set; }
    public decimal VencidasMonto { get; set; }

    public string IngresosHoyTexto => Formato.Moneda(IngresosHoy);
    public string IngresosAyerTexto => "Ayer: " + Formato.Moneda(IngresosAyer);
    public string VariacionTexto
    {
        get
        {
            if (IngresosAyer <= 0) return "Sin datos de ayer";
            var pct = (IngresosHoy - IngresosAyer) / IngresosAyer * 100m;
            var flecha = pct >= 0 ? "▲" : "▼";
            return flecha + " " + Math.Abs(pct).ToString("0.0", Formato.Cultura) + "% vs ayer";
        }
    }
    public Color VariacionColor => IngresosHoy >= IngresosAyer ? Paleta.Exito : Paleta.Peligro;
    public string TicketPromedioTexto => DocumentosHoy == 0
        ? "Sin documentos hoy"
        : "Ticket promedio " + Formato.Moneda(IngresosHoy / DocumentosHoy);
    public string StockCriticoTexto => ProductosSinStock > 0
        ? $"{ProductosSinStock} sin stock"
        : "Ninguno sin stock";
    public string PorCobrarTexto => Formato.Moneda(PorCobrarMonto);
    public string PorCobrarDetalle => $"{PorCobrarDocumentos} documentos · {VencidasDocumentos} vencidos";
    public string EfectivoTexto => Formato.Moneda(EfectivoHoy);
    public string OtrosMediosTexto => Formato.Moneda(IngresosHoy - EfectivoHoy);
}

public class BarraVenta
{
    public string Etiqueta { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public double Altura { get; set; }
    public bool EsHoy { get; set; }
    public string MontoCorto => Formato.MonedaCorta(Monto);
    public Color ColorBarra => EsHoy ? Paleta.Primario : Color.FromArgb("#6FA8EE");
}

public class MedioPago
{
    public string Nombre { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public double Porcentaje { get; set; }
    public Color Color { get; set; } = Paleta.Gris;
    public string PorcentajeTexto => Porcentaje.ToString("0", Formato.Cultura) + "%";
}

public class ProductoVendido
{
    public int Posicion { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Unidades { get; set; }
    public double Progreso { get; set; }
    public string UnidadesTexto => Unidades.ToString("N0", Formato.Cultura);
}

public class AlertaStock
{
    public string Sku { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Presentacion { get; set; } = string.Empty;
    public int Stock { get; set; }
    public int StockMinimo { get; set; }
    public string Lote { get; set; } = string.Empty;
    public DateTime? Vencimiento { get; set; }

    public string SkuTexto => $"{Sku} · {Presentacion}";
    public string StockTexto => $"{Stock} / {StockMinimo}";
    public double Proporcion => StockMinimo <= 0 ? 0 : Math.Clamp((double)Stock / StockMinimo, 0, 1);
    public Color ColorStock => Stock <= StockMinimo * 0.5 ? Paleta.Peligro : Paleta.Advertencia;

    private int? Dias => Vencimiento.HasValue ? (int)(Vencimiento.Value.Date - DateTime.Today).TotalDays : null;
    public string VencimientoTexto => Vencimiento.HasValue
        ? Vencimiento.Value.ToString("dd MMM yyyy", Formato.Cultura) : "—";
    public string DiasTexto => Dias.HasValue ? $"{Dias} días restantes" : "Sin stock en lote";
    public Color VenceFondo => Dias is null ? Color.FromArgb("#F3F4F6")
        : Dias <= 30 ? Color.FromArgb("#FEE2E2")
        : Dias <= 90 ? Color.FromArgb("#FEF3C7") : Color.FromArgb("#DCFCE7");
    public Color VenceColor => Dias is null ? Paleta.Gris
        : Dias <= 30 ? Color.FromArgb("#B91C1C")
        : Dias <= 90 ? Color.FromArgb("#B45309") : Color.FromArgb("#15803D");
}
