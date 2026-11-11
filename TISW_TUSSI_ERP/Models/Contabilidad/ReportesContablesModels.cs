using TISW_TUSSI_ERP.Helpers;

namespace TISW_TUSSI_ERP.Models.Contabilidad;

// ===================================================================
//  Estilos (colores de pills) compartidos por las pantallas contables
// ===================================================================
public static class EstiloContable
{
    private static (Color Fondo, Color Texto) P(string fondo, string texto) => (Color.FromArgb(fondo), Color.FromArgb(texto));

    public static (Color Fondo, Color Texto) Origen(string? origen) => (origen ?? "").ToUpperInvariant() switch
    {
        "VENTA" => P("#D1FAE5", "#047857"),
        "COMPRA" => P("#DBEAFE", "#1D4ED8"),
        "RECEPCION" => P("#E0E7FF", "#4338CA"),
        "AJUSTE" => P("#FEF3C7", "#B45309"),
        _ => P("#F1F5F9", "#475569"),
    };

    public static (Color Fondo, Color Texto) TipoCuenta(string? tipo) => (tipo ?? "").ToUpperInvariant() switch
    {
        "ACTIVO" => P("#DBEAFE", "#1D4ED8"),
        "PASIVO" => P("#FFE4E6", "#BE123C"),
        "PATRIMONIO" => P("#EDE9FE", "#6D28D9"),
        "INGRESO" => P("#D1FAE5", "#047857"),
        "GASTO" => P("#FEF3C7", "#B45309"),
        _ => P("#F1F5F9", "#475569"),
    };

    public static (Color Fondo, Color Texto) EstadoDocumento(string? estado) => estado switch
    {
        "Pagado" => P("#D1FAE5", "#047857"),
        "Vencido" => P("#FFE4E6", "#BE123C"),
        "Anulado" => P("#F1F5F9", "#64748B"),
        _ => P("#FEF3C7", "#B45309"),
    };

    public static bool EsNaturalezaDeudora(string? tipo) => (tipo ?? "").ToUpperInvariant() is "ACTIVO" or "GASTO";

    public static string Capitalizar(string texto) => Formato.Capitalizar(texto);

    // Tramos de antigüedad
    public static readonly string[] Tramos = { "Por vencer", "1 - 30 días", "31 - 60 días", "61 - 90 días", "Más de 90 días" };
    public static readonly Color[] ColoresTramo =
    {
        Color.FromArgb("#10B981"), Color.FromArgb("#3B82F6"), Color.FromArgb("#F59E0B"), Color.FromArgb("#F97316"), Color.FromArgb("#E11D48")
    };
    public static readonly Color[] FondosTramo =
    {
        Color.FromArgb("#D1FAE5"), Color.FromArgb("#DBEAFE"), Color.FromArgb("#FEF3C7"), Color.FromArgb("#FFEDD5"), Color.FromArgb("#FFE4E6")
    };

    public static int IndiceTramo(int diasVencido) => diasVencido switch
    {
        <= 0 => 0,
        <= 30 => 1,
        <= 60 => 2,
        <= 90 => 3,
        _ => 4
    };
}

// ===================================================================
//  Libro Diario detallado
// ===================================================================
public class LineaAsiento
{
    public string CodigoCuenta { get; set; } = string.Empty;
    public string NombreCuenta { get; set; } = string.Empty;
    public decimal Debe { get; set; }
    public decimal Haber { get; set; }

    public bool EsHaber => Haber > 0 && Debe == 0;
    public string DebeTexto => Debe > 0 ? Formato.Moneda(Debe) : "";
    public string HaberTexto => Haber > 0 ? Formato.Moneda(Haber) : "";
    public string CuentaTexto => EsHaber ? $"a  {NombreCuenta}" : NombreCuenta;
    public Thickness Sangria => EsHaber ? new Thickness(28, 0, 0, 0) : new Thickness(0);
}

