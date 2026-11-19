using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.Collections.ObjectModel;
using System.Windows.Input;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Auth;
using TISW_TUSSI_ERP.Models.Inventario;
using TISW_TUSSI_ERP.Services.Api;
using TISW_TUSSI_ERP.Views.Inventario;

namespace TISW_TUSSI_ERP.ViewModels.Inventario;

public class UbicacionesBodegaViewModel : BaseViewModel
{
    private readonly InventarioService _servicio;
    private List<LoteBodegaItemModel> _todosLotes = new();

    public ObservableCollection<LoteBodegaItemModel> LotesFiltrados { get; } = new();
    public ObservableCollection<UbicacionModel> UbicacionesBodega { get; } = new();
    public ObservableCollection<LoteUbicacionDetalleModel> UbicacionesDelLote { get; } = new();

    private string _textoBusqueda = string.Empty;
    public string TextoBusqueda
    {
        get => _textoBusqueda;
        set
        {
            if (SetProperty(ref _textoBusqueda, value))
                FiltrarLotes();
        }
    }

    private LoteBodegaItemModel? _loteSeleccionado;
    public LoteBodegaItemModel? LoteSeleccionado
    {
        get => _loteSeleccionado;
        set
        {
            if (SetProperty(ref _loteSeleccionado, value))
            {
                OnPropertyChanged(nameof(HayLoteSeleccionado));
                CantidadAsignar = value?.StockSinUbicar.ToString() ?? "0";
                _ = CargarDetalleUbicacionesLoteAsync();
            }
        }
    }

    public bool HayLoteSeleccionado => LoteSeleccionado != null;

    private UbicacionModel? _ubicacionSeleccionada;
    public UbicacionModel? UbicacionSeleccionada
    {
        get => _ubicacionSeleccionada;
        set => SetProperty(ref _ubicacionSeleccionada, value);
    }

    private string _cantidadAsignar = string.Empty;
    public string CantidadAsignar
    {
        get => _cantidadAsignar;
        set => SetProperty(ref _cantidadAsignar, value);
    }

    // Reubicación (Flujo alterno A3)
    private LoteUbicacionDetalleModel? _ubicacionOrigenReubicar;
    public LoteUbicacionDetalleModel? UbicacionOrigenReubicar
    {
        get => _ubicacionOrigenReubicar;
        set
        {
            if (SetProperty(ref _ubicacionOrigenReubicar, value))
                CantidadReubicar = value?.Cantidad.ToString() ?? "0";
        }
    }

    private UbicacionModel? _ubicacionDestinoReubicar;
    public UbicacionModel? UbicacionDestinoReubicar
    {
        get => _ubicacionDestinoReubicar;
        set => SetProperty(ref _ubicacionDestinoReubicar, value);
    }

    private string _cantidadReubicar = string.Empty;
    public string CantidadReubicar
    {
        get => _cantidadReubicar;
        set => SetProperty(ref _cantidadReubicar, value);
    }

    public ICommand ComandoRecargar { get; }
    public ICommand ComandoAsignar { get; }
    public ICommand ComandoReubicar { get; }
    public ICommand ComandoNuevaUbicacion { get; }

    public UbicacionesBodegaViewModel(InventarioService servicio)
    {
        _servicio = servicio;
        Titulo = "Ubicaciones en Bodega";

        ComandoRecargar = new Command(async () => await CargarAsync());
        ComandoAsignar = new Command(async () => await AsignarAsync());
        ComandoReubicar = new Command(async () => await ReubicarAsync());
        ComandoNuevaUbicacion = new Command(async () => await AbrirGestionUbicacionesModalAsync());
    }

