using System.Collections.ObjectModel;
using System.Windows.Input;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Auth;
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

    // TODAS | PENDIENTE | PARCIAL | LIQUIDADA | PENDIENTE_AUTORIZACION | ANULADA
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
            OnPropertyChanged(nameof(EsAdminOGerente));
            _ = CargarDetalleAsync(value);
        }
    }
    public bool HayOrden => OrdenSeleccionada is not null;
    public bool NoHayOrden => OrdenSeleccionada is null;

    public bool EsAdminOGerente => Sesion.UsuarioActual?.IdRol == 1 || Sesion.UsuarioActual?.IdRol == 10;

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
    public ICommand ComandoAprobar { get; }
    public ICommand ComandoRechazar { get; }
    public ICommand ComandoAnular { get; }
    public ICommand ComandoExportarCsv { get; }

    public OrdenesCompraViewModel(ComprasService servicio)
    {
        _servicio = servicio;
        Titulo = "Órdenes de Compra";

        ComandoFiltrar = new Command<string>(f =>
        {
            FiltroActual = f ?? "TODAS";
            AplicarFiltro();
        });

        ComandoNuevaOrden = new Command(async () => await Shell.Current.GoToAsync(nameof(Views.Compras.NuevaOrdenPage)));

        ComandoRegistrarRecepcion = new Command(async () =>
        {
            if (OrdenSeleccionada is null) return;
            // Navega directamente a la pantalla de Recepción de Facturas pasando el ID de la OC seleccionada
            await Shell.Current.GoToAsync($"RecepcionFacturasPage?idOrden={OrdenSeleccionada.Id}");
        });

        ComandoAprobar = new Command(async () => await AprobarOrdenAsync());
        ComandoRechazar = new Command(async () => await RechazarOrdenAsync());
        ComandoAnular = new Command(async () => await AnularOrdenAsync());
        ComandoExportarCsv = new Command(async () => await ExportarCsvAsync());
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
            "PENDIENTE_AUTORIZACION" => o.Estado == "PENDIENTE_AUTORIZACION",
            "ANULADA" => o.Estado == "ANULADA",
            _ => true
        }).ToList();

        Ordenes.Clear();
        foreach (var o in filtradas) Ordenes.Add(o);

        CantidadTexto = $"Órdenes ({Ordenes.Count})";

        OrdenSeleccionada = idPrevio.HasValue
            ? Ordenes.FirstOrDefault(o => o.Id == idPrevio.Value) ?? Ordenes.FirstOrDefault()
            : Ordenes.FirstOrDefault();
    }

    private async Task CargarDetalleAsync(OrdenCompra? orden)
    {
        Detalles.Clear();
        if (orden is null) return;

        try
        {
            var lineas = await _servicio.ObtenerDetalleAsync(orden.Id);
            foreach (var d in lineas) Detalles.Add(d);
        }
        catch (Exception ex)
        {
            MensajeError = "No se pudo cargar el detalle: " + ex.Message;
        }
    }

    private async Task AprobarOrdenAsync()
    {
        if (OrdenSeleccionada is null) return;

        if (!EsAdminOGerente)
        {
            await Shell.Current.DisplayAlert("Permiso denegado", "Solo un usuario con rol Administrador o Gerente puede autorizar compras sobre el límite presupuestario.", "OK");
            return;
        }

        var confirmar = await Shell.Current.DisplayAlert("Aprobar orden de compra", $"¿Autorizar la emisión de la orden {OrdenSeleccionada.Numero} por {OrdenSeleccionada.TotalTexto}?", "Aprobar", "Cancelar");
        if (!confirmar) return;

        try
        {
            await _servicio.AprobarOrdenCompraAsync(OrdenSeleccionada.Id);
            await Shell.Current.DisplayAlert("Orden autorizada", $"La orden {OrdenSeleccionada.Numero} fue aprobada y emitida exitosamente al proveedor.", "OK");
            await CargarAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async Task RechazarOrdenAsync()
    {
        if (OrdenSeleccionada is null) return;

        if (!EsAdminOGerente)
        {
            await Shell.Current.DisplayAlert("Permiso denegado", "Solo un usuario con rol Administrador o Gerente puede rechazar compras en flujo jerárquico.", "OK");
            return;
        }

        var motivo = await Shell.Current.DisplayPromptAsync("Rechazar orden", "Ingresa el motivo obligatorio del rechazo de la solicitud:", "Rechazar", "Cancelar", "Ej: Presupuesto excedido para el período");
        if (string.IsNullOrWhiteSpace(motivo)) return;

        try
        {
            await _servicio.RechazarOrdenCompraAsync(OrdenSeleccionada.Id, motivo.Trim());
            await Shell.Current.DisplayAlert("Orden rechazada", $"La orden {OrdenSeleccionada.Numero} fue rechazada y marcada como Anulada.", "OK");
            await CargarAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async Task AnularOrdenAsync()
    {
        if (OrdenSeleccionada is null) return;

        var confirmar = await Shell.Current.DisplayAlert("Anular orden", $"¿Deseas anular la orden {OrdenSeleccionada.Numero}? Esta acción cancelará el pedido.", "Sí, anular", "Cancelar");
        if (!confirmar) return;

        try
        {
            await _servicio.AnularOrdenCompraAsync(OrdenSeleccionada.Id);
            await CargarAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async Task ExportarCsvAsync()
    {
        if (Ordenes.Count == 0)
        {
            await Shell.Current.DisplayAlert("Aviso", "No hay órdenes para exportar en el listado actual.", "OK");
            return;
        }

        try
        {
            var headers = new[] { "Número OC", "Fecha Emisión", "RUT Proveedor", "Proveedor", "Estado", "Subtotal", "IVA", "Total", "Creador" };
            var rows = Ordenes.Select(o => new[]
            {
                o.Numero,
                o.FechaTexto,
                o.RutProveedor,
                o.Proveedor,
                o.EstadoTexto,
                o.Subtotal.ToString("0", Formato.Cultura),
                o.Iva.ToString("0", Formato.Cultura),
                o.Total.ToString("0", Formato.Cultura),
                o.Creador
            });

            await CsvExportHelper.ExportarYCompartirAsync("Ordenes_de_Compra", headers, rows);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error al exportar", ex.Message, "OK");
        }
    }
}
