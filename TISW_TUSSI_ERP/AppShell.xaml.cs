using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Shapes;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Auth;
using TISW_TUSSI_ERP.Views.Auth;

namespace TISW_TUSSI_ERP;

public partial class AppShell : Shell
{
    private record ItemMenu(string Icono, string Titulo, string Ruta, string Seccion);
    private record SeccionMenu(string Nombre, ItemMenu[] Items);

    // Rutas implementadas en el portal
    private static readonly HashSet<string> Implementadas = new()
    {
        "DashboardPage",
        "OrdenesCompraPage",
        "PosPage",
        "ClientesPage",
        "ConveniosPage",
        "RankingVentasPage",
        "ProveedoresPage",
        "ReporteComprasPage",
        "PlanCuentasPage",
        "LibroDiarioPage",
        "LibroMayorPage",
        "BalanceOchoColumnasPage",
        "EstadoResultadosPage",
        "MayorCentralizadoPage",
        "CuentasPorCobrarPage",
        "CuentasPorPagarPage",
        "AntiguedadSaldosPage",
        "CentralizadorImpuestosPage",
        "BalanceGeneralPage",
        "FlujoCajaPage",
        "UsuariosPage",
        "PerfilesPage",
        "AccesosPage"
    };

    private static readonly SeccionMenu[] Secciones =
    {
        new("PRINCIPAL", new[]
        {
            new ItemMenu("", "Dashboard", "DashboardPage", "PRINCIPAL")
        }),
        new("OPERACIÓN", new[]
        {
            new ItemMenu("", "Punto de Venta", "PosPage", "OPERACIÓN"),
            new ItemMenu("", "Control de Caja", "CajaPage", "OPERACIÓN"),
            new ItemMenu("", "Clientes", "ClientesPage", "OPERACIÓN"),
            new ItemMenu("", "Convenios", "ConveniosPage", "OPERACIÓN"),
            new ItemMenu("", "Ranking de Ventas", "RankingVentasPage", "OPERACIÓN"),
        }),
        new("INVENTARIOS", new[]
        {
            new ItemMenu("", "Catálogo SKUs", "CatalogoPage", "INVENTARIOS"),
            new ItemMenu("", "Kardex y Lotes", "KardexPage", "INVENTARIOS"),
            new ItemMenu("", "Mermas", "MermasPage", "INVENTARIOS"),
        }),
        new("COMPRAS", new[]
        {
            new ItemMenu("", "Proveedores", "ProveedoresPage", "COMPRAS"),
            new ItemMenu("", "Órdenes de Compra", "OrdenesCompraPage", "COMPRAS"),
            new ItemMenu("", "Recepción Facturas", "RecepcionPage", "COMPRAS"),
            new ItemMenu("", "Reporte de Compras", "ReporteComprasPage", "COMPRAS"),
        }),
        new("CONTABILIDAD", new[]
        {
            new ItemMenu("", "Plan de Cuentas (RF-01)", "PlanCuentasPage", "CONTABILIDAD"),
            new ItemMenu("", "Libro Diario (RF-02)", "LibroDiarioPage", "CONTABILIDAD"),
            new ItemMenu("", "Libro Mayor (RF-03)", "LibroMayorPage", "CONTABILIDAD"),
            new ItemMenu("", "Balance General (RF-03)", "BalanceGeneralPage", "CONTABILIDAD"),
            new ItemMenu("", "Balance 8 Columnas (RF-04)", "BalanceOchoColumnasPage", "CONTABILIDAD"),
            new ItemMenu("", "Estado de Resultados (RF-05)", "EstadoResultadosPage", "CONTABILIDAD"),
            new ItemMenu("", "Mayor Centralizado (RF-06)", "MayorCentralizadoPage", "CONTABILIDAD"),
            new ItemMenu("", "Cuentas por Cobrar (RF-07)", "CuentasPorCobrarPage", "CONTABILIDAD"),
            new ItemMenu("", "Cuentas por Pagar (RF-07)", "CuentasPorPagarPage", "CONTABILIDAD"),
            new ItemMenu("", "Antigüedad de Saldos (RF-08)", "AntiguedadSaldosPage", "CONTABILIDAD"),
            new ItemMenu("", "Impuestos y F29 SII (RF-09)", "CentralizadorImpuestosPage", "CONTABILIDAD"),
            new ItemMenu("", "Flujo de Caja", "FlujoCajaPage", "CONTABILIDAD"),
        }),
        new("ADMINISTRACIÓN", new[]
        {
            new ItemMenu("", "Usuarios (RF-POR-02)", "UsuariosPage", "ADMINISTRACIÓN"),
            new ItemMenu("", "Perfiles", "PerfilesPage", "ADMINISTRACIÓN"),
            new ItemMenu("", "Accesos RBAC (RF-POR-03)", "AccesosPage", "ADMINISTRACIÓN"),
        }),
    };

