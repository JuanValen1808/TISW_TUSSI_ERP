using System.Collections.ObjectModel;
using System.Windows.Input;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Seguridad;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Seguridad;

public class PerfilesViewModel : BaseViewModel
{
    private readonly SeguridadService _seguridadService;

    public ObservableCollection<RolModel> Roles { get; } = new();

    private string _textoBusqueda = string.Empty;
    public string TextoBusqueda
    {
        get => _textoBusqueda;
        set
        {
            if (SetProperty(ref _textoBusqueda, value))
            {
                _ = CargarPerfilesAsync();
            }
        }
    }

    // Modal de edición / creación
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

    public string TituloFormulario => EsEdicion ? "Modificar Perfil" : "Registrar Nuevo Perfil";
    public string TextoBotonGuardar => EsEdicion ? "Guardar Cambios" : "Crear Perfil";

    // Campos formulario
    private int _formIdRol;

    private string _formNombreRol = string.Empty;
    public string FormNombreRol
    {
        get => _formNombreRol;
        set
        {
            if (SetProperty(ref _formNombreRol, value))
            {
                ErrorNombre = string.Empty;
            }
        }
    }

    private string _formDescripcion = string.Empty;
    public string FormDescripcion
    {
        get => _formDescripcion;
        set
        {
            if (SetProperty(ref _formDescripcion, value))
            {
                ErrorDescripcion = string.Empty;
            }
        }
    }

    // Modal para agregar usuario directo al perfil
    private bool _mostrarModalAgregarUsuario;
    public bool MostrarModalAgregarUsuario
    {
        get => _mostrarModalAgregarUsuario;
        set => SetProperty(ref _mostrarModalAgregarUsuario, value);
    }

    private RolModel? _rolSeleccionadoParaUsuario;
    public RolModel? RolSeleccionadoParaUsuario
    {
        get => _rolSeleccionadoParaUsuario;
        set => SetProperty(ref _rolSeleccionadoParaUsuario, value);
    }

    private string _nuevoUsuarioRut = string.Empty;
    public string NuevoUsuarioRut
    {
        get => _nuevoUsuarioRut;
        set
        {
            if (SetProperty(ref _nuevoUsuarioRut, value))
            {
                ErrorNuevoUsuarioRut = string.Empty;
            }
        }
    }

    private string _nuevoUsuarioNombre = string.Empty;
    public string NuevoUsuarioNombre
    {
        get => _nuevoUsuarioNombre;
        set
        {
            if (SetProperty(ref _nuevoUsuarioNombre, value))
            {
                ErrorNuevoUsuarioNombre = string.Empty;
            }
        }
    }

    private string _nuevoUsuarioEmail = string.Empty;
    public string NuevoUsuarioEmail
    {
        get => _nuevoUsuarioEmail;
        set
        {
            if (SetProperty(ref _nuevoUsuarioEmail, value))
            {
                ErrorNuevoUsuarioEmail = string.Empty;
            }
        }
    }

    private string _nuevoUsuarioPassword = "1234";
    public string NuevoUsuarioPassword
    {
        get => _nuevoUsuarioPassword;
        set => SetProperty(ref _nuevoUsuarioPassword, value);
    }

    private bool _nuevoUsuarioActivo = true;
    public bool NuevoUsuarioActivo
    {
        get => _nuevoUsuarioActivo;
        set => SetProperty(ref _nuevoUsuarioActivo, value);
    }

    private string _errorNuevoUsuarioRut = string.Empty;
    public string ErrorNuevoUsuarioRut
    {
        get => _errorNuevoUsuarioRut;
        set { if (SetProperty(ref _errorNuevoUsuarioRut, value)) OnPropertyChanged(nameof(HayErrorNuevoUsuarioRut)); }
    }
    public bool HayErrorNuevoUsuarioRut => !string.IsNullOrEmpty(ErrorNuevoUsuarioRut);

    private string _errorNuevoUsuarioNombre = string.Empty;
    public string ErrorNuevoUsuarioNombre
    {
        get => _errorNuevoUsuarioNombre;
        set { if (SetProperty(ref _errorNuevoUsuarioNombre, value)) OnPropertyChanged(nameof(HayErrorNuevoUsuarioNombre)); }
    }
    public bool HayErrorNuevoUsuarioNombre => !string.IsNullOrEmpty(ErrorNuevoUsuarioNombre);

    private string _errorNuevoUsuarioEmail = string.Empty;
    public string ErrorNuevoUsuarioEmail
    {
        get => _errorNuevoUsuarioEmail;
        set { if (SetProperty(ref _errorNuevoUsuarioEmail, value)) OnPropertyChanged(nameof(HayErrorNuevoUsuarioEmail)); }
    }
    public bool HayErrorNuevoUsuarioEmail => !string.IsNullOrEmpty(ErrorNuevoUsuarioEmail);

