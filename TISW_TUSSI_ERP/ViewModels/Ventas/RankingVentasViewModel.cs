
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Ventas;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Ventas;

// RF-VEN-04: Ranking de ventas por cliente y convenio.
public class RankingVentasViewModel : BaseViewModel
{
    private readonly VentasService _servicio;

    public ObservableCollection<FilaRanking> Filas { get; } = new();

    private string _totalGeneralTexto = Formato.Moneda(0);
    private string _mensajeError = string.Empty;
    private string _busqueda = string.Empty;

    private int _totalDocumentos;
    private int _totalClientes;
    private decimal _totalGeneral;

    public string TotalGeneralTexto
    {
        get => _totalGeneralTexto;
        private set => SetProperty(ref _totalGeneralTexto, value);
    }

    public string TotalDocumentosTexto =>
        _totalDocumentos.ToString("N0");

    public string TotalClientesTexto =>
        _totalClientes.ToString("N0");

    public string CantidadClientesTexto =>
        $"Clientes con ventas: {_totalClientes}";

    public string TotalFilasTexto =>
        $"{Filas.Count} registros";

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

    public bool HayDatos => Filas.Count > 0;
    public bool NoHayDatos => !HayDatos;

    public string Busqueda
    {
        get => _busqueda;
        set
        {
            if (SetProperty(ref _busqueda, value))
                AplicarFiltro();
        }
    }

    private readonly System.Collections.Generic.List<FilaRanking>
        _filasOriginales = new();

    public ICommand ComandoActualizar { get; }
    public ICommand ComandoLimpiarFiltro { get; }
    public ICommand ExportarPdfCommand { get; }
    public ICommand ExportarCsvCommand { get; }

    public RankingVentasViewModel(VentasService servicio)
    {
        _servicio = servicio;

        Titulo = "Ranking de Ventas";

        ComandoActualizar = new Command(
            async () => await CargarAsync());

        ComandoLimpiarFiltro = new Command(() =>
        {
            Busqueda = string.Empty;
        });

        ExportarPdfCommand = new Command(async () => await ExportarAsync(true));
        ExportarCsvCommand = new Command(async () => await ExportarAsync(false));
    }

    private async Task ExportarAsync(bool pdf)
    {
        if (!Filas.Any())
            await CargarAsync();
        if (!Filas.Any()) return;

        var sec = new SeccionReporte(
            "Ranking de Ventas por Cliente y Convenio",
            new ColumnaReporte("RUT Cliente"),
            new ColumnaReporte("Cliente / Institución"),
            new ColumnaReporte("Tipo"),
            new ColumnaReporte("Convenio de Salud"),
            new ColumnaReporte("N° Documentos", TipoColumna.Numero),
            new ColumnaReporte("Total Ventas", TipoColumna.Moneda),
            new ColumnaReporte("Participación %", TipoColumna.Porcentaje));

        foreach (var f in Filas)
        {
            sec.Fila(f.RutCliente, f.NombreCliente, f.TipoCliente, f.NombreConvenio, f.Documentos, f.TotalVentas, f.Porcentaje);
        }
        sec.Total("TOTAL GENERAL", "", "", "", Filas.Sum(x => x.Documentos), Filas.Sum(x => x.TotalVentas), 100m);

        var doc = new DocumentoReporte
        {
            Titulo = "Ranking de Ventas por Cliente y Convenio (RF-VEN-04)",
            Subtitulo = "Análisis Pareto de clientes e instituciones de salud con mayores montos acumulados",
            Horizontal = true,
            Kpis =
            {
                ("Total Vendido", TotalGeneralTexto),
                ("Documentos Emitidos", TotalDocumentosTexto),
                ("Clientes con Ventas", TotalClientesTexto)
            },
            Secciones = { sec }
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
        MensajeError = string.Empty;

        try
        {
            var (filas, total) =
                await _servicio.ObtenerRankingAsync();

            _filasOriginales.Clear();
            _filasOriginales.AddRange(filas);

            _totalGeneral = total;
            _totalDocumentos = filas.Sum(f => f.Documentos);

            _totalClientes = filas
                .Select(f => f.RutCliente + "|" + f.NombreCliente)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            TotalGeneralTexto = Formato.Moneda(_totalGeneral);

            OnPropertyChanged(nameof(TotalDocumentosTexto));
            OnPropertyChanged(nameof(TotalClientesTexto));
            OnPropertyChanged(nameof(CantidadClientesTexto));

            AplicarFiltro();
        }
        catch (Exception ex)
        {
            MensajeError =
                "No se pudo generar el reporte: " + ex.Message;
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private void AplicarFiltro()
    {
        Filas.Clear();

        string filtro = (Busqueda ?? string.Empty).Trim();

        var resultado = string.IsNullOrWhiteSpace(filtro)
            ? _filasOriginales
            : _filasOriginales.Where(f =>
                (f.NombreCliente ?? string.Empty).Contains(
                    filtro, StringComparison.OrdinalIgnoreCase)
                ||
                (f.RutCliente ?? string.Empty).Contains(
                    filtro, StringComparison.OrdinalIgnoreCase)
                ||
                (f.NombreConvenio ?? string.Empty).Contains(
                    filtro, StringComparison.OrdinalIgnoreCase)
            ).ToList();

        foreach (var fila in resultado)
            Filas.Add(fila);

        OnPropertyChanged(nameof(TotalFilasTexto));
        OnPropertyChanged(nameof(HayDatos));
        OnPropertyChanged(nameof(NoHayDatos));
    }
}
