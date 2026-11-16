
using Microsoft.Extensions.Logging;
using TISW_TUSSI_ERP.Services.Api;
using TISW_TUSSI_ERP.Services.Navigation;
using TISW_TUSSI_ERP.ViewModels.Auth;
using TISW_TUSSI_ERP.ViewModels.Compras;
using TISW_TUSSI_ERP.ViewModels.Dashboard;
using TISW_TUSSI_ERP.ViewModels.Ventas;
using TISW_TUSSI_ERP.Views.Auth;
using TISW_TUSSI_ERP.Views.Compras;
using TISW_TUSSI_ERP.Views.Dashboard;
using TISW_TUSSI_ERP.Views.Ventas;
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
                    fonts.AddFont(
                        "OpenSans-Regular.ttf",
                        "OpenSansRegular");

                    fonts.AddFont(
                        "OpenSans-Semibold.ttf",
                        "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            // Servicios compartidos

            builder.Services.AddSingleton<DatabaseService>();
            builder.Services.AddSingleton<NavigationService>();
            builder.Services.AddSingleton<ComprasService>();
            builder.Services.AddSingleton<DashboardService>();
            builder.Services.AddSingleton<VentasService>();
            builder.Services.AddSingleton<ConveniosService>();

            // Control de Caja

            builder.Services.AddTransient<CajaService>();
            builder.Services.AddTransient<CajaViewModel>();
            builder.Services.AddTransient<CajaPage>();

            // Autenticación

            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<LoginPage>();

            // Dashboard

            builder.Services.AddTransient<DashboardViewModel>();
            builder.Services.AddTransient<DashboardPage>();

            // Compras

            builder.Services.AddTransient<OrdenesCompraViewModel>();
            builder.Services.AddTransient<OrdenesCompraPage>();

            // Ventas / POS

            builder.Services.AddTransient<PosViewModel>();
            builder.Services.AddTransient<PosPage>();

            // Historial de Ventas

            builder.Services.AddTransient<HistorialVentasService>();
            builder.Services.AddTransient<HistorialVentasViewModel>();
            builder.Services.AddTransient<HistorialVentasPage>();

            // Clientes

            builder.Services.AddSingleton<ClientesService>();
            builder.Services.AddTransient<ClientesViewModel>();
            builder.Services.AddTransient<ClientesPage>();

            // Convenios

            builder.Services.AddTransient<ConveniosViewModel>();
            builder.Services.AddTransient<ConveniosPage>();

            // Ranking de Ventas

            builder.Services.AddTransient<RankingVentasViewModel>();
            builder.Services.AddTransient<RankingVentasPage>();

            // Reporte Productos

            builder.Services.AddTransient<ReporteProductosService>();
            builder.Services.AddTransient<ReporteProductosViewModel>();
            builder.Services.AddTransient<ReporteProductosPage>();

            // Cuentas por Cobrar

            builder.Services.AddSingleton<CuentasPorCobrarVentasService>();
            builder.Services.AddTransient<CuentasPorCobrarVentasViewModel>();
            builder.Services.AddTransient<CuentasPorCobrarVentasPage>();

            // Devoluciones

            builder.Services.AddSingleton<DevolucionesService>();

            // Servicio independiente para seleccionar ventas
            builder.Services.AddTransient<SelectorVentasDevolucionService>();

            builder.Services.AddTransient<DevolucionesViewModel>();
            builder.Services.AddTransient<DevolucionesPage>();

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

            builder.Services.AddTransient<CuentasPorCobrarViewModel>();
            builder.Services.AddTransient<CuentasPorCobrarPage>();

            builder.Services.AddTransient<CuentasPorPagarViewModel>();
            builder.Services.AddTransient<CuentasPorPagarPage>();

            // Administración

            builder.Services.AddSingleton<SeguridadService>();

            builder.Services.AddTransient<
                TISW_TUSSI_ERP.ViewModels.Seguridad.UsuariosViewModel>();

            builder.Services.AddTransient<
                TISW_TUSSI_ERP.Views.Admin.UsuariosPage>();

            builder.Services.AddTransient<
                TISW_TUSSI_ERP.ViewModels.Seguridad.PerfilesViewModel>();

            builder.Services.AddTransient<
                TISW_TUSSI_ERP.Views.Admin.PerfilesPage>();

            builder.Services.AddTransient<
                TISW_TUSSI_ERP.ViewModels.Seguridad.AccesosViewModel>();

            builder.Services.AddTransient<
                TISW_TUSSI_ERP.Views.Admin.AccesosPage>();

            return builder.Build();
        }
    }
}
