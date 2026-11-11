using TISW_TUSSI_ERP.ViewModels.Compras;

namespace TISW_TUSSI_ERP.Views.Compras;

public partial class RecepcionFacturasPage : ContentPage, IQueryAttributable
{
    private readonly RecepcionFacturasViewModel _viewModel;
    private int? _idOrdenInicial;

    public RecepcionFacturasPage(RecepcionFacturasViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("idOrden", out var val) && val is string str && int.TryParse(str, out int id))
        {
            _idOrdenInicial = id;
        }
        else if (query.TryGetValue("idOrden", out var valInt) && valInt is int idInt)
        {
            _idOrdenInicial = idInt;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.CargarOrdenesDisponiblesAsync(_idOrdenInicial);
        _idOrdenInicial = null;
    }
}

