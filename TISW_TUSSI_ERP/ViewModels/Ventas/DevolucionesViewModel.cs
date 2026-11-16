
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Auth;
using TISW_TUSSI_ERP.Models.Ventas;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Ventas;

public class DevolucionesViewModel : BaseViewModel
{
    private readonly DevolucionesService _servicio;

    public ObservableCollection<DetalleDevolucionVenta> Productos
    { get; } = new();

    private string _folioBuscado = "";

    public string FolioBuscado
    {
        get => _folioBuscado;
        set => SetProperty(ref _folioBuscado, value);
    }

    private DevolucionVenta? _ventaSeleccionada;

    public DevolucionVenta? VentaSeleccionada
    {
        get => _ventaSeleccionada;
        private set
        {
            if (SetProperty(ref _ventaSeleccionada, value))
            {
                OnPropertyChanged(nameof(HayVenta));
                OnPropertyChanged(nameof(FolioVenta));
                OnPropertyChanged(nameof(ClienteVenta));
            }
        }
    }

    public bool HayVenta => VentaSeleccionada != null;

    public string FolioVenta =>
        VentaSeleccionada?.FolioVenta ?? "";

    public string ClienteVenta =>
        VentaSeleccionada?.Cliente ?? "";

    private string _motivo = "";

    public string Motivo
    {
        get => _motivo;
        set => SetProperty(ref _motivo, value);
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
        !string.IsNullOrWhiteSpace(MensajeError);

    private string _mensajeOk = "";

    public string MensajeOk
    {
        get => _mensajeOk;
        private set
        {
            if (SetProperty(ref _mensajeOk, value))
                OnPropertyChanged(nameof(HayOk));
        }
    }

    public bool HayOk =>
        !string.IsNullOrWhiteSpace(MensajeOk);

    public int CantidadProductos =>
        Productos.Count(p => p.CantidadDevolver > 0);

    public int CantidadUnidades =>
        Productos.Sum(p => p.CantidadDevolver);

    public decimal TotalDevolucion =>
        Productos.Sum(p => p.SubtotalDevolucion);

    public string TotalDevolucionTexto =>
        Formato.Moneda(TotalDevolucion);

    public string ResumenTexto =>
        $"{CantidadProductos} productos · " +
        $"{CantidadUnidades} unidades";

    public ICommand ComandoBuscar { get; }
    public ICommand ComandoRegistrar { get; }
    public ICommand ComandoLimpiar { get; }

    public DevolucionesViewModel(
        DevolucionesService servicio)
    {
        _servicio = servicio;

        Titulo = "Devoluciones de Ventas";

        ComandoBuscar = new Command(
            async () => await BuscarAsync());

        ComandoRegistrar = new Command(
            async () => await RegistrarAsync());

        ComandoLimpiar = new Command(Limpiar);
    }

    // =====================================
    // BUSCAR VENTA
    // =====================================
    private async Task BuscarAsync()
    {
        if (EstaOcupado)
            return;

        MensajeError = "";
        MensajeOk = "";

        LimpiarVenta();

        if (string.IsNullOrWhiteSpace(FolioBuscado))
        {
            MensajeError = "Ingresa el folio de la venta.";
            return;
        }

        EstaOcupado = true;

        try
        {
            var venta = await _servicio.BuscarVentaAsync(
                FolioBuscado.Trim());

            if (venta == null)
            {
                MensajeError =
                    "No se encontró una venta pagada " +
                    "con ese folio.";
                return;
            }

            var detalles = await _servicio.ObtenerDetalleAsync(
                venta.IdVentaOriginal);

            VentaSeleccionada = venta;

            foreach (var detalle in detalles)
            {
                detalle.CantidadDevolver = 0;
                Productos.Add(detalle);
            }

            ActualizarTotales();

            if (Productos.Count == 0)
            {
                MensajeError =
                    "La venta no tiene productos registrados.";
            }
            else if (Productos.All(
                p => p.CantidadDisponible <= 0))
            {
                MensajeError =
                    "Todos los productos de esta venta " +
                    "ya fueron devueltos.";
            }
        }
        catch (Exception ex)
        {
            LimpiarVenta();

            MensajeError =
                "Error al buscar la venta: " + ex.Message;
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    // =====================================
    // ACTUALIZAR CANTIDAD
    // =====================================
    public void ActualizarCantidad(
        DetalleDevolucionVenta detalle,
        int cantidad)
    {
        if (detalle == null)
            return;

        cantidad = Math.Clamp(
            cantidad,
            0,
            Math.Max(0, detalle.CantidadDisponible));

        detalle.CantidadDevolver = cantidad;

        ActualizarTotales();
    }

    public void ActualizarTotales()
    {
        OnPropertyChanged(nameof(CantidadProductos));
        OnPropertyChanged(nameof(CantidadUnidades));
        OnPropertyChanged(nameof(TotalDevolucion));
        OnPropertyChanged(nameof(TotalDevolucionTexto));
        OnPropertyChanged(nameof(ResumenTexto));
    }

    // =====================================
    // REGISTRAR DEVOLUCIÓN
    // =====================================
    private async Task RegistrarAsync()
    {
        if (EstaOcupado)
            return;

        MensajeError = "";
        MensajeOk = "";

        if (VentaSeleccionada == null)
        {
            MensajeError = "Primero debes buscar una venta.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Motivo))
        {
            MensajeError =
                "Debes indicar el motivo de la devolución.";
            return;
        }

        if (Motivo.Trim().Length > 255)
        {
            MensajeError =
                "El motivo no puede superar los 255 caracteres.";
            return;
        }

        var seleccionados = Productos
            .Where(p => p.CantidadDevolver > 0)
            .ToList();

        if (seleccionados.Count == 0)
        {
            MensajeError =
                "Selecciona al menos un producto para devolver.";
            return;
        }

        foreach (var producto in seleccionados)
        {
            if (producto.CantidadDevolver >
                producto.CantidadDisponible)
            {
                MensajeError =
                    $"La cantidad de {producto.NombreProducto} " +
                    "supera las unidades disponibles.";
                return;
            }
        }

        int idUsuario =
            Sesion.UsuarioActual?.IdUsuario ?? 0;

        if (idUsuario <= 0)
        {
            MensajeError =
                "No se encontró un usuario con sesión activa.";
            return;
        }

        bool confirmar = await Shell.Current.DisplayAlertAsync(
            "Confirmar devolución",
            $"¿Registrar la devolución de " +
            $"{CantidadUnidades} unidades por " +
            $"{TotalDevolucionTexto}?\n\n" +
            "Los productos se registrarán en cuarentena. " +
            "No aumentarán el stock disponible.",
            "Registrar",
            "Cancelar");

        if (!confirmar)
            return;

        EstaOcupado = true;

        try
        {
            string folio =
                await _servicio.RegistrarDevolucionAsync(
                    VentaSeleccionada.IdVentaOriginal,
                    idUsuario,
                    Motivo.Trim(),
                    seleccionados);

            LimpiarVenta();

            Motivo = "";
            FolioBuscado = "";

            MensajeOk =
                "Devolución registrada correctamente.\n" +
                $"Folio interno: {folio}\n" +
                "Condición: CUARENTENA.\n" +
                "Stock vendible sin cambios.";

            await Shell.Current.DisplayAlertAsync(
                "Devolución registrada",
                MensajeOk,
                "Aceptar");
        }
        catch (Exception ex)
        {
            MensajeError =
                "No se pudo registrar la devolución: " +
                ex.Message;
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    // =====================================
    // LIMPIAR
    // =====================================
    private void Limpiar()
    {
        if (EstaOcupado)
            return;

        FolioBuscado = "";
        Motivo = "";

        MensajeError = "";
        MensajeOk = "";

        LimpiarVenta();
    }

    private void LimpiarVenta()
    {
        VentaSeleccionada = null;
        Productos.Clear();

        ActualizarTotales();
    }
}
