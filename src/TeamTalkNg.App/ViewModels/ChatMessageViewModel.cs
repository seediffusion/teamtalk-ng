using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.ViewModels;

public sealed class ChatMessageViewModel : ObservableObject
{
    private bool hideDirectMessageText;

    public ChatMessageViewModel(ChatMessage message, bool hideDirectMessageText = false)
    {
        Time = message.Timestamp.ToLocalTime().ToString("HH:mm:ss");
        Sender = message.Sender;
        Text = message.Text;
        IsDirect = message.IsDirect;
        DirectUserId = message.DirectUserId;
        this.hideDirectMessageText = hideDirectMessageText;
    }

    public string Time { get; }

    public string Sender { get; }

    public string Text { get; }

    public bool IsDirect { get; }

    public int? DirectUserId { get; }

    public bool HideDirectMessageText
    {
        get => hideDirectMessageText;
        set
        {
            if (SetProperty(ref hideDirectMessageText, value))
            {
                OnPropertyChanged(nameof(DisplayText));
                OnPropertyChanged(nameof(AccessibleName));
            }
        }
    }

    private string DirectSender => Sender.StartsWith("Direct ", StringComparison.OrdinalIgnoreCase)
        ? Sender
        : $"Direct from {Sender}";

    private string DirectMessageNotice
    {
        get
        {
            string participant = GetDirectMessageParticipantName();
            string direction = Sender.StartsWith("Direct to ", StringComparison.OrdinalIgnoreCase) ? "to" : "from";
            return $"Direct message {direction} {participant}. Click or press Enter to view.";
        }
    }

    public string DisplayText => IsDirect
        ? HideDirectMessageText ? $"{Time} {DirectMessageNotice}" : FullDisplayText
        : $"{Time} {Sender}: {Text}";

    public string FullDisplayText => IsDirect
        ? $"{Time} {DirectSender}: {Text}"
        : $"{Time} {Sender}: {Text}";

    public string AccessibleName => IsDirect
        ? HideDirectMessageText ? $"{Time}, {DirectMessageNotice}" : FullAccessibleName
        : $"{Time}, {Sender}: {Text}";

    public string FullAccessibleName => IsDirect
        ? $"{Time}, {DirectSender}: {Text}"
        : $"{Time}, {Sender}: {Text}";

    public override string ToString()
    {
        return AccessibleName;
    }

    private string GetDirectMessageParticipantName()
    {
        const string directFromPrefix = "Direct from ";
        const string directToPrefix = "Direct to ";

        if (Sender.StartsWith(directFromPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return Sender[directFromPrefix.Length..];
        }

        if (Sender.StartsWith(directToPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return Sender[directToPrefix.Length..];
        }

        return Sender.StartsWith("Direct ", StringComparison.OrdinalIgnoreCase)
            ? Sender["Direct ".Length..]
            : Sender;
    }
}
