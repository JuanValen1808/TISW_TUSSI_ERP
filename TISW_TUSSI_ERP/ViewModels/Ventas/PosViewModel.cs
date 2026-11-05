using System.Collections.ObjectModel;
using System.Windows.Input;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Auth;
using TISW_TUSSI_ERP.Models.Ventas;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Ventas;

// ViewModel del Punto de Venta (RF-VEN-02).
// Contiene las validaciones de operación:
// cantidad > 0, stock por lote, lote no vencido
// y convenio obligatorio para ventas a crédito.
public class PosViewModel : BaseViewModel
{
    private readonly VentasService _servicio;

    public ObservableCollection<ItemCarrito> Carrito { get; } = new();
    public ObservableCollection<LotePos> Lotes { get; } = new();
    public ObservableCollection<ClientePos> Clientes { get; } = new();
    public ObservableCollection<ConvenioPos> Convenios { get; } = new();

    // Coincide con el ENUM forma_pago de ventas_encabezado.
    public string[] FormasPago { get; } =
    {
        "EFECTIVO",
        "TARJETA",
        "TRANSFERENCIA",
        "CREDITO"
    };

    // ---------------------------------------------------------
    // BÚSQUEDA DE PRODUCTO
    // ---------------------------------------------------------

    private string _codigoBuscado = string.Empty;

    public string CodigoBuscado
    {
        get => _codigoBuscado;
        set => SetProperty(ref _codigoBuscado, value);
    }

    private ProductoPos? _producto;

    public ProductoPos? Producto
    {
        get => _producto;
        private set
        {
            if (SetProperty(ref _producto, value))
                OnPropertyChanged(nameof(HayProducto));
        }
    }

    public bool HayProducto => Producto is not null;

    private LotePos? _loteSeleccionado;

    public LotePos? LoteSeleccionado
    {
        get => _loteSeleccionado;
        set => SetProperty(ref _loteSeleccionado, value);
    }

    private string _cantidadTexto = string.Empty;

    public string CantidadTexto
    {
        get => _cantidadTexto;
        set => SetProperty(ref _cantidadTexto, value);
    }

    // ---------------------------------------------------------
    // CLIENTE / CONVENIO / FORMA DE PAGO
    // ---------------------------------------------------------

    private ClientePos? _clienteSeleccionado;

    public ClientePos? ClienteSeleccionado
    {
        get => _clienteSeleccionado;

        set
        {
            if (!SetProperty(ref _clienteSeleccionado, value))
                return;

            _ = CargarConveniosAsync(value);
        }
    }

    private ConvenioPos? _convenioSeleccionado;

    public ConvenioPos? ConvenioSeleccionado
    {
        get => _convenioSeleccionado;

        set
        {
            if (SetProperty(ref _convenioSeleccionado, value))
                Recalcular();
        }
    }

    private string _formaPago = "EFECTIVO";

    public string FormaPagoSeleccionada
    {
        get => _formaPago;

        set
        {
            if (SetProperty(ref _formaPago, value))
                Recalcular();
        }
    }

    // ---------------------------------------------------------
    // TOTALES
    // ---------------------------------------------------------

    public string SubtotalTexto =>
        Formato.Moneda(Carrito.Sum(i => i.Subtotal));

    public string DescuentoTexto =>
        Formato.Moneda(Descuento);

    public string NetoTexto =>
        Formato.Moneda(Neto);

    public string IvaTexto =>
        Formato.Moneda(Iva);

    public string TotalTexto =>
        Formato.Moneda(Total);

    public bool HayItems => Carrito.Count > 0;

    // El descuento del convenio solamente se aplica
    // cuando la venta se realiza a crédito.
    private decimal PctDescuento =>
        FormaPagoSeleccionada == "CREDITO"
            ? ConvenioSeleccionado?.PorcentajeDescuento ?? 0m
            : 0m;

    private decimal Descuento =>
        Math.Round(
            Carrito.Sum(i => i.Subtotal) * PctDescuento / 100m
        );

    private decimal Total =>
        Carrito.Sum(i => i.Subtotal) - Descuento;

    private decimal Neto =>
        Math.Round(Total / 1.19m);

    private decimal Iva =>
        Total - Neto;

    // ---------------------------------------------------------
    // MENSAJES
    // ---------------------------------------------------------

    private string _mensajeError = string.Empty;

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

