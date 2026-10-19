using TISW_TUSSI_ERP.ViewModels.Seguridad;

namespace TISW_TUSSI_ERP.Views.Admin;

public partial class PerfilesPage : ContentPage
{
    private readonly PerfilesViewModel _vm;

    public PerfilesPage(PerfilesViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CargarPerfilesAsync();
    }
}
