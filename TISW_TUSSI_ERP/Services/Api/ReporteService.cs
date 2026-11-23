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


    public byte[] GenerarReporteKardexPdf(List<KardexMovimientoModel> movimientos, string filtroProducto = "Todos")
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape()); // Horizontal para que quepan bien todas las columnas del Kardex
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(QuestPDF.Helpers.Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9));

                // Encabezado del Reporte
                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("FarmSalud ERP").FontSize(18).Bold().FontColor("#1D6FD8");
                        col.Item().Text($"Informe Oficial de Kardex y Trazabilidad · Filtro: {filtroProducto}").FontSize(11).SemiBold().FontColor("#4B5563");
                    });

                    row.ConstantItem(180).Column(col =>
                    {
                        col.Item().AlignRight().Text($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor("#6B7280");
                        col.Item().AlignRight().Text("Trazabilidad de Lotes").FontSize(8).Bold().FontColor("#15803D");
                    });
                });

                // Cuerpo del informe con la tabla de movimientos
                page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(90);  // Fecha
                            columns.ConstantColumn(80);  // SKU
                            columns.RelativeColumn(2);   // Producto
                            columns.ConstantColumn(80);  // Lote
                            columns.ConstantColumn(75);  // Movimiento
                            columns.ConstantColumn(75);  // Origen
                            columns.ConstantColumn(55);  // Cantidad
                            columns.ConstantColumn(70);  // Costo U.
                            columns.ConstantColumn(70);  // Stock Final
                        });

                        // Encabezados de Tabla
                        table.Header(header =>
                        {
                            header.Cell().Background("#E8F1FD").Padding(5).Text("Fecha").Bold();
                            header.Cell().Background("#E8F1FD").Padding(5).Text("SKU").Bold();
                            header.Cell().Background("#E8F1FD").Padding(5).Text("Producto").Bold();
                            header.Cell().Background("#E8F1FD").Padding(5).Text("N° Lote").Bold();
                            header.Cell().Background("#E8F1FD").Padding(5).Text("Movimiento").Bold();
                            header.Cell().Background("#E8F1FD").Padding(5).Text("Origen").Bold();
                            header.Cell().Background("#E8F1FD").Padding(5).AlignRight().Text("Cant.").Bold();
                            header.Cell().Background("#E8F1FD").Padding(5).AlignRight().Text("Costo U.").Bold();
                            header.Cell().Background("#E8F1FD").Padding(5).AlignRight().Text("Stock Fin.").Bold();
                        });

                        // Filas de movimientos
                        foreach (var m in movimientos)
                        {
                            table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).Text(m.FechaTexto);
                            table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).Text(m.Sku);
                            table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).Text(m.NombreProducto);
                            table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).Text(m.NumeroLote);
                            table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).Text(m.TipoMovimiento);
                            table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).Text(m.OrigenDocumento);
                            table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).AlignRight().Text(m.Cantidad.ToString());
                            table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).AlignRight().Text(m.CostoUnitarioTexto);
                            table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).AlignRight().Text(m.SaldoStockProducto.ToString());
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

    public byte[] GenerarReporteMermasPdf(List<MermaModel> mermas)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(QuestPDF.Helpers.Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9));

                // Encabezado del Reporte
                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("FarmSalud ERP").FontSize(18).Bold().FontColor("#1D6FD8");
                        col.Item().Text("Informe Oficial de Control de Mermas y Bajas Operacionales").FontSize(11).SemiBold().FontColor("#4B5563");
                    });

                    row.ConstantItem(180).Column(col =>
                    {
                        col.Item().AlignRight().Text($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor("#6B7280");
                        col.Item().AlignRight().Text("Auditoría de Bodega").FontSize(8).Bold().FontColor("#DC2626");
                    });
                });

                // Cuerpo del informe con la tabla detallada
                page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(90);  // Fecha
                            columns.ConstantColumn(80);  // SKU
                            columns.RelativeColumn(2);   // Producto
                            columns.ConstantColumn(70);  // N° Lote
                            columns.ConstantColumn(60);  // Cantidad
                            columns.ConstantColumn(80);  // Motivo
                            columns.ConstantColumn(90);  // Responsable
                        });

                        // Encabezados de Tabla
                        table.Header(header =>
                        {
                            header.Cell().Background("#E8F1FD").Padding(5).Text("Fecha").Bold();
                            header.Cell().Background("#E8F1FD").Padding(5).Text("SKU").Bold();
                            header.Cell().Background("#E8F1FD").Padding(5).Text("Producto").Bold();
                            header.Cell().Background("#E8F1FD").Padding(5).Text("N° Lote").Bold();
                            header.Cell().Background("#E8F1FD").Padding(5).AlignRight().Text("Cant.").Bold();
                            header.Cell().Background("#E8F1FD").Padding(5).Text("Motivo").Bold();
                            header.Cell().Background("#E8F1FD").Padding(5).Text("Responsable").Bold();
                        });

                        // Filas de registros de mermas
                        foreach (var m in mermas)
                        {
                            table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).Text(m.FechaTexto);
                            table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).Text(m.Sku);
                            table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).Text(m.NombreProducto);
                            table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).Text(m.NumeroLote);
                            table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).AlignRight().Text($"{m.CantidadBaja} un.");
                            table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).Text(m.Motivo);
                            table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).Text(m.UsuarioResponsable);
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