    private string _mensajeOk = string.Empty;

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
        !string.IsNullOrEmpty(MensajeOk);

    // ---------------------------------------------------------
    // COMANDOS
    // ---------------------------------------------------------

    public ICommand ComandoBuscar { get; }
    public ICommand ComandoAgregar { get; }
    public ICommand ComandoQuitar { get; }
    public ICommand ComandoEmitir { get; }
    public ICommand ComandoVerRanking { get; }

    // ---------------------------------------------------------
    // CONSTRUCTOR
    // ---------------------------------------------------------

    public PosViewModel(VentasService servicio)
    {
        _servicio = servicio;

        Titulo = "Punto de Venta";

        ComandoBuscar =
            new Command(async () => await BuscarAsync());

        ComandoAgregar =
            new Command(async () => await AgregarAsync());

        ComandoQuitar =
            new Command<ItemCarrito>(i =>
            {
                if (i is not null)
                {
                    Carrito.Remove(i);
                    Recalcular();
                }
            });

        ComandoEmitir =
            new Command(async () => await EmitirAsync());

        ComandoVerRanking =
            new Command(async () =>
                await Shell.Current.GoToAsync("//RankingVentasPage"));
    }

    // ---------------------------------------------------------
    // CARGAR CLIENTES
    // ---------------------------------------------------------

