
using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace TISW_TUSSI_ERP.Helpers;

public class DecimalSeguroConverter : IValueConverter
{
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (value is decimal numero)
        {
            return numero.ToString(
                "0.##",
                CultureInfo.InvariantCulture);
        }

        return "0";
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        string texto = value?.ToString()?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(texto))
            return 0m;

        texto = texto.Replace(',', '.');

        if (decimal.TryParse(
            texto,
            NumberStyles.AllowDecimalPoint |
            NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture,
            out decimal resultado))
        {
            return resultado;
        }

        return BindableProperty.UnsetValue;
    }
}
