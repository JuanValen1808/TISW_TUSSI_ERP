using System.Collections.ObjectModel;
using System.Windows.Input;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Compras;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Compras;

public class OrdenesCompraViewModel : BaseViewModel
{
    private readonly ComprasService _servicio;
    private List<OrdenCompra> _todas = new();

    public ObservableCollection<OrdenCompra> Ordenes { get; } = new();
    public ObservableCollection<DetalleOrdenCompra> Detalles { get; } = new();

    private ResumenCompras _resumen = new();
    public ResumenCompras Resumen { get => _resumen; private set => SetProperty(ref _resumen, value); }

    // TODAS | PENDIENTE | PARCIAL | LIQUIDADA
    private string _filtroActual = "TODAS";
    public string FiltroActual { get => _filtroActual; private set => SetProperty(ref _filtroActual, value); }

    private string _cantidadTexto = "Órdenes (0)";
    public string CantidadTexto { get => _cantidadTexto; private set => SetProperty(ref _cantidadTexto, value); }

    private OrdenCompra? _ordenSeleccionada;
    public OrdenCompra? OrdenSeleccionada
    {
        get => _ordenSeleccionada;
        set
        {
            if (!SetProperty(ref _ordenSeleccionada, value)) return;
            OnPropertyChanged(nameof(HayOrden));
            OnPropertyChanged(nameof(NoHayOrden));
            _ = CargarDetalleAsync(value);
        }
    }
    public bool HayOrden => OrdenSeleccionada is not null;
    public bool NoHayOrden => OrdenSeleccionada is null;

    private string _mensajeError = string.Empty;
    public string MensajeError
    {
        get => _mensajeError;
        private set { if (SetProperty(ref _mensajeError, value)) OnPropertyChanged(nameof(HayError)); }
    }
    public bool HayError => !string.IsNullOrEmpty(MensajeError);

    public ICommand ComandoFiltrar { get; }
    public ICommand ComandoNuevaOrden { get; }
    public ICommand ComandoRegistrarRecepcion { get; }
    public ICommand ComandoImprimir { get; }

    public OrdenesCompraViewModel(ComprasService servicio)
    {
        _servicio = servicio;
        Titulo = "Órdenes de Compra";

        ComandoFiltrar = new Command<string>(f =>
        {
            FiltroActual = f ?? "TODAS";
            AplicarFiltro();
        });
        ComandoNuevaOrden = new Command(async () => await Aviso("Nueva orden de compra", "Este formulario se implementará en la siguiente etapa del módulo Compras."));
        ComandoRegistrarRecepcion = new Command(async () => await Aviso("Registrar recepción", "Se implementará en la pantalla \"Recepción Facturas\"."));
        ComandoImprimir = new Command(async () => await Aviso("Imprimir", "La impresión de la orden aún no está implementada."));
    }

    public async Task CargarAsync()
    {
        if (EstaOcupado) return;
        EstaOcupado = true;
        MensajeError = string.Empty;
        try
        {
            Resumen = await _servicio.ObtenerResumenAsync();
            _todas = await _servicio.ObtenerOrdenesAsync();
            AplicarFiltro();
        }
        catch (Exception ex)
        {
            MensajeError = "No se pudieron cargar las órdenes: " + ex.Message;
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private void AplicarFiltro()
    {
        var idPrevio = OrdenSeleccionada?.Id;

        var filtradas = _todas.Where(o => FiltroActual switch
        {
            "PENDIENTE" => o.Estado is "BORRADOR" or "EMITIDA",
            "PARCIAL" => o.Estado == "PARCIAL",
            "LIQUIDADA" => o.Estado == "RECIBIDA",
            _ => true
        }).ToList();

        Ordenes.Clear();
        foreach (var o in filtradas) Ordenes.Add(o);
        CantidadTexto = $"Órdenes ({Ordenes.Count})";

        OrdenSeleccionada = Ordenes.FirstOrDefault(o => o.Id == idPrevio) ?? Ordenes.FirstOrDefault();
    }

    private async Task CargarDetalleAsync(OrdenCompra? orden)
    {
        if (orden is null) { Detalles.Clear(); return; }
        try
        {
            var lista = await _servicio.ObtenerDetalleAsync(orden.Id);
            if (OrdenSeleccionada?.Id != orden.Id) return; // el usuario cambió de orden mientras cargaba
            Detalles.Clear();
            foreach (var d in lista) Detalles.Add(d);
        }
        catch (Exception ex)
        {
            MensajeError = "No se pudo cargar el detalle: " + ex.Message;
        }
    }

    private static Task Aviso(string titulo, string mensaje)
        => Shell.Current.DisplayAlert(titulo, mensaje, "OK");
}
