using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows.Input;
using TISW_TUSSI_ERP.Models.Compras;
using TISW_TUSSI_ERP.Services.Api;

namespace TISW_TUSSI_ERP.ViewModels.Compras;

// Mantenedor CRUD de Proveedores (RF-COM-01)
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
    public string Rut { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string Contacto { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CondicionesComerciales { get; set; } = string.Empty;

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

    // Validaciones de operación (RNF transversal): RUT chileno, email y campos obligatorios
    private static readonly Regex RegexRut = new(@"^\d{1,2}\.?\d{3}\.?\d{3}-[\dkK]$", RegexOptions.Compiled);
    private static readonly Regex RegexEmail = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public ProveedoresViewModel(ComprasService servicio)
    {
        _servicio = servicio;
        Titulo = "Proveedores";

        ComandoNuevo = new Command(AbrirNuevo);
        ComandoEditar = new Command<Proveedor>(AbrirEdicion);
        ComandoEliminar = new Command<Proveedor>(async p => await EliminarAsync(p));
        ComandoGuardar = new Command(async () => await GuardarAsync());
        ComandoCancelar = new Command(() => FormularioVisible = false);
    }

    public async Task CargarAsync()
    {
        if (EstaOcupado) return;
        EstaOcupado = true;
        MensajeError = string.Empty;
        try
        {
            _todos = await _servicio.ObtenerProveedoresAsync();
            AplicarFiltro();
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
        _idEnEdicion = 0;
        Rut = RazonSocial = Contacto = Telefono = Email = CondicionesComerciales = string.Empty;
        NotificarCamposFormulario();
        EsEdicion = false;
        ErrorFormulario = string.Empty;
        FormularioVisible = true;
    }

    private void AbrirEdicion(Proveedor? p)
    {
        if (p is null) return;
        _idEnEdicion = p.Id;
        Rut = p.Rut; RazonSocial = p.RazonSocial; Contacto = p.Contacto;
        Telefono = p.Telefono; Email = p.Email; CondicionesComerciales = p.CondicionesComerciales;
        NotificarCamposFormulario();
        EsEdicion = true;
        ErrorFormulario = string.Empty;
        FormularioVisible = true;
    }

    private void NotificarCamposFormulario()
    {
        OnPropertyChanged(nameof(Rut));
        OnPropertyChanged(nameof(RazonSocial));
        OnPropertyChanged(nameof(Contacto));
        OnPropertyChanged(nameof(Telefono));
        OnPropertyChanged(nameof(Email));
        OnPropertyChanged(nameof(CondicionesComerciales));
        OnPropertyChanged(nameof(TituloFormulario));
    }

    private async Task GuardarAsync()
    {
        // ---- Validaciones de operación del sistema ----
        if (string.IsNullOrWhiteSpace(RazonSocial))
        {
            ErrorFormulario = "La razón social es obligatoria.";
            return;
        }
        if (string.IsNullOrWhiteSpace(Rut) || !RegexRut.IsMatch(Rut.Trim()))
        {
            ErrorFormulario = "Ingresa un RUT válido (ej: 76.123.456-7).";
            return;
        }
        if (!string.IsNullOrWhiteSpace(Email) && !RegexEmail.IsMatch(Email.Trim()))
        {
            ErrorFormulario = "El correo electrónico no tiene un formato válido.";
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
                Rut = Rut,
                RazonSocial = RazonSocial,
                Contacto = Contacto,
                Telefono = Telefono,
                Email = Email,
                CondicionesComerciales = CondicionesComerciales
            };

            if (_idEnEdicion == 0)
                await _servicio.CrearProveedorAsync(proveedor);
            else
                await _servicio.ActualizarProveedorAsync(proveedor);

            FormularioVisible = false;
            await CargarAsync();
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
        if (p is null) return;
        var confirmar = await Shell.Current.DisplayAlert(
            "Eliminar proveedor", $"¿Eliminar a \"{p.RazonSocial}\"? Esta acción no se puede deshacer.",
            "Eliminar", "Cancelar");
        if (!confirmar) return;

        try
        {
            await _servicio.EliminarProveedorAsync(p.Id);
            await CargarAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("No se pudo eliminar", ex.Message, "OK");
        }
    }
}