public class AsientoDetallado
{
    public int IdAsiento { get; set; }
    public int NumeroAsiento { get; set; }
    public DateTime Fecha { get; set; }
    public string Glosa { get; set; } = string.Empty;
    public string Origen { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public List<LineaAsiento> Lineas { get; } = new();

    public decimal TotalDebe => Lineas.Sum(l => l.Debe);
    public decimal TotalHaber => Lineas.Sum(l => l.Haber);
    public bool Cuadrado => TotalDebe == TotalHaber && TotalDebe > 0;

    public string NumeroTexto => $"Asiento N° {NumeroAsiento}";
    public string FechaTexto => Formato.Capitalizar(Fecha.ToString("dddd dd 'de' MMMM yyyy", Formato.Cultura));
    public string GlosaTexto => string.IsNullOrWhiteSpace(Glosa) ? "(Sin glosa)" : Glosa;
    public string UsuarioTexto => string.IsNullOrWhiteSpace(Usuario) ? "" : $"Registrado por {Usuario}";
    public string TotalDebeTexto => Formato.Moneda(TotalDebe);
    public string TotalHaberTexto => Formato.Moneda(TotalHaber);
    public string CuadraturaTexto => Cuadrado ? "CUADRADO" : "DESCUADRADO";
    public Color CuadraturaFondo => Cuadrado ? Color.FromArgb("#D1FAE5") : Color.FromArgb("#FFE4E6");
    public Color CuadraturaColor => Cuadrado ? Color.FromArgb("#047857") : Color.FromArgb("#BE123C");
    public Color OrigenFondo => EstiloContable.Origen(Origen).Fondo;
    public Color OrigenColor => EstiloContable.Origen(Origen).Texto;
}

// ===================================================================
//  Saldos por cuenta (base del Balance General)
// ===================================================================
public class SaldoCuenta
{
    public int IdCuenta { get; set; }
    public string CodigoCuenta { get; set; } = string.Empty;
    public string NombreCuenta { get; set; } = string.Empty;
    public string TipoCuenta { get; set; } = string.Empty;
    public int Nivel { get; set; }
    public decimal TotalDebe { get; set; }
    public decimal TotalHaber { get; set; }

    // Saldo según la naturaleza de la cuenta (deudora: Debe-Haber / acreedora: Haber-Debe)
    public decimal SaldoNatural => EstiloContable.EsNaturalezaDeudora(TipoCuenta) ? TotalDebe - TotalHaber : TotalHaber - TotalDebe;
}

public class LineaBalanceGeneral
{
    public string CodigoCuenta { get; set; } = string.Empty;
    public string NombreCuenta { get; set; } = string.Empty;
    public decimal Saldo { get; set; }
    public double Proporcion { get; set; }
    public Color Color { get; set; } = Colors.SteelBlue;
    public bool EsCalculada { get; set; }

    public string SaldoTexto => Formato.Moneda(Saldo);
    public string PorcentajeTexto => Proporcion.ToString("P1", Formato.Cultura);
    public FontAttributes Fuente => EsCalculada ? FontAttributes.Italic : FontAttributes.None;
}

// ===================================================================
//  Antigüedad de saldos (CxC / CxP)
// ===================================================================
public class DocumentoAntiguedad
{
    public string Documento { get; set; } = string.Empty;
    public string Tercero { get; set; } = string.Empty;
    public string Rut { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public decimal MontoTotal { get; set; }
    public decimal Saldo { get; set; }
    public int DiasVencido { get; set; }

    public int IndiceTramo => EstiloContable.IndiceTramo(DiasVencido);
    public string Tramo => EstiloContable.Tramos[IndiceTramo];
    public Color TramoFondo => EstiloContable.FondosTramo[IndiceTramo];
    public Color TramoColor => EstiloContable.ColoresTramo[IndiceTramo];
    public string EmisionTexto => FechaEmision.ToString("dd/MM/yyyy");
    public string VencimientoTexto => FechaVencimiento.ToString("dd/MM/yyyy");
    public string MontoTexto => Formato.Moneda(MontoTotal);
    public string SaldoTexto => Formato.Moneda(Saldo);
    public string DiasTexto => DiasVencido > 0 ? $"{DiasVencido} días vencido" : DiasVencido == 0 ? "Vence hoy" : $"Vence en {-DiasVencido} días";
}

public class TramoAntiguedad
{
    public string Nombre { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public int Cantidad { get; set; }
    public double Proporcion { get; set; }
    public Color Color { get; set; } = Colors.SteelBlue;
    public Color Fondo { get; set; } = Colors.AliceBlue;

    public string MontoTexto => Formato.Moneda(Monto);
    public string PorcentajeTexto => Proporcion.ToString("P1", Formato.Cultura);
    public string CantidadTexto => Cantidad == 1 ? "1 documento" : $"{Cantidad} documentos";
}

public class TerceroAntiguedad
{
    public string Tercero { get; set; } = string.Empty;
    public string Rut { get; set; } = string.Empty;
    public decimal[] Tramos { get; } = new decimal[5];
    public decimal Total => Tramos.Sum();

