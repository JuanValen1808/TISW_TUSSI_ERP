using TISW_TUSSI_ERP.ViewModels.Auth;

namespace TISW_TUSSI_ERP.Views.Auth;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
