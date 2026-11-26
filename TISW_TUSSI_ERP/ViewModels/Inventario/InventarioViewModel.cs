using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Inventario;
using TISW_TUSSI_ERP.Services.Api;
using TISW_TUSSI_ERP.Views.Inventario;

namespace TISW_TUSSI_ERP.ViewModels.Inventario;

public class InventarioViewModel : BaseViewModel
{
    private readonly InventarioService _servicio;
    private readonly ReporteService _reporteService;
    private List<ProductoModel> _todosProductos = new();

    // Colecciones observables para la UI
    public ObservableCollection<ProductoModel> Productos { get; } = new();
    public ObservableCollection<LoteModel> LotesProducto { get; } = new();

    // Resumen y KPIs del Inventario
    private ResumenInventario _resumen = new();
    public ResumenInventario Resumen
    {
        get => _resumen;
        private set => SetProperty(ref _resumen, value);
    }

    // Filtros: TODOS | NORMAL | BAJO | AGOTADO
    private string _filtroActual = "TODOS";
    public string FiltroActual
    {
        get => _filtroActual;
        private set => SetProperty(ref _filtroActual, value);
    }

    private string _busquedaTexto = string.Empty;
    public string BusquedaTexto
    {
        get => _busquedaTexto;
        set
        {
            if (SetProperty(ref _busquedaTexto, value))
                AplicarFiltros();
        }
    }

    // Alias para compatibilidad con el XAML (TextoBusqueda / BusquedaTexto)
    public string TextoBusqueda
    {
        get => BusquedaTexto;
        set => BusquedaTexto = value;
    }

    private string _cantidadTexto = "Productos (0)";
    public string CantidadTexto
    {
        get => _cantidadTexto;
        private set => SetProperty(ref _cantidadTexto, value);
    }

    // Selección de producto para ver sus lotes / detalles
    private ProductoModel? _productoSeleccionado;
    public ProductoModel? ProductoSeleccionado
    {
        get => _productoSeleccionado;
        set
        {
            if (!SetProperty(ref _productoSeleccionado, value)) return;
            OnPropertyChanged(nameof(HayProducto));
            OnPropertyChanged(nameof(NoHayProducto));
            _ = CargarLotesAsync(value);
        }
    }

    public bool HayProducto => ProductoSeleccionado is not null;
    public bool NoHayProducto => ProductoSeleccionado is null;

    // Propiedad calculada para el subtítulo principal
    public string ValorizacionTotalTexto => Resumen?.ValorizacionTotalTexto ?? "$0";

    // Manejo de errores
    private string _mensajeError = string.Empty;
    public string MensajeError
    {
        get => _mensajeError;
        private set { if (SetProperty(ref _mensajeError, value)) OnPropertyChanged(nameof(HayError)); }
    }
    public bool HayError => !string.IsNullOrEmpty(MensajeError);

    // Comandos de la vista
    public ICommand ComandoFiltrar { get; }
    public ICommand ComandoNuevoProducto { get; }
    public ICommand ComandoRegistrarMerma { get; }
    public ICommand ComandoRegistrarAjuste { get; }
    public ICommand ComandoEditarProducto { get; }
    public ICommand ComandoVerKardex { get; }
    public ICommand ComandoExportarDocumento { get; }

    public InventarioViewModel(InventarioService servicio, ReporteService reporteService)
    {
        _servicio = servicio;
        _reporteService = reporteService;
        Titulo = "Gestión de Inventario";

        ComandoFiltrar = new Command<string>(f =>
        {
            FiltroActual = f ?? "TODOS";
            AplicarFiltros();
        });

        ComandoNuevoProducto = new Command(async () =>
        {
            var modal = new NuevoProductoPage(async productoNuevo =>
            {
                if (productoNuevo != null)
                {
                    var (exito, mensaje) = await _servicio.GuardarProductoAsync(productoNuevo);
                    await Aviso(exito ? "Éxito" : "Atención", mensaje);
                    if (exito)
                    {
                        await CargarAsync(); // Recargar lista y KPIs
                    }
                }
            });

            await Shell.Current.Navigation.PushModalAsync(modal);
        });

        ComandoEditarProducto = new Command(async () =>
        {
            if (ProductoSeleccionado is null)
            {
                await Aviso("Editar Ficha", "Seleccione un producto de la lista para editar.");
                return;
            }

            // Abrir ventana emergente modal con el diseño de la plataforma
            var modal = new EditarProductoPage(ProductoSeleccionado, async productoModificado =>
            {
                if (productoModificado != null)
                {
                    // Guardar cambios a través del servicio
                    var (exito, mensaje) = await _servicio.GuardarProductoAsync(productoModificado);
                    await Aviso(exito ? "Éxito" : "Atención", mensaje);
                    if (exito)
                    {
                        await CargarAsync(); // Recargar la lista y KPIs
                    }
                }
            });

            await Shell.Current.Navigation.PushModalAsync(modal);
        });

        ComandoRegistrarMerma = new Command(async () =>
            await Shell.Current.GoToAsync("//MermasPage"));

        ComandoRegistrarAjuste = new Command(async () =>
            await Shell.Current.GoToAsync("//MermasPage"));

        ComandoVerKardex = new Command(async () =>
            await Shell.Current.GoToAsync("//KardexPage"));

        ComandoExportarDocumento = new Command(async () => await ExportarDocumentoPdfAsync());
    }

