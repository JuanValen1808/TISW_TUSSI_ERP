using System.Collections.ObjectModel;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Contabilidad;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Contabilidad;

// REPORTE 2: Balance General / Estado de Situación Financiera a una fecha de corte.
public class BalanceGeneralViewModel : ReporteBaseViewModel
{
    private readonly ReportesContablesService _servicio;

    private static readonly Color ColorActivo = Color.FromArgb("#1D6FD8");
    private static readonly Color ColorPasivo = Color.FromArgb("#E11D48");
    private static readonly Color ColorPatrimonio = Color.FromArgb("#7C3AED");

    public ObservableCollection<LineaBalanceGeneral> Activos { get; } = new();
    public ObservableCollection<LineaBalanceGeneral> Pasivos { get; } = new();
    public ObservableCollection<LineaBalanceGeneral> Patrimonio { get; } = new();

    private DateTime _fechaCorte = DateTime.Today;
    public DateTime FechaCorte { get => _fechaCorte; set => SetProperty(ref _fechaCorte, value); }

    private decimal _totalActivos, _totalPasivos, _totalPatrimonio, _resultado;

    public string TotalActivosTexto => Formato.Moneda(_totalActivos);
    public string TotalPasivosTexto => Formato.Moneda(_totalPasivos);
    public string TotalPatrimonioTexto => Formato.Moneda(_totalPatrimonio);
    public string TotalPasivoPatrimonioTexto => Formato.Moneda(_totalPasivos + _totalPatrimonio);
    public string ResultadoTexto => Formato.Moneda(_resultado);
    public string ResultadoEtiqueta => _resultado >= 0 ? "UTILIDAD DEL EJERCICIO" : "PÉRDIDA DEL EJERCICIO";
    public Color ResultadoColor => _resultado >= 0 ? Color.FromArgb("#047857") : Color.FromArgb("#BE123C");

    public bool Cuadra => _totalActivos == _totalPasivos + _totalPatrimonio;
    public string CuadraturaTexto => Cuadra
        ? "Ecuación contable verificada: Activo = Pasivo + Patrimonio"
        : $"Diferencia de {Formato.Moneda(Math.Abs(_totalActivos - _totalPasivos - _totalPatrimonio))} entre Activo y Pasivo + Patrimonio";
    public Color CuadraturaFondo => Cuadra ? Color.FromArgb("#ECFDF5") : Color.FromArgb("#FEF2F2");
    public Color CuadraturaColor => Cuadra ? Color.FromArgb("#047857") : Color.FromArgb("#BE123C");

    public string EndeudamientoTexto => _totalActivos == 0 ? "–" : (_totalPasivos / _totalActivos).ToString("P1", Formato.Cultura);
    public string SolvenciaTexto => _totalPasivos == 0 ? "–" : (_totalActivos / _totalPasivos).ToString("0.00", Formato.Cultura) + "x";
    public string AutonomiaTexto => _totalActivos == 0 ? "–" : (_totalPatrimonio / _totalActivos).ToString("P1", Formato.Cultura);
    public string FechaCorteTexto => FechaCorte.ToString("dd 'de' MMMM 'de' yyyy", Formato.Cultura);

    public BalanceGeneralViewModel(ReportesContablesService servicio)
    {
        _servicio = servicio;
        Titulo = "Balance General";
    }

