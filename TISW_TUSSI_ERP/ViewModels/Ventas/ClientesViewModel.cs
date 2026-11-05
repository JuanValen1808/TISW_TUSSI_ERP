
using System.Collections.ObjectModel;
using System.Windows.Input;
using TISW_TUSSI_ERP.Models.Ventas;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Ventas;

public class ClientesViewModel : BaseViewModel
{
    private readonly ClientesService _service;

    public ObservableCollection<Cliente> Clientes { get; } = new();

    private string _busqueda = string.Empty;
    public string Busqueda
    {
        get => _busqueda;
        set => SetProperty(ref _busqueda, value);
    }

    // Cliente seleccionado en la tabla.
    private Cliente? _clienteSeleccionado;
    public Cliente? ClienteSeleccionado
    {
        get => _clienteSeleccionado;
        set
        {
            if (SetProperty(ref _clienteSeleccionado, value))
            {
                if (value != null)
                {
                    // Copia independiente para editar sin alterar la lista.
                    ClienteFormulario = CopiarCliente(value);
                    EsNuevoCliente = false;
                }
            }
        }
    }

    // Cliente que se edita en el formulario.
    private Cliente? _clienteFormulario;
    public Cliente? ClienteFormulario
    {
        get => _clienteFormulario;
        set => SetProperty(ref _clienteFormulario, value);
    }

    private bool _esNuevoCliente;
    public bool EsNuevoCliente
    {
        get => _esNuevoCliente;
        set => SetProperty(ref _esNuevoCliente, value);
    }

    private bool _cargando;
    public bool Cargando
    {
        get => _cargando;
        set => SetProperty(ref _cargando, value);
    }

    private string _mensaje = string.Empty;
    public string Mensaje
    {
        get => _mensaje;
        set => SetProperty(ref _mensaje, value);
    }

    public ICommand CargarCommand { get; }
    public ICommand BuscarCommand { get; }
    public ICommand GuardarCommand { get; }
    public ICommand EliminarCommand { get; }

    public ClientesViewModel(ClientesService service)
    {
        _service = service;

        CargarCommand = new Command(async () =>
            await CargarClientesAsync());

        BuscarCommand = new Command(async () =>
            await BuscarClientesAsync());

        GuardarCommand = new Command(async () =>
            await GuardarClienteAsync());

        EliminarCommand = new Command(async () =>
            await EliminarClienteAsync());
    }

    private static Cliente CopiarCliente(Cliente origen)
    {
        return new Cliente
        {
            IdCliente = origen.IdCliente,
            RutCliente = origen.RutCliente,
            RazonSocialNombre = origen.RazonSocialNombre,
            Email = origen.Email,
            Telefono = origen.Telefono,
            TipoCliente = origen.TipoCliente
        };
    }

    public async Task CargarClientesAsync()
    {
        if (Cargando) return;

        try
        {
            Cargando = true;

            var datos = await _service.ObtenerClientesAsync();

            Clientes.Clear();

            foreach (var cliente in datos)
                Clientes.Add(cliente);

            Mensaje = $"Clientes registrados: {Clientes.Count}";
        }
        catch (Exception ex)
        {
            Mensaje = $"Error al cargar clientes: {ex.Message}";
        }
        finally
        {
            Cargando = false;
        }
    }

    public async Task BuscarClientesAsync()
    {
        if (Cargando) return;

        try
        {
            Cargando = true;

            var texto = Busqueda?.Trim() ?? string.Empty;

            var datos = string.IsNullOrWhiteSpace(texto)
                ? await _service.ObtenerClientesAsync()
                : await _service.BuscarClientesAsync(texto);

            Clientes.Clear();

            foreach (var cliente in datos)
                Clientes.Add(cliente);

            Mensaje = Clientes.Count == 0
                ? "No se encontraron clientes."
                : $"Se encontraron {Clientes.Count} cliente(s).";
        }
        catch (Exception ex)
        {
            Mensaje = $"Error al buscar clientes: {ex.Message}";
        }
        finally
        {
            Cargando = false;
        }
    }

    public void NuevoCliente()
    {
        // Quitar selección anterior.
        ClienteSeleccionado = null;

        // Crear formulario independiente.
        ClienteFormulario = new Cliente
        {
            TipoCliente = "PERSONA"
        };

        EsNuevoCliente = true;
        Mensaje = "Ingresa los datos del nuevo cliente.";
    }

    public async Task GuardarClienteAsync()
    {
        if (Cargando || ClienteFormulario == null)
        {
            Mensaje = "Primero selecciona o crea un cliente.";
            return;
        }

        var cliente = ClienteFormulario;

        // Validaciones básicas.
        if (string.IsNullOrWhiteSpace(cliente.RutCliente))
        {
            Mensaje = "Debes ingresar el RUT del cliente.";
            return;
        }

        if (string.IsNullOrWhiteSpace(cliente.RazonSocialNombre))
        {
            Mensaje = "Debes ingresar el nombre o razón social.";
            return;
        }

        if (cliente.TipoCliente != "PERSONA" &&
            cliente.TipoCliente != "EMPRESA" &&
            cliente.TipoCliente != "INSTITUCION")
        {
            Mensaje = "Selecciona un tipo de cliente válido.";
            return;
        }

        bool esNuevo = cliente.IdCliente == 0;

        try
        {
            Cargando = true;

            cliente.RutCliente = cliente.RutCliente.Trim();
            cliente.RazonSocialNombre =
                cliente.RazonSocialNombre.Trim();

            if (esNuevo)
            {
                await _service.CrearClienteAsync(cliente);
            }
            else
            {
                await _service.ActualizarClienteAsync(cliente);
            }
        }
        catch (MySqlConnector.MySqlException ex)
            when (ex.Number == 1062)
        {
            Mensaje = "Ya existe un cliente registrado con ese RUT.";
            return;
        }
        catch (Exception ex)
        {
            Mensaje = $"Error al guardar cliente: {ex.Message}";
            return;
        }
        finally
        {
            Cargando = false;
        }

        // Recargar únicamente si se guardó correctamente.
        await BuscarClientesAsync();

        ClienteSeleccionado = null;
        ClienteFormulario = null;
        EsNuevoCliente = false;

        Mensaje = esNuevo
            ? "Cliente registrado correctamente."
            : "Cliente actualizado correctamente.";
    }

    public async Task EliminarClienteAsync()
    {
        if (Cargando || ClienteSeleccionado == null ||
            ClienteSeleccionado.IdCliente <= 0)
        {
            Mensaje = "Selecciona un cliente registrado para eliminar.";
            return;
        }

        try
        {
            Cargando = true;

            await _service.EliminarClienteAsync(
                ClienteSeleccionado.IdCliente);
        }
        catch (MySqlConnector.MySqlException ex)
            when (ex.Number == 1451)
        {
            Mensaje = "No se puede eliminar el cliente porque " +
                      "tiene ventas o convenios asociados.";
            return;
        }
        catch (Exception ex)
        {
            Mensaje = $"Error al eliminar cliente: {ex.Message}";
            return;
        }
        finally
        {
            Cargando = false;
        }

        ClienteSeleccionado = null;
        ClienteFormulario = null;
        EsNuevoCliente = false;

        await BuscarClientesAsync();

        Mensaje = "Cliente eliminado correctamente.";
    }
}
