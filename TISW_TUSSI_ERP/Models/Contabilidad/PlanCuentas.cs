namespace TISW_TUSSI_ERP.Models.Contabilidad;

public class PlanCuentas
{
    public int IdCuenta { get; set; }
    public string CodigoCuenta { get; set; } = string.Empty;
    public string NombreCuenta { get; set; } = string.Empty;
    public string TipoCuenta { get; set; } = string.Empty;
    public int Nivel { get; set; }
    public int? CuentaPadreId { get; set; }

    // Presentación
    public bool EsGrupo => Nivel <= 1;
    public string Descripcion => $"{CodigoCuenta}  ·  {NombreCuenta}";
    public string TipoTexto => Helpers.Formato.Capitalizar(TipoCuenta.ToLowerInvariant());
    public string NivelTexto => EsGrupo ? "Grupo" : $"Nivel {Nivel}";
    public string NaturalezaTexto => EstiloContable.EsNaturalezaDeudora(TipoCuenta) ? "Deudora" : "Acreedora";
    public Thickness Sangria => new((Math.Max(Nivel, 1) - 1) * 24, 0, 0, 0);
    public FontAttributes Fuente => EsGrupo ? FontAttributes.Bold : FontAttributes.None;
    public Color TipoFondo => EstiloContable.TipoCuenta(TipoCuenta).Fondo;
    public Color TipoColor => EstiloContable.TipoCuenta(TipoCuenta).Texto;
}
