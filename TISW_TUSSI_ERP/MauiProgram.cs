using Microsoft.Extensions.Logging;
using TISW_TUSSI_ERP.Services.Api;
using TISW_TUSSI_ERP.Services.Navigation;
using TISW_TUSSI_ERP.ViewModels.Auth;
using TISW_TUSSI_ERP.ViewModels.Compras;
using TISW_TUSSI_ERP.ViewModels.Dashboard;
using TISW_TUSSI_ERP.ViewModels.Inventario; // <-- AÑADIR
using TISW_TUSSI_ERP.Views.Auth;
using TISW_TUSSI_ERP.Views.Compras;
using TISW_TUSSI_ERP.Views.Dashboard;
using TISW_TUSSI_ERP.Views.Inventario; // <-- AÑADIR

namespace TISW_TUSSI_ERP
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            // ---------------- Inyección de dependencias ----------------

            // Servicios compartidos por todos los módulos
            builder.Services.AddSingleton<DatabaseService>();
            builder.Services.AddSingleton<NavigationService>();
            builder.Services.AddSingleton<ComprasService>();
            builder.Services.AddSingleton<DashboardService>();
            builder.Services.AddSingleton<InventarioService>(); // <-- AÑADIR (Si usas un servicio de inventario)

            // Auth
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<LoginPage>();

            // Dashboard
            builder.Services.AddTransient<DashboardViewModel>();
            builder.Services.AddTransient<DashboardPage>();

            // Compras
            builder.Services.AddTransient<OrdenesCompraViewModel>();
            builder.Services.AddTransient<OrdenesCompraPage>();

            // Inventario / Catálogo (AÑADIR)
            builder.Services.AddTransient<InventarioViewModel>();
            builder.Services.AddTransient<InventarioPage>();

            return builder.Build();
        }
    }
}
