using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Contabilidad;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Contabilidad;

// RF-CON-01: Plan de cuentas - Estructura jerárquica del catálogo de cuentas contables (activo, pasivo, patrimonio, ingreso, gasto).
public class PlanCuentasViewModel : ReporteBaseViewModel
{
    private readonly ContabilidadService _contabilidadService;
    private List<PlanCuentas> _todas = new();

    public ObservableCollection<PlanCuentas> Cuentas { get; } = new();
    public ObservableCollection<PlanCuentas> CuentasPadreDisponibles { get; } = new();
    public List<string> TiposFiltro { get; } = new() { "Todos", "ACTIVO", "PASIVO", "PATRIMONIO", "INGRESO", "GASTO" };
    public List<string> TiposCuentaFormulario { get; } = new() { "ACTIVO", "PASIVO", "PATRIMONIO", "INGRESO", "GASTO" };
    public List<int> NivelesFormulario { get; } = new() { 1, 2, 3 };

    private string _tipoSeleccionado = "Todos";
    public string TipoSeleccionado
    {
        get => _tipoSeleccionado;
        set { if (SetProperty(ref _tipoSeleccionado, value)) AplicarFiltro(); }
    }

    private string _busqueda = string.Empty;
    public string Busqueda
    {
        get => _busqueda;
        set { if (SetProperty(ref _busqueda, value)) AplicarFiltro(); }
    }

    // KPIs
    private int _totalCuentas;
    public int TotalCuentas { get => _totalCuentas; set => SetProperty(ref _totalCuentas, value); }

    private int _cuentasImputables;
    public int CuentasImputables { get => _cuentasImputables; set => SetProperty(ref _cuentasImputables, value); }

    private string _totalActivosTexto = "$0";
    public string TotalActivosTexto { get => _totalActivosTexto; set => SetProperty(ref _totalActivosTexto, value); }

    private string _totalPasivosTexto = "$0";
    public string TotalPasivosTexto { get => _totalPasivosTexto; set => SetProperty(ref _totalPasivosTexto, value); }

    private string _totalPatrimonioTexto = "$0";
    public string TotalPatrimonioTexto { get => _totalPatrimonioTexto; set => SetProperty(ref _totalPatrimonioTexto, value); }

    // Modal Crear / Editar Cuenta Contable
    private bool _mostrarModal;
    public bool MostrarModal { get => _mostrarModal; set => SetProperty(ref _mostrarModal, value); }

    private int _idCuentaEditando;
    public string TituloModal => _idCuentaEditando == 0 ? "Nueva Cuenta Contable (Catálogo SII / IFRS)" : "Editar Cuenta Contable";

    private string _formCodigo = string.Empty;
    public string FormCodigo { get => _formCodigo; set => SetProperty(ref _formCodigo, value); }

    private string _formNombre = string.Empty;
    public string FormNombre { get => _formNombre; set => SetProperty(ref _formNombre, value); }

    private string _formTipo = "ACTIVO";
    public string FormTipo { get => _formTipo; set => SetProperty(ref _formTipo, value); }

    private int _formNivel = 2;
    public int FormNivel { get => _formNivel; set => SetProperty(ref _formNivel, value); }

    private PlanCuentas? _formCuentaPadre;
    public PlanCuentas? FormCuentaPadre { get => _formCuentaPadre; set => SetProperty(ref _formCuentaPadre, value); }

    public ICommand CargarCuentasCommand { get; }
    public ICommand FiltrarTipoCommand { get; }
    public ICommand NuevaCuentaCommand { get; }
    public ICommand EditarCuentaCommand { get; }
    public ICommand GuardarCuentaCommand { get; }
    public ICommand EliminarCuentaCommand { get; }
    public ICommand CerrarModalCommand { get; }
    public ICommand HomologarPlanSiiCommand { get; }

