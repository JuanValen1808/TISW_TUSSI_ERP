using Microsoft.Extensions.Logging;
using TISW_TUSSI_ERP.Services.Api;
using TISW_TUSSI_ERP.Services.Navigation;
using TISW_TUSSI_ERP.ViewModels.Auth;
using TISW_TUSSI_ERP.ViewModels.Compras;
using TISW_TUSSI_ERP.ViewModels.Dashboard;
using TISW_TUSSI_ERP.Views.Auth;
using TISW_TUSSI_ERP.Views.Compras;
using TISW_TUSSI_ERP.Views.Dashboard;
using TISW_TUSSI_ERP.ViewModels.Contabilidad;
using TISW_TUSSI_ERP.Views.Contabilidad;

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

            // Auth
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<LoginPage>();

            // Dashboard
            builder.Services.AddTransient<DashboardViewModel>();
            builder.Services.AddTransient<DashboardPage>();

            // Compras
            builder.Services.AddTransient<OrdenesCompraViewModel>();
            builder.Services.AddTransient<OrdenesCompraPage>();

            // Contabilidad
            builder.Services.AddSingleton<ContabilidadService>();
            builder.Services.AddTransient<PlanCuentasViewModel>();
            builder.Services.AddTransient<PlanCuentasPage>();
            builder.Services.AddTransient<LibroDiarioViewModel>();
            builder.Services.AddTransient<LibroDiarioPage>();
            builder.Services.AddTransient<LibroMayorViewModel>();
            builder.Services.AddTransient<LibroMayorPage>();
            builder.Services.AddTransient<BalanceOchoColumnasViewModel>();
            builder.Services.AddTransient<BalanceOchoColumnasPage>();
            builder.Services.AddTransient<EstadoResultadosViewModel>();
            builder.Services.AddTransient<EstadoResultadosPage>();
            builder.Services.AddTransient<MayorCentralizadoViewModel>();
            builder.Services.AddTransient<MayorCentralizadoPage>();

            return builder.Build();
        }
    }
}
