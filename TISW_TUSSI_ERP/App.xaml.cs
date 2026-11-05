using Microsoft.Extensions.DependencyInjection;
using TISW_TUSSI_ERP.ViewModels.Auth;

namespace TISW_TUSSI_ERP
{
    public partial class App : Application
    {
        public App(LoginViewModel loginViewModel)
        {
            InitializeComponent();
<<<<<<< HEAD
            // Fuerza el tema claro en toda la app sin importar cómo esté configurado el sistema del usuario
            Application.Current!.UserAppTheme = AppTheme.Light;
=======
            Application.Current.UserAppTheme = AppTheme.Light;
>>>>>>> a0f867ad90cd1e308d70330d2abd90a39f4b5680

            // Arranca la aplicación directamente en la pantalla de Login
            MainPage = new Views.Auth.LoginPage(loginViewModel);
        }
    }
}