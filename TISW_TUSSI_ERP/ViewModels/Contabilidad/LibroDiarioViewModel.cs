using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Auth;
using TISW_TUSSI_ERP.Models.Contabilidad;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Contabilidad;

// RF-CON-02: Libro diario - Asientos automáticos generados desde compras, ventas e inventario, además del registro de asientos manuales.
public class LibroDiarioViewModel : ReporteBaseViewModel
{
    private readonly ContabilidadService _contabilidadService;
    private List<LibroDiarioAsiento> _todos = new();

    public ObservableCollection<LibroDiarioAsiento> Asientos { get; } = new();
    public ObservableCollection<PlanCuentas> CuentasImputables { get; } = new();
    public ObservableCollection<LineaAsientoInput> LineasNuevoAsiento { get; } = new();

    public List<string> OrigenesFiltro { get; } = new() { "Todos", "VENTA", "COMPRA", "RECEPCION", "AJUSTE", "OTRO" };
    public List<string> OrigenesManuales { get; } = new() { "AJUSTE", "OTRO", "VENTA", "COMPRA", "RECEPCION" };

    private DateTime _fechaDesde = new(DateTime.Today.Year, 1, 1);
    public DateTime FechaDesde { get => _fechaDesde; set => SetProperty(ref _fechaDesde, value); }

    private DateTime _fechaHasta = DateTime.Today.AddMonths(2);
    public DateTime FechaHasta { get => _fechaHasta; set => SetProperty(ref _fechaHasta, value); }

    private string _origenSeleccionado = "Todos";
    public string OrigenSeleccionado
    {
        get => _origenSeleccionado;
        set { if (SetProperty(ref _origenSeleccionado, value)) AplicarFiltro(); }
    }

    private string _busqueda = string.Empty;
    public string Busqueda
    {
        get => _busqueda;
        set { if (SetProperty(ref _busqueda, value)) AplicarFiltro(); }
    }

    // KPIs de cuadratura y centralización
    private int _cantidadAsientos;
    public int CantidadAsientos { get => _cantidadAsientos; set => SetProperty(ref _cantidadAsientos, value); }

    private string _totalDebeTexto = "$0";
    public string TotalDebeTexto { get => _totalDebeTexto; set => SetProperty(ref _totalDebeTexto, value); }

    private string _totalHaberTexto = "$0";
    public string TotalHaberTexto { get => _totalHaberTexto; set => SetProperty(ref _totalHaberTexto, value); }

    private string _estadoCuadratura = "Cuadrado";
    public string EstadoCuadratura { get => _estadoCuadratura; set => SetProperty(ref _estadoCuadratura, value); }

    private Color _colorCuadratura = Color.FromArgb("#047857");
    public Color ColorCuadratura { get => _colorCuadratura; set => SetProperty(ref _colorCuadratura, value); }

    private int _pendientesCentralizar;
    public int PendientesCentralizar
    {
        get => _pendientesCentralizar;
        set
        {
            if (SetProperty(ref _pendientesCentralizar, value))
                OnPropertyChanged(nameof(HayPendientesCentralizar));
        }
    }
    public bool HayPendientesCentralizar => PendientesCentralizar > 0;

    private string _detallePendientesTexto = "Sin documentos pendientes";
    public string DetallePendientesTexto { get => _detallePendientesTexto; set => SetProperty(ref _detallePendientesTexto, value); }

    // Modal de Registro de Asiento Manual (Partida Doble)
    private bool _mostrarModalAsiento;
    public bool MostrarModalAsiento { get => _mostrarModalAsiento; set => SetProperty(ref _mostrarModalAsiento, value); }

    private DateTime _nuevoFecha = DateTime.Today;
    public DateTime NuevoFecha { get => _nuevoFecha; set => SetProperty(ref _nuevoFecha, value); }

    private string _nuevoOrigen = "AJUSTE";
    public string NuevoOrigen { get => _nuevoOrigen; set => SetProperty(ref _nuevoOrigen, value); }

    private string _nuevaGlosa = string.Empty;
    public string NuevaGlosa { get => _nuevaGlosa; set => SetProperty(ref _nuevaGlosa, value); }

