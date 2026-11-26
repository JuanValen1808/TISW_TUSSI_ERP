
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Auth;
using TISW_TUSSI_ERP.Models.Ventas;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Ventas;

public class CuentasPorCobrarVentasViewModel : BaseViewModel
{
    private readonly CuentasPorCobrarVentasService _servicio;
    private readonly CajaService _cajaService;

    private List<CuentaPorCobrarVenta> _todas = new();

    public ObservableCollection<CuentaPorCobrarVenta> Cuentas
    { get; } = new();

    private string _busqueda = "";

    public string Busqueda
    {
        get => _busqueda;
        set
        {
            if (SetProperty(ref _busqueda, value))
                Filtrar();
        }
    }

    private string _mensajeError = "";

    public string MensajeError
    {
        get => _mensajeError;
        private set
        {
            if (SetProperty(ref _mensajeError, value))
                OnPropertyChanged(nameof(HayError));
        }
    }

    public bool HayError =>
        !string.IsNullOrEmpty(MensajeError);

    public string TotalPendienteTexto =>
        Formato.Moneda(Cuentas.Sum(c => c.Saldo));

    public string CantidadTexto =>
        $"Documentos: {Cuentas.Count}";

    public ICommand ComandoActualizar { get; }

    public ICommand ComandoRegistrarAbono { get; }

    public CuentasPorCobrarVentasViewModel(
        CuentasPorCobrarVentasService servicio,
        CajaService cajaService)
    {
        _servicio = servicio;
        _cajaService = cajaService;

        Titulo = "Cuentas por Cobrar · Ventas";

        ComandoActualizar =
            new Command(async () => await CargarAsync());

        ComandoRegistrarAbono =
            new Command<CuentaPorCobrarVenta>(
                async cuenta => await RegistrarAbonoAsync(cuenta));
    }

