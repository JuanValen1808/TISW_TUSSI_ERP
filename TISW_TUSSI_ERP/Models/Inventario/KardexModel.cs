using System.Text.Json.Serialization;

namespace TISW_TUSSI_ERP.Models.Inventario
{
    public class KardexMovimientoModel
    {
        [JsonPropertyName("idKardex")]
        public long IdKardex { get; set; }

        [JsonPropertyName("idProducto")]
        public int IdProducto { get; set; }

        [JsonPropertyName("nombreProducto")]
        public string NombreProducto { get; set; } = string.Empty;

        [JsonPropertyName("sku")]
        public string Sku { get; set; } = string.Empty;

        [JsonPropertyName("numeroLote")]
        public string NumeroLote { get; set; } = string.Empty;

        [JsonPropertyName("tipoMovimiento")]
        public string TipoMovimiento { get; set; } = string.Empty; // ENTRADA, SALIDA, AJUSTE, MERMA

        [JsonPropertyName("origenDocumento")]
        public string OrigenDocumento { get; set; } = string.Empty; // COMPRA, VENTA, MERMA, AJUSTE, OTRO

        [JsonPropertyName("cantidad")]
        public int Cantidad { get; set; }

        [JsonPropertyName("costoUnitario")]
        public decimal CostoUnitario { get; set; }

        [JsonPropertyName("saldoStockProducto")]
        public int SaldoStockProducto { get; set; }

        [JsonPropertyName("fechaMovimiento")]
        public DateTime FechaMovimiento { get; set; }

        // Propiedades de formato visual para la UI de MAUI
        public string FechaTexto => FechaMovimiento.ToString("dd/MM/yyyy HH:mm");
        public string CostoUnitarioTexto => CostoUnitario.ToString("C0");
        public string CantidadFormateada => TipoMovimiento == "ENTRADA" || (TipoMovimiento == "AJUSTE" && Cantidad > 0) ? $"+{Cantidad}" : $"{Cantidad}";

        public string ColorTipo => TipoMovimiento switch
        {
            "ENTRADA" => "#15803D", // Verde
            "SALIDA" => "#1D4ED8",  // Azul
            "MERMA" => "#B91C1C",   // Rojo
            _ => "#D97706"          // Amarillo / Ámbar para Ajustes
        };

        public string FondoTipo => TipoMovimiento switch
        {
            "ENTRADA" => "#DCFCE7",
            "SALIDA" => "#DBEAFE",
            "MERMA" => "#FEE2E2",
            _ => "#FEF3C7"
        };
    }
}