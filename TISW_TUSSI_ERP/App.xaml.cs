using TISW_TUSSI_ERP.Views.Auth;

namespace TISW_TUSSI_ERP;

public partial class App : Application
{
    public App(LoginPage loginPage)
    {
        InitializeComponent();

        // La app siempre arranca en el Login, ignorando el Shell inicial.
        // Solo al validar credenciales correctas se reemplaza MainPage por AppShell.
        MainPage = new NavigationPage(loginPage);
    }
}