    private static string T(decimal v) => v == 0 ? "–" : Formato.Moneda(v);
    public string T0 => T(Tramos[0]);
    public string T1 => T(Tramos[1]);
    public string T2 => T(Tramos[2]);
    public string T3 => T(Tramos[3]);
    public string T4 => T(Tramos[4]);
    public string TotalTexto => Formato.Moneda(Total);
}

// ===================================================================
//  Flujo de Caja
// ===================================================================
public class MovimientoCaja
{
    public DateTime Fecha { get; set; }
    public int NumeroAsiento { get; set; }
    public string Glosa { get; set; } = string.Empty;
    public string Origen { get; set; } = string.Empty;
    public string Contrapartida { get; set; } = string.Empty;
    public decimal Entrada { get; set; }
    public decimal Salida { get; set; }
    public decimal Saldo { get; set; }

    public string FechaTexto => Fecha.ToString("dd/MM/yyyy");
    public string AsientoTexto => $"#{NumeroAsiento}";
    public string EntradaTexto => Entrada > 0 ? "+ " + Formato.Moneda(Entrada) : "";
    public string SalidaTexto => Salida > 0 ? "− " + Formato.Moneda(Salida) : "";
    public string SaldoTexto => Formato.Moneda(Saldo);
    public string GlosaTexto => string.IsNullOrWhiteSpace(Glosa) ? "(Sin glosa)" : Glosa;
    public Color OrigenFondo => EstiloContable.Origen(Origen).Fondo;
    public Color OrigenColor => EstiloContable.Origen(Origen).Texto;
}

public class ResumenOrigenCaja
{
    public string Origen { get; set; } = string.Empty;
    public string Actividad { get; set; } = string.Empty;
    public decimal Entradas { get; set; }
    public decimal Salidas { get; set; }
    public decimal Neto => Entradas - Salidas;

    public string EntradasTexto => Formato.Moneda(Entradas);
    public string SalidasTexto => Formato.Moneda(Salidas);
    public string NetoTexto => (Neto >= 0 ? "+ " : "− ") + Formato.Moneda(Math.Abs(Neto));
    public Color NetoColor => Neto >= 0 ? Color.FromArgb("#047857") : Color.FromArgb("#BE123C");
    public Color OrigenFondo => EstiloContable.Origen(Origen).Fondo;
    public Color OrigenColor => EstiloContable.Origen(Origen).Texto;
}

// ===================================================================
//  Análisis por Cuenta
// ===================================================================
public class MovimientoCuenta
{
    public DateTime Fecha { get; set; }
    public int NumeroAsiento { get; set; }
    public string Glosa { get; set; } = string.Empty;
    public string Origen { get; set; } = string.Empty;
    public string CodigoCuenta { get; set; } = string.Empty;
    public string Contrapartida { get; set; } = string.Empty;
    public decimal Debe { get; set; }
    public decimal Haber { get; set; }
    public decimal Saldo { get; set; }

    public string FechaTexto => Fecha.ToString("dd/MM/yyyy");
    public string DebeTexto => Debe > 0 ? Formato.Moneda(Debe) : "";
    public string HaberTexto => Haber > 0 ? Formato.Moneda(Haber) : "";
    public string SaldoTexto => Formato.Moneda(Saldo);
    public string GlosaTexto => string.IsNullOrWhiteSpace(Glosa) ? "(Sin glosa)" : Glosa;
    public Color OrigenFondo => EstiloContable.Origen(Origen).Fondo;
    public Color OrigenColor => EstiloContable.Origen(Origen).Texto;
}

public class ResumenMensualCuenta
{
    public DateTime Mes { get; set; }
    public decimal Debe { get; set; }
    public decimal Haber { get; set; }
    public decimal Neto { get; set; }
    public decimal SaldoAcumulado { get; set; }
    public int Movimientos { get; set; }

