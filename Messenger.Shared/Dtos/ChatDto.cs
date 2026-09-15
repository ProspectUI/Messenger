namespace Messenger.Shared.Dtos;

public class ChatDto
{
    public Guid Id { get; set; }
    public string OtherUserName { get; set; } = string.Empty;
    public string? LastMessageText { get; set; }
    public DateTime? LastMessageAt { get; set; }
}
