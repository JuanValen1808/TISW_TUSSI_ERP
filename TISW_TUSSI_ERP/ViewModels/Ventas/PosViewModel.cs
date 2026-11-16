
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Auth;
using TISW_TUSSI_ERP.Models.Ventas;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Ventas;

public class PosViewModel : BaseViewModel
{
    private readonly VentasService _servicio;
    private int _versionCargaConvenios;

    public string UltimoComprobante { get; private set; } = string.Empty;

    public ObservableCollection<ItemCarrito> Carrito { get; } = new();
    public ObservableCollection<LotePos> Lotes { get; } = new();
    public ObservableCollection<ClientePos> Clientes { get; } = new();
    public ObservableCollection<ConvenioPos> Convenios { get; } = new();
    public ObservableCollection<ProductoPos> Productos { get; } = new();

    public string[] FormasPago { get; } =
    {
        "EFECTIVO",
        "TARJETA",
        "TRANSFERENCIA",
        "CREDITO"
    };

    public string[] TiposDescuento { get; } =
    {
        "Sin descuento",
        "Cumpleaños (10%)",
        "Promoción (15%)"
    };

    private string _codigoBuscado = string.Empty;
    public string CodigoBuscado
    {
        get => _codigoBuscado;
        set => SetProperty(ref _codigoBuscado, value);
    }