    public PlanCuentasViewModel(ContabilidadService contabilidadService)
    {
        _contabilidadService = contabilidadService;
        Titulo = "Plan de Cuentas";
        CargarCuentasCommand = GenerarCommand;
        FiltrarTipoCommand = new Command<string>(t => TipoSeleccionado = t ?? "Todos");
        NuevaCuentaCommand = new Command(AbrirNuevaCuenta);
        EditarCuentaCommand = new Command<PlanCuentas>(AbrirEditarCuenta);
        GuardarCuentaCommand = new Command(async () => await GuardarCuentaAsync());
        EliminarCuentaCommand = new Command<PlanCuentas>(async c => await EliminarCuentaAsync(c));
        CerrarModalCommand = new Command(() => MostrarModal = false);
        HomologarPlanSiiCommand = new Command(async () => await HomologarPlanSiiAsync());
    }

    protected override async Task CargarDatosAsync()
    {
        await _contabilidadService.AsegurarPlanCuentasChilenoAsync();
        _todas = await _contabilidadService.ObtenerPlanCuentasAsync();

        CuentasPadreDisponibles.Clear();
        CuentasPadreDisponibles.Add(new PlanCuentas { IdCuenta = 0, CodigoCuenta = "—", NombreCuenta = "(Sin cuenta padre / Nivel 1)" });
        foreach (var g in _todas.Where(c => c.Nivel <= 2))
            CuentasPadreDisponibles.Add(g);

        AplicarFiltro();
    }

    private void AplicarFiltro()
    {
        var q = _todas.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(TipoSeleccionado) && TipoSeleccionado != "Todos")
            q = q.Where(c => string.Equals(c.TipoCuenta, TipoSeleccionado, StringComparison.OrdinalIgnoreCase));

        var txt = Busqueda.Trim();
        if (!string.IsNullOrEmpty(txt))
            q = q.Where(c => c.CodigoCuenta.Contains(txt, StringComparison.OrdinalIgnoreCase)
                          || c.NombreCuenta.Contains(txt, StringComparison.OrdinalIgnoreCase)
                          || c.TipoCuenta.Contains(txt, StringComparison.OrdinalIgnoreCase));

        var lista = q.ToList();
        Cuentas.Clear();
        foreach (var c in lista) Cuentas.Add(c);

        TotalCuentas = _todas.Count;
        CuentasImputables = _todas.Count(c => !c.EsGrupo);

        var imputables = _todas.Where(c => !c.EsGrupo).ToList();
        var act = imputables.Where(c => c.TipoCuenta == "ACTIVO").Sum(c => c.SaldoActual);
        var pas = imputables.Where(c => c.TipoCuenta == "PASIVO").Sum(c => c.SaldoActual);
        var pat = imputables.Where(c => c.TipoCuenta == "PATRIMONIO").Sum(c => c.SaldoActual);

