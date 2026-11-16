
using System;
using TISW_TUSSI_ERP.ViewModels.Ventas;

namespace TISW_TUSSI_ERP.Views.Ventas;

public partial class CajaPage : ContentPage
{
    private readonly CajaViewModel _vm;

    public CajaPage(CajaViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            await _vm.CargarAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Error al cargar caja",
                ex.Message,
                "Aceptar");
        }
    }

    private async void OnAbrirClicked(
        object sender,
        EventArgs e)
    {
        if (_vm.HayTurnoAbierto)
            return;

        bool confirmar = await DisplayAlertAsync(
            "Abrir caja",
            $"¿Confirmas abrir la caja con monto inicial de " +
            $"{_vm.MontoInicialTexto}?",
            "Abrir",
            "Cancelar");

        if (confirmar && _vm.ComandoAbrir.CanExecute(null))
            _vm.ComandoAbrir.Execute(null);
    }

    private async void OnMovimientoClicked(
        object sender,
        EventArgs e)
    {
        if (!_vm.HayTurnoAbierto)
            return;

        bool confirmar = await DisplayAlertAsync(
            "Registrar movimiento",
            $"¿Confirmas {_vm.TipoMovimiento.ToLowerInvariant()} " +
            $"por {_vm.MontoMovimientoTexto}?\n" +
            $"Concepto: {_vm.Concepto}",
            "Registrar",
            "Cancelar");

        if (confirmar && _vm.ComandoMovimiento.CanExecute(null))
            _vm.ComandoMovimiento.Execute(null);
    }

    private async void OnCerrarClicked(
        object sender,
        EventArgs e)
    {
        if (!_vm.HayTurnoAbierto)
            return;

        bool confirmar = await DisplayAlertAsync(
            "Confirmar cierre de caja",
            $"Saldo esperado: {_vm.SaldoEsperadoTexto}\n" +
            $"Efectivo contado: {_vm.MontoFinalTexto}\n\n" +
            "Esta operación cerrará el turno. ¿Continuar?",
            "Cerrar caja",
            "Cancelar");

        if (confirmar && _vm.ComandoCerrar.CanExecute(null))
            _vm.ComandoCerrar.Execute(null);
    }
}
