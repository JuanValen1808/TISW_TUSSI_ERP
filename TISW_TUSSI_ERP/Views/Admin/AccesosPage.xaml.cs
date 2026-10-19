using TISW_TUSSI_ERP.ViewModels.Seguridad;

namespace TISW_TUSSI_ERP.Views.Admin;

public partial class AccesosPage : ContentPage
{
    private readonly AccesosViewModel _vm;

    public AccesosPage(AccesosViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CargarDatosInicialesAsync();
    }
}