    public async Task CargarAsync()
    {
        if (EstaOcupado)
            return;

        EstaOcupado = true;

        MensajeError = string.Empty;

        try
        {
            var clientes =
                await _servicio.ObtenerClientesAsync();

            Clientes.Clear();

            foreach (var cliente in clientes)
                Clientes.Add(cliente);

            ClienteSeleccionado ??=
                Clientes.FirstOrDefault();
        }
        catch (Exception ex)
        {
            MensajeError =
                "No se pudieron cargar los clientes: " +
                ex.Message;
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    // ---------------------------------------------------------
    // BUSCAR PRODUCTO
    // ---------------------------------------------------------

    private async Task BuscarAsync()
    {
        MensajeError = string.Empty;
        MensajeOk = string.Empty;

        if (string.IsNullOrWhiteSpace(CodigoBuscado))
        {
            MensajeError =
                "Ingrese un código de barras o SKU.";

            return;
        }

        EstaOcupado = true;

        try
        {
            Producto =
                await _servicio.BuscarProductoAsync(
                    CodigoBuscado
                );

            Lotes.Clear();

            if (Producto is null)
            {
                MensajeError =
                    "No existe un producto activo con ese código.";

                return;
            }

            var lotes =
                await _servicio.ObtenerLotesAsync(
                    Producto.IdProducto
                );

            if (lotes.Count == 0)
            {
                Producto = null;

                MensajeError =
                    "El producto no tiene lotes con stock disponible.";

                return;
            }

            foreach (var lote in lotes)
                Lotes.Add(lote);

            // FEFO: primero el lote que vence antes.
            LoteSeleccionado = Lotes.First();

            CantidadTexto = "1";
        }
        catch (Exception ex)
        {
            MensajeError =
                "Error en la búsqueda: " +
                ex.Message;
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    // ---------------------------------------------------------
    // AGREGAR PRODUCTO AL CARRITO
    // ---------------------------------------------------------

    private async Task AgregarAsync()
    {
        MensajeError = string.Empty;
        MensajeOk = string.Empty;

        if (Producto is null ||
            LoteSeleccionado is null)
        {
            return;
        }

        // Validación: cantidad entera mayor a cero.
        if (!int.TryParse(
                CantidadTexto,
                out int cantidad
            ) ||
            cantidad <= 0)
        {
            MensajeError =
                "La cantidad debe ser un número entero mayor a cero.";

            return;
        }

        // Validación de stock.
        if (cantidad >
            LoteSeleccionado.StockActual)
        {
            MensajeError =
                $"El lote {LoteSeleccionado.NumeroLote} " +
                $"solo tiene {LoteSeleccionado.StockActual} unidades.";

            return;
        }

        // Validación de vencimiento.
        if (LoteSeleccionado.FechaVencimiento <
            DateTime.Today)
        {
            MensajeError =
                $"El lote {LoteSeleccionado.NumeroLote} " +
                $"está vencido " +
                $"({LoteSeleccionado.FechaVencimiento:dd/MM/yyyy}) " +
                $"y no puede venderse.";

            return;
        }

        var existente =
            Carrito.FirstOrDefault(
                i => i.IdLote ==
                     LoteSeleccionado.IdLote
            );

        if (existente is not null)
        {
            if (existente.Cantidad + cantidad >
                LoteSeleccionado.StockActual)
            {
                MensajeError =
                    $"Stock máximo del lote alcanzado " +
                    $"({LoteSeleccionado.StockActual} unidades).";

                return;
            }

            existente.Cantidad += cantidad;
        }
        else
        {
            Carrito.Add(
                new ItemCarrito
                {
                    IdProducto =
                        Producto.IdProducto,

                    IdLote =
                        LoteSeleccionado.IdLote,

                    NombreProducto =
                        Producto.NombreComercial,

                    NumeroLote =
                        LoteSeleccionado.NumeroLote,

                    Cantidad =
                        cantidad,

                    PrecioUnitario =
                        Producto.PrecioVenta
                }
            );
        }

        Producto = null;

        Lotes.Clear();

        CodigoBuscado = string.Empty;

        Recalcular();

        await Task.CompletedTask;
    }

    // ---------------------------------------------------------
    // EMITIR VENTA
    // ---------------------------------------------------------

    private async Task EmitirAsync()
    {
        MensajeError = string.Empty;
        MensajeOk = string.Empty;

        if (Carrito.Count == 0)
        {
            MensajeError =
                "El carrito está vacío.";

            return;
        }

        if (ClienteSeleccionado is null)
        {
            MensajeError =
                "Seleccione un cliente.";

            return;
        }

        // El crédito exige convenio.
        if (FormaPagoSeleccionada == "CREDITO" &&
            ConvenioSeleccionado is null)
        {
            MensajeError =
                "Para pago a CRÉDITO debe seleccionar " +
                "un convenio de salud.";

            return;
        }

        var idCajero =
            Sesion.UsuarioActual?.IdUsuario ?? 0;

        if (idCajero == 0)
        {
            MensajeError =
                "No hay un usuario con sesión activa.";

            return;
        }

        EstaOcupado = true;

        try
        {
            // Solo asociamos convenio a la venta
            // cuando la forma de pago es CREDITO.
            int? idConvenio =
                FormaPagoSeleccionada == "CREDITO"
                    ? ConvenioSeleccionado?.IdConvenio
                    : null;

            var folio =
                await _servicio.EmitirVentaAsync(
                    ClienteSeleccionado.IdCliente,
                    idConvenio,
                    FormaPagoSeleccionada,
                    Carrito.ToList(),
                    Carrito.Sum(i => i.Subtotal),
                    Descuento,
                    Neto,
                    Iva,
                    Total,
                    idCajero
                );

            MensajeOk =
                $"Venta {folio} registrada por " +
                $"{Formato.Moneda(Total)}.";

            Carrito.Clear();

            ConvenioSeleccionado = null;

            Recalcular();
        }
        catch (Exception ex)
        {
            MensajeError = ex.Message;
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    // ---------------------------------------------------------
    // CARGAR CONVENIOS DEL CLIENTE
    // ---------------------------------------------------------

    private async Task CargarConveniosAsync(
        ClientePos? cliente
    )
    {
        Convenios.Clear();

        ConvenioSeleccionado = null;

        if (cliente is null)
        {
            Recalcular();
            return;
        }

        try
        {
            var convenios =
                await _servicio.ObtenerConveniosAsync(
                    cliente.IdCliente
                );

            foreach (var convenio in convenios)
                Convenios.Add(convenio);

            // No seleccionamos automáticamente.
            // El cajero debe elegirlo para una venta a crédito.
            ConvenioSeleccionado = null;
        }
        catch (Exception ex)
        {
            MensajeError =
                "No se pudieron cargar los convenios: " +
                ex.Message;
        }

        Recalcular();

        await Task.CompletedTask;
    }

    // ---------------------------------------------------------
    // RECALCULAR TOTALES
    // ---------------------------------------------------------

    private void Recalcular()
    {
        OnPropertyChanged(nameof(SubtotalTexto));
        OnPropertyChanged(nameof(DescuentoTexto));
        OnPropertyChanged(nameof(NetoTexto));
        OnPropertyChanged(nameof(IvaTexto));
        OnPropertyChanged(nameof(TotalTexto));
        OnPropertyChanged(nameof(HayItems));
    }
}