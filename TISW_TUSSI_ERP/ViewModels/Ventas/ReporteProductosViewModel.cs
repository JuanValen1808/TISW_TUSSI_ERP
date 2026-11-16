
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Ventas;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Ventas;

public class ReporteProductosViewModel : BaseViewModel
{
    private readonly ReporteProductosService _servicio;

    private readonly List<ReporteProductoVenta>
        _productosOriginales = new();

    private readonly List<ReporteCategoriaVenta>
        _categoriasOriginales = new();

    private static readonly CultureInfo Cultura =
        CultureInfo.GetCultureInfo("es-CL");

    public ObservableCollection<ReporteProductoVenta>
        Productos
    { get; } = new();

    public ObservableCollection<ReporteCategoriaVenta>
        Categorias
    { get; } = new();

    public ObservableCollection<string>
        OpcionesCategoria
    { get; } = new();

    private string _busqueda = "";
    private string _categoriaSeleccionada = "Todas";
    private bool _usarFechas;
    private DateTime _fechaDesde = DateTime.Today.AddDays(-30);
    private DateTime _fechaHasta = DateTime.Today;
    private string _mensajeError = "";

    public string Busqueda
    {
        get => _busqueda;
        set
        {
            if (SetProperty(ref _busqueda, value))
                AplicarFiltros();
        }
    }

    public string CategoriaSeleccionada
    {
        get => _categoriaSeleccionada;
        set
        {
            if (SetProperty(ref _categoriaSeleccionada, value))
                AplicarFiltros();
        }
    }

    public bool UsarFechas
    {
        get => _usarFechas;
        set => SetProperty(ref _usarFechas, value);
    }

    public DateTime FechaDesde
    {
        get => _fechaDesde;
        set => SetProperty(ref _fechaDesde, value);
    }

    public DateTime FechaHasta
    {
        get => _fechaHasta;
        set => SetProperty(ref _fechaHasta, value);
    }

    public string MensajeError
    {
        get => _mensajeError;
        private set
        {
            if (SetProperty(ref _mensajeError, value))
                OnPropertyChanged(nameof(HayError));
        }
    }

    public bool HayError =>
        !string.IsNullOrWhiteSpace(MensajeError);

    public bool HayProductos => Productos.Count > 0;

    public string TotalVendidoTexto =>
        Productos.Sum(p => p.TotalVendido)
            .ToString("C0", Cultura);

    public string TotalUnidadesTexto =>
        Productos.Sum(p => p.UnidadesVendidas)
            .ToString("N0", Cultura);

    public string TotalProductosTexto =>
        Productos.Count.ToString("N0", Cultura);

    public string TotalCategoriasTexto =>
        Categorias.Count.ToString("N0", Cultura);

    public string CantidadResultadosTexto =>
        $"{Productos.Count} productos";

    public ICommand ComandoActualizar { get; }
    public ICommand ComandoLimpiar { get; }
    public ICommand ExportarPdfCommand { get; }
    public ICommand ExportarCsvCommand { get; }

    public ReporteProductosViewModel(
        ReporteProductosService servicio)
    {
        _servicio = servicio;
        Titulo = "Ventas por Producto y Categoría";

        OpcionesCategoria.Add("Todas");

        ComandoActualizar = new Command(
            async () => await CargarAsync());

        ComandoLimpiar = new Command(() =>
        {
            Busqueda = "";
            CategoriaSeleccionada = "Todas";
            UsarFechas = false;
            _ = CargarAsync();
        });

        ExportarPdfCommand = new Command(async () => await ExportarAsync(true));
        ExportarCsvCommand = new Command(async () => await ExportarAsync(false));
    }