    private PlanCuentas? _cuentaLineaSeleccionada;
    public PlanCuentas? CuentaLineaSeleccionada { get => _cuentaLineaSeleccionada; set => SetProperty(ref _cuentaLineaSeleccionada, value); }

    private string _montoDebeLinea = string.Empty;
    public string MontoDebeLinea { get => _montoDebeLinea; set => SetProperty(ref _montoDebeLinea, value); }

    private string _montoHaberLinea = string.Empty;
    public string MontoHaberLinea { get => _montoHaberLinea; set => SetProperty(ref _montoHaberLinea, value); }

    public decimal TotalDebeNuevo => LineasNuevoAsiento.Sum(l => l.Debe);
    public decimal TotalHaberNuevo => LineasNuevoAsiento.Sum(l => l.Haber);
    public decimal DiferenciaNuevo => Math.Abs(TotalDebeNuevo - TotalHaberNuevo);
    public bool NuevoEstaCuadrado => TotalDebeNuevo > 0 && TotalDebeNuevo == TotalHaberNuevo && LineasNuevoAsiento.Count >= 2;
    public string TotalDebeNuevoTexto => Formato.Moneda(TotalDebeNuevo);
    public string TotalHaberNuevoTexto => Formato.Moneda(TotalHaberNuevo);
    public string EstadoNuevoAsientoTexto => NuevoEstaCuadrado
        ? "✓ Partida Doble Cuadrada (Debe = Haber)"
        : $"Diferencia por cuadrar: {Formato.Moneda(DiferenciaNuevo)} (mín. 2 líneas)";
    public Color ColorEstadoNuevoAsiento => NuevoEstaCuadrado ? Color.FromArgb("#047857") : Color.FromArgb("#BE123C");

    public ICommand CargarAsientosCommand { get; }
    public ICommand CentralizarAutomaticoCommand { get; }
    public ICommand AbrirModalAsientoCommand { get; }
    public ICommand CerrarModalAsientoCommand { get; }
    public ICommand AgregarLineaCommand { get; }
    public ICommand EliminarLineaCommand { get; }
    public ICommand GuardarAsientoManualCommand { get; }

    public LibroDiarioViewModel(ContabilidadService contabilidadService)
    {
        _contabilidadService = contabilidadService;
        Titulo = "Libro Diario Oficial";
        CargarAsientosCommand = GenerarCommand;
        CentralizarAutomaticoCommand = new Command(async () => await CentralizarPendientesAsync());
        AbrirModalAsientoCommand = new Command(async () => await AbrirModalNuevoAsientoAsync());
        CerrarModalAsientoCommand = new Command(() => MostrarModalAsiento = false);
        AgregarLineaCommand = new Command(AgregarLineaAsiento);
        EliminarLineaCommand = new Command<LineaAsientoInput>(QuitarLineaAsiento);
        GuardarAsientoManualCommand = new Command(async () => await GuardarAsientoManualAsync());
    }

    protected override async Task CargarDatosAsync()
    {
        await _contabilidadService.AsegurarPlanCuentasChilenoAsync();
        _todos = await _contabilidadService.ObtenerLibroDiarioAsync(FechaDesde, FechaHasta, null);

        var pend = await _contabilidadService.ObtenerPendientesCentralizarAsync();
        PendientesCentralizar = pend.TotalPendientes;
        DetallePendientesTexto = pend.HayPendientes
            ? $"{pend.VentasSinCentralizar} ventas, {pend.ComprasSinCentralizar} compras y {pend.MermasSinCentralizar} mermas listas para centralizar"
            : "Ventas, compras y mermas 100% centralizadas";

        AplicarFiltro();
    }

    private void AplicarFiltro()
    {
        var q = _todos.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(OrigenSeleccionado) && OrigenSeleccionado != "Todos")
            q = q.Where(a => string.Equals(a.Origen, OrigenSeleccionado, StringComparison.OrdinalIgnoreCase));

