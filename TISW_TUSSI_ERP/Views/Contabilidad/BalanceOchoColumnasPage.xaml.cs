using Microsoft.Maui.Controls;
using TISW_TUSSI_ERP.ViewModels.Contabilidad;

namespace TISW_TUSSI_ERP.Views.Contabilidad;

public partial class BalanceOchoColumnasPage : ContentPage
{
    private readonly BalanceOchoColumnasViewModel _viewModel;

    public BalanceOchoColumnasPage(BalanceOchoColumnasViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel.Items.Count == 0)
        {
            _viewModel.CargarBalanceCommand.Execute(null);
        }
    }
}
