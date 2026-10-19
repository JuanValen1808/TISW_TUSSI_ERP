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

    private async Task IrAAccesosAsync(RolModel? rol)
    {
        if (rol is null) return;
        Preferences.Set("perfil_seleccionado_accesos", rol.IdRol);
        await Shell.Current.GoToAsync("//AccesosPage");
    }
}
