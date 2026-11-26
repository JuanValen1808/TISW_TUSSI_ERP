
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Auth;
using TISW_TUSSI_ERP.Models.Ventas;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Ventas;

public class CajaViewModel : BaseViewModel
{
    private readonly CajaService _servicio;
    private readonly CultureInfo _cultura =
        CultureInfo.GetCultureInfo("es-CL");

    private bool _ocupado;
    private Caja? _cajaSeleccionada;
    private CajaTurno? _turnoActual;

    private string _montoInicialTexto = "0";
    private string _montoMovimientoTexto = "";
    private string _tipoMovimiento = "INGRESO";
    private string _concepto = "";
    private string _montoFinalTexto = "";
    private string _observaciones = "";
    private string _mensaje = "";

    private decimal _saldoEsperado;
    private ResumenPagosCaja _resumen = new();

    public ObservableCollection<Caja> Cajas { get; } = new();
    public ObservableCollection<MovimientoCaja> Movimientos { get; } = new();

    public List<string> TiposMovimiento { get; } =
        new() { "INGRESO", "EGRESO" };

    public CajaViewModel(CajaService servicio)
    {
        _servicio = servicio;
        Titulo = "Control de Caja";

        ComandoAbrir = new Command(async () => await AbrirAsync());
        ComandoMovimiento = new Command(
            async () => await RegistrarMovimientoAsync());
        ComandoCerrar = new Command(async () => await CerrarAsync());
        ComandoActualizar = new Command(async () => await CargarAsync());
    }

    public Caja? CajaSeleccionada
    {
        get => _cajaSeleccionada;
        set
        {
            if (_cajaSeleccionada == value) return;

            _cajaSeleccionada = value;
            OnPropertyChanged();

            if (!_ocupado)
                _ = CargarTurnoAsync();
        }
    }

