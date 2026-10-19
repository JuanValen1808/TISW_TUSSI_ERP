using System.ComponentModel;
using System.Runtime.CompilerServices;
using TISW_TUSSI_ERP.Helpers;

namespace TISW_TUSSI_ERP.Models.Seguridad;

public class UsuarioModel : INotifyPropertyChanged
{
    private int _idUsuario;
    private string _rutUsuario = string.Empty;
    private string _nombre = string.Empty;
    private string _email = string.Empty;
    private string _passwordHash = string.Empty;
    private int _idRol;
    private string _nombreRol = string.Empty;
    private bool _activo = true;
    private DateTime _createdAt = DateTime.Now;

    public int IdUsuario
    {
        get => _idUsuario;
        set => SetField(ref _idUsuario, value);
    }

    public string RutUsuario
    {
        get => _rutUsuario;
        set => SetField(ref _rutUsuario, value);
    }

    public string Nombre
    {
        get => _nombre;
        set
        {
            if (SetField(ref _nombre, value))
            {
                OnPropertyChanged(nameof(Iniciales));
            }
        }
    }

    public string Email
    {
        get => _email;
        set => SetField(ref _email, value);
    }

    public string PasswordHash
    {
        get => _passwordHash;
        set => SetField(ref _passwordHash, value);
    }

    public int IdRol
    {
        get => _idRol;
        set => SetField(ref _idRol, value);
    }

    public string NombreRol
    {
        get => _nombreRol;
        set => SetField(ref _nombreRol, value);
    }

    public bool Activo
    {
        get => _activo;
        set
        {
            if (SetField(ref _activo, value))
            {
                OnPropertyChanged(nameof(EstadoTexto));
                OnPropertyChanged(nameof(EstadoColor));
                OnPropertyChanged(nameof(EstadoFondo));
                OnPropertyChanged(nameof(BotonEstadoTexto));
            }
        }
    }

    public DateTime CreatedAt
    {
        get => _createdAt;
        set
        {
            if (SetField(ref _createdAt, value))
            {
                OnPropertyChanged(nameof(FechaRegistroTexto));
            }
        }
    }

    public string Iniciales => Formato.Iniciales(Nombre);
    public string EstadoTexto => Activo ? "Activo" : "Inactivo";
    public string BotonEstadoTexto => Activo ? "Desactivar" : "Activar";
    public Color EstadoColor => Activo ? Paleta.Exito : Paleta.Peligro;
    public Color EstadoFondo => Activo ? Color.FromArgb("#DCFCE7") : Color.FromArgb("#FEE2E2");
    public string FechaRegistroTexto => CreatedAt.ToString("dd/MM/yyyy HH:mm");

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class RolModel : INotifyPropertyChanged
{
    private int _idRol;
    private string _nombreRol = string.Empty;
    private string _descripcion = string.Empty;
    private int _cantidadUsuarios;
    private int _cantidadPermisos;

    public int IdRol
    {
        get => _idRol;
        set => SetField(ref _idRol, value);
    }

    public string NombreRol
    {
        get => _nombreRol;
        set => SetField(ref _nombreRol, value);
    }

    public string Descripcion
    {
        get => _descripcion;
        set => SetField(ref _descripcion, value);
    }

    public int CantidadUsuarios
    {
        get => _cantidadUsuarios;
        set
        {
            if (SetField(ref _cantidadUsuarios, value))
            {
                OnPropertyChanged(nameof(UsuariosTexto));
            }
        }
    }

    public int CantidadPermisos
    {
        get => _cantidadPermisos;
        set
        {
            if (SetField(ref _cantidadPermisos, value))
            {
                OnPropertyChanged(nameof(PermisosTexto));
            }
        }
    }

    private bool _esSeleccionado;
    public bool EsSeleccionado
    {
        get => _esSeleccionado;
        set
        {
            if (SetField(ref _esSeleccionado, value))
            {
                OnPropertyChanged(nameof(FondoSeleccionado));
                OnPropertyChanged(nameof(BordeSeleccionado));
                OnPropertyChanged(nameof(GrosorBordeSeleccionado));
            }
        }
    }

    public Color FondoSeleccionado => EsSeleccionado ? Color.FromArgb("#E8F1FD") : Colors.White;
    public Color BordeSeleccionado => EsSeleccionado ? Color.FromArgb("#1D6FD8") : Color.FromArgb("#E3E8EF");
    public double GrosorBordeSeleccionado => EsSeleccionado ? 2 : 1;

    public string UsuariosTexto => $"{CantidadUsuarios} usuario(s)";
    public string PermisosTexto => $"{CantidadPermisos} permiso(s)";

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class PermisoModel : INotifyPropertyChanged
{
    private int _idPermiso;
    private string _codigoPermiso = string.Empty;
    private string _modulo = string.Empty;
    private string _descripcion = string.Empty;
    private bool _asignado;

    public Action? OnAsignadoCambiado { get; set; }

    public int IdPermiso
    {
        get => _idPermiso;
        set => SetField(ref _idPermiso, value);
    }

    public string CodigoPermiso
    {
        get => _codigoPermiso;
        set => SetField(ref _codigoPermiso, value);
    }

    public string Modulo
    {
        get => _modulo;
        set => SetField(ref _modulo, value);
    }

    public string Descripcion
    {
        get => _descripcion;
        set => SetField(ref _descripcion, value);
    }

    public bool Asignado
    {
        get => _asignado;
        set
        {
            if (SetField(ref _asignado, value))
            {
                OnPropertyChanged(nameof(EstadoTexto));
                OnPropertyChanged(nameof(EstadoColor));
                OnAsignadoCambiado?.Invoke();
            }
        }
    }

    public string EstadoTexto => Asignado ? "Habilitado" : "Denegado";
    public Color EstadoColor => Asignado ? Paleta.Exito : Paleta.Gris;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class ModuloPermisosGrupo : INotifyPropertyChanged
{
    public string NombreModulo { get; set; } = string.Empty;
    public List<PermisoModel> Permisos { get; set; } = new();

    public int TotalPermisos => Permisos.Count;
    public int PermisosHabilitados => Permisos.Count(p => p.Asignado);
    public string ResumenHabilitados => $"{PermisosHabilitados} de {TotalPermisos} activos";

    public void NotificarCambios()
    {
        OnPropertyChanged(nameof(PermisosHabilitados));
        OnPropertyChanged(nameof(ResumenHabilitados));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
