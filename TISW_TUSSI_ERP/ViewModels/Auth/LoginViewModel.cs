using System.Security.Cryptography;
using System.Text;
using System.Windows.Input;
using TISW_TUSSI_ERP.Models.Auth;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Auth;

public class LoginViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;

    private string _email = string.Empty;
    public string Email { get => _email; set => SetProperty(ref _email, value); }

    private string _contrasena = string.Empty;
    public string Contrasena { get => _contrasena; set => SetProperty(ref _contrasena, value); }

    private bool _ocultarContrasena = true;
    public bool OcultarContrasena { get => _ocultarContrasena; set => SetProperty(ref _ocultarContrasena, value); }

    private bool _recordarSesion;
    public bool RecordarSesion { get => _recordarSesion; set => SetProperty(ref _recordarSesion, value); }

    private string _mensajeError = string.Empty;
    public string MensajeError
    {
        get => _mensajeError;
        set { if (SetProperty(ref _mensajeError, value)) OnPropertyChanged(nameof(HayError)); }
    }
    public bool HayError => !string.IsNullOrEmpty(MensajeError);

    // Perfiles del "Acceso rápido de demostración" (coinciden con los usuarios de init.sql)
    public List<PerfilDemo> Perfiles { get; } = new()
    {
        new() { Iniciales = "AD", Rol = "Administrador",     Email = "admin@erp.cl",    Clave = "admin123" },
        new() { Iniciales = "CA", Rol = "Cajero / Vendedor", Email = "ventas@erp.cl",   Clave = "ventas123" },
        new() { Iniciales = "BO", Rol = "Bodega",            Email = "bodega@erp.cl",   Clave = "bodega123" },
        new() { Iniciales = "CO", Rol = "Contador",          Email = "contador@erp.cl", Clave = "contador123" },
    };

    public ICommand ComandoIniciarSesion { get; }
    public ICommand ComandoUsarPerfil { get; }
    public ICommand ComandoAlternarContrasena { get; }

    public LoginViewModel(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        Titulo = "Iniciar sesión";

        RecordarSesion = Preferences.Get("recordar_sesion", false);
        if (RecordarSesion) Email = Preferences.Get("email_guardado", string.Empty);

        ComandoIniciarSesion = new Command(async () => await IniciarSesionAsync());
        ComandoUsarPerfil = new Command<PerfilDemo>(p =>
        {
            if (p is null) return;
            Email = p.Email;
            Contrasena = p.Clave;
            MensajeError = string.Empty;
        });
        ComandoAlternarContrasena = new Command(() => OcultarContrasena = !OcultarContrasena);
    }

    private async Task IniciarSesionAsync()
    {
        if (EstaOcupado) return;

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Contrasena))
        {
            MensajeError = "Debes ingresar tu correo y contraseña.";
            return;
        }

        EstaOcupado = true;
        MensajeError = string.Empty;

        try
        {
            // Aseguramos eliminar cualquier espacio invisible antes de encriptar
            var hash = CalcularHash(Contrasena.Trim());
            var usuario = await _databaseService.ValidarCredencialesAsync(Email.Trim(), hash);

            if (usuario is null)
            {
                MensajeError = "Correo o contraseña incorrectos.";
                return;
            }

            if (RecordarSesion)
            {
                Preferences.Set("recordar_sesion", true);
                Preferences.Set("email_guardado", Email.Trim());
            }
            else
            {
                Preferences.Remove("recordar_sesion");
                Preferences.Remove("email_guardado");
            }

            Sesion.UsuarioActual = usuario;
            Application.Current!.MainPage = new AppShell();
        }
        catch (Exception ex)
        {
            MensajeError = $"No se pudo conectar a la base de datos: {ex.Message}";
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    // SHA-256 en hexadecimal minúscula: coincide con SHA2(clave, 256) de MySQL en init.sql.
    // (En producción usar BCrypt/Argon2 con sal.)
    private static string CalcularHash(string texto)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(texto))).ToLowerInvariant();
}
