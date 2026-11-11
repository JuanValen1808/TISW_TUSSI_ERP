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

// RF-CON-07: Gestión de cuentas por cobrar y pagar - Registro, consulta y seguimiento de obligaciones de pago a proveedores con actualización automática de saldos y asientos al registrar pagos.
public class CuentasPorPagarViewModel : ReporteBaseViewModel
{
    private readonly ContabilidadService _contabilidadService;
    private List<CuentaPorPagar> _todos = new();

    public ObservableCollection<CuentaPorPagar> Documentos { get; } = new();
    public List<string> EstadosFiltro { get; } = new() { "Todos", "Pendiente", "Vencido", "Pagado" };
    public List<string> MediosPago { get; } = new() { "TRANSFERENCIA", "EFECTIVO", "CHEQUE" };

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

    private decimal _totalPagar;
    public decimal TotalPagar { get => _totalPagar; set { if (SetProperty(ref _totalPagar, value)) OnPropertyChanged(nameof(TotalPagarTexto)); } }
    public string TotalPagarTexto => Formato.Moneda(TotalPagar);

    private string _totalVencidoTexto = "$0";
    public string TotalVencidoTexto { get => _totalVencidoTexto; set => SetProperty(ref _totalVencidoTexto, value); }

    private string _totalPagadoTexto = "$0";
    public string TotalPagadoTexto { get => _totalPagadoTexto; set => SetProperty(ref _totalPagadoTexto, value); }

    private int _documentosPendientesCount;
    public int DocumentosPendientesCount { get => _documentosPendientesCount; set => SetProperty(ref _documentosPendientesCount, value); }

    // Modal de Registro de Pago a Proveedor
    private bool _mostrarModalPago;
    public bool MostrarModalPago { get => _mostrarModalPago; set => SetProperty(ref _mostrarModalPago, value); }

    private CuentaPorPagar? _documentoSeleccionado;
    public CuentaPorPagar? DocumentoSeleccionado { get => _documentoSeleccionado; set => SetProperty(ref _documentoSeleccionado, value); }

    private string _montoAbonoTexto = string.Empty;
    public string MontoAbonoTexto { get => _montoAbonoTexto; set => SetProperty(ref _montoAbonoTexto, value); }

    private string _medioPagoSeleccionado = "TRANSFERENCIA";
    public string MedioPagoSeleccionado { get => _medioPagoSeleccionado; set => SetProperty(ref _medioPagoSeleccionado, value); }

    private string _referenciaPago = string.Empty;
    public string ReferenciaPago { get => _referenciaPago; set => SetProperty(ref _referenciaPago, value); }

    // Alias de propiedades y comandos para XAML
    public string TextoBusqueda { get => Busqueda; set { Busqueda = value; OnPropertyChanged(); } }
    public string TotalPorPagarTexto => TotalPagarTexto;
    public int CantidadPendientes => DocumentosPendientesCount;
    public int CantidadVencidas => _todos.Count(d => d.Estado == "Vencido");
    public string TotalVigenteTexto => Formato.Moneda(_todos.Where(d => d.Estado == "Pendiente").Sum(d => d.SaldoPendiente));
    public string TotalPagadoHistoricoTexto => TotalPagadoTexto;
    public string MontoPagoTexto { get => MontoAbonoTexto; set { MontoAbonoTexto = value; OnPropertyChanged(); } }
    public string MedioPago { get => MedioPagoSeleccionado; set { MedioPagoSeleccionado = value; OnPropertyChanged(); } }

    public ICommand CargarDocumentosCommand { get; }
    public ICommand RefrescarCommand => GenerarCommand;
    public ICommand AbrirPagoCommand { get; }
    public ICommand AbrirModalPagoCommand => AbrirPagoCommand;
    public ICommand FiltrarEstadoCommand { get; }
    public ICommand CerrarModalPagoCommand { get; }
    public ICommand ConfirmarPagoCommand { get; }
    public ICommand PagarTotalRapidoCommand { get; }

    public CuentasPorPagarViewModel(ContabilidadService contabilidadService)
    {
        _contabilidadService = contabilidadService;
        Titulo = "Cuentas por Pagar (Proveedores)";
        CargarDocumentosCommand = GenerarCommand;
        AbrirPagoCommand = new Command<CuentaPorPagar>(AbrirModalPago);
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
        CerrarModalPagoCommand = new Command(() => MostrarModalPago = false);
        ConfirmarPagoCommand = new Command(async () => await ConfirmarPagoAsync());
        PagarTotalRapidoCommand = new Command(() =>
        {
            if (DocumentoSeleccionado != null)
                MontoAbonoTexto = DocumentoSeleccionado.SaldoPendiente.ToString("0");
        });
    }

