
using System;
using System.Linq;
using TISW_TUSSI_ERP.Models.Ventas;
using TISW_TUSSI_ERP.ViewModels.Ventas;

namespace TISW_TUSSI_ERP.Views.Ventas;

public partial class HistorialVentasPage : ContentPage
{
    private readonly HistorialVentasViewModel _vm;

    public HistorialVentasPage(HistorialVentasViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CargarAsync();
    }

    private async void OnActualizarClicked(
        object sender,
        EventArgs e)
    {
        await _vm.CargarAsync();
    }

    private void OnLimpiarClicked(
        object sender,
        EventArgs e)
    {
        _vm.LimpiarFiltros();
    }

    private async void OnVentaSeleccionada(
        object sender,
        SelectionChangedEventArgs e)
    {
        var venta = e.CurrentSelection
            .FirstOrDefault() as VentaHistorial;

        if (venta != null)
            await _vm.SeleccionarVentaAsync(venta);
    }

    private void OnCerrarDetalleClicked(
        object sender,
        EventArgs e)
    {
        _vm.CerrarDetalle();
    }
}
