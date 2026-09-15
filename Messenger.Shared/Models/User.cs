namespace Messenger.Shared.Models;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public ICollection<ChatMember> ChatMembers { get; set; } = new List<ChatMember>();
}
