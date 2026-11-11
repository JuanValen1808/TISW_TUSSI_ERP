using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Contabilidad;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Contabilidad;

// RF-CON-03: Libro mayor y balances - Vista analítica por cuenta contable y cálculo de saldos finales.
public class LibroMayorViewModel : ReporteBaseViewModel
{
    private readonly ContabilidadService _contabilidadService;
    private List<LibroMayorItem> _todos = new();

    public ObservableCollection<LibroMayorItem> SaldosMayores { get; } = new();
    public ObservableCollection<MayorCentralizadoItem> MovimientosCuentaAnalitica { get; } = new();
    public List<string> TiposFiltro { get; } = new() { "Todos", "ACTIVO", "PASIVO", "PATRIMONIO", "INGRESO", "GASTO" };

    private DateTime _fechaDesde = new(DateTime.Today.Year, 1, 1);
    public DateTime FechaDesde { get => _fechaDesde; set => SetProperty(ref _fechaDesde, value); }

    private DateTime _fechaHasta = DateTime.Today.AddMonths(2);
    public DateTime FechaHasta { get => _fechaHasta; set => SetProperty(ref _fechaHasta, value); }

    private string _tipoSeleccionado = "Todos";
    public string TipoSeleccionado
    {
        get => _tipoSeleccionado;
        set { if (SetProperty(ref _tipoSeleccionado, value)) AplicarFiltro(); }
    }

    private string _busqueda = string.Empty;
    public string Busqueda
    {
        get => _busqueda;
        set { if (SetProperty(ref _busqueda, value)) AplicarFiltro(); }
    }

    private decimal _sumaTotalDebe;
    public decimal SumaTotalDebe
    {
        get => _sumaTotalDebe;
        set { if (SetProperty(ref _sumaTotalDebe, value)) OnPropertyChanged(nameof(SumaTotalDebeTexto)); }
    }
    public string SumaTotalDebeTexto => Formato.Moneda(SumaTotalDebe);

    private decimal _sumaTotalHaber;
    public decimal SumaTotalHaber
    {
        get => _sumaTotalHaber;
        set { if (SetProperty(ref _sumaTotalHaber, value)) OnPropertyChanged(nameof(SumaTotalHaberTexto)); }
    }
    public string SumaTotalHaberTexto => Formato.Moneda(SumaTotalHaber);

    private decimal _sumaTotalSaldoDeudor;
    public decimal SumaTotalSaldoDeudor
    {
        get => _sumaTotalSaldoDeudor;
        set { if (SetProperty(ref _sumaTotalSaldoDeudor, value)) OnPropertyChanged(nameof(SumaTotalSaldoDeudorTexto)); }
    }
    public string SumaTotalSaldoDeudorTexto => Formato.Moneda(SumaTotalSaldoDeudor);

    private decimal _sumaTotalSaldoAcreedor;
    public decimal SumaTotalSaldoAcreedor
    {
        get => _sumaTotalSaldoAcreedor;
        set { if (SetProperty(ref _sumaTotalSaldoAcreedor, value)) OnPropertyChanged(nameof(SumaTotalSaldoAcreedorTexto)); }
    }
    public string SumaTotalSaldoAcreedorTexto => Formato.Moneda(SumaTotalSaldoAcreedor);

    public bool Cuadrado => SumaTotalDebe == SumaTotalHaber && SumaTotalSaldoDeudor == SumaTotalSaldoAcreedor;
    public string EstadoCuadraturaTexto => Cuadrado ? "Cuadratura Mayor OK (Deudor = Acreedor)" : "Filtrado por rubro/búsqueda";

    // Modal Vista Analítica por Cuenta (Tarjeta Mayor T-Account)
    private bool _mostrarDetalleCuenta;
    public bool MostrarDetalleCuenta { get => _mostrarDetalleCuenta; set => SetProperty(ref _mostrarDetalleCuenta, value); }

    private LibroMayorItem? _cuentaActiva;
    public LibroMayorItem? CuentaActiva { get => _cuentaActiva; set => SetProperty(ref _cuentaActiva, value); }

    public ICommand CargarSaldosCommand { get; }
    public ICommand VerAnaliticoCuentaCommand { get; }
    public ICommand CerrarAnaliticoCommand { get; }

