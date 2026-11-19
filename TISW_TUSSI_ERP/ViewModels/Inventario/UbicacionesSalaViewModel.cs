using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Windows.Input;
using TISW_TUSSI_ERP.Models.Auth;
using TISW_TUSSI_ERP.Models.Inventario;
using TISW_TUSSI_ERP.Services.Api;
using TISW_TUSSI_ERP.Views.Inventario;

namespace TISW_TUSSI_ERP.ViewModels.Inventario;

public class UbicacionesSalaViewModel : BaseViewModel
{
    private readonly InventarioService _servicio;
    private List<LoteSalaItemModel> _todosLotes = new();

    public ObservableCollection<LoteSalaItemModel> LotesFiltrados { get; } = new();
    public ObservableCollection<UbicacionModel> UbicacionesSala { get; } = new();
    public ObservableCollection<UbicacionModel> UbicacionesBodega { get; } = new();
    public ObservableCollection<LoteUbicacionDetalleModel> UbicacionesBodegaDelLote { get; } = new();
    public ObservableCollection<LoteUbicacionDetalleModel> UbicacionesSalaDelLote { get; } = new();

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

    private LoteSalaItemModel? _loteSeleccionado;
    public LoteSalaItemModel? LoteSeleccionado
    {
        get => _loteSeleccionado;
        set
        {
            if (SetProperty(ref _loteSeleccionado, value))
            {
                OnPropertyChanged(nameof(HayLoteSeleccionado));
                _ = CargarDetallesLoteAsync();
            }
        }
    }

    public bool HayLoteSeleccionado => LoteSeleccionado != null;

    // Traspaso Bodega -> Sala
    private LoteUbicacionDetalleModel? _bodegaOrigenSeleccionada;
    public LoteUbicacionDetalleModel? BodegaOrigenSeleccionada
    {
        get => _bodegaOrigenSeleccionada;
        set
        {
            if (SetProperty(ref _bodegaOrigenSeleccionada, value))
                CantidadExhibir = value?.Cantidad.ToString() ?? "0";
        }
    }

    private UbicacionModel? _salaDestinoSeleccionada;
    public UbicacionModel? SalaDestinoSeleccionada
    {
        get => _salaDestinoSeleccionada;
        set => SetProperty(ref _salaDestinoSeleccionada, value);
    }

    private string _cantidadExhibir = string.Empty;
    public string CantidadExhibir
    {
        get => _cantidadExhibir;
        set => SetProperty(ref _cantidadExhibir, value);
    }

    // Retiro Sala -> Bodega (A3)
    private LoteUbicacionDetalleModel? _salaOrigenRetiro;
    public LoteUbicacionDetalleModel? SalaOrigenRetiro
    {
        get => _salaOrigenRetiro;
        set
        {
            if (SetProperty(ref _salaOrigenRetiro, value))
                CantidadRetirar = value?.Cantidad.ToString() ?? "0";
        }
    }

    private UbicacionModel? _bodegaDestinoRetiro;
    public UbicacionModel? BodegaDestinoRetiro
    {
        get => _bodegaDestinoRetiro;
        set => SetProperty(ref _bodegaDestinoRetiro, value);
    }

    private string _cantidadRetirar = string.Empty;
    public string CantidadRetirar
    {
        get => _cantidadRetirar;
        set => SetProperty(ref _cantidadRetirar, value);
    }

    public ICommand ComandoRecargar { get; }
    public ICommand ComandoExhibirEnSala { get; }
    public ICommand ComandoRetirarABodega { get; }
    public ICommand ComandoNuevaUbicacionSala { get; }

