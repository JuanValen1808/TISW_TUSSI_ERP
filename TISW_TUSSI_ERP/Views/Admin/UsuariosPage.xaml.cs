using TISW_TUSSI_ERP.ViewModels.Seguridad;

namespace TISW_TUSSI_ERP.Views.Admin;

public partial class UsuariosPage : ContentPage
{
    private readonly UsuariosViewModel _vm;

    public UsuariosPage(UsuariosViewModel vm)
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
