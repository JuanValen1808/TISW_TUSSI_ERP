using TISW_TUSSI_ERP.ViewModels.Compras;

namespace TISW_TUSSI_ERP.Views.Compras;

public partial class OrdenesCompraPage : ContentPage
{
    private readonly OrdenesCompraViewModel _vm;

    public OrdenesCompraPage(OrdenesCompraViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _vm = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await _vm.CargarAsync();
        }
        catch
        {
            // Evita excepciones no controladas en ciclo de vida de la vista
        }
    }
}
