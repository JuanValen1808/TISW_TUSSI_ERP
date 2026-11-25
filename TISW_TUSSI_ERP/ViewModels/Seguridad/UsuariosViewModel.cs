using System.Collections.ObjectModel;
using System.Windows.Input;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Seguridad;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Seguridad;

public class UsuariosViewModel : BaseViewModel
{
    private readonly SeguridadService _seguridadService;

    public ObservableCollection<UsuarioModel> Usuarios { get; } = new();
    public ObservableCollection<RolModel> Roles { get; } = new();
    public ObservableCollection<RolModel> RolesFiltro { get; } = new();

    private string _textoBusqueda = string.Empty;
    public string TextoBusqueda
    {
        get => _textoBusqueda;
        set
        {
            if (SetProperty(ref _textoBusqueda, value))
            {
                _ = FiltrarUsuariosLocalmenteAsync();
            }
        }
    }

    private RolModel? _rolFiltroSeleccionado;
    public RolModel? RolFiltroSeleccionado
    {
        get => _rolFiltroSeleccionado;
        set
        {
            if (SetProperty(ref _rolFiltroSeleccionado, value))
            {
                _ = CargarUsuariosAsync();
            }
        }
    }

    // Estado del formulario (Modal)
    private bool _mostrarFormulario;
    public bool MostrarFormulario
    {
        get => _mostrarFormulario;
        set => SetProperty(ref _mostrarFormulario, value);
    }

    private bool _esEdicion;
    public bool EsEdicion
    {
        get => _esEdicion;
        set
        {
            if (SetProperty(ref _esEdicion, value))
            {
                OnPropertyChanged(nameof(TituloFormulario));
                OnPropertyChanged(nameof(TextoBotonGuardar));
            }
        }
    }

    public string TituloFormulario => EsEdicion ? "Modificar Usuario" : "Registrar Nuevo Usuario";
    public string TextoBotonGuardar => EsEdicion ? "Guardar Cambios" : "Crear Usuario";

    // Campos del formulario
    private int _formIdUsuario;

    private string _formRut = string.Empty;
    public string FormRut
    {
        get => _formRut;
        set
        {
            if (SetProperty(ref _formRut, value))
            {
                ErrorRut = string.Empty;
            }
        }
    }

    private string _formNombre = string.Empty;
    public string FormNombre
    {
        get => _formNombre;
        set
        {
            if (SetProperty(ref _formNombre, value))
            {
                ErrorNombre = string.Empty;
            }
        }
    }

    private string _formEmail = string.Empty;
    public string FormEmail
    {
        get => _formEmail;
        set
        {
            if (SetProperty(ref _formEmail, value))
            {
                ErrorEmail = string.Empty;
            }
        }
    }

    private string _formPassword = string.Empty;
    public string FormPassword
    {
        get => _formPassword;
        set
        {
            if (SetProperty(ref _formPassword, value))
            {
                ErrorPassword = string.Empty;
            }
        }
    }

    private RolModel? _formRolSeleccionado;
    public RolModel? FormRolSeleccionado
    {
        get => _formRolSeleccionado;
        set
        {
            if (SetProperty(ref _formRolSeleccionado, value))
            {
                ErrorRol = string.Empty;
            }
        }
    }

    private bool _formActivo = true;
    public bool FormActivo
    {
        get => _formActivo;
        set => SetProperty(ref _formActivo, value);
    }

    // Errores de validación
    private string _errorRut = string.Empty;
    public string ErrorRut
    {
        get => _errorRut;
        set { if (SetProperty(ref _errorRut, value)) OnPropertyChanged(nameof(HayErrorRut)); }
    }
    public bool HayErrorRut => !string.IsNullOrEmpty(ErrorRut);

    private string _errorNombre = string.Empty;
    public string ErrorNombre
    {
        get => _errorNombre;
        set { if (SetProperty(ref _errorNombre, value)) OnPropertyChanged(nameof(HayErrorNombre)); }
    }
    public bool HayErrorNombre => !string.IsNullOrEmpty(ErrorNombre);

    private string _errorEmail = string.Empty;
    public string ErrorEmail
    {
        get => _errorEmail;
        set { if (SetProperty(ref _errorEmail, value)) OnPropertyChanged(nameof(HayErrorEmail)); }
    }
    public bool HayErrorEmail => !string.IsNullOrEmpty(ErrorEmail);

