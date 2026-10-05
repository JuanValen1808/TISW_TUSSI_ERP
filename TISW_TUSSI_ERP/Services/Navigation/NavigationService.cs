namespace TISW_TUSSI_ERP.Services.Navigation;

// Centraliza la navegación entre páginas para no repetir
// Shell.Current.GoToAsync(...) en cada ViewModel.
public class NavigationService
{
    public Task IrA(string ruta) => Shell.Current.GoToAsync(ruta);

    public Task IrAConParametros(string ruta, IDictionary<string, object> parametros)
        => Shell.Current.GoToAsync(ruta, parametros);

    public Task VolverAtras() => Shell.Current.GoToAsync("..");
}
