using System.Text;

namespace TISW_TUSSI_ERP.Helpers;

/// <summary>
/// Helper seguro y nativo para la exportación de reportes a formato CSV.
/// No requiere librerías externas vulnerables y permite abrir el archivo directamente en Excel.
/// Incluye BOM UTF-8 para garantizar la correcta visualización de tildes, caracteres 'ñ' y símbolos.
/// </summary>
public static class CsvExportHelper
{
    public static async Task ExportarYCompartirAsync(string nombreBaseArchivo, string[] encabezados, IEnumerable<string[]> filas)
    {
        var sb = new StringBuilder();

        // 1. Escribir encabezados con formato CSV seguro (RFC 4180)
        sb.AppendLine(string.Join(";", encabezados.Select(EscaparCsv)));

        // 2. Escribir filas
        foreach (var fila in filas)
        {
            sb.AppendLine(string.Join(";", fila.Select(EscaparCsv)));
        }

        // 3. Crear archivo temporal en el almacenamiento local seguro de la aplicación
        var nombreArchivo = $"{nombreBaseArchivo}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        var rutaArchivo = Path.Combine(FileSystem.CacheDirectory, nombreArchivo);

        // UTF8 con BOM para que Excel en Windows y Mac reconozca la codificación automáticamente
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        await File.WriteAllTextAsync(rutaArchivo, sb.ToString(), encoding);

        // 4. Abrir o compartir el archivo de manera nativa en el dispositivo
        try
        {
            await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                Title = $"Abrir reporte: {nombreBaseArchivo}",
                File = new ReadOnlyFile(rutaArchivo)
            });
        }
        catch
        {
            // Fallback: compartir el archivo si la apertura directa no es admitida por el SO
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = $"Exportación: {nombreBaseArchivo}",
                File = new ShareFile(rutaArchivo)
            });
        }
    }

    private static string EscaparCsv(string? campo)
    {
        if (string.IsNullOrEmpty(campo)) return "";

        var valor = campo.Replace("\"", "\"\"");
        if (valor.Contains(';') || valor.Contains('"') || valor.Contains('\n') || valor.Contains('\r'))
        {
            return $"\"{valor}\"";
        }
        return valor;
    }
}

