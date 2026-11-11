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

// RF-CON-07: Gestión de cuentas por cobrar y pagar - Registro, consulta y seguimiento de obligaciones pendientes de cobro a clientes, con actualización automática de saldos y asientos al registrar pagos.
public class CuentasPorCobrarViewModel : ReporteBaseViewModel
{
    private readonly ContabilidadService _contabilidadService;
    private List<CuentaPorCobrar> _todos = new();

    public ObservableCollection<CuentaPorCobrar> Documentos { get; } = new();
    public List<string> EstadosFiltro { get; } = new() { "Todos", "Pendiente", "Vencido", "Pagado" };
    public List<string> MediosPago { get; } = new() { "TRANSFERENCIA", "EFECTIVO", "TARJETA" };

    private string _estadoSeleccionado = "Todos";
    public string EstadoSeleccionado
    {
        get => _estadoSeleccionado;
        set { if (SetProperty(ref _estadoSeleccionado, value)) AplicarFiltro(); }
    }

    private string _busqueda = string.Empty;
    public string Busqueda
    {
        get => _busqueda;
        set { if (SetProperty(ref _busqueda, value)) AplicarFiltro(); }
    }

    private decimal _totalCobrar;
    public decimal TotalCobrar { get => _totalCobrar; set { if (SetProperty(ref _totalCobrar, value)) OnPropertyChanged(nameof(TotalCobrarTexto)); } }
    public string TotalCobrarTexto => Formato.Moneda(TotalCobrar);

    private string _totalVencidoTexto = "$0";
    public string TotalVencidoTexto { get => _totalVencidoTexto; set => SetProperty(ref _totalVencidoTexto, value); }

    private string _totalRecaudadoTexto = "$0";
    public string TotalRecaudadoTexto { get => _totalRecaudadoTexto; set => SetProperty(ref _totalRecaudadoTexto, value); }

    private int _documentosPendientesCount;
    public int DocumentosPendientesCount { get => _documentosPendientesCount; set => SetProperty(ref _documentosPendientesCount, value); }

    // Modal de Registro de Cobro a Cliente
    private bool _mostrarModalCobro;
    public bool MostrarModalCobro { get => _mostrarModalCobro; set => SetProperty(ref _mostrarModalCobro, value); }

    private CuentaPorCobrar? _documentoSeleccionado;
    public CuentaPorCobrar? DocumentoSeleccionado { get => _documentoSeleccionado; set => SetProperty(ref _documentoSeleccionado, value); }

    private string _montoAbonoTexto = string.Empty;
    public string MontoAbonoTexto { get => _montoAbonoTexto; set => SetProperty(ref _montoAbonoTexto, value); }

    private string _medioPagoSeleccionado = "TRANSFERENCIA";
    public string MedioPagoSeleccionado { get => _medioPagoSeleccionado; set => SetProperty(ref _medioPagoSeleccionado, value); }

    private string _referenciaCobro = string.Empty;
    public string ReferenciaCobro { get => _referenciaCobro; set => SetProperty(ref _referenciaCobro, value); }

    // Alias de propiedades y comandos para XAML
    public string TextoBusqueda { get => Busqueda; set { Busqueda = value; OnPropertyChanged(); } }
    public string TotalPorCobrarTexto => TotalCobrarTexto;
    public int CantidadPendientes => DocumentosPendientesCount;
    public int CantidadVencidas => _todos.Count(d => d.Estado == "Vencido");
    public string TotalVigenteTexto => Formato.Moneda(_todos.Where(d => d.Estado == "Pendiente").Sum(d => d.SaldoPendiente));
    public string MontoCobroTexto { get => MontoAbonoTexto; set { MontoAbonoTexto = value; OnPropertyChanged(); } }
    public List<string> MediosCobro => MediosPago;
    public string MedioCobro { get => MedioPagoSeleccionado; set { MedioPagoSeleccionado = value; OnPropertyChanged(); } }

    public ICommand CargarDocumentosCommand { get; }
    public ICommand RefrescarCommand => GenerarCommand;
    public ICommand AbrirCobroCommand { get; }
    public ICommand AbrirModalCobroCommand => AbrirCobroCommand;
    public ICommand FiltrarEstadoCommand { get; }
    public ICommand CerrarModalCobroCommand { get; }
    public ICommand ConfirmarCobroCommand { get; }
    public ICommand PagarTotalRapidoCommand { get; }

    public CuentasPorCobrarViewModel(ContabilidadService contabilidadService)
    {
        _contabilidadService = contabilidadService;
        Titulo = "Cuentas por Cobrar (Clientes)";
        CargarDocumentosCommand = GenerarCommand;
        AbrirCobroCommand = new Command<CuentaPorCobrar>(AbrirModalCobro);
        FiltrarEstadoCommand = new Command<string>(est =>
        {
            EstadoSeleccionado = est?.ToUpperInvariant() switch
            {
                "PENDIENTES" => "Pendientes",
                "VENCIDO" => "Vencido",
                "VIGENTE" => "Pendiente",
                "PAGADO" => "Pagado",
                _ => "Todos"
            };
        });
        CerrarModalCobroCommand = new Command(() => MostrarModalCobro = false);
        ConfirmarCobroCommand = new Command(async () => await ConfirmarCobroAsync());
        PagarTotalRapidoCommand = new Command(() =>
        {
            if (DocumentoSeleccionado != null)
                MontoAbonoTexto = DocumentoSeleccionado.SaldoPendiente.ToString("0");
        });
    }

    public async Task CargarDocumentosAsync() => await GenerarAsync();

