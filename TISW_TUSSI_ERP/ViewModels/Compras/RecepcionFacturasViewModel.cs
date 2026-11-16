using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Auth;
using TISW_TUSSI_ERP.Models.Compras;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Compras;

// =========================================================================
// MÓDULO INTEGRAL DE RECEPCIÓN, CALIDAD, DEVOLUCIONES Y EQUIVALENCIAS
// RF-COM-03: Recepción y Liquidación de Facturas
// RF-COM-07: Gestión de Devoluciones y Notas de Crédito al Proveedor
// RF-COM-09: Trazabilidad y Estado de Cuarentena (Aprobación QF)
// RF-COM-11: Gestión de Equivalencias de Presentación y Conversión Automática
// =========================================================================
public class RecepcionFacturasViewModel : BaseViewModel
{
    private readonly ComprasService _servicio;
    private int _lineasVersion;

    // ── Control de Pestañas Principales ──────────────────────────────────
    // PESTAÑAS: RECEPCION | CUARENTENA | DEVOLUCIONES | EQUIVALENCIAS
    private string _pestanaActual = "RECEPCION";
    public string PestanaActual
    {
        get => _pestanaActual;
        set
        {
            if (SetProperty(ref _pestanaActual, value))
            {
                OnPropertyChanged(nameof(EsPestanaRecepcion));
                OnPropertyChanged(nameof(EsPestanaCuarentena));
                OnPropertyChanged(nameof(EsPestanaDevoluciones));
                OnPropertyChanged(nameof(EsPestanaEquivalencias));
                NotificarEstilosPestanas();
            }
        }
    }

    public bool EsPestanaRecepcion => PestanaActual == "RECEPCION";
    public bool EsPestanaCuarentena => PestanaActual == "CUARENTENA";
    public bool EsPestanaDevoluciones => PestanaActual == "DEVOLUCIONES";
    public bool EsPestanaEquivalencias => PestanaActual == "EQUIVALENCIAS";

    private void NotificarEstilosPestanas()
    {
        OnPropertyChanged(nameof(ColorFondoPestanaRecepcion));
        OnPropertyChanged(nameof(ColorTextoPestanaRecepcion));
        OnPropertyChanged(nameof(ColorBordePestanaRecepcion));
        OnPropertyChanged(nameof(ColorFondoPestanaCuarentena));
        OnPropertyChanged(nameof(ColorTextoPestanaCuarentena));
        OnPropertyChanged(nameof(ColorBordePestanaCuarentena));
        OnPropertyChanged(nameof(ColorFondoPestanaDevoluciones));
        OnPropertyChanged(nameof(ColorTextoPestanaDevoluciones));
        OnPropertyChanged(nameof(ColorBordePestanaDevoluciones));
        OnPropertyChanged(nameof(ColorFondoPestanaEquivalencias));
        OnPropertyChanged(nameof(ColorTextoPestanaEquivalencias));
        OnPropertyChanged(nameof(ColorBordePestanaEquivalencias));
    }

    public Color ColorFondoPestanaRecepcion => EsPestanaRecepcion ? Color.FromArgb("#2563EB") : Color.FromArgb("#F3F4F6");
    public Color ColorTextoPestanaRecepcion => EsPestanaRecepcion ? Colors.White : Color.FromArgb("#4B5563");
    public Color ColorBordePestanaRecepcion => EsPestanaRecepcion ? Color.FromArgb("#2563EB") : Color.FromArgb("#E5E7EB");

    public Color ColorFondoPestanaCuarentena => EsPestanaCuarentena ? Color.FromArgb("#2563EB") : Color.FromArgb("#F3F4F6");
    public Color ColorTextoPestanaCuarentena => EsPestanaCuarentena ? Colors.White : Color.FromArgb("#4B5563");
    public Color ColorBordePestanaCuarentena => EsPestanaCuarentena ? Color.FromArgb("#2563EB") : Color.FromArgb("#E5E7EB");

    public Color ColorFondoPestanaDevoluciones => EsPestanaDevoluciones ? Color.FromArgb("#2563EB") : Color.FromArgb("#F3F4F6");
    public Color ColorTextoPestanaDevoluciones => EsPestanaDevoluciones ? Colors.White : Color.FromArgb("#4B5563");
    public Color ColorBordePestanaDevoluciones => EsPestanaDevoluciones ? Color.FromArgb("#2563EB") : Color.FromArgb("#E5E7EB");

    public Color ColorFondoPestanaEquivalencias => EsPestanaEquivalencias ? Color.FromArgb("#2563EB") : Color.FromArgb("#F3F4F6");
    public Color ColorTextoPestanaEquivalencias => EsPestanaEquivalencias ? Colors.White : Color.FromArgb("#4B5563");
    public Color ColorBordePestanaEquivalencias => EsPestanaEquivalencias ? Color.FromArgb("#2563EB") : Color.FromArgb("#E5E7EB");