    protected override async Task CargarDatosAsync()
    {
        var saldos = await _servicio.ObtenerSaldosAFechaAsync(FechaCorte);

        var activos = saldos.Where(s => s.TipoCuenta == "ACTIVO" && s.SaldoNatural != 0).ToList();
        var pasivos = saldos.Where(s => s.TipoCuenta == "PASIVO" && s.SaldoNatural != 0).ToList();
        var patrimonio = saldos.Where(s => s.TipoCuenta == "PATRIMONIO" && s.SaldoNatural != 0).ToList();
        var ingresos = saldos.Where(s => s.TipoCuenta == "INGRESO").Sum(s => s.SaldoNatural);
        var gastos = saldos.Where(s => s.TipoCuenta == "GASTO").Sum(s => s.SaldoNatural);

        _resultado = ingresos - gastos;
        _totalActivos = activos.Sum(s => s.SaldoNatural);
        _totalPasivos = pasivos.Sum(s => s.SaldoNatural);
        _totalPatrimonio = patrimonio.Sum(s => s.SaldoNatural) + _resultado;
        var totalPasPat = _totalPasivos + _totalPatrimonio;

        Llenar(Activos, activos, _totalActivos, ColorActivo);
        Llenar(Pasivos, pasivos, totalPasPat, ColorPasivo);
        Llenar(Patrimonio, patrimonio, totalPasPat, ColorPatrimonio);
        if (_resultado != 0)
        {
            Patrimonio.Add(new LineaBalanceGeneral
            {
                CodigoCuenta = "—",
                NombreCuenta = _resultado >= 0 ? "Utilidad del ejercicio" : "Pérdida del ejercicio",
                Saldo = _resultado,
                Proporcion = totalPasPat == 0 ? 0 : (double)Math.Abs(_resultado / totalPasPat),
                Color = ColorPatrimonio,
                EsCalculada = true
            });
        }

        HayDatos = Activos.Count + Pasivos.Count + Patrimonio.Count > 0;
        foreach (var p in new[] { nameof(TotalActivosTexto), nameof(TotalPasivosTexto), nameof(TotalPatrimonioTexto), nameof(TotalPasivoPatrimonioTexto),
                                  nameof(ResultadoTexto), nameof(ResultadoEtiqueta), nameof(ResultadoColor), nameof(Cuadra), nameof(CuadraturaTexto),
                                  nameof(CuadraturaFondo), nameof(CuadraturaColor), nameof(EndeudamientoTexto), nameof(SolvenciaTexto),
                                  nameof(AutonomiaTexto), nameof(FechaCorteTexto) })
            OnPropertyChanged(p);
    }

    private static void Llenar(ObservableCollection<LineaBalanceGeneral> destino, List<SaldoCuenta> origen, decimal total, Color color)
    {
        destino.Clear();
        foreach (var s in origen)
        {
            destino.Add(new LineaBalanceGeneral
            {
                CodigoCuenta = s.CodigoCuenta,
                NombreCuenta = s.NombreCuenta,
                Saldo = s.SaldoNatural,
                Proporcion = total == 0 ? 0 : (double)Math.Clamp(Math.Abs(s.SaldoNatural / total), 0, 1),
                Color = color
            });
        }
    }

    protected override DocumentoReporte ConstruirDocumento()
    {
        var doc = new DocumentoReporte
        {
            Titulo = "Balance General",
            Subtitulo = $"Estado de Situación Financiera al {FechaCorteTexto}"
        };
        doc.Filtros.Add(("Fecha de corte", FechaCorte.ToString("dd/MM/yyyy")));
        doc.Kpis.Add(("Total Activos", TotalActivosTexto));
        doc.Kpis.Add(("Total Pasivos", TotalPasivosTexto));
        doc.Kpis.Add(("Total Patrimonio", TotalPatrimonioTexto));
        doc.Kpis.Add((ResultadoEtiqueta, ResultadoTexto));
        doc.Kpis.Add(("Endeudamiento", EndeudamientoTexto));
        doc.Kpis.Add(("Solvencia", SolvenciaTexto));

        var cols = new[] { new ColumnaReporte("Código"), new ColumnaReporte("Cuenta"), new ColumnaReporte("Saldo", TipoColumna.Moneda), new ColumnaReporte("% del total", TipoColumna.Porcentaje) };

        var sA = new SeccionReporte("Activos", cols);
        foreach (var l in Activos) sA.Fila(l.CodigoCuenta, l.NombreCuenta, l.Saldo, l.Proporcion);
        sA.Total("", "TOTAL ACTIVOS", _totalActivos, 1d);
        doc.Secciones.Add(sA);

        var sP = new SeccionReporte("Pasivos y Patrimonio", cols);
        sP.Subtitulo("Pasivos");
        foreach (var l in Pasivos) sP.Fila(l.CodigoCuenta, l.NombreCuenta, l.Saldo, l.Proporcion);
        sP.Total("", "Total Pasivos", _totalPasivos, null);
        sP.Subtitulo("Patrimonio");
        foreach (var l in Patrimonio) sP.Fila(l.CodigoCuenta, l.NombreCuenta, l.Saldo, l.Proporcion);
        sP.Total("", "Total Patrimonio", _totalPatrimonio, null);
        sP.Total("", "TOTAL PASIVO + PATRIMONIO", _totalPasivos + _totalPatrimonio, 1d);
        doc.Secciones.Add(sP);

        doc.NotaPie = CuadraturaTexto + ".";
        return doc;
    }
}
