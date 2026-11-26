
using System.Collections.ObjectModel;
using System.Windows.Input;
using TISW_TUSSI_ERP.Models.Ventas;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Ventas;

public class ConveniosViewModel : BaseViewModel
{
    private readonly ConveniosService _service;
    private readonly ClientesService _clientesService;

    private List<Cliente> _todosLosClientes = new();
    private int _versionCarga;
    private bool _viendoTodos = true;

    public ObservableCollection<Cliente> Clientes { get; } = new();
    public ObservableCollection<ConvenioSalud> Convenios { get; } = new();

    private string _busquedaCliente = string.Empty;

    public string BusquedaCliente
    {
        get => _busquedaCliente;
        set
        {
            if (SetProperty(ref _busquedaCliente, value))
                FiltrarClientes();
        }
    }

    private Cliente? _clienteSeleccionado;

    public Cliente? ClienteSeleccionado
    {
        get => _clienteSeleccionado;
        set
        {
            if (SetProperty(ref _clienteSeleccionado, value))
            {
                ++_versionCarga;
                _viendoTodos = value == null;

                AplicarDatosCliente(value);

                ConvenioSeleccionado = null;
                ConvenioFormulario = null;
                Convenios.Clear();

                Mensaje = value == null
                    ? "Mostrando todos los convenios registrados."
                    : "Cargando convenios del cliente...";
            }
        }
    }

    private void AplicarDatosCliente(Cliente? cliente)
    {
        IdCliente = cliente?.IdCliente ?? 0;
        NombreCliente = cliente?.RazonSocialNombre
            ?? "Todos los clientes (vista general)";
        RutCliente = cliente?.RutCliente ?? "—";
        TipoCliente = cliente?.TipoCliente ?? "TODOS";
        EmailCliente = cliente?.Email ?? "";
        TelefonoCliente = cliente?.Telefono ?? "";
    }

    private int _idCliente;

    public int IdCliente
    {
        get => _idCliente;
        private set => SetProperty(ref _idCliente, value);
    }

    private string _nombreCliente = "Todos los clientes (vista general)";

    public string NombreCliente
    {
        get => _nombreCliente;
        private set => SetProperty(ref _nombreCliente, value);
    }

    private string _rutCliente = "—";

    public string RutCliente
    {
        get => _rutCliente;
        private set => SetProperty(ref _rutCliente, value);
    }

    private string _tipoCliente = "TODOS";

    public string TipoCliente
    {
        get => _tipoCliente;
        private set => SetProperty(ref _tipoCliente, value);
    }

    private string _emailCliente = "";

    public string EmailCliente
    {
        get => _emailCliente;
        private set => SetProperty(ref _emailCliente, value);
    }

    private string _telefonoCliente = "";

    public string TelefonoCliente
    {
        get => _telefonoCliente;
        private set => SetProperty(ref _telefonoCliente, value);
    }

    private ConvenioSalud? _convenioSeleccionado;

    public ConvenioSalud? ConvenioSeleccionado
    {
        get => _convenioSeleccionado;
        set
        {
            if (SetProperty(ref _convenioSeleccionado, value))
            {
                if (value != null)
                {
                    // Si estábamos viendo todos los convenios, cargar también los datos de su cliente en el encabezado sin borrar la lista.
                    var propietario = _todosLosClientes.FirstOrDefault(
                        c => c.IdCliente == value.IdCliente);
                    if (propietario != null)
                        AplicarDatosCliente(propietario);
                    else
                        IdCliente = value.IdCliente;

                    ConvenioFormulario = CopiarConvenio(value);
                    Mensaje = $"Editando convenio: {value.NombreConvenio} ({value.NombreCliente})";
                }
            }
        }
    }

    private ConvenioSalud? _convenioFormulario;

    public ConvenioSalud? ConvenioFormulario
    {
        get => _convenioFormulario;
        set => SetProperty(ref _convenioFormulario, value);
    }

    private bool _cargando;

    public bool Cargando
    {
        get => _cargando;
        set => SetProperty(ref _cargando, value);
    }

    private string _mensaje = "";

    public string Mensaje
    {
        get => _mensaje;
        set => SetProperty(ref _mensaje, value);
    }

    public ICommand NuevoCommand { get; }
    public ICommand GuardarCommand { get; }
    public ICommand CargarCommand { get; }
    public ICommand VerTodosCommand { get; }

    public ConveniosViewModel(
        ConveniosService service,
        ClientesService clientesService)
    {
        _service = service;
        _clientesService = clientesService;

        NuevoCommand = new Command(NuevoConvenio);

        GuardarCommand = new Command(
            async () => await GuardarConvenioAsync());

        CargarCommand = new Command(
            async () => await CargarConveniosAsync());

        VerTodosCommand = new Command(
            async () => await MostrarTodosAsync());
    }

