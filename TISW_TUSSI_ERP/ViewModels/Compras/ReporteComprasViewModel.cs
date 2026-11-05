using System.Collections.ObjectModel;
using System.Windows.Input;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Compras;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Compras;

// Reporte: Cumplimiento de Órdenes de Compra por Proveedor (RF-COM-04 / RF-COM-05)
public class ReporteComprasViewModel : BaseViewModel
{
    private readonly ComprasService _servicio;

    // Lista observable para llenar la tabla/grilla de resultados
    public ObservableCollection<CompraPorProveedor> Filas { get; } = new();

    // Objeto que contiene las 4 métricas superiores del Dashboard
    private ResumenReporteCompras _resumen = new();
    public ResumenReporteCompras Resumen { get => _resumen; private set => SetProperty(ref _resumen, value); }

    // Filtros de búsqueda con notificación de cambios para la UI
    private DateTime _fechaDesde = DateTime.Today.AddDays(-30);
    public DateTime FechaDesde { get => _fechaDesde; set => SetProperty(ref _fechaDesde, value); }

    private DateTime _fechaHasta = DateTime.Today;
    public DateTime FechaHasta { get => _fechaHasta; set => SetProperty(ref _fechaHasta, value); }

    private string _rutFiltro = string.Empty;
    public string RutFiltro { get => _rutFiltro; set => SetProperty(ref _rutFiltro, value); }

    // Manejo de estado y errores
    private string _mensajeError = string.Empty;
    public string MensajeError
    {
        get => _mensajeError;
        private set { if (SetProperty(ref _mensajeError, value)) OnPropertyChanged(nameof(HayError)); }
    }
    public bool HayError => !string.IsNullOrEmpty(MensajeError);

    private bool _sinResultados;
    public bool SinResultados { get => _sinResultados; private set => SetProperty(ref _sinResultados, value); }

    // Comandos
    public ICommand ComandoGenerar { get; }
    public ICommand ComandoLimpiarFiltro { get; }

    public ReporteComprasViewModel(ComprasService servicio)
    {
        _servicio = servicio;
        Titulo = "Reporte de Compras";

        ComandoGenerar = new Command(async () => await GenerarAsync());

        ComandoLimpiarFiltro = new Command(async () =>
        {
            RutFiltro = string.Empty;
            await GenerarAsync();
        });
    }

    public async Task GenerarAsync()
    {
        if (EstaOcupado) return;

        // Validación de operación (Requisito para los 40 puntos de la evaluación)
        if (FechaDesde > FechaHasta)
        {
            MensajeError = "La fecha 'Desde' no puede ser posterior a la fecha 'Hasta'.";
            return;
        }

        EstaOcupado = true;
        MensajeError = string.Empty;

        try
        {
            // Llama al método de tu servicio que extrae las métricas y la lista en una sola consulta
            var (resumen, filas) = await _servicio.ObtenerReporteComprasAsync(FechaDesde, FechaHasta, RutFiltro);

            // Actualiza los cuadros superiores
            Resumen = resumen;

            // Actualiza la tabla inferior
            Filas.Clear();
            foreach (var f in filas)
            {
                Filas.Add(f);
            }

            SinResultados = Filas.Count == 0;
        }
        catch (Exception ex)
        {
            // Combina el mensaje visual de tu código con la alerta nativa del mío
            MensajeError = "Ocurrió un problema de conexión al generar el reporte.";
            await Application.Current!.MainPage!.DisplayAlert("Error de Base de Datos", ex.Message, "OK");
        }
        finally
        {
            EstaOcupado = false;
        }
    }
}