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
using TISW_TUSSI_ERP.ViewModels.Inventario;
using TISW_TUSSI_ERP.Views.Inventario;

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
            builder.Services.AddSingleton<VentasService>();
            builder.Services.AddSingleton<ConveniosService>();
            builder.Services.AddSingleton<InventarioService>();
            builder.Services.AddTransient<ReporteService>();

            // Auth
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<LoginPage>();

            // Dashboard
            builder.Services.AddTransient<DashboardViewModel>();
            builder.Services.AddTransient<DashboardPage>();

            // Compras
            builder.Services.AddTransient<OrdenesCompraViewModel>();
            builder.Services.AddTransient<OrdenesCompraPage>();
            builder.Services.AddTransient<ProveedoresViewModel>();
            builder.Services.AddTransient<ProveedoresPage>();
            builder.Services.AddTransient<ReporteComprasViewModel>();
            builder.Services.AddTransient<ReporteComprasPage>();
            builder.Services.AddTransient<NuevaOrdenViewModel>();
            builder.Services.AddTransient<NuevaOrdenPage>();
            builder.Services.AddTransient<RecepcionFacturasViewModel>();
            builder.Services.AddTransient<RecepcionFacturasPage>();
            builder.Services.AddTransient<ReporteCumplimientoViewModel>();
            builder.Services.AddTransient<ReporteCumplimientoPage>();
            builder.Services.AddTransient<ReporteHistorialPreciosViewModel>();
            builder.Services.AddTransient<ReporteHistorialPreciosPage>();

            // Ventas
            builder.Services.AddTransient<PosViewModel>();
            builder.Services.AddTransient<PosPage>();
            builder.Services.AddSingleton<ClientesService>();
            builder.Services.AddTransient<ClientesViewModel>();
            builder.Services.AddTransient<ClientesPage>();
            builder.Services.AddTransient<RankingVentasViewModel>();
            builder.Services.AddTransient<RankingVentasPage>();
            builder.Services.AddTransient<ConveniosViewModel>();
            builder.Services.AddTransient<ConveniosPage>();

            // Contabilidad (RF-CON-01 a RF-CON-09)
            builder.Services.AddSingleton<ContabilidadService>();
            builder.Services.AddSingleton<ReportesContablesService>();
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
            builder.Services.AddTransient<AntiguedadSaldosViewModel>();
            builder.Services.AddTransient<AntiguedadSaldosPage>();
            builder.Services.AddTransient<CentralizadorImpuestosViewModel>();
            builder.Services.AddTransient<CentralizadorImpuestosPage>();
            builder.Services.AddTransient<BalanceGeneralViewModel>();
            builder.Services.AddTransient<BalanceGeneralPage>();
            builder.Services.AddTransient<FlujoCajaViewModel>();
            builder.Services.AddTransient<FlujoCajaPage>();

#if WINDOWS
            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("Borderless", (handler, view) =>
            {
                handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
                handler.PlatformView.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
            });
            Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("Borderless", (handler, view) =>
            {
                handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
                handler.PlatformView.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
            });
            Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping(nameof(Microsoft.Maui.IPicker.Title), (handler, view) =>
            {
                handler.PlatformView.Header = null;
                handler.PlatformView.PlaceholderText = view.Title;
            });
#endif

            // Administración / Seguridad (Usuarios, Perfiles, Accesos - RF-POR-02 y RF-POR-03)
            builder.Services.AddSingleton<SeguridadService>();
            builder.Services.AddTransient<TISW_TUSSI_ERP.ViewModels.Seguridad.UsuariosViewModel>();
            builder.Services.AddTransient<TISW_TUSSI_ERP.Views.Admin.UsuariosPage>();
            builder.Services.AddTransient<TISW_TUSSI_ERP.ViewModels.Seguridad.PerfilesViewModel>();
            builder.Services.AddTransient<TISW_TUSSI_ERP.Views.Admin.PerfilesPage>();
            builder.Services.AddTransient<TISW_TUSSI_ERP.ViewModels.Seguridad.AccesosViewModel>();
            builder.Services.AddTransient<TISW_TUSSI_ERP.Views.Admin.AccesosPage>();

            // Inventario
            builder.Services.AddTransient<InventarioViewModel>();
            builder.Services.AddTransient<InventarioPage>();
            builder.Services.AddTransient<KardexViewModel>();
            builder.Services.AddTransient<KardexPage>();
            builder.Services.AddTransient<MermasViewModel>();
            builder.Services.AddTransient<MermasPage>();

            return builder.Build();
        }
    }
}
