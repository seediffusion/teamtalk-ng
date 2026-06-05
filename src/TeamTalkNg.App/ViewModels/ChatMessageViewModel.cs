using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.ViewModels;

public sealed class ChatMessageViewModel
{
    public ChatMessageViewModel(ChatMessage message)
    {
        Time = message.Timestamp.ToLocalTime().ToString("HH:mm:ss");
        Sender = message.Sender;
        Text = message.Text;
        IsDirect = message.IsDirect;
        DirectUserId = message.DirectUserId;
    }

    public string Time { get; }

    public string Sender { get; }

    public string Text { get; }

    public bool IsDirect { get; }

    public int? DirectUserId { get; }

    private string DirectSender => Sender.StartsWith("Direct ", StringComparison.OrdinalIgnoreCase)
        ? Sender
        : $"Direct from {Sender}";

    public string DisplayText => IsDirect
        ? $"{Time} {DirectSender}: {Text}"
        : $"{Time} {Sender}: {Text}";

    public string AccessibleName => IsDirect
        ? $"{Time}, {DirectSender}: {Text}"
        : $"{Time}, {Sender}: {Text}";

    public override string ToString()
    {
        return AccessibleName;
    }
}
