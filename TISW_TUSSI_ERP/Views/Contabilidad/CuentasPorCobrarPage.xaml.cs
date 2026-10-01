using TISW_TUSSI_ERP.ViewModels.Contabilidad;

namespace TISW_TUSSI_ERP.Views.Contabilidad;

public partial class CuentasPorCobrarPage : ContentPage
{
    private readonly CuentasPorCobrarViewModel _viewModel;

    public CuentasPorCobrarPage(CuentasPorCobrarViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.CargarDocumentosAsync();
    }
}
