using System.Collections.ObjectModel;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Contabilidad;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Contabilidad;

// REPORTE 5: Análisis por Cuenta (evolución mensual, saldo según naturaleza y contrapartidas).
public class AnalisisCuentaViewModel : ReporteBaseViewModel
{
    private readonly ReportesContablesService _reportes;
    private readonly ContabilidadService _contabilidad;

    public ObservableCollection<PlanCuentas> Cuentas { get; } = new();
    public ObservableCollection<MovimientoCuenta> Movimientos { get; } = new();
    public ObservableCollection<ResumenMensualCuenta> ResumenMensual { get; } = new();

    private PlanCuentas? _cuentaSeleccionada;
    public PlanCuentas? CuentaSeleccionada { get => _cuentaSeleccionada; set => SetProperty(ref _cuentaSeleccionada, value); }

    private DateTime _fechaDesde = new(DateTime.Today.Year, 1, 1);
    public DateTime FechaDesde { get => _fechaDesde; set => SetProperty(ref _fechaDesde, value); }

    private DateTime _fechaHasta = DateTime.Today;
    public DateTime FechaHasta { get => _fechaHasta; set => SetProperty(ref _fechaHasta, value); }

    private PlanCuentas? _cuentaAnalizada;
    private decimal _saldoInicial, _debe, _haber;
    private bool _deudora = true;

    public string CuentaTexto => _cuentaAnalizada == null ? "Selecciona una cuenta" : _cuentaAnalizada.Descripcion;
    public string TipoTexto => _cuentaAnalizada?.TipoTexto ?? "–";
    public Color TipoFondo => _cuentaAnalizada?.TipoFondo ?? Color.FromArgb("#F1F5F9");
    public Color TipoColor => _cuentaAnalizada?.TipoColor ?? Color.FromArgb("#475569");
    public string NaturalezaTexto => _cuentaAnalizada == null ? "–" : $"Naturaleza {(_deudora ? "deudora" : "acreedora")}";
    public string SaldoInicialTexto => Formato.Moneda(_saldoInicial);
    public string DebeTexto => Formato.Moneda(_debe);
    public string HaberTexto => Formato.Moneda(_haber);
    public decimal SaldoFinal => _saldoInicial + (_deudora ? _debe - _haber : _haber - _debe);
    public string SaldoFinalTexto => Formato.Moneda(SaldoFinal);
    public string VariacionTexto
    {
        get
        {
            var delta = SaldoFinal - _saldoInicial;
            if (_saldoInicial == 0) return delta == 0 ? "Sin variación" : $"{(delta > 0 ? "+" : "−")} {Formato.Moneda(Math.Abs(delta))} en el período";
            return $"{(delta >= 0 ? "▲" : "▼")} {(Math.Abs(delta / _saldoInicial)).ToString("P1", Formato.Cultura)} vs saldo inicial";
        }
    }
    public Color VariacionColor => SaldoFinal >= _saldoInicial ? Color.FromArgb("#047857") : Color.FromArgb("#BE123C");
    public string MovimientosTexto => Movimientos.Count == 1 ? "1 movimiento" : $"{Movimientos.Count} movimientos";
    public string PeriodoTexto => Rango(FechaDesde, FechaHasta);

    private GraficoBarrasDrawable _grafico = new();
    public GraficoBarrasDrawable Grafico { get => _grafico; set => SetProperty(ref _grafico, value); }

    public AnalisisCuentaViewModel(ReportesContablesService reportes, ContabilidadService contabilidad)
    {
        _reportes = reportes;
        _contabilidad = contabilidad;
        Titulo = "Análisis por Cuenta";
    }

    public async Task InicializarAsync()
    {
        if (Cuentas.Count > 0) return;
        try
        {
            var cuentas = await _contabilidad.ObtenerPlanCuentasAsync();
            foreach (var c in cuentas) Cuentas.Add(c);
            // Por defecto, la primera cuenta de detalle (ej: Caja)
            CuentaSeleccionada = Cuentas.FirstOrDefault(c => !c.EsGrupo) ?? Cuentas.FirstOrDefault();
        }
        catch (Exception ex)
        {
            MensajeError = $"No se pudo cargar el plan de cuentas: {ex.Message}";
        }
    }

