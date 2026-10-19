using Microsoft.Maui.Controls;
using TISW_TUSSI_ERP.ViewModels.Contabilidad;

namespace TISW_TUSSI_ERP.Views.Contabilidad;

public partial class LibroMayorPage : ContentPage
{
    private readonly LibroMayorViewModel _viewModel;

    public LibroMayorPage(LibroMayorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel.SaldosMayores.Count == 0)
        {
            _viewModel.CargarSaldosCommand.Execute(null);
        }
    }
}