    // Validaciones
    private string _errorNombre = string.Empty;
    public string ErrorNombre
    {
        get => _errorNombre;
        set { if (SetProperty(ref _errorNombre, value)) OnPropertyChanged(nameof(HayErrorNombre)); }
    }
    public bool HayErrorNombre => !string.IsNullOrEmpty(ErrorNombre);

    private string _errorDescripcion = string.Empty;
    public string ErrorDescripcion
    {
        get => _errorDescripcion;
        set { if (SetProperty(ref _errorDescripcion, value)) OnPropertyChanged(nameof(HayErrorDescripcion)); }
    }
    public bool HayErrorDescripcion => !string.IsNullOrEmpty(ErrorDescripcion);

    public bool NoHayRoles => Roles.Count == 0 && !EstaOcupado;

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
    public ICommand ComandoEliminar { get; }
    public ICommand ComandoGestionarAccesos { get; }
    public ICommand ComandoAbrirAgregarUsuario { get; }
    public ICommand ComandoCerrarModalAgregarUsuario { get; }
    public ICommand ComandoGuardarNuevoUsuarioPerfil { get; }
    public ICommand ComandoVerUsuarios { get; }

    public PerfilesViewModel(SeguridadService seguridadService)
    {
        _seguridadService = seguridadService;
        Titulo = "Mantenedor de Perfiles";

        ComandoCargar = new Command(async () => await CargarPerfilesAsync());
        ComandoNuevo = new Command(AbrirFormularioNuevo);
        ComandoEditar = new Command<RolModel>(AbrirFormularioEdicion);
        ComandoGuardar = new Command(async () => await GuardarPerfilAsync());
        ComandoCancelar = new Command(CerrarFormulario);
        ComandoEliminar = new Command<RolModel>(async r => await EliminarPerfilAsync(r));
        ComandoGestionarAccesos = new Command<RolModel>(async r => await IrAAccesosAsync(r));
        ComandoAbrirAgregarUsuario = new Command<RolModel>(AbrirModalAgregarUsuario);
        ComandoCerrarModalAgregarUsuario = new Command(() => MostrarModalAgregarUsuario = false);
        ComandoGuardarNuevoUsuarioPerfil = new Command(async () => await GuardarNuevoUsuarioPerfilAsync());
        ComandoVerUsuarios = new Command<RolModel>(async r =>
        {
            if (r is null) return;
            Preferences.Set("filtro_rol_usuarios", r.IdRol);
            await Shell.Current.GoToAsync("//UsuariosPage");
        });
    }

