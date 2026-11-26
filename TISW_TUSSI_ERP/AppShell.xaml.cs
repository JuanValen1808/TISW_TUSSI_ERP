using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Shapes;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Auth;
using TISW_TUSSI_ERP.Views.Auth;

namespace TISW_TUSSI_ERP;

public partial class AppShell : Shell
{
    public record ItemMenu(string Icono, string Titulo, string Ruta, string Seccion, string Permiso = "");
    public record SeccionMenu(string Nombre, ItemMenu[] Items);

    public static AppShell? InstanciaActual { get; private set; }

    // Rutas implementadas en el portal
    private static readonly HashSet<string> Implementadas = new()
    {
        "DashboardPage",
        "OrdenesCompraPage",
        "PosPage",
        "HistorialVentasPage",
        "ClientesPage",
        "ConveniosPage",
        "RankingVentasPage",
        "ReporteProductosPage",
        "CuentasPorCobrarVentasPage",
        "DevolucionesPage",
        "CajaPage",
        "ProveedoresPage",
        "RecepcionFacturasPage",
        "ReporteComprasPage",
        "ReporteCumplimientoPage",
        "ReporteHistorialPreciosPage",
        "InventarioPage",
        "KardexPage",
        "MermasPage",
        "UbicacionesBodegaPage",
        "UbicacionesSalaPage",
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

    public static readonly SeccionMenu[] Secciones =
    {
        new("PRINCIPAL", new[]
        {
            new ItemMenu("", "Dashboard", "DashboardPage", "PRINCIPAL", "")
        }),
        new("VENTAS", new[]
        {
            new ItemMenu("", "Punto de Venta", "PosPage", "VENTAS", "VENTAS_POS"),
            new ItemMenu("", "Historial de Ventas", "HistorialVentasPage", "VENTAS", "VENTAS_HISTORIAL"),
            new ItemMenu("", "Control de Caja", "CajaPage", "VENTAS", "VENTAS_CAJA"),
            new ItemMenu("", "Clientes", "ClientesPage", "VENTAS", "VENTAS_CLIENTES"),
            new ItemMenu("", "Convenios", "ConveniosPage", "VENTAS", "VENTAS_CLIENTES"),
            new ItemMenu("", "Ranking de Ventas", "RankingVentasPage", "VENTAS", "VENTAS_REPORTES"),
            new ItemMenu("", "Ventas por Producto y Categoría", "ReporteProductosPage", "VENTAS", "VENTAS_REPORTES"),
            new ItemMenu("", "Cuentas por Cobrar (Ventas)", "CuentasPorCobrarVentasPage", "VENTAS", "VENTAS_CXC_DEVOLUCIONES"),
            new ItemMenu("", "Devoluciones de Ventas", "DevolucionesPage", "VENTAS", "VENTAS_CXC_DEVOLUCIONES"),
        }),
        new("INVENTARIOS", new[]
        {
            new ItemMenu("", "Catálogo SKUs", "InventarioPage", "INVENTARIOS", "INVENTARIO_CATALOGO"),
            new ItemMenu("", "Kardex y Lotes", "KardexPage", "INVENTARIOS", "INVENTARIO_KARDEX"),
            new ItemMenu("", "Mermas", "MermasPage", "INVENTARIOS", "INVENTARIO_MERMAS"),
            new ItemMenu("", "Ubicaciones Bodega", "UbicacionesBodegaPage", "INVENTARIOS", "INVENTARIO_UBICACIONES_BODEGA"),
            new ItemMenu("", "Sala de Ventas", "UbicacionesSalaPage", "INVENTARIOS", "INVENTARIO_UBICACIONES_SALA"),
        }),
        new("COMPRAS", new[]
        {
            new ItemMenu("", "Proveedores", "ProveedoresPage", "COMPRAS", "COMPRAS_PROVEEDORES"),
            new ItemMenu("", "Órdenes de Compra", "OrdenesCompraPage", "COMPRAS", "COMPRAS_ORDENES"),
            new ItemMenu("", "Recepción Facturas", "RecepcionFacturasPage", "COMPRAS", "COMPRAS_RECEPCION"),
            new ItemMenu("", "Reporte Compras Lab.", "ReporteComprasPage", "COMPRAS", "COMPRAS_REPORTES"),
            new ItemMenu("", "Reporte Cumplimiento", "ReporteCumplimientoPage", "COMPRAS", "COMPRAS_REPORTES"),
            new ItemMenu("", "Historial de Precios", "ReporteHistorialPreciosPage", "COMPRAS", "COMPRAS_REPORTES"),
        }),
        new("CONTABILIDAD", new[]
        {
            new ItemMenu("", "Plan de Cuentas", "PlanCuentasPage", "CONTABILIDAD", "CONTAB_PLAN_CUENTAS"),
            new ItemMenu("", "Libro Diario", "LibroDiarioPage", "CONTABILIDAD", "CONTAB_LIBRO_DIARIO"),
            new ItemMenu("", "Libro Mayor", "LibroMayorPage", "CONTABILIDAD", "CONTAB_MAYOR_BALANCES"),
            new ItemMenu("", "Balance General", "BalanceGeneralPage", "CONTABILIDAD", "CONTAB_MAYOR_BALANCES"),
            new ItemMenu("", "Balance 8 Columnas", "BalanceOchoColumnasPage", "CONTABILIDAD", "CONTAB_MAYOR_BALANCES"),
            new ItemMenu("", "Estado de Resultados", "EstadoResultadosPage", "CONTABILIDAD", "CONTAB_ESTADO_RESULTADOS"),
            new ItemMenu("", "Mayor Centralizado", "MayorCentralizadoPage", "CONTABILIDAD", "CONTAB_MAYOR_CENTRALIZADO"),
            new ItemMenu("", "Cuentas por Cobrar", "CuentasPorCobrarPage", "CONTABILIDAD", "CONTAB_CXC_CXP"),
            new ItemMenu("", "Cuentas por Pagar", "CuentasPorPagarPage", "CONTABILIDAD", "CONTAB_CXC_CXP"),
            new ItemMenu("", "Antigüedad de Saldos", "AntiguedadSaldosPage", "CONTABILIDAD", "CONTAB_CXC_CXP"),
            new ItemMenu("", "Impuestos y F29 SII", "CentralizadorImpuestosPage", "CONTABILIDAD", "CONTAB_IMPUESTOS_SII"),
            new ItemMenu("", "Flujo de Caja", "FlujoCajaPage", "CONTABILIDAD", "CONTAB_FLUJO_CAJA"),
        }),
        new("ADMINISTRACIÓN", new[]
        {
            new ItemMenu("", "Usuarios", "UsuariosPage", "ADMINISTRACIÓN", "ADMIN_USUARIOS"),
            new ItemMenu("", "Perfiles", "PerfilesPage", "ADMINISTRACIÓN", "ADMIN_RBAC"),
            new ItemMenu("", "Accesos y Permisos", "AccesosPage", "ADMINISTRACIÓN", "ADMIN_RBAC"),
        }),
    };

    private readonly List<(Border Borde, Label Texto, string Ruta)> _items = new();

    public AppShell()
    {
        InitializeComponent();
        InstanciaActual = this;
        Routing.RegisterRoute(nameof(Views.Compras.NuevaOrdenPage), typeof(Views.Compras.NuevaOrdenPage));
        Routing.RegisterRoute(nameof(Views.Compras.RecepcionFacturasPage), typeof(Views.Compras.RecepcionFacturasPage));
        ConstruirMenu();

        var u = Sesion.UsuarioActual;
        NombreUsuario.Text = u?.NombreCompleto ?? "Invitado";
        RolUsuario.Text = u?.NombreRol ?? "";
        InicialesUsuario.Text = Formato.Iniciales(u?.NombreCompleto ?? "");

        var tapSalir = new TapGestureRecognizer();
        tapSalir.Tapped += (_, _) => CerrarSesion();
        CerrarSesionLabel.GestureRecognizers.Add(tapSalir);

        Navigating += (sender, args) =>
        {
            var loc = args.Target?.Location?.ToString()?.Trim('/') ?? "";
            var rutaDestino = loc.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "";
            if (string.IsNullOrWhiteSpace(rutaDestino) || rutaDestino.Equals("DashboardPage", StringComparison.OrdinalIgnoreCase))
                return;

            var item = Secciones.SelectMany(s => s.Items)
                .FirstOrDefault(i => rutaDestino.Equals(i.Ruta, StringComparison.OrdinalIgnoreCase));
            if (item != null && !Sesion.PuedeAccederVista(item.Permiso, item.Seccion))
            {
                args.Cancel();
                _ = DisplayAlertAsync("Acceso Denegado (RBAC)",
                    $"Tu perfil actual ({Sesion.UsuarioActual?.NombreRol ?? "Invitado"}) no posee el poder/permiso '{item.Permiso}' para acceder a '{item.Titulo}'.",
                    "Entendido");
            }
        };

        Navigated += (_, e) => Resaltar(e.Current?.Location?.ToString() ?? "");
        Resaltar("DashboardPage");
    }

    public static void RecargarMenuActual()
    {
        InstanciaActual?.ConstruirMenu();
    }

    private void ConstruirMenu()
    {
        Menu.Clear();
        _items.Clear();

        foreach (var seccion in Secciones)
        {
            // Despliegue dinámico y Control de Accesos RBAC por sección y por vista individual (RF-POR-01 / RF-POR-03)
            var itemsPermitidos = seccion.Items
                .Where(item => Sesion.PuedeAccederVista(item.Permiso, item.Seccion))
                .ToList();

            if (itemsPermitidos.Count == 0)
                continue;

            Menu.Add(new Label
            {
                Text = seccion.Nombre,
                FontSize = 10,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#9CA3AF"),
                Margin = new Thickness(12, 10, 0, 2)
            });

            foreach (var item in itemsPermitidos)
            {
                var texto = new Label
                {
                    Text = string.IsNullOrEmpty(item.Icono) ? item.Titulo : $"{item.Icono}   {item.Titulo}",
                    FontSize = 12.5,
                    TextColor = Color.FromArgb("#374151")
                };

                var borde = new Border
                {
                    StrokeThickness = 0,
                    StrokeShape = new RoundRectangle { CornerRadius = 8 },
                    Padding = new Thickness(12, 6),
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
        if (!Sesion.PuedeAccederVista(item.Permiso, item.Seccion))
        {
            await DisplayAlertAsync("Acceso Restringido (RBAC)",
                $"Tu perfil actual ({Sesion.UsuarioActual?.NombreRol ?? "Invitado"}) no posee el permiso '{item.Permiso}' para acceder a '{item.Titulo}'.",
                "Entendido");
            return;
        }

        if (Implementadas.Contains(item.Ruta))
        {
            await GoToAsync($"//{item.Ruta}");
        }
        else
        {
            await DisplayAlertAsync("Módulo en desarrollo",
                $"\"{item.Titulo}\" aún no está implementado.", "OK");
        }
    }

    private void Resaltar(string ubicacion)
    {
        var rutaActual = ubicacion.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? ubicacion;
        foreach (var (borde, texto, ruta) in _items)
        {
            var activo = rutaActual.Equals(ruta, StringComparison.OrdinalIgnoreCase);
            borde.BackgroundColor = activo ? Color.FromArgb("#E8F1FD") : Colors.Transparent;
            texto.TextColor = activo ? Color.FromArgb("#1D6FD8") : Color.FromArgb("#374151");
            texto.FontAttributes = activo ? FontAttributes.Bold : FontAttributes.None;
        }
    }

    private static void CerrarSesion()
    {
        Sesion.CerrarSesion();
        var login = IPlatformApplication.Current!.Services.GetRequiredService<LoginPage>();
        var ventana = Application.Current?.Windows.FirstOrDefault();
        if (ventana != null)
            ventana.Page = new NavigationPage(login);
    }
}
