using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Auth;
using TISW_TUSSI_ERP.Models.Compras;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Compras;

public class NuevaOrdenViewModel : BaseViewModel
{
    private readonly ComprasService _servicio;

    public ObservableCollection<Proveedor> Proveedores { get; } = new();
    public ObservableCollection<ProductoSelector> Productos { get; } = new();
    public ObservableCollection<CarritoItem> Carrito { get; } = new();

    private Proveedor? _proveedorSeleccionado;
    public Proveedor? ProveedorSeleccionado
    {
        get => _proveedorSeleccionado;
        set => SetProperty(ref _proveedorSeleccionado, value);
    }

    private ProductoSelector? _productoSeleccionado;
    public ProductoSelector? ProductoSeleccionado
    {
        get => _productoSeleccionado;
        set
        {
            if (SetProperty(ref _productoSeleccionado, value))
            {
                if (value != null)
                {
                    CostoPactadoTexto = value.Costo.ToString("0");
                }
            }
        }
    }

    private string _cantidadFiltro = "10";
    public string CantidadFiltro { get => _cantidadFiltro; set => SetProperty(ref _cantidadFiltro, value); }

    private string _costoPactadoTexto = "0";
    public string CostoPactadoTexto { get => _costoPactadoTexto; set => SetProperty(ref _costoPactadoTexto, value); }

    private decimal _subtotal;
    public decimal Subtotal { get => _subtotal; private set => SetProperty(ref _subtotal, value); }

    private decimal _iva;
    public decimal Iva { get => _iva; private set => SetProperty(ref _iva, value); }

    private decimal _total;
    public decimal Total { get => _total; private set => SetProperty(ref _total, value); }

    public string SubtotalTexto => Formato.Moneda(Subtotal);
    public string IvaTexto => Formato.Moneda(Iva);
    public string TotalTexto => Formato.Moneda(Total);

    public bool SuperaLimitePresupuestario => Total > ComprasService.LimiteGastoAutorizacion;
    public bool NoEstaOcupado => !EstaOcupado;

    public ICommand ComandoCargarDatos { get; }
    public ICommand ComandoAgregarProducto { get; }
    public ICommand ComandoEliminarProducto { get; }
    public ICommand ComandoGuardarOrden { get; }
    public ICommand ComandoCargarSugerencias { get; }
    public ICommand ComandoVolver { get; }

