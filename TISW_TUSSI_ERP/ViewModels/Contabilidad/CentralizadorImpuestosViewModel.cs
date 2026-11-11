using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Auth;
using TISW_TUSSI_ERP.Models.Contabilidad;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Contabilidad;

// RF-CON-09: Reporte centralizador de impuestos - Libro de compras y ventas consolidado para el periodo,
// con cálculo automático de montos netos, IVA (débito y crédito fiscal) y totales para facilitar la declaración tributaria (F29 SII Chile).
public class CentralizadorImpuestosViewModel : ReporteBaseViewModel
{
    private readonly ContabilidadService _contabilidadService;

    public ObservableCollection<DocumentoLibroVentasItem> LibroVentas { get; } = new();
    public ObservableCollection<DocumentoLibroComprasItem> LibroCompras { get; } = new();
    public ObservableCollection<ResumenDteSiiItem> ResumenDte { get; } = new();
    public ObservableCollection<LineaFormulario29Item> LineasF29 { get; } = new();

    private DateTime _fechaDesde = new(DateTime.Today.Year, 1, 1);
    public DateTime FechaDesde { get => _fechaDesde; set => SetProperty(ref _fechaDesde, value); }

    private DateTime _fechaHasta = DateTime.Today.AddMonths(2);
    public DateTime FechaHasta { get => _fechaHasta; set => SetProperty(ref _fechaHasta, value); }

    // Tasa PPM (Pago Provisional Mensual Art. 84 LIR - Pro Pyme 0.25% / Régimen General 1.5%)
    private decimal _tasaPpmPorcentaje = 1.5m;
    public decimal TasaPpmPorcentaje
    {
        get => _tasaPpmPorcentaje;
        set
        {
            if (SetProperty(ref _tasaPpmPorcentaje, value))
                RecalcularTotalesTributarios();
        }
    }

    // Totales Libro de Ventas
    private decimal _ventasNetoTotal, _ivaDebitoFiscal, _ventasTotalBruto;
    public string VentasNetoTexto => Formato.Moneda(_ventasNetoTotal);
    public string IvaDebitoFiscalTexto => Formato.Moneda(_ivaDebitoFiscal);
    public string VentasTotalBrutoTexto => Formato.Moneda(_ventasTotalBruto);

    // Totales Libro de Compras
    private decimal _comprasNetoTotal, _ivaCreditoFiscal, _comprasTotalBruto;
    public string ComprasNetoTexto => Formato.Moneda(_comprasNetoTotal);
    public string IvaCreditoFiscalTexto => Formato.Moneda(_ivaCreditoFiscal);
    public string ComprasTotalBrutoTexto => Formato.Moneda(_comprasTotalBruto);

    // Determinación IVA y Formulario 29 SII Chile
    private decimal _ivaDeterminadoPagar, _remanenteCreditoFiscal, _montoPpm, _totalPagarF29;
    public string IvaDeterminadoTexto => Formato.Moneda(_ivaDeterminadoPagar);
    public string RemanenteTexto => Formato.Moneda(_remanenteCreditoFiscal);
    public string PpmTexto => Formato.Moneda(_montoPpm);
    public string TotalPagarF29Texto => Formato.Moneda(_totalPagarF29);

    public string EstadoTributarioIva => _ivaDebitoFiscal >= _ivaCreditoFiscal
        ? $"IVA A PAGAR (Cód. 89): {IvaDeterminadoTexto}"
        : $"REMANENTE CRÉDITO FISCAL (Cód. 77): {RemanenteTexto}";

    public Color ColorEstadoTributario => _ivaDebitoFiscal >= _ivaCreditoFiscal
        ? Color.FromArgb("#D97706")
        : Color.FromArgb("#047857");

    // Alias de propiedades y comandos para XAML
    public string PeriodoTexto => Rango(FechaDesde, FechaHasta);
    public string VentasIvaTexto => IvaDebitoFiscalTexto;
    public string ComprasIvaTexto => IvaCreditoFiscalTexto;
    public string EstadoIvaEtiqueta => _ivaDebitoFiscal >= _ivaCreditoFiscal ? "IVA NETO A PAGAR (CÓD. 89)" : "REMANENTE CRÉDITO (CÓD. 77)";
    public Color EstadoIvaColor => ColorEstadoTributario;
    public Color EstadoIvaFondo => _ivaDebitoFiscal >= _ivaCreditoFiscal ? Color.FromArgb("#FEF3C7") : Color.FromArgb("#ECFDF5");
    public string PpmMontoTexto => PpmTexto;
    public string TotalF29Texto => TotalPagarF29Texto;
    public ObservableCollection<LineaFormulario29Item> LineasFormulario29 => LineasF29;
    public string VentasTotalTexto => VentasTotalBrutoTexto;
    public string ComprasTotalTexto => ComprasTotalBrutoTexto;

