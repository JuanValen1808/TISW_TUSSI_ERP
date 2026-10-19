using Microsoft.Maui.Controls;
using TISW_TUSSI_ERP.ViewModels.Contabilidad;

namespace TISW_TUSSI_ERP.Views.Contabilidad;

public partial class LibroDiarioPage : ContentPage
{
    private readonly LibroDiarioViewModel _viewModel;

    public LibroDiarioPage(LibroDiarioViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel.Asientos.Count == 0)
        {
            _viewModel.CargarAsientosCommand.Execute(null);
        }
    }
}
