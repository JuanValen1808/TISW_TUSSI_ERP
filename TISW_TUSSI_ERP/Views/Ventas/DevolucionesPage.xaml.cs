
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using TISW_TUSSI_ERP.Models.Ventas;
using TISW_TUSSI_ERP.Services.Api;
using TISW_TUSSI_ERP.ViewModels.Ventas;

namespace TISW_TUSSI_ERP.Views.Ventas;

public partial class DevolucionesPage : ContentPage
{
    private readonly DevolucionesViewModel _vm;

    private readonly SelectorVentasDevolucionService
        _servicioSelector;

    private readonly ObservableCollection<VentaParaDevolucion>
        _ventas = new();

    private bool _actualizandoCantidad;
    private bool _cargandoVentas;

    public DevolucionesPage(
        DevolucionesViewModel vm,
        SelectorVentasDevolucionService servicioSelector)
    {
        InitializeComponent();

        _vm = vm;
        _servicioSelector = servicioSelector;

        BindingContext = _vm;

        ListaVentas.ItemsSource = _ventas;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await CargarVentasAsync();
    }

    // LISTAR VENTAS

    private async Task CargarVentasAsync()
    {
        if (_cargandoVentas)
            return;

        _cargandoVentas = true;

        try
        {
            CargandoVentas.IsRunning = true;
            CargandoVentas.IsVisible = true;

            MensajeSelector.Text = "";

            string filtro = BusquedaVentas.Text ?? "";

            var ventas = await _servicioSelector
                .ListarVentasDisponiblesAsync(filtro);

            _ventas.Clear();

            foreach (var venta in ventas)
            {
                _ventas.Add(venta);
            }

            MensajeSelector.Text = _ventas.Count == 0
                ? "No se encontraron ventas con unidades disponibles."
                : $"{_ventas.Count} ventas encontradas. " +
                  "Selecciona una para cargar sus productos.";
        }
        catch (Exception ex)
        {
            MensajeSelector.Text =
                "No se pudieron consultar las ventas: " +
                ex.Message;
        }
        finally
        {
            CargandoVentas.IsRunning = false;
            CargandoVentas.IsVisible = false;

            _cargandoVentas = false;
        }
    }

    private async void OnBuscarVentasClicked(
        object sender,
        EventArgs e)
    {
        await CargarVentasAsync();
    }

    private async void OnBusquedaVentasCompleted(
        object sender,
        EventArgs e)
    {
        await CargarVentasAsync();
    }

    // SELECCIONAR VENTA

    private void OnSeleccionarVentaClicked(
        object sender,
        EventArgs e)
    {
        if (sender is not Button boton)
            return;

        if (boton.CommandParameter
            is not VentaParaDevolucion venta)
            return;

        if (_vm.EstaOcupado)
            return;

        _vm.FolioBuscado = venta.Folio;

        if (_vm.ComandoBuscar.CanExecute(null))
        {
            _vm.ComandoBuscar.Execute(null);
        }
    }

    // CANTIDADES

    private void OnCantidadTextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (_actualizandoCantidad)
            return;

        if (sender is not Entry entry)
            return;

        if (entry.BindingContext
            is not DetalleDevolucionVenta detalle)
            return;

        string texto = e.NewTextValue ?? "";

        if (!int.TryParse(
            texto,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out int cantidad))
        {
            cantidad = 0;
        }

        cantidad = Math.Clamp(
            cantidad,
            0,
            Math.Max(0, detalle.CantidadDisponible));

        _actualizandoCantidad = true;

        try
        {
            _vm.ActualizarCantidad(detalle, cantidad);

            string normalizado = cantidad.ToString(
                CultureInfo.InvariantCulture);

            if (entry.Text != normalizado)
                entry.Text = normalizado;
        }
        finally
        {
            _actualizandoCantidad = false;
        }
    }
}
