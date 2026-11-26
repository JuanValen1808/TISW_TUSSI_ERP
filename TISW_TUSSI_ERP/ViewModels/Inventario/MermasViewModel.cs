using CommunityToolkit.Maui.Storage; // <-- Añadir este using
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using TISW_TUSSI_ERP.Models.Inventario;
using TISW_TUSSI_ERP.Services.Api;
using TISW_TUSSI_ERP.Views.Inventario;

namespace TISW_TUSSI_ERP.ViewModels.Inventario;

public class MermasViewModel : BaseViewModel
{
    private readonly InventarioService _servicio;
    private readonly ReporteService _reporteService;

    public ObservableCollection<MermaModel> Mermas { get; } = new();

    public ICommand ComandoRecargar { get; }
    public ICommand ComandoExportarPdf { get; }
    public ICommand ComandoAbrirRegistrarMerma { get; }

    public MermasViewModel(InventarioService servicio, ReporteService reporteService)
    {
        _servicio = servicio;
        _reporteService = reporteService;
        Titulo = "Control de Mermas y Bajas";

        ComandoRecargar = new Command(async () => await CargarMermasAsync());
        ComandoExportarPdf = new Command(async () => await ExportarMermasPdfAsync());
        ComandoAbrirRegistrarMerma = new Command(async () => await AbrirRegistrarMermaModalAsync());
    }

    private async Task AbrirRegistrarMermaModalAsync()
    {
        var modal = new RegistrarMermaModalPage(_servicio, async () =>
        {
            await CargarMermasAsync();
        });

        await Shell.Current.Navigation.PushModalAsync(modal);
    }

    public async Task CargarMermasAsync()
    {
        if (EstaOcupado) return;
        EstaOcupado = true;

        try
        {
            var lista = await _servicio.ObtenerMermasAsync();
            Mermas.Clear();
            foreach (var m in lista) Mermas.Add(m);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", "No se pudo cargar el registro de mermas: " + ex.Message, "OK");
        }
        finally
        {
            EstaOcupado = false;
        }
    }



    private async Task ExportarMermasPdfAsync()
    {
        if (!Mermas.Any())
        {
            await Shell.Current.DisplayAlert("Exportar", "No hay registros de mermas para exportar.", "OK");
            return;
        }

        try
        {
            // 1. Generar bytes del reporte mediante QuestPDF
            byte[] pdfBytes = _reporteService.GenerarReporteMermasPdf(Mermas.ToList());

            string nombreArchivo = $"Informe_Mermas_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

            // 2. Usar FileSaver nativo para que el usuario elija dónde guardar el PDF
            using var stream = new MemoryStream(pdfBytes);
            var fileSaveResult = await FileSaver.Default.SaveAsync(nombreArchivo, stream, CancellationToken.None);

            if (fileSaveResult.IsSuccessful && !string.IsNullOrEmpty(fileSaveResult.FilePath))
            {
                bool abrir = await Shell.Current.DisplayAlert(
                    "Reporte Guardado",
                    $"El informe de mermas se ha guardado correctamente en:\n{fileSaveResult.FilePath}\n\n¿Desea abrirlo ahora?",
                    "Abrir Archivo",
                    "Cerrar");

                if (abrir)
                {
                    await Launcher.Default.OpenAsync(new OpenFileRequest
                    {
                        File = new ReadOnlyFile(fileSaveResult.FilePath)
                    });
                }
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", "No se pudo generar o guardar el PDF de mermas: " + ex.Message, "OK");
        }
    }
}