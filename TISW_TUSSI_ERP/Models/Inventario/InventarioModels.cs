using System.Text.Json.Serialization;
using System;
using System.Text.Json.Serialization;

namespace TISW_TUSSI_ERP.Models.Inventario;

public class ResumenInventario
{
    [JsonPropertyName("totalSkus")]
    public int TotalProductos { get; set; }

    [JsonPropertyName("productosStockBajo")]
    public int ProductosStockBajo { get; set; }

    [JsonPropertyName("productosPorVencer")]
    public int ProductosPorVencer { get; set; }

    [JsonPropertyName("valorTotalInventario")]
    public decimal ValorizacionTotal { get; set; }

    // Formato para la UI
    public string ValorizacionTotalTexto => ValorizacionTotal.ToString("C0");
}

public class ProductoModel
{
    [JsonPropertyName("idProducto")]
    public int IdProducto { get; set; }

    [JsonPropertyName("sku")]
    public string Sku { get; set; } = string.Empty;

    [JsonPropertyName("codigoBarras")]
    public string CodigoBarras { get; set; } = string.Empty;

    [JsonPropertyName("nombreComercial")]
    public string NombreComercial { get; set; } = string.Empty;

    [JsonPropertyName("principioActivo")]
    public string PrincipioActivo { get; set; } = string.Empty;

    [JsonPropertyName("registroSanitario")]
    public string RegistroSanitario { get; set; } = string.Empty;

    [JsonPropertyName("formatoPresentacion")]
    public string FormatoPresentacion { get; set; } = string.Empty;

    [JsonPropertyName("idCategoria")]
    public int IdCategoria { get; set; }

    [JsonPropertyName("categoriaNombre")]
    public string CategoriaNombre { get; set; } = string.Empty;

    [JsonPropertyName("precioVenta")]
    public decimal PrecioVenta { get; set; }

    [JsonPropertyName("costoPromedio")]
    public decimal CostoPromedio { get; set; }

    [JsonPropertyName("stockActual")]
    public int StockActual { get; set; }

    [JsonPropertyName("stockMinimo")]
    public int StockMinimo { get; set; }

    [JsonPropertyName("puntoReorden")]
    public int PuntoReorden { get; set; }

    [JsonPropertyName("activo")]
    public bool Activo { get; set; }

    // Propiedades calculadas para XAML
    public string EstadoStock => StockActual <= 0 ? "Agotado" : (StockActual <= StockMinimo ? "Crítico" : (StockActual <= PuntoReorden ? "Reorden" : "Normal"));
    public decimal ValorizacionTotal => StockActual * CostoPromedio;
    public string ValorizacionTotalTexto => ValorizacionTotal.ToString("C0");
    public string PrecioVentaTexto => PrecioVenta.ToString("C0");
    public string CostoPromedioTexto => CostoPromedio.ToString("C0");
    public string StockActualTexto => StockActual.ToString("N0");
    public string PuntoReordenTexto => $"{PuntoReorden} unidades";
    public string EstadoTexto => EstadoStock.ToUpper();

    public string EstadoColor => EstadoStock switch
    {
        "Crítico" or "Agotado" => "#B91C1C",
        "Reorden" => "#D97706",
        _ => "#15803D"
    };

    public string EstadoFondo => EstadoStock switch
    {
        "Crítico" or "Agotado" => "#FEE2E2",
        "Reorden" => "#FEF3C7",
        _ => "#DCFCE7"
    };
}

public class LoteModel
{
    [JsonPropertyName("idLote")]
    public int IdLote { get; set; }

    [JsonPropertyName("idProducto")]
    public int IdProducto { get; set; }

    [JsonPropertyName("sku")]
    public string Sku { get; set; } = string.Empty;

    [JsonPropertyName("nombreProducto")]
    public string NombreProducto { get; set; } = string.Empty;

    [JsonPropertyName("numeroLote")]
    public string NumeroLote { get; set; } = string.Empty;

    [JsonPropertyName("fechaVencimiento")]
    public DateTime FechaVencimiento { get; set; }

    [JsonPropertyName("stockActual")]
    public int StockActual { get; set; }

    [JsonPropertyName("costoUnitario")]
    public decimal CostoUnitario { get; set; }

    [JsonPropertyName("estadoLote")]
    public string EstadoLote { get; set; } = "Vigente";

    // Formato para la UI
    public string FechaVencimientoTexto => FechaVencimiento.ToString("dd/MM/yyyy");
    public string CostoTexto => CostoUnitario.ToString("C0");
    public string EstadoTexto => EstadoLote.ToUpper();

    public string EstadoColor => EstadoLote switch
    {
        "Vencido" => "#B91C1C",
        "Por Vencer" => "#D97706",
        _ => "#15803D"
    };

    public string EstadoFondo => EstadoLote switch
    {
        "Vencido" => "#FEE2E2",
        "Por Vencer" => "#FEF3C7",
        _ => "#DCFCE7"
    };
}

public class KardexMovimientoModel
{
    public int IdKardex { get; set; }
    public int IdProducto { get; set; }
    public string NombreProducto { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string NumeroLote { get; set; } = string.Empty;
    public string TipoMovimiento { get; set; } = string.Empty;
    public string OrigenDocumento { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public int SaldoStockProducto { get; set; }
    public DateTime FechaMovimiento { get; set; }
}