    public string MesTexto => Formato.Capitalizar(Mes.ToString("MMMM yyyy", Formato.Cultura));
    public string MesCorto => Mes.ToString("MMM yy", Formato.Cultura);
    public string DebeTexto => Formato.Moneda(Debe);
    public string HaberTexto => Formato.Moneda(Haber);
    public string NetoTexto => (Neto >= 0 ? "+ " : "− ") + Formato.Moneda(Math.Abs(Neto));
    public Color NetoColor => Neto >= 0 ? Color.FromArgb("#047857") : Color.FromArgb("#BE123C");
    public string SaldoTexto => Formato.Moneda(SaldoAcumulado);
}

// ===================================================================
//  RF-CON-05: Estado de Resultados Integral (IFRS Pyme / SII Chile)
// ===================================================================
public class LineaEstadoResultadosItem
{
    public string CodigoCuenta { get; set; } = string.Empty;
    public string NombreCuenta { get; set; } = string.Empty;
    public string CategoriaRubro { get; set; } = string.Empty; // INGRESO, COSTO_VENTA, GASTO_OPERACIONAL
    public decimal Monto { get; set; }
    public double ParticipacionSobreVentas { get; set; }

    public string MontoTexto => Formato.Moneda(Monto);
    public string ParticipacionTexto => ParticipacionSobreVentas.ToString("P1", Formato.Cultura);
    public string PorcentajeSobreIngresosTexto => ParticipacionTexto;
    public Color RubroFondo => CategoriaRubro switch
    {
        "INGRESO" => Color.FromArgb("#D1FAE5"),
        "COSTO_VENTA" => Color.FromArgb("#FEF3C7"),
        _ => Color.FromArgb("#FFE4E6")
    };
    public Color RubroColor => CategoriaRubro switch
    {
        "INGRESO" => Color.FromArgb("#047857"),
        "COSTO_VENTA" => Color.FromArgb("#B45309"),
        _ => Color.FromArgb("#BE123C")
    };
}

// ===================================================================
//  RF-CON-09: Reporte Centralizador de Impuestos (Libro Compras/Ventas + F29 SII)
// ===================================================================
public class DocumentoLibroVentasItem
{
    public int IdVenta { get; set; }
    public DateTime FechaEmision { get; set; }
    public string TipoDocumento { get; set; } = "BOLETA";
    public string Folio { get; set; } = string.Empty;
    public string RutCliente { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public decimal MontoDescuento { get; set; }
    public decimal MontoNeto { get; set; }
    public decimal MontoIva { get; set; }
    public decimal MontoTotal { get; set; }
    public string FormaPago { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;

    public bool EsNotaCredito => TipoDocumento.ToUpperInvariant() == "NOTA_CREDITO";
    public string CodigoSii => TipoDocumento.ToUpperInvariant() switch
    {
        "FACTURA" => "DTE 33",
        "BOLETA" => "DTE 39",
        "NOTA_CREDITO" => "DTE 61",
        _ => "DTE 33"
    };
    public string TipoDteTexto => TipoDocumento.ToUpperInvariant() switch
    {
        "FACTURA" => "Factura Electrónica (33)",
        "BOLETA" => "Boleta Afecta (39)",
        "NOTA_CREDITO" => "Nota de Crédito (61)",
        _ => TipoDocumento
    };
    public string FechaTexto => FechaEmision.ToString("dd/MM/yyyy");
    public string NetoTexto => (EsNotaCredito ? "− " : "") + Formato.Moneda(MontoNeto);
    public string IvaTexto => (EsNotaCredito ? "− " : "") + Formato.Moneda(MontoIva);
    public string TotalTexto => (EsNotaCredito ? "− " : "") + Formato.Moneda(MontoTotal);
    public string TipoDte => TipoDteTexto;
    public string MontoNetoTexto => NetoTexto;
    public string MontoIvaTexto => IvaTexto;
    public string MontoTotalTexto => TotalTexto;
    public Color DteFondo => EsNotaCredito ? Color.FromArgb("#FFE4E6") : (TipoDocumento == "FACTURA" ? Color.FromArgb("#DBEAFE") : Color.FromArgb("#D1FAE5"));
    public Color DteColor => EsNotaCredito ? Color.FromArgb("#BE123C") : (TipoDocumento == "FACTURA" ? Color.FromArgb("#1D4ED8") : Color.FromArgb("#047857"));
}

public class DocumentoLibroComprasItem
{
    public int IdRecepcion { get; set; }
    public DateTime FechaFactura { get; set; }
    public string NumeroFactura { get; set; } = string.Empty;
    public string NumeroOrdenCompra { get; set; } = string.Empty;
    public string RutProveedor { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public decimal MontoNeto { get; set; }
    public decimal MontoIvaCredito { get; set; }
    public decimal MontoTotal { get; set; }
    public string EstadoPago { get; set; } = "PENDIENTE";

    public string CodigoSii => "DTE 33";
    public string TipoDteTexto => "Factura Compra Giro (33)";
    public string FechaTexto => FechaFactura.ToString("dd/MM/yyyy");
    public string NetoTexto => Formato.Moneda(MontoNeto);
    public string IvaTexto => Formato.Moneda(MontoIvaCredito);
    public string TotalTexto => Formato.Moneda(MontoTotal);
    public string TipoDte => TipoDteTexto;
    public string Folio => NumeroFactura;
    public string MontoNetoTexto => NetoTexto;
    public string MontoIvaTexto => IvaTexto;
    public string MontoTotalTexto => TotalTexto;
    public Color EstadoFondo => EstadoPago == "PAGADO" ? Color.FromArgb("#D1FAE5") : Color.FromArgb("#FEF3C7");
    public Color EstadoColor => EstadoPago == "PAGADO" ? Color.FromArgb("#047857") : Color.FromArgb("#B45309");
}

public class ResumenDteSiiItem
{
    public string CodigoSii { get; set; } = string.Empty;
    public string TipoDocumento { get; set; } = string.Empty;
    public string Libro { get; set; } = string.Empty; // VENTAS o COMPRAS
    public int CantidadDocumentos { get; set; }
    public decimal TotalNeto { get; set; }
    public decimal TotalIva { get; set; }
    public decimal TotalBruto { get; set; }

    public string CantidadTexto => CantidadDocumentos == 1 ? "1 doc." : $"{CantidadDocumentos} docs.";
    public string NetoTexto => Formato.Moneda(TotalNeto);
    public string IvaTexto => Formato.Moneda(TotalIva);
    public string TotalTexto => Formato.Moneda(TotalBruto);
    public string CodigoDte => CodigoSii;
    public string NombreDte => TipoDocumento;
    public string TipoRegistro => Libro;
    public string MontoNetoTexto => NetoTexto;
    public string MontoIvaTexto => IvaTexto;
    public string MontoTotalTexto => TotalTexto;
}

public class LineaFormulario29Item
{
    public string CodigoCasilla { get; set; } = string.Empty;
    public string GlosaCasilla { get; set; } = string.Empty;
    public string ReferenciaLegal { get; set; } = string.Empty;
    public string Operador { get; set; } = "+";
    public decimal Monto { get; set; }
    public bool EsTotal { get; set; }

    public string MontoTexto => Formato.Moneda(Monto);
    public Color FondoFila => EsTotal ? Color.FromArgb("#EEF2FF") : Colors.White;
    public FontAttributes Fuente => EsTotal ? FontAttributes.Bold : FontAttributes.None;
    public Color ColorMonto => Operador == "−" ? Color.FromArgb("#047857") : (EsTotal ? Color.FromArgb("#1D4ED8") : Color.FromArgb("#0F172A"));
    public string CodigoSii => CodigoCasilla;
    public string Glosa => GlosaCasilla;
    public string Naturaleza => Operador;
    public Color ColorTexto => ColorMonto;
}
