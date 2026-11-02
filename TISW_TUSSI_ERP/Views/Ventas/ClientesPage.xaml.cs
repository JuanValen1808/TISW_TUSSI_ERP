
using TISW_TUSSI_ERP.Models.Ventas;
using TISW_TUSSI_ERP.ViewModels.Ventas;

namespace TISW_TUSSI_ERP.Views.Ventas;

public partial class ClientesPage : ContentPage
{
    private readonly ClientesViewModel _viewModel;

    public ClientesPage(ClientesViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await _viewModel.CargarClientesAsync();
    }

    private void OnNuevoClienteClicked(
        object sender, EventArgs e)
    {
        PanelTipos.IsVisible = false;
        _viewModel.NuevoCliente();
    }

    // Mostrar u ocultar las opciones de tipo de cliente
    private void OnMostrarTiposClicked(
        object sender, TappedEventArgs e)
    {
        if (_viewModel.ClienteFormulario == null)
        {
            _viewModel.Mensaje =
                "Primero selecciona un cliente o pulsa Nuevo cliente.";
            return;
        }

        PanelTipos.IsVisible = !PanelTipos.IsVisible;
    }

    // Seleccionar el tipo sin perder los datos del formulario
    private void SeleccionarTipo(string tipo)
    {
        var actual = _viewModel.ClienteFormulario;

        if (actual == null)
            return;

        _viewModel.ClienteFormulario = new Cliente
        {
            IdCliente = actual.IdCliente,
            RutCliente = actual.RutCliente,
            RazonSocialNombre = actual.RazonSocialNombre,
            Email = actual.Email,
            Telefono = actual.Telefono,
            TipoCliente = tipo
        };

        PanelTipos.IsVisible = false;
    }

    private void OnPersonaClicked(
        object sender, EventArgs e)
    {
        SeleccionarTipo("PERSONA");
    }

    private void OnEmpresaClicked(
        object sender, EventArgs e)
    {
        SeleccionarTipo("EMPRESA");
    }

    private void OnInstitucionClicked(
        object sender, EventArgs e)
    {
        SeleccionarTipo("INSTITUCION");
    }

    // Eliminar cliente con confirmación
    private async void OnEliminarClienteClicked(
        object sender, EventArgs e)
    {
        if (_viewModel.ClienteSeleccionado == null ||
            _viewModel.ClienteSeleccionado.IdCliente <= 0)
        {
            await DisplayAlertAsync(
                "Atención",
                "Selecciona un cliente registrado para eliminar.",
                "Aceptar");
            return;
        }

        bool confirmar = await DisplayAlertAsync(
            "Confirmar eliminación",
            "¿Seguro que deseas eliminar este cliente?",
            "Eliminar",
            "Cancelar");

        if (confirmar)
        {
            PanelTipos.IsVisible = false;
            await _viewModel.EliminarClienteAsync();
        }
    }

    // Abrir los convenios del cliente seleccionado
    private async void OnVerConveniosClicked(
        object sender, EventArgs e)
    {
        var cliente = _viewModel.ClienteSeleccionado;

        if (cliente == null || cliente.IdCliente <= 0)
        {
            await DisplayAlertAsync(
                "Seleccionar cliente",
                "Primero selecciona un cliente registrado para ver sus convenios.",
                "Aceptar");
            return;
        }

        await Shell.Current.GoToAsync("//ConveniosPage");

        if (Shell.Current.CurrentPage is ConveniosPage pagina)
        {
            await pagina.CargarClienteAsync(
                cliente.IdCliente,
                cliente.RazonSocialNombre);
        }
    }
}
