using TISW_TUSSI_ERP.Models.Inventario;
using TISW_TUSSI_ERP.ViewModels.Inventario;

namespace TISW_TUSSI_ERP.Views.Inventario;

public partial class EditarProductoPage : ContentPage
{
    public EditarProductoPage(ProductoModel producto, Action<ProductoModel?> onActualizado)
    {
        InitializeComponent();
        var vm = new EditarProductoViewModel(producto);
        vm.ProductoActualizado += onActualizado;
        BindingContext = vm;
    }
}