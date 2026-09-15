using CommunityToolkit.Mvvm.ComponentModel;
using Messenger.Client.Localization;
using Messenger.Client.Services;

namespace Messenger.Client.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly ApiService _api;

    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isBusy;

    public event Action<string, string>? LoginSucceeded;

    public LoginViewModel(ApiService api) => _api = api;

    public async Task LoginAsync(string password)
    {
        var loc = LocalizationService.Instance;
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(password))
        { ErrorMessage = loc.ErrorEnterCredentials; return; }
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var token = await _api.LoginAsync(Username.Trim(), password);
            if (token is null) ErrorMessage = loc.ErrorInvalidCredentials;
            else LoginSucceeded?.Invoke(token, Username.Trim());
        }
        catch { ErrorMessage = loc.ErrorConnectionFailed; }
        finally { IsBusy = false; }
    }

    public async Task RegisterAsync(string password)
    {
        var loc = LocalizationService.Instance;
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(password))
        { ErrorMessage = loc.ErrorEnterCredentials; return; }
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var status = await _api.RegisterAsync(Username.Trim(), password);
            if (status == System.Net.HttpStatusCode.Conflict)
            { ErrorMessage = loc.ErrorUsernameTaken; return; }
            if ((int)status < 200 || (int)status >= 300)
            { ErrorMessage = loc.ErrorConnectionFailed; return; }
            var token = await _api.LoginAsync(Username.Trim(), password);
            if (token is not null) LoginSucceeded?.Invoke(token, Username.Trim());
        }
        catch { ErrorMessage = loc.ErrorConnectionFailed; }
        finally { IsBusy = false; }
    }
}