        var txt = Busqueda.Trim();
        if (!string.IsNullOrEmpty(txt))
        {
            q = q.Where(a => a.GlosaDescripcion.Contains(txt, StringComparison.OrdinalIgnoreCase)
                          || a.NumeroAsiento.ToString().Contains(txt)
                          || a.Usuario.Contains(txt, StringComparison.OrdinalIgnoreCase)
                          || a.Lineas.Any(l => l.NombreCuenta.Contains(txt, StringComparison.OrdinalIgnoreCase)
                                            || l.CodigoCuenta.Contains(txt, StringComparison.OrdinalIgnoreCase)));
        }

        var filtrados = q.ToList();
        Asientos.Clear();
        foreach (var a in filtrados) Asientos.Add(a);

        var sumDebe = filtrados.Sum(a => a.TotalDebe);
        var sumHaber = filtrados.Sum(a => a.TotalHaber);

        CantidadAsientos = filtrados.Count;
        TotalDebeTexto = Formato.Moneda(sumDebe);
        TotalHaberTexto = Formato.Moneda(sumHaber);
        bool ok = sumDebe == sumHaber;
        EstadoCuadratura = filtrados.Count == 0 ? "Sin movimientos" : (ok ? "Partida Doble Cuadrada" : "Descuadre Detectado");
        ColorCuadratura = ok ? Color.FromArgb("#047857") : Color.FromArgb("#BE123C");
        HayDatos = filtrados.Count > 0;
    }

    private async Task CentralizarPendientesAsync()
    {
        if (EstaOcupado) return;
        try
        {
            EstaOcupado = true;
            MensajeError = string.Empty;
            MensajeExito = string.Empty;

            int idUsuario = Sesion.UsuarioActual?.IdUsuario ?? 1;
            var (v, c, m) = await _contabilidadService.CentralizarMovimientosPendientesAsync(idUsuario);
            int total = v + c + m;

            EstaOcupado = false;
            await GenerarAsync();

            MensajeExito = total > 0
                ? $"Se generaron {total} asientos automáticos en el Libro Diario ({v} de Ventas con IVA/Costo, {c} de Compras con IVA Crédito y {m} de Mermas de Inventario)."
                : "Todos los documentos de Ventas, Compras e Inventario ya se encuentran centralizados en el Libro Diario.";
        }
        catch (Exception ex)
        {
            EstaOcupado = false;
            MensajeError = $"Error al centralizar movimientos automáticos: {ex.Message}";
        }
    }

    private async Task AbrirModalNuevoAsientoAsync()
    {
        MensajeError = string.Empty;
        MensajeExito = string.Empty;

        var cuentas = await _contabilidadService.ObtenerPlanCuentasAsync();
        CuentasImputables.Clear();
        foreach (var c in cuentas.Where(x => !x.EsGrupo))
            CuentasImputables.Add(c);

        CuentaLineaSeleccionada = CuentasImputables.FirstOrDefault();
        NuevoFecha = DateTime.Today;
        NuevoOrigen = "AJUSTE";
        NuevaGlosa = string.Empty;
        MontoDebeLinea = string.Empty;
        MontoHaberLinea = string.Empty;
        LineasNuevoAsiento.Clear();
        NotificarTotalesNuevoAsiento();
        MostrarModalAsiento = true;
    }

    private void AgregarLineaAsiento()
    {
        if (CuentaLineaSeleccionada == null)
        {
            MensajeError = "Selecciona una cuenta contable imputable.";
            return;
        }

        decimal.TryParse(MontoDebeLinea.Replace(".", "").Replace(",", "."), out var debe);
        decimal.TryParse(MontoHaberLinea.Replace(".", "").Replace(",", "."), out var haber);

        if ((debe <= 0 && haber <= 0) || (debe > 0 && haber > 0))
        {
            MensajeError = "Cada línea debe registrar un monto en el Debe O en el Haber (no ambos al mismo tiempo).";
            return;
        }

        MensajeError = string.Empty;
        LineasNuevoAsiento.Add(new LineaAsientoInput
        {
            IdCuenta = CuentaLineaSeleccionada.IdCuenta,
            CodigoCuenta = CuentaLineaSeleccionada.CodigoCuenta,
            NombreCuenta = CuentaLineaSeleccionada.NombreCuenta,
            Debe = debe,
            Haber = haber
        });

        MontoDebeLinea = string.Empty;
        MontoHaberLinea = string.Empty;
        NotificarTotalesNuevoAsiento();
    }

    private void QuitarLineaAsiento(LineaAsientoInput? linea)
    {
        if (linea == null) return;
        LineasNuevoAsiento.Remove(linea);
        NotificarTotalesNuevoAsiento();
    }

    private void NotificarTotalesNuevoAsiento()
    {
        OnPropertyChanged(nameof(TotalDebeNuevo));
        OnPropertyChanged(nameof(TotalHaberNuevo));
        OnPropertyChanged(nameof(DiferenciaNuevo));
        OnPropertyChanged(nameof(NuevoEstaCuadrado));
        OnPropertyChanged(nameof(TotalDebeNuevoTexto));
        OnPropertyChanged(nameof(TotalHaberNuevoTexto));
        OnPropertyChanged(nameof(EstadoNuevoAsientoTexto));
        OnPropertyChanged(nameof(ColorEstadoNuevoAsiento));
    }

    private async Task GuardarAsientoManualAsync()
    {
        if (string.IsNullOrWhiteSpace(NuevaGlosa))
        {
            MensajeError = "Ingresa la glosa explicativa del asiento contable (exigencia Art. 25 Código de Comercio / SII).";
            return;
        }

        if (!NuevoEstaCuadrado)
        {
            MensajeError = "El asiento no cumple con la Partida Doble (Total Debe debe ser igual a Total Haber y tener al menos 2 líneas).";
            return;
        }

        try
        {
            int idUsuario = Sesion.UsuarioActual?.IdUsuario ?? 1;
            await _contabilidadService.RegistrarAsientoManualAsync(NuevoFecha, NuevaGlosa, NuevoOrigen, idUsuario, LineasNuevoAsiento.ToList());
            MostrarModalAsiento = false;
            await GenerarAsync();
            MensajeExito = "Asiento contable manual registrado exitosamente en el Libro Diario.";
        }
        catch (Exception ex)
        {
            MensajeError = $"No se pudo registrar el asiento: {ex.Message}";
        }
    }

    protected override DocumentoReporte ConstruirDocumento()
    {
        var doc = new DocumentoReporte
        {
            Titulo = "Libro Diario General (SII Chile)",
            Subtitulo = $"Registro cronológico de asientos automáticos y manuales · {Rango(FechaDesde, FechaHasta)}"
        };
        doc.Filtros.Add(("Período", Rango(FechaDesde, FechaHasta)));
        doc.Filtros.Add(("Origen", OrigenSeleccionado));
        doc.Kpis.Add(("Asientos", CantidadAsientos.ToString()));
        doc.Kpis.Add(("Total Debe", TotalDebeTexto));
        doc.Kpis.Add(("Total Haber", TotalHaberTexto));
        doc.Kpis.Add(("Estado", EstadoCuadratura));

        var s = new SeccionReporte("Asientos del Libro Diario",
            new ColumnaReporte("Fecha", TipoColumna.Fecha),
            new ColumnaReporte("Folio"),
            new ColumnaReporte("Origen"),
            new ColumnaReporte("Código"),
            new ColumnaReporte("Cuenta Contable / Glosa"),
            new ColumnaReporte("Debe", TipoColumna.Moneda),
            new ColumnaReporte("Haber", TipoColumna.Moneda));

        foreach (var a in Asientos)
        {
            s.Subtitulo($"{a.NumeroTexto} · {a.FechaTexto} · [{a.Origen}] · {a.GlosaTexto} (por {a.UsuarioTexto})");
            foreach (var l in a.Lineas)
            {
                s.Filas.Add(new FilaReporte
                {
                    Celdas = new object?[] { a.FechaAsiento, a.NumeroTexto, a.Origen, l.CodigoCuenta, l.CuentaTexto, l.Debe, l.Haber },
                    Nivel = l.EsHaber ? 1 : 0
                });
            }
            s.Total(null, "", "", "", $"Subtotal {a.NumeroTexto}", a.TotalDebe, a.TotalHaber);
        }

        s.Total(null, "", "", "", "SUMAS IGUALES DEL LIBRO DIARIO", Asientos.Sum(x => x.TotalDebe), Asientos.Sum(x => x.TotalHaber));
        doc.Secciones.Add(s);
        return doc;
    }
}
