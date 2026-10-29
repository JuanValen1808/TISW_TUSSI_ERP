using System.Collections.ObjectModel;
using System.Windows.Input;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Contabilidad;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Contabilidad;

// REPORTE 1: Libro Diario detallado (asientos con sus líneas Debe / Haber y control de cuadratura)
public class LibroDiarioDetalladoViewModel : ReporteBaseViewModel
{
    private readonly ReportesContablesService _servicio;
    private List<AsientoDetallado> _todos = new();

    public ObservableCollection<AsientoDetallado> Asientos { get; } = new();
    public List<string> Origenes { get; } = new() { "Todos", "VENTA", "COMPRA", "RECEPCION", "AJUSTE", "OTRO" };

    private DateTime _fechaDesde = new(DateTime.Today.Year, 1, 1);
    public DateTime FechaDesde { get => _fechaDesde; set => SetProperty(ref _fechaDesde, value); }

    private DateTime _fechaHasta = DateTime.Today;
    public DateTime FechaHasta { get => _fechaHasta; set => SetProperty(ref _fechaHasta, value); }

    private string _origenSeleccionado = "Todos";
    public string OrigenSeleccionado { get => _origenSeleccionado; set => SetProperty(ref _origenSeleccionado, value); }

    private string _busqueda = string.Empty;
    public string Busqueda
    {
        get => _busqueda;
        set { if (SetProperty(ref _busqueda, value)) AplicarFiltro(); }
    }

    // KPIs
    private int _cantidadAsientos;
    public int CantidadAsientos { get => _cantidadAsientos; set => SetProperty(ref _cantidadAsientos, value); }
    private string _totalDebeTexto = "$0";
    public string TotalDebeTexto { get => _totalDebeTexto; set => SetProperty(ref _totalDebeTexto, value); }
    private string _totalHaberTexto = "$0";
    public string TotalHaberTexto { get => _totalHaberTexto; set => SetProperty(ref _totalHaberTexto, value); }
    private string _cuadraturaTexto = "–";
    public string CuadraturaTexto { get => _cuadraturaTexto; set => SetProperty(ref _cuadraturaTexto, value); }
    private string _cuadraturaDetalle = string.Empty;
    public string CuadraturaDetalle { get => _cuadraturaDetalle; set => SetProperty(ref _cuadraturaDetalle, value); }
    private Color _cuadraturaColor = Color.FromArgb("#111827");
    public Color CuadraturaColor { get => _cuadraturaColor; set => SetProperty(ref _cuadraturaColor, value); }

    public string PeriodoTexto => Rango(FechaDesde, FechaHasta);

    public LibroDiarioDetalladoViewModel(ReportesContablesService servicio)
    {
        _servicio = servicio;
        Titulo = "Libro Diario Detallado";
    }

    protected override async Task CargarDatosAsync()
    {
        if (FechaHasta < FechaDesde)
            throw new InvalidOperationException("La fecha 'Hasta' no puede ser anterior a 'Desde'.");

        var origen = OrigenSeleccionado == "Todos" ? null : OrigenSeleccionado;
        _todos = await _servicio.ObtenerLibroDiarioDetalladoAsync(FechaDesde, FechaHasta, origen);
        OnPropertyChanged(nameof(PeriodoTexto));
        AplicarFiltro();
    }

    private void AplicarFiltro()
    {
        var texto = Busqueda.Trim();
        var filtrados = string.IsNullOrEmpty(texto)
            ? _todos
            : _todos.Where(a => a.Glosa.Contains(texto, StringComparison.OrdinalIgnoreCase)
                             || a.NumeroAsiento.ToString().Contains(texto)
                             || a.Lineas.Any(l => l.NombreCuenta.Contains(texto, StringComparison.OrdinalIgnoreCase)
                                               || l.CodigoCuenta.Contains(texto))).ToList();

        Asientos.Clear();
        foreach (var a in filtrados) Asientos.Add(a);

        var debe = filtrados.Sum(a => a.TotalDebe);
        var haber = filtrados.Sum(a => a.TotalHaber);
        var cuadrados = filtrados.Count(a => a.Cuadrado);

        CantidadAsientos = filtrados.Count;
        TotalDebeTexto = Formato.Moneda(debe);
        TotalHaberTexto = Formato.Moneda(haber);
        CuadraturaTexto = filtrados.Count == 0 ? "–" : debe == haber ? "Cuadrado" : "Descuadrado";
        CuadraturaColor = filtrados.Count == 0 ? Color.FromArgb("#111827") : debe == haber ? Color.FromArgb("#047857") : Color.FromArgb("#BE123C");
        CuadraturaDetalle = $"{cuadrados} de {filtrados.Count} asientos cuadrados";
        HayDatos = filtrados.Count > 0;
    }

    protected override DocumentoReporte ConstruirDocumento()
    {
        var doc = new DocumentoReporte
        {
            Titulo = "Libro Diario Detallado",
            Subtitulo = $"Registro cronológico de asientos con su detalle contable · Período {PeriodoTexto}"
        };
        doc.Filtros.Add(("Período", PeriodoTexto));
        doc.Filtros.Add(("Origen", OrigenSeleccionado));
        if (!string.IsNullOrWhiteSpace(Busqueda)) doc.Filtros.Add(("Búsqueda", Busqueda));
        doc.Kpis.Add(("Asientos", CantidadAsientos.ToString()));
        doc.Kpis.Add(("Total Debe", TotalDebeTexto));
        doc.Kpis.Add(("Total Haber", TotalHaberTexto));
        doc.Kpis.Add(("Cuadratura", $"{CuadraturaTexto} ({CuadraturaDetalle})"));

        var s = new SeccionReporte("Detalle de asientos",
            new ColumnaReporte("Fecha", TipoColumna.Fecha), new ColumnaReporte("N°"), new ColumnaReporte("Código"),
            new ColumnaReporte("Cuenta / Glosa"), new ColumnaReporte("Debe", TipoColumna.Moneda), new ColumnaReporte("Haber", TipoColumna.Moneda));

        foreach (var a in Asientos)
        {
            s.Subtitulo($"Asiento N° {a.NumeroAsiento} · {a.Fecha:dd/MM/yyyy} · {a.Origen} · {a.GlosaTexto}");
            foreach (var l in a.Lineas)
                s.Filas.Add(new FilaReporte { Celdas = new object?[] { a.Fecha, a.NumeroAsiento.ToString(), l.CodigoCuenta, l.EsHaber ? "a " + l.NombreCuenta : l.NombreCuenta, l.Debe, l.Haber }, Nivel = l.EsHaber ? 1 : 0 });
            s.Total("", "", "", $"Total asiento ({a.CuadraturaTexto.ToLower()})", a.TotalDebe, a.TotalHaber);
        }
        s.Total("", "", "", "TOTALES DEL PERÍODO", Asientos.Sum(a => a.TotalDebe), Asientos.Sum(a => a.TotalHaber));
        doc.Secciones.Add(s);
        return doc;
    }
}