    // ── Mensajes y Estados Generales ─────────────────────────────────────
    private string _mensajeError = string.Empty;
    public string MensajeError
    {
        get => _mensajeError;
        private set { if (SetProperty(ref _mensajeError, value)) OnPropertyChanged(nameof(HayError)); }
    }
    public bool HayError => !string.IsNullOrEmpty(MensajeError);
    public bool NoEstaOcupado => !EstaOcupado;

    // Permisos Químico Farmacéutico / Administrador (RF-COM-09)
    public bool EsQF => Sesion.UsuarioActual?.IdRol == 3 || Sesion.UsuarioActual?.IdRol == 1 || Sesion.UsuarioActual?.IdRol == 10;
    public bool NoEsQF => !EsQF;

    // =====================================================================
    // 1. PESTAÑA RECEPCIÓN Y LIQUIDACIÓN (RF-COM-03 & RF-COM-11)
    // =====================================================================
    public ObservableCollection<OrdenCompra> OrdenesDisponibles { get; } = new();
    public ObservableCollection<LineaRecepcionFacturaItem> Lineas { get; } = new();

    private OrdenCompra? _ordenSeleccionada;
    public OrdenCompra? OrdenSeleccionada
    {
        get => _ordenSeleccionada;
        set
        {
            if (SetProperty(ref _ordenSeleccionada, value))
            {
                OnPropertyChanged(nameof(HayOrdenSeleccionada));
                OnPropertyChanged(nameof(NoHayOrdenSeleccionada));
                _ = CargarLineasAsync(value);
            }
        }
    }
    public bool HayOrdenSeleccionada => OrdenSeleccionada is not null;
    public bool NoHayOrdenSeleccionada => OrdenSeleccionada is null;

    private string _numeroFactura = $"FAC-{DateTime.Now.Year}-{new Random().Next(1000, 9999)}";
    public string NumeroFactura { get => _numeroFactura; set => SetProperty(ref _numeroFactura, value); }

    private DateTime _fechaFactura = DateTime.Today;
    public DateTime FechaFactura { get => _fechaFactura; set => SetProperty(ref _fechaFactura, value); }

    private decimal _montoNeto;
    public decimal MontoNeto
    {
        get => _montoNeto;
        private set
        {
            if (SetProperty(ref _montoNeto, value))
            {
                OnPropertyChanged(nameof(MontoNetoTexto));
                OnPropertyChanged(nameof(MontoIva));
                OnPropertyChanged(nameof(MontoIvaTexto));
                OnPropertyChanged(nameof(MontoTotalFacturado));
                OnPropertyChanged(nameof(MontoTotalFacturadoTexto));
            }
        }
    }

    public decimal MontoIva => Math.Round(MontoNeto * 0.19m, 0);
    public decimal MontoTotalFacturado => MontoNeto + MontoIva;

    public string MontoNetoTexto => Formato.Moneda(MontoNeto);
    public string MontoIvaTexto => Formato.Moneda(MontoIva);
    public string MontoTotalFacturadoTexto => Formato.Moneda(MontoTotalFacturado);

    // =====================================================================
    // 2. PESTAÑA CUARENTENA DE LOTES (RF-COM-09)
    // =====================================================================
    public ObservableCollection<LoteCuarentena> LotesCuarentena { get; } = new();
    private string _filtroCuarentena = "TODOS";
    public string FiltroCuarentena
    {
        get => _filtroCuarentena;
        set
        {
            if (SetProperty(ref _filtroCuarentena, value))
            {
                OnPropertyChanged(nameof(ColorFondoFiltroTodos));
                OnPropertyChanged(nameof(ColorTextoFiltroTodos));
                OnPropertyChanged(nameof(ColorFondoFiltroCuarentena));
                OnPropertyChanged(nameof(ColorTextoFiltroCuarentena));
                OnPropertyChanged(nameof(ColorFondoFiltroLiberado));
                OnPropertyChanged(nameof(ColorTextoFiltroLiberado));
            }
        }
    }

    public Color ColorFondoFiltroTodos => FiltroCuarentena == "TODOS" ? Color.FromArgb("#2563EB") : Color.FromArgb("#F3F4F6");
    public Color ColorTextoFiltroTodos => FiltroCuarentena == "TODOS" ? Colors.White : Color.FromArgb("#4B5563");

    public Color ColorFondoFiltroCuarentena => FiltroCuarentena == "CUARENTENA" ? Color.FromArgb("#DC2626") : Color.FromArgb("#F3F4F6");
    public Color ColorTextoFiltroCuarentena => FiltroCuarentena == "CUARENTENA" ? Colors.White : Color.FromArgb("#4B5563");