    private string _errorPassword = string.Empty;
    public string ErrorPassword
    {
        get => _errorPassword;
        set { if (SetProperty(ref _errorPassword, value)) OnPropertyChanged(nameof(HayErrorPassword)); }
    }
    public bool HayErrorPassword => !string.IsNullOrEmpty(ErrorPassword);

    private string _errorRol = string.Empty;
    public string ErrorRol
    {
        get => _errorRol;
        set { if (SetProperty(ref _errorRol, value)) OnPropertyChanged(nameof(HayErrorRol)); }
    }
    public bool HayErrorRol => !string.IsNullOrEmpty(ErrorRol);

    public bool NoHayUsuarios => Usuarios.Count == 0 && !EstaOcupado;

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

    // Modal de Asignación de Poderes y Vistas por Usuario/Perfil
    private bool _mostrarModalPoderes;
    public bool MostrarModalPoderes
    {
        get => _mostrarModalPoderes;
        set => SetProperty(ref _mostrarModalPoderes, value);
    }

    private UsuarioModel? _usuarioPoderesSeleccionado;
    public UsuarioModel? UsuarioPoderesSeleccionado
    {
        get => _usuarioPoderesSeleccionado;
        set
        {
            if (SetProperty(ref _usuarioPoderesSeleccionado, value))
                OnPropertyChanged(nameof(TituloModalPoderes));
        }
    }

    public string TituloModalPoderes => UsuarioPoderesSeleccionado != null
        ? $"Asignar Poderes y Vistas — {UsuarioPoderesSeleccionado.Nombre}"
        : "Asignar Poderes y Vistas";

    private RolModel? _rolPoderesSeleccionado;
    public RolModel? RolPoderesSeleccionado
    {
        get => _rolPoderesSeleccionado;
        set
        {
            if (SetProperty(ref _rolPoderesSeleccionado, value) && value != null)
            {
                _ = CargarPermisosParaModalPoderesAsync(value.IdRol);
            }
        }
    }

    public ObservableCollection<ModuloPermisosGrupo> GruposPoderesUsuario { get; } = new();

    private string _vistasPoderesTexto = "Dashboard";
    public string VistasPoderesTexto
    {
        get => _vistasPoderesTexto;
        set => SetProperty(ref _vistasPoderesTexto, value);
    }

    // Comandos
    public ICommand ComandoCargar { get; }
    public ICommand ComandoNuevo { get; }
    public ICommand ComandoEditar { get; }
    public ICommand ComandoGuardar { get; }
    public ICommand ComandoCancelar { get; }
    public ICommand ComandoCambiarEstado { get; }
    public ICommand ComandoEliminar { get; }
    public ICommand ComandoLimpiarFiltros { get; }
    public ICommand ComandoAbrirPoderes { get; }
    public ICommand ComandoCerrarPoderes { get; }
    public ICommand ComandoGuardarPoderes { get; }
    public ICommand ComandoPresetPoderes { get; }

