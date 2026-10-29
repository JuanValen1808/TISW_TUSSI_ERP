using System.Collections.ObjectModel;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Contabilidad;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Contabilidad;

// REPORTE 4: Flujo de Caja (movimientos reales de efectivo + proyección a 30 días con CxC / CxP).
public class FlujoCajaViewModel : ReporteBaseViewModel
{
    private readonly ReportesContablesService _reportes;
    private readonly ContabilidadService _contabilidad;

    public ObservableCollection<MovimientoCaja> Movimientos { get; } = new();
    public ObservableCollection<ResumenOrigenCaja> ResumenOrigen { get; } = new();

    private DateTime _fechaDesde = new(DateTime.Today.Year, 1, 1);
    public DateTime FechaDesde { get => _fechaDesde; set => SetProperty(ref _fechaDesde, value); }

    private DateTime _fechaHasta = DateTime.Today;
    public DateTime FechaHasta { get => _fechaHasta; set => SetProperty(ref _fechaHasta, value); }

    private decimal _saldoInicial, _entradas, _salidas, _cobros30, _pagos30;
    public string SaldoInicialTexto => Formato.Moneda(_saldoInicial);
    public string EntradasTexto => Formato.Moneda(_entradas);
    public string SalidasTexto => Formato.Moneda(_salidas);
    public decimal FlujoNeto => _entradas - _salidas;
    public string FlujoNetoTexto => (FlujoNeto >= 0 ? "+ " : "− ") + Formato.Moneda(Math.Abs(FlujoNeto));
    public Color FlujoNetoColor => FlujoNeto >= 0 ? Color.FromArgb("#047857") : Color.FromArgb("#BE123C");
    public decimal SaldoFinal => _saldoInicial + FlujoNeto;
    public string SaldoFinalTexto => Formato.Moneda(SaldoFinal);
    public string Cobros30Texto => Formato.Moneda(_cobros30);
    public string Pagos30Texto => Formato.Moneda(_pagos30);
    public decimal SaldoProyectado => SaldoFinal + _cobros30 - _pagos30;
    public string SaldoProyectadoTexto => Formato.Moneda(SaldoProyectado);
    public Color SaldoProyectadoColor => SaldoProyectado >= 0 ? Color.FromArgb("#047857") : Color.FromArgb("#BE123C");
    public string ProyeccionAlerta => SaldoProyectado >= 0
        ? "La caja proyectada cubre los compromisos de los próximos 30 días."
        : "Alerta: los pagos comprometidos superan la caja disponible en los próximos 30 días.";
    public string PeriodoTexto => Rango(FechaDesde, FechaHasta);

    private GraficoBarrasDrawable _grafico = new();
    public GraficoBarrasDrawable Grafico { get => _grafico; set => SetProperty(ref _grafico, value); }

    public FlujoCajaViewModel(ReportesContablesService reportes, ContabilidadService contabilidad)
    {
        _reportes = reportes;
        _contabilidad = contabilidad;
        Titulo = "Flujo de Caja";
    }

    private static string Actividad(string origen) => origen switch
    {
        "VENTA" => "Operación · cobros por ventas",
        "COMPRA" or "RECEPCION" => "Operación · pagos a proveedores",
        "AJUSTE" => "Financiamiento / aportes y ajustes",
        _ => "Otras actividades"
    };