    private readonly List<(Border Borde, Label Texto, string Ruta)> _items = new();

    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(Views.Compras.NuevaOrdenPage), typeof(Views.Compras.NuevaOrdenPage));
        ConstruirMenu();

        var u = Sesion.UsuarioActual;
        NombreUsuario.Text = u?.NombreCompleto ?? "Invitado";
        RolUsuario.Text = u?.NombreRol ?? "";
        InicialesUsuario.Text = Formato.Iniciales(u?.NombreCompleto ?? "");

        var tapSalir = new TapGestureRecognizer();
        tapSalir.Tapped += (_, _) => CerrarSesion();
        CerrarSesionLabel.GestureRecognizers.Add(tapSalir);

        Navigated += (_, e) => Resaltar(e.Current?.Location?.ToString() ?? "");
        Resaltar("DashboardPage");
    }

    private void ConstruirMenu()
    {
        foreach (var seccion in Secciones)
        {
            // Despliegue dinámico y Control de Accesos RBAC (RF-POR-01 / RF-POR-03)
            if (!Sesion.PuedeAccederSeccion(seccion.Nombre))
                continue;

            Menu.Add(new Label
            {
                Text = seccion.Nombre,
                FontSize = 10,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#9CA3AF"),
                Margin = new Thickness(12, 14, 0, 4)
            });

            foreach (var item in seccion.Items)
            {
                var texto = new Label
                {
                    Text = string.IsNullOrEmpty(item.Icono) ? item.Titulo : $"{item.Icono}   {item.Titulo}",
                    FontSize = 13,
                    TextColor = Color.FromArgb("#374151")
                };
                var borde = new Border
                {
                    StrokeThickness = 0,
                    StrokeShape = new RoundRectangle { CornerRadius = 8 },
                    Padding = new Thickness(12, 9),
                    BackgroundColor = Colors.Transparent,
                    Content = texto
                };
                var tap = new TapGestureRecognizer();
                tap.Tapped += async (_, _) => await Navegar(item);
                borde.GestureRecognizers.Add(tap);

                Menu.Add(borde);
                _items.Add((borde, texto, item.Ruta));
            }
        }
    }

    private async Task Navegar(ItemMenu item)
    {
        // Middleware RBAC en navegación (RF-POR-03)
        if (!Sesion.PuedeAccederSeccion(item.Seccion))
        {
            await DisplayAlertAsync("Acceso Restringido (RBAC)",
                $"Tu perfil actual ({Sesion.UsuarioActual?.NombreRol ?? "Invitado"}) no posee permisos para acceder al módulo '{item.Seccion}'.",
                "Entendido");
            return;
        }

        if (Implementadas.Contains(item.Ruta))
            await GoToAsync($"//{item.Ruta}");
        else
            await DisplayAlertAsync("Módulo en desarrollo", $"\"{item.Titulo}\" aún no está implementado.", "OK");
    }

    private void Resaltar(string ubicacion)
    {
        foreach (var (borde, texto, ruta) in _items)
        {
            var activo = ubicacion.Contains(ruta);
            borde.BackgroundColor = activo ? Color.FromArgb("#E8F1FD") : Colors.Transparent;
            texto.TextColor = activo ? Color.FromArgb("#1D6FD8") : Color.FromArgb("#374151");
            texto.FontAttributes = activo ? FontAttributes.Bold : FontAttributes.None;
        }
    }

    private static void CerrarSesion()
    {
        Sesion.CerrarSesion();

        var login = IPlatformApplication.Current!
            .Services.GetRequiredService<LoginPage>();

        var ventana = Application.Current?.Windows.FirstOrDefault();

        if (ventana != null)
        {
            ventana.Page = new NavigationPage(login);
        }
    }
}
