using System.Net.Mail;
using System.Text.RegularExpressions;

namespace TISW_TUSSI_ERP.Helpers;

public static class Validaciones
{
    // Valida y formatea un RUT chileno con algoritmo Módulo 11
    public static bool ValidarRut(string? rutInput, out string rutFormateado, out string error)
    {
        rutFormateado = string.Empty;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(rutInput))
        {
            error = "El RUT es obligatorio.";
            return false;
        }

        var limpio = rutInput.Replace(".", "").Replace("-", "").Trim().ToUpperInvariant();
        if (limpio.Length < 8 || limpio.Length > 9)
        {
            error = "El RUT debe tener entre 8 y 9 caracteres (ej: 12.345.678-9).";
            return false;
        }

        var cuerpo = limpio[..^1];
        var dv = limpio[^1];

        if (!int.TryParse(cuerpo, out var numero))
        {
            error = "El cuerpo del RUT solo debe contener números.";
            return false;
        }

        // Cálculo dígito verificador Módulo 11
        int suma = 0;
        int multiplicador = 2;
        for (int i = cuerpo.Length - 1; i >= 0; i--)
        {
            suma += (cuerpo[i] - '0') * multiplicador;
            multiplicador = multiplicador == 7 ? 2 : multiplicador + 1;
        }

        int resto = 11 - (suma % 11);
        char dvCalculado = resto switch
        {
            11 => '0',
            10 => 'K',
            _ => (char)('0' + resto)
        };

        if (dv != dvCalculado)
        {
            error = $"El dígito verificador no es válido (esperado: {dvCalculado}).";
            return false;
        }

        rutFormateado = string.Format("{0:N0}-{1}", numero, dv).Replace(",", ".");
        return true;
    }

    public static bool ValidarEmail(string? email, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(email))
        {
            error = "El correo electrónico es obligatorio.";
            return false;
        }

        var limpio = email.Trim();
        var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
        if (!regex.IsMatch(limpio))
        {
            error = "El formato de correo no es válido (ej: usuario@erp.cl).";
            return false;
        }

        try
        {
            var addr = new MailAddress(limpio);
            if (addr.Address != limpio)
            {
                error = "El formato del correo es inválido.";
                return false;
            }
        }
        catch
        {
            error = "El correo no tiene un formato válido.";
            return false;
        }

        return true;
    }

    public static bool ValidarTextoRequerido(string? valor, string nombreCampo, int minLongitud, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(valor))
        {
            error = $"{nombreCampo} es obligatorio.";
            return false;
        }

        if (valor.Trim().Length < minLongitud)
        {
            error = $"{nombreCampo} debe tener al menos {minLongitud} caracteres.";
            return false;
        }

        return true;
    }

    public static bool ValidarPassword(string? password, bool obligatoria, out string error)
    {
        error = string.Empty;
        if (obligatoria && string.IsNullOrWhiteSpace(password))
        {
            error = "La contraseña es requerida.";
            return false;
        }

        if (!string.IsNullOrEmpty(password) && password.Length < 4)
        {
            error = "La contraseña debe tener un mínimo de 4 caracteres.";
            return false;
        }

        return true;
    }
}