    public NuevaOrdenViewModel(ComprasService servicio)
    {
        _servicio = servicio;
        Titulo = "Nueva Orden de Compra";

        ComandoCargarDatos = new Command(async () => await CargarDatosAsync(), () => NoEstaOcupado);
        ComandoAgregarProducto = new Command(async () => await AgregarAlCarritoAsync(), () => NoEstaOcupado);
        ComandoEliminarProducto = new Command<CarritoItem>(EliminarDelCarrito, _ => NoEstaOcupado);
        ComandoGuardarOrden = new Command(async () => await GuardarOrdenAsync(), () => NoEstaOcupado);
        ComandoCargarSugerencias = new Command(async () => await CargarSugerenciasReabastecimientoAsync(), () => NoEstaOcupado);
        ComandoVolver = new Command(async () => await VolverAsync(), () => NoEstaOcupado);

        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(EstaOcupado))
            {
                OnPropertyChanged(nameof(NoEstaOcupado));
                (ComandoCargarDatos as Command)?.ChangeCanExecute();
                (ComandoAgregarProducto as Command)?.ChangeCanExecute();
                (ComandoEliminarProducto as Command)?.ChangeCanExecute();
                (ComandoGuardarOrden as Command)?.ChangeCanExecute();
                (ComandoCargarSugerencias as Command)?.ChangeCanExecute();
                (ComandoVolver as Command)?.ChangeCanExecute();
            }
        };
    }

    public async Task CargarDatosAsync()
    {
        if (EstaOcupado) return;
        EstaOcupado = true;
        try
        {
            int? idProvActual = ProveedorSeleccionado?.Id;
            var provs = await _servicio.ObtenerProveedoresAsync();
            Proveedores.Clear();
            foreach (var p in provs) Proveedores.Add(p);

            var prods = await _servicio.ObtenerProductosParaCompraAsync();
            Productos.Clear();
            foreach (var p in prods) Productos.Add(p);

            if (Proveedores.Count > 0)
            {
                ProveedorSeleccionado = (idProvActual.HasValue
                    ? Proveedores.FirstOrDefault(p => p.Id == idProvActual.Value)
                    : null) ?? Proveedores.FirstOrDefault();
            }
            else
            {
                ProveedorSeleccionado = null;
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private async Task VolverAsync()
    {
        if (EstaOcupado) return;
        EstaOcupado = true;
        try
        {
            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private async Task AgregarAlCarritoAsync()
    {
        if (EstaOcupado) return;

        // Validaciones de operación del sistema
        if (ProductoSeleccionado == null)
        {
            await Shell.Current.DisplayAlert("Atención", "Debes seleccionar un producto del catálogo.", "OK");
            return;
        }

        if (!int.TryParse(CantidadFiltro?.Trim(), NumberStyles.Integer, Formato.Cultura, out int cantidad) || cantidad <= 0)
        {
            await Shell.Current.DisplayAlert("Atención", "La cantidad solicitada debe ser un número entero mayor a 0.", "OK");
            return;
        }

        if (!decimal.TryParse(CostoPactadoTexto?.Trim(), NumberStyles.Number, Formato.Cultura, out decimal costo) || costo <= 0)
        {
            await Shell.Current.DisplayAlert("Atención", "El costo unitario pactado debe ser mayor a $0.", "OK");
            return;
        }

        // Si ya existe en el carrito se actualiza
        var existente = Carrito.FirstOrDefault(c => c.IdProducto == ProductoSeleccionado.Id);
        if (existente != null)
        {
            existente.Cantidad += cantidad;
            existente.PrecioUnitario = costo;
            // Refrescar lista
            var index = Carrito.IndexOf(existente);
            Carrito.RemoveAt(index);
            Carrito.Insert(index, existente);
        }
        else
        {
            Carrito.Add(new CarritoItem
            {
                IdProducto = ProductoSeleccionado.Id,
                Sku = ProductoSeleccionado.Sku,
                NombreProducto = ProductoSeleccionado.Nombre,
                Cantidad = cantidad,
                PrecioUnitario = costo
            });
        }

        CalcularTotales();
        ProductoSeleccionado = null;
        CantidadFiltro = "10";
        CostoPactadoTexto = "0";
    }

    private void EliminarDelCarrito(CarritoItem? item)
    {
        if (EstaOcupado || item is null) return;
        Carrito.Remove(item);
        CalcularTotales();
    }

    private void CalcularTotales()
    {
        Subtotal = Carrito.Sum(item => item.Subtotal);
        Iva = Math.Round(Subtotal * 0.19m, 2);
        Total = Subtotal + Iva;

        OnPropertyChanged(nameof(SubtotalTexto));
        OnPropertyChanged(nameof(IvaTexto));
        OnPropertyChanged(nameof(TotalTexto));
        OnPropertyChanged(nameof(SuperaLimitePresupuestario));
    }

    // Sugerencia automatizada de reabastecimiento
    private async Task CargarSugerenciasReabastecimientoAsync()
    {
        if (EstaOcupado) return;
        EstaOcupado = true;
        try
        {
            var sugerencias = await _servicio.ObtenerSugerenciasReabastecimientoAsync();
            if (sugerencias.Count == 0)
            {
                await Shell.Current.DisplayAlert("Sugerencias de Reabastecimiento", "Todos los productos del inventario se encuentran actualmente por encima de su punto de reorden.", "OK");
                return;
            }

            int agregados = 0;
            foreach (var s in sugerencias)
            {
                var enCarrito = Carrito.FirstOrDefault(c => c.IdProducto == s.IdProducto);
                if (enCarrito == null)
                {
                    Carrito.Add(new CarritoItem
                    {
                        IdProducto = s.IdProducto,
                        Sku = s.Sku,
                        NombreProducto = s.NombreProducto,
                        Cantidad = s.CantidadSugerida,
                        PrecioUnitario = s.CostoEstimado > 0 ? s.CostoEstimado : 1000m
                    });
                    agregados++;
                }
            }

            CalcularTotales();
            await Shell.Current.DisplayAlert("Sugerencias Cargadas", $"Se precargaron {agregados} fármacos con stock crítico o debajo del punto de reorden conforme al cálculo automatizado de demanda.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private async Task GuardarOrdenAsync()
    {
        if (EstaOcupado) return;
        EstaOcupado = true;

        try
        {
            // Validaciones de operación
            if (ProveedorSeleccionado == null)
            {
                await Shell.Current.DisplayAlert("Atención", "Debes seleccionar un proveedor.", "OK");
                return;
            }

            if (Carrito.Count == 0)
            {
                await Shell.Current.DisplayAlert("Atención", "El pedido no contiene ningún producto. Agrega al menos un ítem.", "OK");
                return;
            }

            int idUsuario = Sesion.UsuarioActual?.IdUsuario ?? 1;
            bool esAdminOGerente = Sesion.UsuarioActual?.IdRol == 1 || Sesion.UsuarioActual?.IdRol == 10;

            string proveedorNombre = ProveedorSeleccionado.RazonSocial;
            string totalMostrado = TotalTexto;

            var (idOrden, numeroOc, estado) = await _servicio.CrearOrdenCompraAsync(
                ProveedorSeleccionado.Id,
                idUsuario,
                esAdminOGerente,
                Subtotal,
                Iva,
                Total,
                Carrito.ToList());

            _servicio.UltimaOrdenCreadaId = idOrden;

            // Limpiar formulario de inmediato tras confirmar persistencia
            Carrito.Clear();
            ProductoSeleccionado = null;
            CantidadFiltro = "10";
            CostoPactadoTexto = "0";
            CalcularTotales();

            if (estado == "PENDIENTE_AUTORIZACION")
            {
                await Shell.Current.DisplayAlert(
                    "Orden Pendiente de Autorización",
                    $"La orden {numeroOc} por {totalMostrado} excede el límite de gasto permitido ($1.000.000) y fue enviada a Aprobación Jerárquica.",
                    "Aceptar");
            }
            else
            {
                await Shell.Current.DisplayAlert(
                    "Orden Emitida con Éxito",
                    $"La orden {numeroOc} por {totalMostrado} fue registrada y emitida exitosamente al proveedor {proveedorNombre}.",
                    "OK");
            }

            // Volver al listado (que recargará automáticamente con la nueva orden)
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error al guardar orden", ex.Message, "OK");
        }
        finally
        {
            EstaOcupado = false;
        }
    }
}
