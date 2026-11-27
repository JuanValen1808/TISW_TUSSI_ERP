using System.Globalization;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Auth;
using TISW_TUSSI_ERP.Models.Inventario;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.Views.Inventario;

public partial class RegistrarMermaModalPage : ContentPage
{
    private readonly InventarioService _servicio;
    private readonly Func<Task> _alGuardarCallback;

    private List<LoteBodegaItemModel> _lotesDisponibles = new();
    private LoteBodegaItemModel? _loteActual;

    // Motivos base alineados con ENUM('VENCIMIENTO','DANO','PERDIDA','DEVOLUCION','OTRO')
    private static readonly List<string> MotivosConocidos = new()
    {
        "VENCIMIENTO",
        "DANO",
        "PERDIDA",
        "DEVOLUCION",
        "OTRO"
    };

    public RegistrarMermaModalPage(InventarioService servicio, Func<Task> alGuardarCallback)
    {
        InitializeComponent();
        _servicio = servicio;
        _alGuardarCallback = alGuardarCallback;
        PkrMotivoExistente.ItemsSource = MotivosConocidos;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarLotesAsync();
    }

    private async Task CargarLotesAsync()
    {
        var lotes = await _servicio.ObtenerLotesParaUbicacionAsync();
        _lotesDisponibles = lotes
            .Where(l => l.StockTotalLote > 0)
            .OrderBy(l => l.NombreProducto)
            .ThenBy(l => l.FechaVencimiento)
            .ToList();

        PkrLote.ItemsSource = _lotesDisponibles
            .Select(l => $"{l.Sku} · {l.NombreProducto} | Lote: {l.NumeroLote} (Stock: {l.StockTotalLote} un., Vence: {l.FechaVencimiento:dd/MM/yyyy})")
            .ToList();
    }

    private void OnLoteChanged(object sender, EventArgs e)
    {
        if (PkrLote.SelectedIndex < 0 || PkrLote.SelectedIndex >= _lotesDisponibles.Count)
        {
            _loteActual = null;
            LblInfoLote.Text = "Seleccione un lote para ver stock disponible y vencimiento.";
            return;
        }

        _loteActual = _lotesDisponibles[PkrLote.SelectedIndex];
        bool vencido = _loteActual.FechaVencimiento.Date < DateTime.Today;
        LblInfoLote.Text = $"Stock disponible: {_loteActual.StockTotalLote} un. · Vence: {_loteActual.FechaVencimiento:dd/MM/yyyy}" +
                          (vencido ? " (LOTE VENCIDO)" : "");

        if (vencido && PkrMotivoExistente.SelectedItem == null)
        {
            PkrMotivoExistente.SelectedItem = "VENCIMIENTO";
        }

        RecalcularCostoPerdida();
    }

    private void OnCantidadChanged(object sender, TextChangedEventArgs e) => RecalcularCostoPerdida();

    private void RecalcularCostoPerdida()
    {
        if (_loteActual == null || !int.TryParse(TxtCantidad.Text, out int cant) || cant <= 0)
        {
            TxtCostoPerdida.Text = "0.00";
            return;
        }

        // Si el modelo trae costo unitario o promedio, se multiplica; si no, queda editable por el usuario
        decimal costoUnitario = 0m;
        var propCosto = _loteActual.GetType().GetProperty("CostoUnitario")
                      ?? _loteActual.GetType().GetProperty("CostoPromedioPonderado");
        if (propCosto?.GetValue(_loteActual) is decimal c)
            costoUnitario = c;

        decimal total = cant * costoUnitario;
        TxtCostoPerdida.Text = total.ToString("0.00", CultureInfo.InvariantCulture);
    }

    private void OnMotivoPickerChanged(object sender, EventArgs e)
    {
        if (PkrMotivoExistente.SelectedItem is string seleccionado)
        {
            TxtMotivoPersonalizado.Text = seleccionado;
        }
    }

    private async void OnGuardarMermaClicked(object sender, EventArgs e)
    {
        if (_loteActual == null)
        {
            await DisplayAlert("Validación", "Debe seleccionar un lote con stock disponible.", "OK");
            return;
        }

        if (!int.TryParse(TxtCantidad.Text, out int cantidad) || cantidad <= 0)
        {
            await DisplayAlert("Validación", "Ingrese una cantidad válida mayor a cero.", "OK");
            return;
        }

        if (cantidad > _loteActual.StockTotalLote)
        {
            await DisplayAlert("Stock Insuficiente", $"No puede dar de baja {cantidad} un. El lote solo tiene {_loteActual.StockTotalLote} un. disponibles.", "OK");
            return;
        }

        string motivoIngresado = (TxtMotivoPersonalizado.Text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(motivoIngresado) && PkrMotivoExistente.SelectedItem is string pkrMot)
            motivoIngresado = pkrMot;

        if (string.IsNullOrWhiteSpace(motivoIngresado))
        {
            await DisplayAlert("Validación", "Debe seleccionar o ingresar un motivo de merma.", "OK");
            return;
        }

        // Normalizar sin tildes para comparar con los motivos conocidos
        string normalizado = motivoIngresado.ToUpperInvariant()
            .Replace("Á", "A").Replace("É", "E").Replace("Í", "I").Replace("Ó", "O").Replace("Ú", "U")
            .Replace("Ñ", "N");

        bool esConocido = MotivosConocidos.Contains(normalizado);
        string motivoEnumParaBd = normalizado;

        if (!esConocido)
        {
            bool confirmarNuevo = await DisplayAlert(
                "Motivo Nuevo Detectado",
                $"El motivo '{motivoIngresado}' no existe en la lista actual de motivos.\n\n¿Desea agregarlo y registrar esta merma?",
                "Sí, agregar y registrar",
                "Cancelar");

            if (!confirmarNuevo)
                return;

            // La columna motivo en registro_mermas es ENUM('VENCIMIENTO','DANO','PERDIDA','DEVOLUCION','OTRO')
            motivoEnumParaBd = "OTRO";
        }

        decimal.TryParse(
            (TxtCostoPerdida.Text ?? "0").Replace(",", "."),
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out decimal costoTotal);

        int idUsuario = Sesion.UsuarioActual?.IdUsuario ?? 1;

        var (exito, mensaje) = await _servicio.RegistrarMermaAsync(
            idProducto: _loteActual.IdProducto,
            idLote: _loteActual.IdLote,
            cantidad: cantidad,
            motivoEnum: motivoEnumParaBd,
            costoTotalPerdida: costoTotal,
            idUsuario: idUsuario,
            motivoTextoOriginal: motivoIngresado);

        await DisplayAlert(exito ? "Merma Registrada" : "Error", mensaje, "OK");

        if (exito)
        {
            await Navigation.PopModalAsync();
            await _alGuardarCallback();
        }
    }

    private async void OnCerrarClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}