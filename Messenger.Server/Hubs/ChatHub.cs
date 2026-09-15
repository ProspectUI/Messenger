using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Messenger.Server.Data;
using Messenger.Shared.Dtos;
using Messenger.Shared.Models;

namespace Messenger.Server.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private static readonly ConcurrentDictionary<Guid, string> OnlineUsers = new();

    private readonly AppDbContext _db;

    public ChatHub(AppDbContext db)
    {
        _db = db;
    }

    private Guid GetUserId() =>
        Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        OnlineUsers[userId] = Context.ConnectionId;

        var chatIds = await _db.ChatMembers
            .Where(cm => cm.UserId == userId)
            .Select(cm => cm.ChatId)
            .ToListAsync();

        foreach (var chatId in chatIds)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"chat-{chatId}");

        await Clients.All.SendAsync("UserListChanged", OnlineUsers.Keys);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        OnlineUsers.TryRemove(userId, out _);
        await Clients.All.SendAsync("UserListChanged", OnlineUsers.Keys);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinChat(Guid chatId)
    {
        var userId = GetUserId();
        var isMember = await _db.ChatMembers.AnyAsync(cm => cm.ChatId == chatId && cm.UserId == userId);
        if (!isMember)
            throw new HubException("Forbidden");

        await Groups.AddToGroupAsync(Context.ConnectionId, $"chat-{chatId}");
    }

    public async Task SendMessage(Guid chatId, string text)
    {
        var userId = GetUserId();
        var isMember = await _db.ChatMembers.AnyAsync(cm => cm.ChatId == chatId && cm.UserId == userId);
        if (!isMember)
            throw new HubException("Forbidden");

        var message = new Message
        {
            Id = Guid.NewGuid(),
            ChatId = chatId,
            SenderId = userId,
            SenderUsername = Context.User!.Identity!.Name!,
            Text = text,
            SentAt = DateTime.UtcNow
        };
        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        var dto = new MessageDto
        {
            Id = message.Id,
            ChatId = message.ChatId,
            SenderId = message.SenderId,
            SenderUsername = message.SenderUsername,
            Text = message.Text,
            SentAt = message.SentAt
        };

        await Clients.Group($"chat-{chatId}").SendAsync("ReceiveMessage", dto);
    }

    public async Task<IEnumerable<MessageDto>> GetHistory(Guid chatId)
    {
        var userId = GetUserId();
        var isMember = await _db.ChatMembers.AnyAsync(cm => cm.ChatId == chatId && cm.UserId == userId);
        if (!isMember)
            throw new HubException("Forbidden");

        return await _db.Messages
            .Where(m => m.ChatId == chatId)
            .OrderByDescending(m => m.SentAt)
            .Take(50)
            .Select(m => new MessageDto
            {
                Id = m.Id,
                ChatId = m.ChatId,
                SenderId = m.SenderId,
                SenderUsername = m.SenderUsername,
                Text = m.Text,
                SentAt = m.SentAt
            })
            .ToListAsync();
    }
}
