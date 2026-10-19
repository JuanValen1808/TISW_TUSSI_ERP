using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using TISW_TUSSI_ERP.Models.Contabilidad;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Contabilidad;

public class BalanceOchoColumnasViewModel : BaseViewModel
{
    private readonly ContabilidadService _contabilidadService;

    public ObservableCollection<BalanceOchoColumnasItem> Items { get; } = new();

    // Totales Sumas
    private decimal _sumDebe;
    public decimal SumDebe { get => _sumDebe; set => SetProperty(ref _sumDebe, value); }
    private decimal _sumHaber;
    public decimal SumHaber { get => _sumHaber; set => SetProperty(ref _sumHaber, value); }

    // Totales Saldos
    private decimal _sumSaldoDeudor;
    public decimal SumSaldoDeudor { get => _sumSaldoDeudor; set => SetProperty(ref _sumSaldoDeudor, value); }
    private decimal _sumSaldoAcreedor;
    public decimal SumSaldoAcreedor { get => _sumSaldoAcreedor; set => SetProperty(ref _sumSaldoAcreedor, value); }

    // Totales Inventario
    private decimal _sumActivo;
    public decimal SumActivo { get => _sumActivo; set => SetProperty(ref _sumActivo, value); }
    private decimal _sumPasivo;
    public decimal SumPasivo { get => _sumPasivo; set => SetProperty(ref _sumPasivo, value); }

    // Totales Resultados
    private decimal _sumPerdida;
    public decimal SumPerdida { get => _sumPerdida; set => SetProperty(ref _sumPerdida, value); }
    private decimal _sumGanancia;
    public decimal SumGanancia { get => _sumGanancia; set => SetProperty(ref _sumGanancia, value); }

    // Resultado del Ejercicio (Utilidad o Pérdida)
    private decimal _resultadoEjercicio;
    public decimal ResultadoEjercicio
    {
        get => _resultadoEjercicio;
        set
        {
            if (SetProperty(ref _resultadoEjercicio, value))
            {
                OnPropertyChanged(nameof(EsUtilidad));
                OnPropertyChanged(nameof(EsPerdida));
            }
        }
    }

    public bool EsUtilidad => ResultadoEjercicio > 0;
    public bool EsPerdida => ResultadoEjercicio < 0;

    private string _textoResultado = string.Empty;
    public string TextoResultado { get => _textoResultado; set => SetProperty(ref _textoResultado, value); }

    // Totales Finales Cuadrados (para la última fila)
    private decimal _totalFinalActivo;
    public decimal TotalFinalActivo { get => _totalFinalActivo; set => SetProperty(ref _totalFinalActivo, value); }
    private decimal _totalFinalPasivo;
    public decimal TotalFinalPasivo { get => _totalFinalPasivo; set => SetProperty(ref _totalFinalPasivo, value); }
    private decimal _totalFinalPerdida;
    public decimal TotalFinalPerdida { get => _totalFinalPerdida; set => SetProperty(ref _totalFinalPerdida, value); }
    private decimal _totalFinalGanancia;
    public decimal TotalFinalGanancia { get => _totalFinalGanancia; set => SetProperty(ref _totalFinalGanancia, value); }

    public ICommand CargarBalanceCommand { get; }

    public BalanceOchoColumnasViewModel(ContabilidadService contabilidadService)
    {
        _contabilidadService = contabilidadService;
        Titulo = "Balance de 8 Columnas";
        CargarBalanceCommand = new Command(async () => await CargarBalanceAsync());
    }

    public async Task CargarBalanceAsync()
    {
        if (EstaOcupado) return;

        try
        {
            EstaOcupado = true;
            Items.Clear();

            var datos = await _contabilidadService.ObtenerBalanceOchoColumnasAsync();
            foreach (var item in datos)
            {
                Items.Add(item);
            }

            CalcularTotales();
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"No se pudo cargar el balance: {ex.Message}", "OK");
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private void CalcularTotales()
    {
        SumDebe = Items.Sum(x => x.TotalDebe);
        SumHaber = Items.Sum(x => x.TotalHaber);
        SumSaldoDeudor = Items.Sum(x => x.SaldoDeudor);
        SumSaldoAcreedor = Items.Sum(x => x.SaldoAcreedor);
        SumActivo = Items.Sum(x => x.Activo);
        SumPasivo = Items.Sum(x => x.Pasivo);
        SumPerdida = Items.Sum(x => x.Perdida);
        SumGanancia = Items.Sum(x => x.Ganancia);

        // Calcular Utilidad o Pérdida
        ResultadoEjercicio = SumGanancia - SumPerdida;

        if (ResultadoEjercicio >= 0)
        {
            TextoResultado = "UTILIDAD DEL EJERCICIO";
            TotalFinalActivo = SumActivo;
            TotalFinalPasivo = SumPasivo + ResultadoEjercicio;
            TotalFinalPerdida = SumPerdida + ResultadoEjercicio;
            TotalFinalGanancia = SumGanancia;
        }
        else
        {
            TextoResultado = "PÉRDIDA DEL EJERCICIO";
            TotalFinalActivo = SumActivo + Math.Abs(ResultadoEjercicio);
            TotalFinalPasivo = SumPasivo;
            TotalFinalPerdida = SumPerdida;
            TotalFinalGanancia = SumGanancia + Math.Abs(ResultadoEjercicio);
        }
    }
}
