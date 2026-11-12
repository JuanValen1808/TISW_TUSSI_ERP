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
    private int _detalleVersion;

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
    public bool NoEstaOcupado => !EstaOcupado;

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
            if (EstaOcupado) return;
            FiltroActual = f ?? "TODAS";
            AplicarFiltro(OrdenSeleccionada?.Id);
        }, _ => NoEstaOcupado);

        ComandoNuevaOrden = new Command(async () => await IrANuevaOrdenAsync(), () => NoEstaOcupado);
        ComandoRegistrarRecepcion = new Command(async () => await IrARegistrarRecepcionAsync(), () => NoEstaOcupado);
        ComandoAprobar = new Command(async () => await AprobarOrdenAsync(), () => NoEstaOcupado);
        ComandoRechazar = new Command(async () => await RechazarOrdenAsync(), () => NoEstaOcupado);
        ComandoAnular = new Command(async () => await AnularOrdenAsync(), () => NoEstaOcupado);
        ComandoExportarCsv = new Command(async () => await ExportarCsvAsync(), () => NoEstaOcupado);

        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(EstaOcupado))
            {
                OnPropertyChanged(nameof(NoEstaOcupado));
                (ComandoFiltrar as Command)?.ChangeCanExecute();
                (ComandoNuevaOrden as Command)?.ChangeCanExecute();
                (ComandoRegistrarRecepcion as Command)?.ChangeCanExecute();
                (ComandoAprobar as Command)?.ChangeCanExecute();
                (ComandoRechazar as Command)?.ChangeCanExecute();
                (ComandoAnular as Command)?.ChangeCanExecute();
                (ComandoExportarCsv as Command)?.ChangeCanExecute();
            }
        };
    }

    public async Task CargarAsync()
    {
        if (EstaOcupado) return;
        EstaOcupado = true;
        MensajeError = string.Empty;
        try
        {
            await CargarInternoAsync();
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

    private async Task CargarInternoAsync()
    {
        MensajeError = string.Empty;
        int? idPreferido = _servicio.UltimaOrdenCreadaId ?? OrdenSeleccionada?.Id;
        if (_servicio.UltimaOrdenCreadaId.HasValue)
        {
            FiltroActual = "TODAS";
            _servicio.UltimaOrdenCreadaId = null;
        }

        Resumen = await _servicio.ObtenerResumenAsync();
        _todas = await _servicio.ObtenerOrdenesAsync();
        AplicarFiltro(idPreferido);
    }

    private void AplicarFiltro(int? idPreferido = null)
    {
        var idPrevio = idPreferido ?? OrdenSeleccionada?.Id;

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
        int version = ++_detalleVersion;
        Detalles.Clear();
        if (orden is null) return;

        try
        {
            var lineas = await _servicio.ObtenerDetalleAsync(orden.Id);
            if (version != _detalleVersion) return;

            Detalles.Clear();
            foreach (var d in lineas) Detalles.Add(d);
        }
        catch (Exception ex)
        {
            if (version == _detalleVersion)
                MensajeError = "No se pudo cargar el detalle: " + ex.Message;
        }
    }

    private async Task IrANuevaOrdenAsync()
    {
        if (EstaOcupado) return;
        EstaOcupado = true;
        try
        {
            await Shell.Current.GoToAsync(nameof(Views.Compras.NuevaOrdenPage));
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private async Task IrARegistrarRecepcionAsync()
    {
        if (EstaOcupado || OrdenSeleccionada is null) return;
        EstaOcupado = true;
        try
        {
            await Shell.Current.GoToAsync($"RecepcionFacturasPage?idOrden={OrdenSeleccionada.Id}");
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private async Task AprobarOrdenAsync()
    {
        if (EstaOcupado || OrdenSeleccionada is null) return;
        EstaOcupado = true;

        try
        {
            if (!EsAdminOGerente)
            {
                await Shell.Current.DisplayAlert("Permiso denegado", "Solo un usuario con rol Administrador o Gerente puede autorizar compras sobre el límite presupuestario.", "OK");
                return;
            }

            var orden = OrdenSeleccionada;
            var confirmar = await Shell.Current.DisplayAlert("Aprobar orden de compra", $"¿Autorizar la emisión de la orden {orden.Numero} por {orden.TotalTexto}?", "Aprobar", "Cancelar");
            if (!confirmar) return;

            await _servicio.AprobarOrdenCompraAsync(orden.Id);
            await CargarInternoAsync();
            await Shell.Current.DisplayAlert("Orden autorizada", $"La orden {orden.Numero} fue aprobada y emitida exitosamente al proveedor.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            await CargarInternoAsync();
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private async Task RechazarOrdenAsync()
    {
        if (EstaOcupado || OrdenSeleccionada is null) return;
        EstaOcupado = true;

        try
        {
            if (!EsAdminOGerente)
            {
                await Shell.Current.DisplayAlert("Permiso denegado", "Solo un usuario con rol Administrador o Gerente puede rechazar compras en flujo jerárquico.", "OK");
                return;
            }

            var orden = OrdenSeleccionada;
            var motivo = await Shell.Current.DisplayPromptAsync("Rechazar orden", "Ingresa el motivo obligatorio del rechazo de la solicitud:", "Rechazar", "Cancelar", "Ej: Presupuesto excedido para el período");
            if (string.IsNullOrWhiteSpace(motivo)) return;

            await _servicio.RechazarOrdenCompraAsync(orden.Id, motivo.Trim());
            await CargarInternoAsync();
            await Shell.Current.DisplayAlert("Orden rechazada", $"La orden {orden.Numero} fue rechazada y marcada como Anulada.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            await CargarInternoAsync();
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private async Task AnularOrdenAsync()
    {
        if (EstaOcupado || OrdenSeleccionada is null) return;
        EstaOcupado = true;

        try
        {
            var orden = OrdenSeleccionada;
            var confirmar = await Shell.Current.DisplayAlert("Anular orden", $"¿Deseas anular la orden {orden.Numero}? Esta acción cancelará el pedido.", "Sí, anular", "Cancelar");
            if (!confirmar) return;

            await _servicio.AnularOrdenCompraAsync(orden.Id);
            await CargarInternoAsync();
            await Shell.Current.DisplayAlert("Orden anulada", $"La orden {orden.Numero} fue anulada correctamente.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            await CargarInternoAsync();
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private async Task ExportarCsvAsync()
    {
        if (EstaOcupado) return;
        EstaOcupado = true;

        try
        {
            if (Ordenes.Count == 0)
            {
                await Shell.Current.DisplayAlert("Aviso", "No hay órdenes para exportar en el listado actual.", "OK");
                return;
            }

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
        finally
        {
            EstaOcupado = false;
        }
    }
}
