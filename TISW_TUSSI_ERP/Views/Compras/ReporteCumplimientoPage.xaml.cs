using TISW_TUSSI_ERP.ViewModels.Compras;

namespace TISW_TUSSI_ERP.Views.Compras;

public partial class ReporteCumplimientoPage : ContentPage
{
    private readonly ReporteCumplimientoViewModel _viewModel;

    public ReporteCumplimientoPage(ReporteCumplimientoViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.CargarInicialAsync();
    }
}

