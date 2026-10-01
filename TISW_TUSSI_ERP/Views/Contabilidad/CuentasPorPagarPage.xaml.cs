using TISW_TUSSI_ERP.ViewModels.Contabilidad;

namespace TISW_TUSSI_ERP.Views.Contabilidad;

public partial class CuentasPorPagarPage : ContentPage
{
    private readonly CuentasPorPagarViewModel _viewModel;

    public CuentasPorPagarPage(CuentasPorPagarViewModel viewModel)
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
