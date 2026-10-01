using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using TISW_TUSSI_ERP.Models.Contabilidad;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Contabilidad;

public class CuentasPorPagarViewModel : BaseViewModel
{
    private readonly ContabilidadService _contabilidadService;

    public ObservableCollection<CuentaPorPagar> Documentos { get; } = new();

    private decimal _totalPagar;
    public decimal TotalPagar
    {
        get => _totalPagar;
        set => SetProperty(ref _totalPagar, value);
    }

    public ICommand CargarDocumentosCommand { get; }

    public CuentasPorPagarViewModel(ContabilidadService contabilidadService)
    {
        _contabilidadService = contabilidadService;
        Titulo = "Cuentas por Pagar";
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

            // Simulación de datos (a falta de la API real de CxP)
            await Task.Delay(500);

            var dummyData = new List<CuentaPorPagar>
            {
                new() { IdDocumento = 1, NumeroDocumento = "FAC-5501", Proveedor = "Proveedor X", FechaEmision = DateTime.Now.AddDays(-10), FechaVencimiento = DateTime.Now.AddDays(20), MontoTotal = 800000, MontoPagado = 0 },
                new() { IdDocumento = 2, NumeroDocumento = "FAC-5502", Proveedor = "Proveedor Y", FechaEmision = DateTime.Now.AddDays(-40), FechaVencimiento = DateTime.Now.AddDays(-10), MontoTotal = 300000, MontoPagado = 150000 },
                new() { IdDocumento = 3, NumeroDocumento = "FAC-5503", Proveedor = "Proveedor Z", FechaEmision = DateTime.Now.AddDays(-2), FechaVencimiento = DateTime.Now.AddDays(28), MontoTotal = 1200000, MontoPagado = 0 }
            };

            decimal total = 0;
            foreach (var doc in dummyData)
            {
                Documentos.Add(doc);
                total += doc.SaldoPendiente;
            }

            TotalPagar = total;
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
