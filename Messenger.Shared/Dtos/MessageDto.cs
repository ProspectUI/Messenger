namespace Messenger.Shared.Dtos;

public class MessageDto
{
    public Guid Id { get; set; }
    public Guid ChatId { get; set; }
    public Guid SenderId { get; set; }
    public string SenderUsername { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}