    protected override async Task CargarDatosAsync()
    {
        if (FechaHasta < FechaDesde)
            throw new InvalidOperationException("La fecha 'Hasta' no puede ser anterior a 'Desde'.");

        var (saldoInicial, movimientos) = await _reportes.ObtenerFlujoCajaAsync(FechaDesde, FechaHasta);
        _saldoInicial = saldoInicial;
        _entradas = movimientos.Sum(m => m.Entrada);
        _salidas = movimientos.Sum(m => m.Salida);

        Movimientos.Clear();
        foreach (var m in movimientos) Movimientos.Add(m);

        ResumenOrigen.Clear();
        foreach (var g in movimientos.GroupBy(m => m.Origen).OrderByDescending(g => g.Sum(m => m.Entrada - m.Salida)))
        {
            ResumenOrigen.Add(new ResumenOrigenCaja
            {
                Origen = g.Key,
                Actividad = Actividad(g.Key),
                Entradas = g.Sum(m => m.Entrada),
                Salidas = g.Sum(m => m.Salida)
            });
        }

        // Proyección 30 días: documentos pendientes que vencen hasta hoy + 30 (incluye vencidos)
        var limite = DateTime.Today.AddDays(30);
        var cxc = await _contabilidad.ObtenerCuentasPorCobrarAsync();
        var cxp = await _contabilidad.ObtenerCuentasPorPagarAsync();
        _cobros30 = cxc.Where(c => c.SaldoPendiente > 0 && c.FechaVencimiento.Date <= limite).Sum(c => c.SaldoPendiente);
        _pagos30 = cxp.Where(p => p.SaldoPendiente > 0 && p.FechaVencimiento.Date <= limite).Sum(p => p.SaldoPendiente);

        // Gráfico mensual entradas vs salidas
        var meses = new List<DateTime>();
        for (var m = new DateTime(FechaDesde.Year, FechaDesde.Month, 1); m <= FechaHasta; m = m.AddMonths(1)) meses.Add(m);
        if (meses.Count > 12) meses = meses.TakeLast(12).ToList();
        Grafico = new GraficoBarrasDrawable
        {
            Categorias = meses.Select(m => m.ToString("MMM yy", Formato.Cultura)).ToList(),
            Series = new List<SerieGrafico>
            {
                new() { Nombre = "Entradas", Color = Color.FromArgb("#10B981"),
                        Valores = meses.Select(m => (double)movimientos.Where(x => x.Fecha.Year == m.Year && x.Fecha.Month == m.Month).Sum(x => x.Entrada)).ToList() },
                new() { Nombre = "Salidas", Color = Color.FromArgb("#F43F5E"),
                        Valores = meses.Select(m => (double)movimientos.Where(x => x.Fecha.Year == m.Year && x.Fecha.Month == m.Month).Sum(x => x.Salida)).ToList() }
            }
        };

        HayDatos = movimientos.Count > 0 || _saldoInicial != 0 || _cobros30 != 0 || _pagos30 != 0;
        foreach (var p in new[] { nameof(SaldoInicialTexto), nameof(EntradasTexto), nameof(SalidasTexto), nameof(FlujoNeto), nameof(FlujoNetoTexto),
                                  nameof(FlujoNetoColor), nameof(SaldoFinal), nameof(SaldoFinalTexto), nameof(Cobros30Texto), nameof(Pagos30Texto),
                                  nameof(SaldoProyectado), nameof(SaldoProyectadoTexto), nameof(SaldoProyectadoColor), nameof(ProyeccionAlerta), nameof(PeriodoTexto) })
            OnPropertyChanged(p);
    }

    protected override DocumentoReporte ConstruirDocumento()
    {
        var doc = new DocumentoReporte
        {
            Titulo = "Flujo de Caja",
            Subtitulo = $"Entradas y salidas de efectivo (cuentas de Caja / Banco) · Período {PeriodoTexto}",
            Horizontal = true
        };
        doc.Filtros.Add(("Período", PeriodoTexto));
        doc.Kpis.Add(("Saldo inicial", SaldoInicialTexto));
        doc.Kpis.Add(("Entradas", EntradasTexto));
        doc.Kpis.Add(("Salidas", SalidasTexto));
        doc.Kpis.Add(("Flujo neto", FlujoNetoTexto));
        doc.Kpis.Add(("Saldo final", SaldoFinalTexto));
        doc.Kpis.Add(("Saldo proyectado 30 días", SaldoProyectadoTexto));

        var sR = new SeccionReporte("Resumen por actividad",
            new ColumnaReporte("Origen"), new ColumnaReporte("Actividad"), new ColumnaReporte("Entradas", TipoColumna.Moneda),
            new ColumnaReporte("Salidas", TipoColumna.Moneda), new ColumnaReporte("Neto", TipoColumna.Moneda));
        foreach (var r in ResumenOrigen) sR.Fila(r.Origen, r.Actividad, r.Entradas, r.Salidas, r.Neto);
        sR.Total("TOTAL", "", _entradas, _salidas, FlujoNeto);
        doc.Secciones.Add(sR);

        var sP = new SeccionReporte("Proyección a 30 días", new ColumnaReporte("Concepto"), new ColumnaReporte("Monto", TipoColumna.Moneda));
        sP.Fila("Saldo de caja al cierre del período", SaldoFinal);
        sP.Fila("(+) Cobros esperados (CxC por vencer y vencidas)", _cobros30);
        sP.Fila("(−) Pagos comprometidos (CxP por vencer y vencidas)", _pagos30);
        sP.Total("SALDO PROYECTADO", SaldoProyectado);
        doc.Secciones.Add(sP);

        var sM = new SeccionReporte("Movimientos de efectivo",
            new ColumnaReporte("Fecha", TipoColumna.Fecha), new ColumnaReporte("N° Asiento"), new ColumnaReporte("Origen"),
            new ColumnaReporte("Glosa"), new ColumnaReporte("Contrapartida"), new ColumnaReporte("Entrada", TipoColumna.Moneda),
            new ColumnaReporte("Salida", TipoColumna.Moneda), new ColumnaReporte("Saldo", TipoColumna.Moneda));
        sM.Fila(null, "", "", "Saldo inicial", "", null, null, _saldoInicial);
        foreach (var m in Movimientos) sM.Fila(m.Fecha, m.NumeroAsiento.ToString(), m.Origen, m.GlosaTexto, m.Contrapartida, m.Entrada, m.Salida, m.Saldo);
        sM.Total(null, "", "", "TOTALES / SALDO FINAL", "", _entradas, _salidas, SaldoFinal);
        doc.Secciones.Add(sM);

        doc.NotaPie = ProyeccionAlerta;
        return doc;
    }
}