    public async Task CargarAsync()
    {
        if (EstaOcupado) return;
        EstaOcupado = true;

        try
        {
            var idLotePrevio = LoteSeleccionado?.IdLote;

            var ubicaciones = await _servicio.ObtenerUbicacionesAsync("BODEGA", true);
            UbicacionesBodega.Clear();
            foreach (var u in ubicaciones) UbicacionesBodega.Add(u);

            _todosLotes = await _servicio.ObtenerLotesParaUbicacionAsync();
            FiltrarLotes();

            LoteSeleccionado = LotesFiltrados.FirstOrDefault(x => x.IdLote == idLotePrevio) ?? LotesFiltrados.FirstOrDefault();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", "No se pudieron cargar las ubicaciones: " + ex.Message, "OK");
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private void FiltrarLotes()
    {
        var filtrados = _todosLotes.Where(l =>
            string.IsNullOrWhiteSpace(TextoBusqueda) ||
            l.Sku.Contains(TextoBusqueda, StringComparison.OrdinalIgnoreCase) ||
            l.CodigoBarras.Contains(TextoBusqueda, StringComparison.OrdinalIgnoreCase) ||
            l.NombreProducto.Contains(TextoBusqueda, StringComparison.OrdinalIgnoreCase) ||
            l.NumeroLote.Contains(TextoBusqueda, StringComparison.OrdinalIgnoreCase)
        ).ToList();

        LotesFiltrados.Clear();
        foreach (var l in filtrados) LotesFiltrados.Add(l);
    }

    private async Task CargarDetalleUbicacionesLoteAsync()
    {
        UbicacionesDelLote.Clear();
        if (LoteSeleccionado == null) return;

        var detalles = await _servicio.ObtenerUbicacionesDeLoteAsync(LoteSeleccionado.IdLote, "BODEGA");
        foreach (var d in detalles) UbicacionesDelLote.Add(d);

        // Preseleccionar origen por defecto para reubicación
        UbicacionOrigenReubicar = UbicacionesDelLote.FirstOrDefault();
        UbicacionDestinoReubicar = UbicacionesBodega.FirstOrDefault(u => u.IdUbicacion != UbicacionOrigenReubicar?.IdUbicacion);
    }

    private async Task AsignarAsync()
    {
        if (LoteSeleccionado == null || UbicacionSeleccionada == null)
        {
            await Shell.Current.DisplayAlert("Atención", "Seleccione un lote y una ubicación de destino en bodega.", "OK");
            return;
        }

        if (!int.TryParse(CantidadAsignar, out int cant) || cant <= 0)
        {
            await Shell.Current.DisplayAlert("Atención", "Ingrese una cantidad válida mayor a cero.", "OK");
            return;
        }

        var (exito, mensaje) = await _servicio.AsignarUbicacionBodegaAsync(LoteSeleccionado.IdLote, UbicacionSeleccionada.IdUbicacion, cant);
        await Shell.Current.DisplayAlert(exito ? "Asignación Exitosa" : "Validación", mensaje, "OK");

        if (exito) await CargarAsync();
    }

    private async Task ReubicarAsync()
    {
        if (LoteSeleccionado == null || UbicacionOrigenReubicar == null || UbicacionDestinoReubicar == null)
        {
            await Shell.Current.DisplayAlert("Reubicación", "Seleccione la ubicación de origen, la ubicación de destino y la cantidad.", "OK");
            return;
        }

        if (!int.TryParse(CantidadReubicar, out int cant) || cant <= 0)
        {
            await Shell.Current.DisplayAlert("Atención", "Ingrese una cantidad válida a reubicar.", "OK");
            return;
        }

        var (exito, mensaje) = await _servicio.ReubicarLoteBodegaAsync(
            LoteSeleccionado.IdLote,
            UbicacionOrigenReubicar.IdUbicacion,
            UbicacionDestinoReubicar.IdUbicacion,
            cant);

        await Shell.Current.DisplayAlert(exito ? "Reubicación Exitosa" : "Validación", mensaje, "OK");
        if (exito) await CargarAsync();
    }

    private async Task AbrirGestionUbicacionesModalAsync()
    {
        string rolActual = Sesion.UsuarioActual?.NombreRol ?? "Encargado de Inventario";
        bool esEncargado = rolActual.Contains("Encargado", StringComparison.OrdinalIgnoreCase) ||
                           rolActual.Contains("Bodega", StringComparison.OrdinalIgnoreCase) ||
                           rolActual.Contains("Administrador", StringComparison.OrdinalIgnoreCase);

        if (!esEncargado)
        {
            await Shell.Current.DisplayAlert("Acceso Denegado (CU-04)", "Solo el Encargado de Inventario puede administrar las ubicaciones de bodega.", "OK");
            return;
        }

        var modal = new GestionUbicacionesModalPage(_servicio, async () =>
        {
            await CargarAsync();
        });

        await Shell.Current.Navigation.PushModalAsync(modal);
    }
}