using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using TISW_TUSSI_ERP.Models.Contabilidad;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Contabilidad;

public class PlanCuentasViewModel : BaseViewModel
{
    private readonly ContabilidadService _contabilidadService;

    public ObservableCollection<PlanCuentas> Cuentas { get; } = new();

    public ICommand CargarCuentasCommand { get; }

    public PlanCuentasViewModel(ContabilidadService contabilidadService)
    {
        _contabilidadService = contabilidadService;
        Titulo = "Plan de Cuentas";
        CargarCuentasCommand = new Command(async () => await CargarCuentasAsync());
    }

    public async Task CargarCuentasAsync()
    {
        if (EstaOcupado)
            return;

        try
        {
            EstaOcupado = true;
            Cuentas.Clear();

            var cuentasDb = await _contabilidadService.ObtenerPlanCuentasAsync();
            
            foreach (var cuenta in cuentasDb)
            {
                Cuentas.Add(cuenta);
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"No se pudo cargar el plan de cuentas: {ex.Message}", "OK");
        }
        finally
        {
            EstaOcupado = false;
        }
    }
}
