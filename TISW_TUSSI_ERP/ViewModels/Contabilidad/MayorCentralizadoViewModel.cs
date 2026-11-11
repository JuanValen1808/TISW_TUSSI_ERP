using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Contabilidad;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Contabilidad;

// RF-CON-06: Reporte de libro mayor centralizado - Desglose detallado de los movimientos mensuales agrupados por cuenta contable.
public class MayorCentralizadoViewModel : ReporteBaseViewModel
{
    private readonly ContabilidadService _contabilidadService;

    public ObservableCollection<MayorCentralizadoItem> Items { get; } = new();
    public ObservableCollection<ResumenMensualMayorCentralizado> ResumenMensual { get; } = new();
    public ObservableCollection<PlanCuentas> CuentasFiltro { get; } = new();

    private PlanCuentas? _cuentaSeleccionada;
    public PlanCuentas? CuentaSeleccionada
    {
        get => _cuentaSeleccionada;
        set => SetProperty(ref _cuentaSeleccionada, value);
    }

    private DateTime _fechaDesde = new DateTime(DateTime.Now.Year, 1, 1);
    public DateTime FechaDesde
    {
        get => _fechaDesde;
        set => SetProperty(ref _fechaDesde, value);
    }

    private DateTime _fechaHasta = DateTime.Now.AddMonths(2);
    public DateTime FechaHasta
    {
        get => _fechaHasta;
        set => SetProperty(ref _fechaHasta, value);
    }

    private decimal _totalDebeFiltro;
    public decimal TotalDebeFiltro { get => _totalDebeFiltro; set { if (SetProperty(ref _totalDebeFiltro, value)) OnPropertyChanged(nameof(TotalDebeFiltroTexto)); } }
    public string TotalDebeFiltroTexto => Formato.Moneda(TotalDebeFiltro);

    private decimal _totalHaberFiltro;
    public decimal TotalHaberFiltro { get => _totalHaberFiltro; set { if (SetProperty(ref _totalHaberFiltro, value)) OnPropertyChanged(nameof(TotalHaberFiltroTexto)); } }
    public string TotalHaberFiltroTexto => Formato.Moneda(TotalHaberFiltro);

    private int _cuentasAgrupadasCount;
    public int CuentasAgrupadasCount { get => _cuentasAgrupadasCount; set => SetProperty(ref _cuentasAgrupadasCount, value); }

    private string _textoBusqueda = string.Empty;
    public string TextoBusqueda { get => _textoBusqueda; set => SetProperty(ref _textoBusqueda, value); }

    public PlanCuentas? CuentaFiltroSeleccionada { get => CuentaSeleccionada; set { CuentaSeleccionada = value; OnPropertyChanged(); } }
    public int CantidadGruposMensuales => ResumenMensual.Count;
    public int CantidadMovimientos => Items.Count;
    public string TotalDebeTexto => TotalDebeFiltroTexto;
    public string TotalHaberTexto => TotalHaberFiltroTexto;
    public string SaldoNetoTexto => Formato.Moneda(TotalDebeFiltro - TotalHaberFiltro);

    public ICommand BuscarCommand { get; }
    public ICommand RangoMesActualCommand { get; }
    public ICommand RangoTrimestreCommand { get; }
    public ICommand RangoAnioActualCommand { get; }
    public ICommand RangoTodoCommand { get; }

