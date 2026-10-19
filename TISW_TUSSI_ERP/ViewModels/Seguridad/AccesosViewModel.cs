using System.Collections.ObjectModel;
using System.Windows.Input;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Seguridad;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Seguridad;

public class AccesosViewModel : BaseViewModel
{
    private readonly SeguridadService _seguridadService;

    public ObservableCollection<RolModel> Roles { get; } = new();
    public ObservableCollection<ModuloPermisosGrupo> GruposModulos { get; } = new();

    private RolModel? _rolSeleccionado;
    public RolModel? RolSeleccionado
    {
        get => _rolSeleccionado;
        set
        {
            if (SetProperty(ref _rolSeleccionado, value))
            {
                foreach (var r in Roles)
                {
                    r.EsSeleccionado = (_rolSeleccionado != null && r.IdRol == _rolSeleccionado.IdRol);
                }
                OnPropertyChanged(nameof(TieneRolSeleccionado));
                OnPropertyChanged(nameof(NombreRolSeleccionadoTexto));
                _ = CargarPermisosDelRolAsync();
            }
        }
    }

    public bool TieneRolSeleccionado => RolSeleccionado is not null;
    public string NombreRolSeleccionadoTexto => RolSeleccionado != null
        ? $"Permisos configurados para: {RolSeleccionado.NombreRol}"
        : "Seleccione un perfil para gestionar sus permisos";

    private int _totalPermisosSistema;
    public int TotalPermisosSistema { get => _totalPermisosSistema; set => SetProperty(ref _totalPermisosSistema, value); }

    private int _totalPermisosHabilitados;
    public int TotalPermisosHabilitados { get => _totalPermisosHabilitados; set => SetProperty(ref _totalPermisosHabilitados, value); }

    public string ResumenPermisosTexto => $"{TotalPermisosHabilitados} de {TotalPermisosSistema} permisos asignados";

    private bool _hayCambiosPendientes;
    public bool HayCambiosPendientes
    {
        get => _hayCambiosPendientes;
        set => SetProperty(ref _hayCambiosPendientes, value);
    }

    // Modal para crear nuevo permiso en catálogo
    private bool _mostrarModalNuevoPermiso;
    public bool MostrarModalNuevoPermiso
    {
        get => _mostrarModalNuevoPermiso;
        set => SetProperty(ref _mostrarModalNuevoPermiso, value);
    }

    private string _nuevoCodigoPermiso = string.Empty;
    public string NuevoCodigoPermiso
    {
        get => _nuevoCodigoPermiso;
        set { if (SetProperty(ref _nuevoCodigoPermiso, value)) ErrorNuevoCodigo = string.Empty; }
    }

    private string _nuevoModulo = "Ventas";
    public string NuevoModulo
    {
        get => _nuevoModulo;
        set => SetProperty(ref _nuevoModulo, value);
    }

    private string _nuevaDescripcion = string.Empty;
    public string NuevaDescripcion
    {
        get => _nuevaDescripcion;
        set { if (SetProperty(ref _nuevaDescripcion, value)) ErrorNuevaDescripcion = string.Empty; }
    }

    private string _errorNuevoCodigo = string.Empty;
    public string ErrorNuevoCodigo
    {
        get => _errorNuevoCodigo;
        set { if (SetProperty(ref _errorNuevoCodigo, value)) OnPropertyChanged(nameof(HayErrorNuevoCodigo)); }
    }
    public bool HayErrorNuevoCodigo => !string.IsNullOrEmpty(ErrorNuevoCodigo);

    private string _errorNuevaDescripcion = string.Empty;
    public string ErrorNuevaDescripcion
    {
        get => _errorNuevaDescripcion;
        set { if (SetProperty(ref _errorNuevaDescripcion, value)) OnPropertyChanged(nameof(HayErrorNuevaDescripcion)); }
    }
    public bool HayErrorNuevaDescripcion => !string.IsNullOrEmpty(ErrorNuevaDescripcion);

    public List<string> ModulosDisponibles { get; } = new()
    {
        "Ventas",
        "Compras",
        "Inventario",
        "Contabilidad",
        "Administración",
        "Gerencia"
    };

    private string _mensajeGeneral = string.Empty;
    public string MensajeGeneral
    {
        get => _mensajeGeneral;
        set { if (SetProperty(ref _mensajeGeneral, value)) OnPropertyChanged(nameof(HayMensajeGeneral)); }
    }
    public bool HayMensajeGeneral => !string.IsNullOrEmpty(MensajeGeneral);