    protected override async Task CargarDatosAsync()
    {
        if (CuentaSeleccionada == null)
            throw new InvalidOperationException("Selecciona una cuenta contable para analizar.");
        if (FechaHasta < FechaDesde)
            throw new InvalidOperationException("La fecha 'Hasta' no puede ser anterior a 'Desde'.");

        _cuentaAnalizada = CuentaSeleccionada;
        _deudora = EstiloContable.EsNaturalezaDeudora(_cuentaAnalizada.TipoCuenta);

        var (debeAnt, haberAnt, movimientos) = await _reportes.ObtenerAnalisisCuentaAsync(_cuentaAnalizada.IdCuenta, FechaDesde, FechaHasta);
        _saldoInicial = _deudora ? debeAnt - haberAnt : haberAnt - debeAnt;
        _debe = movimientos.Sum(m => m.Debe);
        _haber = movimientos.Sum(m => m.Haber);

        decimal saldo = _saldoInicial;
        Movimientos.Clear();
        foreach (var m in movimientos)
        {
            saldo += _deudora ? m.Debe - m.Haber : m.Haber - m.Debe;
            m.Saldo = saldo;
            Movimientos.Add(m);
        }

        // Resumen mensual (todos los meses del rango, máx. 24)
        var meses = new List<DateTime>();
        for (var mes = new DateTime(FechaDesde.Year, FechaDesde.Month, 1); mes <= FechaHasta && meses.Count < 24; mes = mes.AddMonths(1)) meses.Add(mes);

        ResumenMensual.Clear();
        decimal acumulado = _saldoInicial;
        foreach (var mes in meses)
        {
            var delMes = movimientos.Where(m => m.Fecha.Year == mes.Year && m.Fecha.Month == mes.Month).ToList();
            var d = delMes.Sum(m => m.Debe);
            var h = delMes.Sum(m => m.Haber);
            var neto = _deudora ? d - h : h - d;
            acumulado += neto;
            ResumenMensual.Add(new ResumenMensualCuenta { Mes = mes, Debe = d, Haber = h, Neto = neto, SaldoAcumulado = acumulado, Movimientos = delMes.Count });
        }

        var ultimos = ResumenMensual.TakeLast(12).ToList();
        Grafico = new GraficoBarrasDrawable
        {
            Categorias = ultimos.Select(r => r.MesCorto).ToList(),
            Series = new List<SerieGrafico>
            {
                new() { Nombre = "Debe", Color = Color.FromArgb("#3B82F6"), Valores = ultimos.Select(r => (double)r.Debe).ToList() },
                new() { Nombre = "Haber", Color = Color.FromArgb("#A855F7"), Valores = ultimos.Select(r => (double)r.Haber).ToList() }
            }
        };

        HayDatos = Movimientos.Count > 0 || _saldoInicial != 0;
        foreach (var p in new[] { nameof(CuentaTexto), nameof(TipoTexto), nameof(TipoFondo), nameof(TipoColor), nameof(NaturalezaTexto),
                                  nameof(SaldoInicialTexto), nameof(DebeTexto), nameof(HaberTexto), nameof(SaldoFinal), nameof(SaldoFinalTexto),
                                  nameof(VariacionTexto), nameof(VariacionColor), nameof(MovimientosTexto), nameof(PeriodoTexto) })
            OnPropertyChanged(p);
    }

    protected override DocumentoReporte ConstruirDocumento()
    {
        var doc = new DocumentoReporte
        {
            Titulo = "Análisis por Cuenta",
            Subtitulo = $"{CuentaTexto} · {TipoTexto} · {NaturalezaTexto} · Período {PeriodoTexto}",
            Horizontal = true
        };
        doc.Filtros.Add(("Cuenta", CuentaTexto));
        doc.Filtros.Add(("Período", PeriodoTexto));
        doc.Kpis.Add(("Saldo inicial", SaldoInicialTexto));
        doc.Kpis.Add(("Cargos (Debe)", DebeTexto));
        doc.Kpis.Add(("Abonos (Haber)", HaberTexto));
        doc.Kpis.Add(("Saldo final", SaldoFinalTexto));
        doc.Kpis.Add(("Variación", VariacionTexto));
        doc.Kpis.Add(("Movimientos", Movimientos.Count.ToString()));

        var sM = new SeccionReporte("Evolución mensual",
            new ColumnaReporte("Mes"), new ColumnaReporte("Movimientos", TipoColumna.Numero), new ColumnaReporte("Debe", TipoColumna.Moneda),
            new ColumnaReporte("Haber", TipoColumna.Moneda), new ColumnaReporte("Variación neta", TipoColumna.Moneda), new ColumnaReporte("Saldo acumulado", TipoColumna.Moneda));
        foreach (var r in ResumenMensual) sM.Fila(r.MesTexto, r.Movimientos, r.Debe, r.Haber, r.Neto, r.SaldoAcumulado);
        sM.Total("TOTAL PERÍODO", Movimientos.Count, _debe, _haber, SaldoFinal - _saldoInicial, SaldoFinal);
        doc.Secciones.Add(sM);

        var sD = new SeccionReporte("Detalle de movimientos",
            new ColumnaReporte("Fecha", TipoColumna.Fecha), new ColumnaReporte("N° Asiento"), new ColumnaReporte("Origen"), new ColumnaReporte("Glosa"),
            new ColumnaReporte("Contrapartida"), new ColumnaReporte("Debe", TipoColumna.Moneda), new ColumnaReporte("Haber", TipoColumna.Moneda),
            new ColumnaReporte("Saldo", TipoColumna.Moneda));
        sD.Fila(null, "", "", "Saldo inicial", "", null, null, _saldoInicial);
        foreach (var m in Movimientos) sD.Fila(m.Fecha, m.NumeroAsiento.ToString(), m.Origen, m.GlosaTexto, m.Contrapartida, m.Debe, m.Haber, m.Saldo);
        sD.Total(null, "", "", "TOTALES / SALDO FINAL", "", _debe, _haber, SaldoFinal);
        doc.Secciones.Add(sD);
        return doc;
    }
}
