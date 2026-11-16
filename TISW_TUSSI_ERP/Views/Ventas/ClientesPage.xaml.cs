
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

    // NUEVO CLIENTE
    private void OnNuevoClienteClicked(
        object sender, EventArgs e)
    {
        _viewModel.NuevoCliente();
    }

    // SELECCIÓN DE TIPO
    private void SeleccionarTipo(string tipo)
    {
        var actual = _viewModel.ClienteFormulario;

        if (actual == null)
        {
            _viewModel.Mensaje =
                "Primero selecciona un cliente o pulsa Nuevo cliente.";
            return;
        }

        _viewModel.ClienteFormulario = new Cliente
        {
            IdCliente = actual.IdCliente,
            RutCliente = actual.RutCliente,
            RazonSocialNombre = actual.RazonSocialNombre,
            Email = actual.Email,
            Telefono = actual.Telefono,
            TipoCliente = tipo
        };

        _viewModel.Mensaje = $"Tipo de cliente seleccionado: {tipo}";
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

    // ELIMINAR CLIENTE
    private async void OnEliminarClienteClicked(
        object sender, EventArgs e)
    {
        var cliente = _viewModel.ClienteSeleccionado;

        if (cliente == null || cliente.IdCliente <= 0)
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
            await _viewModel.EliminarClienteAsync();
        }
    }

    // VER CONVENIOS
    private async void OnVerConveniosClicked(
        object sender, EventArgs e)
    {
        var cliente = _viewModel.ClienteSeleccionado;

        if (cliente == null || cliente.IdCliente <= 0)
        {
            await DisplayAlertAsync(
                "Seleccionar cliente",
                "Primero selecciona un cliente registrado.",
                "Aceptar");
            return;
        }

        try
        {
            var nombre = Uri.EscapeDataString(
                cliente.RazonSocialNombre ?? "");

            await Shell.Current.GoToAsync(
                $"//ConveniosPage?idCliente={cliente.IdCliente}&nombreCliente={nombre}");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Error de navegación",
                $"No se pudo abrir Convenios: {ex.Message}",
                "Aceptar");
        }
    }
}
