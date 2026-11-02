
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TISW_TUSSI_ERP.Models.Ventas;

public class ConvenioSalud : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotificarCambio(
        [CallerMemberName] string? propiedad = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propiedad));
    }

    private bool Cambiar<T>(
        ref T campo,
        T valor,
        [CallerMemberName] string? propiedad = null)
    {
        if (EqualityComparer<T>.Default.Equals(campo, valor))
            return false;

        campo = valor;
        NotificarCambio(propiedad);
        return true;
    }

    private int _idConvenio;
    public int IdConvenio
    {
        get => _idConvenio;
        set => Cambiar(ref _idConvenio, value);
    }

    private int _idCliente;
    public int IdCliente
    {
        get => _idCliente;
        set => Cambiar(ref _idCliente, value);
    }

    private string _nombreConvenio = string.Empty;
    public string NombreConvenio
    {
        get => _nombreConvenio;
        set => Cambiar(ref _nombreConvenio, value);
    }

    private decimal _porcentajeDescuento;
    public decimal PorcentajeDescuento
    {
        get => _porcentajeDescuento;
        set => Cambiar(ref _porcentajeDescuento, value);
    }

    private decimal _limiteCredito;
    public decimal LimiteCredito
    {
        get => _limiteCredito;
        set => Cambiar(ref _limiteCredito, value);
    }

    private int _diasCreditoMorosidad;
    public int DiasCreditoMorosidad
    {
        get => _diasCreditoMorosidad;
        set => Cambiar(ref _diasCreditoMorosidad, value);
    }

    private bool _activo = true;
    public bool Activo
    {
        get => _activo;
        set
        {
            if (Cambiar(ref _activo, value))
            {
                NotificarCambio(nameof(EstadoTexto));
                NotificarCambio(nameof(EstadoColor));
            }
        }
    }

    public string EstadoTexto =>
        Activo ? "Activo" : "Inactivo";

    public Color EstadoColor =>
        Activo
            ? Color.FromArgb("#15803D")
            : Color.FromArgb("#DC2626");
}
