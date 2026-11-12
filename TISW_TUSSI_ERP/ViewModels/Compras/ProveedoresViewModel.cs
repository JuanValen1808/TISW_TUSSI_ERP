using System.Collections.ObjectModel;
using System.Windows.Input;
using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Compras;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Compras;

// Mantenedor de Proveedores y Laboratorios
public class ProveedoresViewModel : BaseViewModel
{
    private readonly ComprasService _servicio;
    private List<Proveedor> _todos = new();

    public ObservableCollection<Proveedor> Proveedores { get; } = new();

    private string _busqueda = string.Empty;
    public string Busqueda
    {
        get => _busqueda;
        set { if (SetProperty(ref _busqueda, value)) AplicarFiltro(); }
    }

    // ---- Formulario (alta / edición) ----
    private bool _formularioVisible;
    public bool FormularioVisible { get => _formularioVisible; set => SetProperty(ref _formularioVisible, value); }

    private bool _esEdicion;
    public bool EsEdicion { get => _esEdicion; set => SetProperty(ref _esEdicion, value); }
    public string TituloFormulario => EsEdicion ? "Editar proveedor" : "Nuevo proveedor";

    private int _idEnEdicion;
    private string _rut = string.Empty;
    public string Rut { get => _rut; set => SetProperty(ref _rut, value); }

    private string _razonSocial = string.Empty;
    public string RazonSocial { get => _razonSocial; set => SetProperty(ref _razonSocial, value); }

    private string _contacto = string.Empty;
    public string Contacto { get => _contacto; set => SetProperty(ref _contacto, value); }

    private string _telefono = string.Empty;
    public string Telefono { get => _telefono; set => SetProperty(ref _telefono, value); }

    private string _email = string.Empty;
    public string Email { get => _email; set => SetProperty(ref _email, value); }

    private string _condicionesComerciales = string.Empty;
    public string CondicionesComerciales { get => _condicionesComerciales; set => SetProperty(ref _condicionesComerciales, value); }

    public bool NoEstaOcupado => !EstaOcupado;

    private string _errorFormulario = string.Empty;
    public string ErrorFormulario
    {
        get => _errorFormulario;
        set { if (SetProperty(ref _errorFormulario, value)) OnPropertyChanged(nameof(HayErrorFormulario)); }
    }
    public bool HayErrorFormulario => !string.IsNullOrEmpty(ErrorFormulario);

    private string _mensajeError = string.Empty;
    public string MensajeError
    {
        get => _mensajeError;
        private set { if (SetProperty(ref _mensajeError, value)) OnPropertyChanged(nameof(HayError)); }
    }
    public bool HayError => !string.IsNullOrEmpty(MensajeError);

    public ICommand ComandoNuevo { get; }
    public ICommand ComandoEditar { get; }
    public ICommand ComandoEliminar { get; }
    public ICommand ComandoGuardar { get; }
    public ICommand ComandoCancelar { get; }

