using Microsoft.AspNetCore.SignalR.Client;
using Messenger.Shared.Dtos;

namespace Messenger.Client.Services;

public class HubService
{
    private HubConnection? _connection;
    private const string HubUrl = "http://localhost:5087/chathub";

    public event Action<MessageDto>? MessageReceived;
    public event Action<IEnumerable<Guid>>? UserListChanged;

    public async Task ConnectAsync(string token)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(HubUrl, o => o.AccessTokenProvider = () => Task.FromResult<string?>(token))
            .WithAutomaticReconnect()
            .Build();

        _connection.On<MessageDto>("ReceiveMessage", dto => MessageReceived?.Invoke(dto));
        _connection.On<IEnumerable<Guid>>("UserListChanged", ids => UserListChanged?.Invoke(ids));

        await _connection.StartAsync();
    }

    public async Task DisconnectAsync()
    {
        if (_connection is not null)
            await _connection.StopAsync();
    }   

    public async Task JoinChatAsync(Guid chatId)
    {
        if (_connection is null) return;
        await _connection.InvokeAsync("JoinChat", chatId);
    }

    public async Task SendMessageAsync(Guid chatId, string text)
    {
        if (_connection is null) return;
        await _connection.InvokeAsync("SendMessage", chatId, text);
    }

    public async Task<List<MessageDto>> GetHistoryAsync(Guid chatId)
    {
        if (_connection is null) return [];
        var result = await _connection.InvokeAsync<IEnumerable<MessageDto>>("GetHistory", chatId);
        return result?.ToList() ?? [];
    }
}