    private bool _esMensajeExito;
    public bool EsMensajeExito
    {
        get => _esMensajeExito;
        set => SetProperty(ref _esMensajeExito, value);
    }

    public Color ColorMensajeGeneral => EsMensajeExito ? Paleta.Exito : Paleta.Peligro;
    public Color FondoMensajeGeneral => EsMensajeExito ? Color.FromArgb("#DCFCE7") : Color.FromArgb("#FEE2E2");

    // Comandos
    public ICommand ComandoCargar { get; }
    public ICommand ComandoSeleccionarRol { get; }
    public ICommand ComandoAlternarPermiso { get; }
    public ICommand ComandoSeleccionarTodosModulo { get; }
    public ICommand ComandoDesmarcarTodosModulo { get; }
    public ICommand ComandoGuardarAccesos { get; }
    public ICommand ComandoAbrirNuevoPermiso { get; }
    public ICommand ComandoCerrarNuevoPermiso { get; }
    public ICommand ComandoGuardarNuevoPermiso { get; }

    public AccesosViewModel(SeguridadService seguridadService)
    {
        _seguridadService = seguridadService;
        Titulo = "Mantenedor de Accesos y Permisos";

        ComandoCargar = new Command(async () => await CargarDatosInicialesAsync());
        ComandoSeleccionarRol = new Command<RolModel>(r => RolSeleccionado = r);
        ComandoAlternarPermiso = new Command<PermisoModel>(AlternarPermiso);
        ComandoSeleccionarTodosModulo = new Command<ModuloPermisosGrupo>(g => SetModuloPermisos(g, true));
        ComandoDesmarcarTodosModulo = new Command<ModuloPermisosGrupo>(g => SetModuloPermisos(g, false));
        ComandoGuardarAccesos = new Command(async () => await GuardarCambiosAccesosAsync());
        ComandoAbrirNuevoPermiso = new Command(AbrirNuevoPermiso);
        ComandoCerrarNuevoPermiso = new Command(() => MostrarModalNuevoPermiso = false);
        ComandoGuardarNuevoPermiso = new Command(async () => await GuardarNuevoPermisoAsync());
    }