        TotalActivosTexto = Formato.Moneda(act);
        TotalPasivosTexto = Formato.Moneda(pas);
        TotalPatrimonioTexto = Formato.Moneda(pat);
        HayDatos = Cuentas.Count > 0;
    }

    private void AbrirNuevaCuenta()
    {
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        _idCuentaEditando = 0;
        FormCodigo = "1.1.07";
        FormNombre = string.Empty;
        FormTipo = "ACTIVO";
        FormNivel = 2;
        FormCuentaPadre = CuentasPadreDisponibles.FirstOrDefault(c => c.CodigoCuenta == "1") ?? CuentasPadreDisponibles.FirstOrDefault();
        OnPropertyChanged(nameof(TituloModal));
        MostrarModal = true;
    }

    private void AbrirEditarCuenta(PlanCuentas? cuenta)
    {
        if (cuenta == null) return;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        _idCuentaEditando = cuenta.IdCuenta;
        FormCodigo = cuenta.CodigoCuenta;
        FormNombre = cuenta.NombreCuenta;
        FormTipo = cuenta.TipoCuenta;
        FormNivel = cuenta.Nivel;
        FormCuentaPadre = CuentasPadreDisponibles.FirstOrDefault(c => c.IdCuenta == (cuenta.CuentaPadreId ?? 0)) ?? CuentasPadreDisponibles.FirstOrDefault();
        OnPropertyChanged(nameof(TituloModal));
        MostrarModal = true;
    }

    private async Task GuardarCuentaAsync()
    {
        if (string.IsNullOrWhiteSpace(FormCodigo) || string.IsNullOrWhiteSpace(FormNombre))
        {
            MensajeError = "Debes ingresar el código jerárquico (ej. 1.1.07) y el nombre de la cuenta contable.";
            return;
        }

        try
        {
            var nueva = new PlanCuentas
            {
                IdCuenta = _idCuentaEditando,
                CodigoCuenta = FormCodigo.Trim(),
                NombreCuenta = FormNombre.Trim(),
                TipoCuenta = FormTipo,
                Nivel = FormNivel,
                CuentaPadreId = (FormCuentaPadre == null || FormCuentaPadre.IdCuenta == 0) ? null : FormCuentaPadre.IdCuenta
            };

            await _contabilidadService.GuardarCuentaContableAsync(nueva);
            MostrarModal = false;
            await GenerarAsync();
            MensajeExito = $"Cuenta '{nueva.CodigoCuenta} · {nueva.NombreCuenta}' guardada correctamente en el catálogo.";
        }
        catch (Exception ex)
        {
            MensajeError = $"No se pudo guardar la cuenta: {ex.Message}";
        }
    }

    private async Task EliminarCuentaAsync(PlanCuentas? cuenta)
    {
        if (cuenta == null) return;
        try
        {
            await _contabilidadService.EliminarCuentaContableAsync(cuenta.IdCuenta);
            await GenerarAsync();
            MensajeExito = $"Cuenta '{cuenta.CodigoCuenta} · {cuenta.NombreCuenta}' eliminada correctamente.";
        }
        catch (Exception ex)
        {
            MensajeError = ex.Message;
        }
    }

    private async Task HomologarPlanSiiAsync()
    {
        try
        {
            await _contabilidadService.AsegurarPlanCuentasChilenoAsync();
            await GenerarAsync();
            MensajeExito = "Plan de Cuentas homologado con las cuentas estándar SII / IFRS Pyme Chile.";
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al homologar el plan de cuentas: {ex.Message}";
        }
    }

    protected override DocumentoReporte ConstruirDocumento()
    {
        var doc = new DocumentoReporte
        {
            Titulo = "Plan de Cuentas Contables (Chile)",
            Subtitulo = "Catálogo jerárquico de cuentas bajo normativa PCGA / IFRS Pyme y SII Chile"
        };
        doc.Filtros.Add(("Rubro", TipoSeleccionado));
        doc.Kpis.Add(("Total cuentas", TotalCuentas.ToString()));
        doc.Kpis.Add(("Cuentas imputables", CuentasImputables.ToString()));
        doc.Kpis.Add(("Saldo Activos", TotalActivosTexto));
        doc.Kpis.Add(("Saldo Pasivos", TotalPasivosTexto));

        var s = new SeccionReporte("Catálogo de Cuentas",
            new ColumnaReporte("Código"),
            new ColumnaReporte("Nombre de la Cuenta"),
            new ColumnaReporte("Tipo / Rubro"),
            new ColumnaReporte("Nivel"),
            new ColumnaReporte("Naturaleza"),
            new ColumnaReporte("Total Debe", TipoColumna.Moneda),
            new ColumnaReporte("Total Haber", TipoColumna.Moneda),
            new ColumnaReporte("Saldo Actual", TipoColumna.Moneda));

        foreach (var c in Cuentas)
        {
            s.Filas.Add(new FilaReporte
            {
                Celdas = new object?[] { c.CodigoCuenta, c.NombreCuenta, c.TipoTexto, c.NivelTexto, c.NaturalezaTexto, c.TotalDebe, c.TotalHaber, c.SaldoActual },
                EsTotal = c.EsGrupo,
                Nivel = Math.Max(0, c.Nivel - 1)
            });
        }

        doc.Secciones.Add(s);
        return doc;
    }
}
