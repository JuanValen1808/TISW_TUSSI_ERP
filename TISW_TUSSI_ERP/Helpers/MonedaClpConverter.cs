
using System.Globalization;

namespace TISW_TUSSI_ERP.Helpers;

// Solo cambia la presentación del monto.
// No modifica los valores guardados en MySQL.
public sealed class MonedaClpConverter : IValueConverter
{
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (value is decimal monto)
            return Formato.Moneda(monto) + " CLP";

        return string.Empty;
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
        => Binding.DoNothing;
}
