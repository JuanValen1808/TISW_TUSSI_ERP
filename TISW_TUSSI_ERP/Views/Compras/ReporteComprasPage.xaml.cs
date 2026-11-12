using TISW_TUSSI_ERP.ViewModels.Compras;

namespace TISW_TUSSI_ERP.Views.Compras;

public partial class ReporteComprasPage : ContentPage
{
    private readonly ReporteComprasViewModel _vm;

    public ReporteComprasPage(ReporteComprasViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _vm = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await _vm.GenerarAsync();
        }
        catch
        {
            // Evita excepciones no controladas en ciclo de vida de la vista
        }
    }
}