    private async Task ExportarAsync(bool pdf)
    {
        if (!Productos.Any())
            await CargarAsync();
        if (!Productos.Any()) return;

        var secProd = new SeccionReporte(
            "Ventas por Producto (SKU)",
            new ColumnaReporte("SKU"),
            new ColumnaReporte("Producto Farmacéutico"),
            new ColumnaReporte("Categoría"),
            new ColumnaReporte("Unidades Vendidas", TipoColumna.Numero),
            new ColumnaReporte("N° Documentos", TipoColumna.Numero),
            new ColumnaReporte("Total Vendido", TipoColumna.Moneda));

        foreach (var p in Productos)
            secProd.Fila(p.Sku, p.NombreProducto, p.Categoria, p.UnidadesVendidas, p.NumeroVentas, p.TotalVendido);
        secProd.Total("TOTAL PRODUCTOS", "", "", Productos.Sum(x => x.UnidadesVendidas), Productos.Sum(x => x.NumeroVentas), Productos.Sum(x => x.TotalVendido));

        var secCat = new SeccionReporte(
            "Resumen por Categoría Farmacéutica",
            new ColumnaReporte("Categoría"),
            new ColumnaReporte("SKUs Distintos", TipoColumna.Numero),
            new ColumnaReporte("Unidades Vendidas", TipoColumna.Numero),
            new ColumnaReporte("Total Ingresos", TipoColumna.Moneda));

        foreach (var c in Categorias)
            secCat.Fila(c.Categoria, c.ProductosDistintos, c.UnidadesVendidas, c.TotalVendido);

        var doc = new DocumentoReporte
        {
            Titulo = "Reporte de Ventas por Producto y Categoría (RF-VEN-05)",
            Subtitulo = "Volumen de unidades dispensadas e ingresos generados por SKU y categoría terapéutica",
            Horizontal = true,
            Kpis =
            {
                ("Total Vendido", TotalVendidoTexto),
                ("Unidades Vendidas", TotalUnidadesTexto),
                ("SKUs con Movimiento", TotalProductosTexto),
                ("Categorías", TotalCategoriasTexto)
            },
            Secciones = { secProd, secCat }
        };

        if (pdf)
            await ExportadorReportes.ExportarPdfAsync(doc);
        else
            await ExportadorReportes.ExportarCsvAsync(doc);
    }

    public async Task CargarAsync()
    {
        if (EstaOcupado)
            return;

        EstaOcupado = true;
        MensajeError = "";

        try
        {
            DateTime? desde = null;
            DateTime? hasta = null;

            if (UsarFechas)
            {
                if (FechaDesde.Date > FechaHasta.Date)
                    throw new InvalidOperationException(
                        "La fecha inicial no puede ser posterior a la final.");

                desde = FechaDesde.Date;
                hasta = FechaHasta.Date;
            }

            var productos =
                await _servicio.ObtenerProductosAsync(
                    desde, hasta);

            var categorias =
                await _servicio.ObtenerCategoriasAsync(
                    desde, hasta);

            _productosOriginales.Clear();
            _productosOriginales.AddRange(productos);

            _categoriasOriginales.Clear();
            _categoriasOriginales.AddRange(categorias);

            string categoriaAnterior =
                CategoriaSeleccionada;

            OpcionesCategoria.Clear();
            OpcionesCategoria.Add("Todas");

            foreach (var categoria in categorias)
            {
                OpcionesCategoria.Add(categoria.Categoria);
            }

            CategoriaSeleccionada =
                OpcionesCategoria.Contains(categoriaAnterior)
                    ? categoriaAnterior
                    : "Todas";

            AplicarFiltros();
        }
        catch (Exception ex)
        {
            MensajeError =
                "Error al generar el reporte: " + ex.Message;
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private void AplicarFiltros()
    {
        string texto = (Busqueda ?? "").Trim();

        var filtrados = _productosOriginales.Where(p =>
            (CategoriaSeleccionada == "Todas"
                || p.Categoria == CategoriaSeleccionada)
            &&
            (string.IsNullOrWhiteSpace(texto)
                || p.NombreProducto.Contains(
                    texto,
                    StringComparison.OrdinalIgnoreCase)
                || p.Sku.Contains(
                    texto,
                    StringComparison.OrdinalIgnoreCase)
                || p.Categoria.Contains(
                    texto,
                    StringComparison.OrdinalIgnoreCase))
        ).ToList();

        Productos.Clear();

        foreach (var producto in filtrados)
            Productos.Add(producto);

        var categoriasFiltradas = filtrados
            .GroupBy(p => p.Categoria)
            .Select(g => new ReporteCategoriaVenta
            {
                Categoria = g.Key,
                UnidadesVendidas =
                    g.Sum(p => p.UnidadesVendidas),
                ProductosDistintos = g.Count(),
                TotalVendido =
                    g.Sum(p => p.TotalVendido)
            })
            .OrderByDescending(c => c.TotalVendido)
            .ToList();

        Categorias.Clear();

        foreach (var categoria in categoriasFiltradas)
            Categorias.Add(categoria);

        OnPropertyChanged(nameof(TotalVendidoTexto));
        OnPropertyChanged(nameof(TotalUnidadesTexto));
        OnPropertyChanged(nameof(TotalProductosTexto));
        OnPropertyChanged(nameof(TotalCategoriasTexto));
        OnPropertyChanged(nameof(CantidadResultadosTexto));
        OnPropertyChanged(nameof(HayProductos));
    }
}