    public CajaTurno? TurnoActual
    {
        get => _turnoActual;
        private set
        {
            _turnoActual = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HayTurnoAbierto));
            OnPropertyChanged(nameof(NoHayTurnoAbierto));
            OnPropertyChanged(nameof(EstadoCaja));
        }
    }

    public bool HayTurnoAbierto =>
        TurnoActual?.EstaAbierta == true;

    public bool NoHayTurnoAbierto => !HayTurnoAbierto;

    public string EstadoCaja =>
        HayTurnoAbierto ? "ABIERTA" : "CERRADA";

    public string MontoInicialTexto
    {
        get => _montoInicialTexto;
        set
        {
            _montoInicialTexto = value;
            OnPropertyChanged();
        }
    }

    public string MontoMovimientoTexto
    {
        get => _montoMovimientoTexto;
        set
        {
            _montoMovimientoTexto = value;
            OnPropertyChanged();
        }
    }

    public string TipoMovimiento
    {
        get => _tipoMovimiento;
        set
        {
            _tipoMovimiento = value;
            OnPropertyChanged();
        }
    }

    public string Concepto
    {
        get => _concepto;
        set
        {
            _concepto = value;
            OnPropertyChanged();
        }
    }

    public string MontoFinalTexto
    {
        get => _montoFinalTexto;
        set
        {
            _montoFinalTexto = value;
            OnPropertyChanged();
        }
    }

    public string Observaciones
    {
        get => _observaciones;
        set
        {
            _observaciones = value;
            OnPropertyChanged();
        }
    }

    public string Mensaje
    {
        get => _mensaje;
        set
        {
            _mensaje = value;
            OnPropertyChanged();
        }
    }

    public decimal SaldoEsperado
    {
        get => _saldoEsperado;
        private set
        {
            _saldoEsperado = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SaldoEsperadoTexto));
        }
    }

    public string SaldoEsperadoTexto =>
        SaldoEsperado.ToString("C0", _cultura);

    public string EfectivoVentasTexto =>
        _resumen.Efectivo.ToString("C0", _cultura);

    public string TarjetaTexto =>
        _resumen.Tarjeta.ToString("C0", _cultura);

    public string TransferenciaTexto =>
        _resumen.Transferencia.ToString("C0", _cultura);

    public string CreditoTexto =>
        _resumen.Credito.ToString("C0", _cultura);

    public string OtrosPagosTexto =>
        _resumen.Otros.ToString("C0", _cultura);

    public string TotalVentasTexto =>
        _resumen.Total.ToString("C0", _cultura);

    public ICommand ComandoAbrir { get; }
    public ICommand ComandoMovimiento { get; }
    public ICommand ComandoCerrar { get; }
    public ICommand ComandoActualizar { get; }

    private void ActualizarResumen(ResumenPagosCaja resumen)
    {
        _resumen = resumen;

        OnPropertyChanged(nameof(EfectivoVentasTexto));
        OnPropertyChanged(nameof(TarjetaTexto));
        OnPropertyChanged(nameof(TransferenciaTexto));
        OnPropertyChanged(nameof(CreditoTexto));
        OnPropertyChanged(nameof(OtrosPagosTexto));
        OnPropertyChanged(nameof(TotalVentasTexto));
    }

    private static int ObtenerUsuario()
    {
        int id = Sesion.UsuarioActual?.IdUsuario ?? 0;

        if (id <= 0)
            throw new Exception("Debes iniciar sesión.");

        return id;
    }

    private static decimal LeerMonto(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            throw new Exception("Debes ingresar un monto.");

        if (!decimal.TryParse(
                texto,
                NumberStyles.Number,
                CultureInfo.GetCultureInfo("es-CL"),
                out decimal monto))
        {
            if (!decimal.TryParse(
                    texto,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out monto))
            {
                throw new Exception("Ingresa un monto numérico válido.");
            }
        }

        return monto;
    }

    public async Task CargarAsync()
    {
        if (_ocupado) return;

        _ocupado = true;

        try
        {
            Mensaje = "";

            int? idAnterior = CajaSeleccionada?.IdCaja;

            var cajas = await _servicio.ObtenerCajasAsync();

            Cajas.Clear();

            foreach (var caja in cajas)
                Cajas.Add(caja);

            _cajaSeleccionada =
                Cajas.FirstOrDefault(c => c.IdCaja == idAnterior)
                ?? Cajas.FirstOrDefault();

            OnPropertyChanged(nameof(CajaSeleccionada));

            await ActualizarTurnoAsync();
        }
        catch (Exception ex)
        {
            Mensaje = "Error: " + ex.Message;
        }
        finally
        {
            _ocupado = false;
        }
    }

    private async Task CargarTurnoAsync()
    {
        if (_ocupado) return;

        _ocupado = true;

        try
        {
            Mensaje = "";
            await ActualizarTurnoAsync();
        }
        catch (Exception ex)
        {
            Mensaje = "Error: " + ex.Message;
        }
        finally
        {
            _ocupado = false;
        }
    }

    private async Task ActualizarTurnoAsync()
    {
        Movimientos.Clear();
        SaldoEsperado = 0;
        TurnoActual = null;
        ActualizarResumen(new ResumenPagosCaja());

        if (CajaSeleccionada == null)
            return;

        TurnoActual =
            await _servicio.ObtenerTurnoAbiertoAsync(
                CajaSeleccionada.IdCaja);

        if (TurnoActual == null)
            return;

        var movimientos =
            await _servicio.ObtenerMovimientosAsync(
                TurnoActual.IdTurno);

        foreach (var movimiento in movimientos)
            Movimientos.Add(movimiento);

        SaldoEsperado =
            await _servicio.CalcularSaldoAsync(
                TurnoActual.IdTurno);

        var resumen =
            await _servicio.ObtenerResumenPagosAsync(
                TurnoActual.IdTurno);

        ActualizarResumen(resumen);
    }

    private async Task AbrirAsync()
    {
        if (_ocupado) return;

        _ocupado = true;

        try
        {
            if (CajaSeleccionada == null)
                throw new Exception("Selecciona una caja.");

            if (HayTurnoAbierto)
                throw new Exception("La caja ya está abierta.");

            decimal monto = LeerMonto(MontoInicialTexto);

            if (monto < 0)
                throw new Exception(
                    "El monto inicial no puede ser negativo.");

            await _servicio.AbrirCajaAsync(
                CajaSeleccionada.IdCaja,
                ObtenerUsuario(),
                monto);

            await ActualizarTurnoAsync();

            Mensaje = "Caja abierta correctamente.";
        }
        catch (Exception ex)
        {
            Mensaje = "Error: " + ex.Message;
            if (Shell.Current != null)
                await Shell.Current.DisplayAlertAsync("No se pudo abrir la caja", ex.Message, "Aceptar");
        }
        finally
        {
            _ocupado = false;
        }
    }

    private async Task RegistrarMovimientoAsync()
    {
        if (_ocupado) return;

        _ocupado = true;

        try
        {
            if (!HayTurnoAbierto || TurnoActual == null)
                throw new Exception("Primero debes abrir una caja.");

            decimal monto = LeerMonto(MontoMovimientoTexto);

            if (monto <= 0)
                throw new Exception(
                    "El monto debe ser mayor que cero.");

            if (string.IsNullOrWhiteSpace(Concepto))
                throw new Exception("Ingresa un concepto.");

            await _servicio.RegistrarMovimientoAsync(
                TurnoActual.IdTurno,
                ObtenerUsuario(),
                TipoMovimiento,
                Concepto,
                monto);

            MontoMovimientoTexto = "";
            Concepto = "";

            await ActualizarTurnoAsync();

            Mensaje = "Movimiento registrado correctamente.";
        }
        catch (Exception ex)
        {
            Mensaje = "Error: " + ex.Message;
            if (Shell.Current != null)
                await Shell.Current.DisplayAlertAsync("Movimiento inválido", ex.Message, "Aceptar");
        }
        finally
        {
            _ocupado = false;
        }
    }

    private async Task CerrarAsync()
    {
        if (_ocupado) return;

        _ocupado = true;

        try
        {
            if (!HayTurnoAbierto || TurnoActual == null)
                throw new Exception("No hay un turno abierto.");

            decimal montoReal = LeerMonto(MontoFinalTexto);

            if (montoReal < 0)
                throw new Exception(
                    "El efectivo contado no puede ser negativo.");

            decimal diferencia =
                await _servicio.CerrarCajaAsync(
                    TurnoActual.IdTurno,
                    ObtenerUsuario(),
                    montoReal,
                    Observaciones);

            await ActualizarTurnoAsync();

            MontoFinalTexto = "";
            Observaciones = "";

            Mensaje = "Caja cerrada. Diferencia: " +
                diferencia.ToString("C0", _cultura);
        }
        catch (Exception ex)
        {
            Mensaje = "Error: " + ex.Message;
            if (Shell.Current != null)
                await Shell.Current.DisplayAlertAsync("No se pudo cerrar la caja", ex.Message, "Aceptar");
        }
        finally
        {
            _ocupado = false;
        }
    }
}
