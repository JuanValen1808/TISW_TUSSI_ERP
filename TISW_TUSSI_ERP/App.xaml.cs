using Microsoft.Extensions.DependencyInjection;
using TISW_TUSSI_ERP.ViewModels.Auth;

namespace TISW_TUSSI_ERP
{
    public partial class App : Application
    {
        public App(LoginViewModel loginViewModel)
        {
            InitializeComponent();

            // Arranca la aplicación directamente en la pantalla de Login
            MainPage = new Views.Auth.LoginPage(loginViewModel);
        }
    }
}