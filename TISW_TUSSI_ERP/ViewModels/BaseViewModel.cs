using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TISW_TUSSI_ERP.ViewModels;

// Clase base para todos los ViewModels del ERP (Auth, Dashboard, Ventas, etc.)
public class BaseViewModel : INotifyPropertyChanged
{
    private bool _estaOcupado;
    public bool EstaOcupado
    {
        get => _estaOcupado;
        set => SetProperty(ref _estaOcupado, value);
    }

    private string _titulo = string.Empty;
    public string Titulo
    {
        get => _titulo;
        set => SetProperty(ref _titulo, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T campo, T valor, [CallerMemberName] string? nombrePropiedad = null)
    {
        if (EqualityComparer<T>.Default.Equals(campo, valor))
            return false;

        campo = valor;
        OnPropertyChanged(nombrePropiedad);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? nombrePropiedad = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombrePropiedad));
}