    public async Task CargarAsync()
    {
        if (EstaOcupado) return;
        EstaOcupado = true;
        MensajeError = string.Empty;

        try
        {
            Resumen = await _servicio.ObtenerResumenAsync();
            OnPropertyChanged(nameof(ValorizacionTotalTexto));

            _todosProductos = await _servicio.ObtenerProductosAsync();
            AplicarFiltros();
        }
        catch (Exception ex)
        {
            MensajeError = "No se pudieron cargar los productos del inventario: " + ex.Message;
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private async Task ExportarDocumentoPdfAsync()
    {
        if (EstaOcupado) return;

        if (!Productos.Any())
        {
            await Aviso("Exportar Reporte", "No hay productos disponibles en el catálogo para exportar.");
            return;
        }

        try
        {
            EstaOcupado = true;

            // 1. Generar los bytes del PDF mediante QuestPDF
            byte[] pdfBytes = _reporteService.GenerarReporteInventarioPdf(Resumen, Productos.ToList());
            await TISW_TUSSI_ERP.Helpers.ExportadorReportes.GuardarYAbrirPdfBytesAsync("Informe_Valorizacion_Inventario", pdfBytes);
        }
        catch (Exception ex)
        {
            MensajeError = "Error al exportar y guardar el reporte PDF: " + ex.Message;
            await Shell.Current.DisplayAlert("Error", MensajeError, "OK");
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private void AplicarFiltros()
    {
        var idPrevio = ProductoSeleccionado?.IdProducto;

        var filtrados = _todosProductos.Where(p =>
        {
            bool cumpleFiltro = FiltroActual switch
            {
                "BAJO" => p.EstadoStock == "Reorden" || p.EstadoStock == "Crítico",
                "AGOTADO" => p.StockActual <= 0,
                "NORMAL" => p.EstadoStock == "Normal",
                _ => true
            };

            if (cumpleFiltro && !string.IsNullOrWhiteSpace(BusquedaTexto))
            {
                string term = BusquedaTexto.ToLowerInvariant();
                cumpleFiltro = (p.NombreComercial?.ToLowerInvariant().Contains(term) ?? false) ||
                               (p.Sku?.ToLowerInvariant().Contains(term) ?? false) ||
                               (p.PrincipioActivo?.ToLowerInvariant().Contains(term) ?? false);
            }

            return cumpleFiltro;
        }).ToList();

        Productos.Clear();
        foreach (var p in filtrados) Productos.Add(p);

        CantidadTexto = $"Productos ({Productos.Count})";

        ProductoSeleccionado = Productos.FirstOrDefault(p => p.IdProducto == idPrevio) ?? Productos.FirstOrDefault();
    }

    private async Task CargarLotesAsync(ProductoModel? producto)
    {
        if (producto is null)
        {
            LotesProducto.Clear();
            return;
        }

        try
        {
            var lotes = await _servicio.ObtenerLotesPorProductoAsync(producto.IdProducto);

            if (ProductoSeleccionado?.IdProducto != producto.IdProducto) return;

            LotesProducto.Clear();
            foreach (var lote in lotes) LotesProducto.Add(lote);
        }
        catch (Exception ex)
        {
            MensajeError = "No se pudieron cargar los lotes del producto: " + ex.Message;
        }
    }

    private static Task Aviso(string titulo, string mensaje)
        => Shell.Current.DisplayAlert(titulo, mensaje, "OK");
}