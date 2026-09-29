namespace TISW_TUSSI_ERP.Models.Auth;

// Guarda en memoria el usuario que inició sesión durante el uso de la app
public static class Sesion
{
    public static Usuario? UsuarioActual { get; set; }

    public static bool HaySesionActiva => UsuarioActual is not null;

    public static void CerrarSesion() => UsuarioActual = null;
}
