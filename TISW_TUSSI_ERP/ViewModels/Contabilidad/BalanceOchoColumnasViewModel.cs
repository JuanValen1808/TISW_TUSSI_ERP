using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Contabilidad;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Contabilidad;

// RF-CON-04: Reporte de balance de comprobación - Matriz de verificación de débitos, créditos y saldos finales para el cuadre contable (8 columnas).
public class BalanceOchoColumnasViewModel : ReporteBaseViewModel
{
    private readonly ContabilidadService _contabilidadService;

    public ObservableCollection<BalanceOchoColumnasItem> Items { get; } = new();

    private DateTime _fechaDesde = new(DateTime.Today.Year, 1, 1);
    public DateTime FechaDesde { get => _fechaDesde; set => SetProperty(ref _fechaDesde, value); }

    private DateTime _fechaHasta = DateTime.Today.AddMonths(2);
    public DateTime FechaHasta { get => _fechaHasta; set => SetProperty(ref _fechaHasta, value); }

    // Totales Sumas (Cols 1-2)
    private decimal _sumDebe;
    public decimal SumDebe { get => _sumDebe; set { if (SetProperty(ref _sumDebe, value)) OnPropertyChanged(nameof(SumDebeTexto)); } }
    public string SumDebeTexto => Formato.Moneda(SumDebe);

    private decimal _sumHaber;
    public decimal SumHaber { get => _sumHaber; set { if (SetProperty(ref _sumHaber, value)) OnPropertyChanged(nameof(SumHaberTexto)); } }
    public string SumHaberTexto => Formato.Moneda(SumHaber);

    // Totales Saldos (Cols 3-4)
    private decimal _sumSaldoDeudor;
    public decimal SumSaldoDeudor { get => _sumSaldoDeudor; set { if (SetProperty(ref _sumSaldoDeudor, value)) OnPropertyChanged(nameof(SumSaldoDeudorTexto)); } }
    public string SumSaldoDeudorTexto => Formato.Moneda(SumSaldoDeudor);

    private decimal _sumSaldoAcreedor;
    public decimal SumSaldoAcreedor { get => _sumSaldoAcreedor; set { if (SetProperty(ref _sumSaldoAcreedor, value)) OnPropertyChanged(nameof(SumSaldoAcreedorTexto)); } }
    public string SumSaldoAcreedorTexto => Formato.Moneda(SumSaldoAcreedor);

    // Totales Inventario / Balance (Cols 5-6)
    private decimal _sumActivo;
    public decimal SumActivo { get => _sumActivo; set { if (SetProperty(ref _sumActivo, value)) OnPropertyChanged(nameof(SumActivoTexto)); } }
    public string SumActivoTexto => Formato.Moneda(SumActivo);

    private decimal _sumPasivo;
    public decimal SumPasivo { get => _sumPasivo; set { if (SetProperty(ref _sumPasivo, value)) OnPropertyChanged(nameof(SumPasivoTexto)); } }
    public string SumPasivoTexto => Formato.Moneda(SumPasivo);

    // Totales Resultados (Cols 7-8)
    private decimal _sumPerdida;
    public decimal SumPerdida { get => _sumPerdida; set { if (SetProperty(ref _sumPerdida, value)) OnPropertyChanged(nameof(SumPerdidaTexto)); } }
    public string SumPerdidaTexto => Formato.Moneda(SumPerdida);

    private decimal _sumGanancia;
    public decimal SumGanancia { get => _sumGanancia; set { if (SetProperty(ref _sumGanancia, value)) OnPropertyChanged(nameof(SumGananciaTexto)); } }
    public string SumGananciaTexto => Formato.Moneda(SumGanancia);

