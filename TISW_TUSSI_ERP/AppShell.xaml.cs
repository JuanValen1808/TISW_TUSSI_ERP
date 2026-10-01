using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Shapes;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Auth;
using TISW_TUSSI_ERP.Views.Auth;

namespace TISW_TUSSI_ERP;

public partial class AppShell : Shell
{
    private record ItemMenu(string Icono, string Titulo, string Ruta);
    private record SeccionMenu(string Nombre, ItemMenu[] Items);

    // Rutas que ya tienen pantalla. Al crear una nueva pantalla:
    //  1) agrégala como <ShellContent Route="..."> en AppShell.xaml
    //  2) añade su ruta aquí
    //  3) regístrala en MauiProgram.cs
    private static readonly HashSet<string> Implementadas = new() { "DashboardPage", "OrdenesCompraPage", "PlanCuentasPage", "LibroDiarioPage", "LibroMayorPage", "BalanceOchoColumnasPage", "EstadoResultadosPage", "MayorCentralizadoPage" }; 

    private static readonly SeccionMenu[] Secciones =
    {
        new("PRINCIPAL", new[] { new ItemMenu("📊", "Dashboard", "DashboardPage") }),
        new("OPERACIÓN", new[]
        {
            new ItemMenu("🛒", "Punto de Venta", "PosPage"),
            new ItemMenu("💵", "Control de Caja", "CajaPage"),
            new ItemMenu("👥", "Clientes", "ClientesPage"),
        }),
        new("INVENTARIOS", new[]
        {
            new ItemMenu("📦", "Catálogo SKUs", "CatalogoPage"),
            new ItemMenu("🗂", "Kardex y Lotes", "KardexPage"),
            new ItemMenu("⚠", "Mermas", "MermasPage"),
        }),
        new("COMPRAS", new[]
        {
            new ItemMenu("🧾", "Órdenes de Compra", "OrdenesCompraPage"),
            new ItemMenu("📥", "Recepción Facturas", "RecepcionPage"),
        }),
        new("CONTABILIDAD", new[]
        {
            new ItemMenu("📒", "Libro Diario", "LibroDiarioPage"),
            new ItemMenu("🧮", "Plan de Cuentas", "PlanCuentasPage"),
            new ItemMenu("⚖️", "Libro Mayor", "LibroMayorPage"),
            new ItemMenu("🔍", "Mayor Centralizado", "MayorCentralizadoPage"),
            new ItemMenu("📊", "Balance 8 Columnas", "BalanceOchoColumnasPage"),
            new ItemMenu("📈", "Estado de Resultados", "EstadoResultadosPage"),
        }),
        new("ADMINISTRACIÓN", new[]
        {
            new ItemMenu("🔐", "Usuarios y Perfiles", "UsuariosPage"),
            new ItemMenu("⚙", "Configuración", "ConfigPage"),
        }),
    };

    private readonly List<(Border Borde, Label Texto, string Ruta)> _items = new();

    public AppShell()
    {
        InitializeComponent();
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
        var rol = Sesion.UsuarioActual?.NombreRol ?? "";

        foreach (var seccion in Secciones)
        {
            // Control de Accesos (RBAC)
            if (rol == "Contador" && seccion.Nombre != "PRINCIPAL" && seccion.Nombre != "CONTABILIDAD")
                continue;
                
            if (rol == "Vendedor" && seccion.Nombre != "PRINCIPAL" && seccion.Nombre != "OPERACIÓN" && seccion.Nombre != "INVENTARIOS")
                continue;
                
            if (rol == "Bodega" && seccion.Nombre != "PRINCIPAL" && seccion.Nombre != "INVENTARIOS" && seccion.Nombre != "COMPRAS")
                continue;
                
            if (rol == "Comprador" && seccion.Nombre != "PRINCIPAL" && seccion.Nombre != "COMPRAS")
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
                    Text = $"{item.Icono}   {item.Titulo}",
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
        if (Implementadas.Contains(item.Ruta))
            await GoToAsync($"//{item.Ruta}");
        else
            await DisplayAlert("Módulo en desarrollo", $"\"{item.Titulo}\" aún no está implementado.", "OK");
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
        var login = IPlatformApplication.Current!.Services.GetRequiredService<LoginPage>();
        Application.Current!.MainPage = new NavigationPage(login);
    }
}
