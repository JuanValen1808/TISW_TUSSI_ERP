using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using TISW_TUSSI_ERP.Models.Contabilidad;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Contabilidad;

public class LibroMayorViewModel : BaseViewModel
{
    private readonly ContabilidadService _contabilidadService;

    public ObservableCollection<LibroMayorItem> SaldosMayores { get; } = new();

    private decimal _sumaTotalDebe;
    public decimal SumaTotalDebe
    {
        get => _sumaTotalDebe;
        set => SetProperty(ref _sumaTotalDebe, value);
    }

    private decimal _sumaTotalHaber;
    public decimal SumaTotalHaber
    {
        get => _sumaTotalHaber;
        set => SetProperty(ref _sumaTotalHaber, value);
    }

    private decimal _sumaTotalSaldoDeudor;
    public decimal SumaTotalSaldoDeudor
    {
        get => _sumaTotalSaldoDeudor;
        set => SetProperty(ref _sumaTotalSaldoDeudor, value);
    }

    private decimal _sumaTotalSaldoAcreedor;
    public decimal SumaTotalSaldoAcreedor
    {
        get => _sumaTotalSaldoAcreedor;
        set => SetProperty(ref _sumaTotalSaldoAcreedor, value);
    }

    public ICommand CargarSaldosCommand { get; }

    public LibroMayorViewModel(ContabilidadService contabilidadService)
    {
        _contabilidadService = contabilidadService;
        Titulo = "Libro Mayor y Balances";
        CargarSaldosCommand = new Command(async () => await CargarSaldosAsync());
    }

    public async Task CargarSaldosAsync()
    {
        if (EstaOcupado) return;

        try
        {
            EstaOcupado = true;
            SaldosMayores.Clear();

            var datos = await _contabilidadService.ObtenerLibroMayorAsync();
            foreach (var item in datos)
            {
                SaldosMayores.Add(item);
            }

            CalcularTotales();
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"No se pudo cargar el libro mayor: {ex.Message}", "OK");
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private void CalcularTotales()
    {
        SumaTotalDebe = SaldosMayores.Sum(x => x.TotalDebe);
        SumaTotalHaber = SaldosMayores.Sum(x => x.TotalHaber);
        SumaTotalSaldoDeudor = SaldosMayores.Sum(x => x.SaldoDeudor);
        SumaTotalSaldoAcreedor = SaldosMayores.Sum(x => x.SaldoAcreedor);
    }
}
