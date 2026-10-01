using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using TISW_TUSSI_ERP.Models.Contabilidad;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Contabilidad;

public class LibroDiarioViewModel : BaseViewModel
{
    private readonly ContabilidadService _contabilidadService;

    public ObservableCollection<LibroDiarioAsiento> Asientos { get; } = new();

    public ICommand CargarAsientosCommand { get; }

    public LibroDiarioViewModel(ContabilidadService contabilidadService)
    {
        _contabilidadService = contabilidadService;
        Titulo = "Libro Diario";
        CargarAsientosCommand = new Command(async () => await CargarAsientosAsync());
    }

    public async Task CargarAsientosAsync()
    {
        if (EstaOcupado)
            return;

        try
        {
            EstaOcupado = true;
            Asientos.Clear();

            var asientosDb = await _contabilidadService.ObtenerLibroDiarioAsync();
            
            foreach (var asiento in asientosDb)
            {
                Asientos.Add(asiento);
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"No se pudo cargar el libro diario: {ex.Message}", "OK");
        }
        finally
        {
            EstaOcupado = false;
        }
    }
}
