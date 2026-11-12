using TISW_TUSSI_ERP.ViewModels.Inventario;

namespace TISW_TUSSI_ERP.Views.Inventario;

public partial class MermasPage : ContentPage
{
    private readonly MermasViewModel _viewModel;

    public MermasPage(MermasViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.CargarMermasAsync();
    }
}