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
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Ejecutamos el comando que va a la base de datos a traer productos y proveedores
        if (_viewModel.ComandoCargarDatos.CanExecute(null))
        {
            _viewModel.ComandoCargarDatos.Execute(null);
        }
    }
}