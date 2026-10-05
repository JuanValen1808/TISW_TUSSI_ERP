using TISW_TUSSI_ERP.ViewModels.Compras;

namespace TISW_TUSSI_ERP.Views.Compras;

public partial class ReporteComprasPage : ContentPage
{
    private readonly ReporteComprasViewModel _vm;

    public ReporteComprasPage(ReporteComprasViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _vm = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.GenerarAsync();
    }
}
