using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TISW_TUSSI_ERP.Models.Auth;
using Colors = QuestPDF.Helpers.Colors;

namespace TISW_TUSSI_ERP.Helpers;

// ===================================================================
//  Modelo genérico de reporte exportable (PDF real con QuestPDF / CSV Excel)
//  Cada ViewModel de reporte arma un DocumentoReporte y el
//  ExportadorReportes se encarga de generar y abrir el archivo.
// ===================================================================

public enum TipoColumna { Texto, Moneda, Numero, Fecha, Porcentaje }

public record ColumnaReporte(string Titulo, TipoColumna Tipo = TipoColumna.Texto);

public class FilaReporte
{
    public object?[] Celdas { get; init; } = Array.Empty<object?>();
    public bool EsTotal { get; init; }
    public bool EsSubtitulo { get; init; }
    public int Nivel { get; init; }

    public FilaReporte() { }
    public FilaReporte(params object?[] celdas) => Celdas = celdas;
}

public class SeccionReporte
{
    public string Titulo { get; set; } = string.Empty;
    public List<ColumnaReporte> Columnas { get; } = new();
    public List<FilaReporte> Filas { get; } = new();

    public SeccionReporte(string titulo, params ColumnaReporte[] columnas)
    {
        Titulo = titulo;
        Columnas.AddRange(columnas);
    }

    public SeccionReporte Fila(params object?[] celdas) { Filas.Add(new FilaReporte(celdas)); return this; }
    public SeccionReporte Total(params object?[] celdas) { Filas.Add(new FilaReporte { Celdas = celdas, EsTotal = true }); return this; }
    public SeccionReporte Subtitulo(string texto) { Filas.Add(new FilaReporte { Celdas = new object?[] { texto }, EsSubtitulo = true }); return this; }
}

public class DocumentoReporte
{
    public string Titulo { get; set; } = string.Empty;
    public string Subtitulo { get; set; } = string.Empty;
    public bool Horizontal { get; set; }
    public List<(string Etiqueta, string Valor)> Filtros { get; } = new();
    public List<(string Etiqueta, string Valor)> Kpis { get; } = new();
    public List<SeccionReporte> Secciones { get; } = new();
    public string? NotaPie { get; set; }
}

public static class ExportadorReportes
{
    private static readonly CultureInfo Cl = Formato.Cultura;

    static ExportadorReportes()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // Carpeta de salida: Documentos\FarmSalud ERP\Reportes en Windows; caché en móviles.
    public static string CarpetaReportes
    {
        get
        {
#if WINDOWS
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FarmSalud ERP", "Reportes");
#else
            var dir = Path.Combine(FileSystem.CacheDirectory, "Reportes");
#endif
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    /// <summary>Genera un archivo PDF nativo (.pdf) mediante QuestPDF y lo abre automáticamente en el visor del sistema.</summary>
    public static async Task<string> ExportarPdfAsync(DocumentoReporte doc)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var ruta = Path.Combine(CarpetaReportes, NombreArchivo(doc.Titulo, "pdf"));
        var bytes = await Task.Run(() => GenerarPdfBytes(doc));
        await File.WriteAllBytesAsync(ruta, bytes);
        await AbrirAsync(doc.Titulo, ruta, "application/pdf");
        return ruta;
    }

    /// <summary>Guarda un arreglo de bytes PDF en la carpeta de reportes y lo abre en el visor predeterminado.</summary>
    public static async Task<string> GuardarYAbrirPdfBytesAsync(string titulo, byte[] pdfBytes)
    {
        var ruta = Path.Combine(CarpetaReportes, NombreArchivo(titulo, "pdf"));
        await File.WriteAllBytesAsync(ruta, pdfBytes);
        await AbrirAsync(titulo, ruta, "application/pdf");
        return ruta;
    }

    /// <summary>Genera un CSV (separador ';' + BOM UTF-8) que Microsoft Excel en español abre directamente con columnas separadas.</summary>
    public static async Task<string> ExportarCsvAsync(DocumentoReporte doc)
    {
        var ruta = Path.Combine(CarpetaReportes, NombreArchivo(doc.Titulo, "csv"));
        await File.WriteAllTextAsync(ruta, GenerarCsv(doc), new UTF8Encoding(true));
        await AbrirAsync(doc.Titulo, ruta, "text/csv");
        return ruta;
    }

    private static async Task AbrirAsync(string titulo, string ruta, string mime)
    {
#if WINDOWS
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = ruta,
                UseShellExecute = true
            });
            return;
        }
        catch
        {
            // Fallback a Launcher de MAUI si falla ShellExecute
        }