    // BUSCAR CLIENTES
    private void FiltrarClientes()
    {
        string texto = (BusquedaCliente ?? "")
            .Trim()
            .ToLowerInvariant();

        var filtrados = _todosLosClientes.Where(c =>
            string.IsNullOrEmpty(texto) ||
            (c.RazonSocialNombre ?? "")
                .ToLowerInvariant().Contains(texto) ||
            (c.RutCliente ?? "")
                .ToLowerInvariant().Contains(texto));

        Clientes.Clear();

        foreach (var cliente in filtrados)
            Clientes.Add(cliente);
    }

    // VOLVER A MOSTRAR TODOS LOS CONVENIOS REGISTRADOS
    public async Task MostrarTodosAsync()
    {
        if (Cargando)
            return;

        _clienteSeleccionado = null;
        OnPropertyChanged(nameof(ClienteSeleccionado));
        _viendoTodos = true;
        AplicarDatosCliente(null);

        await CargarTodosLosConveniosAsync();
    }

    // CARGAR CLIENTES Y CONVENIOS INICIALES
    public async Task CargarClientesAsync(
        int? idClientePreferido = null)
    {
        Cliente? encontrado = null;

        try
        {
            Cargando = true;

            int? idBuscado = idClientePreferido
                ?? (_viendoTodos ? null : ClienteSeleccionado?.IdCliente);

            var datos = await _clientesService.ObtenerClientesAsync();

            _todosLosClientes = datos;

            BusquedaCliente = "";
            FiltrarClientes();

            encontrado = idBuscado.HasValue
                ? _todosLosClientes.FirstOrDefault(
                    c => c.IdCliente == idBuscado.Value)
                : null;

            _clienteSeleccionado = encontrado;
            OnPropertyChanged(nameof(ClienteSeleccionado));
            _viendoTodos = encontrado == null;
            AplicarDatosCliente(encontrado);
        }
        catch (Exception ex)
        {
            Mensaje = "Error al cargar clientes: " + ex.Message;
            return;
        }
        finally
        {
            Cargando = false;
        }

        if (encontrado != null)
            await CargarConveniosAsync();
        else
            await CargarTodosLosConveniosAsync();
    }

    // ENTRADA DESDE CLIENTES > VER CONVENIOS
    public async Task InicializarAsync(
        int idCliente,
        string nombreCliente)
    {
        if (idCliente <= 0)
        {
            await CargarClientesAsync();
            return;
        }

        await CargarClientesAsync(idCliente);
    }

    // CONSULTAR TODOS LOS CONVENIOS REGISTRADOS EN MYSQL
    public async Task CargarTodosLosConveniosAsync()
    {
        int version = ++_versionCarga;

        try
        {
            Cargando = true;

            var datos = await _service.ObtenerTodosAsync();

            if (version != _versionCarga)
                return;

            ConvenioSeleccionado = null;
            ConvenioFormulario = null;
            Convenios.Clear();

            foreach (var convenio in datos)
                Convenios.Add(convenio);

            Mensaje = Convenios.Count == 0
                ? "No existen convenios registrados en el sistema."
                : $"Mostrando todos los convenios registrados ({Convenios.Count}). Haz clic en un cliente arriba si deseas filtrar o crear uno nuevo.";
        }
        catch (Exception ex)
        {
            if (version == _versionCarga)
                Mensaje = "Error al cargar convenios: " + ex.Message;
        }
        finally
        {
            if (version == _versionCarga)
                Cargando = false;
        }
    }

    // CONSULTAR CONVENIOS DE UN CLIENTE EN MYSQL
    public async Task CargarConveniosAsync()
    {
        int idConsultado = IdCliente;

        if (idConsultado <= 0)
        {
            await CargarTodosLosConveniosAsync();
            return;
        }

        int version = ++_versionCarga;

        try
        {
            Cargando = true;

            var datos = await _service.ObtenerPorClienteAsync(
                idConsultado);

            if (version != _versionCarga ||
                IdCliente != idConsultado)
                return;

            ConvenioSeleccionado = null;
            ConvenioFormulario = null;
            Convenios.Clear();

            foreach (var convenio in datos)
                Convenios.Add(convenio);

            Mensaje = Convenios.Count == 0
                ? "Este cliente no tiene convenios registrados. Puedes crearle uno con '+ Nuevo convenio' o pulsar 'Ver todos'."
                : $"Convenios de {NombreCliente}: {Convenios.Count}";
        }
        catch (Exception ex)
        {
            if (version == _versionCarga)
                Mensaje = "Error al cargar convenios: " + ex.Message;
        }
        finally
        {
            if (version == _versionCarga)
                Cargando = false;
        }
    }