    public async Task CargarPerfilesAsync()
    {
        if (EstaOcupado) return;
        EstaOcupado = true;
        MensajeGeneral = string.Empty;

        try
        {
            var lista = await _seguridadService.ObtenerRolesAsync(TextoBusqueda);
            Roles.Clear();
            foreach (var r in lista)
            {
                Roles.Add(r);
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

    private void AbrirFormularioNuevo()
    {
        _formIdRol = 0;
        FormNombreRol = string.Empty;
        FormDescripcion = string.Empty;
        LimpiarErroresValidacion();
        EsEdicion = false;
        MostrarFormulario = true;
    }

    private void AbrirFormularioEdicion(RolModel? rol)
    {
        if (rol is null) return;

        _formIdRol = rol.IdRol;
        FormNombreRol = rol.NombreRol;
        FormDescripcion = rol.Descripcion;
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
        ErrorNombre = string.Empty;
        ErrorDescripcion = string.Empty;
    }

    private async Task GuardarPerfilAsync()
    {
        LimpiarErroresValidacion();
        bool esValido = true;

        if (!Validaciones.ValidarTextoRequerido(FormNombreRol, "El nombre del perfil", 3, out var errNom))
        {
            ErrorNombre = errNom;
            esValido = false;
        }

        if (string.IsNullOrWhiteSpace(FormDescripcion))
        {
            ErrorDescripcion = "Debe ingresar una descripción de las responsabilidades del perfil.";
            esValido = false;
        }

        if (!esValido) return;

        EstaOcupado = true;
        try
        {
            var modelo = new RolModel
            {
                IdRol = _formIdRol,
                NombreRol = FormNombreRol.Trim(),
                Descripcion = FormDescripcion.Trim()
            };

            await _seguridadService.GuardarRolAsync(modelo);

            MostrarFormulario = false;
            MensajeGeneral = _formIdRol == 0
                ? $"Perfil '{modelo.NombreRol}' creado exitosamente."
                : $"Perfil '{modelo.NombreRol}' actualizado exitosamente.";
            EsMensajeExito = true;

            await CargarPerfilesAsync();
        }
        catch (Exception ex)
        {
            MensajeGeneral = $"Error al guardar el perfil: {ex.Message}";
            EsMensajeExito = false;
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private async Task EliminarPerfilAsync(RolModel? rol)
    {
        if (rol is null || EstaOcupado) return;

        if (rol.NombreRol.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
        {
            await Application.Current!.Windows[0].Page!.DisplayAlertAsync(
                "Acción no permitida",
                "El perfil de Administrador del sistema es protegido y no puede ser eliminado.",
                "Entendido");
            return;
        }

        bool confirma = await Application.Current!.Windows[0].Page!.DisplayAlertAsync(
            "Eliminar Perfil",
            $"¿Está seguro de eliminar el perfil '{rol.NombreRol}'?\n\nSe eliminarán también todos los accesos configurados para este perfil.",
            "Eliminar",
            "Cancelar");

        if (!confirma) return;

        EstaOcupado = true;
        try
        {
            await _seguridadService.EliminarRolAsync(rol.IdRol);
            Roles.Remove(rol);
            MensajeGeneral = $"Perfil '{rol.NombreRol}' eliminado correctamente.";
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
            MensajeGeneral = $"Error al eliminar el perfil: {ex.Message}";
            EsMensajeExito = false;
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private void AbrirModalAgregarUsuario(RolModel? rol)
    {
        if (rol is null) return;
        RolSeleccionadoParaUsuario = rol;
        NuevoUsuarioRut = string.Empty;
        NuevoUsuarioNombre = string.Empty;
        NuevoUsuarioEmail = string.Empty;
        NuevoUsuarioPassword = "1234";
        NuevoUsuarioActivo = true;
        LimpiarErroresNuevoUsuario();
        MostrarModalAgregarUsuario = true;
    }

    private void LimpiarErroresNuevoUsuario()
    {
        ErrorNuevoUsuarioRut = string.Empty;
        ErrorNuevoUsuarioNombre = string.Empty;
        ErrorNuevoUsuarioEmail = string.Empty;
    }

    private async Task GuardarNuevoUsuarioPerfilAsync()
    {
        if (RolSeleccionadoParaUsuario is null) return;

        LimpiarErroresNuevoUsuario();
        bool esValido = true;

        string rutFinal = NuevoUsuarioRut;
        if (!Validaciones.ValidarRut(NuevoUsuarioRut, out var rutFormateado, out var errRut))
        {
            ErrorNuevoUsuarioRut = errRut;
            esValido = false;
        }
        else if (await _seguridadService.ExisteRutAsync(rutFormateado))
        {
            ErrorNuevoUsuarioRut = "Este RUT ya se encuentra registrado.";
            esValido = false;
        }
        else
        {
            rutFinal = rutFormateado;
        }

        if (!Validaciones.ValidarTextoRequerido(NuevoUsuarioNombre, "El nombre del usuario", 3, out var errNom))
        {
            ErrorNuevoUsuarioNombre = errNom;
            esValido = false;
        }

        if (!Validaciones.ValidarEmail(NuevoUsuarioEmail, out var errMail))
        {
            ErrorNuevoUsuarioEmail = errMail;
            esValido = false;
        }
        else if (await _seguridadService.ExisteEmailAsync(NuevoUsuarioEmail))
        {
            ErrorNuevoUsuarioEmail = "Este correo electrónico ya está registrado.";
            esValido = false;
        }

        if (!esValido) return;

        EstaOcupado = true;
        try
        {
            var clave = string.IsNullOrWhiteSpace(NuevoUsuarioPassword) ? "1234" : NuevoUsuarioPassword.Trim();
            var nuevo = new UsuarioModel
            {
                IdUsuario = 0,
                RutUsuario = rutFinal,
                Nombre = NuevoUsuarioNombre.Trim(),
                Email = NuevoUsuarioEmail.Trim(),
                IdRol = RolSeleccionadoParaUsuario.IdRol,
                NombreRol = RolSeleccionadoParaUsuario.NombreRol,
                Activo = NuevoUsuarioActivo
            };

            await _seguridadService.GuardarUsuarioAsync(nuevo, clave);

            MostrarModalAgregarUsuario = false;
            MensajeGeneral = $"Usuario '{nuevo.Nombre}' creado y asignado exitosamente al perfil '{RolSeleccionadoParaUsuario.NombreRol}'.";
            EsMensajeExito = true;

            await CargarPerfilesAsync();
        }
        catch (Exception ex)
        {
            MensajeGeneral = $"Error al registrar usuario: {ex.Message}";
            EsMensajeExito = false;
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private async Task IrAAccesosAsync(RolModel? rol)
    {
        if (rol is null) return;
        Preferences.Set("perfil_seleccionado_accesos", rol.IdRol);
        await Shell.Current.GoToAsync("//AccesosPage");
    }
}
