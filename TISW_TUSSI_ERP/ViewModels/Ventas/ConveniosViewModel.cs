
using System.Collections.ObjectModel;
using System.Windows.Input;
using TISW_TUSSI_ERP.Models.Ventas;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Ventas;

public class ConveniosViewModel : BaseViewModel
{
    private readonly ConveniosService _service;

    public ObservableCollection<ConvenioSalud> Convenios { get; } = new();

    private int _idCliente;
    public int IdCliente
    {
        get => _idCliente;
        private set => SetProperty(ref _idCliente, value);
    }

    private string _nombreCliente = string.Empty;
    public string NombreCliente
    {
        get => _nombreCliente;
        private set => SetProperty(ref _nombreCliente, value);
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
                    ConvenioFormulario = CopiarConvenio(value);
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

    private string _mensaje = string.Empty;
    public string Mensaje
    {
        get => _mensaje;
        set => SetProperty(ref _mensaje, value);
    }

    public ICommand CargarCommand { get; }
    public ICommand NuevoCommand { get; }
    public ICommand GuardarCommand { get; }
    public ICommand EliminarCommand { get; }

    public ConveniosViewModel(ConveniosService service)
    {
        _service = service;

        CargarCommand = new Command(async () =>
            await CargarConveniosAsync());

        NuevoCommand = new Command(NuevoConvenio);

        GuardarCommand = new Command(async () =>
            await GuardarConvenioAsync());

        EliminarCommand = new Command(async () =>
            await EliminarConvenioAsync());
    }

    // Establecer el cliente al entrar a la pantalla
    public async Task InicializarAsync(
        int idCliente,
        string nombreCliente)
    {
        if (idCliente <= 0)
        {
            Mensaje = "Debes seleccionar un cliente registrado.";
            return;
        }

        IdCliente = idCliente;
        NombreCliente = nombreCliente;

        ConvenioSeleccionado = null;
        ConvenioFormulario = null;

        await CargarConveniosAsync();
    }

    // Consultar convenios desde MySQL
    public async Task CargarConveniosAsync()
    {
        if (Cargando || IdCliente <= 0)
            return;

        try
        {
            Cargando = true;

            var datos = await _service.ObtenerPorClienteAsync(IdCliente);

            Convenios.Clear();

            foreach (var convenio in datos)
                Convenios.Add(convenio);
        }
        catch (Exception ex)
        {
            Mensaje = $"Error al cargar convenios: {ex.Message}";
        }
        finally
        {
            Cargando = false;
        }
    }

    // Preparar formulario vacío
    public void NuevoConvenio()
    {
        if (IdCliente <= 0)
        {
            Mensaje = "Primero debes seleccionar un cliente.";
            return;
        }

        ConvenioSeleccionado = null;

        ConvenioFormulario = new ConvenioSalud
        {
            IdCliente = IdCliente,
            NombreConvenio = string.Empty,
            PorcentajeDescuento = 0,
            LimiteCredito = 0,
            DiasCreditoMorosidad = 0,
            Activo = true
        };

        Mensaje = "Ingresa los datos del nuevo convenio.";
    }

    // Crear o actualizar convenio
    public async Task GuardarConvenioAsync()
    {
        if (Cargando || ConvenioFormulario == null)
        {
            Mensaje = "Selecciona un convenio o crea uno nuevo.";
            return;
        }

        bool guardado = false;

        try
        {
            Cargando = true;

            ConvenioFormulario.IdCliente = IdCliente;

            if (ConvenioFormulario.IdConvenio == 0)
            {
                await _service.CrearConvenioAsync(ConvenioFormulario);
                Mensaje = "Convenio registrado correctamente.";
            }
            else
            {
                await _service.ActualizarConvenioAsync(ConvenioFormulario);
                Mensaje = "Convenio actualizado correctamente.";
            }

            guardado = true;
        }
        catch (Exception ex)
        {
            Mensaje = $"Error al guardar convenio: {ex.Message}";
        }
        finally
        {
            Cargando = false;
        }

        if (guardado)
        {
            ConvenioSeleccionado = null;
            ConvenioFormulario = null;

            await CargarConveniosAsync();
        }
    }

    // Eliminar convenio seleccionado
    public async Task EliminarConvenioAsync()
    {
        if (Cargando ||
            ConvenioSeleccionado == null ||
            ConvenioSeleccionado.IdConvenio <= 0)
        {
            Mensaje = "Selecciona un convenio registrado para eliminar.";
            return;
        }

        bool eliminado = false;

        try
        {
            Cargando = true;

            await _service.EliminarConvenioAsync(
                ConvenioSeleccionado.IdConvenio,
                IdCliente);

            eliminado = true;
            Mensaje = "Convenio eliminado correctamente.";
        }
        catch (MySqlConnector.MySqlException ex)
            when (ex.Number == 1451)
        {
            Mensaje = "No se puede eliminar el convenio porque " +
                      "tiene registros asociados.";
        }
        catch (Exception ex)
        {
            Mensaje = $"Error al eliminar convenio: {ex.Message}";
        }
        finally
        {
            Cargando = false;
        }

        if (eliminado)
        {
            ConvenioSeleccionado = null;
            ConvenioFormulario = null;

            await CargarConveniosAsync();
        }
    }

    // Copiar datos para no editar directamente la lista
    private static ConvenioSalud CopiarConvenio(
        ConvenioSalud origen)
    {
        return new ConvenioSalud
        {
            IdConvenio = origen.IdConvenio,
            IdCliente = origen.IdCliente,
            NombreConvenio = origen.NombreConvenio,
            PorcentajeDescuento = origen.PorcentajeDescuento,
            LimiteCredito = origen.LimiteCredito,
            DiasCreditoMorosidad = origen.DiasCreditoMorosidad,
            Activo = origen.Activo
        };
    }
}
