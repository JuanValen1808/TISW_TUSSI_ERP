using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using TISW_TUSSI_ERP.Models.Contabilidad;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Contabilidad;

public class CuentasPorCobrarViewModel : BaseViewModel
{
    private readonly ContabilidadService _contabilidadService;

    public ObservableCollection<CuentaPorCobrar> Documentos { get; } = new();

    private decimal _totalCobrar;
    public decimal TotalCobrar
    {
        get => _totalCobrar;
        set => SetProperty(ref _totalCobrar, value);
    }

    public ICommand CargarDocumentosCommand { get; }

    public CuentasPorCobrarViewModel(ContabilidadService contabilidadService)
    {
        _contabilidadService = contabilidadService;
        Titulo = "Cuentas por Cobrar";
        CargarDocumentosCommand = new Command(async () => await CargarDocumentosAsync());
    }

    public async Task CargarDocumentosAsync()
    {
        if (EstaOcupado)
            return;

        try
        {
            EstaOcupado = true;
            Documentos.Clear();

            // Simulación de datos (a falta de la API real de CxC)
            await Task.Delay(500); 

            var dummyData = new List<CuentaPorCobrar>
            {
                new() { IdDocumento = 1, NumeroDocumento = "FAC-1001", Cliente = "Cliente A", FechaEmision = DateTime.Now.AddDays(-15), FechaVencimiento = DateTime.Now.AddDays(15), MontoTotal = 500000, MontoPagado = 0 },
                new() { IdDocumento = 2, NumeroDocumento = "FAC-1002", Cliente = "Cliente B", FechaEmision = DateTime.Now.AddDays(-35), FechaVencimiento = DateTime.Now.AddDays(-5), MontoTotal = 250000, MontoPagado = 100000 },
                new() { IdDocumento = 3, NumeroDocumento = "FAC-1003", Cliente = "Cliente C", FechaEmision = DateTime.Now.AddDays(-5), FechaVencimiento = DateTime.Now.AddDays(25), MontoTotal = 1500000, MontoPagado = 1500000 }
            };

            decimal total = 0;
            foreach (var doc in dummyData)
            {
                Documentos.Add(doc);
                total += doc.SaldoPendiente;
            }

            TotalCobrar = total;
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"No se pudo cargar la información: {ex.Message}", "OK");
        }
        finally
        {
            EstaOcupado = false;
        }
    }
}
