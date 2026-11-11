using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Contabilidad;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Contabilidad;

// RF-CON-05: Reporte de estado de resultados - Cálculo de la utilidad bruta y neta a partir de los ingresos por ventas, el costo de ventas y los gastos operativos.
public class EstadoResultadosViewModel : ReporteBaseViewModel
{
    private readonly ContabilidadService _contabilidadService;

    public ObservableCollection<LineaEstadoResultadosItem> Ingresos { get; } = new();
    public ObservableCollection<LineaEstadoResultadosItem> CostosVentas { get; } = new();
    public ObservableCollection<LineaEstadoResultadosItem> GastosOperacionales { get; } = new();
    public ObservableCollection<LineaEstadoResultadosItem> GastosOperativos => GastosOperacionales;
    public ObservableCollection<LineaEstadoResultadosItem> Gastos => GastosOperacionales;

    private DateTime _fechaDesde = new(DateTime.Today.Year, 1, 1);
    public DateTime FechaDesde { get => _fechaDesde; set => SetProperty(ref _fechaDesde, value); }
    public DateTime FechaInicio { get => FechaDesde; set { FechaDesde = value; OnPropertyChanged(); OnPropertyChanged(nameof(PeriodoTexto)); } }

    private DateTime _fechaHasta = DateTime.Today.AddMonths(2);
    public DateTime FechaHasta { get => _fechaHasta; set => SetProperty(ref _fechaHasta, value); }
    public DateTime FechaFin { get => FechaHasta; set { FechaHasta = value; OnPropertyChanged(); OnPropertyChanged(nameof(PeriodoTexto)); } }

    public string PeriodoTexto => Rango(FechaDesde, FechaHasta);

    private decimal _totalIngresos;
    public decimal TotalIngresos { get => _totalIngresos; set { if (SetProperty(ref _totalIngresos, value)) OnPropertyChanged(nameof(TotalIngresosTexto)); } }
    public string TotalIngresosTexto => Formato.Moneda(TotalIngresos);

    private decimal _totalCostosVentas;
    public decimal TotalCostosVentas { get => _totalCostosVentas; set { if (SetProperty(ref _totalCostosVentas, value)) OnPropertyChanged(nameof(TotalCostosVentasTexto)); } }
    public string TotalCostosVentasTexto => Formato.Moneda(TotalCostosVentas);

    // UTILIDAD BRUTA (MARGEN BRUTO) = Ingresos por Ventas - Costo de Ventas
    private decimal _utilidadBruta;
    public decimal UtilidadBruta { get => _utilidadBruta; set { if (SetProperty(ref _utilidadBruta, value)) OnPropertyChanged(nameof(UtilidadBrutaTexto)); } }
    public string UtilidadBrutaTexto => Formato.Moneda(UtilidadBruta);
    public string MargenBrutoPorcentajeTexto => TotalIngresos > 0 ? (UtilidadBruta / TotalIngresos).ToString("P1", Formato.Cultura) : "0,0%";

    private decimal _totalGastosOperacionales;
    public decimal TotalGastosOperacionales { get => _totalGastosOperacionales; set { if (SetProperty(ref _totalGastosOperacionales, value)) OnPropertyChanged(nameof(TotalGastosOperacionalesTexto)); } }
    public string TotalGastosOperacionalesTexto => Formato.Moneda(TotalGastosOperacionales);

    public decimal TotalGastos => TotalCostosVentas + TotalGastosOperacionales;
    public string TotalGastosTexto => Formato.Moneda(TotalGastos);

    // RESULTADO OPERACIONAL (Utilidad antes de Impuestos - RLI preliminar)
    private decimal _resultadoOperacional;
    public decimal ResultadoOperacional { get => _resultadoOperacional; set { if (SetProperty(ref _resultadoOperacional, value)) OnPropertyChanged(nameof(ResultadoOperacionalTexto)); } }
    public string ResultadoOperacionalTexto => Formato.Moneda(ResultadoOperacional);
    public string MargenOperacionalPorcentajeTexto => TotalIngresos > 0 ? (ResultadoOperacional / TotalIngresos).ToString("P1", Formato.Cultura) : "0,0%";

