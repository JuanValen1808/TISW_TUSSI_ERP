using System.Collections.ObjectModel;
using System.Windows.Input;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Auth;
using TISW_TUSSI_ERP.Models.Compras;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Compras;

// Pantalla 3: Recepción y Liquidación de Factura (RF-COM-03 & RF-COM-09)
public class RecepcionFacturasViewModel : BaseViewModel
{
    private readonly ComprasService _servicio;

    public ObservableCollection<OrdenCompra> OrdenesDisponibles { get; } = new();
    public ObservableCollection<LineaRecepcionFacturaItem> Lineas { get; } = new();

    private OrdenCompra? _ordenSeleccionada;
    public OrdenCompra? OrdenSeleccionada
    {
        get => _ordenSeleccionada;
        set
        {
            if (SetProperty(ref _ordenSeleccionada, value))
            {
                OnPropertyChanged(nameof(HayOrdenSeleccionada));
                OnPropertyChanged(nameof(NoHayOrdenSeleccionada));
                _ = CargarLineasAsync(value);
            }
        }
    }
    public bool HayOrdenSeleccionada => OrdenSeleccionada is not null;
    public bool NoHayOrdenSeleccionada => OrdenSeleccionada is null;

    private string _numeroFactura = $"FAC-{DateTime.Now.Year}-{new Random().Next(1000, 9999)}";
    public string NumeroFactura { get => _numeroFactura; set => SetProperty(ref _numeroFactura, value); }

    private DateTime _fechaFactura = DateTime.Today;
    public DateTime FechaFactura { get => _fechaFactura; set => SetProperty(ref _fechaFactura, value); }

    private decimal _montoTotalFacturado;
    public decimal MontoTotalFacturado
    {
        get => _montoTotalFacturado;
        private set
        {
            if (SetProperty(ref _montoTotalFacturado, value))
            {
                OnPropertyChanged(nameof(MontoTotalFacturadoTexto));
                OnPropertyChanged(nameof(MontoNetoTexto));
                OnPropertyChanged(nameof(MontoIvaTexto));
            }
        }
    }

    public string MontoTotalFacturadoTexto => Formato.Moneda(MontoTotalFacturado);
    public string MontoNetoTexto => Formato.Moneda(Math.Round(MontoTotalFacturado / 1.19m, 0));
    public string MontoIvaTexto => Formato.Moneda(MontoTotalFacturado - Math.Round(MontoTotalFacturado / 1.19m, 0));

    private string _mensajeError = string.Empty;
    public string MensajeError
    {
        get => _mensajeError;
        private set { if (SetProperty(ref _mensajeError, value)) OnPropertyChanged(nameof(HayError)); }
    }
    public bool HayError => !string.IsNullOrEmpty(MensajeError);

    public ICommand ComandoCargarOrdenes { get; }
    public ICommand ComandoLiquidarRecepcion { get; }
    public ICommand ComandoRecalcularTotal { get; }

    public RecepcionFacturasViewModel(ComprasService servicio)
    {
        _servicio = servicio;
        Titulo = "Recepción y Liquidación de Factura";

        ComandoCargarOrdenes = new Command(async () => await CargarOrdenesDisponiblesAsync());
        ComandoLiquidarRecepcion = new Command(async () => await LiquidarRecepcionAsync());
        ComandoRecalcularTotal = new Command(RecalcularTotal);
    }

