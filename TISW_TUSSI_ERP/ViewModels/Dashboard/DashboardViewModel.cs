using System.Collections.ObjectModel;
using System.Windows.Input;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Dashboard;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Dashboard;

public class DashboardViewModel : BaseViewModel
{
    private readonly DashboardService _servicio;

    // La página se suscribe para redibujar el gráfico de dona
    public event Action? DatosActualizados;

    private ResumenDashboard _resumen = new();
    public ResumenDashboard Resumen { get => _resumen; private set => SetProperty(ref _resumen, value); }

    public ObservableCollection<BarraVenta> Barras { get; } = new();
    public ObservableCollection<MedioPago> MediosPago { get; } = new();
    public ObservableCollection<ProductoVendido> TopProductos { get; } = new();
    public ObservableCollection<AlertaStock> Alertas { get; } = new();

    private string _fechaTexto = string.Empty;
    public string FechaTexto { get => _fechaTexto; private set => SetProperty(ref _fechaTexto, value); }

    private string _totalSemanaTexto = string.Empty;
    public string TotalSemanaTexto { get => _totalSemanaTexto; private set => SetProperty(ref _totalSemanaTexto, value); }

    private string _mensajeError = string.Empty;
    public string MensajeError
    {
        get => _mensajeError;
        private set { if (SetProperty(ref _mensajeError, value)) OnPropertyChanged(nameof(HayError)); }
    }
    public bool HayError => !string.IsNullOrEmpty(MensajeError);

    public ICommand ComandoActualizar { get; }

    public DashboardViewModel(DashboardService servicio)
    {
        _servicio = servicio;
        Titulo = "Dashboard";
        ComandoActualizar = new Command(async () => await CargarAsync());
    }

    public async Task CargarAsync()
    {
        if (EstaOcupado) return;
        EstaOcupado = true;
        MensajeError = string.Empty;

        try
        {
            var datos = await _servicio.ObtenerAsync();

            Resumen = datos.Resumen;
            FechaTexto = Formato.Capitalizar(datos.Hoy.ToString("dddd, d 'de' MMMM 'de' yyyy", Formato.Cultura));
            TotalSemanaTexto = "Acumulado " + Formato.Moneda(datos.Barras.Sum(b => b.Monto));

            Reemplazar(Barras, datos.Barras);
            Reemplazar(MediosPago, datos.MediosPago);
            Reemplazar(TopProductos, datos.TopProductos);
            Reemplazar(Alertas, datos.Alertas);

            DatosActualizados?.Invoke();
        }
        catch (Exception ex)
        {
            MensajeError = "No se pudieron cargar los datos: " + ex.Message;
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private static void Reemplazar<T>(ObservableCollection<T> destino, IEnumerable<T> origen)
    {
        destino.Clear();
        foreach (var item in origen) destino.Add(item);
    }
}
