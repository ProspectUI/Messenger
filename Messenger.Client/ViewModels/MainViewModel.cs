using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Messenger.Client.Services;
using Messenger.Shared.Dtos;

namespace Messenger.Client.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ApiService _api;
    private readonly HubService _hub;
    private readonly string _token;

    public string CurrentUsername { get; }
    public ObservableCollection<ChatItemViewModel> Chats { get; } = [];
    public ObservableCollection<UserDto> Users { get; }
    public ObservableCollection<MessageViewModel> Messages { get; } = [];

    public bool HasUserResults => Users.Count > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedChat))]
    private ChatItemViewModel? _selectedChat;

    [ObservableProperty] private string _messageInput = string.Empty;
    [ObservableProperty] private bool _isUsersPanelOpen;
    [ObservableProperty] private string _userSearchQuery = string.Empty;

    public bool HasSelectedChat => SelectedChat is not null;

    public event Action? ScrollToBottomRequested;
    public event Action? LogoutRequested;

    private HashSet<Guid> _onlineIds = [];
    private CancellationTokenSource? _searchCts;

    public MainViewModel(ApiService api, HubService hub, string token, string username)
    {
        Users = [];
        Users.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasUserResults));
        _api = api; _hub = hub; _token = token; CurrentUsername = username;
        _hub.MessageReceived += OnMessageReceived;
        _hub.UserListChanged += OnUserListChanged;
    }

    public async Task InitializeAsync()
    {
        await _hub.ConnectAsync(_token);
        await RefreshChatsAsync();
    }

    private async Task RefreshChatsAsync()
    {
        var list = await _api.GetChatsAsync(_token);
        App.Current.Dispatcher.Invoke(() =>
        {
            Chats.Clear();
            foreach (var c in list) Chats.Add(new ChatItemViewModel(c));
            SyncOnlineStatus();
        });
    }

    partial void OnUserSearchQueryChanged(string value)
    {
        _ = SearchUsersAsync(value);
    }

    private async Task SearchUsersAsync(string query)
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var cts = _searchCts;

        try
        {
            await Task.Delay(300, cts.Token);
            var list = await _api.GetUsersAsync(_token, query);
            if (cts.IsCancellationRequested) return;
            App.Current.Dispatcher.Invoke(() =>
            {
                Users.Clear();
                foreach (var u in list) Users.Add(u);
            });
        }
        catch (OperationCanceledException) { }
    }

    partial void OnSelectedChatChanged(ChatItemViewModel? value)
    {
        if (value is null) return;
        IsUsersPanelOpen = false;
        _ = LoadHistoryAsync(value);
    }

    partial void OnIsUsersPanelOpenChanged(bool value)
    {
        if (!value)
        {
            UserSearchQuery = string.Empty;
            Users.Clear();
        }
    }

    private async Task LoadHistoryAsync(ChatItemViewModel chat)
    {
        var history = await _hub.GetHistoryAsync(chat.Id);
        App.Current.Dispatcher.Invoke(() =>
        {
            Messages.Clear();
            foreach (var m in history.OrderBy(x => x.SentAt))
                Messages.Add(new MessageViewModel(m, CurrentUsername));
            ScrollToBottomRequested?.Invoke();
        });
    }

    [RelayCommand]
    private async Task OpenChatWithUserAsync(UserDto user)
    {
        var dto = await _api.CreatePrivateChatAsync(_token, user.Id);
        if (dto is null) return;

        var existing = Chats.FirstOrDefault(c => c.Id == dto.Id);
        if (existing is null)
        {
            existing = new ChatItemViewModel(dto);
            Chats.Insert(0, existing);
            await _hub.JoinChatAsync(dto.Id);
        }
        SelectedChat = existing;
        IsUsersPanelOpen = false;
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (SelectedChat is null || string.IsNullOrWhiteSpace(MessageInput)) return;
        var text = MessageInput.Trim();
        MessageInput = string.Empty;
        await _hub.SendMessageAsync(SelectedChat.Id, text);
    }

    [RelayCommand]
    private void ToggleUsersPanel() => IsUsersPanelOpen = !IsUsersPanelOpen;

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _hub.DisconnectAsync();
        LogoutRequested?.Invoke();
    }

    private void OnMessageReceived(MessageDto dto)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            if (SelectedChat?.Id == dto.ChatId)
            {
                Messages.Add(new MessageViewModel(dto, CurrentUsername));
                ScrollToBottomRequested?.Invoke();
            }
            var chat = Chats.FirstOrDefault(c => c.Id == dto.ChatId);
            if (chat is not null)
            {
                chat.LastMessageText = dto.Text;
                chat.LastMessageAt = dto.SentAt;
            }
        });
    }

    private void OnUserListChanged(IEnumerable<Guid> ids)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            _onlineIds = ids.ToHashSet();
            SyncOnlineStatus();
        });
    }

    private void SyncOnlineStatus()
    {
        foreach (var chat in Chats)
        {
            var user = Users.FirstOrDefault(u => u.Username == chat.OtherUserName);
            if (user is not null) chat.IsOnline = _onlineIds.Contains(user.Id);
        }
    }
}
