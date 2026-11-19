using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Auth;
using TISW_TUSSI_ERP.Models.Inventario;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.Views.Inventario;

public partial class GestionUbicacionesSalaModalPage : ContentPage
{
    private readonly InventarioService _servicio;
    private readonly Func<Task> _alCerrarCallback;
    private UbicacionModel? _ubicacionEnEdicion;

    public GestionUbicacionesSalaModalPage(InventarioService servicio, Func<Task> alCerrarCallback)
    {
        InitializeComponent();
        _servicio = servicio;
        _alCerrarCallback = alCerrarCallback;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarListaAsync();
    }

    private async Task CargarListaAsync()
    {
        var lista = await _servicio.ObtenerUbicacionesAsync("SALA_VENTAS", soloActivas: false);
        ListaUbicacionesSala.ItemsSource = lista;
    }

    private async void OnGuardarClicked(object sender, EventArgs e)
    {
        string rolActual = Sesion.UsuarioActual?.NombreRol ?? "Encargado de Inventario";

        var modelo = new UbicacionModel
        {
            IdUbicacion = _ubicacionEnEdicion?.IdUbicacion ?? 0,
            TipoZona = "SALA_VENTAS",
            CodigoUbicacion = TxtCodigo.Text ?? string.Empty,
            SectorPasillo = TxtZona.Text ?? string.Empty,
            EstanteMueble = TxtSeccion.Text ?? string.Empty,
            RepisaNivel = TxtNivel.Text ?? string.Empty,
            Descripcion = TxtDescripcion.Text ?? string.Empty
        };

        var (exito, mensaje) = _ubicacionEnEdicion == null
            ? await _servicio.CrearUbicacionAsync(modelo, rolActual)
            : await _servicio.ActualizarUbicacionSalaAsync(modelo, rolActual);

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
            LblModoFormulario.Text = $"Editando Exhibidor: {item.CodigoUbicacion}";
            TxtCodigo.Text = item.CodigoUbicacion;
            TxtZona.Text = item.SectorPasillo;
            TxtSeccion.Text = item.EstanteMueble;
            TxtNivel.Text = item.RepisaNivel;
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
        LblModoFormulario.Text = "Nueva Ubicación en Sala de Ventas";
        TxtCodigo.Text = string.Empty;
        TxtZona.Text = string.Empty;
        TxtSeccion.Text = string.Empty;
        TxtNivel.Text = string.Empty;
        TxtDescripcion.Text = string.Empty;
        BtnLimpiar.IsVisible = false;
        BtnGuardar.Text = "💾 Guardar Exhibidor";
    }

    private async void OnCerrarClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
        await _alCerrarCallback();
    }
}