
using TISW_TUSSI_ERP.ViewModels.Ventas;

namespace TISW_TUSSI_ERP.Views.Ventas;

public partial class ConveniosPage : ContentPage
{
    private readonly ConveniosViewModel _viewModel;

    public ConveniosPage(ConveniosViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    // Recibe el cliente desde ClientesPage
    public async Task CargarClienteAsync(
        int idCliente,
        string nombreCliente)
    {
        await _viewModel.InicializarAsync(
            idCliente,
            nombreCliente);
    }

    private void OnNuevoConvenioClicked(
        object sender, EventArgs e)
    {
        _viewModel.NuevoConvenio();
    }

    private async void OnEliminarConvenioClicked(
        object sender, EventArgs e)
    {
        if (_viewModel.ConvenioSeleccionado == null)
        {
            await DisplayAlertAsync(
                "Atención",
                "Selecciona un convenio registrado.",
                "Aceptar");
            return;
        }

        bool confirmar = await DisplayAlertAsync(
            "Eliminar convenio",
            "¿Seguro que deseas eliminar este convenio?",
            "Eliminar",
            "Cancelar");

        if (confirmar)
            await _viewModel.EliminarConvenioAsync();
    }

    private async void OnVolverClicked(
        object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//ClientesPage");
    }
}