    // NUEVO CONVENIO
    public void NuevoConvenio()
    {
        if (IdCliente <= 0)
        {
            Mensaje = "Primero selecciona un cliente en la lista superior para asignarle el nuevo convenio.";
            return;
        }

        if (Cargando)
            return;

        ConvenioSeleccionado = null;

        ConvenioFormulario = new ConvenioSalud
        {
            IdCliente = IdCliente,
            NombreCliente = NombreCliente,
            NombreConvenio = "",
            PorcentajeDescuento = 0,
            LimiteCredito = 0,
            DiasCreditoMorosidad = 0,
            Activo = true
        };

        Mensaje = $"Creando nuevo convenio para {NombreCliente}.";
    }

    // GUARDAR CONVENIO
    public async Task GuardarConvenioAsync()
    {
        if (Cargando)
            return;

        if (IdCliente <= 0 || ConvenioFormulario == null)
        {
            Mensaje = "Selecciona un cliente arriba (para crear) o un convenio de la lista (para editar).";
            return;
        }

        var convenio = ConvenioFormulario;

        if (string.IsNullOrWhiteSpace(convenio.NombreConvenio))
        {
            Mensaje = "El nombre del convenio es obligatorio.";
            return;
        }

        if (convenio.PorcentajeDescuento < 0 ||
            convenio.PorcentajeDescuento > 100)
        {
            Mensaje = "El descuento debe estar entre 0 y 100%.";
            return;
        }

        if (convenio.LimiteCredito < 0 ||
            convenio.DiasCreditoMorosidad < 0)
        {
            Mensaje = "El crédito y los días no pueden ser negativos.";
            return;
        }

        int idOriginal = IdCliente;
        bool esNuevo = convenio.IdConvenio == 0;

        try
        {
            Cargando = true;

            convenio.IdCliente = idOriginal;
            convenio.NombreConvenio =
                convenio.NombreConvenio.Trim();

            if (esNuevo)
                await _service.CrearConvenioAsync(convenio);
            else
                await _service.ActualizarConvenioAsync(convenio);
        }
        catch (Exception ex)
        {
            Mensaje = "Error al guardar convenio: " + ex.Message;
            return;
        }
        finally
        {
            Cargando = false;
        }

        ConvenioSeleccionado = null;
        ConvenioFormulario = null;

        if (_viendoTodos)
        {
            AplicarDatosCliente(null);
            await CargarTodosLosConveniosAsync();
        }
        else if (IdCliente == idOriginal)
        {
            await CargarConveniosAsync();
        }

        if (!Mensaje.StartsWith("Error al cargar"))
        {
            Mensaje = esNuevo
                ? "Convenio registrado correctamente."
                : "Convenio actualizado correctamente.";
        }
    }

    // ELIMINAR CONVENIO
    public async Task EliminarConvenioAsync()
    {
        if (Cargando ||
            IdCliente <= 0 ||
            ConvenioSeleccionado == null)
        {
            Mensaje = "Selecciona un convenio registrado.";
            return;
        }

        int idConvenio = ConvenioSeleccionado.IdConvenio;
        int idCliente = IdCliente;

        try
        {
            Cargando = true;

            await _service.EliminarConvenioAsync(
                idConvenio, idCliente);
        }
        catch (MySqlConnector.MySqlException ex)
            when (ex.Number == 1451)
        {
            Mensaje = "No se puede eliminar: tiene ventas o cuentas asociadas.";
            return;
        }
        catch (Exception ex)
        {
            Mensaje = "Error al eliminar convenio: " + ex.Message;
            return;
        }
        finally
        {
            Cargando = false;
        }

        ConvenioSeleccionado = null;
        ConvenioFormulario = null;

        if (_viendoTodos)
        {
            AplicarDatosCliente(null);
            await CargarTodosLosConveniosAsync();
        }
        else if (IdCliente == idCliente)
        {
            await CargarConveniosAsync();
        }

        if (!Mensaje.StartsWith("Error al cargar"))
            Mensaje = "Convenio eliminado correctamente.";
    }

    private static ConvenioSalud CopiarConvenio(
        ConvenioSalud origen)
    {
        return new ConvenioSalud
        {
            IdConvenio = origen.IdConvenio,
            IdCliente = origen.IdCliente,
            NombreCliente = origen.NombreCliente,
            NombreConvenio = origen.NombreConvenio,
            PorcentajeDescuento = origen.PorcentajeDescuento,
            LimiteCredito = origen.LimiteCredito,
            DiasCreditoMorosidad = origen.DiasCreditoMorosidad,
            Activo = origen.Activo
        };
    }
}
