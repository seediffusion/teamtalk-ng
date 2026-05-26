using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.ViewModels;

public sealed class ChatMessageViewModel
{
    public ChatMessageViewModel(ChatMessage message)
    {
        Time = message.Timestamp.ToLocalTime().ToString("HH:mm:ss");
        Sender = message.IsDirect && !message.Sender.StartsWith("Direct", StringComparison.OrdinalIgnoreCase)
            ? $"Direct {message.Sender}"
            : message.Sender;
        Text = message.Text;
        IsDirect = message.IsDirect;
    }

    public string Time { get; }

    public string Sender { get; }

    public string Text { get; }

    public bool IsDirect { get; }

    public string AccessibleName => IsDirect
        ? $"{Time}, direct message, {Sender}: {Text}"
        : $"{Time}, {Sender}: {Text}";

    public override string ToString()
    {
        return AccessibleName;
    }
}
