using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Messenger.Client.Localization;

public enum AppLanguage { Ukrainian, English }

public partial class LocalizationService : ObservableObject
{
    public static LocalizationService Instance { get; } = new();
    private LocalizationService() { }

    [ObservableProperty] private AppLanguage _language = AppLanguage.Ukrainian;

    partial void OnLanguageChanged(AppLanguage value) => OnPropertyChanged(string.Empty);

    private bool IsUk => Language == AppLanguage.Ukrainian;

    // Login window
    public string AppSubtitle        => IsUk ? "Увійдіть або створіть акаунт"          : "Sign in or create an account";
    public string UsernameLabel      => IsUk ? "Ім'я користувача"                      : "Username";
    public string PasswordLabel      => IsUk ? "Пароль"                                : "Password";
    public string ShowPassword       => IsUk ? "Показати"                              : "Show";
    public string HidePassword       => IsUk ? "Сховати"                               : "Hide";
    public string LoginButton        => IsUk ? "Увійти"                                : "Login";
    public string RegisterButton     => IsUk ? "Реєстрація"                            : "Register";

    // Main window
    public string NewChatTitle       => IsUk ? "Новий чат"                             : "New Chat";
    public string SearchPlaceholder  => IsUk ? "Пошук за ніком..."                     : "Search by username...";
    public string SearchHint         => IsUk ? "Введіть ім'я користувача для пошуку"   : "Enter a username to search";
    public string NoUsersFound       => IsUk ? "Користувачів не знайдено"              : "No users found";
    public string SelectChatHint     => IsUk ? "Оберіть чат або почніть новий"         : "Select a chat or start a new one";
    public string MessagePlaceholder => IsUk ? "Написати повідомлення..."               : "Write a message...";
    public string OnlineText         => IsUk ? "онлайн"                                : "online";
    public string OfflineText        => IsUk ? "не в мережі"                           : "offline";
    public string SearchTooltip      => IsUk ? "Пошук користувачів"                    : "Search users";
    public string LogoutTooltip      => IsUk ? "Вийти з акаунту"                       : "Sign out";

    // Errors
    public string ErrorEnterCredentials   => IsUk ? "Введіть ім'я користувача та пароль." : "Enter username and password.";
    public string ErrorInvalidCredentials => IsUk ? "Невірний логін або пароль."           : "Invalid username or password.";
    public string ErrorConnectionFailed   => IsUk ? "Помилка з'єднання з сервером."        : "Connection error.";
    public string ErrorUsernameTaken      => IsUk ? "Ім'я користувача вже зайнято."        : "Username already taken.";

    [RelayCommand]
    private void SetLanguage(string lang) =>
        Language = lang == "EN" ? AppLanguage.English : AppLanguage.Ukrainian;
}
