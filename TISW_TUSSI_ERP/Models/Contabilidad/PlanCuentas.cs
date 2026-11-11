using TISW_TUSSI_ERP.Helpers;

namespace TISW_TUSSI_ERP.Models.Contabilidad;

public class PlanCuentas
{
    public int IdCuenta { get; set; }
    public string CodigoCuenta { get; set; } = string.Empty;
    public string NombreCuenta { get; set; } = string.Empty;
    public string TipoCuenta { get; set; } = string.Empty;
    public int Nivel { get; set; }
    public int? CuentaPadreId { get; set; }

    // Movimientos acumulados desde detalle_asiento_contable (incluye subcuentas si es grupo)
    public decimal TotalDebe { get; set; }
    public decimal TotalHaber { get; set; }
    public int CantidadMovimientos { get; set; }

    // Presentación jerárquica y normativa SII / IFRS Chile
    public bool EsGrupo => Nivel <= 1;
    public bool EsImputable => !EsGrupo;
    public bool EsDeudora => EstiloContable.EsNaturalezaDeudora(TipoCuenta);
    public decimal SaldoActual => EsDeudora ? (TotalDebe - TotalHaber) : (TotalHaber - TotalDebe);

    public string Descripcion => $"{CodigoCuenta}  ·  {NombreCuenta}";
    public string TipoTexto => Formato.Capitalizar(TipoCuenta.ToLowerInvariant());
    public string NivelTexto => Nivel switch
    {
        1 => "Nivel 1 · Rubro / Grupo",
        2 => "Nivel 2 · Cuenta Mayor",
        _ => $"Nivel {Nivel} · Subcuenta"
    };
    public string ImputableTexto => EsGrupo ? "Agrupación" : "Imputable";
    public string NaturalezaTexto => EsDeudora ? "Deudora (Debe +)" : "Acreedora (Haber +)";
    public string ClasificacionSii => (TipoCuenta ?? "").ToUpperInvariant() switch
    {
        "ACTIVO" => "Balance · Activo (Inventarios)",
        "PASIVO" => "Balance · Pasivo Exigible",
        "PATRIMONIO" => "Balance · Patrimonio Neto",
        "INGRESO" => "Estado Resultados · Ganancia",
        "GASTO" => "Estado Resultados · Pérdida",
        _ => "General"
    };

    public string TotalDebeTexto => TotalDebe > 0 ? Formato.Moneda(TotalDebe) : "–";
    public string TotalHaberTexto => TotalHaber > 0 ? Formato.Moneda(TotalHaber) : "–";
    public string SaldoTexto => SaldoActual == 0 ? "$0" : Formato.Moneda(SaldoActual);
    public string MovimientosTexto => CantidadMovimientos == 1 ? "1 mov." : $"{CantidadMovimientos} movs.";

    public Thickness Sangria => new((Math.Max(Nivel, 1) - 1) * 22, 0, 0, 0);
    public FontAttributes Fuente => EsGrupo ? FontAttributes.Bold : FontAttributes.None;
    public double TamanoFuente => EsGrupo ? 14 : 13;
    public Color FondoFila => EsGrupo ? Color.FromArgb("#F8FAFC") : Colors.White;
    public Color TipoFondo => EstiloContable.TipoCuenta(TipoCuenta).Fondo;
    public Color TipoColor => EstiloContable.TipoCuenta(TipoCuenta).Texto;
    public Color NaturalezaColor => EsDeudora ? Color.FromArgb("#1D4ED8") : Color.FromArgb("#7C3AED");
}
