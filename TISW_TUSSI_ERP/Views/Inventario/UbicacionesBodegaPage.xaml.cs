using TISW_TUSSI_ERP.ViewModels.Inventario;

namespace TISW_TUSSI_ERP.Views.Inventario;

public partial class UbicacionesBodegaPage : ContentPage
{
    private readonly UbicacionesBodegaViewModel _vm;

    public UbicacionesBodegaPage(UbicacionesBodegaViewModel vm)
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