    public async Task CargarOrdenesDisponiblesAsync(int? idPreseleccion = null)
    {
        if (EstaOcupado) return;
        EstaOcupado = true;
        MensajeError = string.Empty;

        try
        {
            var ordenes = await _servicio.ObtenerOrdenesParaRecepcionAsync();
            OrdenesDisponibles.Clear();
            foreach (var o in ordenes)
            {
                OrdenesDisponibles.Add(o);
            }

            if (idPreseleccion.HasValue)
            {
                OrdenSeleccionada = OrdenesDisponibles.FirstOrDefault(o => o.Id == idPreseleccion.Value)
                                    ?? OrdenesDisponibles.FirstOrDefault();
            }
            else if (OrdenSeleccionada == null && OrdenesDisponibles.Count > 0)
            {
                OrdenSeleccionada = OrdenesDisponibles.FirstOrDefault();
            }
        }
        catch (Exception ex)
        {
            MensajeError = "No se pudieron obtener las órdenes de compra: " + ex.Message;
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private async Task CargarLineasAsync(OrdenCompra? orden)
    {
        Lineas.Clear();
        MontoTotalFacturado = 0;
        if (orden is null) return;

        try
        {
            var items = await _servicio.ObtenerLineasParaRecepcionAsync(orden.Id);
            foreach (var item in items)
            {
                item.PropertyChanged += (_, _) => RecalcularTotal();
                Lineas.Add(item);
            }

            RecalcularTotal();
        }
        catch (Exception ex)
        {
            MensajeError = "Error al cargar los ítems de la orden: " + ex.Message;
        }
    }

    public void RecalcularTotal()
    {
        MontoTotalFacturado = Lineas.Sum(l => l.SubtotalLinea);
    }

    private async Task LiquidarRecepcionAsync()
    {
        // ==========================================================
        // VALIDACIONES DE OPERACIÓN DEL SISTEMA
        // ==========================================================
        if (OrdenSeleccionada is null)
        {
            await Shell.Current.DisplayAlert("Validación", "Debe seleccionar una orden de compra pendiente de recepción.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(NumeroFactura))
        {
            await Shell.Current.DisplayAlert("Validación", "El número de factura del proveedor es obligatorio.", "OK");
            return;
        }

        var lineasARecibir = Lineas.Where(l => l.CantidadARecibir > 0).ToList();
        if (lineasARecibir.Count == 0)
        {
            await Shell.Current.DisplayAlert("Validación", "Debe ingresar una cantidad a recepcionar mayor a 0 en al menos un medicamento.", "OK");
            return;
        }

        foreach (var l in lineasARecibir)
        {
            if (string.IsNullOrWhiteSpace(l.NumeroLote))
            {
                await Shell.Current.DisplayAlert("Validación Sanitaria", $"El producto '{l.NombreProducto}' requiere un número de lote para cumplir con la trazabilidad normativa del ISP.", "OK");
                return;
            }

            if (l.FechaVencimiento.Date <= DateTime.Today)
            {
                await Shell.Current.DisplayAlert("Validación Sanitaria", $"La fecha de vencimiento del lote '{l.NumeroLote}' ({l.NombreProducto}) no puede ser anterior o igual a la fecha de hoy.", "OK");
                return;
            }

            if (l.CantidadARecibir > l.CantidadPendiente)
            {
                await Shell.Current.DisplayAlert("Validación Operativa", $"La cantidad a ingresar ({l.CantidadARecibir}) para '{l.NombreProducto}' supera la cantidad pendiente pactada ({l.CantidadPendiente}).", "OK");
                return;
            }

            if (l.CostoFacturado <= 0)
            {
                await Shell.Current.DisplayAlert("Validación Operativa", $"El costo unitario facturado para '{l.NombreProducto}' debe ser mayor a $0.", "OK");
                return;
            }
        }

        var confirmar = await Shell.Current.DisplayAlert(
            "Liquidar Factura de Compra",
            $"¿Confirmar la recepción de {lineasARecibir.Count} medicamento(s) por un total de {MontoTotalFacturadoTexto} bajo la Factura {NumeroFactura}? Esta acción actualizará el stock físico, inyectará los movimientos en Kardex y creará la cuenta por pagar en contabilidad.",
            "Confirmar y Liquidar", "Cancelar");

        if (!confirmar) return;

        EstaOcupado = true;
        try
        {
            var input = new RecepcionFacturaInput
            {
                IdOrdenCompra = OrdenSeleccionada.Id,
                IdProveedor = OrdenSeleccionada.IdProveedor,
                NumeroFactura = NumeroFactura.Trim(),
                FechaFactura = FechaFactura,
                MontoTotalFacturado = MontoTotalFacturado,
                Lineas = lineasARecibir
            };

            int idUsuario = Sesion.UsuarioActual?.IdUsuario ?? 1;
            var (idRecepcion, numFact) = await _servicio.RegistrarRecepcionFacturaAsync(input, idUsuario);

            await Shell.Current.DisplayAlert(
                "Factura Liquidada Exitosamente",
                $"Recepción N° {idRecepcion} procesada correctamente para la Factura {numFact}.\n\n" +
                "✓ Stock de inventario actualizado en lotes.\n" +
                "✓ Entradas registradas cronológicamente en Kardex.\n" +
                "✓ Estado de Orden de Compra actualizado.\n" +
                "✓ Cuenta por pagar creada y asiento contable enlazado.",
                "Entendido");

            // Refrescar lista de órdenes disponibles
            await CargarOrdenesDisponiblesAsync();
            NumeroFactura = $"FAC-{DateTime.Now.Year}-{new Random().Next(1000, 9999)}";
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error en la liquidación", ex.Message, "OK");
        }
        finally
        {
            EstaOcupado = false;
        }
    }
}

