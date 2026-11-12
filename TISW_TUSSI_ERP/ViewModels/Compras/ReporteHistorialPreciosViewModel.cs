using System.Collections.ObjectModel;
using System.Windows.Input;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Compras;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Compras;

// Reporte: Historial de Precios y Variación de Costos de Adquisición
public class ReporteHistorialPreciosViewModel : BaseViewModel
{
    private readonly ComprasService _servicio;

    public ObservableCollection<ItemHistorialPrecio> Items { get; } = new();
    public ObservableCollection<ProductoSelector> Productos { get; } = new();

    private ResumenHistorialPrecios _resumen = new();
    public ResumenHistorialPrecios Resumen { get => _resumen; private set => SetProperty(ref _resumen, value); }

    private DateTime _fechaDesde = DateTime.Today.AddDays(-180);
    public DateTime FechaDesde { get => _fechaDesde; set => SetProperty(ref _fechaDesde, value); }

    private DateTime _fechaHasta = DateTime.Today;
    public DateTime FechaHasta { get => _fechaHasta; set => SetProperty(ref _fechaHasta, value); }

    private ProductoSelector? _productoFiltro;
    public ProductoSelector? ProductoFiltro { get => _productoFiltro; set => SetProperty(ref _productoFiltro, value); }

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
    public ICommand ComandoLimpiarFiltros { get; }
    public ICommand ComandoExportarCsv { get; }

    public ReporteHistorialPreciosViewModel(ComprasService servicio)
    {
        _servicio = servicio;
        Titulo = "Historial de Precios de Adquisición";

        ComandoGenerar = new Command(async () => await GenerarReporteAsync(), () => NoEstaOcupado);
        ComandoLimpiarFiltros = new Command(async () =>
        {
            if (EstaOcupado) return;
            FechaDesde = DateTime.Today.AddDays(-180);
            FechaHasta = DateTime.Today;
            ProductoFiltro = null;
            await GenerarReporteAsync();
        }, () => NoEstaOcupado);
        ComandoExportarCsv = new Command(async () => await ExportarCsvAsync(), () => NoEstaOcupado);

        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(EstaOcupado))
            {
                OnPropertyChanged(nameof(NoEstaOcupado));
                (ComandoGenerar as Command)?.ChangeCanExecute();
                (ComandoLimpiarFiltros as Command)?.ChangeCanExecute();
                (ComandoExportarCsv as Command)?.ChangeCanExecute();
            }
        };
    }

    public async Task CargarInicialAsync()
    {
        if (EstaOcupado) return;
        EstaOcupado = true;
        MensajeError = string.Empty;

        try
        {
            int? idProdActual = ProductoFiltro?.Id;
            var prods = await _servicio.ObtenerProductosParaCompraAsync();
            Productos.Clear();
            foreach (var p in prods) Productos.Add(p);

            ProductoFiltro = idProdActual.HasValue
                ? Productos.FirstOrDefault(p => p.Id == idProdActual.Value)
                : null;

            await GenerarInternoAsync();
        }
        catch (Exception ex)
        {
            MensajeError = "Error al iniciar: " + ex.Message;
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    public async Task GenerarReporteAsync()
    {
        if (EstaOcupado) return;

        if (FechaDesde > FechaHasta)
        {
            MensajeError = "La fecha 'Desde' no puede ser posterior a la fecha 'Hasta'.";
            return;
        }

        EstaOcupado = true;
        MensajeError = string.Empty;

        try
        {
            await GenerarInternoAsync();
        }
        catch (Exception ex)
        {
            MensajeError = "No se pudo generar el historial de precios: " + ex.Message;
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private async Task GenerarInternoAsync()
    {
        if (FechaDesde > FechaHasta)
        {
            MensajeError = "La fecha 'Desde' no puede ser posterior a la fecha 'Hasta'.";
            return;
        }

        MensajeError = string.Empty;
        int? idProd = ProductoFiltro?.Id;
        var (resumen, lista) = await _servicio.ObtenerReporteHistorialPreciosAsync(idProd, FechaDesde, FechaHasta);

        Resumen = resumen;
        Items.Clear();
        foreach (var item in lista)
        {
            Items.Add(item);
        }

        SinResultados = Items.Count == 0;
    }

    private async Task ExportarCsvAsync()
    {
        if (EstaOcupado) return;
        EstaOcupado = true;

        try
        {
            if (Items.Count == 0)
            {
                await Shell.Current.DisplayAlert("Aviso", "No hay datos para exportar en el historial actual.", "OK");
                return;
            }

            var headers = new[] { "Fecha", "Documento", "SKU", "Medicamento", "Proveedor", "Cantidad", "Costo Anterior", "Costo Unitario", "Variación ($)", "Variación (%)", "Tendencia" };
            var rows = Items.Select(i => new[]
            {
                i.FechaTexto,
                i.Documento,
                i.Sku,
                i.NombreProducto,
                i.Proveedor,
                i.Cantidad.ToString(),
                i.CostoAnterior.HasValue ? i.CostoAnterior.Value.ToString("0", Formato.Cultura) : "—",
                i.CostoUnitario.ToString("0", Formato.Cultura),
                i.VariacionMonto.ToString("0", Formato.Cultura),
                $"{i.VariacionPorcentaje:0.0}%",
                i.IndicadorTendencia
            });

            await CsvExportHelper.ExportarYCompartirAsync("Reporte_Historial_Precios", headers, rows);
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

