using System;
using System.Collections.Generic;
using System.Linq;
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
    public List<LineaAsiento> Lineas { get; set; } = new();

    // Presentación
    public string NumeroTexto => $"Folio #{NumeroAsiento:D4}";
    public string FechaTexto => FechaAsiento.ToString("dd/MM/yyyy");
    public string FechaHoraTexto => CreatedAt.Year > 2000 ? CreatedAt.ToString("dd/MM/yyyy HH:mm") : FechaTexto;
    public string GlosaTexto => string.IsNullOrWhiteSpace(GlosaDescripcion) ? "(Sin glosa)" : GlosaDescripcion;
    public string UsuarioTexto => string.IsNullOrWhiteSpace(Usuario) ? "Sistema ERP" : Usuario;
    public string TotalDebeTexto => Formato.Moneda(TotalDebe);
    public string TotalHaberTexto => Formato.Moneda(TotalHaber);
    public string MontoTexto => Formato.Moneda(TotalDebe);
    public string LineasTexto => CantidadLineas == 1 ? "1 imputación" : $"{CantidadLineas} imputaciones";
    public bool Cuadrado => TotalDebe == TotalHaber && TotalDebe > 0;
    public string EstadoCuadraturaTexto => Cuadrado ? "PARTIDA DOBLE OK" : "DESCUADRADO";
    public Color OrigenFondo => EstiloContable.Origen(Origen).Fondo;
    public Color OrigenColor => EstiloContable.Origen(Origen).Texto;
    public Color CuadraturaFondo => Cuadrado ? Color.FromArgb("#D1FAE5") : Color.FromArgb("#FFE4E6");
    public Color CuadraturaColor => Cuadrado ? Color.FromArgb("#047857") : Color.FromArgb("#BE123C");
    public string TipoAsientoTexto => Origen.ToUpperInvariant() switch
    {
        "VENTA" => "Automático · Ventas",
        "COMPRA" => "Automático · Compras",
        "RECEPCION" => "Automático · Recepción",
        "AJUSTE" => "Manual / Ajuste",
        _ => "Manual · General"
    };
}

public class LineaAsientoInput
{
    public int IdCuenta { get; set; }
    public string CodigoCuenta { get; set; } = string.Empty;
    public string NombreCuenta { get; set; } = string.Empty;
    public decimal Debe { get; set; }
    public decimal Haber { get; set; }

    public string CuentaCompleta => $"{CodigoCuenta} · {NombreCuenta}";
    public string DebeTexto => Debe > 0 ? Formato.Moneda(Debe) : "–";
    public string HaberTexto => Haber > 0 ? Formato.Moneda(Haber) : "–";
    public string TipoImputacion => Debe > 0 ? "CARGO (DEBE)" : "ABONO (HABER)";
    public Color ImputacionFondo => Debe > 0 ? Color.FromArgb("#DBEAFE") : Color.FromArgb("#EDE9FE");
    public Color ImputacionColor => Debe > 0 ? Color.FromArgb("#1D4ED8") : Color.FromArgb("#6D28D9");
}

public class ResumenPendientesContables
{
    public int VentasSinCentralizar { get; set; }
    public int ComprasSinCentralizar { get; set; }
    public int MermasSinCentralizar { get; set; }
    public int TotalPendientes => VentasSinCentralizar + ComprasSinCentralizar + MermasSinCentralizar;
    public bool HayPendientes => TotalPendientes > 0;
}