    // Impuesto de Primera Categoría SII Chile (Régimen Pro Pyme Art. 14 D N°3 LIR: 25% si hay utilidad)
    private decimal _impuestoRenta;
    public decimal ImpuestoRenta { get => _impuestoRenta; set { if (SetProperty(ref _impuestoRenta, value)) OnPropertyChanged(nameof(ImpuestoRentaTexto)); } }
    public string ImpuestoRentaTexto => Formato.Moneda(ImpuestoRenta);

    // UTILIDAD (O PÉRDIDA) NETA DEL EJERCICIO
    private decimal _resultadoEjercicio;
    public decimal ResultadoEjercicio { get => _resultadoEjercicio; set { if (SetProperty(ref _resultadoEjercicio, value)) OnPropertyChanged(nameof(ResultadoEjercicioTexto)); } }
    public string ResultadoEjercicioTexto => Formato.Moneda(ResultadoEjercicio);
    public string MargenNetoPorcentajeTexto => TotalIngresos > 0 ? (ResultadoEjercicio / TotalIngresos).ToString("P1", Formato.Cultura) : "0,0%";

    private string _textoResultado = "UTILIDAD NETA DEL EJERCICIO";
    public string TextoResultado { get => _textoResultado; set => SetProperty(ref _textoResultado, value); }

    private Color _colorResultado = Color.FromArgb("#047857");
    public Color ColorResultado { get => _colorResultado; set => SetProperty(ref _colorResultado, value); }

    public ICommand CargarEstadoResultadosCommand { get; }
    public ICommand RangoMesActualCommand { get; }
    public ICommand RangoTrimestreCommand { get; }
    public ICommand RangoAnioActualCommand { get; }
    public ICommand RangoTodoCommand { get; }

    // Alias de presentación para XAML
    public string TotalCostoVentasTexto => TotalCostosVentasTexto;
    public string MargenBrutoTexto => MargenBrutoPorcentajeTexto;
    public string TotalGastosOperativosTexto => TotalGastosOperacionalesTexto;
    public string ImpuestoRentaEstimadoTexto => ImpuestoRentaTexto;
    public string EstadoResultadoTexto => TextoResultado;
    public string UtilidadNetaTexto => ResultadoOperacionalTexto;
    public string MargenOperacionalTexto => MargenOperacionalPorcentajeTexto;
    public string UtilidadNetaDespuesImpuestosTexto => ResultadoEjercicioTexto;
    public string MargenNetoDespuesImpuestosTexto => MargenNetoPorcentajeTexto;
    public Color ColorUtilidad => ResultadoEjercicio >= 0 ? Color.FromArgb("#047857") : Color.FromArgb("#BE123C");
    public Color UtilidadFondo => ResultadoEjercicio >= 0 ? Color.FromArgb("#ECFDF5") : Color.FromArgb("#FEF2F2");

