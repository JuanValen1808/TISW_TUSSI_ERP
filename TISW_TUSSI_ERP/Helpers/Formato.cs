using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace TISW_TUSSI_ERP.Helpers;

// Formatos y colores compartidos por todas las pantallas.
public static class Formato
{
    public static readonly CultureInfo Cultura = new("es-CL");

    // Moneda CLP: $12.480  (si tu equipo usa otra moneda, se cambia solo aquí)
    public static string Moneda(decimal valor) => valor.ToString("C0", Cultura);

    public static string MonedaCorta(decimal valor)
    {
        if (valor >= 1_000_000m) return "$" + (valor / 1_000_000m).ToString("0.0", Cultura) + "M";
        if (valor >= 1_000m) return "$" + (valor / 1_000m).ToString("0", Cultura) + "k";
        return "$" + valor.ToString("0", Cultura);
    }

    public static string Iniciales(string nombre)
    {
        var partes = (nombre ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length == 0) return "?";
        if (partes.Length == 1) return partes[0][..1].ToUpperInvariant();
        return (partes[0][..1] + partes[1][..1]).ToUpperInvariant();
    }

    public static string Capitalizar(string texto)
        => string.IsNullOrEmpty(texto) ? texto : char.ToUpper(texto[0], Cultura) + texto[1..];

    public static string EncriptarSHA256(string texto)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(texto));
        var builder = new StringBuilder();
        foreach (var b in bytes)
        {
            builder.Append(b.ToString("x2"));
        }
        return builder.ToString();
    }
}

public static class Paleta
{
    public static readonly Color Primario = Color.FromArgb("#1D6FD8");
    public static readonly Color Exito = Color.FromArgb("#16A34A");
    public static readonly Color Peligro = Color.FromArgb("#DC2626");
    public static readonly Color Advertencia = Color.FromArgb("#F59E0B");
    public static readonly Color Gris = Color.FromArgb("#6B7280");
}
