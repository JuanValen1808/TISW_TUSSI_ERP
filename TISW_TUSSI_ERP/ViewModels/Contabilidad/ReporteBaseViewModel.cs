using System.Windows.Input;
using TISW_TUSSI_ERP.Helpers;

namespace TISW_TUSSI_ERP.ViewModels.Contabilidad;

// Base de todos los reportes contables: generación, manejo de errores y exportación PDF / CSV.
public abstract class ReporteBaseViewModel : BaseViewModel
{
    private bool _hayDatos;
    public bool HayDatos
    {
        get => _hayDatos;
        set { if (SetProperty(ref _hayDatos, value)) OnPropertyChanged(nameof(SinDatos)); }
    }
    public bool SinDatos => !HayDatos && !EstaOcupado;

    private string _mensajeError = string.Empty;
    public string MensajeError
    {
        get => _mensajeError;
        set { if (SetProperty(ref _mensajeError, value)) OnPropertyChanged(nameof(HayError)); }
    }
    public bool HayError => !string.IsNullOrEmpty(MensajeError);

    private string _mensajeExito = string.Empty;
    public string MensajeExito
    {
        get => _mensajeExito;
        set { if (SetProperty(ref _mensajeExito, value)) OnPropertyChanged(nameof(HayExito)); }
    }
    public bool HayExito => !string.IsNullOrEmpty(MensajeExito);

    private string _generadoTexto = "Sin generar";
    public string GeneradoTexto { get => _generadoTexto; set => SetProperty(ref _generadoTexto, value); }

    public ICommand GenerarCommand { get; }
    public ICommand ExportarPdfCommand { get; }
    public ICommand ExportarCsvCommand { get; }

    protected ReporteBaseViewModel()
    {
        GenerarCommand = new Command(async () => await GenerarAsync());
        ExportarPdfCommand = new Command(async () => await ExportarAsync(pdf: true));
        ExportarCsvCommand = new Command(async () => await ExportarAsync(pdf: false));
        PropertyChanged += (_, e) => { if (e.PropertyName == nameof(EstaOcupado)) OnPropertyChanged(nameof(SinDatos)); };
    }

    /// <summary>Consulta la BD y llena las colecciones del reporte.</summary>
    protected abstract Task CargarDatosAsync();

    /// <summary>Arma el documento genérico que se exporta a PDF / CSV.</summary>
    protected abstract DocumentoReporte ConstruirDocumento();

    public async Task GenerarAsync()
    {
        if (EstaOcupado) return;
        try
        {
            EstaOcupado = true;
            MensajeError = string.Empty;
            MensajeExito = string.Empty;
            await CargarDatosAsync();
            GeneradoTexto = $"Actualizado {DateTime.Now:dd/MM/yyyy HH:mm}";
        }
        catch (Exception ex)
        {
            HayDatos = false;
            MensajeError = $"No se pudo generar el reporte. Verifica la conexión con la base de datos. Detalle: {ex.Message}";
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private async Task ExportarAsync(bool pdf)
    {
        if (EstaOcupado) return;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;

        if (!HayDatos)
        {
            MensajeError = "No hay datos para exportar. Genera el reporte primero o ajusta los filtros.";
            return;
        }

        try
        {
            var doc = ConstruirDocumento();
            var ruta = pdf ? await ExportadorReportes.ExportarPdfAsync(doc) : await ExportadorReportes.ExportarCsvAsync(doc);
            MensajeExito = pdf
                ? $"Reporte abierto en el navegador para imprimir / guardar como PDF. Archivo: {ruta}"
                : $"Archivo CSV generado (se abre con Excel). Archivo: {ruta}";
        }
        catch (Exception ex)
        {
            MensajeError = $"No se pudo exportar el reporte: {ex.Message}";
        }
    }

    protected static string Rango(DateTime desde, DateTime hasta) => $"{desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy}";
}