    private string _busquedaProducto = string.Empty;
    public string BusquedaProducto
    {
        get => _busquedaProducto;
        set => SetProperty(ref _busquedaProducto, value);
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

    public bool HayProducto => Producto != null;

    private LotePos? _loteSeleccionado;
    public LotePos? LoteSeleccionado
    {
        get => _loteSeleccionado;
        set => SetProperty(ref _loteSeleccionado, value);
    }

    private string _cantidadTexto = "1";
    public string CantidadTexto
    {
        get => _cantidadTexto;
        set => SetProperty(ref _cantidadTexto, value);
    }

    private ClientePos? _clienteSeleccionado;
    public ClientePos? ClienteSeleccionado
    {
        get => _clienteSeleccionado;
        set
        {
            if (!SetProperty(ref _clienteSeleccionado, value))
                return;

            _ = CargarConveniosAsync(value);
            OnPropertyChanged(nameof(NombreClienteActual));
            OnPropertyChanged(nameof(EsPublicoGeneral));
            Recalcular();
        }
    }

    public bool EsPublicoGeneral =>
        ClienteSeleccionado == null;

    public string NombreClienteActual =>
        ClienteSeleccionado?.Nombre ?? "Público general";

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

    private string _tipoDescuentoSeleccionado = "Sin descuento";
    public string TipoDescuentoSeleccionado
    {
        get => _tipoDescuentoSeleccionado;
        set
        {
            if (SetProperty(ref _tipoDescuentoSeleccionado, value))
                Recalcular();
        }
    }

    private decimal PorcentajePromocion =>
        TipoDescuentoSeleccionado switch
        {
            "Cumpleaños (10%)" => 10m,
            "Promoción (15%)" => 15m,
            _ => 0m
        };

    private decimal PorcentajeConvenio =>
        FormaPagoSeleccionada == "CREDITO"
            ? ConvenioSeleccionado?.PorcentajeDescuento ?? 0m
            : 0m;

    private decimal PorcentajeDescuento =>
        FormaPagoSeleccionada == "CREDITO"
            ? PorcentajeConvenio
            : PorcentajePromocion;

    public string PorcentajeDescuentoTexto =>
        $"{PorcentajeDescuento:0.##}%";

    public string MotivoDescuento =>
        FormaPagoSeleccionada == "CREDITO"
            ? ConvenioSeleccionado == null
                ? "Sin descuento"
                : $"Convenio: {ConvenioSeleccionado.NombreConvenio}"
            : TipoDescuentoSeleccionado;

    private decimal Subtotal =>
        Carrito.Sum(i => i.Subtotal);

    private decimal Descuento =>
        Math.Round(
            Subtotal * PorcentajeDescuento / 100m,
            0,
            MidpointRounding.AwayFromZero);

    private decimal Total => Subtotal - Descuento;

    private decimal Neto =>
        Math.Round(
            Total / 1.19m,
            0,
            MidpointRounding.AwayFromZero);

    private decimal Iva => Total - Neto;

    public string SubtotalTexto => Formato.Moneda(Subtotal);
    public string DescuentoTexto => Formato.Moneda(Descuento);
    public string NetoTexto => Formato.Moneda(Neto);
    public string IvaTexto => Formato.Moneda(Iva);
    public string TotalTexto => Formato.Moneda(Total);

    public bool HayItems => Carrito.Count > 0;

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
        !string.IsNullOrWhiteSpace(MensajeError);

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
        !string.IsNullOrWhiteSpace(MensajeOk);

    public ICommand ComandoBuscar { get; }
    public ICommand ComandoBuscarCatalogo { get; }
    public ICommand ComandoSeleccionarProducto { get; }
    public ICommand ComandoAgregar { get; }
    public ICommand ComandoQuitar { get; }
    public ICommand ComandoAumentarCantidad { get; }
    public ICommand ComandoReducirCantidad { get; }
    public ICommand ComandoEmitir { get; }
    public ICommand ComandoVerRanking { get; }
    public ICommand ComandoLimpiar { get; }
    public ICommand ComandoPublicoGeneral { get; }

    public PosViewModel(VentasService servicio)
    {
        _servicio = servicio;
        Titulo = "Punto de Venta";

        ComandoBuscar =
            new Command(async () => await BuscarAsync());

        ComandoBuscarCatalogo =
            new Command(async () => await CargarProductosAsync());

        ComandoSeleccionarProducto =
            new Command<ProductoPos>(async producto =>
            {
                if (producto != null)
                    await SeleccionarProductoAsync(producto);
            });

        ComandoAgregar =
            new Command(async () => await AgregarAsync());

        ComandoQuitar =
            new Command<ItemCarrito>(item =>
            {
                if (EstaOcupado || item == null)
                    return;

                Carrito.Remove(item);
                Recalcular();
            });

        ComandoAumentarCantidad =
            new Command<ItemCarrito>(
                async item => await CambiarCantidadAsync(item, 1));

        ComandoReducirCantidad =
            new Command<ItemCarrito>(
                async item => await CambiarCantidadAsync(item, -1));

        ComandoEmitir =
            new Command(async () => await EmitirAsync());

        ComandoVerRanking =
            new Command(async () =>
                await Shell.Current.GoToAsync(
                    "//RankingVentasPage"));

        ComandoLimpiar = new Command(LimpiarVenta);

        ComandoPublicoGeneral = new Command(() =>
        {
            if (!EstaOcupado)
                ClienteSeleccionado = null;
        });
    }

    public async Task CargarAsync()
    {
        if (EstaOcupado)
            return;

        EstaOcupado = true;
        MensajeError = string.Empty;

        try
        {
            var clientes = await _servicio.ObtenerClientesAsync();

            ++_versionCargaConvenios;
            Clientes.Clear();

            foreach (var cliente in clientes)
                Clientes.Add(cliente);

            ClienteSeleccionado = null;
            Convenios.Clear();
            ConvenioSeleccionado = null;
        }
        catch (Exception ex)
        {
            MensajeError =
                "No se pudieron cargar los clientes: " + ex.Message;
        }
        finally
        {
            EstaOcupado = false;
        }

        await CargarProductosAsync();
    }

    public async Task CargarProductosAsync()
    {
        if (EstaOcupado)
            return;

        EstaOcupado = true;

        try
        {
            var productos =
                await _servicio.ObtenerProductosAsync(
                    BusquedaProducto ?? "");

            Productos.Clear();

            foreach (var producto in productos)
                Productos.Add(producto);
        }
        catch (Exception ex)
        {
            MensajeError =
                "No se pudieron cargar los productos: " + ex.Message;
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private async Task BuscarAsync()
    {
        if (EstaOcupado)
            return;

        MensajeError = string.Empty;
        MensajeOk = string.Empty;

        if (string.IsNullOrWhiteSpace(CodigoBuscado))
        {
            MensajeError = "Ingresa un SKU o código de barras.";
            return;
        }

        EstaOcupado = true;

        try
        {
            var producto =
                await _servicio.BuscarProductoAsync(CodigoBuscado);

            if (producto == null)
            {
                Producto = null;
                Lotes.Clear();
                MensajeError =
                    "No se encontró un producto con ese código.";
                return;
            }

            await CargarLotesProductoAsync(producto);
        }
        catch (Exception ex)
        {
            MensajeError = "Error al buscar: " + ex.Message;
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private async Task SeleccionarProductoAsync(ProductoPos producto)
    {
        if (EstaOcupado)
            return;

        MensajeError = string.Empty;
        MensajeOk = string.Empty;
        EstaOcupado = true;

        try
        {
            await CargarLotesProductoAsync(producto);
        }
        catch (Exception ex)
        {
            MensajeError =
                "Error al seleccionar producto: " + ex.Message;
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private async Task CargarLotesProductoAsync(ProductoPos producto)
    {
        Producto = null;
        LoteSeleccionado = null;
        Lotes.Clear();

        var lotes =
            await _servicio.ObtenerLotesAsync(producto.IdProducto);

        if (lotes.Count == 0)
        {
            MensajeError =
                "El producto no tiene lotes disponibles.";
            return;
        }

        Producto = producto;

        foreach (var lote in lotes)
            Lotes.Add(lote);

        LoteSeleccionado = Lotes.FirstOrDefault();
        CantidadTexto = "1";
    }

    private async Task AgregarAsync()
    {
        if (EstaOcupado)
            return;

        MensajeError = string.Empty;
        MensajeOk = string.Empty;

        if (Producto == null || LoteSeleccionado == null)
        {
            MensajeError = "Selecciona un producto y un lote.";
            return;
        }

        if (!int.TryParse(CantidadTexto, out int cantidad)
            || cantidad <= 0)
        {
            MensajeError =
                "La cantidad debe ser un entero mayor a cero.";
            return;
        }

        if (LoteSeleccionado.FechaVencimiento.Date <
            DateTime.Today)
        {
            MensajeError = "No puedes vender un lote vencido.";
            return;
        }

        var existente = Carrito.FirstOrDefault(i =>
            i.IdLote == LoteSeleccionado.IdLote);

        int cantidadAcumulada =
            cantidad + (existente?.Cantidad ?? 0);

        if (cantidadAcumulada > LoteSeleccionado.StockActual)
        {
            MensajeError =
                $"Stock insuficiente. Disponible: " +
                $"{LoteSeleccionado.StockActual} unidades.";
            return;
        }

        if (existente != null)
        {
            int indice = Carrito.IndexOf(existente);

            Carrito[indice] = new ItemCarrito
            {
                IdProducto = existente.IdProducto,
                IdLote = existente.IdLote,
                NombreProducto = existente.NombreProducto,
                NumeroLote = existente.NumeroLote,
                PrecioUnitario = existente.PrecioUnitario,
                Cantidad = cantidadAcumulada
            };
        }
        else
        {
            Carrito.Add(new ItemCarrito
            {
                IdProducto = Producto.IdProducto,
                IdLote = LoteSeleccionado.IdLote,
                NombreProducto = Producto.NombreComercial,
                NumeroLote = LoteSeleccionado.NumeroLote,
                Cantidad = cantidad,
                PrecioUnitario = Producto.PrecioVenta
            });
        }

        Producto = null;
        LoteSeleccionado = null;
        Lotes.Clear();
        CodigoBuscado = string.Empty;
        CantidadTexto = "1";

        Recalcular();

        await Task.CompletedTask;
    }

    private async Task CambiarCantidadAsync(
        ItemCarrito? item,
        int cambio)
    {
        if (EstaOcupado || item == null)
            return;

        MensajeError = string.Empty;
        MensajeOk = string.Empty;

        int indice = Carrito.IndexOf(item);

        if (indice < 0)
            return;

        int nuevaCantidad = item.Cantidad + cambio;

        if (nuevaCantidad <= 0)
        {
            Carrito.RemoveAt(indice);
            Recalcular();
            return;
        }

        if (cambio > 0)
        {
            EstaOcupado = true;

            try
            {
                var lotes =
                    await _servicio.ObtenerLotesAsync(
                        item.IdProducto);

                var lote = lotes.FirstOrDefault(
                    l => l.IdLote == item.IdLote);

                if (lote == null)
                {
                    MensajeError =
                        "El lote ya no tiene stock disponible.";
                    return;
                }

                if (nuevaCantidad > lote.StockActual)
                {
                    MensajeError =
                        $"Stock máximo disponible: " +
                        $"{lote.StockActual} unidades.";
                    return;
                }
            }
            catch (Exception ex)
            {
                MensajeError =
                    "No se pudo verificar el stock: " +
                    ex.Message;
                return;
            }
            finally
            {
                EstaOcupado = false;
            }
        }

        // Buscar nuevamente por si el carrito
        // cambió durante la consulta asíncrona.
        indice = Carrito.IndexOf(item);

        if (indice < 0)
            return;

        Carrito[indice] = new ItemCarrito
        {
            IdProducto = item.IdProducto,
            IdLote = item.IdLote,
            NombreProducto = item.NombreProducto,
            NumeroLote = item.NumeroLote,
            PrecioUnitario = item.PrecioUnitario,
            Cantidad = nuevaCantidad
        };

        Recalcular();
    }

    private async Task CargarConveniosAsync(ClientePos? cliente)
    {
        int version = ++_versionCargaConvenios;

        Convenios.Clear();
        ConvenioSeleccionado = null;

        if (cliente == null)
        {
            Recalcular();
            return;
        }

        try
        {
            var datos =
                await _servicio.ObtenerConveniosAsync(
                    cliente.IdCliente);

            if (version != _versionCargaConvenios ||
                ClienteSeleccionado?.IdCliente != cliente.IdCliente)
                return;

            foreach (var convenio in datos)
                Convenios.Add(convenio);
        }
        catch (Exception ex)
        {
            if (version == _versionCargaConvenios)
            {
                MensajeError =
                    "Error al cargar convenios: " + ex.Message;
            }
        }

        Recalcular();
    }

    private void LimpiarVenta()
    {
        if (EstaOcupado)
            return;

        Carrito.Clear();
        Producto = null;
        LoteSeleccionado = null;
        Lotes.Clear();

        CodigoBuscado = string.Empty;
        CantidadTexto = "1";

        ClienteSeleccionado = null;
        ConvenioSeleccionado = null;

        FormaPagoSeleccionada = "EFECTIVO";
        TipoDescuentoSeleccionado = "Sin descuento";

        MensajeError = string.Empty;
        MensajeOk = string.Empty;

        Recalcular();
    }

    private async Task EmitirAsync()
    {
        if (EstaOcupado)
            return;

        MensajeError = string.Empty;
        MensajeOk = string.Empty;

        if (Carrito.Count == 0)
        {
            MensajeError = "El carrito está vacío.";
            return;
        }

        if (FormaPagoSeleccionada == "CREDITO")
        {
            if (ClienteSeleccionado == null)
            {
                MensajeError =
                    "Para vender a crédito debes seleccionar " +
                    "un cliente registrado.";
                return;
            }

            if (ConvenioSeleccionado == null)
            {
                MensajeError =
                    "Para vender a crédito debes seleccionar " +
                    "un convenio.";
                return;
            }
        }

        int idCajero =
            Sesion.UsuarioActual?.IdUsuario ?? 0;

        if (idCajero <= 0)
        {
            MensajeError =
                "No existe una sesión activa de cajero.";
            return;
        }

        int? idCliente = ClienteSeleccionado?.IdCliente;

        int? idConvenio =
            FormaPagoSeleccionada == "CREDITO"
                ? ConvenioSeleccionado?.IdConvenio
                : null;

        string formaPago = FormaPagoSeleccionada;

        var items = Carrito.Select(i => new ItemCarrito
        {
            IdProducto = i.IdProducto,
            IdLote = i.IdLote,
            NombreProducto = i.NombreProducto,
            NumeroLote = i.NumeroLote,
            Cantidad = i.Cantidad,
            PrecioUnitario = i.PrecioUnitario
        }).ToList();

        decimal subtotal = Subtotal;
        decimal descuento = Descuento;
        decimal neto = Neto;
        decimal iva = Iva;
        decimal total = Total;
        string nombreCliente = NombreClienteActual;
        string motivoDescuento = MotivoDescuento;

        EstaOcupado = true;

        try
        {
            string folio = await _servicio.EmitirVentaAsync(
                idCliente,
                idConvenio,
                formaPago,
                items,
                subtotal,
                descuento,
                neto,
                iva,
                total,
                idCajero);

            // Generar el comprobante solo después de confirmar la venta.
            UltimoComprobante = ComprobantePos.Generar(
                folio, DateTime.Now, nombreCliente, formaPago, items,
                subtotal, descuento, neto, iva, total, motivoDescuento);

            Carrito.Clear();
            Producto = null;
            LoteSeleccionado = null;
            Lotes.Clear();

            CodigoBuscado = string.Empty;
            CantidadTexto = "1";

            ClienteSeleccionado = null;
            ConvenioSeleccionado = null;

            FormaPagoSeleccionada = "EFECTIVO";
            TipoDescuentoSeleccionado = "Sin descuento";

            Recalcular();

            MensajeOk =
                $"Venta {folio} registrada correctamente " +
                $"por {Formato.Moneda(total)}.";

            try
            {
                await Shell.Current.DisplayAlertAsync(
                    "Comprobante de venta", UltimoComprobante, "Cerrar");
            }
            catch
            {
                // El comprobante es secundario: no invalidar una venta guardada.
            }
        }
        catch (Exception ex)
        {
            MensajeError =
                "No se pudo registrar la venta: " + ex.Message;
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private void Recalcular()
    {
        OnPropertyChanged(nameof(SubtotalTexto));
        OnPropertyChanged(nameof(DescuentoTexto));
        OnPropertyChanged(nameof(NetoTexto));
        OnPropertyChanged(nameof(IvaTexto));
        OnPropertyChanged(nameof(TotalTexto));
        OnPropertyChanged(nameof(HayItems));
        OnPropertyChanged(nameof(PorcentajeDescuentoTexto));
        OnPropertyChanged(nameof(MotivoDescuento));
    }
}
