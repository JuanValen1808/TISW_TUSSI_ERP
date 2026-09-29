using TISW_TUSSI_ERP.Helpers;
using TISW_TUSSI_ERP.Models.Auth;

namespace TISW_TUSSI_ERP.Views.Shared;

public partial class PageHeader : ContentView
{
    public static readonly BindableProperty TituloProperty =
        BindableProperty.Create(nameof(Titulo), typeof(string), typeof(PageHeader), string.Empty);
    public static readonly BindableProperty RutaProperty =
        BindableProperty.Create(nameof(Ruta), typeof(string), typeof(PageHeader), string.Empty);

    public string Titulo { get => (string)GetValue(TituloProperty); set => SetValue(TituloProperty, value); }
    public string Ruta { get => (string)GetValue(RutaProperty); set => SetValue(RutaProperty, value); }

    public PageHeader()
    {
        InitializeComponent();
        Raiz.BindingContext = this;

        var u = Sesion.UsuarioActual;
        NombreLabel.Text = u?.NombreCompleto ?? "Invitado";
        RolLabel.Text = u?.NombreRol ?? "";
        InicialesLabel.Text = Formato.Iniciales(u?.NombreCompleto ?? "");
    }
}