    protected override async Task CargarDatosAsync()
    {
        _todos = await _contabilidadService.ObtenerCuentasPorCobrarAsync();
        AplicarFiltro();
    }

    private void AplicarFiltro()
    {
        var q = _todos.AsEnumerable();
        if (EstadoSeleccionado == "Pendientes")
            q = q.Where(d => d.SaldoPendiente > 0);
        else if (!string.IsNullOrWhiteSpace(EstadoSeleccionado) && EstadoSeleccionado != "Todos")
            q = q.Where(d => string.Equals(d.Estado, EstadoSeleccionado, StringComparison.OrdinalIgnoreCase));

        var txt = Busqueda.Trim();
        if (!string.IsNullOrEmpty(txt))
            q = q.Where(d => d.Cliente.Contains(txt, StringComparison.OrdinalIgnoreCase)
                          || d.RutCliente.Contains(txt, StringComparison.OrdinalIgnoreCase)
                          || d.NumeroDocumento.Contains(txt, StringComparison.OrdinalIgnoreCase)
                          || d.Convenio.Contains(txt, StringComparison.OrdinalIgnoreCase));

        Documentos.Clear();
        foreach (var d in q)
            Documentos.Add(d);

        TotalCobrar = _todos.Sum(d => d.SaldoPendiente);
        TotalVencidoTexto = Formato.Moneda(_todos.Where(d => d.Estado == "Vencido").Sum(d => d.SaldoPendiente));
        TotalRecaudadoTexto = Formato.Moneda(_todos.Sum(d => d.MontoPagado));
        DocumentosPendientesCount = _todos.Count(d => d.SaldoPendiente > 0);
        foreach (var p in new[] { nameof(TotalPorCobrarTexto), nameof(CantidadPendientes), nameof(CantidadVencidas), nameof(TotalVigenteTexto) })
            OnPropertyChanged(p);
        HayDatos = Documentos.Count > 0;
    }

    private void AbrirModalCobro(CuentaPorCobrar? doc)
    {
        if (doc == null || doc.SaldoPendiente <= 0) return;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        DocumentoSeleccionado = doc;
        MontoAbonoTexto = doc.SaldoPendiente.ToString("0");
        MedioPagoSeleccionado = "TRANSFERENCIA";
        MostrarModalCobro = true;
    }

    private async Task ConfirmarCobroAsync()
    {
        if (DocumentoSeleccionado == null) return;
        if (!decimal.TryParse(MontoAbonoTexto.Replace(".", "").Replace(",", "."), out var monto) || monto <= 0)
        {
            MensajeError = "Ingresa un monto de cobro válido mayor a $0.";
            return;
        }

        try
        {
            int idUsuario = Sesion.UsuarioActual?.IdUsuario ?? 1;
            string folio = DocumentoSeleccionado.NumeroDocumento;
            string cliente = DocumentoSeleccionado.Cliente;

            await _contabilidadService.RegistrarCobroClienteAsync(DocumentoSeleccionado.IdDocumento, monto, MedioPagoSeleccionado, idUsuario);
            MostrarModalCobro = false;
            await GenerarAsync();
            MensajeExito = $"Cobro de {Formato.Moneda(monto)} registrado para {cliente} (Doc. {folio}). Saldo actualizado y asiento generado en el Libro Diario.";
        }
        catch (Exception ex)
        {
            MensajeError = ex.Message;
        }
    }

    protected override DocumentoReporte ConstruirDocumento()
    {
        var doc = new DocumentoReporte
        {
            Titulo = "Cartera de Cuentas por Cobrar (Clientes y Convenios)",
            Subtitulo = $"Control de facturación al crédito, convenios de salud y recaudación al {DateTime.Today:dd/MM/yyyy}",
            Horizontal = true
        };
        doc.Filtros.Add(("Estado", EstadoSeleccionado));
        doc.Kpis.Add(("Saldo por Cobrar", TotalCobrarTexto));
        doc.Kpis.Add(("Mora Vencida", TotalVencidoTexto));
        doc.Kpis.Add(("Total Recaudado", TotalRecaudadoTexto));
        doc.Kpis.Add(("Docs. Pendientes", DocumentosPendientesCount.ToString()));

        var s = new SeccionReporte("Detalle de Cuentas por Cobrar",
            new ColumnaReporte("Folio"),
            new ColumnaReporte("Tipo"),
            new ColumnaReporte("RUT Cliente"),
            new ColumnaReporte("Cliente / Institución"),
            new ColumnaReporte("Convenio"),
            new ColumnaReporte("Emisión", TipoColumna.Fecha),
            new ColumnaReporte("Vencimiento", TipoColumna.Fecha),
            new ColumnaReporte("Monto Total", TipoColumna.Moneda),
            new ColumnaReporte("Cobrado", TipoColumna.Moneda),
            new ColumnaReporte("Saldo Pendiente", TipoColumna.Moneda),
            new ColumnaReporte("Estado"));

        foreach (var d in Documentos)
            s.Fila(d.NumeroDocumento, d.TipoDocumento, d.RutCliente, d.Cliente, d.Convenio, d.FechaEmision, d.FechaVencimiento, d.MontoTotal, d.MontoPagado, d.SaldoPendiente, d.Estado);

        s.Total("TOTALES", "", "", "", "", null, null, Documentos.Sum(x => x.MontoTotal), Documentos.Sum(x => x.MontoPagado), Documentos.Sum(x => x.SaldoPendiente), "");
        doc.Secciones.Add(s);
        return doc;
    }
}
