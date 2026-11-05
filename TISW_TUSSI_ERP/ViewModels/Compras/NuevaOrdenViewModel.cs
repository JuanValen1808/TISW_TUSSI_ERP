using System.Collections.ObjectModel;
using System.Windows.Input;
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
    public Proveedor? ProveedorSeleccionado { get => _proveedorSeleccionado; set => SetProperty(ref _proveedorSeleccionado, value); }

    private ProductoSelector? _productoSeleccionado;
    public ProductoSelector? ProductoSeleccionado { get => _productoSeleccionado; set => SetProperty(ref _productoSeleccionado, value); }

    private string _cantidadFiltro = "1";
    public string CantidadFiltro { get => _cantidadFiltro; set => SetProperty(ref _cantidadFiltro, value); }

    private decimal _subtotal;
    public decimal Subtotal { get => _subtotal; private set => SetProperty(ref _subtotal, value); }

    private decimal _iva;
    public decimal Iva { get => _iva; private set => SetProperty(ref _iva, value); }

    private decimal _total;
    public decimal Total { get => _total; private set => SetProperty(ref _total, value); }

    public ICommand ComandoCargarDatos { get; }
    public ICommand ComandoAgregarProducto { get; }
    public ICommand ComandoEliminarProducto { get; }
    public ICommand ComandoGuardarOrden { get; }

    public NuevaOrdenViewModel(ComprasService servicio)
    {
        _servicio = servicio;
        Titulo = "Nueva Orden de Compra";

        ComandoCargarDatos = new Command(async () => await CargarDatosAsync());
        ComandoAgregarProducto = new Command(AgregarAlCarrito);
        ComandoEliminarProducto = new Command<CarritoItem>(EliminarDelCarrito);
        ComandoGuardarOrden = new Command(async () => await GuardarOrdenAsync());
    }

    private async Task CargarDatosAsync()
    {
        if (EstaOcupado) return;
        EstaOcupado = true;
        try
        {
            var provs = await _servicio.ObtenerProveedoresAsync();
            Proveedores.Clear();
            foreach (var p in provs) Proveedores.Add(p);

            var prods = await _servicio.ObtenerProductosParaCompraAsync();
            Productos.Clear();
            foreach (var p in prods) Productos.Add(p);
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private void AgregarAlCarrito()
    {
        if (ProductoSeleccionado == null)
        {
            Application.Current!.MainPage!.DisplayAlert("Atención", "Debes seleccionar un producto.", "OK");
            return;
        }

        if (!int.TryParse(CantidadFiltro, out int cantidad) || cantidad <= 0)
        {
            Application.Current!.MainPage!.DisplayAlert("Atención", "La cantidad debe ser un número mayor a cero.", "OK");
            return;
        }

        var item = new CarritoItem
        {
            IdProducto = ProductoSeleccionado.Id,
            NombreProducto = ProductoSeleccionado.Nombre,
            Cantidad = cantidad,
            PrecioUnitario = ProductoSeleccionado.Costo
        };

        Carrito.Add(item);
        CalcularTotales();

        // Limpiar selección
        ProductoSeleccionado = null;
        CantidadFiltro = "1";
    }

    private void EliminarDelCarrito(CarritoItem item)
    {
        if (item != null)
        {
            Carrito.Remove(item);
            CalcularTotales();
        }
    }

    private void CalcularTotales()
    {
        Subtotal = Carrito.Sum(x => x.Subtotal);
        Iva = Subtotal * 0.19m; // 19% IVA en Chile
        Total = Subtotal + Iva;
    }

    private async Task GuardarOrdenAsync()
    {
        if (ProveedorSeleccionado == null)
        {
            await Application.Current!.MainPage!.DisplayAlert("Error", "Debes seleccionar un proveedor.", "OK");
            return;
        }

        if (Carrito.Count == 0)
        {
            await Application.Current!.MainPage!.DisplayAlert("Error", "El carrito está vacío.", "OK");
            return;
        }

        EstaOcupado = true;
        try
        {
            int idUsuarioLogueado = 1; // Asumimos Administrador por ahora

            await _servicio.CrearOrdenCompraAsync(
                ProveedorSeleccionado.Id,
                idUsuarioLogueado,
                Subtotal,
                Iva,
                Total,
                Carrito.ToList());

            await Application.Current!.MainPage!.DisplayAlert("Éxito", "Orden de compra generada correctamente.", "OK");
            await Shell.Current.GoToAsync(".."); // Regresa a la pantalla anterior
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert("Error al Guardar", ex.Message, "OK");
        }
        finally
        {
            EstaOcupado = false;
        }
    }
}