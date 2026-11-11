using System.Collections.ObjectModel;
using System.Windows.Input;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Compras;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Compras;

// Reporte: Cumplimiento de Órdenes de Compra y Nivel de Servicio de Proveedores
public class ReporteCumplimientoViewModel : BaseViewModel
{
    private readonly ComprasService _servicio;

    public ObservableCollection<ItemCumplimientoOrden> Items { get; } = new();
    public ObservableCollection<Proveedor> Proveedores { get; } = new();

    private ResumenCumplimientoOrdenes _resumen = new();
    public ResumenCumplimientoOrdenes Resumen { get => _resumen; private set => SetProperty(ref _resumen, value); }

    private DateTime _fechaDesde = DateTime.Today.AddDays(-60);
    public DateTime FechaDesde { get => _fechaDesde; set => SetProperty(ref _fechaDesde, value); }

    private DateTime _fechaHasta = DateTime.Today;
    public DateTime FechaHasta { get => _fechaHasta; set => SetProperty(ref _fechaHasta, value); }

    private string _estadoFiltro = "TODAS";
    public string EstadoFiltro { get => _estadoFiltro; set => SetProperty(ref _estadoFiltro, value); }

    private Proveedor? _proveedorFiltro;
    public Proveedor? ProveedorFiltro { get => _proveedorFiltro; set => SetProperty(ref _proveedorFiltro, value); }

    private string _mensajeError = string.Empty;
    public string MensajeError
    {
        get => _mensajeError;
        private set { if (SetProperty(ref _mensajeError, value)) OnPropertyChanged(nameof(HayError)); }
    }
    public bool HayError => !string.IsNullOrEmpty(MensajeError);

    private bool _sinResultados;
    public bool SinResultados { get => _sinResultados; private set => SetProperty(ref _sinResultados, value); }

    public ICommand ComandoGenerar { get; }
    public ICommand ComandoLimpiarFiltros { get; }
    public ICommand ComandoExportarCsv { get; }

    public ReporteCumplimientoViewModel(ComprasService servicio)
    {
        _servicio = servicio;
        Titulo = "Reporte de Cumplimiento y SLA";

        ComandoGenerar = new Command(async () => await GenerarReporteAsync());
        ComandoLimpiarFiltros = new Command(async () =>
        {
            FechaDesde = DateTime.Today.AddDays(-60);
            FechaHasta = DateTime.Today;
            EstadoFiltro = "TODAS";
            ProveedorFiltro = null;
            await GenerarReporteAsync();
        });
        ComandoExportarCsv = new Command(async () => await ExportarCsvAsync());
    }

    public async Task CargarInicialAsync()
    {
        try
        {
            var provs = await _servicio.ObtenerProveedoresAsync();
            Proveedores.Clear();
            foreach (var p in provs) Proveedores.Add(p);

            await GenerarReporteAsync();
        }
        catch (Exception ex)
        {
            MensajeError = "Error al iniciar: " + ex.Message;
        }
    }

    public async Task GenerarReporteAsync()
    {
        if (EstaOcupado) return;

        // Validación de fechas
        if (FechaDesde > FechaHasta)
        {
            MensajeError = "La fecha 'Desde' no puede ser posterior a la fecha 'Hasta'.";
            return;
        }

        EstaOcupado = true;
        MensajeError = string.Empty;

        try
        {
            int? idProv = ProveedorFiltro?.Id;
            var (resumen, lista) = await _servicio.ObtenerReporteCumplimientoAsync(FechaDesde, FechaHasta, EstadoFiltro, idProv);

            Resumen = resumen;
            Items.Clear();
            foreach (var item in lista)
            {
                Items.Add(item);
            }

            SinResultados = Items.Count == 0;
        }
        catch (Exception ex)
        {
            MensajeError = "No se pudo generar el reporte de cumplimiento: " + ex.Message;
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private async Task ExportarCsvAsync()
    {
        if (Items.Count == 0)
        {
            await Shell.Current.DisplayAlert("Aviso", "No hay datos para exportar en el reporte actual.", "OK");
            return;
        }

        try
        {
            var headers = new[] { "Número OC", "Fecha Emisión", "Proveedor", "RUT", "Estado", "Unidades Solicitadas", "Unidades Recibidas", "Discrepancia", "Tasa Cumplimiento", "Nivel de Servicio (SLA)", "Monto Total" };
            var rows = Items.Select(i => new[]
            {
                i.NumeroOc,
                i.FechaEmisionTexto,
                i.Proveedor,
                i.RutProveedor,
                i.Estado,
                i.TotalSolicitado.ToString(),
                i.TotalRecibido.ToString(),
                i.Discrepancia.ToString(),
                i.PorcentajeTexto,
                i.NivelServicio,
                i.MontoTotal.ToString("0", Formato.Cultura)
            });

            await CsvExportHelper.ExportarYCompartirAsync("Reporte_Cumplimiento_Ordenes_Compra", headers, rows);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", "No se pudo exportar a CSV: " + ex.Message, "OK");
        }
    }
}

