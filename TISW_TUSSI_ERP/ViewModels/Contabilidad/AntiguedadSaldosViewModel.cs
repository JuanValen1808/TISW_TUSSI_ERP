using System.Collections.ObjectModel;
using System.Windows.Input;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Contabilidad;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Contabilidad;

// REPORTE 3: Antigüedad de saldos de Cuentas por Cobrar (clientes) o por Pagar (proveedores).
public class AntiguedadSaldosViewModel : ReporteBaseViewModel
{
    private readonly ContabilidadService _servicio;
    private List<CuentaPorCobrar> _cxc = new();
    private List<CuentaPorPagar> _cxp = new();

    public ObservableCollection<TramoAntiguedad> Tramos { get; } = new();
    public ObservableCollection<TerceroAntiguedad> Terceros { get; } = new();
    public ObservableCollection<DocumentoAntiguedad> Documentos { get; } = new();

    private string _modo = "CXC";
    public string Modo
    {
        get => _modo;
        set
        {
            if (SetProperty(ref _modo, value))
            {
                OnPropertyChanged(nameof(EtiquetaTercero));
                OnPropertyChanged(nameof(DescripcionModo));
                Calcular();
            }
        }
    }
    public string EtiquetaTercero => Modo == "CXC" ? "CLIENTE" : "PROVEEDOR";
    public string DescripcionModo => Modo == "CXC" ? "Cuentas por Cobrar a clientes" : "Cuentas por Pagar a proveedores";

    private DateTime _fechaCorte = DateTime.Today;
    public DateTime FechaCorte { get => _fechaCorte; set { if (SetProperty(ref _fechaCorte, value)) Calcular(); } }

    private string _totalTexto = "$0", _vencidoTexto = "$0", _porcentajeVencidoTexto = "0%", _moraPromedioTexto = "0 días";
    public string TotalTexto { get => _totalTexto; set => SetProperty(ref _totalTexto, value); }
    public string VencidoTexto { get => _vencidoTexto; set => SetProperty(ref _vencidoTexto, value); }
    public string PorcentajeVencidoTexto { get => _porcentajeVencidoTexto; set => SetProperty(ref _porcentajeVencidoTexto, value); }
    public string MoraPromedioTexto { get => _moraPromedioTexto; set => SetProperty(ref _moraPromedioTexto, value); }
    private int _cantidadDocumentos;
    public int CantidadDocumentos { get => _cantidadDocumentos; set => SetProperty(ref _cantidadDocumentos, value); }

    private readonly decimal[] _totalPorTramo = new decimal[5];
    public string TotalT0 => Formato.Moneda(_totalPorTramo[0]);
    public string TotalT1 => Formato.Moneda(_totalPorTramo[1]);
    public string TotalT2 => Formato.Moneda(_totalPorTramo[2]);
    public string TotalT3 => Formato.Moneda(_totalPorTramo[3]);
    public string TotalT4 => Formato.Moneda(_totalPorTramo[4]);
    public string TotalGeneral => Formato.Moneda(_totalPorTramo.Sum());

    public ICommand CambiarModoCommand { get; }

    public AntiguedadSaldosViewModel(ContabilidadService servicio)
    {
        _servicio = servicio;
        Titulo = "Antigüedad de Saldos";
        CambiarModoCommand = new Command<string>(m => Modo = m);
    }

    protected override async Task CargarDatosAsync()
    {
        _cxc = await _servicio.ObtenerCuentasPorCobrarAsync();
        _cxp = await _servicio.ObtenerCuentasPorPagarAsync();
        Calcular();
    }

    private void Calcular()
    {
        var corte = FechaCorte.Date;
        IEnumerable<DocumentoAntiguedad> docs = Modo == "CXC"
            ? _cxc.Where(c => c.SaldoPendiente > 0).Select(c => new DocumentoAntiguedad
            {
                Documento = c.NumeroDocumento, Tercero = c.Cliente, Rut = c.RutCliente, FechaEmision = c.FechaEmision,
                FechaVencimiento = c.FechaVencimiento, MontoTotal = c.MontoTotal, Saldo = c.SaldoPendiente,
                DiasVencido = (corte - c.FechaVencimiento.Date).Days
            })
            : _cxp.Where(p => p.SaldoPendiente > 0).Select(p => new DocumentoAntiguedad
            {
                Documento = p.NumeroDocumento, Tercero = p.Proveedor, Rut = p.RutProveedor, FechaEmision = p.FechaEmision,
                FechaVencimiento = p.FechaVencimiento, MontoTotal = p.MontoTotal, Saldo = p.SaldoPendiente,
                DiasVencido = (corte - p.FechaVencimiento.Date).Days
            });

        var lista = docs.OrderByDescending(d => d.DiasVencido).ToList();
        var total = lista.Sum(d => d.Saldo);
        var vencidos = lista.Where(d => d.DiasVencido > 0).ToList();
        var totalVencido = vencidos.Sum(d => d.Saldo);

        Documentos.Clear();
        foreach (var d in lista) Documentos.Add(d);

        Array.Clear(_totalPorTramo);
        Tramos.Clear();
        for (int i = 0; i < 5; i++)
        {
            var enTramo = lista.Where(d => d.IndiceTramo == i).ToList();
            _totalPorTramo[i] = enTramo.Sum(d => d.Saldo);
            Tramos.Add(new TramoAntiguedad
            {
                Nombre = EstiloContable.Tramos[i],
                Monto = _totalPorTramo[i],
                Cantidad = enTramo.Count,
                Proporcion = total == 0 ? 0 : (double)(_totalPorTramo[i] / total),
                Color = EstiloContable.ColoresTramo[i],
                Fondo = EstiloContable.FondosTramo[i]
            });
        }

        Terceros.Clear();
        foreach (var g in lista.GroupBy(d => d.Tercero).OrderByDescending(g => g.Sum(d => d.Saldo)))
        {
            var t = new TerceroAntiguedad { Tercero = g.Key, Rut = g.First().Rut };
            foreach (var d in g) t.Tramos[d.IndiceTramo] += d.Saldo;
            Terceros.Add(t);
        }

        TotalTexto = Formato.Moneda(total);
        VencidoTexto = Formato.Moneda(totalVencido);
        PorcentajeVencidoTexto = total == 0 ? "0%" : (totalVencido / total).ToString("P1", Formato.Cultura);
        var mora = totalVencido == 0 ? 0 : vencidos.Sum(d => d.Saldo * d.DiasVencido) / totalVencido;
        MoraPromedioTexto = $"{mora:0} días";
        CantidadDocumentos = lista.Count;
        HayDatos = lista.Count > 0;

        foreach (var p in new[] { nameof(TotalT0), nameof(TotalT1), nameof(TotalT2), nameof(TotalT3), nameof(TotalT4), nameof(TotalGeneral) })
            OnPropertyChanged(p);
    }

