using System.Windows.Input;
using Microsoft.Maui.Controls;
using TISW_TUSSI_ERP.Models.Inventario;

namespace TISW_TUSSI_ERP.ViewModels.Inventario;

public class NuevoProductoViewModel : BindableObject
{
    public event Action<ProductoModel?>? ProductoCreado;

    public string Sku { get; set; } = string.Empty;
    public string CodigoBarras { get; set; } = string.Empty;
    public string NombreComercial { get; set; } = string.Empty;
    public string PrincipioActivo { get; set; } = string.Empty;
    public string RegistroSanitario { get; set; } = string.Empty;
    public string FormatoPresentacion { get; set; } = string.Empty;
    public decimal CostoPromedio { get; set; }
    public decimal PrecioVenta { get; set; }
    public int StockMinimo { get; set; } = 10;
    public int PuntoReorden { get; set; } = 20;

    public ICommand ComandoGuardar { get; }
    public ICommand ComandoCerrar { get; }

    public NuevoProductoViewModel()
    {
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

        var nuevo = new ProductoModel
        {
            IdProducto = 0,
            Sku = Sku.Trim(),
            CodigoBarras = CodigoBarras?.Trim() ?? string.Empty,
            NombreComercial = NombreComercial.Trim(),
            PrincipioActivo = PrincipioActivo?.Trim() ?? string.Empty,
            RegistroSanitario = RegistroSanitario.Trim(),
            FormatoPresentacion = FormatoPresentacion?.Trim() ?? string.Empty,
            IdCategoria = 1, // Categoría por defecto
            CostoPromedio = CostoPromedio,
            PrecioVenta = PrecioVenta,
            StockMinimo = StockMinimo,
            PuntoReorden = PuntoReorden,
            Activo = true
        };

        await CerrarModalAsync(nuevo);
    }

    private async Task CerrarModalAsync(ProductoModel? resultado)
    {
        ProductoCreado?.Invoke(resultado);
        await Shell.Current.Navigation.PopModalAsync();
    }
}