    public ProveedoresViewModel(ComprasService servicio)
    {
        _servicio = servicio;
        Titulo = "Proveedores";

        ComandoNuevo = new Command(AbrirNuevo, () => NoEstaOcupado);
        ComandoEditar = new Command<Proveedor>(AbrirEdicion, _ => NoEstaOcupado);
        ComandoEliminar = new Command<Proveedor>(async p => await EliminarAsync(p), _ => NoEstaOcupado);
        ComandoGuardar = new Command(async () => await GuardarAsync(), () => NoEstaOcupado);
        ComandoCancelar = new Command(() => { if (!EstaOcupado) FormularioVisible = false; }, () => NoEstaOcupado);

        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(EstaOcupado))
            {
                OnPropertyChanged(nameof(NoEstaOcupado));
                (ComandoNuevo as Command)?.ChangeCanExecute();
                (ComandoEditar as Command)?.ChangeCanExecute();
                (ComandoEliminar as Command)?.ChangeCanExecute();
                (ComandoGuardar as Command)?.ChangeCanExecute();
                (ComandoCancelar as Command)?.ChangeCanExecute();
            }
        };
    }

    public async Task CargarAsync()
    {
        if (EstaOcupado) return;
        EstaOcupado = true;
        MensajeError = string.Empty;
        try
        {
            await CargarInternoAsync();
        }
        catch (Exception ex)
        {
            MensajeError = "No se pudieron cargar los proveedores: " + ex.Message;
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private async Task CargarInternoAsync()
    {
        MensajeError = string.Empty;
        _todos = await _servicio.ObtenerProveedoresAsync();
        AplicarFiltro();
    }

    private void AplicarFiltro()
    {
        var texto = (Busqueda ?? "").Trim().ToLowerInvariant();
        var filtrados = string.IsNullOrEmpty(texto)
            ? _todos
            : _todos.Where(p => p.RazonSocial.ToLowerInvariant().Contains(texto)
                              || p.Rut.ToLowerInvariant().Contains(texto)).ToList();

        Proveedores.Clear();
        foreach (var p in filtrados) Proveedores.Add(p);
    }

    private void AbrirNuevo()
    {
        if (EstaOcupado) return;
        _idEnEdicion = 0;
        Rut = RazonSocial = Contacto = Telefono = Email = CondicionesComerciales = string.Empty;
        OnPropertyChanged(nameof(TituloFormulario));
        EsEdicion = false;
        OnPropertyChanged(nameof(TituloFormulario));
        ErrorFormulario = string.Empty;
        FormularioVisible = true;
    }

    private void AbrirEdicion(Proveedor? p)
    {
        if (EstaOcupado || p is null) return;
        _idEnEdicion = p.Id;
        Rut = p.Rut;
        RazonSocial = p.RazonSocial;
        Contacto = p.Contacto;
        Telefono = p.Telefono;
        Email = p.Email;
        CondicionesComerciales = p.CondicionesComerciales;
        EsEdicion = true;
        OnPropertyChanged(nameof(TituloFormulario));
        ErrorFormulario = string.Empty;
        FormularioVisible = true;
    }

    private async Task GuardarAsync()
    {
        if (EstaOcupado) return;

        // ---- Validaciones de operación del sistema ----
        if (string.IsNullOrWhiteSpace(RazonSocial))
        {
            ErrorFormulario = "La razón social es obligatoria.";
            return;
        }

        if (!Validaciones.ValidarRut(Rut, out string rutLimpio, out string errorRut))
        {
            ErrorFormulario = errorRut;
            return;
        }
        Rut = rutLimpio;

        if (!string.IsNullOrWhiteSpace(Email) && !Validaciones.ValidarEmail(Email, out string errorEmail))
        {
            ErrorFormulario = errorEmail;
            return;
        }

        EstaOcupado = true;
        ErrorFormulario = string.Empty;
        try
        {
            if (await _servicio.ExisteRutAsync(Rut.Trim(), _idEnEdicion == 0 ? null : _idEnEdicion))
            {
                ErrorFormulario = "Ya existe un proveedor registrado con ese RUT.";
                return;
            }

            var proveedor = new Proveedor
            {
                Id = _idEnEdicion,
                Rut = Rut.Trim(),
                RazonSocial = RazonSocial.Trim(),
                Contacto = (Contacto ?? string.Empty).Trim(),
                Telefono = (Telefono ?? string.Empty).Trim(),
                Email = (Email ?? string.Empty).Trim(),
                CondicionesComerciales = (CondicionesComerciales ?? string.Empty).Trim()
            };

            bool esNuevo = _idEnEdicion == 0;
            if (esNuevo)
                await _servicio.CrearProveedorAsync(proveedor);
            else
                await _servicio.ActualizarProveedorAsync(proveedor);

            FormularioVisible = false;
            if (esNuevo && !string.IsNullOrWhiteSpace(Busqueda))
                Busqueda = string.Empty;

            await CargarInternoAsync();
        }
        catch (Exception ex)
        {
            ErrorFormulario = "No se pudo guardar: " + ex.Message;
        }
        finally
        {
            EstaOcupado = false;
        }
    }

    private async Task EliminarAsync(Proveedor? p)
    {
        if (EstaOcupado || p is null) return;
        EstaOcupado = true;
        try
        {
            var confirmar = await Shell.Current.DisplayAlert(
                "Eliminar proveedor", $"¿Eliminar a \"{p.RazonSocial}\"? Esta acción no se puede deshacer.",
                "Eliminar", "Cancelar");
            if (!confirmar) return;

            await _servicio.EliminarProveedorAsync(p.Id);
            await CargarInternoAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("No se pudo eliminar", ex.Message, "OK");
        }
        finally
        {
            EstaOcupado = false;
        }
    }
}
