using System.Windows.Input;
using TISW_TUSSI_ERP.Models.Auth;
using TISW_TUSSI_ERP.Services.Navigation;

namespace TISW_TUSSI_ERP.ViewModels.Dashboard;

public class DashboardViewModel : BaseViewModel
{
    private readonly NavigationService _navigationService;

    public string NombreUsuarioActual => Sesion.UsuarioActual?.NombreCompleto ?? "Invitado";

    public ICommand ComandoIrAVentas { get; }
    public ICommand ComandoIrACompras { get; }
    public ICommand ComandoIrAInventario { get; }
    public ICommand ComandoIrAContabilidad { get; }
    public ICommand ComandoCerrarSesion { get; }

    public DashboardViewModel(NavigationService navigationService)
    {
        _navigationService = navigationService;
        Titulo = "Dashboard";

        ComandoIrAVentas = new Command(async () => await _navigationService.IrA("PosTerminalPage"));
        ComandoIrACompras = new Command(async () => await _navigationService.IrA("ComprasPage"));
        ComandoIrAInventario = new Command(async () => await _navigationService.IrA("KardexPage"));
        ComandoIrAContabilidad = new Command(async () => await _navigationService.IrA("ContabilidadPage"));
        ComandoCerrarSesion = new Command(CerrarSesion);
    }

    private void CerrarSesion()
    {
        Sesion.CerrarSesion();
        Application.Current!.MainPage = new NavigationPage(new Views.Auth.LoginPage());
    }
}
