using TISW_TUSSI_ERP.ViewModels.Dashboard;

namespace TISW_TUSSI_ERP.Views.Dashboard;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _vm;
    private readonly DonutDrawable _donut = new();

    public DashboardPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _vm = viewModel;

        Donut.Drawable = _donut;
        _vm.DatosActualizados += () =>
        {
            _donut.Segmentos = _vm.MediosPago.ToList();
            Donut.Invalidate();
        };
    }

    // Cada vez que se entra al Dashboard se leen los datos frescos desde MySQL
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CargarAsync();
    }
}
