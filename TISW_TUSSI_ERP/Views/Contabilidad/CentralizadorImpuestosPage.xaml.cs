using TISW_TUSSI_ERP.ViewModels.Contabilidad;

namespace TISW_TUSSI_ERP.Views.Contabilidad;

public partial class CentralizadorImpuestosPage : ContentPage
{
    private readonly CentralizadorImpuestosViewModel _viewModel;

    public CentralizadorImpuestosPage(CentralizadorImpuestosViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.CargarCommand.Execute(null);
    }
}
