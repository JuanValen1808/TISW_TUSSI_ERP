
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using MySqlConnector;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Services.Api;
using TISW_TUSSI_ERP.ViewModels.Ventas;

namespace TISW_TUSSI_ERP.Views.Ventas;

public partial class CuentasPorCobrarVentasPage : ContentPage
{
    private readonly CuentasPorCobrarVentasViewModel _vm;
    private readonly DatabaseService _db;

    private readonly ObservableCollection<NotaInternaItem>
        _notas = new();

    private bool _cargandoNotas;

    private static readonly CultureInfo Cultura =
        CultureInfo.GetCultureInfo("es-CL");

    public CuentasPorCobrarVentasPage(
        CuentasPorCobrarVentasViewModel vm,
        DatabaseService db)
    {
        InitializeComponent();

        _vm = vm;
        _db = db;

        BindingContext = _vm;

        ListaNotas.ItemsSource = _notas;
    }

    // =====================================
    // CARGA INICIAL
    // =====================================

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            await _vm.CargarAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Error",
                "No se pudieron cargar las cuentas: "
                + ex.Message,
                "Aceptar");
        }
    }

    // =====================================
    // PESTAÑA CUENTAS POR COBRAR
    // =====================================

    private void OnCobrosClicked(
        object sender,
        EventArgs e)
    {
        PanelCobros.IsVisible = true;
        PanelNotas.IsVisible = false;

        BotonCobros.BackgroundColor =
            Color.FromArgb("#2563EB");

        BotonCobros.TextColor = Colors.White;

        BotonNotas.BackgroundColor =
            Color.FromArgb("#E2E8F0");

        BotonNotas.TextColor =
            Color.FromArgb("#334155");
    }

    // =====================================
    // PESTAÑA NOTAS DE CRÉDITO
    // =====================================

    private async void OnNotasClicked(
        object sender,
        EventArgs e)
    {
        PanelCobros.IsVisible = false;
        PanelNotas.IsVisible = true;

        BotonNotas.BackgroundColor =
            Color.FromArgb("#2563EB");

        BotonNotas.TextColor = Colors.White;

        BotonCobros.BackgroundColor =
            Color.FromArgb("#E2E8F0");

        BotonCobros.TextColor =
            Color.FromArgb("#334155");

        await CargarNotasAsync();
    }

    private async void OnActualizarNotasClicked(
        object sender,
        EventArgs e)
    {
        await CargarNotasAsync();
    }

    private async void OnExportarNotasPdfClicked(
        object sender,
        EventArgs e)
    {
        await ExportarNotasAsync(true);
    }

    private async void OnExportarNotasCsvClicked(
        object sender,
        EventArgs e)
    {
        await ExportarNotasAsync(false);
    }

    private async Task ExportarNotasAsync(bool pdf)
    {
        if (_notas.Count == 0)
            await CargarNotasAsync();
        if (_notas.Count == 0)
        {
            await DisplayAlertAsync("Exportar", "No hay notas de crédito internas para exportar.", "Aceptar");
            return;
        }

        try
        {
            var sec = new SeccionReporte(
                "Notas de Crédito Internas por Devoluciones de Venta",
                new ColumnaReporte("Folio Nota"),
                new ColumnaReporte("Venta Original"),
                new ColumnaReporte("Fecha Emisión"),
                new ColumnaReporte("Motivo de Devolución"),
                new ColumnaReporte("Estado"),
                new ColumnaReporte("Monto Total", TipoColumna.Moneda));

            foreach (var n in _notas)
                sec.Fila(n.Folio, n.FolioVenta, n.FechaTexto, n.Motivo, n.Estado, n.Total);

            sec.Total("TOTAL NOTAS INTERNAS", "", "", "", $"{_notas.Count} docs.", _notas.Sum(x => x.Total));

            var doc = new DocumentoReporte
            {
                Titulo = "Reporte de Devoluciones y Notas de Crédito Internas",
                Subtitulo = "Trazabilidad de devoluciones de clientes enviadas a cuarentena farmacéutica",
                Horizontal = true,
                Kpis =
                {
                    ("Notas Emitidas", _notas.Count.ToString("N0")),
                    ("Monto Total Devuelto", Dinero(_notas.Sum(x => x.Total)))
                },
                Secciones = { sec }
            };

            if (pdf)
                await ExportadorReportes.ExportarPdfAsync(doc);
            else
                await ExportadorReportes.ExportarCsvAsync(doc);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error al exportar", ex.Message, "Aceptar");
        }
    }

    private async void OnBuscarNotasCompleted(
        object sender,
        EventArgs e)
    {
        await CargarNotasAsync();
    }

    // =====================================
    // CONSULTAR NOTAS EN MYSQL
    // =====================================

    private async Task CargarNotasAsync()
    {
        if (_cargandoNotas)
            return;

        _cargandoNotas = true;

        CargandoNotas.IsVisible = true;
        CargandoNotas.IsRunning = true;

        MensajeNotas.Text = "";

        try
        {
            string filtro =
                (BusquedaNotas.Text ?? "").Trim();

            const string sql = """
                SELECT
                    dv.id_devolucion,
                    dv.folio_nota_credito,
                    ve.folio_documento,
                    dv.fecha_devolucion,
                    dv.motivo,
                    dv.estado,
                    dv.monto_total
                FROM devoluciones_ventas dv
                INNER JOIN ventas_encabezado ve
                    ON ve.id_venta = dv.id_venta_original
                WHERE
                    @filtro = ''
                    OR dv.folio_nota_credito LIKE @patron
                    OR ve.folio_documento LIKE @patron
                    OR dv.motivo LIKE @patron
                ORDER BY
                    dv.fecha_devolucion DESC,
                    dv.id_devolucion DESC
                LIMIT 300;
                """;

            await using var conexion =
                _db.CrearConexion();

            await conexion.OpenAsync();

            await using var cmd =
                new MySqlCommand(sql, conexion);

            cmd.Parameters.AddWithValue(
                "@filtro",
                filtro);

            cmd.Parameters.AddWithValue(
                "@patron",
                $"%{filtro}%");

            await using var reader =
                await cmd.ExecuteReaderAsync();

            var resultados =
                new List<NotaInternaItem>();

            while (await reader.ReadAsync())
            {
                resultados.Add(new NotaInternaItem
                {
                    IdDevolucion = reader.GetInt32(0),

                    Folio = reader.IsDBNull(1)
                        ? ""
                        : reader.GetString(1),

                    FolioVenta = reader.IsDBNull(2)
                        ? ""
                        : reader.GetString(2),

                    Fecha = reader.GetDateTime(3),

                    Motivo = reader.IsDBNull(4)
                        ? ""
                        : reader.GetString(4),

                    Estado = reader.IsDBNull(5)
                        ? ""
                        : reader.GetString(5),

                    Total = reader.GetDecimal(6)
                });
            }

            _notas.Clear();

            foreach (var nota in resultados)
                _notas.Add(nota);

            MensajeNotas.Text =
                $"{_notas.Count} notas internas encontradas.";
        }
        catch (Exception ex)
        {
            MensajeNotas.Text =
                "No se pudieron consultar las notas: "
                + ex.Message;
        }
        finally
        {
            CargandoNotas.IsRunning = false;
            CargandoNotas.IsVisible = false;

            _cargandoNotas = false;
        }
    }

    // =====================================
    // VER DETALLE DE NOTA
    // =====================================

    private async void OnVerNotaClicked(
        object sender,
        EventArgs e)
    {
        if (sender is not Button boton)
            return;

        if (boton.CommandParameter
            is not NotaInternaItem nota)
            return;

        try
        {
            // CORREGIDO:
            // La columna real es dd.subtotal,
            // no dd.subtotal_linea.

            const string sql = """
                SELECT
                    p.nombre_comercial,
                    dd.cantidad_devuelta,
                    dd.precio_unitario,
                    dd.subtotal,
                    dd.condicion_producto
                FROM devoluciones_detalle dd
                INNER JOIN ventas_detalle vd
                    ON vd.id_detalle_venta =
                       dd.id_detalle_venta
                INNER JOIN productos p
                    ON p.id_producto = vd.id_producto
                WHERE dd.id_devolucion = @id
                ORDER BY dd.id_detalle_devolucion;
                """;

            await using var conexion =
                _db.CrearConexion();

            await conexion.OpenAsync();

            await using var cmd =
                new MySqlCommand(sql, conexion);

            cmd.Parameters.AddWithValue(
                "@id",
                nota.IdDevolucion);

            var productos = new List<string>();

            await using (var reader =
                await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    string nombre =
                        reader.GetString(0);

                    int cantidad =
                        reader.GetInt32(1);

                    decimal precio =
                        reader.GetDecimal(2);

                    decimal subtotal =
                        reader.GetDecimal(3);

                    string condicion =
                        reader.GetString(4);

                    productos.Add(
                        $"{nombre}\n" +
                        $"Cantidad: {cantidad}\n" +
                        $"Precio: {Dinero(precio)}\n" +
                        $"Subtotal: {Dinero(subtotal)}\n" +
                        $"Condición: {condicion}");
                }
            }

            string textoProductos =
                productos.Count > 0
                    ? string.Join("\n\n", productos)
                    : "No se encontraron productos asociados.";

            string detalle =
                "FARMSALUD ERP\n" +
                "NOTA DE CRÉDITO INTERNA\n\n" +

                $"Folio: {nota.Folio}\n" +
                $"Venta original: {nota.FolioVenta}\n" +
                $"Fecha: {nota.Fecha:dd/MM/yyyy HH:mm}\n" +
                $"Estado: {nota.Estado}\n" +
                $"Motivo: {nota.Motivo}\n\n" +

                "PRODUCTOS DEVUELTOS\n\n" +

                textoProductos +

                "\n\n--------------------------\n" +

                $"TOTAL: {Dinero(nota.Total)}\n\n" +

                "Documento interno.\n" +
                "No válido como nota de crédito " +
                "tributaria SII.";

            await DisplayAlertAsync(
                "Detalle de nota interna",
                detalle,
                "Cerrar");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Error",
                "No se pudo consultar el detalle: "
                + ex.Message,
                "Aceptar");
        }
    }

    // =====================================
    // FORMATO MONEDA
    // =====================================

    private static string Dinero(
        decimal valor)
    {
        return valor.ToString(
            "C0",
            Cultura);
    }

    // =====================================
    // MODELO PARA LISTADO DE NOTAS
    // =====================================

    public sealed class NotaInternaItem
    {
        public int IdDevolucion { get; set; }

        public string Folio { get; set; } = "";

        public string FolioVenta { get; set; } = "";

        public DateTime Fecha { get; set; }

        public string Motivo { get; set; } = "";

        public string Estado { get; set; } = "";

        public decimal Total { get; set; }

        public string FechaTexto =>
            Fecha.ToString("dd/MM/yyyy HH:mm");

        public string TotalTexto =>
            Total.ToString("C0", Cultura);
    }
}
