using System.Collections.ObjectModel;
using System.Windows.Input;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Ventas;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Ventas;

// Reporte 1 (RF-VEN-04): ranking de ventas por cliente y convenio (analisis Pareto)
public class RankingVentasViewModel : BaseViewModel
{
    private readonly VentasService _servicio;

    public ObservableCollection<FilaRanking> Filas { get; } = new();

    public string TotalGeneralTexto { get; private set; } = Formato.Moneda(0);
    public string CantidadClientesTexto => $"Clientes con ventas: {Filas.Count}";

    private string _mensajeError = string.Empty;
    public string MensajeError
    {
        get => _mensajeError;
        private set { if (SetProperty(ref _mensajeError, value)) OnPropertyChanged(nameof(HayError)); }
    }
    public bool HayError => !string.IsNullOrEmpty(MensajeError);

    public ICommand ComandoActualizar { get; }

    public RankingVentasViewModel(VentasService servicio)
    {
        _servicio = servicio;
        Titulo = "Ranking de Ventas";
        ComandoActualizar = new Command(async () => await CargarAsync());
    }

    public async Task CargarAsync()
    {
        if (EstaOcupado) return;
        EstaOcupado = true;
        MensajeError = string.Empty;
        try
        {
            var (filas, total) = await _servicio.ObtenerRankingAsync();
            Filas.Clear();
            foreach (var f in filas) Filas.Add(f);
            TotalGeneralTexto = Formato.Moneda(total);
            OnPropertyChanged(nameof(TotalGeneralTexto));
            OnPropertyChanged(nameof(CantidadClientesTexto));
        }
        catch (Exception ex)
        {
            MensajeError = "No se pudo generar el reporte: " + ex.Message;
        }
        finally { EstaOcupado = false; }
    }
}
