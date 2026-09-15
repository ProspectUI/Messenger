using System.Windows;
using System.Windows.Controls;
using Messenger.Client.Localization;
using Messenger.Client.ViewModels;

namespace Messenger.Client.Windows;

public partial class LoginWindow : Window
{
    private LoginViewModel Vm => (LoginViewModel)DataContext;
    private bool _passwordVisible;

    public LoginWindow()
    {
        InitializeComponent();
        LocalizationService.Instance.PropertyChanged += (_, _) => UpdateToggleButtonText();
    }

    private string GetPassword() =>
        _passwordVisible ? PasswordTextBox.Text : PasswordBox.Password;

    private void UpdateToggleButtonText() =>
        TogglePasswordBtn.Content = _passwordVisible
            ? LocalizationService.Instance.HidePassword
            : LocalizationService.Instance.ShowPassword;

    private void TogglePasswordBtn_Click(object sender, RoutedEventArgs e)
    {
        _passwordVisible = !_passwordVisible;
        if (_passwordVisible)
        {
            PasswordTextBox.Text = PasswordBox.Password;
            PasswordBox.Visibility = Visibility.Collapsed;
            PasswordTextBox.Visibility = Visibility.Visible;
        }
        else
        {
            PasswordBox.Password = PasswordTextBox.Text;
            PasswordTextBox.Visibility = Visibility.Collapsed;
            PasswordBox.Visibility = Visibility.Visible;
        }
        UpdateToggleButtonText();
    }

    private async void LoginBtn_Click(object sender, RoutedEventArgs e) =>
        await Vm.LoginAsync(GetPassword());

    private async void RegisterBtn_Click(object sender, RoutedEventArgs e) =>
        await Vm.RegisterAsync(GetPassword());
}