#endif
        try
        {
            await Launcher.Default.OpenAsync(new OpenFileRequest(titulo, new ReadOnlyFile(ruta, mime)));
        }
        catch
        {
            // El archivo igualmente queda guardado en CarpetaReportes.
        }
    }

    private static string NombreArchivo(string titulo, string ext)
    {
        var limpio = new string(SinTildes(titulo).Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray()).Trim('_');
        if (string.IsNullOrWhiteSpace(limpio)) limpio = "Reporte_ERP";
        return $"{limpio}_{DateTime.Now:yyyyMMdd_HHmmss}.{ext}";
    }

    private static string SinTildes(string texto)
    {
        var normal = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normal.Length);
        foreach (var c in normal)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    // ------------------------------------------------------------------
    //  Formato de celdas
    // ------------------------------------------------------------------
    private static string FormatearTextoPdf(object? valor, TipoColumna tipo) => valor switch
    {
        null => "",
        string s => s,
        DateTime d => d.ToString("dd/MM/yyyy", Cl),
        decimal m when tipo == TipoColumna.Moneda => m == 0 ? "–" : Formato.Moneda(m),
        decimal m when tipo == TipoColumna.Porcentaje => (m / 100m).ToString("P1", Cl),
        double p when tipo == TipoColumna.Porcentaje => p.ToString("P1", Cl),
        decimal m => m.ToString("N0", Cl),
        int i => i.ToString("N0", Cl),
        _ => Convert.ToString(valor, Cl) ?? ""
    };

    private static string FormatearCsv(object? valor, TipoColumna tipo) => valor switch
    {
        null => "",
        string s => s,
        DateTime d => d.ToString("dd/MM/yyyy", Cl),
        decimal m when tipo == TipoColumna.Porcentaje => (m / 100m).ToString("0.##%", Cl),
        double p when tipo == TipoColumna.Porcentaje => p.ToString("0.##%", Cl),
        decimal m => m.ToString("0.##", Cl),
        int i => i.ToString(Cl),
        _ => Convert.ToString(valor, Cl) ?? ""
    };

    private static bool AlineaDerecha(TipoColumna t) => t is TipoColumna.Moneda or TipoColumna.Numero or TipoColumna.Porcentaje;

    // ------------------------------------------------------------------
    //  Generación PDF Nativo con QuestPDF
    // ------------------------------------------------------------------
    public static byte[] GenerarPdfBytes(DocumentoReporte doc)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var usuario = Sesion.UsuarioActual?.NombreCompleto ?? "Contador / Administrador";
        var rol = Sesion.UsuarioActual?.NombreRol ?? "Contabilidad";

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(doc.Horizontal ? PageSizes.A4.Landscape() : PageSizes.A4);
                page.Margin(1.4f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(doc.Horizontal ? 8.5f : 9.5f).FontColor("#0F172A"));

                // ENCABEZADO CORPORATIVO
                page.Header().Column(headerCol =>
                {
                    headerCol.Item().Background("#0F172A").Padding(14).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("FARMSALUD · T.U.S.S.I. ERP FARMACÉUTICO · NORMATIVA SII CHILE")
                                .FontSize(8).Bold().FontColor("#93C5FD");
                            col.Item().PaddingTop(2).Text(doc.Titulo)
                                .FontSize(16).Bold().FontColor(Colors.White);
                            if (!string.IsNullOrWhiteSpace(doc.Subtitulo))
                            {
                                col.Item().PaddingTop(2).Text(doc.Subtitulo)
                                    .FontSize(9.5f).FontColor("#CBD5E1");
                            }
                        });

                        row.ConstantItem(175).Column(col =>
                        {
                            col.Item().AlignRight().Text($"Emitido: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                .FontSize(8.5f).FontColor("#E2E8F0");
                            col.Item().AlignRight().Text($"Responsable: {usuario}")
                                .FontSize(8.5f).FontColor("#E2E8F0");
                            col.Item().AlignRight().Text($"Perfil: {rol} · Estado: OFICIAL")
                                .FontSize(8f).Bold().FontColor("#6EE7B7");
                        });
                    });

                    // Filtros aplicados
                    if (doc.Filtros.Count > 0)
                    {
                        headerCol.Item().Background("#F1F5F9").BorderBottom(1).BorderColor("#CBD5E1").PaddingHorizontal(12).PaddingVertical(6).Row(r =>
                        {
                            foreach (var (etiqueta, valor) in doc.Filtros)
                            {
                                r.AutoItem().PaddingRight(16).Text(t =>
                                {
                                    t.Span($"{etiqueta}: ").FontSize(8).Bold().FontColor("#475569");
                                    t.Span(valor).FontSize(8).FontColor("#0F172A");
                                });
                            }
                        });
                    }

                    headerCol.Item().Height(8);
                });

                // CONTENIDO PRINCIPAL
                page.Content().Column(col =>
                {
                    // Tarjetas KPI de resumen
                    if (doc.Kpis.Count > 0)
                    {
                        col.Item().PaddingBottom(10).Row(row =>
                        {
                            foreach (var (etiqueta, valor) in doc.Kpis)
                            {
                                row.RelativeItem().PaddingHorizontal(3).Border(1).BorderColor("#CBD5E1")
                                    .Background("#F8FAFC").Padding(8).Column(k =>
                                    {
                                        k.Item().Text(etiqueta.ToUpper(Cl)).FontSize(7.5f).Bold().FontColor("#64748B");
                                        k.Item().PaddingTop(2).Text(valor).FontSize(11f).Bold().FontColor("#1D4ED8");
                                    });
                            }
                        });
                    }

                    // Secciones tabulares del reporte
                    foreach (var seccion in doc.Secciones)
                    {
                        if (seccion.Columnas.Count == 0) continue;

                        col.Item().PaddingTop(6).PaddingBottom(4).Text(seccion.Titulo.ToUpper(Cl))
                            .FontSize(10).Bold().FontColor("#1E3A8A");

                        col.Item().PaddingBottom(10).Table(table =>
                        {
                            int numCols = seccion.Columnas.Count;
                            table.ColumnsDefinition(columns =>
                            {
                                for (int i = 0; i < numCols; i++)
                                {
                                    var tipo = seccion.Columnas[i].Tipo;
                                    if (i == 0 && numCols > 4)
                                        columns.RelativeColumn(1.1f);
                                    else if (tipo == TipoColumna.Texto)
                                        columns.RelativeColumn(1.8f);
                                    else
                                        columns.RelativeColumn(1.1f);
                                }
                            });

                            table.Header(header =>
                            {
                                foreach (var c in seccion.Columnas)
                                {
                                    var cell = header.Cell().Background("#E2E8F0").BorderBottom(1.5f).BorderColor("#94A3B8")
                                        .PaddingVertical(5).PaddingHorizontal(5);
                                    if (AlineaDerecha(c.Tipo))
                                        cell.AlignRight().Text(c.Titulo).FontSize(8).Bold().FontColor("#1E293B");
                                    else
                                        cell.AlignLeft().Text(c.Titulo).FontSize(8).Bold().FontColor("#1E293B");
                                }
                            });

                            int filaIdx = 0;
                            foreach (var fila in seccion.Filas)
                            {
                                if (fila.EsSubtitulo)
                                {
                                    var textoSub = fila.Celdas.FirstOrDefault()?.ToString() ?? "";
                                    table.Cell().ColumnSpan((uint)numCols)
                                        .Background("#EFF6FF").BorderBottom(1).BorderColor("#BFDBFE")
                                        .PaddingVertical(5).PaddingHorizontal(6)
                                        .Text(textoSub).FontSize(8.5f).Bold().FontColor("#1D4ED8");
                                    continue;
                                }

                                string bg = fila.EsTotal
                                    ? "#E0E7FF"
                                    : (filaIdx % 2 == 1 ? "#F8FAFC" : "#FFFFFF");

                                for (int i = 0; i < numCols; i++)
                                {
                                    var colDef = seccion.Columnas[i];
                                    var val = i < fila.Celdas.Length ? fila.Celdas[i] : null;
                                    var txt = FormatearTextoPdf(val, colDef.Tipo);
                                    if (i == 0 && fila.Nivel > 0)
                                        txt = new string(' ', fila.Nivel * 3) + txt;

                                    var cell = table.Cell()
                                        .Background(bg)
                                        .BorderBottom(fila.EsTotal ? 1.5f : 0.5f)
                                        .BorderColor(fila.EsTotal ? "#6366F1" : "#E2E8F0")
                                        .PaddingVertical(fila.EsTotal ? 5 : 4)
                                        .PaddingHorizontal(5);

                                    if (AlineaDerecha(colDef.Tipo))
                                    {
                                        var t = cell.AlignRight().Text(txt);
                                        if (fila.EsTotal) t.Bold().FontColor("#1E1B4B");
                                    }
                                    else
                                    {
                                        var t = cell.AlignLeft().Text(txt);
                                        if (fila.EsTotal) t.Bold().FontColor("#1E1B4B");
                                    }
                                }

                                filaIdx++;
                            }
                        });
                    }

                    // Nota al pie y bloque de firma contable
                    if (!string.IsNullOrWhiteSpace(doc.NotaPie))
                    {
                        col.Item().PaddingTop(6).Background("#F8FAFC").Border(1).BorderColor("#E2E8F0")
                            .Padding(8).Text(doc.NotaPie!).FontSize(8).FontColor("#475569");
                    }

                    col.Item().PaddingTop(24).Row(r =>
                    {
                        r.RelativeItem();
                        r.ConstantItem(210).Column(f =>
                        {
                            f.Item().LineHorizontal(1).LineColor("#94A3B8");
                            f.Item().PaddingTop(4).AlignCenter().Text("Firma Contador General / Auditoría Tributaria")
                                .FontSize(8.5f).Bold().FontColor("#334155");
                            f.Item().AlignCenter().Text("T.U.S.S.I. ERP Farmacéutico · SII Chile")
                                .FontSize(7.5f).FontColor("#64748B");
                        });
                    });
                });

                // PIE DE PÁGINA
                page.Footer().PaddingTop(6).BorderTop(0.5f).BorderColor("#CBD5E1").Row(row =>
                {
                    row.RelativeItem().Text("FarmSalud · T.U.S.S.I. ERP — Documento contable oficial generado electrónicamente")
                        .FontSize(7.5f).FontColor("#64748B");
                    row.ConstantItem(100).AlignRight().Text(x =>
                    {
                        x.Span("Página ").FontSize(7.5f).FontColor("#64748B");
                        x.CurrentPageNumber().FontSize(7.5f).Bold().FontColor("#334155");
                        x.Span(" de ").FontSize(7.5f).FontColor("#64748B");
                        x.TotalPages().FontSize(7.5f).Bold().FontColor("#334155");
                    });
                });
            });
        }).GeneratePdf();
    }

    // ------------------------------------------------------------------
    //  CSV (Compatible directamente con Microsoft Excel en español)
    // ------------------------------------------------------------------
    public static string GenerarCsv(DocumentoReporte doc)
    {
        var sb = new StringBuilder();
        void Linea(IEnumerable<string> celdas) => sb.AppendLine(string.Join(";", celdas.Select(Escapar)));

        Linea(new[] { "FarmSalud ERP - " + doc.Titulo });
        if (!string.IsNullOrWhiteSpace(doc.Subtitulo)) Linea(new[] { doc.Subtitulo });
        Linea(new[] { "Generado", DateTime.Now.ToString("dd/MM/yyyy HH:mm", Cl), "Usuario", Sesion.UsuarioActual?.NombreCompleto ?? "-" });
        foreach (var (e, v) in doc.Filtros) Linea(new[] { e, v });
        if (doc.Kpis.Count > 0)
        {
            sb.AppendLine();
            Linea(new[] { "RESUMEN EJECUTIVO" });
            foreach (var (e, v) in doc.Kpis) Linea(new[] { e, v });
        }

        foreach (var s in doc.Secciones)
        {
            sb.AppendLine();
            Linea(new[] { s.Titulo.ToUpper(Cl) });
            Linea(s.Columnas.Select(c => c.Titulo));
            foreach (var f in s.Filas)
            {
                var celdas = new List<string>();
                for (int i = 0; i < s.Columnas.Count; i++)
                {
                    var v = i < f.Celdas.Length ? f.Celdas[i] : null;
                    var txt = FormatearCsv(v, s.Columnas[i].Tipo);
                    if (i == 0 && f.Nivel > 0) txt = new string(' ', f.Nivel * 3) + txt;
                    celdas.Add(txt);
                }
                Linea(celdas);
            }
        }

        if (!string.IsNullOrWhiteSpace(doc.NotaPie))
        {
            sb.AppendLine();
            Linea(new[] { doc.NotaPie! });
        }
        return sb.ToString();
    }

    private static string Escapar(string v)
    {
        if (v.Contains(';') || v.Contains('"') || v.Contains('\n'))
            return "\"" + v.Replace("\"", "\"\"") + "\"";
        return v;
    }
}