    public async Task CargarDatosInicialesAsync()
    {
        if (EstaOcupado) return;
        EstaOcupado = true;
        MensajeGeneral = string.Empty;

        try
        {
            var rolesDb = await _seguridadService.ObtenerRolesAsync();
            Roles.Clear();
            foreach (var r in rolesDb)
            {
                Roles.Add(r);
            }

            int idPreseleccionado = Preferences.Get("perfil_seleccionado_accesos", 0);
            if (idPreseleccionado > 0)
            {
                Preferences.Remove("perfil_seleccionado_accesos");
                RolSeleccionado = Roles.FirstOrDefault(r => r.IdRol == idPreseleccionado) ?? Roles.FirstOrDefault();
            }
            else
            {
                RolSeleccionado = Roles.FirstOrDefault();
            }
        }
        catch (Exception ex)
        {
            MensajeGeneral = $"Error al cargar perfiles: {ex.Message}";
            EsMensajeExito = false;
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private async Task CargarPermisosDelRolAsync()
    {
        if (RolSeleccionado is null)
        {
            GruposModulos.Clear();
            TotalPermisosSistema = 0;
            TotalPermisosHabilitados = 0;
            return;
        }

        EstaOcupado = true;
        try
        {
            var lista = await _seguridadService.ObtenerPermisosPorRolAsync(RolSeleccionado.IdRol);

            foreach (var p in lista)
            {
                p.OnAsignadoCambiado = () =>
                {
                    HayCambiosPendientes = true;
                    ActualizarTotales();
                    var grupo = GruposModulos.FirstOrDefault(g => g.Permisos.Contains(p));
                    grupo?.NotificarCambios();
                };
            }

            var agrupados = lista
                .GroupBy(p => p.Modulo)
                .OrderBy(g => g.Key)
                .Select(g => new ModuloPermisosGrupo
                {
                    NombreModulo = g.Key,
                    Permisos = g.ToList()
                })
                .ToList();

            GruposModulos.Clear();
            foreach (var g in agrupados)
            {
                GruposModulos.Add(g);
            }

            ActualizarTotales();
            HayCambiosPendientes = false;
        }
        catch (Exception ex)
        {
            MensajeGeneral = $"Error al cargar permisos del rol: {ex.Message}";
            EsMensajeExito = false;
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private void AlternarPermiso(PermisoModel? permiso)
    {
        if (permiso is null) return;
        permiso.Asignado = !permiso.Asignado;
        HayCambiosPendientes = true;
        ActualizarTotales();

        // Notificar al grupo
        var grupo = GruposModulos.FirstOrDefault(g => g.Permisos.Contains(permiso));
        grupo?.NotificarCambios();
    }

    private void SetModuloPermisos(ModuloPermisosGrupo? grupo, bool asignar)
    {
        if (grupo is null) return;
        foreach (var p in grupo.Permisos)
        {
            p.Asignado = asignar;
        }
        grupo.NotificarCambios();
        HayCambiosPendientes = true;
        ActualizarTotales();
    }

    private void ActualizarTotales()
    {
        var todos = GruposModulos.SelectMany(g => g.Permisos).ToList();
        TotalPermisosSistema = todos.Count;
        TotalPermisosHabilitados = todos.Count(p => p.Asignado);
        OnPropertyChanged(nameof(ResumenPermisosTexto));
    }

    private async Task GuardarCambiosAccesosAsync()
    {
        if (RolSeleccionado is null || EstaOcupado) return;

        // Si es el rol Administrador y se le desmarcan todos los permisos, advertir
        if (RolSeleccionado.NombreRol.Equals("Administrador", StringComparison.OrdinalIgnoreCase) && TotalPermisosHabilitados == 0)
        {
            bool continuar = await Application.Current!.Windows[0].Page!.DisplayAlertAsync(
                "Advertencia de Seguridad",
                "Está por quitar todos los permisos al perfil Administrador. Los administradores no podrán ejecutar acciones operativas. ¿Desea continuar de todas formas?",
                "Continuar",
                "Cancelar");

            if (!continuar) return;
        }

        EstaOcupado = true;
        try
        {
            var idsHabilitados = GruposModulos
                .SelectMany(g => g.Permisos)
                .Where(p => p.Asignado)
                .Select(p => p.IdPermiso)
                .ToList();

            await _seguridadService.GuardarPermisosRolAsync(RolSeleccionado.IdRol, idsHabilitados);
            RolSeleccionado.CantidadPermisos = idsHabilitados.Count;

            HayCambiosPendientes = false;
            MensajeGeneral = $"Permisos actualizados correctamente para el perfil '{RolSeleccionado.NombreRol}'.";
            EsMensajeExito = true;
        }
        catch (Exception ex)
        {
            MensajeGeneral = $"Error al guardar accesos: {ex.Message}";
            EsMensajeExito = false;
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private void AbrirNuevoPermiso()
    {
        NuevoCodigoPermiso = string.Empty;
        NuevaDescripcion = string.Empty;
        NuevoModulo = ModulosDisponibles.FirstOrDefault() ?? "Ventas";
        ErrorNuevoCodigo = string.Empty;
        ErrorNuevaDescripcion = string.Empty;
        MostrarModalNuevoPermiso = true;
    }

    private async Task GuardarNuevoPermisoAsync()
    {
        ErrorNuevoCodigo = string.Empty;
        ErrorNuevaDescripcion = string.Empty;
        bool valido = true;

        if (string.IsNullOrWhiteSpace(NuevoCodigoPermiso) || NuevoCodigoPermiso.Trim().Length < 3)
        {
            ErrorNuevoCodigo = "El código debe tener al menos 3 caracteres (ej: VENTAS_DESCUENTO).";
            valido = false;
        }

        if (string.IsNullOrWhiteSpace(NuevaDescripcion))
        {
            ErrorNuevaDescripcion = "Debe describir qué autoriza este permiso.";
            valido = false;
        }

        if (!valido) return;

        EstaOcupado = true;
        try
        {
            await _seguridadService.CrearPermisoAsync(NuevoCodigoPermiso, NuevoModulo, NuevaDescripcion);
            MostrarModalNuevoPermiso = false;
            MensajeGeneral = $"Nuevo permiso '{NuevoCodigoPermiso.ToUpperInvariant()}' creado en el catálogo.";
            EsMensajeExito = true;

            await CargarPermisosDelRolAsync();
        }
        catch (Exception ex)
        {
            MensajeGeneral = $"No se pudo crear el permiso: {ex.Message}";
            EsMensajeExito = false;
        }
        finally
        {
            EstaOcupado = false;
        }
    }
}
