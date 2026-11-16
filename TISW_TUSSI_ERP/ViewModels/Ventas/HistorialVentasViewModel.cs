
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Ventas;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Ventas;

public class HistorialVentasViewModel : BaseViewModel
{
    private readonly HistorialVentasService _servicio;
    private List<VentaHistorial> _todas = new();

    public ObservableCollection<VentaHistorial> Ventas { get; } = new();
    public ObservableCollection<DetalleVentaHistorial> Detalles { get; } = new();

    public string[] Estados { get; } =
    {
        "Todos",
        "EMITIDO",
        "PAGADO",
        "PENDIENTE",
        "ANULADO"
    };

    public string[] FormasPago { get; } =
    {
        "Todas",
        "EFECTIVO",
        "TARJETA",
        "TRANSFERENCIA",
        "CREDITO",
        "OTRO"
    };

    private string _busqueda = "";
    public string Busqueda
    {
        get => _busqueda;
        set
        {
            if (SetProperty(ref _busqueda, value))
                Filtrar();
        }
    }

    private string _estadoFiltro = "Todos";
    public string EstadoFiltro
    {
        get => _estadoFiltro;
        set
        {
            if (SetProperty(ref _estadoFiltro, value))
                Filtrar();
        }
    }

    private string _pagoFiltro = "Todas";
    public string PagoFiltro
    {
        get => _pagoFiltro;
        set
        {
            if (SetProperty(ref _pagoFiltro, value))
                Filtrar();
        }
    }

    private VentaHistorial? _ventaSeleccionada;
    public VentaHistorial? VentaSeleccionada
    {
        get => _ventaSeleccionada;
        private set
        {
            if (SetProperty(ref _ventaSeleccionada, value))
            {
                OnPropertyChanged(nameof(HayDetalle));
            }
        }
    }

    public bool HayDetalle => VentaSeleccionada != null;

    private string _mensaje = "";
    public string Mensaje
    {
        get => _mensaje;
        private set => SetProperty(ref _mensaje, value);
    }

    public int CantidadVentas => Ventas.Count;

    public string TotalVentasTexto =>
        Formato.Moneda(Ventas.Sum(v => v.Total));

    public string TotalPagadoTexto =>
        Formato.Moneda(
            Ventas.Where(v => v.Estado == "PAGADO")
                  .Sum(v => v.Total));

    public string TotalPendienteTexto =>
        Formato.Moneda(
            Ventas.Where(v => v.Estado == "PENDIENTE")
                  .Sum(v => v.Total));

    public HistorialVentasViewModel(HistorialVentasService servicio)
    {
        _servicio = servicio;
        Titulo = "Historial y Monitor de Ventas";
    }

    public async Task CargarAsync()
    {
        if (EstaOcupado)
            return;

        EstaOcupado = true;
        Mensaje = "";

        try
        {
            _todas = await _servicio.ObtenerVentasAsync();
            Filtrar();
        }
        catch (Exception ex)
        {
            Mensaje = "Error al consultar ventas: " + ex.Message;
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    public void Filtrar()
    {
        IEnumerable<VentaHistorial> consulta = _todas;

        string texto = (Busqueda ?? "").Trim();

        if (!string.IsNullOrWhiteSpace(texto))
        {
            consulta = consulta.Where(v =>
                v.Folio.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                v.Cliente.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                v.RutCliente.Contains(texto, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(EstadoFiltro) &&
            EstadoFiltro != "Todos")
        {
            consulta = consulta.Where(v =>
                v.Estado == EstadoFiltro);
        }

        if (!string.IsNullOrWhiteSpace(PagoFiltro) &&
            PagoFiltro != "Todas")
        {
            consulta = consulta.Where(v =>
                v.FormaPago == PagoFiltro);
        }

        Ventas.Clear();

        foreach (var venta in consulta)
            Ventas.Add(venta);

        OnPropertyChanged(nameof(CantidadVentas));
        OnPropertyChanged(nameof(TotalVentasTexto));
        OnPropertyChanged(nameof(TotalPagadoTexto));
        OnPropertyChanged(nameof(TotalPendienteTexto));
    }

    public async Task SeleccionarVentaAsync(VentaHistorial? venta)
    {
        if (venta == null)
        {
            CerrarDetalle();
            return;
        }

        Mensaje = "";

        try
        {
            var detalles = await _servicio.ObtenerDetalleAsync(
                venta.IdVenta);

            Detalles.Clear();

            foreach (var detalle in detalles)
                Detalles.Add(detalle);

            VentaSeleccionada = venta;
        }
        catch (Exception ex)
        {
            Mensaje = "Error al cargar detalle: " + ex.Message;
        }
    }

    public void CerrarDetalle()
    {
        VentaSeleccionada = null;
        Detalles.Clear();
    }

    public void LimpiarFiltros()
    {
        Busqueda = "";
        EstadoFiltro = "Todos";
        PagoFiltro = "Todas";
    }
}
