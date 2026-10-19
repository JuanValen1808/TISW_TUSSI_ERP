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

    // Comandos
    public ICommand ComandoCargar { get; }
    public ICommand ComandoNuevo { get; }
    public ICommand ComandoEditar { get; }
    public ICommand ComandoGuardar { get; }
    public ICommand ComandoCancelar { get; }
    public ICommand ComandoCambiarEstado { get; }
    public ICommand ComandoEliminar { get; }
    public ICommand ComandoLimpiarFiltros { get; }

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

            RolFiltroSeleccionado = RolesFiltro[0];
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
}
