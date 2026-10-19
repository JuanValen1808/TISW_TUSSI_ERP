using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using TISW_TUSSI_ERP.Models.Contabilidad;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Contabilidad;

public class MayorCentralizadoViewModel : BaseViewModel
{
    private readonly ContabilidadService _contabilidadService;

    public ObservableCollection<MayorCentralizadoItem> Items { get; } = new();
    public ObservableCollection<PlanCuentas> CuentasFiltro { get; } = new();

    private PlanCuentas? _cuentaSeleccionada;
    public PlanCuentas? CuentaSeleccionada
    {
        get => _cuentaSeleccionada;
        set => SetProperty(ref _cuentaSeleccionada, value);
    }

    private DateTime _fechaDesde = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
    public DateTime FechaDesde
    {
        get => _fechaDesde;
        set => SetProperty(ref _fechaDesde, value);
    }

    private DateTime _fechaHasta = DateTime.Now;
    public DateTime FechaHasta
    {
        get => _fechaHasta;
        set => SetProperty(ref _fechaHasta, value);
    }

    private decimal _totalDebeFiltro;
    public decimal TotalDebeFiltro { get => _totalDebeFiltro; set => SetProperty(ref _totalDebeFiltro, value); }

    private decimal _totalHaberFiltro;
    public decimal TotalHaberFiltro { get => _totalHaberFiltro; set => SetProperty(ref _totalHaberFiltro, value); }

    public ICommand BuscarCommand { get; }

    public MayorCentralizadoViewModel(ContabilidadService contabilidadService)
    {
        _contabilidadService = contabilidadService;
        Titulo = "Mayor Centralizado";
        BuscarCommand = new Command(async () => await BuscarAsync());
        
        _ = CargarCuentasFiltroAsync();
    }

    private async Task CargarCuentasFiltroAsync()
    {
        var cuentas = await _contabilidadService.ObtenerPlanCuentasAsync();
        
        // Agregar opcion "Todas las Cuentas" con Id 0
        CuentasFiltro.Add(new PlanCuentas { IdCuenta = 0, CodigoCuenta = "0", NombreCuenta = "-- TODAS LAS CUENTAS --" });
        
        foreach (var c in cuentas.OrderBy(x => x.CodigoCuenta))
        {
            CuentasFiltro.Add(c);
        }
        
        CuentaSeleccionada = CuentasFiltro.First();
    }

    private async Task BuscarAsync()
    {
        if (EstaOcupado) return;

        try
        {
            EstaOcupado = true;
            Items.Clear();

            int? idCuentaFiltro = CuentaSeleccionada?.IdCuenta == 0 ? null : CuentaSeleccionada?.IdCuenta;

            var resultados = await _contabilidadService.ObtenerMayorCentralizadoAsync(idCuentaFiltro, FechaDesde, FechaHasta);
            
            foreach (var item in resultados)
            {
                Items.Add(item);
            }

            TotalDebeFiltro = Items.Sum(x => x.Debe);
            TotalHaberFiltro = Items.Sum(x => x.Haber);
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"No se pudo cargar el reporte: {ex.Message}", "OK");
        }
        finally
        {
            EstaOcupado = false;
        }
    }
}
