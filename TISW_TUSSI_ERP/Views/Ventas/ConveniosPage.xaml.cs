
using TISW_TUSSI_ERP.Models.Ventas;
using TISW_TUSSI_ERP.ViewModels.Ventas;

namespace TISW_TUSSI_ERP.Views.Ventas;

public partial class ConveniosPage : ContentPage, IQueryAttributable
{
    private readonly ConveniosViewModel _viewModel;

    private bool _inicializando;
    private bool _seleccionandoCliente;
    private bool _paginaVisible;

    private int _idClientePendiente;
    private string _nombreClientePendiente = string.Empty;

    public ConveniosPage(ConveniosViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    // RECIBIR CLIENTE DESDE LA NAVEGACION
    public void ApplyQueryAttributes(
        IDictionary<string, object> query)
    {
        if (!query.TryGetValue("idCliente", out var valor))
            return;

        if (!int.TryParse(valor?.ToString(), out int idCliente)
            || idCliente <= 0)
            return;

        _idClientePendiente = idCliente;

        _nombreClientePendiente =
            query.TryGetValue("nombreCliente", out var nombre)
                ? Uri.UnescapeDataString(nombre?.ToString() ?? "")
                : string.Empty;

        // Si la página ya está visible, procesar el cliente.
        if (_paginaVisible)
        {
            _ = ProcesarClientePendienteAsync();
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _paginaVisible = true;

        if (_inicializando)
            return;

        try
        {
            _inicializando = true;

            if (_idClientePendiente > 0)
            {
                int id = _idClientePendiente;
                string nombre = _nombreClientePendiente;

                _idClientePendiente = 0;

                await _viewModel.InicializarAsync(id, nombre);
            }
            else
            {
                await _viewModel.CargarClientesAsync();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Error",
                $"No se pudieron cargar los clientes: {ex.Message}",
                "Aceptar");
        }
        finally
        {
            _inicializando = false;
        }

        // Por si el parámetro llegó durante la carga inicial.
        if (_idClientePendiente > 0)
            await ProcesarClientePendienteAsync();
    }

    protected override void OnDisappearing()
    {
        _paginaVisible = false;
        base.OnDisappearing();
    }

    // PROCESAR CLIENTE RECIBIDO
    private async Task ProcesarClientePendienteAsync()
    {
        if (_inicializando || _idClientePendiente <= 0)
            return;

        try
        {
            _inicializando = true;

            int id = _idClientePendiente;
            string nombre = _nombreClientePendiente;

            _idClientePendiente = 0;

            await _viewModel.InicializarAsync(id, nombre);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Error",
                $"No se pudo seleccionar el cliente: {ex.Message}",
                "Aceptar");
        }
        finally
        {
            _inicializando = false;
        }
    }

    // SELECCIONAR CLIENTE MEDIANTE CLIC
    private async void OnClienteTarjetaTapped(
        object sender,
        TappedEventArgs e)
    {
        if (_inicializando || _seleccionandoCliente)
            return;

        if (e.Parameter is not Cliente cliente)
            return;

        if (_viewModel.IdCliente == cliente.IdCliente)
            return;

        try
        {
            _seleccionandoCliente = true;

            _viewModel.ClienteSeleccionado = cliente;

            await _viewModel.CargarConveniosAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Error",
                $"No se pudieron cargar los convenios: {ex.Message}",
                "Aceptar");
        }
        finally
        {
            _seleccionandoCliente = false;
        }
    }

    // NUEVO CONVENIO
    private void OnNuevoConvenioClicked(
        object sender,
        EventArgs e)
    {
        _viewModel.NuevoConvenio();
    }

    // ELIMINAR CONVENIO
    private async void OnEliminarConvenioClicked(
        object sender,
        EventArgs e)
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

    // VOLVER A CLIENTES
    private async void OnVolverClicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("//ClientesPage");
    }
}
