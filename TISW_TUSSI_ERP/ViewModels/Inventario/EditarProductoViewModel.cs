using System.Windows.Input;
using Microsoft.Maui.Controls;
using TISW_TUSSI_ERP.Models.Inventario;

namespace TISW_TUSSI_ERP.ViewModels.Inventario;

public class EditarProductoViewModel : BindableObject
{
    private readonly ProductoModel _productoOriginal;
    public event Action<ProductoModel?>? ProductoActualizado;

    public string Sku { get; set; }
    public string CodigoBarras { get; set; }
    public string NombreComercial { get; set; }
    public string PrincipioActivo { get; set; }
    public string RegistroSanitario { get; set; }
    public string FormatoPresentacion { get; set; }
    public decimal CostoPromedio { get; set; }
    public decimal PrecioVenta { get; set; }
    public int StockMinimo { get; set; }
    public int PuntoReorden { get; set; }

    public ICommand ComandoGuardar { get; }
    public ICommand ComandoCerrar { get; }

    public EditarProductoViewModel(ProductoModel producto)
    {
        _productoOriginal = producto;

        // Copiar valores actuales
        Sku = producto.Sku;
        CodigoBarras = producto.CodigoBarras;
        NombreComercial = producto.NombreComercial;
        PrincipioActivo = producto.PrincipioActivo;
        RegistroSanitario = producto.RegistroSanitario;
        FormatoPresentacion = producto.FormatoPresentacion;
        CostoPromedio = producto.CostoPromedio;
        PrecioVenta = producto.PrecioVenta;
        StockMinimo = producto.StockMinimo;
        PuntoReorden = producto.PuntoReorden;

        ComandoGuardar = new Command(async () => await GuardarAsync());
        ComandoCerrar = new Command(async () => await CerrarModalAsync(null));
    }

    private async Task GuardarAsync()
    {
        if (string.IsNullOrWhiteSpace(Sku) || string.IsNullOrWhiteSpace(NombreComercial) || string.IsNullOrWhiteSpace(RegistroSanitario))
        {
            await Shell.Current.DisplayAlert("Atención", "Los campos SKU, Nombre Comercial y Registro Sanitario son obligatorios.", "OK");
            return;
        }

        if (PrecioVenta <= 0 || CostoPromedio <= 0)
        {
            await Shell.Current.DisplayAlert("Atención", "El costo y el precio estimado de venta deben ser mayores a cero.", "OK");
            return;
        }

        // Mapeo completo y explícito al objeto original
        _productoOriginal.Sku = Sku.Trim();
        _productoOriginal.CodigoBarras = string.IsNullOrWhiteSpace(CodigoBarras) ? string.Empty : CodigoBarras.Trim();
        _productoOriginal.NombreComercial = NombreComercial.Trim();
        _productoOriginal.PrincipioActivo = string.IsNullOrWhiteSpace(PrincipioActivo) ? string.Empty : PrincipioActivo.Trim();
        _productoOriginal.RegistroSanitario = RegistroSanitario.Trim();
        _productoOriginal.FormatoPresentacion = string.IsNullOrWhiteSpace(FormatoPresentacion) ? string.Empty : FormatoPresentacion.Trim();
        _productoOriginal.CostoPromedio = CostoPromedio;
        _productoOriginal.PrecioVenta = PrecioVenta;
        _productoOriginal.StockMinimo = StockMinimo;
        _productoOriginal.PuntoReorden = PuntoReorden;

        await CerrarModalAsync(_productoOriginal);
    }

    private async Task CerrarModalAsync(ProductoModel? resultado)
    {
        ProductoActualizado?.Invoke(resultado);
        await Shell.Current.Navigation.PopModalAsync();
    }
}