    // Resultado del Ejercicio (Utilidad o Pérdida Tributaria)
    private decimal _resultadoEjercicio;
    public decimal ResultadoEjercicio
    {
        get => _resultadoEjercicio;
        set
        {
            if (SetProperty(ref _resultadoEjercicio, value))
            {
                OnPropertyChanged(nameof(EsUtilidad));
                OnPropertyChanged(nameof(EsPerdida));
                OnPropertyChanged(nameof(ResultadoAbsoluto));
                OnPropertyChanged(nameof(ResultadoAbsolutoTexto));
                OnPropertyChanged(nameof(ImpuestoRentaEstimadoTexto));
            }
        }
    }

    public bool EsUtilidad => ResultadoEjercicio >= 0;
    public bool EsPerdida => ResultadoEjercicio < 0;
    public decimal ResultadoAbsoluto => Math.Abs(ResultadoEjercicio);
    public string ResultadoAbsolutoTexto => Formato.Moneda(ResultadoAbsoluto);

    // Impuesto 1ª Categoría Chile (Régimen Pro Pyme Art. 14 D N°3 LIR: 25% sobre base imponible positiva)
    public decimal ImpuestoRentaEstimado => ResultadoEjercicio > 0 ? Math.Round(ResultadoEjercicio * 0.25m, 0) : 0;
    public string ImpuestoRentaEstimadoTexto => Formato.Moneda(ImpuestoRentaEstimado);

    private string _textoResultado = "UTILIDAD DEL EJERCICIO";
    public string TextoResultado { get => _textoResultado; set => SetProperty(ref _textoResultado, value); }

    // Totales Finales Cuadrados (Sumas Iguales SII)
    private decimal _totalFinalActivo;
    public decimal TotalFinalActivo { get => _totalFinalActivo; set { if (SetProperty(ref _totalFinalActivo, value)) OnPropertyChanged(nameof(TotalFinalActivoTexto)); } }
    public string TotalFinalActivoTexto => Formato.Moneda(TotalFinalActivo);

    private decimal _totalFinalPasivo;
    public decimal TotalFinalPasivo { get => _totalFinalPasivo; set { if (SetProperty(ref _totalFinalPasivo, value)) OnPropertyChanged(nameof(TotalFinalPasivoTexto)); } }
    public string TotalFinalPasivoTexto => Formato.Moneda(TotalFinalPasivo);

    private decimal _totalFinalPerdida;
    public decimal TotalFinalPerdida { get => _totalFinalPerdida; set { if (SetProperty(ref _totalFinalPerdida, value)) OnPropertyChanged(nameof(TotalFinalPerdidaTexto)); } }
    public string TotalFinalPerdidaTexto => Formato.Moneda(TotalFinalPerdida);

    private decimal _totalFinalGanancia;
    public decimal TotalFinalGanancia { get => _totalFinalGanancia; set { if (SetProperty(ref _totalFinalGanancia, value)) OnPropertyChanged(nameof(TotalFinalGananciaTexto)); } }
    public string TotalFinalGananciaTexto => Formato.Moneda(TotalFinalGanancia);

    public ICommand CargarBalanceCommand { get; }

    public BalanceOchoColumnasViewModel(ContabilidadService contabilidadService)
    {
        _contabilidadService = contabilidadService;
        Titulo = "Balance Tributario de 8 Columnas";
        CargarBalanceCommand = GenerarCommand;
    }

    protected override async Task CargarDatosAsync()
    {
        Items.Clear();
        var datos = await _contabilidadService.ObtenerBalanceOchoColumnasAsync(FechaDesde, FechaHasta);
        foreach (var item in datos)
            Items.Add(item);

        CalcularTotales();
        HayDatos = Items.Count > 0;
    }

