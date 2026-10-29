using TISW_TUSSI_ERP.Helpers;

namespace TISW_TUSSI_ERP.Models.Contabilidad;

public class CuentaPorCobrar
{
    public int IdDocumento { get; set; }
    public string NumeroDocumento { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public decimal MontoTotal { get; set; }
    public decimal MontoPagado { get; set; }
    public decimal SaldoPendiente => MontoTotal - MontoPagado;
    public string Estado => SaldoPendiente <= 0 ? "Pagado" : (DateTime.Now > FechaVencimiento ? "Vencido" : "Pendiente");

    // Datos adicionales provenientes de la BD (ventas_encabezado / clientes / convenios_salud)
    public string RutCliente { get; set; } = string.Empty;
    public string TipoDocumento { get; set; } = string.Empty;
    public string Convenio { get; set; } = string.Empty;

    // Presentación
    public int DiasVencido => (DateTime.Today - FechaVencimiento.Date).Days;
    public string EmisionTexto => FechaEmision.ToString("dd/MM/yyyy");
    public string VencimientoTexto => FechaVencimiento.ToString("dd/MM/yyyy");
    public string MontoTotalTexto => Formato.Moneda(MontoTotal);
    public string MontoPagadoTexto => Formato.Moneda(MontoPagado);
    public string SaldoTexto => Formato.Moneda(SaldoPendiente);
    public string DetalleTexto => string.IsNullOrWhiteSpace(Convenio) ? RutCliente : $"{RutCliente} · {Convenio}";
    public string PlazoTexto => Estado switch
    {
        "Pagado" => "Liquidado",
        "Vencido" => $"{DiasVencido} días de mora",
        _ => DiasVencido == 0 ? "Vence hoy" : $"Vence en {-DiasVencido} días"
    };
    public double AvancePago => MontoTotal <= 0 ? 0 : (double)Math.Clamp(MontoPagado / MontoTotal, 0, 1);
    public Color EstadoFondo => EstiloContable.EstadoDocumento(Estado).Fondo;
    public Color EstadoColor => EstiloContable.EstadoDocumento(Estado).Texto;
}