    public Color ColorFondoFiltroLiberado => FiltroCuarentena == "LIBERADO" ? Color.FromArgb("#16A34A") : Color.FromArgb("#F3F4F6");
    public Color ColorTextoFiltroLiberado => FiltroCuarentena == "LIBERADO" ? Colors.White : Color.FromArgb("#4B5563");

    private int _totalEnCuarentena;
    public int TotalEnCuarentena { get => _totalEnCuarentena; set => SetProperty(ref _totalEnCuarentena, value); }

    private int _totalLiberados;
    public int TotalLiberados { get => _totalLiberados; set => SetProperty(ref _totalLiberados, value); }

    // =====================================================================
    // 3. PESTAÑA DEVOLUCIONES A PROVEEDOR (RF-COM-07)
    // =====================================================================
    public ObservableCollection<Proveedor> ProveedoresDevolucion { get; } = new();
    public ObservableCollection<LoteDevolucionItem> LotesDevolucion { get; } = new();
    public ObservableCollection<HistorialDevolucionProveedor> HistorialDevoluciones { get; } = new();

    public List<string> MotivosDevolucion { get; } = new()
    {
        "MERMA",
        "VENCIMIENTO_PROXIMO",
        "DAÑO",
        "OTRO"
    };

    private Proveedor? _proveedorDevolucionSeleccionado;
    public Proveedor? ProveedorDevolucionSeleccionado
    {
        get => _proveedorDevolucionSeleccionado;
        set
        {
            if (SetProperty(ref _proveedorDevolucionSeleccionado, value))
            {
                OnPropertyChanged(nameof(HayProveedorDevSeleccionado));
                OnPropertyChanged(nameof(NoHayProveedorDevSeleccionado));
                _ = CargarLotesDevolucionAsync(value);
            }
        }
    }
    public bool HayProveedorDevSeleccionado => ProveedorDevolucionSeleccionado != null;
    public bool NoHayProveedorDevSeleccionado => ProveedorDevolucionSeleccionado == null;

    private string _numeroNotaCreditoDev = $"NC-{DateTime.Now.Year}-{new Random().Next(100, 999)}";
    public string NumeroNotaCreditoDev { get => _numeroNotaCreditoDev; set => SetProperty(ref _numeroNotaCreditoDev, value); }

    private DateTime _fechaDevolucion = DateTime.Today;
    public DateTime FechaDevolucion { get => _fechaDevolucion; set => SetProperty(ref _fechaDevolucion, value); }

    private string _motivoDevolucion = "MERMA";
    public string MotivoDevolucion { get => _motivoDevolucion; set => SetProperty(ref _motivoDevolucion, value); }

    private string _observacionesDev = string.Empty;
    public string ObservacionesDev { get => _observacionesDev; set => SetProperty(ref _observacionesDev, value); }

    public decimal MontoTotalDevolucion => LotesDevolucion.Where(l => l.CantidadADevolver > 0).Sum(l => l.Subtotal);
    public string MontoTotalDevolucionTexto => Formato.Moneda(MontoTotalDevolucion);

    // =====================================================================
    // 4. PESTAÑA EQUIVALENCIAS DE PRESENTACIÓN (RF-COM-11)
    // =====================================================================
    public ObservableCollection<EquivalenciaProducto> Equivalencias { get; } = new();
    public ObservableCollection<ProductoSelector> ProductosParaEquivalencia { get; } = new();

    private ProductoSelector? _productoEqSeleccionado;
    public ProductoSelector? ProductoEqSeleccionado { get => _productoEqSeleccionado; set => SetProperty(ref _productoEqSeleccionado, value); }

    private string _unidadCompraEq = "Caja x 1000 comp";
    public string UnidadCompraEq { get => _unidadCompraEq; set => SetProperty(ref _unidadCompraEq, value); }

    private string _unidadVentaEq = "Blíster x 10 comp";
    public string UnidadVentaEq { get => _unidadVentaEq; set => SetProperty(ref _unidadVentaEq, value); }

    private string _factorConversionEqTexto = "100";
    public string FactorConversionEqTexto { get => _factorConversionEqTexto; set => SetProperty(ref _factorConversionEqTexto, value); }

    private bool _formularioEquivalenciaVisible;
    public bool FormularioEquivalenciaVisible { get => _formularioEquivalenciaVisible; set => SetProperty(ref _formularioEquivalenciaVisible, value); }

    // =====================================================================
    // COMANDOS
    // =====================================================================
    public ICommand ComandoCambiarPestana { get; }
    public ICommand ComandoCargarOrdenes { get; }
    public ICommand ComandoLiquidarRecepcion { get; }
    public ICommand ComandoRecalcularTotal { get; }

