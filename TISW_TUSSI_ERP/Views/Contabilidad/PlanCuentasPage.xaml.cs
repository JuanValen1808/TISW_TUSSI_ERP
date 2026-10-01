using Microsoft.Maui.Controls;
using TISW_TUSSI_ERP.ViewModels.Contabilidad;

namespace TISW_TUSSI_ERP.Views.Contabilidad;

public partial class PlanCuentasPage : ContentPage
{
    private readonly PlanCuentasViewModel _viewModel;

    public PlanCuentasPage(PlanCuentasViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel.Cuentas.Count == 0)
        {
            _viewModel.CargarCuentasCommand.Execute(null);
        }
    }
}