    public EstadoResultadosViewModel(ContabilidadService contabilidadService)
    {
        _contabilidadService = contabilidadService;
        Titulo = "Estado de Resultados Integral";
        CargarEstadoResultadosCommand = GenerarCommand;
        RangoMesActualCommand = new Command(async () => { FechaInicio = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1); FechaFin = DateTime.Today; await GenerarAsync(); });
        RangoTrimestreCommand = new Command(async () => { FechaInicio = DateTime.Today.AddMonths(-3); FechaFin = DateTime.Today; await GenerarAsync(); });
        RangoAnioActualCommand = new Command(async () => { FechaInicio = new DateTime(DateTime.Today.Year, 1, 1); FechaFin = DateTime.Today.AddMonths(1); await GenerarAsync(); });
        RangoTodoCommand = new Command(async () => { FechaInicio = new DateTime(2020, 1, 1); FechaFin = DateTime.Today.AddYears(1); await GenerarAsync(); });
    }

    protected override async Task CargarDatosAsync()
    {
        Ingresos.Clear();
        CostosVentas.Clear();
        GastosOperacionales.Clear();

        var datos = await _contabilidadService.ObtenerBalanceOchoColumnasAsync(FechaDesde, FechaHasta);
        var cuentasIngreso = datos.Where(x => x.TipoCuenta.Equals("INGRESO", StringComparison.OrdinalIgnoreCase) && x.Ganancia > 0).ToList();
        var cuentasGasto = datos.Where(x => x.TipoCuenta.Equals("GASTO", StringComparison.OrdinalIgnoreCase) && x.Perdida > 0).ToList();

        TotalIngresos = cuentasIngreso.Sum(x => x.Ganancia);

        foreach (var ing in cuentasIngreso)
        {
            Ingresos.Add(new LineaEstadoResultadosItem
            {
                CodigoCuenta = ing.CodigoCuenta,
                NombreCuenta = ing.NombreCuenta,
                CategoriaRubro = "INGRESO",
                Monto = ing.Ganancia,
                ParticipacionSobreVentas = TotalIngresos > 0 ? (double)(ing.Ganancia / TotalIngresos) : 0
            });
        }

        foreach (var gas in cuentasGasto)
        {
            bool esCostoVenta = gas.CodigoCuenta == "5.1.01" || gas.NombreCuenta.Contains("Costo de Venta", StringComparison.OrdinalIgnoreCase);
            var item = new LineaEstadoResultadosItem
            {
                CodigoCuenta = gas.CodigoCuenta,
                NombreCuenta = gas.NombreCuenta,
                CategoriaRubro = esCostoVenta ? "COSTO_VENTA" : "GASTO_OPERACIONAL",
                Monto = gas.Perdida,
                ParticipacionSobreVentas = TotalIngresos > 0 ? (double)(gas.Perdida / TotalIngresos) : 0
            };

            if (esCostoVenta)
                CostosVentas.Add(item);
            else
                GastosOperacionales.Add(item);
        }

        TotalCostosVentas = CostosVentas.Sum(x => x.Monto);
        UtilidadBruta = TotalIngresos - TotalCostosVentas;
        TotalGastosOperacionales = GastosOperacionales.Sum(x => x.Monto);
        ResultadoOperacional = UtilidadBruta - TotalGastosOperacionales;
        ImpuestoRenta = ResultadoOperacional > 0 ? Math.Round(ResultadoOperacional * 0.25m, 0) : 0m;
        ResultadoEjercicio = ResultadoOperacional - ImpuestoRenta;

        if (ResultadoEjercicio >= 0)
        {
            TextoResultado = "UTILIDAD NETA DEL EJERCICIO (DESPUÉS DE IMPUESTO 1ª CAT.)";
            ColorResultado = Color.FromArgb("#34D399");
        }
        else
        {
            TextoResultado = "PÉRDIDA NETA DEL EJERCICIO";
            ColorResultado = Color.FromArgb("#FCA5A5");
        }

        OnPropertyChanged(nameof(TotalGastos));
        OnPropertyChanged(nameof(TotalGastosTexto));
        OnPropertyChanged(nameof(MargenBrutoPorcentajeTexto));
        OnPropertyChanged(nameof(MargenOperacionalPorcentajeTexto));
        OnPropertyChanged(nameof(MargenNetoPorcentajeTexto));
        foreach (var p in new[] { nameof(TotalCostoVentasTexto), nameof(MargenBrutoTexto), nameof(TotalGastosOperativosTexto),
                                  nameof(ImpuestoRentaEstimadoTexto), nameof(EstadoResultadoTexto), nameof(UtilidadNetaTexto),
                                  nameof(MargenOperacionalTexto), nameof(UtilidadNetaDespuesImpuestosTexto),
                                  nameof(MargenNetoDespuesImpuestosTexto), nameof(ColorUtilidad), nameof(UtilidadFondo), nameof(PeriodoTexto) })
            OnPropertyChanged(p);
        HayDatos = Ingresos.Count + CostosVentas.Count + GastosOperacionales.Count > 0;
    }

    protected override DocumentoReporte ConstruirDocumento()
    {
        var doc = new DocumentoReporte
        {
            Titulo = "Estado de Resultados Integral (IFRS Pyme / SII Chile)",
            Subtitulo = $"Determinación de Utilidad Bruta, Resultado Operacional y Utilidad Neta · {Rango(FechaDesde, FechaHasta)}"
        };
        doc.Filtros.Add(("Período", Rango(FechaDesde, FechaHasta)));
        doc.Kpis.Add(("Ingresos por Ventas", TotalIngresosTexto));
        doc.Kpis.Add(("Costo de Ventas", TotalCostosVentasTexto));
        doc.Kpis.Add(("Utilidad Bruta", $"{UtilidadBrutaTexto} ({MargenBrutoPorcentajeTexto})"));
        doc.Kpis.Add(("Gastos Operativos", TotalGastosOperacionalesTexto));
        doc.Kpis.Add(("Resultado Operacional", ResultadoOperacionalTexto));
        doc.Kpis.Add(("Utilidad Neta", $"{ResultadoEjercicioTexto} ({MargenNetoPorcentajeTexto})"));

        var s = new SeccionReporte("Estado de Resultados",
            new ColumnaReporte("Código"),
            new ColumnaReporte("Rubro / Cuenta Contable"),
            new ColumnaReporte("Monto CLP", TipoColumna.Moneda),
            new ColumnaReporte("% s/ Ventas", TipoColumna.Porcentaje));

        s.Subtitulo("1. INGRESOS DE ACTIVIDADES ORDINARIAS (VENTAS NETAS)");
        foreach (var i in Ingresos)
            s.Fila(i.CodigoCuenta, i.NombreCuenta, i.Monto, i.ParticipacionSobreVentas);
        s.Total("", "(+) TOTAL INGRESOS OPERACIONALES", TotalIngresos, 1d);

        s.Subtitulo("2. COSTO DE VENTAS (MERCADERÍAS VENDIDAS)");
        foreach (var c in CostosVentas)
            s.Fila(c.CodigoCuenta, c.NombreCuenta, -c.Monto, c.ParticipacionSobreVentas);
        s.Total("", "(=) UTILIDAD BRUTA (MARGEN DE CONTRIBUCIÓN)", UtilidadBruta, TotalIngresos > 0 ? (double)(UtilidadBruta / TotalIngresos) : 0d);

        s.Subtitulo("3. GASTOS DE ADMINISTRACIÓN, VENTAS Y MERMAS");
        foreach (var g in GastosOperacionales)
            s.Fila(g.CodigoCuenta, g.NombreCuenta, -g.Monto, g.ParticipacionSobreVentas);
        s.Total("", "(=) RESULTADO OPERACIONAL (ANTES DE IMPUESTOS)", ResultadoOperacional, TotalIngresos > 0 ? (double)(ResultadoOperacional / TotalIngresos) : 0d);

        s.Fila("2.1.03", "(-) Provisión Impuesto a la Renta 1ª Categoría (25% Régimen Pro Pyme Art. 14 D)", -ImpuestoRenta, TotalIngresos > 0 ? (double)(ImpuestoRenta / TotalIngresos) : 0d);
        s.Total("", $"(=) {TextoResultado}", ResultadoEjercicio, TotalIngresos > 0 ? (double)(ResultadoEjercicio / TotalIngresos) : 0d);

        doc.Secciones.Add(s);
        return doc;
    }
}
