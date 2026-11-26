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

        // 3. Crear archivo en la carpeta de reportes del usuario (Documentos\FarmSalud ERP\Reportes)
        var nombreArchivo = $"{nombreBaseArchivo}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        var rutaArchivo = Path.Combine(ExportadorReportes.CarpetaReportes, nombreArchivo);

        // UTF8 con BOM para que Excel en Windows y Mac reconozca la codificación automáticamente
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        await File.WriteAllTextAsync(rutaArchivo, sb.ToString(), encoding);

        // 4. Abrir el archivo de manera nativa en el dispositivo
#if WINDOWS
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = rutaArchivo,
                UseShellExecute = true
            });
            return;
        }
        catch { }
#endif
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

    public static async Task ExportarPdfAsync(string nombreBaseArchivo, string[] encabezados, IEnumerable<string[]> filas)
    {
        var cols = encabezados.Select(h => new ColumnaReporte(h)).ToArray();
        var sec = new SeccionReporte(nombreBaseArchivo.Replace("_", " "), cols);
        foreach (var fila in filas)
        {
            sec.Fila(fila.Cast<object?>().ToArray());
        }

        var doc = new DocumentoReporte
        {
            Titulo = nombreBaseArchivo.Replace("_", " "),
            Subtitulo = "Reporte oficial generado desde T.U.S.S.I. ERP Farmacéutico",
            Horizontal = encabezados.Length > 5,
            Secciones = { sec }
        };

        await ExportadorReportes.ExportarPdfAsync(doc);
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

