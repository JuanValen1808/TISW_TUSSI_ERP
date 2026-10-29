using System.Globalization;
using System.Net;
using System.Text;
using TISW_TUSSI_ERP.Models.Auth;

namespace TISW_TUSSI_ERP.Helpers;

// ===================================================================
//  Modelo genérico de reporte exportable (PDF imprimible / CSV Excel)
//  Cada ViewModel de reporte arma un DocumentoReporte y el
//  ExportadorReportes se encarga de generar el archivo.
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

    /// <summary>Genera un HTML con estilo de informe y lo abre en el navegador listo para "Imprimir / Guardar como PDF".</summary>
    public static async Task<string> ExportarPdfAsync(DocumentoReporte doc)
    {
        var ruta = Path.Combine(CarpetaReportes, NombreArchivo(doc.Titulo, "html"));
        await File.WriteAllTextAsync(ruta, GenerarHtml(doc), new UTF8Encoding(true));
        await AbrirAsync(doc.Titulo, ruta, "text/html");
        return ruta;
    }

    /// <summary>Genera un CSV (separador ';' + BOM UTF-8) que Excel en español abre directamente.</summary>
    public static async Task<string> ExportarCsvAsync(DocumentoReporte doc)
    {
        var ruta = Path.Combine(CarpetaReportes, NombreArchivo(doc.Titulo, "csv"));
        await File.WriteAllTextAsync(ruta, GenerarCsv(doc), new UTF8Encoding(true));
        await AbrirAsync(doc.Titulo, ruta, "text/csv");
        return ruta;
    }

    private static async Task AbrirAsync(string titulo, string ruta, string mime)
    {
        try
        {
            await Launcher.Default.OpenAsync(new OpenFileRequest(titulo, new ReadOnlyFile(ruta, mime)));
        }
        catch
        {
            // Si no hay aplicación asociada, el archivo igualmente queda guardado en la carpeta de reportes.
        }
    }

    private static string NombreArchivo(string titulo, string ext)
    {
        var limpio = new string(SinTildes(titulo).Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray()).Trim('_');
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
    private static string FormatearHtml(object? valor, TipoColumna tipo) => valor switch
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
    //  CSV
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
            Linea(new[] { "RESUMEN" });
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

    // ------------------------------------------------------------------
    //  HTML (vista imprimible -> PDF)
    // ------------------------------------------------------------------
    public static string GenerarHtml(DocumentoReporte doc)
    {
        static string H(string? s) => WebUtility.HtmlEncode(s ?? "");
        var usuario = Sesion.UsuarioActual?.NombreCompleto ?? "-";
        var sb = new StringBuilder();

        sb.Append($@"<!DOCTYPE html>
<html lang=""es"">
<head>
<meta charset=""utf-8"" />
<meta name=""viewport"" content=""width=device-width, initial-scale=1"" />
<meta name=""description"" content=""{H(doc.Titulo)} - FarmSalud ERP"" />
<title>{H(doc.Titulo)} · FarmSalud ERP</title>
<style>
  @page {{ size: A4 {(doc.Horizontal ? "landscape" : "portrait")}; margin: 12mm; }}
  * {{ box-sizing: border-box; }}
  body {{ font-family: 'Segoe UI', Inter, Roboto, Arial, sans-serif; color: #111827; margin: 0; background: #F4F6FA; }}
  .page {{ max-width: {(doc.Horizontal ? "1200px" : "900px")}; margin: 24px auto; background: #fff; border-radius: 16px; overflow: hidden; box-shadow: 0 10px 30px rgba(15,23,42,.08); }}
  .hero {{ background: linear-gradient(135deg, #0B2A4A 0%, #1D4ED8 65%, #6366F1 100%); color: #fff; padding: 28px 32px; }}
  .brand {{ display: flex; align-items: center; gap: 10px; font-size: 12px; letter-spacing: 1.4px; color: #C7D2FE; font-weight: 700; text-transform: uppercase; }}
  .logo {{ width: 30px; height: 30px; border-radius: 8px; background: rgba(255,255,255,.18); display: grid; place-items: center; color: #fff; font-weight: 800; }}
  h1 {{ margin: 12px 0 4px; font-size: 26px; }}
  .sub {{ color: #DBEAFE; font-size: 13px; margin: 0; }}
  .meta {{ display: flex; flex-wrap: wrap; gap: 8px; margin-top: 16px; }}
  .chip {{ background: rgba(255,255,255,.15); border: 1px solid rgba(255,255,255,.3); border-radius: 999px; padding: 4px 12px; font-size: 11px; }}
  .content {{ padding: 24px 32px 32px; }}
  .kpis {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(170px, 1fr)); gap: 12px; margin-bottom: 22px; }}
  .kpi {{ border: 1px solid #E3E8EF; border-left: 4px solid #4F46E5; border-radius: 12px; padding: 12px 14px; }}
  .kpi span {{ display: block; font-size: 10px; font-weight: 700; letter-spacing: .8px; color: #6B7280; text-transform: uppercase; }}
  .kpi b {{ display: block; font-size: 18px; margin-top: 4px; }}
  h2 {{ font-size: 14px; margin: 22px 0 8px; color: #0B2A4A; text-transform: uppercase; letter-spacing: .6px; }}
  table {{ width: 100%; border-collapse: collapse; font-size: 12px; }}
  thead th {{ background: #F1F5F9; color: #475569; font-size: 10px; letter-spacing: .6px; text-transform: uppercase; text-align: left; padding: 9px 10px; border-bottom: 2px solid #E2E8F0; }}
  td {{ padding: 8px 10px; border-bottom: 1px solid #EEF2F7; }}
  tbody tr:nth-child(even) td {{ background: #FAFBFD; }}
  .r {{ text-align: right; font-variant-numeric: tabular-nums; white-space: nowrap; }}
  tr.total td {{ background: #EEF2FF !important; font-weight: 700; border-top: 2px solid #C7D2FE; }}
  tr.subt td {{ background: #F8FAFC !important; font-weight: 700; color: #1D4ED8; }}
  .foot {{ margin-top: 26px; padding-top: 12px; border-top: 1px dashed #CBD5E1; font-size: 11px; color: #64748B; display: flex; justify-content: space-between; gap: 12px; }}
  .firma {{ margin-top: 48px; display: flex; justify-content: flex-end; }}
  .firma div {{ width: 240px; border-top: 1px solid #94A3B8; text-align: center; font-size: 11px; color: #475569; padding-top: 6px; }}
  .print {{ position: fixed; right: 24px; bottom: 24px; background: #1D4ED8; color: #fff; border: 0; border-radius: 999px; padding: 12px 20px; font-weight: 700; cursor: pointer; box-shadow: 0 8px 20px rgba(29,78,216,.35); }}
  .print:hover {{ background: #1E40AF; }}
  @media print {{ body {{ background: #fff; }} .page {{ box-shadow: none; margin: 0; max-width: none; border-radius: 0; }} .print {{ display: none; }}
                 .hero {{ -webkit-print-color-adjust: exact; print-color-adjust: exact; }} thead th, tr.total td, tr.subt td, .kpi {{ -webkit-print-color-adjust: exact; print-color-adjust: exact; }} }}
</style>
</head>
<body>
<main class=""page"">
  <header class=""hero"">
    <div class=""brand""><div class=""logo"">FS</div>FarmSalud ERP · Contabilidad</div>
    <h1>{H(doc.Titulo)}</h1>
    <p class=""sub"">{H(doc.Subtitulo)}</p>
    <div class=""meta"">
      <span class=""chip"">Generado: {DateTime.Now.ToString("dd/MM/yyyy HH:mm", Cl)}</span>
      <span class=""chip"">Usuario: {H(usuario)}</span>");
        foreach (var (e, v) in doc.Filtros)
            sb.Append($@"
      <span class=""chip"">{H(e)}: {H(v)}</span>");
        sb.Append(@"
    </div>
  </header>
  <section class=""content"">");

        if (doc.Kpis.Count > 0)
        {
            sb.Append(@"
    <div class=""kpis"">");
            foreach (var (e, v) in doc.Kpis)
                sb.Append($@"<div class=""kpi""><span>{H(e)}</span><b>{H(v)}</b></div>");
            sb.Append("</div>");
        }

        foreach (var s in doc.Secciones)
        {
            sb.Append($@"
    <h2>{H(s.Titulo)}</h2>
    <table>
      <thead><tr>");
            foreach (var c in s.Columnas)
                sb.Append($@"<th class=""{(AlineaDerecha(c.Tipo) ? "r" : "")}"">{H(c.Titulo)}</th>");
            sb.Append("</tr></thead>\n      <tbody>");

            if (s.Filas.Count == 0)
                sb.Append($@"<tr><td colspan=""{s.Columnas.Count}"" style=""text-align:center;color:#94A3B8;padding:18px"">Sin registros para los filtros seleccionados</td></tr>");

            foreach (var f in s.Filas)
            {
                if (f.EsSubtitulo)
                {
                    sb.Append($@"<tr class=""subt""><td colspan=""{s.Columnas.Count}"">{H(Convert.ToString(f.Celdas.FirstOrDefault(), Cl))}</td></tr>");
                    continue;
                }
                sb.Append(f.EsTotal ? @"<tr class=""total"">" : "<tr>");
                for (int i = 0; i < s.Columnas.Count; i++)
                {
                    var col = s.Columnas[i];
                    var v = i < f.Celdas.Length ? f.Celdas[i] : null;
                    var estilo = i == 0 && f.Nivel > 0 ? $@" style=""padding-left:{10 + f.Nivel * 18}px""" : "";
                    sb.Append($@"<td class=""{(AlineaDerecha(col.Tipo) ? "r" : "")}""{estilo}>{H(FormatearHtml(v, col.Tipo))}</td>");
                }
                sb.Append("</tr>");
            }
            sb.Append("</tbody>\n    </table>");
        }

        sb.Append($@"
    <div class=""firma""><div>Contador / Responsable</div></div>
    <div class=""foot""><span>{H(doc.NotaPie ?? "Documento generado automáticamente por FarmSalud ERP.")}</span><span>Montos expresados en pesos chilenos (CLP)</span></div>
  </section>
</main>
<button class=""print"" onclick=""window.print()"">Imprimir / Guardar PDF</button>
<script>window.addEventListener('load', function () {{ setTimeout(function () {{ window.print(); }}, 600); }});</script>
</body>
</html>");
        return sb.ToString();
    }
}
