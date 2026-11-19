using TISW_TUSSI_ERP.Models.Inventario;
using TISW_TUSSI_ERP.ViewModels.Inventario;

namespace TISW_TUSSI_ERP.Views.Inventario;

public partial class NuevoProductoPage : ContentPage
{
    public NuevoProductoPage(Action<ProductoModel?> onCreado)
    {
        InitializeComponent();
        var vm = new NuevoProductoViewModel();
        vm.ProductoCreado += onCreado;
        BindingContext = vm;
    }
}