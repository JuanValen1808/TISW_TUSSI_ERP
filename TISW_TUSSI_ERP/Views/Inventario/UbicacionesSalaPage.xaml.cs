using TISW_TUSSI_ERP.ViewModels.Inventario;

namespace TISW_TUSSI_ERP.Views.Inventario;

public partial class UbicacionesSalaPage : ContentPage
{
    private readonly UbicacionesSalaViewModel _vm;

    public UbicacionesSalaPage(UbicacionesSalaViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CargarAsync();
    }
}