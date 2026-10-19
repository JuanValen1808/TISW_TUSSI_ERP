using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using TISW_TUSSI_ERP.Models.Contabilidad;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Contabilidad;

public class EstadoResultadosViewModel : BaseViewModel
{
    private readonly ContabilidadService _contabilidadService;

    public ObservableCollection<BalanceOchoColumnasItem> Ingresos { get; } = new();
    public ObservableCollection<BalanceOchoColumnasItem> Gastos { get; } = new();

    private decimal _totalIngresos;
    public decimal TotalIngresos { get => _totalIngresos; set => SetProperty(ref _totalIngresos, value); }

    private decimal _totalGastos;
    public decimal TotalGastos { get => _totalGastos; set => SetProperty(ref _totalGastos, value); }

    private decimal _resultadoEjercicio;
    public decimal ResultadoEjercicio { get => _resultadoEjercicio; set => SetProperty(ref _resultadoEjercicio, value); }

    private string _textoResultado = "RESULTADO DEL EJERCICIO";
    public string TextoResultado { get => _textoResultado; set => SetProperty(ref _textoResultado, value); }

    private Color _colorResultado = Colors.Black;
    public Color ColorResultado { get => _colorResultado; set => SetProperty(ref _colorResultado, value); }

    public ICommand CargarEstadoResultadosCommand { get; }

    public EstadoResultadosViewModel(ContabilidadService contabilidadService)
    {
        _contabilidadService = contabilidadService;
        Titulo = "Estado de Resultados";
        CargarEstadoResultadosCommand = new Command(async () => await CargarEstadoResultadosAsync());
    }

    public async Task CargarEstadoResultadosAsync()
    {
        if (EstaOcupado) return;

        try
        {
            EstaOcupado = true;
            Ingresos.Clear();
            Gastos.Clear();

            var datos = await _contabilidadService.ObtenerBalanceOchoColumnasAsync();
            
            foreach (var item in datos)
            {
                if (item.TipoCuenta.ToUpper() == "INGRESO")
                {
                    Ingresos.Add(item);
                }
                else if (item.TipoCuenta.ToUpper() == "GASTO")
                {
                    Gastos.Add(item);
                }
            }

            TotalIngresos = Ingresos.Sum(x => x.Ganancia);
            TotalGastos = Gastos.Sum(x => x.Perdida);

            ResultadoEjercicio = TotalIngresos - TotalGastos;

            if (ResultadoEjercicio >= 0)
            {
                TextoResultado = "UTILIDAD NETA DEL EJERCICIO";
                ColorResultado = Colors.DarkGreen;
            }
            else
            {
                TextoResultado = "PÉRDIDA NETA DEL EJERCICIO";
                ColorResultado = Colors.DarkRed;
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"No se pudo cargar el estado de resultados: {ex.Message}", "OK");
        }
        finally
        {
            EstaOcupado = false;
        }
    }
}
