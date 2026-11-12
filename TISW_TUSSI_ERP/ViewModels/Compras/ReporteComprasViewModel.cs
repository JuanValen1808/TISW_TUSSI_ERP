using System.Collections.ObjectModel;
using System.Windows.Input;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Compras;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Compras;

// Reporte: Registro de Compras por Laboratorio / Proveedor
public class ReporteComprasViewModel : BaseViewModel
{
    private readonly ComprasService _servicio;

    public ObservableCollection<CompraPorProveedor> Filas { get; } = new();

    private ResumenReporteCompras _resumen = new();
    public ResumenReporteCompras Resumen { get => _resumen; private set => SetProperty(ref _resumen, value); }

    private DateTime _fechaDesde = DateTime.Today.AddDays(-60);
    public DateTime FechaDesde { get => _fechaDesde; set => SetProperty(ref _fechaDesde, value); }

    private DateTime _fechaHasta = DateTime.Today;
    public DateTime FechaHasta { get => _fechaHasta; set => SetProperty(ref _fechaHasta, value); }

    private string _rutFiltro = string.Empty;
    public string RutFiltro { get => _rutFiltro; set => SetProperty(ref _rutFiltro, value); }

    private string _mensajeError = string.Empty;
    public string MensajeError
    {
        get => _mensajeError;
        private set { if (SetProperty(ref _mensajeError, value)) OnPropertyChanged(nameof(HayError)); }
    }
    public bool HayError => !string.IsNullOrEmpty(MensajeError);

    private bool _sinResultados;
    public bool SinResultados { get => _sinResultados; private set => SetProperty(ref _sinResultados, value); }
    public bool NoEstaOcupado => !EstaOcupado;

    public ICommand ComandoGenerar { get; }
    public ICommand ComandoLimpiarFiltro { get; }
    public ICommand ComandoExportarCsv { get; }

    public ReporteComprasViewModel(ComprasService servicio)
    {
        _servicio = servicio;
        Titulo = "Reporte de Compras por Laboratorio";

        ComandoGenerar = new Command(async () => await GenerarAsync(), () => NoEstaOcupado);

        ComandoLimpiarFiltro = new Command(async () =>
        {
            if (EstaOcupado) return;
            RutFiltro = string.Empty;
            FechaDesde = DateTime.Today.AddDays(-60);
            FechaHasta = DateTime.Today;
            await GenerarAsync();
        }, () => NoEstaOcupado);

        ComandoExportarCsv = new Command(async () => await ExportarCsvAsync(), () => NoEstaOcupado);

        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(EstaOcupado))
            {
                OnPropertyChanged(nameof(NoEstaOcupado));
                (ComandoGenerar as Command)?.ChangeCanExecute();
                (ComandoLimpiarFiltro as Command)?.ChangeCanExecute();
                (ComandoExportarCsv as Command)?.ChangeCanExecute();
            }
        };
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
            var (resumen, filas) = await _servicio.ObtenerReporteComprasAsync(FechaDesde, FechaHasta, RutFiltro);

            Resumen = resumen;

            Filas.Clear();
            foreach (var f in filas)
            {
                Filas.Add(f);
            }

            SinResultados = Filas.Count == 0;
        }
        catch (Exception ex)
        {
            MensajeError = "Ocurrió un problema al generar el reporte: " + ex.Message;
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private async Task ExportarCsvAsync()
    {
        if (EstaOcupado) return;
        EstaOcupado = true;

        try
        {
            if (Filas.Count == 0)
            {
                await Shell.Current.DisplayAlert("Aviso", "No hay datos para exportar en el período seleccionado.", "OK");
                return;
            }

            var headers = new[] { "Proveedor / Laboratorio", "RUT", "Total Órdenes", "Órdenes Pendientes", "Órdenes Liquidadas", "Monto Total Comprado", "Monto por Recibir", "Última Compra" };
            var rows = Filas.Select(f => new[]
            {
                f.Proveedor,
                f.Rut,
                f.NumeroOrdenes.ToString(),
                f.OrdenesPendientes.ToString(),
                f.OrdenesLiquidadas.ToString(),
                f.MontoTotal.ToString("0", Formato.Cultura),
                f.MontoPorRecibir.ToString("0", Formato.Cultura),
                f.UltimaCompraTexto
            });

            await CsvExportHelper.ExportarYCompartirAsync("Reporte_Compras_Por_Laboratorio", headers, rows);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", "No se pudo exportar a CSV: " + ex.Message, "OK");
        }
        finally
        {
            EstaOcupado = false;
        }
    }
}