    public LibroMayorViewModel(ContabilidadService contabilidadService)
    {
        _contabilidadService = contabilidadService;
        Titulo = "Libro Mayor y Balances";
        CargarSaldosCommand = GenerarCommand;
        VerAnaliticoCuentaCommand = new Command<LibroMayorItem>(async item => await VerAnaliticoCuentaAsync(item));
        CerrarAnaliticoCommand = new Command(() => MostrarDetalleCuenta = false);
    }

    protected override async Task CargarDatosAsync()
    {
        _todos = await _contabilidadService.ObtenerLibroMayorAsync(FechaDesde, FechaHasta);
        AplicarFiltro();
    }

    private void AplicarFiltro()
    {
        var q = _todos.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(TipoSeleccionado) && TipoSeleccionado != "Todos")
            q = q.Where(x => string.Equals(x.TipoCuenta, TipoSeleccionado, StringComparison.OrdinalIgnoreCase));

        var txt = Busqueda.Trim();
        if (!string.IsNullOrEmpty(txt))
            q = q.Where(x => x.CodigoCuenta.Contains(txt, StringComparison.OrdinalIgnoreCase)
                          || x.NombreCuenta.Contains(txt, StringComparison.OrdinalIgnoreCase));

        SaldosMayores.Clear();
        foreach (var item in q)
            SaldosMayores.Add(item);

        SumaTotalDebe = SaldosMayores.Sum(x => x.TotalDebe);
        SumaTotalHaber = SaldosMayores.Sum(x => x.TotalHaber);
        SumaTotalSaldoDeudor = SaldosMayores.Sum(x => x.SaldoDeudor);
        SumaTotalSaldoAcreedor = SaldosMayores.Sum(x => x.SaldoAcreedor);
        OnPropertyChanged(nameof(Cuadrado));
        OnPropertyChanged(nameof(EstadoCuadraturaTexto));
        HayDatos = SaldosMayores.Count > 0;
    }

    private async Task VerAnaliticoCuentaAsync(LibroMayorItem? cuenta)
    {
        if (cuenta == null) return;
        try
        {
            CuentaActiva = cuenta;
            MovimientosCuentaAnalitica.Clear();
            var movs = await _contabilidadService.ObtenerMayorCentralizadoAsync(cuenta.IdCuenta, FechaDesde, FechaHasta);
            foreach (var m in movs)
                MovimientosCuentaAnalitica.Add(m);
            MostrarDetalleCuenta = true;
        }
        catch (Exception ex)
        {
            MensajeError = $"No se pudo abrir la ficha analítica de la cuenta: {ex.Message}";
        }
    }

    protected override DocumentoReporte ConstruirDocumento()
    {
        var doc = new DocumentoReporte
        {
            Titulo = "Libro Mayor y Balances",
            Subtitulo = $"Vista analítica de sumas y saldos finales por cuenta contable · {Rango(FechaDesde, FechaHasta)}"
        };
        doc.Filtros.Add(("Período", Rango(FechaDesde, FechaHasta)));
        doc.Filtros.Add(("Tipo de Cuenta", TipoSeleccionado));
        doc.Kpis.Add(("Cuentas con movimiento", SaldosMayores.Count.ToString()));
        doc.Kpis.Add(("Suma Débitos", SumaTotalDebeTexto));
        doc.Kpis.Add(("Suma Créditos", SumaTotalHaberTexto));
        doc.Kpis.Add(("Saldo Deudor", SumaTotalSaldoDeudorTexto));
        doc.Kpis.Add(("Saldo Acreedor", SumaTotalSaldoAcreedorTexto));

        var s = new SeccionReporte("Saldos del Libro Mayor",
            new ColumnaReporte("Código"),
            new ColumnaReporte("Cuenta Contable"),
            new ColumnaReporte("Rubro"),
            new ColumnaReporte("Débitos (Debe)", TipoColumna.Moneda),
            new ColumnaReporte("Créditos (Haber)", TipoColumna.Moneda),
            new ColumnaReporte("Saldo Deudor", TipoColumna.Moneda),
            new ColumnaReporte("Saldo Acreedor", TipoColumna.Moneda));

        foreach (var item in SaldosMayores)
            s.Fila(item.CodigoCuenta, item.NombreCuenta, item.TipoTexto, item.TotalDebe, item.TotalHaber, item.SaldoDeudor, item.SaldoAcreedor);

        s.Total("", "TOTALES DE CUADRATURA", "", SumaTotalDebe, SumaTotalHaber, SumaTotalSaldoDeudor, SumaTotalSaldoAcreedor);
        doc.Secciones.Add(s);
        return doc;
    }
}