    private void CalcularTotales()
    {
        SumDebe = Items.Sum(x => x.TotalDebe);
        SumHaber = Items.Sum(x => x.TotalHaber);
        SumSaldoDeudor = Items.Sum(x => x.SaldoDeudor);
        SumSaldoAcreedor = Items.Sum(x => x.SaldoAcreedor);
        SumActivo = Items.Sum(x => x.Activo);
        SumPasivo = Items.Sum(x => x.Pasivo);
        SumPerdida = Items.Sum(x => x.Perdida);
        SumGanancia = Items.Sum(x => x.Ganancia);

        ResultadoEjercicio = SumGanancia - SumPerdida;

        if (ResultadoEjercicio >= 0)
        {
            TextoResultado = "UTILIDAD DEL EJERCICIO";
            TotalFinalActivo = SumActivo;
            TotalFinalPasivo = SumPasivo + ResultadoEjercicio;
            TotalFinalPerdida = SumPerdida + ResultadoEjercicio;
            TotalFinalGanancia = SumGanancia;
        }
        else
        {
            TextoResultado = "PÉRDIDA DEL EJERCICIO";
            TotalFinalActivo = SumActivo + Math.Abs(ResultadoEjercicio);
            TotalFinalPasivo = SumPasivo;
            TotalFinalPerdida = SumPerdida;
            TotalFinalGanancia = SumGanancia + Math.Abs(ResultadoEjercicio);
        }
    }

    protected override DocumentoReporte ConstruirDocumento()
    {
        var doc = new DocumentoReporte
        {
            Titulo = "Balance Tributario de 8 Columnas (Formato SII Chile)",
            Subtitulo = $"Matriz de comprobación, saldos, inventario y resultado · Período {Rango(FechaDesde, FechaHasta)}",
            Horizontal = true
        };
        doc.Filtros.Add(("Período", Rango(FechaDesde, FechaHasta)));
        doc.Kpis.Add(("Sumas Iguales", SumDebeTexto));
        doc.Kpis.Add(("Total Activos", SumActivoTexto));
        doc.Kpis.Add(("Total Pasivo + Patr.", SumPasivoTexto));
        doc.Kpis.Add((TextoResultado, ResultadoAbsolutoTexto));
        doc.Kpis.Add(("Provisión IDPC (25%)", ImpuestoRentaEstimadoTexto));

        var s = new SeccionReporte("Hoja de Trabajo 8 Columnas",
            new ColumnaReporte("Código"),
            new ColumnaReporte("Cuenta Contable"),
            new ColumnaReporte("Débitos", TipoColumna.Moneda),
            new ColumnaReporte("Créditos", TipoColumna.Moneda),
            new ColumnaReporte("Saldo Deudor", TipoColumna.Moneda),
            new ColumnaReporte("Saldo Acreedor", TipoColumna.Moneda),
            new ColumnaReporte("Activo", TipoColumna.Moneda),
            new ColumnaReporte("Pasivo/Patr.", TipoColumna.Moneda),
            new ColumnaReporte("Pérdida", TipoColumna.Moneda),
            new ColumnaReporte("Ganancia", TipoColumna.Moneda));

        foreach (var i in Items)
            s.Fila(i.CodigoCuenta, i.NombreCuenta, i.TotalDebe, i.TotalHaber, i.SaldoDeudor, i.SaldoAcreedor, i.Activo, i.Pasivo, i.Perdida, i.Ganancia);

        s.Total("", "SUBTOTALES DEL PERÍODO", SumDebe, SumHaber, SumSaldoDeudor, SumSaldoAcreedor, SumActivo, SumPasivo, SumPerdida, SumGanancia);
        s.Total("", TextoResultado, 0m, 0m, 0m, 0m,
            EsPerdida ? ResultadoAbsoluto : 0m,
            EsUtilidad ? ResultadoAbsoluto : 0m,
            EsUtilidad ? ResultadoAbsoluto : 0m,
            EsPerdida ? ResultadoAbsoluto : 0m);
        s.Total("", "SUMAS IGUALES (CUADRATURA SII)", SumDebe, SumHaber, SumSaldoDeudor, SumSaldoAcreedor, TotalFinalActivo, TotalFinalPasivo, TotalFinalPerdida, TotalFinalGanancia);

        doc.Secciones.Add(s);
        return doc;
    }
}