    public ICommand ContabilizarLiquidacionIvaCommand { get; }
    public ICommand RangoMesActualCommand { get; }
    public ICommand RangoTrimestreCommand { get; }
    public ICommand RangoAnioActualCommand { get; }

    public CentralizadorImpuestosViewModel(ContabilidadService contabilidadService)
    {
        _contabilidadService = contabilidadService;
        Titulo = "Centralizador de Impuestos (Libro Compras/Ventas y F29)";
        ContabilizarLiquidacionIvaCommand = new Command(async () => await ContabilizarLiquidacionIvaAsync());
        RangoMesActualCommand = new Command(async () => { FechaDesde = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1); FechaHasta = DateTime.Today; await GenerarAsync(); });
        RangoTrimestreCommand = new Command(async () => { FechaDesde = DateTime.Today.AddMonths(-3); FechaHasta = DateTime.Today; await GenerarAsync(); });
        RangoAnioActualCommand = new Command(async () => { FechaDesde = new DateTime(DateTime.Today.Year, 1, 1); FechaHasta = DateTime.Today.AddMonths(1); await GenerarAsync(); });
    }

    protected override async Task CargarDatosAsync()
    {
        if (FechaHasta < FechaDesde)
            throw new InvalidOperationException("La fecha 'Hasta' no puede ser anterior a 'Desde'.");

        var (ventas, compras) = await _contabilidadService.ObtenerLibrosImpuestosAsync(FechaDesde, FechaHasta);

        LibroVentas.Clear();
        foreach (var v in ventas) LibroVentas.Add(v);

        LibroCompras.Clear();
        foreach (var c in compras) LibroCompras.Add(c);

        RecalcularTotalesTributarios();
        HayDatos = LibroVentas.Count > 0 || LibroCompras.Count > 0;
    }

    private void RecalcularTotalesTributarios()
    {
        // Ventas: Facturas y Boletas suman al Débito Fiscal; Notas de Crédito restan al Débito Fiscal (Art. 21 D.L. 825)
        decimal ventasFacturasBoletasNeto = LibroVentas.Where(v => !v.EsNotaCredito).Sum(v => v.MontoNeto);
        decimal ventasNotasCreditoNeto = LibroVentas.Where(v => v.EsNotaCredito).Sum(v => v.MontoNeto);
        _ventasNetoTotal = Math.Max(0, ventasFacturasBoletasNeto - ventasNotasCreditoNeto);

        decimal ivaDebitoVentas = LibroVentas.Where(v => !v.EsNotaCredito).Sum(v => v.MontoIva);
        decimal ivaNotasCredito = LibroVentas.Where(v => v.EsNotaCredito).Sum(v => v.MontoIva);
        _ivaDebitoFiscal = Math.Max(0, ivaDebitoVentas - ivaNotasCredito);

        _ventasTotalBruto = Math.Max(0, LibroVentas.Where(v => !v.EsNotaCredito).Sum(v => v.MontoTotal) - LibroVentas.Where(v => v.EsNotaCredito).Sum(v => v.MontoTotal));

        // Compras: IVA Crédito Fiscal Recuperable del Giro (Art. 23 D.L. 825)
        _comprasNetoTotal = LibroCompras.Sum(c => c.MontoNeto);
        _ivaCreditoFiscal = LibroCompras.Sum(c => c.MontoIvaCredito);
        _comprasTotalBruto = LibroCompras.Sum(c => c.MontoTotal);

        // Determinación IVA Mensual
        if (_ivaDebitoFiscal >= _ivaCreditoFiscal)
        {
            _ivaDeterminadoPagar = _ivaDebitoFiscal - _ivaCreditoFiscal;
            _remanenteCreditoFiscal = 0;
        }
        else
        {
            _ivaDeterminadoPagar = 0;
            _remanenteCreditoFiscal = _ivaCreditoFiscal - _ivaDebitoFiscal;
        }

        // PPM sobre Ingresos Brutos Netos del período (Art. 84 Ley de Impuesto a la Renta)
        _montoPpm = Math.Round(_ventasNetoTotal * (TasaPpmPorcentaje / 100m), 0);
        _totalPagarF29 = _ivaDeterminadoPagar + _montoPpm;

        // Resumen por Tipo de DTE (Registro de Compras y Ventas SII)
        ResumenDte.Clear();
        foreach (var g in LibroVentas.GroupBy(v => new { v.CodigoSii, v.TipoDteTexto }).OrderBy(g => g.Key.CodigoSii))
        {
            ResumenDte.Add(new ResumenDteSiiItem
            {
                CodigoSii = g.Key.CodigoSii,
                TipoDocumento = g.Key.TipoDteTexto,
                Libro = "VENTAS (DÉBITO)",
                CantidadDocumentos = g.Count(),
                TotalNeto = g.Sum(x => x.MontoNeto),
                TotalIva = g.Sum(x => x.MontoIva),
                TotalBruto = g.Sum(x => x.MontoTotal)
            });
        }

        if (LibroCompras.Count > 0)
        {
            ResumenDte.Add(new ResumenDteSiiItem
            {
                CodigoSii = "DTE 33",
                TipoDocumento = "Facturas Recibidas del Giro (Compras)",
                Libro = "COMPRAS (CRÉDITO)",
                CantidadDocumentos = LibroCompras.Count,
                TotalNeto = _comprasNetoTotal,
                TotalIva = _ivaCreditoFiscal,
                TotalBruto = _comprasTotalBruto
            });
        }

        // Construcción de Casillas Oficiales Formulario 29 SII Chile
        LineasF29.Clear();
        LineasF29.Add(new LineaFormulario29Item
        {
            CodigoCasilla = "Cód. 538",
            GlosaCasilla = "Total IVA Débito Fiscal determinado en el período (Facturas + Boletas − Notas de Crédito)",
            ReferenciaLegal = "D.L. 825 Art. 14 y 20",
            Operador = "+",
            Monto = _ivaDebitoFiscal
        });
        LineasF29.Add(new LineaFormulario29Item
        {
            CodigoCasilla = "Cód. 537",
            GlosaCasilla = "Total IVA Crédito Fiscal recuperable del giro en el período (Facturas de Compra)",
            ReferenciaLegal = "D.L. 825 Art. 23",
            Operador = "−",
            Monto = _ivaCreditoFiscal
        });

        if (_ivaDeterminadoPagar > 0)
        {
            LineasF29.Add(new LineaFormulario29Item
            {
                CodigoCasilla = "Cód. 089",
                GlosaCasilla = "Impuesto Determinado (IVA Neto a Pagar al Fisco)",
                ReferenciaLegal = "Débito Fiscal > Crédito Fiscal",
                Operador = "=",
                Monto = _ivaDeterminadoPagar,
                EsTotal = true
            });
        }
        else
        {
            LineasF29.Add(new LineaFormulario29Item
            {
                CodigoCasilla = "Cód. 077",
                GlosaCasilla = "Remanente de Crédito Fiscal para el período siguiente (a favor del contribuyente)",
                ReferenciaLegal = "Crédito Fiscal > Débito Fiscal",
                Operador = "=",
                Monto = _remanenteCreditoFiscal,
                EsTotal = true
            });
        }

        LineasF29.Add(new LineaFormulario29Item
        {
            CodigoCasilla = "Cód. 062",
            GlosaCasilla = $"Pago Provisional Mensual Obligatorio (PPM {TasaPpmPorcentaje:0.##}% sobre Ventas Netas de {VentasNetoTexto})",
            ReferenciaLegal = "LIR Art. 84 letra a)",
            Operador = "+",
            Monto = _montoPpm
        });

        LineasF29.Add(new LineaFormulario29Item
        {
            CodigoCasilla = "Cód. 091",
            GlosaCasilla = "TOTAL DETERMINADO A PAGAR EN FORMULARIO 29 (Vencimiento legal: día 20 mes siguiente)",
            ReferenciaLegal = "Tesorería General de la República / SII",
            Operador = "=",
            Monto = _totalPagarF29,
            EsTotal = true
        });

        foreach (var p in new[]
        {
            nameof(VentasNetoTexto), nameof(IvaDebitoFiscalTexto), nameof(VentasTotalBrutoTexto),
            nameof(ComprasNetoTexto), nameof(IvaCreditoFiscalTexto), nameof(ComprasTotalBrutoTexto),
            nameof(IvaDeterminadoTexto), nameof(RemanenteTexto), nameof(PpmTexto), nameof(TotalPagarF29Texto),
            nameof(EstadoTributarioIva), nameof(ColorEstadoTributario),
            nameof(PeriodoTexto), nameof(VentasIvaTexto), nameof(ComprasIvaTexto),
            nameof(EstadoIvaEtiqueta), nameof(EstadoIvaColor), nameof(EstadoIvaFondo),
            nameof(PpmMontoTexto), nameof(TotalF29Texto), nameof(VentasTotalTexto), nameof(ComprasTotalTexto)
        })
        {
            OnPropertyChanged(p);
        }
    }

    private async Task ContabilizarLiquidacionIvaAsync()
    {
        if (_ivaDebitoFiscal == 0 && _ivaCreditoFiscal == 0)
        {
            MensajeError = "No hay montos de IVA en el período seleccionado para centralizar.";
            return;
        }

        try
        {
            await _contabilidadService.AsegurarPlanCuentasChilenoAsync();
            var cuentas = await _contabilidadService.ObtenerPlanCuentasAsync();

            var cIvaDebito = cuentas.FirstOrDefault(c => c.CodigoCuenta == "2.1.02");
            var cIvaCredito = cuentas.FirstOrDefault(c => c.CodigoCuenta == "1.1.04");
            var cImpuestosPagar = cuentas.FirstOrDefault(c => c.CodigoCuenta == "2.1.03") ?? cIvaDebito;

            if (cIvaDebito == null || cIvaCredito == null || cImpuestosPagar == null)
            {
                MensajeError = "No se encontraron las cuentas de IVA en el Plan de Cuentas.";
                return;
            }

            // Para cuadrar la compensación mensual de IVA (cierre de IVA del mes):
            // Se compensa hasta el menor entre IVA Débito e IVA Crédito, y la diferencia si Débito > Crédito queda en Impuestos por Pagar F29.
            var lineas = new List<LineaAsientoInput>();
            if (_ivaDebitoFiscal >= _ivaCreditoFiscal)
            {
                if (_ivaDebitoFiscal > 0)
                    lineas.Add(new LineaAsientoInput { IdCuenta = cIvaDebito.IdCuenta, CodigoCuenta = cIvaDebito.CodigoCuenta, NombreCuenta = cIvaDebito.NombreCuenta, Debe = _ivaDebitoFiscal });
                if (_ivaCreditoFiscal > 0)
                    lineas.Add(new LineaAsientoInput { IdCuenta = cIvaCredito.IdCuenta, CodigoCuenta = cIvaCredito.CodigoCuenta, NombreCuenta = cIvaCredito.NombreCuenta, Haber = _ivaCreditoFiscal });
                if (_ivaDeterminadoPagar > 0)
                    lineas.Add(new LineaAsientoInput { IdCuenta = cImpuestosPagar.IdCuenta, CodigoCuenta = cImpuestosPagar.CodigoCuenta, NombreCuenta = cImpuestosPagar.NombreCuenta, Haber = _ivaDeterminadoPagar });
            }
            else
            {
                // Si Crédito > Débito, compensamos todo el Débito contra Crédito Fiscal, quedando el remanente en la cuenta de activo IVA Crédito Fiscal.
                lineas.Add(new LineaAsientoInput { IdCuenta = cIvaDebito.IdCuenta, CodigoCuenta = cIvaDebito.CodigoCuenta, NombreCuenta = cIvaDebito.NombreCuenta, Debe = _ivaDebitoFiscal });
                lineas.Add(new LineaAsientoInput { IdCuenta = cIvaCredito.IdCuenta, CodigoCuenta = cIvaCredito.CodigoCuenta, NombreCuenta = cIvaCredito.NombreCuenta, Haber = _ivaDebitoFiscal });
            }

            int idUsuario = Sesion.UsuarioActual?.IdUsuario ?? 1;
            string glosa = $"Liquidación y compensación mensual de IVA F29 ({Rango(FechaDesde, FechaHasta)})";
            await _contabilidadService.RegistrarAsientoManualAsync( DateTime.Today, glosa, "AJUSTE", idUsuario, lineas);

            MensajeExito = $"Asiento de Liquidación Mensual de IVA (F29) registrado correctamente en el Libro Diario.";
        }
        catch (Exception ex)
        {
            MensajeError = $"No se pudo contabilizar la liquidación de IVA: {ex.Message}";
        }
    }

    protected override DocumentoReporte ConstruirDocumento()
    {
        var doc = new DocumentoReporte
        {
            Titulo = "Reporte Centralizador de Impuestos (Libro Compras/Ventas y F29 SII)",
            Subtitulo = $"Consolidado tributario de Ventas, Compras, IVA Débito/Crédito Fiscal (19%) y PPM · {Rango(FechaDesde, FechaHasta)}",
            Horizontal = true
        };
        doc.Filtros.Add(("Período Tributario", Rango(FechaDesde, FechaHasta)));
        doc.Kpis.Add(("Ventas Netas", VentasNetoTexto));
        doc.Kpis.Add(("IVA Débito (19%)", IvaDebitoFiscalTexto));
        doc.Kpis.Add(("Compras Netas", ComprasNetoTexto));
        doc.Kpis.Add(("IVA Crédito (19%)", IvaCreditoFiscalTexto));
        doc.Kpis.Add(("PPM (Art. 84)", PpmTexto));
        doc.Kpis.Add(("Total F29 a Pagar", TotalPagarF29Texto));

        var sF29 = new SeccionReporte("Declaración Mensual Formulario 29 (Simulación SII Chile)",
            new ColumnaReporte("Código F29"),
            new ColumnaReporte("Concepto Tributario"),
            new ColumnaReporte("Normativa"),
            new ColumnaReporte("Op."),
            new ColumnaReporte("Monto CLP", TipoColumna.Moneda));
        foreach (var f in LineasF29)
            sF29.Filas.Add(new FilaReporte { Celdas = new object?[] { f.CodigoCasilla, f.GlosaCasilla, f.ReferenciaLegal, f.Operador, f.Monto }, EsTotal = f.EsTotal });
        doc.Secciones.Add(sF29);

        var sLV = new SeccionReporte("Libro de Ventas Consolidado (SII)",
            new ColumnaReporte("Fecha", TipoColumna.Fecha),
            new ColumnaReporte("DTE"),
            new ColumnaReporte("Folio"),
            new ColumnaReporte("RUT Cliente"),
            new ColumnaReporte("Razón Social / Cliente"),
            new ColumnaReporte("Monto Neto", TipoColumna.Moneda),
            new ColumnaReporte("IVA Débito (19%)", TipoColumna.Moneda),
            new ColumnaReporte("Monto Total", TipoColumna.Moneda));
        foreach (var v in LibroVentas)
            sLV.Fila(v.FechaEmision, v.TipoDteTexto, v.Folio, v.RutCliente, v.RazonSocial, v.MontoNeto, v.MontoIva, v.MontoTotal);
        sLV.Total(null, "", "TOTAL VENTAS", "", "", _ventasNetoTotal, _ivaDebitoFiscal, _ventasTotalBruto);
        doc.Secciones.Add(sLV);

        var sLC = new SeccionReporte("Libro de Compras Consolidado (SII)",
            new ColumnaReporte("Fecha", TipoColumna.Fecha),
            new ColumnaReporte("DTE"),
            new ColumnaReporte("N° Factura"),
            new ColumnaReporte("Orden Compra"),
            new ColumnaReporte("RUT Proveedor"),
            new ColumnaReporte("Razón Social Proveedor"),
            new ColumnaReporte("Monto Neto", TipoColumna.Moneda),
            new ColumnaReporte("IVA Crédito (19%)", TipoColumna.Moneda),
            new ColumnaReporte("Monto Total", TipoColumna.Moneda));
        foreach (var c in LibroCompras)
            sLC.Fila(c.FechaFactura, c.TipoDteTexto, c.NumeroFactura, c.NumeroOrdenCompra, c.RutProveedor, c.RazonSocial, c.MontoNeto, c.MontoIvaCredito, c.MontoTotal);
        sLC.Total(null, "", "TOTAL COMPRAS", "", "", "", _comprasNetoTotal, _ivaCreditoFiscal, _comprasTotalBruto);
        doc.Secciones.Add(sLC);

        return doc;
    }
}
