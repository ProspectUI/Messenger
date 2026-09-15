using CommunityToolkit.Mvvm.ComponentModel;
using Messenger.Client.Localization;
using Messenger.Shared.Dtos;

namespace Messenger.Client.ViewModels;

public partial class ChatItemViewModel : ObservableObject
{
    public Guid Id { get; }

    [ObservableProperty] private string _otherUserName = string.Empty;
    [ObservableProperty] private string? _lastMessageText;
    [ObservableProperty] private DateTime? _lastMessageAt;
    [ObservableProperty] private bool _isOnline;

    public string TimeDisplay => LastMessageAt.HasValue
        ? LastMessageAt.Value.ToLocalTime().ToString("HH:mm")
        : string.Empty;

    public string OnlineStatusText => IsOnline
        ? LocalizationService.Instance.OnlineText
        : LocalizationService.Instance.OfflineText;

    public ChatItemViewModel(ChatDto dto)
    {
        Id = dto.Id;
        _otherUserName = dto.OtherUserName;
        _lastMessageText = dto.LastMessageText;
        _lastMessageAt = dto.LastMessageAt;

        LocalizationService.Instance.PropertyChanged += (_, _) =>
            OnPropertyChanged(nameof(OnlineStatusText));
    }

    partial void OnLastMessageAtChanged(DateTime? value) =>
        OnPropertyChanged(nameof(TimeDisplay));

    partial void OnIsOnlineChanged(bool value) =>
        OnPropertyChanged(nameof(OnlineStatusText));
}
