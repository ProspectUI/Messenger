using Messenger.Shared.Dtos;

namespace Messenger.Client.ViewModels;

public class MessageViewModel
{
    public string SenderUsername { get; }
    public string Text { get; }
    public string TimeDisplay { get; }
    public bool IsOwn { get; }

    public MessageViewModel(MessageDto dto, string currentUsername)
    {
        SenderUsername = dto.SenderUsername;
        Text = dto.Text;
        TimeDisplay = dto.SentAt.ToLocalTime().ToString("HH:mm");
        IsOwn = dto.SenderUsername == currentUsername;
    }
}
