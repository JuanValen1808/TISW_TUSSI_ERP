
using System;
using TISW_TUSSI_ERP.ViewModels.Ventas;

namespace TISW_TUSSI_ERP.Views.Ventas;

public partial class PosPage : ContentPage
{
    private readonly PosViewModel _vm;

    public PosPage(PosViewModel viewModel)
    {
        InitializeComponent();

        _vm = viewModel;
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            await _vm.CargarAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Error al cargar el POS",
                ex.Message,
                "Aceptar");
        }
    }
}