    protected override DocumentoReporte ConstruirDocumento()
    {
        var doc = new DocumentoReporte
        {
            Titulo = "Antigüedad de Saldos",
            Subtitulo = $"{DescripcionModo} clasificadas por tramo de vencimiento al {FechaCorte:dd/MM/yyyy}",
            Horizontal = true
        };
        doc.Filtros.Add(("Tipo", Modo == "CXC" ? "Cuentas por Cobrar" : "Cuentas por Pagar"));
        doc.Filtros.Add(("Fecha de corte", FechaCorte.ToString("dd/MM/yyyy")));
        doc.Kpis.Add(("Saldo total", TotalTexto));
        doc.Kpis.Add(("Saldo vencido", VencidoTexto));
        doc.Kpis.Add(("% vencido", PorcentajeVencidoTexto));
        doc.Kpis.Add(("Mora promedio ponderada", MoraPromedioTexto));
        doc.Kpis.Add(("Documentos", CantidadDocumentos.ToString()));

        var sT = new SeccionReporte("Resumen por tramo",
            new ColumnaReporte("Tramo"), new ColumnaReporte("Documentos", TipoColumna.Numero),
            new ColumnaReporte("Saldo", TipoColumna.Moneda), new ColumnaReporte("% del total", TipoColumna.Porcentaje));
        foreach (var t in Tramos) sT.Fila(t.Nombre, t.Cantidad, t.Monto, t.Proporcion);
        sT.Total("TOTAL", CantidadDocumentos, _totalPorTramo.Sum(), 1d);
        doc.Secciones.Add(sT);

        var sC = new SeccionReporte($"Saldos por {EtiquetaTercero.ToLower()}",
            new ColumnaReporte(Formato.Capitalizar(EtiquetaTercero.ToLower())), new ColumnaReporte("RUT"),
            new ColumnaReporte(EstiloContable.Tramos[0], TipoColumna.Moneda), new ColumnaReporte(EstiloContable.Tramos[1], TipoColumna.Moneda),
            new ColumnaReporte(EstiloContable.Tramos[2], TipoColumna.Moneda), new ColumnaReporte(EstiloContable.Tramos[3], TipoColumna.Moneda),
            new ColumnaReporte(EstiloContable.Tramos[4], TipoColumna.Moneda), new ColumnaReporte("Total", TipoColumna.Moneda));
        foreach (var t in Terceros) sC.Fila(t.Tercero, t.Rut, t.Tramos[0], t.Tramos[1], t.Tramos[2], t.Tramos[3], t.Tramos[4], t.Total);
        sC.Total("TOTAL", "", _totalPorTramo[0], _totalPorTramo[1], _totalPorTramo[2], _totalPorTramo[3], _totalPorTramo[4], _totalPorTramo.Sum());
        doc.Secciones.Add(sC);

        var sD = new SeccionReporte("Detalle de documentos",
            new ColumnaReporte("Documento"), new ColumnaReporte(Formato.Capitalizar(EtiquetaTercero.ToLower())),
            new ColumnaReporte("Emisión", TipoColumna.Fecha), new ColumnaReporte("Vencimiento", TipoColumna.Fecha),
            new ColumnaReporte("Días vencido", TipoColumna.Numero), new ColumnaReporte("Tramo"),
            new ColumnaReporte("Monto", TipoColumna.Moneda), new ColumnaReporte("Saldo", TipoColumna.Moneda));
        foreach (var d in Documentos) sD.Fila(d.Documento, d.Tercero, d.FechaEmision, d.FechaVencimiento, Math.Max(0, d.DiasVencido), d.Tramo, d.MontoTotal, d.Saldo);
        doc.Secciones.Add(sD);
        return doc;
    }
}
