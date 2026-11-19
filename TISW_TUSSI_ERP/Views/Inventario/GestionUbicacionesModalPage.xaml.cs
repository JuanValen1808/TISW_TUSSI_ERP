using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Auth;
using TISW_TUSSI_ERP.Models.Inventario;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.Views.Inventario;

public partial class GestionUbicacionesModalPage : ContentPage
{
    private readonly InventarioService _servicio;
    private readonly Func<Task> _alCerrarCallback;
    private UbicacionModel? _ubicacionEnEdicion;

    private static readonly List<string> PasillosDisponibles = new()
    {
        "Pasillo A", "Pasillo B", "Pasillo C", "Pasillo D", "Cámara Fría"
    };

    private static readonly List<string> EstantesDisponibles = new()
    {
        "Estante 1", "Estante 2", "Estante 3", "Estante 4", "Estante 5"
    };

    private static readonly List<string> RepisasDisponibles = new()
    {
        "", "Repisa 1", "Repisa 2", "Repisa 3", "Repisa 4", "Repisa 5"
    };

    public GestionUbicacionesModalPage(InventarioService servicio, Func<Task> alCerrarCallback)
    {
        InitializeComponent();
        _servicio = servicio;
        _alCerrarCallback = alCerrarCallback;

        PkrPasillo.ItemsSource = PasillosDisponibles;
        PkrEstante.ItemsSource = EstantesDisponibles;
        PkrRepisa.ItemsSource = RepisasDisponibles;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarListaAsync();
    }

    private async Task CargarListaAsync()
    {
        var lista = await _servicio.ObtenerUbicacionesAsync("BODEGA", soloActivas: false);
        ListaUbicaciones.ItemsSource = lista;
    }

    private async void OnGuardarClicked(object sender, EventArgs e)
    {
        string rolActual = Sesion.UsuarioActual?.NombreRol ?? "Encargado de Inventario";
        string pasillo = PkrPasillo.SelectedItem as string ?? string.Empty;
        string estante = PkrEstante.SelectedItem as string ?? string.Empty;
        string repisa = PkrRepisa.SelectedItem as string ?? string.Empty;

        var modelo = new UbicacionModel
        {
            IdUbicacion = _ubicacionEnEdicion?.IdUbicacion ?? 0,
            TipoZona = "BODEGA",
            CodigoUbicacion = TxtCodigo.Text ?? string.Empty,
            SectorPasillo = pasillo,
            EstanteMueble = estante,
            RepisaNivel = repisa,
            Descripcion = TxtDescripcion.Text ?? string.Empty
        };

        var (exito, mensaje) = _ubicacionEnEdicion == null
            ? await _servicio.CrearUbicacionAsync(modelo, rolActual)
            : await _servicio.ActualizarUbicacionAsync(modelo, rolActual);

        await DisplayAlert(exito ? "Éxito" : "Atención", mensaje, "OK");

        if (exito)
        {
            LimpiarFormulario();
            await CargarListaAsync();
        }
    }

    private void OnEditarItemClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is UbicacionModel item)
        {
            _ubicacionEnEdicion = item;
            LblModoFormulario.Text = $"Editando Ubicación: {item.CodigoUbicacion}";
            TxtCodigo.Text = item.CodigoUbicacion;
            PkrPasillo.SelectedItem = PasillosDisponibles.Contains(item.SectorPasillo) ? item.SectorPasillo : null;
            PkrEstante.SelectedItem = EstantesDisponibles.Contains(item.EstanteMueble) ? item.EstanteMueble : null;
            PkrRepisa.SelectedItem = RepisasDisponibles.Contains(item.RepisaNivel) ? item.RepisaNivel : "";
            TxtDescripcion.Text = item.Descripcion;
            BtnLimpiar.IsVisible = true;
            BtnGuardar.Text = "💾 Actualizar";
        }
    }

    private async void OnToggleEstadoClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is UbicacionModel item)
        {
            string rolActual = Sesion.UsuarioActual?.NombreRol ?? "Encargado de Inventario";
            var (exito, mensaje) = await _servicio.CambiarEstadoUbicacionAsync(item.IdUbicacion, !item.Activo, rolActual);
            await DisplayAlert(exito ? "Estado Actualizado" : "Validación", mensaje, "OK");

            if (exito)
                await CargarListaAsync();
        }
    }

    private void OnLimpiarClicked(object sender, EventArgs e) => LimpiarFormulario();

    private void LimpiarFormulario()
    {
        _ubicacionEnEdicion = null;
        LblModoFormulario.Text = "Nueva Ubicación en Bodega";
        TxtCodigo.Text = string.Empty;
        PkrPasillo.SelectedItem = null;
        PkrEstante.SelectedItem = null;
        PkrRepisa.SelectedItem = null;
        TxtDescripcion.Text = string.Empty;
        BtnLimpiar.IsVisible = false;
        BtnGuardar.Text = "💾 Guardar Ubicación";
    }

    private async void OnCerrarClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
        await _alCerrarCallback();
    }
}