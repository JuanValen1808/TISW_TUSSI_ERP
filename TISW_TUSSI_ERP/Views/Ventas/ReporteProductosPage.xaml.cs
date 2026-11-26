
using TISW_TUSSI_ERP.ViewModels.Ventas;

namespace TISW_TUSSI_ERP.Views.Ventas;

public partial class ReporteProductosPage : ContentPage
{
    private readonly ReporteProductosViewModel _vm;

    public ReporteProductosPage(
        ReporteProductosViewModel vm)
    {
        InitializeComponent();

        _vm = vm;
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await _vm.CargarAsync();
    }
}
