using TISW_TUSSI_ERP.ViewModels.Compras;

namespace TISW_TUSSI_ERP.Views.Compras;

public partial class NuevaOrdenPage : ContentPage
{
    private readonly NuevaOrdenViewModel _viewModel;

    public NuevaOrdenPage(NuevaOrdenViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    // Este método se ejecuta automáticamente justo antes de que la página se muestre al usuario
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await _viewModel.CargarDatosAsync();
        }
        catch
        {
            // Evita excepciones no controladas en ciclo de vida de la vista
        }
    }
}