    public UsuariosViewModel(SeguridadService seguridadService)
    {
        _seguridadService = seguridadService;
        Titulo = "Mantenedor de Usuarios";

        ComandoCargar = new Command(async () => await CargarDatosInicialesAsync());
        ComandoNuevo = new Command(AbrirFormularioNuevo);
        ComandoEditar = new Command<UsuarioModel>(AbrirFormularioEdicion);
        ComandoGuardar = new Command(async () => await GuardarUsuarioAsync());
        ComandoCancelar = new Command(CerrarFormulario);
        ComandoCambiarEstado = new Command<UsuarioModel>(async u => await CambiarEstadoAsync(u));
        ComandoEliminar = new Command<UsuarioModel>(async u => await EliminarUsuarioAsync(u));
        ComandoLimpiarFiltros = new Command(async () =>
        {
            TextoBusqueda = string.Empty;
            RolFiltroSeleccionado = RolesFiltro.FirstOrDefault();
            await CargarUsuariosAsync();
        });
        ComandoAbrirPoderes = new Command<UsuarioModel>(async u => await AbrirModalPoderesAsync(u));
        ComandoCerrarPoderes = new Command(() => MostrarModalPoderes = false);
        ComandoGuardarPoderes = new Command(async () => await GuardarPoderesUsuarioAsync());
        ComandoPresetPoderes = new Command<string>(AplicarPresetPoderes);
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
            RolesFiltro.Clear();

            // Opción 'Todos los roles' para el filtro
            RolesFiltro.Add(new RolModel { IdRol = 0, NombreRol = "Todos los roles" });

            foreach (var r in rolesDb)
            {
                Roles.Add(r);
                RolesFiltro.Add(r);
            }

            int idPre = Preferences.Get("filtro_rol_usuarios", 0);
            if (idPre > 0)
            {
                Preferences.Remove("filtro_rol_usuarios");
                RolFiltroSeleccionado = RolesFiltro.FirstOrDefault(r => r.IdRol == idPre) ?? RolesFiltro[0];
            }
            else
            {
                RolFiltroSeleccionado = RolesFiltro[0];
            }
            await CargarUsuariosAsync();
        }
        catch (Exception ex)
        {
            MensajeGeneral = $"Error al cargar datos: {ex.Message}";
            EsMensajeExito = false;
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    public async Task CargarUsuariosAsync()
    {
        try
        {
            int? rolId = (RolFiltroSeleccionado != null && RolFiltroSeleccionado.IdRol > 0)
                ? RolFiltroSeleccionado.IdRol
                : null;

            var lista = await _seguridadService.ObtenerUsuariosAsync(TextoBusqueda, rolId);
            Usuarios.Clear();
            foreach (var u in lista)
            {
                Usuarios.Add(u);
            }
        }
        catch (Exception ex)
        {
            MensajeGeneral = $"Error al obtener usuarios: {ex.Message}";
            EsMensajeExito = false;
        }
    }

    private async Task FiltrarUsuariosLocalmenteAsync()
    {
        await CargarUsuariosAsync();
    }

    private void AbrirFormularioNuevo()
    {
        _formIdUsuario = 0;
        FormRut = string.Empty;
        FormNombre = string.Empty;
        FormEmail = string.Empty;
        FormPassword = string.Empty;
        FormRolSeleccionado = Roles.FirstOrDefault();
        FormActivo = true;

        LimpiarErroresValidacion();
        EsEdicion = false;
        MostrarFormulario = true;
    }

    private void AbrirFormularioEdicion(UsuarioModel? usuario)
    {
        if (usuario is null) return;

        _formIdUsuario = usuario.IdUsuario;
        FormRut = usuario.RutUsuario;
        FormNombre = usuario.Nombre;
        FormEmail = usuario.Email;
        FormPassword = string.Empty; // No cargamos el hash por seguridad
        FormRolSeleccionado = Roles.FirstOrDefault(r => r.IdRol == usuario.IdRol);
        FormActivo = usuario.Activo;

        LimpiarErroresValidacion();
        EsEdicion = true;
        MostrarFormulario = true;
    }

    private void CerrarFormulario()
    {
        MostrarFormulario = false;
        LimpiarErroresValidacion();
    }

    private void LimpiarErroresValidacion()
    {
        ErrorRut = string.Empty;
        ErrorNombre = string.Empty;
        ErrorEmail = string.Empty;
        ErrorPassword = string.Empty;
        ErrorRol = string.Empty;
    }

    private async Task GuardarUsuarioAsync()
    {
        LimpiarErroresValidacion();
        bool esValido = true;

        // 1. Validar RUT
        if (!Validaciones.ValidarRut(FormRut, out var rutFormateado, out var errRut))
        {
            ErrorRut = errRut;
            esValido = false;
        }
        else
        {
            FormRut = rutFormateado;
            // Comprobar RUT único
            if (await _seguridadService.ExisteRutAsync(FormRut, _formIdUsuario > 0 ? _formIdUsuario : null))
            {
                ErrorRut = "Ya existe un usuario registrado con este RUT.";
                esValido = false;
            }
        }

        // 2. Validar Nombre
        if (!Validaciones.ValidarTextoRequerido(FormNombre, "El nombre", 3, out var errNom))
        {
            ErrorNombre = errNom;
            esValido = false;
        }

        // 3. Validar Email
        if (!Validaciones.ValidarEmail(FormEmail, out var errMail))
        {
            ErrorEmail = errMail;
            esValido = false;
        }
        else
        {
            // Comprobar Email único
            if (await _seguridadService.ExisteEmailAsync(FormEmail, _formIdUsuario > 0 ? _formIdUsuario : null))
            {
                ErrorEmail = "Este correo electrónico ya está registrado por otro usuario.";
                esValido = false;
            }
        }

        // 4. Validar Rol
        if (FormRolSeleccionado is null || FormRolSeleccionado.IdRol <= 0)
        {
            ErrorRol = "Debe asignar un perfil o rol al usuario.";
            esValido = false;
        }

        // 5. Validar Password
        bool passwordObligatoria = _formIdUsuario == 0;
        if (!Validaciones.ValidarPassword(FormPassword, passwordObligatoria, out var errPass))
        {
            ErrorPassword = errPass;
            esValido = false;
        }

        if (!esValido) return;

        EstaOcupado = true;
        try
        {
            var modelo = new UsuarioModel
            {
                IdUsuario = _formIdUsuario,
                RutUsuario = FormRut,
                Nombre = FormNombre.Trim(),
                Email = FormEmail.Trim().ToLowerInvariant(),
                IdRol = FormRolSeleccionado!.IdRol,
                NombreRol = FormRolSeleccionado.NombreRol,
                Activo = FormActivo
            };

            await _seguridadService.GuardarUsuarioAsync(modelo, string.IsNullOrWhiteSpace(FormPassword) ? null : FormPassword);

            MostrarFormulario = false;
            MensajeGeneral = _formIdUsuario == 0
                ? $"Usuario '{modelo.Nombre}' registrado exitosamente."
                : $"Usuario '{modelo.Nombre}' actualizado exitosamente.";
            EsMensajeExito = true;

            await CargarUsuariosAsync();
        }
        catch (Exception ex)
        {
            MensajeGeneral = $"Error al guardar usuario: {ex.Message}";
            EsMensajeExito = false;
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private async Task CambiarEstadoAsync(UsuarioModel? usuario)
    {
        if (usuario is null || EstaOcupado) return;

        bool nuevoEstado = !usuario.Activo;
        string accion = nuevoEstado ? "activar" : "desactivar";

        bool confirma = await Application.Current!.Windows[0].Page!.DisplayAlertAsync(
            "Confirmación",
            $"¿Desea {accion} el acceso al usuario '{usuario.Nombre}'?",
            "Confirmar",
            "Cancelar");

        if (!confirma) return;

        EstaOcupado = true;
        try
        {
            await _seguridadService.CambiarEstadoUsuarioAsync(usuario.IdUsuario, nuevoEstado);
            usuario.Activo = nuevoEstado;
            MensajeGeneral = $"El usuario '{usuario.Nombre}' ahora está {(nuevoEstado ? "activo" : "inactivo")}.";
            EsMensajeExito = true;
        }
        catch (Exception ex)
        {
            MensajeGeneral = $"No se pudo cambiar el estado: {ex.Message}";
            EsMensajeExito = false;
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private async Task EliminarUsuarioAsync(UsuarioModel? usuario)
    {
        if (usuario is null || EstaOcupado) return;

        bool confirma = await Application.Current!.Windows[0].Page!.DisplayAlertAsync(
            "Eliminar Usuario",
            $"¿Está seguro de eliminar definitivamente al usuario '{usuario.Nombre}' ({usuario.RutUsuario})?\n\nEsta acción no se puede deshacer.",
            "Eliminar",
            "Cancelar");

        if (!confirma) return;

        EstaOcupado = true;
        try
        {
            await _seguridadService.EliminarUsuarioAsync(usuario.IdUsuario);
            Usuarios.Remove(usuario);
            MensajeGeneral = $"Usuario '{usuario.Nombre}' eliminado correctamente.";
            EsMensajeExito = true;
        }
        catch (InvalidOperationException exOp)
        {
            MensajeGeneral = exOp.Message;
            EsMensajeExito = false;
            await Application.Current!.Windows[0].Page!.DisplayAlertAsync(
                "Aviso del Sistema",
                exOp.Message,
                "Entendido");
        }
        catch (Exception ex)
        {
            MensajeGeneral = $"Error al eliminar usuario: {ex.Message}";
            EsMensajeExito = false;
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private async Task AbrirModalPoderesAsync(UsuarioModel? usuario)
    {
        if (usuario is null) return;
        UsuarioPoderesSeleccionado = usuario;
        var rol = Roles.FirstOrDefault(r => r.IdRol == usuario.IdRol) ?? Roles.FirstOrDefault();
        _rolPoderesSeleccionado = rol;
        OnPropertyChanged(nameof(RolPoderesSeleccionado));
        if (rol != null)
        {
            await CargarPermisosParaModalPoderesAsync(rol.IdRol);
        }
        MostrarModalPoderes = true;
    }

    private async Task CargarPermisosParaModalPoderesAsync(int idRol)
    {
        try
        {
            var lista = await _seguridadService.ObtenerPermisosPorRolAsync(idRol);
            foreach (var p in lista)
            {
                p.OnAsignadoCambiado = ActualizarVistaPreviaPoderes;
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

            GruposPoderesUsuario.Clear();
            foreach (var g in agrupados)
                GruposPoderesUsuario.Add(g);

            ActualizarVistaPreviaPoderes();
        }
        catch (Exception ex)
        {
            MensajeGeneral = $"Error al cargar poderes: {ex.Message}";
            EsMensajeExito = false;
        }
    }

    private void AplicarPresetPoderes(string? moduloPreset)
    {
        if (string.IsNullOrWhiteSpace(moduloPreset)) return;

        foreach (var grupo in GruposPoderesUsuario)
        {
            bool habilitar = moduloPreset.Equals("Total", StringComparison.OrdinalIgnoreCase)
                             || grupo.NombreModulo.Equals(moduloPreset, StringComparison.OrdinalIgnoreCase);
            foreach (var p in grupo.Permisos)
            {
                p.Asignado = habilitar;
            }
            grupo.NotificarCambios();
        }
        ActualizarVistaPreviaPoderes();
    }

    private void ActualizarVistaPreviaPoderes()
    {
        var todos = GruposPoderesUsuario.SelectMany(g => g.Permisos).ToList();
        var codigosActivos = new HashSet<string>(
            todos.Where(p => p.Asignado).Select(p => p.CodigoPermiso),
            StringComparer.OrdinalIgnoreCase);

        bool esAdmin = RolPoderesSeleccionado != null && RolPoderesSeleccionado.NombreRol.Equals("Administrador", StringComparison.OrdinalIgnoreCase);

        var vistas = new List<string> { "Dashboard" };
        foreach (var sec in AppShell.Secciones)
        {
            if (sec.Nombre == "PRINCIPAL") continue;
            foreach (var item in sec.Items)
            {
                if (esAdmin || (!string.IsNullOrEmpty(item.Permiso) && codigosActivos.Contains(item.Permiso)))
                {
                    vistas.Add($"[{sec.Nombre}] {item.Titulo}");
                }
            }
        }

        VistasPoderesTexto = vistas.Count > 1
            ? string.Join("  ·  ", vistas)
            : "Solo Dashboard (Principal)";
    }

    private async Task GuardarPoderesUsuarioAsync()
    {
        if (UsuarioPoderesSeleccionado is null || RolPoderesSeleccionado is null || EstaOcupado)
            return;

        EstaOcupado = true;
        try
        {
            // 1. Si el administrador cambió el rol/perfil del usuario, actualizamos el usuario
            if (UsuarioPoderesSeleccionado.IdRol != RolPoderesSeleccionado.IdRol)
            {
                UsuarioPoderesSeleccionado.IdRol = RolPoderesSeleccionado.IdRol;
                UsuarioPoderesSeleccionado.NombreRol = RolPoderesSeleccionado.NombreRol;
                await _seguridadService.GuardarUsuarioAsync(UsuarioPoderesSeleccionado, null);
            }

            // 2. Guardamos los permisos/poderes marcados para ese rol
            var idsHabilitados = GruposPoderesUsuario
                .SelectMany(g => g.Permisos)
                .Where(p => p.Asignado)
                .Select(p => p.IdPermiso)
                .ToList();

            await _seguridadService.GuardarPermisosRolAsync(RolPoderesSeleccionado.IdRol, idsHabilitados);

            if (Models.Auth.Sesion.UsuarioActual != null && Models.Auth.Sesion.UsuarioActual.IdRol == RolPoderesSeleccionado.IdRol)
            {
                Models.Auth.Sesion.UsuarioActual.Permisos.Clear();
                foreach (var cod in GruposPoderesUsuario.SelectMany(g => g.Permisos).Where(p => p.Asignado).Select(p => p.CodigoPermiso))
                {
                    Models.Auth.Sesion.UsuarioActual.Permisos.Add(cod);
                }
                AppShell.RecargarMenuActual();
            }

            MostrarModalPoderes = false;
            MensajeGeneral = $"Poderes y vistas actualizados correctamente para '{UsuarioPoderesSeleccionado.Nombre}' (Perfil: {RolPoderesSeleccionado.NombreRol}).";
            EsMensajeExito = true;

            await CargarUsuariosAsync();
        }
        catch (Exception ex)
        {
            MensajeGeneral = $"Error al guardar poderes: {ex.Message}";
            EsMensajeExito = false;
        }
        finally
        {
            EstaOcupado = false;
        }
    }
}