public byte[] GenerarReporteStockMinimoPdf(List<ProductoModel> productosBajoReorden)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(QuestPDF.Helpers.Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9));

                // Encabezado del Reporte
                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("FarmSalud ERP").FontSize(18).Bold().FontColor("#1D6FD8");
                        col.Item().Text("Informe Oficial de Alertas de Stock Mínimo y Reorden (CU-INV-06)").FontSize(11).SemiBold().FontColor("#4B5563");
                    });

                    row.ConstantItem(180).Column(col =>
                    {
                        col.Item().AlignRight().Text($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor("#6B7280");
                        col.Item().AlignRight().Text("Gestión de Reabastecimiento").FontSize(8).Bold().FontColor("#D97706");
                    });
                });

                // Cuerpo del informe con la tabla detallada
                page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(80);  // SKU
                            columns.RelativeColumn(2);   // Producto
                            columns.ConstantColumn(60);  // Stock Actual
                            columns.ConstantColumn(60);  // Stock Mín.
                            columns.ConstantColumn(60);  // Reorden
                            columns.ConstantColumn(60);  // Déficit
                            columns.ConstantColumn(80);  // Estado
                        });

                        // Encabezados de Tabla
                        table.Header(header =>
                        {
                            header.Cell().Background("#FEF3C7").Padding(5).Text("SKU").Bold();
                            header.Cell().Background("#FEF3C7").Padding(5).Text("Producto").Bold();
                            header.Cell().Background("#FEF3C7").Padding(5).AlignRight().Text("Stock").Bold();
                            header.Cell().Background("#FEF3C7").Padding(5).AlignRight().Text("Mínimo").Bold();
                            header.Cell().Background("#FEF3C7").Padding(5).AlignRight().Text("Reorden").Bold();
                            header.Cell().Background("#FEF3C7").Padding(5).AlignRight().Text("Déficit").Bold();
                            header.Cell().Background("#FEF3C7").Padding(5).Text("Estado").Bold();
                        });

                        // Filas de productos bajo reorden
                        foreach (var p in productosBajoReorden)
                        {
                            int deficit = Math.Max(0, p.PuntoReorden - p.StockActual);

                            table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).Text(p.Sku);
                            table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).Text(p.NombreComercial);
                            table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).AlignRight().Text($"{p.StockActual}");
                            table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).AlignRight().Text($"{p.StockMinimo}");
                            table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).AlignRight().Text($"{p.PuntoReorden}");
                            table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).AlignRight().Text($"{deficit}").FontColor("#B91C1C").Bold();
                            table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).Text(p.EstadoStock);
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


    /// <summary>
    /// CU-INV-09: Reporte de existencias y vencimientos en Bodega agrupados por ubicación/estante.
    /// Resalta lotes dentro de umbrales de 30, 60 y 90 días.
    /// </summary>
    public byte[] GenerarReporteExistenciasBodegaPdf(
        List<LoteBodegaItemModel> lotesBodega,
        List<UbicacionModel> ubicacionesBodega,
        string filtroUbicacion = "Todas")
    {
        var lotes = lotesBodega ?? new List<LoteBodegaItemModel>();
        var ubicaciones = ubicacionesBodega ?? new List<UbicacionModel>();

        int totalUnidadesBodega = lotes.Sum(x => x.StockUbicadoBodega);
        int totalSinUbicar = lotes.Sum(x => x.StockSinUbicar);
        int lotesCriticos30 = lotes.Count(x => x.StockUbicadoBodega > 0 && (x.FechaVencimiento.Date - DateTime.Today).TotalDays <= 30);
        int lotesAlerta60 = lotes.Count(x => x.StockUbicadoBodega > 0 && (x.FechaVencimiento.Date - DateTime.Today).TotalDays > 30 && (x.FechaVencimiento.Date - DateTime.Today).TotalDays <= 60);
        int lotesAlerta90 = lotes.Count(x => x.StockUbicadoBodega > 0 && (x.FechaVencimiento.Date - DateTime.Today).TotalDays > 60 && (x.FechaVencimiento.Date - DateTime.Today).TotalDays <= 90);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(QuestPDF.Helpers.Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9));

                // Encabezado
                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("FarmSalud ERP").FontSize(18).Bold().FontColor("#1D6FD8");
                        col.Item().Text($"CU-INV-09 · Reporte de Existencias y Vencimientos en Bodega · Filtro: {filtroUbicacion}")
                            .FontSize(11).SemiBold().FontColor("#4B5563");
                        col.Item().Text($"Estantes/Ubicaciones activas registradas: {ubicaciones.Count}")
                            .FontSize(8).FontColor("#6B7280");
                    });

                    row.ConstantItem(190).Column(col =>
                    {
                        col.Item().AlignRight().Text($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor("#6B7280");
                        col.Item().AlignRight().Text("Control de Bodega y Caducidad").FontSize(8).Bold().FontColor("#15803D");
                    });
                });

                page.Content().PaddingVertical(0.8f, Unit.Centimetre).Column(col =>
                {
                    // Resumen de Bodega y Umbrales de Vencimiento (30, 60 y 90 días)
                    col.Item().Background("#F8FAFC").Padding(10).Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Stock en Bodega").FontSize(8).FontColor("#6B7280");
                            c.Item().Text($"{totalUnidadesBodega} un.").FontSize(12).Bold().FontColor("#1D6FD8");
                        });
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Sin Ubicación (CU-INV-07)").FontSize(8).FontColor("#6B7280");
                            c.Item().Text($"{totalSinUbicar} un.").FontSize(12).Bold().FontColor("#374151");
                        });
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Vencen ≤ 30 días").FontSize(8).FontColor("#6B7280");
                            c.Item().Text($"{lotesCriticos30} lotes").FontSize(12).Bold().FontColor("#B91C1C");
                        });
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Vencen 31–60 días").FontSize(8).FontColor("#6B7280");
                            c.Item().Text($"{lotesAlerta60} lotes").FontSize(12).Bold().FontColor("#D97706");
                        });
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Vencen 61–90 días").FontSize(8).FontColor("#6B7280");
                            c.Item().Text($"{lotesAlerta90} lotes").FontSize(12).Bold().FontColor("#B45309");
                        });
                    });

                    col.Item().Height(12);

                    if (lotes.Count == 0)
                    {
                        col.Item().Background("#FEF3C7").Padding(12).Text("No existen lotes con existencias en bodega para el filtro seleccionado.")
                            .FontSize(10).SemiBold().FontColor("#92400E");
                        return;
                    }

                    // Agrupado por estado de ubicación / lote
                    var grupos = lotes
                        .OrderBy(x => x.FechaVencimiento)
                        .ThenBy(x => x.NombreProducto)
                        .GroupBy(x => x.StockUbicadoBodega > 0 ? "Lotes con Stock en Bodega" : "Sin ubicación asignada");

                    foreach (var grupo in grupos)
                    {
                        col.Item().PaddingTop(6).PaddingBottom(4)
                            .Text($"{grupo.Key} ({grupo.Count()} lotes)")
                            .FontSize(10).Bold().FontColor("#1F2937");

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(75);  // SKU
                                columns.RelativeColumn(2.2f);// Producto
                                columns.ConstantColumn(85);  // Lote
                                columns.ConstantColumn(75);  // Vencimiento
                                columns.ConstantColumn(65);  // En Bodega
                                columns.ConstantColumn(65);  // Sin Ubicar
                                columns.ConstantColumn(95);  // Estado Lote
                                columns.ConstantColumn(95);  // Umbral Caducidad
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background("#E8F1FD").Padding(5).Text("SKU").Bold();
                                header.Cell().Background("#E8F1FD").Padding(5).Text("Producto").Bold();
                                header.Cell().Background("#E8F1FD").Padding(5).Text("N° Lote").Bold();
                                header.Cell().Background("#E8F1FD").Padding(5).Text("Vencimiento").Bold();
                                header.Cell().Background("#E8F1FD").Padding(5).AlignRight().Text("Bodega").Bold();
                                header.Cell().Background("#E8F1FD").Padding(5).AlignRight().Text("Sin Ubicar").Bold();
                                header.Cell().Background("#E8F1FD").Padding(5).Text("Estado Lote").Bold();
                                header.Cell().Background("#E8F1FD").Padding(5).Text("Alerta Venc.").Bold();
                            });

                            foreach (var item in grupo)
                            {
                                double diasRestantes = (item.FechaVencimiento.Date - DateTime.Today).TotalDays;
                                string umbralTexto;
                                string colorHex;

                                if (diasRestantes < 0)
                                {
                                    umbralTexto = "VENCIDO";
                                    colorHex = "#B91C1C";
                                }
                                else if (diasRestantes <= 30)
                                {
                                    umbralTexto = "Crítico (≤30d)";
                                    colorHex = "#B91C1C";
                                }
                                else if (diasRestantes <= 60)
                                {
                                    umbralTexto = "Alerta (≤60d)";
                                    colorHex = "#D97706";
                                }
                                else if (diasRestantes <= 90)
                                {
                                    umbralTexto = "Preventivo (≤90d)";
                                    colorHex = "#B45309";
                                }
                                else
                                {
                                    umbralTexto = "Vigente (>90d)";
                                    colorHex = "#15803D";
                                }

                                string estadoLote = item.StockSinUbicar == 0
                                    ? "100% Ubicado"
                                    : (item.StockUbicadoBodega > 0 ? "Parcial" : "Sin ubicación");

                                table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).Text(item.Sku ?? string.Empty);
                                table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).Text(item.NombreProducto ?? string.Empty);
                                table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).Text(item.NumeroLote ?? string.Empty);
                                table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).Text($"{item.FechaVencimiento:dd/MM/yyyy}");
                                table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).AlignRight().Text($"{item.StockUbicadoBodega} un.");
                                table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).AlignRight().Text($"{item.StockSinUbicar} un.");
                                table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).Text(estadoLote);
                                table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).Text(umbralTexto).FontColor(colorHex).Bold();
                            }
                        });
                    }
                });

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

    /// <summary>
    /// CU-INV-10: Reporte de stock y caducidad en Sala de Ventas agrupado por exhibidor/ubicación.
    /// Marca como "Reponer" los productos sin existencias en sala que tienen stock en bodega,
    /// y alerta lotes vencidos en sala.
    /// </summary>
    public byte[] GenerarReporteStockSalaVentasPdf(
        List<LoteSalaItemModel> lotesSala,
        List<UbicacionModel> exhibidoresSala,
        string filtroExhibidor = "Todos")
    {
        var lotes = lotesSala ?? new List<LoteSalaItemModel>();
        var exhibidores = exhibidoresSala ?? new List<UbicacionModel>();

        int totalUnidadesSala = lotes.Sum(x => x.StockUbicadoSala);
        int lotesReponer = lotes.Count(x => x.StockUbicadoSala == 0 && x.StockUbicadoBodega > 0);
        int lotesVencidosEnSala = lotes.Count(x => x.StockUbicadoSala > 0 && x.FechaVencimiento.Date < DateTime.Today);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(QuestPDF.Helpers.Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9));

                // Encabezado
                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("FarmSalud ERP").FontSize(18).Bold().FontColor("#1D6FD8");
                        col.Item().Text($"CU-INV-10 · Reporte de Stock y Caducidad en Sala de Ventas · Exhibidor: {filtroExhibidor}")
                            .FontSize(11).SemiBold().FontColor("#4B5563");
                        col.Item().Text($"Góndolas / Vitrinas / Mostradores registrados: {exhibidores.Count}")
                            .FontSize(8).FontColor("#6B7280");
                    });

                    row.ConstantItem(190).Column(col =>
                    {
                        col.Item().AlignRight().Text($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor("#6B7280");
                        col.Item().AlignRight().Text("Exhibición y Reposición").FontSize(8).Bold().FontColor("#1D6FD8");
                    });
                });

                page.Content().PaddingVertical(0.8f, Unit.Centimetre).Column(col =>
                {
                    // Resumen de Sala de Ventas
                    col.Item().Background("#F8FAFC").Padding(10).Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Unidades Expuestas en Sala").FontSize(8).FontColor("#6B7280");
                            c.Item().Text($"{totalUnidadesSala} un.").FontSize(12).Bold().FontColor("#1D6FD8");
                        });
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Pendientes de Reposición").FontSize(8).FontColor("#6B7280");
                            c.Item().Text($"{lotesReponer} lotes").FontSize(12).Bold().FontColor("#D97706");
                        });
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Vencidos en Sala (Retirar)").FontSize(8).FontColor("#6B7280");
                            c.Item().Text($"{lotesVencidosEnSala} lotes").FontSize(12).Bold().FontColor("#B91C1C");
                        });
                    });

                    col.Item().Height(12);

                    if (lotes.Count == 0)
                    {
                        col.Item().Background("#FEF3C7").Padding(12).Text("No hay productos o lotes configurados para exhibición en sala de ventas con el filtro actual.")
                            .FontSize(10).SemiBold().FontColor("#92400E");
                        return;
                    }

                    // Agrupado por condición de exhibición/reposición
                    var grupos = lotes
                        .OrderByDescending(x => x.StockUbicadoSala == 0 && x.StockUbicadoBodega > 0)
                        .ThenBy(x => x.FechaVencimiento)
                        .ThenBy(x => x.NombreProducto)
                        .GroupBy(x => (x.StockUbicadoSala == 0 && x.StockUbicadoBodega > 0)
                            ? "Requieren Reposición (Sin Stock en Sala / Con Stock en Bodega)"
                            : "Lotes en Sala de Ventas");

                    foreach (var grupo in grupos)
                    {
                        col.Item().PaddingTop(6).PaddingBottom(4)
                            .Text($"{grupo.Key} ({grupo.Count()})")
                            .FontSize(10).Bold().FontColor("#1F2937");

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(75);   // SKU
                                columns.RelativeColumn(2.2f); // Producto
                                columns.ConstantColumn(85);   // Lote
                                columns.ConstantColumn(75);   // Vencimiento
                                columns.ConstantColumn(65);   // En Sala
                                columns.ConstantColumn(65);   // En Bodega
                                columns.ConstantColumn(115);  // Estado / Acción
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background("#E8F1FD").Padding(5).Text("SKU").Bold();
                                header.Cell().Background("#E8F1FD").Padding(5).Text("Producto").Bold();
                                header.Cell().Background("#E8F1FD").Padding(5).Text("N° Lote").Bold();
                                header.Cell().Background("#E8F1FD").Padding(5).Text("Vencimiento").Bold();
                                header.Cell().Background("#E8F1FD").Padding(5).AlignRight().Text("En Sala").Bold();
                                header.Cell().Background("#E8F1FD").Padding(5).AlignRight().Text("Bodega").Bold();
                                header.Cell().Background("#E8F1FD").Padding(5).Text("Estado / Reposición").Bold();
                            });

                            foreach (var item in grupo)
                            {
                                bool estaVencidoEnSala = item.StockUbicadoSala > 0 && item.FechaVencimiento.Date < DateTime.Today;
                                bool requiereReponer = item.StockUbicadoSala == 0 && item.StockUbicadoBodega > 0;

                                string estadoTexto;
                                string estadoColor;

                                if (estaVencidoEnSala)
                                {
                                    estadoTexto = "VENCIDO - Retirar (CU-INV-03)";
                                    estadoColor = "#B91C1C";
                                }
                                else if (requiereReponer)
                                {
                                    estadoTexto = "Reponer";
                                    estadoColor = "#D97706";
                                }
                                else if (item.StockUbicadoSala > 0)
                                {
                                    estadoTexto = "Exhibido";
                                    estadoColor = "#15803D";
                                }
                                else
                                {
                                    estadoTexto = "Sin Stock";
                                    estadoColor = "#6B7280";
                                }

                                table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).Text(item.Sku ?? string.Empty);
                                table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).Text(item.NombreProducto ?? string.Empty);
                                table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).Text(item.NumeroLote ?? string.Empty);
                                table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).Text($"{item.FechaVencimiento:dd/MM/yyyy}");
                                table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).AlignRight().Text($"{item.StockUbicadoSala} un.");
                                table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).AlignRight().Text($"{item.StockUbicadoBodega} un.");
                                table.Cell().BorderBottom(1).BorderColor("#E5E7EB").Padding(4).Text(estadoTexto).FontColor(estadoColor).Bold();
                            }
                        });
                    }
                });

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