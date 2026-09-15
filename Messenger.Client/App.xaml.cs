using System.Windows;
using Messenger.Client.Services;
using Messenger.Client.ViewModels;
using Messenger.Client.Windows;

namespace Messenger.Client;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var api = new ApiService();
        ShowLoginWindow(api);
    }

    private void ShowLoginWindow(ApiService api)
    {
        var vm = new LoginViewModel(api);
        var loginWindow = new LoginWindow { DataContext = vm };
        vm.LoginSucceeded += (token, username) =>
        {
            var hub = new HubService();
            var mainVm = new MainViewModel(api, hub, token, username);
            var mainWindow = new MainWindow(mainVm);
            mainVm.LogoutRequested += () =>
            {
                ShowLoginWindow(api);
                mainWindow.Close();
            };
            mainWindow.Show();
            loginWindow.Close();
        };
        loginWindow.Show();
    }
}
