using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using TISW_TUSSI_ERP.Models.Inventario;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Inventario
{
    public class KardexViewModel : BaseViewModel
    {
        private readonly InventarioService _servicio;
        private readonly ReporteService _reporteService;

        public ObservableCollection<KardexMovimientoModel> Movimientos { get; } = new();
        public ObservableCollection<ProductoModel> ProductosDisponibles { get; } = new();

        private ProductoModel? _productoSeleccionado;
        public ProductoModel? ProductoSeleccionado
        {
            get => _productoSeleccionado;
            set
            {
                if (SetProperty(ref _productoSeleccionado, value))
                {
                    _ = CargarMovimientosAsync();
                }
            }
        }

        private string _textoFiltro = string.Empty;
        public string TextoFiltro
        {
            get => _textoFiltro;
            set => SetProperty(ref _textoFiltro, value);
        }

        public ICommand ComandoFiltrar { get; }
        public ICommand ComandoLimpiarFiltro { get; }
        public ICommand ComandoExportarKardex { get; }
        public ICommand ComandoRecargar { get; }

        public KardexViewModel(InventarioService servicio, ReporteService reporteService)
        {
            _servicio = servicio;
            _reporteService = reporteService;
            Titulo = "Kardex y Trazabilidad";

            ComandoRecargar = new Command(async () => await CargarDatosInicialesAsync());
            ComandoFiltrar = new Command(async () => await CargarMovimientosAsync());
            ComandoLimpiarFiltro = new Command(async () =>
            {
                ProductoSeleccionado = null;
                await CargarMovimientosAsync();
            });
            ComandoExportarKardex = new Command(async () => await ExportarKardexPdfAsync());
        }

        public async Task CargarDatosInicialesAsync()
        {
            if (EstaOcupado) return;
            EstaOcupado = true;

            try
            {
                var prods = await _servicio.ObtenerProductosAsync();
                ProductosDisponibles.Clear();
                foreach (var p in prods) ProductosDisponibles.Add(p);

                await CargarMovimientosAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", "No se pudo inicializar el Kardex: " + ex.Message, "OK");
            }
            finally
            {
                EstaOcupado = false;
            }
        }

        private async Task CargarMovimientosAsync()
        {
            try
            {
                int? idProd = ProductoSeleccionado?.IdProducto;
                var lista = await _servicio.ObtenerKardexAsync(idProd);

                Movimientos.Clear();
                foreach (var m in lista)
                {
                    if (string.IsNullOrWhiteSpace(TextoFiltro) ||
                        m.NombreProducto.Contains(TextoFiltro, StringComparison.OrdinalIgnoreCase) ||
                        m.Sku.Contains(TextoFiltro, StringComparison.OrdinalIgnoreCase) ||
                        m.NumeroLote.Contains(TextoFiltro, StringComparison.OrdinalIgnoreCase))
                    {
                        Movimientos.Add(m);
                    }
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", "Error al consultar los movimientos: " + ex.Message, "OK");
            }
        }

        private async Task ExportarKardexPdfAsync()
        {
            if (!Movimientos.Any())
            {
                await Shell.Current.DisplayAlert("Exportar", "No hay movimientos en el Kardex para exportar.", "OK");
                return;
            }

            try
            {
                string nombreFiltro = ProductoSeleccionado?.NombreComercial ?? "Todos los productos";
                byte[] pdfBytes = _reporteService.GenerarReporteKardexPdf(Movimientos.ToList(), nombreFiltro);

                string nombreArchivo = $"Kardex_Trazabilidad_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
                string rutaArchivo = Path.Combine(FileSystem.CacheDirectory, nombreArchivo);

                await File.WriteAllBytesAsync(rutaArchivo, pdfBytes);

                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Informe de Kardex y Trazabilidad (PDF)",
                    File = new ShareFile(rutaArchivo)
                });
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", "No se pudo generar el PDF del Kardex: " + ex.Message, "OK");
            }
        }
    }
}