    public UbicacionesSalaViewModel(InventarioService servicio)
    {
        _servicio = servicio;
        Titulo = "Ubicaciones en Sala de Ventas";

        ComandoRecargar = new Command(async () => await CargarAsync());
        ComandoExhibirEnSala = new Command(async () => await ExhibirEnSalaAsync());
        ComandoRetirarABodega = new Command(async () => await RetirarABodegaAsync());
        ComandoNuevaUbicacionSala = new Command(async () =>
        {
            string rolActual = Sesion.UsuarioActual?.NombreRol ?? "Encargado de Inventario";
            bool esEncargado = rolActual.Contains("Encargado", StringComparison.OrdinalIgnoreCase) ||
                               rolActual.Contains("Administrador", StringComparison.OrdinalIgnoreCase);

            if (!esEncargado)
            {
                await Shell.Current.DisplayAlert("Acceso Denegado (CU-04)", "Solo el Encargado de Inventario puede administrar los exhibidores de sala.", "OK");
                return;
            }

            var modal = new GestionUbicacionesSalaModalPage(_servicio, async () =>
            {
                await CargarAsync();
            });

            await Shell.Current.Navigation.PushModalAsync(modal);
        });
    }

    public async Task CargarAsync()
    {
        if (EstaOcupado) return;
        EstaOcupado = true;

        try
        {
            var idPrevio = LoteSeleccionado?.IdLote;

            var salas = await _servicio.ObtenerUbicacionesAsync("SALA_VENTAS", true);
            UbicacionesSala.Clear();
            foreach (var s in salas) UbicacionesSala.Add(s);

            var bodegas = await _servicio.ObtenerUbicacionesAsync("BODEGA", true);
            UbicacionesBodega.Clear();
            foreach (var b in bodegas) UbicacionesBodega.Add(b);

            // Obtener lotes y mapearlos al modelo limpio LoteSalaItemModel
            var lotesBase = await _servicio.ObtenerLotesParaUbicacionAsync();
            _todosLotes = lotesBase.Select(l => new LoteSalaItemModel
            {
                IdLote = l.IdLote,
                IdProducto = l.IdProducto,
                Sku = l.Sku,
                CodigoBarras = l.CodigoBarras,
                NombreProducto = l.NombreProducto,
                NumeroLote = l.NumeroLote,
                FechaVencimiento = l.FechaVencimiento,
                StockTotalLote = l.StockTotalLote,
                StockUbicadoBodega = l.StockUbicadoBodega,
                StockUbicadoSala = l.StockUbicadoSala
            }).ToList();

            FiltrarLotes();

            LoteSeleccionado = LotesFiltrados.FirstOrDefault(x => x.IdLote == idPrevio) ?? LotesFiltrados.FirstOrDefault();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", "No se pudieron cargar los datos de sala de ventas: " + ex.Message, "OK");
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

    private async Task CargarDetallesLoteAsync()
    {
        UbicacionesBodegaDelLote.Clear();
        UbicacionesSalaDelLote.Clear();

        if (LoteSeleccionado == null) return;

        var enBodega = await _servicio.ObtenerUbicacionesDeLoteAsync(LoteSeleccionado.IdLote, "BODEGA");
        foreach (var b in enBodega) UbicacionesBodegaDelLote.Add(b);
        BodegaOrigenSeleccionada = UbicacionesBodegaDelLote.FirstOrDefault();

        var enSala = await _servicio.ObtenerUbicacionesDeLoteAsync(LoteSeleccionado.IdLote, "SALA_VENTAS");
        foreach (var s in enSala) UbicacionesSalaDelLote.Add(s);
        SalaOrigenRetiro = UbicacionesSalaDelLote.FirstOrDefault();
    }

    private async Task ExhibirEnSalaAsync()
    {
        if (LoteSeleccionado == null)
        {
            await Shell.Current.DisplayAlert("Atención", "Seleccione un lote del listado.", "OK");
            return;
        }

        if (LoteSeleccionado.EstaVencido)
        {
            await Shell.Current.DisplayAlert("Lote Vencido (CU-INV-08 A1)", $"Este lote venció el {LoteSeleccionado.VencimientoTexto} y no puede exhibirse al público.", "OK");
            return;
        }

        if (BodegaOrigenSeleccionada == null || SalaDestinoSeleccionada == null)
        {
            await Shell.Current.DisplayAlert("Atención", "Seleccione la ubicación de origen en Bodega y la góndola/vitrina de destino en Sala.", "OK");
            return;
        }

        if (!int.TryParse(CantidadExhibir, out int cant) || cant <= 0)
        {
            await Shell.Current.DisplayAlert("Atención", "Ingrese una cantidad válida mayor a cero.", "OK");
            return;
        }

        var (exito, mensaje) = await _servicio.TraspasarBodegaASalaAsync(
            LoteSeleccionado.IdLote,
            BodegaOrigenSeleccionada.IdUbicacion,
            SalaDestinoSeleccionada.IdUbicacion,
            cant);

        await Shell.Current.DisplayAlert(exito ? "Traspaso a Sala Exitoso" : "Validación", mensaje, "OK");
        if (exito) await CargarAsync();
    }

    private async Task RetirarABodegaAsync()
    {
        if (LoteSeleccionado == null || SalaOrigenRetiro == null || BodegaDestinoRetiro == null)
        {
            await Shell.Current.DisplayAlert("Retiro de Sala", "Seleccione la ubicación en sala de origen y el estante de bodega de destino.", "OK");
            return;
        }

        if (!int.TryParse(CantidadRetirar, out int cant) || cant <= 0)
        {
            await Shell.Current.DisplayAlert("Atención", "Ingrese una cantidad válida a retirar.", "OK");
            return;
        }

        var (exito, mensaje) = await _servicio.RetirarSalaABodegaAsync(
            LoteSeleccionado.IdLote,
            SalaOrigenRetiro.IdUbicacion,
            BodegaDestinoRetiro.IdUbicacion,
            cant);

        await Shell.Current.DisplayAlert(exito ? "Retiro Exitoso" : "Validación", mensaje, "OK");
        if (exito) await CargarAsync();
    }

    private async Task CrearUbicacionSalaAsync()
    {
        string rolActual = Preferences.Get("UsuarioRol", "Encargado de Inventario");

        bool esEncargado = rolActual.Contains("Encargado", StringComparison.OrdinalIgnoreCase) ||
                           rolActual.Contains("Administrador", StringComparison.OrdinalIgnoreCase);

        if (!esEncargado)
        {
            await Shell.Current.DisplayAlert("Acceso Denegado (CU-04)", "Solo el Encargado de Inventario puede crear ubicaciones en sala de ventas.", "OK");
            return;
        }

        string codigo = await Shell.Current.DisplayPromptAsync("Nueva Ubicación Sala", "Código único (Ej: SAL-GON-02-N1):", "Siguiente", "Cancelar");
        if (string.IsNullOrWhiteSpace(codigo)) return;

        string zona = await Shell.Current.DisplayPromptAsync("Nueva Ubicación Sala", "Góndola / Vitrina / Mostrador (Ej: Góndola 2):", "Siguiente", "Cancelar");
        if (string.IsNullOrWhiteSpace(zona)) return;

        string mueble = await Shell.Current.DisplayPromptAsync("Nueva Ubicación Sala", "Cuerpo o Sección (Ej: Cuerpo B):", "Siguiente", "Cancelar");
        if (string.IsNullOrWhiteSpace(mueble)) return;

        string nivel = await Shell.Current.DisplayPromptAsync("Nueva Ubicación Sala", "Nivel / Bandeja (Opcional, Ej: Nivel 1):", "Guardar", "Cancelar") ?? "";

        var nueva = new UbicacionModel
        {
            TipoZona = "SALA_VENTAS",
            CodigoUbicacion = codigo,
            SectorPasillo = zona,
            EstanteMueble = mueble,
            RepisaNivel = nivel
        };

        var (exito, mensaje) = await _servicio.CrearUbicacionAsync(nueva, rolActual);
        await Shell.Current.DisplayAlert(exito ? "Éxito" : "Error", mensaje, "OK");
        if (exito) await CargarAsync();
    }
}