    public async Task CargarAsync()
    {
        if (EstaOcupado)
            return;

        EstaOcupado = true;
        MensajeError = "";

        try
        {
            _todas = await _servicio.ObtenerAsync();
            Filtrar();
        }
        catch (Exception ex)
        {
            MensajeError =
                "No se pudieron consultar las cuentas: "
                + ex.Message;
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private async Task<int?> SeleccionarTurnoAbiertoAsync()
    {
        var pagina = Shell.Current;

        var cajas = await _cajaService.ObtenerCajasAsync();

        var disponibles = new List<(Caja Caja, CajaTurno Turno)>();

        foreach (var caja in cajas)
        {
            var turno =
                await _cajaService.ObtenerTurnoAbiertoAsync(
                    caja.IdCaja);

            if (turno?.EstaAbierta == true)
                disponibles.Add((caja, turno));
        }

        if (disponibles.Count == 0)
        {
            await pagina.DisplayAlertAsync(
                "Caja cerrada",
                "No hay ninguna caja con turno abierto. " +
                "Abre una caja desde Control de Caja " +
                "antes de registrar un cobro.",
                "Aceptar");

            return null;
        }

        if (disponibles.Count == 1)
            return disponibles[0].Turno.IdTurno;

        // Usamos el ID para evitar depender
        // de propiedades de nombre no verificadas.
        string[] opciones = disponibles
            .Select(x =>
                $"Caja #{x.Caja.IdCaja} - Turno #{x.Turno.IdTurno}")
            .ToArray();

        string? seleccion = await pagina.DisplayActionSheetAsync(
            "Selecciona la caja que recibirá el efectivo",
            "Cancelar",
            null,
            opciones);

        if (string.IsNullOrWhiteSpace(seleccion))
            return null;

        int indice = Array.IndexOf(opciones, seleccion);

        if (indice < 0)
            return null;

        return disponibles[indice].Turno.IdTurno;
    }

    private async Task RegistrarAbonoAsync(
        CuentaPorCobrarVenta? cuenta)
    {
        if (cuenta == null || EstaOcupado)
            return;

        var pagina = Shell.Current;

        if (cuenta.Saldo <= 0 ||
            cuenta.Estado == "PAGADA" ||
            cuenta.Estado == "ANULADA")
        {
            await pagina.DisplayAlertAsync(
                "Cuenta no disponible",
                "Esta cuenta no tiene un saldo pendiente válido.",
                "Aceptar");

            return;
        }

        string? montoTexto = await pagina.DisplayPromptAsync(
            "Registrar abono",
            $"Cliente: {cuenta.Cliente}\n" +
            $"Folio: {cuenta.Folio}\n" +
            $"Saldo pendiente: {Formato.Moneda(cuenta.Saldo)}\n\n" +
            "Ingresa el monto del abono:",
            "Continuar",
            "Cancelar",
            placeholder: "Ejemplo: 1000",
            keyboard: Keyboard.Numeric);

        if (string.IsNullOrWhiteSpace(montoTexto))
            return;

        if (!decimal.TryParse(
                montoTexto.Trim(),
                NumberStyles.Number,
                CultureInfo.GetCultureInfo("es-CL"),
                out decimal monto) ||
            monto <= 0 ||
            decimal.Round(monto, 2) != monto)
        {
            await pagina.DisplayAlertAsync(
                "Monto inválido",
                "Ingresa un monto positivo con máximo dos decimales.",
                "Aceptar");

            return;
        }

        if (monto > cuenta.Saldo)
        {
            await pagina.DisplayAlertAsync(
                "Monto inválido",
                $"El abono no puede superar {Formato.Moneda(cuenta.Saldo)}.",
                "Aceptar");

            return;
        }

        string? formaPago = await pagina.DisplayActionSheetAsync(
            "Selecciona la forma de pago",
            "Cancelar",
            null,
            "EFECTIVO",
            "TARJETA",
            "TRANSFERENCIA");

        if (formaPago != "EFECTIVO" &&
            formaPago != "TARJETA" &&
            formaPago != "TRANSFERENCIA")
            return;

        int? idTurno;

        try
        {
            idTurno = await SeleccionarTurnoAbiertoAsync();

            if (idTurno == null)
                return;
        }
        catch (Exception ex)
        {
            await pagina.DisplayAlertAsync(
                "Error al consultar caja",
                ex.Message,
                "Aceptar");

            return;
        }

        int idUsuario = Sesion.UsuarioActual?.IdUsuario ?? 0;

        if (idUsuario <= 0)
        {
            await pagina.DisplayAlertAsync(
                "Sesión requerida",
                "Debes iniciar sesión para registrar abonos.",
                "Aceptar");

            return;
        }

        string detalleCaja = formaPago == "EFECTIVO"
            ? $"\nTurno de caja: #{idTurno}"
            : "\nNo modifica el efectivo físico.";

        bool confirmar = await pagina.DisplayAlertAsync(
            "Confirmar abono",
            $"Cliente: {cuenta.Cliente}\n" +
            $"Monto: {Formato.Moneda(monto)}\n" +
            $"Forma de pago: {formaPago}\n" +
            $"Saldo restante estimado: " +
            $"{Formato.Moneda(cuenta.Saldo - monto)}" +
            detalleCaja,
            "Registrar",
            "Cancelar");

        if (!confirmar)
            return;

        EstaOcupado = true;
        MensajeError = "";

        bool registrado = false;

        try
        {
            await _servicio.RegistrarAbonoAsync(
                cuenta.IdCuenta,
                monto,
                formaPago,
                idUsuario,
                idTurno);

            registrado = true;
        }
        catch (Exception ex)
        {
            await pagina.DisplayAlertAsync(
                "No se pudo registrar",
                ex.Message,
                "Aceptar");
        }
        finally
        {
            EstaOcupado = false;
        }

        if (registrado)
        {
            await CargarAsync();

            await pagina.DisplayAlertAsync(
                "Abono registrado",
                $"Se registró un abono de {Formato.Moneda(monto)}.\n" +
                (formaPago == "EFECTIVO"
                    ? "El ingreso quedó vinculado al turno de caja."
                    : "El pago quedó registrado sin modificar el efectivo."),
                "Aceptar");
        }
    }

    private void Filtrar()
    {
        var q = (Busqueda ?? "").Trim();

        var filtradas = _todas.Where(c =>
            string.IsNullOrEmpty(q)
            || c.Folio.Contains(
                q, StringComparison.OrdinalIgnoreCase)
            || c.Cliente.Contains(
                q, StringComparison.OrdinalIgnoreCase)
            || c.Rut.Contains(
                q, StringComparison.OrdinalIgnoreCase)
            || c.Estado.Contains(
                q, StringComparison.OrdinalIgnoreCase)
        ).ToList();

        Cuentas.Clear();

        foreach (var cuenta in filtradas)
            Cuentas.Add(cuenta);

        OnPropertyChanged(nameof(TotalPendienteTexto));
        OnPropertyChanged(nameof(CantidadTexto));
    }
}