    public async Task CargarDocumentosAsync() => await GenerarAsync();

    protected override async Task CargarDatosAsync()
    {
        _todos = await _contabilidadService.ObtenerCuentasPorPagarAsync();
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
            q = q.Where(d => d.Proveedor.Contains(txt, StringComparison.OrdinalIgnoreCase)
                          || d.RutProveedor.Contains(txt, StringComparison.OrdinalIgnoreCase)
                          || d.NumeroDocumento.Contains(txt, StringComparison.OrdinalIgnoreCase));

        Documentos.Clear();
        foreach (var d in q)
            Documentos.Add(d);

        TotalPagar = _todos.Sum(d => d.SaldoPendiente);
        TotalVencidoTexto = Formato.Moneda(_todos.Where(d => d.Estado == "Vencido").Sum(d => d.SaldoPendiente));
        TotalPagadoTexto = Formato.Moneda(_todos.Sum(d => d.MontoPagado));
        DocumentosPendientesCount = _todos.Count(d => d.SaldoPendiente > 0);
        foreach (var p in new[] { nameof(TotalPorPagarTexto), nameof(CantidadPendientes), nameof(CantidadVencidas), nameof(TotalVigenteTexto), nameof(TotalPagadoHistoricoTexto) })
            OnPropertyChanged(p);
        HayDatos = Documentos.Count > 0;
    }

    private void AbrirModalPago(CuentaPorPagar? doc)
    {
        if (doc == null || doc.SaldoPendiente <= 0) return;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        DocumentoSeleccionado = doc;
        MontoAbonoTexto = doc.SaldoPendiente.ToString("0");
        MedioPagoSeleccionado = "TRANSFERENCIA";
        MostrarModalPago = true;
    }

    private async Task ConfirmarPagoAsync()
    {
        if (DocumentoSeleccionado == null) return;
        if (!decimal.TryParse(MontoAbonoTexto.Replace(".", "").Replace(",", "."), out var monto) || monto <= 0)
        {
            MensajeError = "Ingresa un monto de pago válido mayor a $0.";
            return;
        }

        try
        {
            int idUsuario = Sesion.UsuarioActual?.IdUsuario ?? 1;
            string docNum = DocumentoSeleccionado.NumeroDocumento;
            string prov = DocumentoSeleccionado.Proveedor;

            await _contabilidadService.RegistrarPagoProveedorAsync(DocumentoSeleccionado.IdDocumento, monto, MedioPagoSeleccionado, idUsuario);
            MostrarModalPago = false;
            await GenerarAsync();
            MensajeExito = $"Pago de {Formato.Moneda(monto)} registrado a {prov} (Factura {docNum}). Saldo actualizado y asiento de egreso contabilizado en el Libro Diario.";
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
            Titulo = "Obligaciones de Cuentas por Pagar (Proveedores)",
            Subtitulo = $"Control de facturas de compra, abonos y saldos por pagar al {DateTime.Today:dd/MM/yyyy}",
            Horizontal = true
        };
        doc.Filtros.Add(("Estado", EstadoSeleccionado));
        doc.Kpis.Add(("Saldo por Pagar", TotalPagarTexto));
        doc.Kpis.Add(("Deuda Vencida", TotalVencidoTexto));
        doc.Kpis.Add(("Total Pagado", TotalPagadoTexto));
        doc.Kpis.Add(("Facturas Pendientes", DocumentosPendientesCount.ToString()));

        var s = new SeccionReporte("Detalle de Cuentas por Pagar",
            new ColumnaReporte("N° Factura"),
            new ColumnaReporte("RUT Proveedor"),
            new ColumnaReporte("Razón Social Proveedor"),
            new ColumnaReporte("Emisión", TipoColumna.Fecha),
            new ColumnaReporte("Vencimiento", TipoColumna.Fecha),
            new ColumnaReporte("Monto Facturado", TipoColumna.Moneda),
            new ColumnaReporte("Monto Pagado", TipoColumna.Moneda),
            new ColumnaReporte("Saldo Pendiente", TipoColumna.Moneda),
            new ColumnaReporte("Estado"));

        foreach (var d in Documentos)
            s.Fila(d.NumeroDocumento, d.RutProveedor, d.Proveedor, d.FechaEmision, d.FechaVencimiento, d.MontoTotal, d.MontoPagado, d.SaldoPendiente, d.Estado);

        s.Total("TOTALES", "", "", null, null, Documentos.Sum(x => x.MontoTotal), Documentos.Sum(x => x.MontoPagado), Documentos.Sum(x => x.SaldoPendiente), "");
        doc.Secciones.Add(s);
        return doc;
    }
}
