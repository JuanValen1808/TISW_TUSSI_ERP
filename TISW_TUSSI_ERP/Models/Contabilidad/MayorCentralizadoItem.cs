using System;
using TISW_TUSSI_ERP.Helpers;

namespace TISW_TUSSI_ERP.Models.Contabilidad;

public class MayorCentralizadoItem
{
    public DateTime Fecha { get; set; }
    public int NumeroAsiento { get; set; }
    public string Origen { get; set; } = "OTRO";
    public string Glosa { get; set; } = string.Empty;
    public string CodigoCuenta { get; set; } = string.Empty;
    public string NombreCuenta { get; set; } = string.Empty;
    public string TipoCuenta { get; set; } = string.Empty;
    public decimal Debe { get; set; }
    public decimal Haber { get; set; }
    public decimal SaldoAcumulado { get; set; } // Calculado en memoria según naturaleza

    public string FechaTexto => Fecha.ToString("dd/MM/yyyy");
    public string AsientoTexto => $"#{NumeroAsiento:D4}";
    public string CuentaCompleta => $"{CodigoCuenta} · {NombreCuenta}";
    public string GlosaTexto => string.IsNullOrWhiteSpace(Glosa) ? "(Sin glosa)" : Glosa;
    public string DebeTexto => Debe > 0 ? Formato.Moneda(Debe) : "–";
    public string HaberTexto => Haber > 0 ? Formato.Moneda(Haber) : "–";
    public string SaldoAcumuladoTexto => Formato.Moneda(SaldoAcumulado);
    public Color OrigenFondo => EstiloContable.Origen(Origen).Fondo;
    public Color OrigenColor => EstiloContable.Origen(Origen).Texto;
    public Color TipoFondo => EstiloContable.TipoCuenta(TipoCuenta).Fondo;
    public Color TipoColor => EstiloContable.TipoCuenta(TipoCuenta).Texto;
}

public class ResumenMensualMayorCentralizado
{
    public DateTime PeriodoDate { get; set; }
    public string PeriodoTexto { get; set; } = string.Empty;
    public string CodigoCuenta { get; set; } = string.Empty;
    public string NombreCuenta { get; set; } = string.Empty;
    public string TipoCuenta { get; set; } = string.Empty;
    public int CantidadMovimientos { get; set; }
    public decimal TotalDebe { get; set; }
    public decimal TotalHaber { get; set; }
    public decimal VariacionNeta { get; set; }
    public decimal SaldoFinalMes { get; set; }

    public string CuentaTexto => $"{CodigoCuenta} · {NombreCuenta}";
    public string MovimientosTexto => CantidadMovimientos == 1 ? "1 asiento" : $"{CantidadMovimientos} asientos";
    public string TotalDebeTexto => TotalDebe > 0 ? Formato.Moneda(TotalDebe) : "–";
    public string TotalHaberTexto => TotalHaber > 0 ? Formato.Moneda(TotalHaber) : "–";
    public string VariacionTexto => (VariacionNeta >= 0 ? "+ " : "− ") + Formato.Moneda(Math.Abs(VariacionNeta));
    public string SaldoFinalTexto => Formato.Moneda(SaldoFinalMes);
    public Color VariacionColor => VariacionNeta >= 0 ? Color.FromArgb("#047857") : Color.FromArgb("#BE123C");
    public Color TipoFondo => EstiloContable.TipoCuenta(TipoCuenta).Fondo;
    public Color TipoColor => EstiloContable.TipoCuenta(TipoCuenta).Texto;

    public string PeriodoNombre => PeriodoTexto;
    public string SaldoMensualTexto => VariacionTexto;
    public Color SaldoColor => VariacionColor;
}
