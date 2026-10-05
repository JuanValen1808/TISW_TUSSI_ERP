using TISW_TUSSI_ERP.ViewModels.Auth;

namespace TISW_TUSSI_ERP.Views.Auth;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        // En móvil ocultamos el panel de branding y el formulario ocupa todo el ancho.
        if (DeviceInfo.Idiom != DeviceIdiom.Desktop)
        {
            PanelIzquierdo.IsVisible = false;
            Grid.SetColumn(PanelDerecho, 0);
            Grid.SetColumnSpan(PanelDerecho, 2);
            Formulario.WidthRequest = -1;
            Formulario.HorizontalOptions = LayoutOptions.Fill;
            Formulario.Padding = new Thickness(24, 30);
        }
    }
}