    // Cuarentena
    public ICommand ComandoCargarCuarentena { get; }
    public ICommand ComandoFiltrarCuarentena { get; }
    public ICommand ComandoLiberarLote { get; }
    public ICommand ComandoPonerEnCuarentena { get; }

    // Devoluciones
    public ICommand ComandoCargarDevoluciones { get; }
    public ICommand ComandoRegistrarDevolucion { get; }

    // Equivalencias
    public ICommand ComandoCargarEquivalencias { get; }
    public ICommand ComandoNuevaEquivalencia { get; }
    public ICommand ComandoGuardarEquivalencia { get; }
    public ICommand ComandoEliminarEquivalencia { get; }
    public ICommand ComandoCancelarEquivalencia { get; }

    public RecepcionFacturasViewModel(ComprasService servicio)
    {
        _servicio = servicio;
        Titulo = "Recepción, Calidad y Devoluciones";

        ComandoCambiarPestana = new Command<string>(async (p) => await CambiarPestanaAsync(p ?? "RECEPCION"), _ => NoEstaOcupado);
        ComandoCargarOrdenes = new Command(async () => await CargarOrdenesDisponiblesAsync(), () => NoEstaOcupado);
        ComandoLiquidarRecepcion = new Command(async () => await LiquidarRecepcionAsync(), () => NoEstaOcupado);
        ComandoRecalcularTotal = new Command(RecalcularTotal, () => NoEstaOcupado);

        // Cuarentena
        ComandoCargarCuarentena = new Command(async () => await CargarCuarentenaAsync(), () => NoEstaOcupado);
        ComandoFiltrarCuarentena = new Command<string>(async (f) =>
        {
            FiltroCuarentena = f ?? "TODOS";
            await CargarCuarentenaAsync();
        }, _ => NoEstaOcupado);
        ComandoLiberarLote = new Command<LoteCuarentena>(async (lote) => await LiberarLoteAsync(lote), (_) => NoEstaOcupado);
        ComandoPonerEnCuarentena = new Command<LoteCuarentena>(async (lote) => await PonerEnCuarentenaAsync(lote), (_) => NoEstaOcupado);

        // Devoluciones
        ComandoCargarDevoluciones = new Command(async () => await CargarDevolucionesAsync(), () => NoEstaOcupado);
        ComandoRegistrarDevolucion = new Command(async () => await RegistrarDevolucionAsync(), () => NoEstaOcupado);

        // Equivalencias
        ComandoCargarEquivalencias = new Command(async () => await CargarEquivalenciasAsync(), () => NoEstaOcupado);
        ComandoNuevaEquivalencia = new Command(AbrirNuevaEquivalencia, () => NoEstaOcupado);
        ComandoGuardarEquivalencia = new Command(async () => await GuardarEquivalenciaAsync(), () => NoEstaOcupado);
        ComandoEliminarEquivalencia = new Command<EquivalenciaProducto>(async (eq) => await EliminarEquivalenciaAsync(eq), (_) => NoEstaOcupado);
        ComandoCancelarEquivalencia = new Command(() => FormularioEquivalenciaVisible = false, () => NoEstaOcupado);

        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(EstaOcupado))
            {
                OnPropertyChanged(nameof(NoEstaOcupado));
                (ComandoCambiarPestana as Command)?.ChangeCanExecute();
                (ComandoCargarOrdenes as Command)?.ChangeCanExecute();
                (ComandoLiquidarRecepcion as Command)?.ChangeCanExecute();
                (ComandoRecalcularTotal as Command)?.ChangeCanExecute();
                (ComandoCargarCuarentena as Command)?.ChangeCanExecute();
                (ComandoLiberarLote as Command<LoteCuarentena>)?.ChangeCanExecute();
                (ComandoPonerEnCuarentena as Command<LoteCuarentena>)?.ChangeCanExecute();
                (ComandoRegistrarDevolucion as Command)?.ChangeCanExecute();
                (ComandoGuardarEquivalencia as Command)?.ChangeCanExecute();
            }
        };
    }

    private async Task CambiarPestanaAsync(string pestana)
    {
        PestanaActual = pestana;
        MensajeError = string.Empty;

        switch (pestana)
        {
            case "RECEPCION":
                await CargarOrdenesDisponiblesAsync();
                break;
            case "CUARENTENA":
                await CargarCuarentenaAsync();
                break;
            case "DEVOLUCIONES":
                await CargarDevolucionesAsync();
                break;
            case "EQUIVALENCIAS":
                await CargarEquivalenciasAsync();
                break;
        }
    }

    // =====================================================================
    // MÉTODOS PESTAÑA 1: RECEPCIÓN Y LIQUIDACIÓN (RF-COM-03 & RF-COM-11)
    // =====================================================================

    public async Task CargarOrdenesDisponiblesAsync(int? idPreseleccion = null)
    {
        if (EstaOcupado) return;
        EstaOcupado = true;
        MensajeError = string.Empty;

        try
        {
            await CargarOrdenesInternoAsync(idPreseleccion, limpiarSeleccionSiSinId: false);
        }
        catch (Exception ex)
        {
            MensajeError = "No se pudieron obtener las órdenes de compra: " + ex.Message;
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private async Task CargarOrdenesInternoAsync(int? idPreseleccion, bool limpiarSeleccionSiSinId)
    {
        MensajeError = string.Empty;
        int? idPrevio = idPreseleccion ?? (limpiarSeleccionSiSinId ? null : OrdenSeleccionada?.Id);

        var ordenes = await _servicio.ObtenerOrdenesParaRecepcionAsync();
        OrdenesDisponibles.Clear();
        foreach (var o in ordenes)
            OrdenesDisponibles.Add(o);

        if (limpiarSeleccionSiSinId && !idPreseleccion.HasValue)
        {
            OrdenSeleccionada = null;
            return;
        }

        if (idPrevio.HasValue)
        {
            OrdenSeleccionada = OrdenesDisponibles.FirstOrDefault(o => o.Id == idPrevio.Value)
                                ?? OrdenesDisponibles.FirstOrDefault();
        }
        else if (OrdenesDisponibles.Count > 0)
        {
            OrdenSeleccionada = OrdenesDisponibles.FirstOrDefault();
        }
        else
        {
            OrdenSeleccionada = null;
        }
    }

    private async Task CargarLineasAsync(OrdenCompra? orden)
    {
        int version = ++_lineasVersion;
        Lineas.Clear();
        MontoNeto = 0;
        if (orden is null) return;

        try
        {
            var items = await _servicio.ObtenerLineasParaRecepcionAsync(orden.Id);
            if (version != _lineasVersion) return;

            Lineas.Clear();
            foreach (var item in items)
            {
                item.PropertyChanged += (_, _) => RecalcularTotal();
                Lineas.Add(item);
            }

            RecalcularTotal();
        }
        catch (Exception ex)
        {
            if (version == _lineasVersion)
                MensajeError = "Error al cargar los ítems de la orden: " + ex.Message;
        }
    }

    public void RecalcularTotal()
    {
        MontoNeto = Lineas.Sum(l => l.SubtotalLinea);
    }

    private async Task LiquidarRecepcionAsync()
    {
        if (EstaOcupado) return;
        EstaOcupado = true;

        try
        {
            var ordenActual = OrdenSeleccionada;
            if (ordenActual is null)
            {
                await Shell.Current.DisplayAlert("Validación", "Debe seleccionar una orden de compra pendiente de recepción.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(NumeroFactura))
            {
                await Shell.Current.DisplayAlert("Validación", "El número de factura del proveedor es obligatorio.", "OK");
                return;
            }

            var lineasARecibir = Lineas.Where(l => l.CantidadARecibir > 0).ToList();
            if (lineasARecibir.Count == 0)
            {
                await Shell.Current.DisplayAlert("Validación", "Debe ingresar una cantidad a recepcionar mayor a 0 en al menos un medicamento.", "OK");
                return;
            }

            foreach (var l in lineasARecibir)
            {
                if (string.IsNullOrWhiteSpace(l.NumeroLote))
                {
                    await Shell.Current.DisplayAlert("Validación Sanitaria", $"El producto '{l.NombreProducto}' requiere un número de lote para cumplir con la trazabilidad normativa del ISP.", "OK");
                    return;
                }

                if (l.FechaVencimiento.Date <= DateTime.Today)
                {
                    await Shell.Current.DisplayAlert("Validación Sanitaria", $"La fecha de vencimiento del lote '{l.NumeroLote}' ({l.NombreProducto}) no puede ser anterior o igual a la fecha de hoy.", "OK");
                    return;
                }

                if (l.CantidadARecibir > l.CantidadPendiente)
                {
                    await Shell.Current.DisplayAlert("Validación Operativa", $"La cantidad a ingresar ({l.CantidadARecibir}) para '{l.NombreProducto}' supera la cantidad pendiente pactada ({l.CantidadPendiente}).", "OK");
                    return;
                }

                if (l.CostoFacturado <= 0)
                {
                    await Shell.Current.DisplayAlert("Validación Operativa", $"El costo unitario facturado para '{l.NombreProducto}' debe ser mayor a $0.", "OK");
                    return;
                }
            }

            // Resumen de conversión de equivalencias si aplica
            var textoConversiones = "";
            int conConversion = lineasARecibir.Count(l => l.TieneConversion);
            if (conConversion > 0)
            {
                textoConversiones = $"\n\nConversión automática de equivalencias:\n" +
                    string.Join("\n", lineasARecibir.Where(l => l.TieneConversion)
                        .Select(l => $"• {l.NombreProducto}: {l.CantidadARecibir} {l.UnidadCompra} → {l.UnidadesStockAIngresar} {l.UnidadVenta} a inventario"));
            }

            var confirmar = await Shell.Current.DisplayAlert(
                "Liquidar Factura de Compra",
                $"¿Confirmar la recepción de {lineasARecibir.Count} medicamento(s) por un total de {MontoTotalFacturadoTexto} bajo la Factura {NumeroFactura}?{textoConversiones}\n\nEsta acción actualizará el stock físico, inyectará los movimientos en Kardex y creará la cuenta por pagar en contabilidad.",
                "Confirmar y Liquidar", "Cancelar");

            if (!confirmar) return;

            var input = new RecepcionFacturaInput
            {
                IdOrdenCompra = ordenActual.Id,
                IdProveedor = ordenActual.IdProveedor,
                NumeroFactura = NumeroFactura.Trim(),
                FechaFactura = FechaFactura,
                MontoNeto = MontoNeto,
                MontoIva = MontoIva,
                MontoTotalFacturado = MontoTotalFacturado,
                Lineas = lineasARecibir
            };

            int idUsuario = Sesion.UsuarioActual?.IdUsuario ?? 1;
            var (idRecepcion, numFact) = await _servicio.RegistrarRecepcionFacturaAsync(input, idUsuario);

            _lineasVersion++;
            Lineas.Clear();
            OrdenSeleccionada = null;
            MontoNeto = 0;
            NumeroFactura = $"FAC-{DateTime.Now.Year}-{new Random().Next(1000, 9999)}";
            FechaFactura = DateTime.Today;

            await CargarOrdenesInternoAsync(null, limpiarSeleccionSiSinId: true);

            await Shell.Current.DisplayAlert(
                "Factura Liquidada Exitosamente",
                $"Recepción N° {idRecepcion} procesada correctamente para la Factura {numFact}.\n\n" +
                "• Stock de inventario actualizado en lotes (con conversión automática de equivalencias).\n" +
                "• Entradas registradas cronológicamente en Kardex.\n" +
                "• Estado de Orden de Compra actualizado.\n" +
                "• Cuenta por pagar creada y asiento contable enlazado.",
                "Entendido");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error en la liquidación", ex.Message, "OK");
            await CargarOrdenesInternoAsync(null, limpiarSeleccionSiSinId: false);
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    // =====================================================================
    // MÉTODOS PESTAÑA 2: CUARENTENA DE LOTES Y APROBACIÓN QF (RF-COM-09)
    // =====================================================================

    public async Task CargarCuarentenaAsync()
    {
        if (EstaOcupado) return;
        EstaOcupado = true;
        MensajeError = string.Empty;

        try
        {
            string? filtro = FiltroCuarentena switch
            {
                "CUARENTENA" => "CUARENTENA",
                "LIBERADO" => "LIBERADO",
                _ => null
            };

            var lotes = await _servicio.ObtenerLotesCuarentenaAsync(filtro);
            LotesCuarentena.Clear();
            foreach (var l in lotes)
                LotesCuarentena.Add(l);

            TotalEnCuarentena = LotesCuarentena.Count(l => l.EstaEnCuarentena);
            TotalLiberados = LotesCuarentena.Count(l => !l.EstaEnCuarentena);
        }
        catch (Exception ex)
        {
            MensajeError = "Error al cargar control de cuarentena: " + ex.Message;
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private async Task LiberarLoteAsync(LoteCuarentena? lote)
    {
        if (lote is null || EstaOcupado) return;

        if (!EsQF)
        {
            await Shell.Current.DisplayAlert("Control Normativo ISP", "La liberación técnica y visual de lotes en cuarentena es facultad exclusiva del Químico Farmacéutico (QF) o Administrador General.", "OK");
            return;
        }

        var confirmar = await Shell.Current.DisplayAlert(
            "Aprobación Técnica del Químico Farmacéutico",
            $"¿Aprueba técnica y visualmente el ingreso definitivo para comercialización del lote '{lote.NumeroLote}' ({lote.NombreProducto}) con {lote.StockActualTexto}?\n\nAl aprobar, el lote pasará a estado LIBERADO y quedará disponible para dispensación en POS.",
            "Aprobar y Liberar", "Cancelar");

        if (!confirmar) return;

        EstaOcupado = true;
        try
        {
            await _servicio.LiberarLoteCuarentenaAsync(lote.IdLote);
            await CargarCuarentenaAsync();
            await Shell.Current.DisplayAlert("Lote Liberado para Comercialización", $"El lote '{lote.NumeroLote}' fue aprobado y liberado exitosamente por el Químico Farmacéutico.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private async Task PonerEnCuarentenaAsync(LoteCuarentena? lote)
    {
        if (lote is null || EstaOcupado) return;

        if (!EsQF)
        {
            await Shell.Current.DisplayAlert("Control Normativo ISP", "Solo el Químico Farmacéutico puede modificar el estado de cuarentena de un lote.", "OK");
            return;
        }

        var confirmar = await Shell.Current.DisplayAlert(
            "Retener Lote en Cuarentena",
            $"¿Desea colocar en CUARENTENA el lote '{lote.NumeroLote}' ({lote.NombreProducto}) para inspección técnica?",
            "Sí, Retener", "Cancelar");

        if (!confirmar) return;

        EstaOcupado = true;
        try
        {
            await _servicio.PonerEnCuarentenaAsync(lote.IdLote);
            await CargarCuarentenaAsync();
            await Shell.Current.DisplayAlert("Lote en Cuarentena", $"El lote '{lote.NumeroLote}' fue retenido preventivamente en estado CUARENTENA.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    // =====================================================================
    // MÉTODOS PESTAÑA 3: DEVOLUCIONES A PROVEEDOR (RF-COM-07)
    // =====================================================================

    public async Task CargarDevolucionesAsync()
    {
        if (EstaOcupado) return;
        EstaOcupado = true;
        MensajeError = string.Empty;

        try
        {
            await _servicio.InicializarTablasDevoluciones();

            var provs = await _servicio.ObtenerProveedoresAsync();
            ProveedoresDevolucion.Clear();
            foreach (var p in provs)
                ProveedoresDevolucion.Add(p);

            var hist = await _servicio.ObtenerHistorialDevolucionesAsync(null);
            HistorialDevoluciones.Clear();
            foreach (var h in hist)
                HistorialDevoluciones.Add(h);
        }
        catch (Exception ex)
        {
            MensajeError = "Error al cargar devoluciones: " + ex.Message;
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private async Task CargarLotesDevolucionAsync(Proveedor? proveedor)
    {
        LotesDevolucion.Clear();
        HistorialDevoluciones.Clear();
        RecalcularTotalDevolucion();

        if (proveedor is null) return;

        try
        {
            var lotes = await _servicio.ObtenerLotesParaDevolucionAsync(proveedor.Id);
            foreach (var l in lotes)
            {
                l.PropertyChanged += (_, _) => RecalcularTotalDevolucion();
                LotesDevolucion.Add(l);
            }

            var hist = await _servicio.ObtenerHistorialDevolucionesAsync(proveedor.Id);
            foreach (var h in hist)
                HistorialDevoluciones.Add(h);

            RecalcularTotalDevolucion();
        }
        catch (Exception ex)
        {
            MensajeError = "Error al cargar lotes para devolución: " + ex.Message;
        }
    }

    private void RecalcularTotalDevolucion()
    {
        OnPropertyChanged(nameof(MontoTotalDevolucion));
        OnPropertyChanged(nameof(MontoTotalDevolucionTexto));
    }

    private async Task RegistrarDevolucionAsync()
    {
        if (EstaOcupado) return;

        if (ProveedorDevolucionSeleccionado == null)
        {
            await Shell.Current.DisplayAlert("Validación", "Debe seleccionar un proveedor para la devolución.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(NumeroNotaCreditoDev))
        {
            await Shell.Current.DisplayAlert("Validación", "El número de nota de crédito correspondiente es obligatorio.", "OK");
            return;
        }

        var lineasConDevolucion = LotesDevolucion.Where(l => l.CantidadADevolver > 0).ToList();
        if (lineasConDevolucion.Count == 0)
        {
            await Shell.Current.DisplayAlert("Validación", "Debe ingresar una cantidad a devolver mayor a 0 en al menos un lote.", "OK");
            return;
        }

        foreach (var l in lineasConDevolucion)
        {
            if (l.CantidadADevolver > l.StockActual)
            {
                await Shell.Current.DisplayAlert("Validación", $"La cantidad a devolver para el lote '{l.NumeroLote}' supera el stock físico disponible ({l.StockActual} un.).", "OK");
                return;
            }
        }

        var confirmar = await Shell.Current.DisplayAlert(
            "Confirmar Devolución y Nota de Crédito",
            $"¿Confirmar la devolución de {lineasConDevolucion.Count} lote(s) al proveedor '{ProveedorDevolucionSeleccionado.RazonSocial}' por un monto total de {MontoTotalDevolucionTexto}?\n\n• Motivo: {MotivoDevolucion}\n• Nota de Crédito: {NumeroNotaCreditoDev}\n• Impacto: Se rebajará el stock de los lotes afectados y se registrará la SALIDA en Kardex.",
            "Generar Devolución", "Cancelar");

        if (!confirmar) return;

        EstaOcupado = true;
        try
        {
            var input = new DevolucionProveedorInput
            {
                IdProveedor = ProveedorDevolucionSeleccionado.Id,
                NumeroNotaCredito = NumeroNotaCreditoDev.Trim(),
                FechaDevolucion = FechaDevolucion,
                Motivo = MotivoDevolucion,
                Observaciones = ObservacionesDev,
                Lineas = lineasConDevolucion
            };

            int idUsuario = Sesion.UsuarioActual?.IdUsuario ?? 1;
            var (idDev, nc) = await _servicio.RegistrarDevolucionProveedorAsync(input, idUsuario);

            NumeroNotaCreditoDev = $"NC-{DateTime.Now.Year}-{new Random().Next(100, 999)}";
            ObservacionesDev = string.Empty;
            MotivoDevolucion = "MERMA";

            await CargarLotesDevolucionAsync(ProveedorDevolucionSeleccionado);

            await Shell.Current.DisplayAlert(
                "Devolución Registrada Exitosamente",
                $"Devolución N° {idDev} procesada correctamente bajo la Nota de Crédito {nc}.\n\n• Stock de los lotes rebajado en inventario.\n• Movimientos de salida registrados en Kardex.\n• Registro archivado en el historial de devoluciones.",
                "Entendido");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    // =====================================================================
    // MÉTODOS PESTAÑA 4: EQUIVALENCIAS DE PRESENTACIÓN (RF-COM-11)
    // =====================================================================

    public async Task CargarEquivalenciasAsync()
    {
        if (EstaOcupado) return;
        EstaOcupado = true;
        MensajeError = string.Empty;

        try
        {
            await _servicio.InicializarTablasEquivalencias();

            var prods = await _servicio.ObtenerProductosParaCompraAsync();
            ProductosParaEquivalencia.Clear();
            foreach (var p in prods)
                ProductosParaEquivalencia.Add(p);

            var eqs = await _servicio.ObtenerEquivalenciasAsync();
            Equivalencias.Clear();
            foreach (var eq in eqs)
                Equivalencias.Add(eq);
        }
        catch (Exception ex)
        {
            MensajeError = "Error al cargar equivalencias: " + ex.Message;
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private void AbrirNuevaEquivalencia()
    {
        ProductoEqSeleccionado = ProductosParaEquivalencia.FirstOrDefault();
        UnidadCompraEq = "Caja x 1000 comp";
        UnidadVentaEq = "Blíster x 10 comp";
        FactorConversionEqTexto = "100";
        FormularioEquivalenciaVisible = true;
    }

    private async Task GuardarEquivalenciaAsync()
    {
        if (EstaOcupado) return;

        if (ProductoEqSeleccionado == null)
        {
            await Shell.Current.DisplayAlert("Validación", "Debe seleccionar un producto.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(UnidadCompraEq))
        {
            await Shell.Current.DisplayAlert("Validación", "Debe indicar la unidad de compra (ej: Caja x 1000).", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(UnidadVentaEq))
        {
            await Shell.Current.DisplayAlert("Validación", "Debe indicar la unidad de venta/dispensación (ej: Blíster x 10).", "OK");
            return;
        }

        if (!decimal.TryParse(FactorConversionEqTexto?.Trim(), NumberStyles.Number, Formato.Cultura, out decimal factor) || factor <= 0)
        {
            await Shell.Current.DisplayAlert("Validación", "El factor de conversión debe ser un número mayor a 0 (ej: 100).", "OK");
            return;
        }

        EstaOcupado = true;
        try
        {
            var eq = new EquivalenciaProducto
            {
                IdProducto = ProductoEqSeleccionado.Id,
                UnidadCompra = UnidadCompraEq.Trim(),
                UnidadVenta = UnidadVentaEq.Trim(),
                FactorConversion = factor,
                Activo = true
            };

            await _servicio.GuardarEquivalenciaAsync(eq);
            FormularioEquivalenciaVisible = false;
            await CargarEquivalenciasAsync();

            await Shell.Current.DisplayAlert("Equivalencia Guardada", $"Equivalencia para '{ProductoEqSeleccionado.Nombre}' configurada: 1 {eq.UnidadCompra} = {factor:0.##} {eq.UnidadVenta}.\n\nAl recepcionar facturas, la conversión se aplicará automáticamente al inventario.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private async Task EliminarEquivalenciaAsync(EquivalenciaProducto? eq)
    {
        if (eq is null || EstaOcupado) return;

        var confirmar = await Shell.Current.DisplayAlert("Eliminar Equivalencia", $"¿Desea eliminar la equivalencia de presentación para '{eq.NombreProducto}'?", "Eliminar", "Cancelar");
        if (!confirmar) return;

        EstaOcupado = true;
        try
        {
            await _servicio.EliminarEquivalenciaAsync(eq.IdEquivalencia);
            await CargarEquivalenciasAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            EstaOcupado = false;
        }
    }
}
