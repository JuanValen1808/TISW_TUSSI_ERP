using TISW_TUSSI_ERP.ViewModels.Dashboard;

namespace TISW_TUSSI_ERP.Views.Dashboard;

public partial class DashboardPage : ContentPage
{
    public DashboardPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
