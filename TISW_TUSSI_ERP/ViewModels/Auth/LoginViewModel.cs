using System.Security.Cryptography;
using System.Text;
using System.Windows.Input;
using TISW_TUSSI_ERP.Models.Auth;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Auth;

public class LoginViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;

    // El login se hace con el email registrado en la tabla usuarios
    public string Email { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;

    private string _mensajeError = string.Empty;
    public string MensajeError
    {
        get => _mensajeError;
        set => SetProperty(ref _mensajeError, value);
    }

    public ICommand ComandoIniciarSesion { get; }

    public LoginViewModel(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        Titulo = "Iniciar sesión";
        ComandoIniciarSesion = new Command(async () => await IniciarSesionAsync());
    }

    private async Task IniciarSesionAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Contrasena))
        {
            MensajeError = "Debes ingresar tu correo y contraseña.";
            return;
        }

        EstaOcupado = true;
        MensajeError = string.Empty;

        try
        {
            var hash = CalcularHash(Contrasena);
            var usuarioValidado = await _databaseService.ValidarCredencialesAsync(Email, hash);

            if (usuarioValidado is null)
            {
                MensajeError = "Correo o contraseña incorrectos.";
                return;
            }

            // Guarda la sesión activa y cambia la raíz de la app al Shell (Dashboard)
            Sesion.UsuarioActual = usuarioValidado;
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

    // Nota: usar BCrypt/Identity en un proyecto real. SHA-256 aquí es solo
    // para que coincida con el dato semilla de init.sql durante el desarrollo.
    private static string CalcularHash(string texto)
    {
        var bytes = Encoding.UTF8.GetBytes(texto);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