    public MayorCentralizadoViewModel(ContabilidadService contabilidadService)
    {
        _contabilidadService = contabilidadService;
        Titulo = "Libro Mayor Centralizado";
        BuscarCommand = GenerarCommand;
        RangoMesActualCommand = new Command(async () => { FechaDesde = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1); FechaHasta = DateTime.Today; await GenerarAsync(); });
        RangoTrimestreCommand = new Command(async () => { FechaDesde = DateTime.Today.AddMonths(-3); FechaHasta = DateTime.Today; await GenerarAsync(); });
        RangoAnioActualCommand = new Command(async () => { FechaDesde = new DateTime(DateTime.Today.Year, 1, 1); FechaHasta = DateTime.Today.AddMonths(1); await GenerarAsync(); });
        RangoTodoCommand = new Command(async () => { FechaDesde = new DateTime(2020, 1, 1); FechaHasta = DateTime.Today.AddYears(1); await GenerarAsync(); });
    }

    public async Task InicializarAsync()
    {
        if (CuentasFiltro.Count == 0)
        {
            var cuentas = await _contabilidadService.ObtenerPlanCuentasAsync();
            CuentasFiltro.Add(new PlanCuentas { IdCuenta = 0, CodigoCuenta = "0", NombreCuenta = "— TODAS LAS CUENTAS CONTABLES —" });
            foreach (var c in cuentas.Where(x => !x.EsGrupo).OrderBy(x => x.CodigoCuenta))
                CuentasFiltro.Add(c);
            CuentaSeleccionada = CuentasFiltro.FirstOrDefault();
        }
        await GenerarAsync();
    }

    protected override async Task CargarDatosAsync()
    {
        if (CuentasFiltro.Count == 0)
        {
            var cuentas = await _contabilidadService.ObtenerPlanCuentasAsync();
            CuentasFiltro.Add(new PlanCuentas { IdCuenta = 0, CodigoCuenta = "0", NombreCuenta = "— TODAS LAS CUENTAS CONTABLES —" });
            foreach (var c in cuentas.Where(x => !x.EsGrupo).OrderBy(x => x.CodigoCuenta))
                CuentasFiltro.Add(c);
            CuentaSeleccionada ??= CuentasFiltro.FirstOrDefault();
        }

        Items.Clear();
        ResumenMensual.Clear();

        int? idCuentaFiltro = CuentaSeleccionada?.IdCuenta == 0 ? null : CuentaSeleccionada?.IdCuenta;
        var resultados = await _contabilidadService.ObtenerMayorCentralizadoAsync(idCuentaFiltro, FechaDesde, FechaHasta);
        if (!string.IsNullOrWhiteSpace(TextoBusqueda))
        {
            var q = TextoBusqueda.Trim();
            resultados = resultados.Where(x => x.CodigoCuenta.Contains(q, StringComparison.OrdinalIgnoreCase)
                                            || x.NombreCuenta.Contains(q, StringComparison.OrdinalIgnoreCase)
                                            || x.Glosa.Contains(q, StringComparison.OrdinalIgnoreCase)
                                            || x.NumeroAsiento.ToString().Contains(q)).ToList();
        }

        foreach (var item in resultados)
            Items.Add(item);

        // Agrupación mensual por cuenta contable (RF-CON-06)
        var gruposMensuales = resultados
            .GroupBy(x => new { Mes = new DateTime(x.Fecha.Year, x.Fecha.Month, 1), x.CodigoCuenta, x.NombreCuenta, x.TipoCuenta })
            .OrderBy(g => g.Key.Mes)
            .ThenBy(g => g.Key.CodigoCuenta);

        foreach (var g in gruposMensuales)
        {
            decimal sumD = g.Sum(x => x.Debe);
            decimal sumH = g.Sum(x => x.Haber);
            bool deudora = EstiloContable.EsNaturalezaDeudora(g.Key.TipoCuenta);
            decimal variacion = deudora ? (sumD - sumH) : (sumH - sumD);
            decimal ultimoSaldo = g.Last().SaldoAcumulado;

            ResumenMensual.Add(new ResumenMensualMayorCentralizado
            {
                PeriodoDate = g.Key.Mes,
                PeriodoTexto = Formato.Capitalizar(g.Key.Mes.ToString("MMMM yyyy", Formato.Cultura)),
                CodigoCuenta = g.Key.CodigoCuenta,
                NombreCuenta = g.Key.NombreCuenta,
                TipoCuenta = g.Key.TipoCuenta,
                CantidadMovimientos = g.Count(),
                TotalDebe = sumD,
                TotalHaber = sumH,
                VariacionNeta = variacion,
                SaldoFinalMes = ultimoSaldo
            });
        }

        TotalDebeFiltro = Items.Sum(x => x.Debe);
        TotalHaberFiltro = Items.Sum(x => x.Haber);
        CuentasAgrupadasCount = resultados.Select(x => x.CodigoCuenta).Distinct().Count();
        foreach (var p in new[] { nameof(CantidadGruposMensuales), nameof(CantidadMovimientos), nameof(TotalDebeTexto), nameof(TotalHaberTexto), nameof(SaldoNetoTexto) })
            OnPropertyChanged(p);
        HayDatos = Items.Count > 0;
    }

    protected override DocumentoReporte ConstruirDocumento()
    {
        var doc = new DocumentoReporte
        {
            Titulo = "Reporte de Libro Mayor Centralizado",
            Subtitulo = $"Desglose detallado de movimientos mensuales agrupados por cuenta contable · {Rango(FechaDesde, FechaHasta)}",
            Horizontal = true
        };
        doc.Filtros.Add(("Cuenta", CuentaSeleccionada?.NombreCuenta ?? "Todas"));
        doc.Filtros.Add(("Período", Rango(FechaDesde, FechaHasta)));
        doc.Kpis.Add(("Cuentas con Movimientos", CuentasAgrupadasCount.ToString()));
        doc.Kpis.Add(("Movimientos", Items.Count.ToString()));
        doc.Kpis.Add(("Total Débitos", TotalDebeFiltroTexto));
        doc.Kpis.Add(("Total Créditos", TotalHaberFiltroTexto));

        var sResumen = new SeccionReporte("Centralización Mensual Agrupada por Cuenta Contable",
            new ColumnaReporte("Mes / Período"),
            new ColumnaReporte("Código"),
            new ColumnaReporte("Cuenta Contable"),
            new ColumnaReporte("Asientos", TipoColumna.Numero),
            new ColumnaReporte("Total Debe", TipoColumna.Moneda),
            new ColumnaReporte("Total Haber", TipoColumna.Moneda),
            new ColumnaReporte("Variación Neta", TipoColumna.Moneda),
            new ColumnaReporte("Saldo al Cierre", TipoColumna.Moneda));

        foreach (var r in ResumenMensual)
            sResumen.Fila(r.PeriodoTexto, r.CodigoCuenta, r.NombreCuenta, r.CantidadMovimientos, r.TotalDebe, r.TotalHaber, r.VariacionNeta, r.SaldoFinalMes);

        sResumen.Total("TOTAL PERÍODO", "", "", Items.Count, TotalDebeFiltro, TotalHaberFiltro, TotalDebeFiltro - TotalHaberFiltro, 0m);
        doc.Secciones.Add(sResumen);

        var sDet = new SeccionReporte("Desglose Detallado de Movimientos por Cuenta",
            new ColumnaReporte("Fecha", TipoColumna.Fecha),
            new ColumnaReporte("Asiento"),
            new ColumnaReporte("Origen"),
            new ColumnaReporte("Código"),
            new ColumnaReporte("Cuenta"),
            new ColumnaReporte("Glosa"),
            new ColumnaReporte("Debe", TipoColumna.Moneda),
            new ColumnaReporte("Haber", TipoColumna.Moneda),
            new ColumnaReporte("Saldo Acumulado", TipoColumna.Moneda));

        foreach (var m in Items)
            sDet.Fila(m.Fecha, m.AsientoTexto, m.Origen, m.CodigoCuenta, m.NombreCuenta, m.GlosaTexto, m.Debe, m.Haber, m.SaldoAcumulado);

        sDet.Total(null, "", "", "", "", "TOTALES DEL PERÍODO", TotalDebeFiltro, TotalHaberFiltro, null);
        doc.Secciones.Add(sDet);
        return doc;
    }
}
