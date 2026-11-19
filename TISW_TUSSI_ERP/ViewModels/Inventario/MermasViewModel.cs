using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using TISW_TUSSI_ERP.Models.Inventario;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Inventario;

public class MermasViewModel : BaseViewModel
{
    private readonly InventarioService _servicio;
    private readonly ReporteService _reporteService;

    public ObservableCollection<MermaModel> Mermas { get; } = new();

    public ICommand ComandoRecargar { get; }
    public ICommand ComandoExportarPdf { get; }

    public MermasViewModel(InventarioService servicio, ReporteService reporteService)
    {
        _servicio = servicio;
        _reporteService = reporteService;
        Titulo = "Control de Mermas y Bajas";

        ComandoRecargar = new Command(async () => await CargarMermasAsync());
        ComandoExportarPdf = new Command(async () => await ExportarMermasPdfAsync());
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
            await TISW_TUSSI_ERP.Helpers.ExportadorReportes.GuardarYAbrirPdfBytesAsync("Informe_Mermas", pdfBytes);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", "No se pudo generar o guardar el PDF de mermas: " + ex.Message, "OK");
        }
    }
}