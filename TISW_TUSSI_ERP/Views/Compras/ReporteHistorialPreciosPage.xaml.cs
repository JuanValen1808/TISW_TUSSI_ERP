using TISW_TUSSI_ERP.ViewModels.Compras;

namespace TISW_TUSSI_ERP.Views.Compras;

public partial class ReporteHistorialPreciosPage : ContentPage
{
    private readonly ReporteHistorialPreciosViewModel _viewModel;

    public ReporteHistorialPreciosPage(ReporteHistorialPreciosViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await _viewModel.CargarInicialAsync();
        }
        catch
        {
            // Evita excepciones no controladas en ciclo de vida de la vista
        }
    }
}

