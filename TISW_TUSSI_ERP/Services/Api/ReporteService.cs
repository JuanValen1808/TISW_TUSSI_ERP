using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TISW_TUSSI_ERP.Models.Inventario;

namespace TISW_TUSSI_ERP.Services.Api;

public class ReporteService
{
    public ReporteService()
    {
        // Licencia comunitaria de QuestPDF
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerarReporteInventarioPdf(ResumenInventario resumen, List<ProductoModel> productos)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(QuestPDF.Helpers.Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                // Encabezado del Reporte
                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("FarmSalud ERP").FontSize(20).Bold().FontColor("#1D6FD8");
                        col.Item().Text("Informe Consolidado de Inventario y Stock").FontSize(12).SemiBold().FontColor("#4B5563");
                    });

                    row.ConstantItem(150).Column(col =>
                    {
                        col.Item().AlignRight().Text($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(9).FontColor("#6B7280");
                        col.Item().AlignRight().Text("Estado: Oficial").FontSize(9).Bold().FontColor("#15803D");
                    });
                });

                // Cuerpo del informe
                page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                {
                    // Bloque de Resumen / KPIs
                    col.Item().Background("#F8FAFC").Padding(10).Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Total SKUs").FontSize(8).FontColor("#6B7280");
                            c.Item().Text($"{resumen?.TotalProductos ?? 0}").FontSize(12).Bold();
                        });
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Stock Crítico").FontSize(8).FontColor("#6B7280");
                            c.Item().Text($"{resumen?.ProductosStockBajo ?? 0}").FontSize(12).Bold().FontColor("#B91C1C");
                        });
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Por Vencer").FontSize(8).FontColor("#6B7280");
                            c.Item().Text($"{resumen?.ProductosPorVencer ?? 0}").FontSize(12).Bold().FontColor("#D97706");
                        });
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Valorización Total").FontSize(8).FontColor("#6B7280");
                            c.Item().Text(resumen?.ValorizacionTotalTexto ?? "$0").FontSize(12).Bold().FontColor("#1D6FD8");
                        });
                    });

                    col.Item().Height(15);

                    // Tabla Detallada de Productos
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(80);  // SKU
                            columns.RelativeColumn(2);  // Nombre
                            columns.RelativeColumn(1);  // Categoría
                            columns.ConstantColumn(50);  // Stock
                            columns.ConstantColumn(70);  // Precio Vta
                            columns.ConstantColumn(70);  // Estado
                        });

                        // Encabezados de Tabla
                        table.Header(header =>
                        {
                            header.Cell().Background("#E8F1FD").Padding(5).Text("SKU").Bold();
                            header.Cell().Background("#E8F1FD").Padding(5).Text("Producto").Bold();
                            header.Cell().Background("#E8F1FD").Padding(5).Text("Categoría").Bold();
                            header.Cell().Background("#E8F1FD").Padding(5).AlignRight().Text("Stock").Bold();
                            header.Cell().Background("#E8F1FD").Padding(5).AlignRight().Text("Precio").Bold();
                            header.Cell().Background("#E8F1FD").Padding(5).Text("Estado").Bold();
                        });

                        // Filas
                        foreach (var p in productos)
                        {
                            table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).Text(p.Sku ?? string.Empty);
                            table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).Text(p.NombreComercial ?? string.Empty);
                            table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).Text(p.CategoriaNombre ?? string.Empty);
                            table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).AlignRight().Text($"{p.StockActual}");
                            table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).AlignRight().Text(p.PrecioVentaTexto ?? string.Empty);
                            table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).Text(p.EstadoTexto ?? string.Empty);
                        }
                    });
                });

                // Pie de página
                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Página ");
                    x.CurrentPageNumber();
                    x.Span(" de ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }
}