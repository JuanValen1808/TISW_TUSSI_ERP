using System;
using TISW_TUSSI_ERP.Helpers;

namespace TISW_TUSSI_ERP.Models.Contabilidad;

public class LibroDiarioAsiento
{
    public int IdAsiento { get; set; }
    public int NumeroAsiento { get; set; }
    public DateTime FechaAsiento { get; set; }
    public string GlosaDescripcion { get; set; } = string.Empty;
    public string Origen { get; set; } = string.Empty;
    public int IdUsuario { get; set; }
    public DateTime CreatedAt { get; set; }

    // Datos agregados (JOIN con usuarios y detalle_asiento_contable)
    public string Usuario { get; set; } = string.Empty;
    public decimal TotalDebe { get; set; }
    public decimal TotalHaber { get; set; }
    public int CantidadLineas { get; set; }

    // Presentación
    public string NumeroTexto => $"N° {NumeroAsiento}";
    public string FechaTexto => FechaAsiento.ToString("dd/MM/yyyy");
    public string GlosaTexto => string.IsNullOrWhiteSpace(GlosaDescripcion) ? "(Sin glosa)" : GlosaDescripcion;
    public string MontoTexto => Formato.Moneda(TotalDebe);
    public string LineasTexto => CantidadLineas == 1 ? "1 línea" : $"{CantidadLineas} líneas";
    public bool Cuadrado => TotalDebe == TotalHaber && TotalDebe > 0;
    public Color OrigenFondo => EstiloContable.Origen(Origen).Fondo;
    public Color OrigenColor => EstiloContable.Origen(Origen).Texto;
    public Color CuadraturaColor => Cuadrado ? Color.FromArgb("#10B981") : Color.FromArgb("#E11D48");
}
