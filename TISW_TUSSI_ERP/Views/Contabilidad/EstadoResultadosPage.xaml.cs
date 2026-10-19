using Microsoft.Maui.Controls;
using TISW_TUSSI_ERP.ViewModels.Contabilidad;

namespace TISW_TUSSI_ERP.Views.Contabilidad;

public partial class EstadoResultadosPage : ContentPage
{
    private readonly EstadoResultadosViewModel _viewModel;

    public EstadoResultadosPage(EstadoResultadosViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel.Ingresos.Count == 0 && _viewModel.Gastos.Count == 0)
        {
            _viewModel.CargarEstadoResultadosCommand.Execute(null);
        }
    }
}
