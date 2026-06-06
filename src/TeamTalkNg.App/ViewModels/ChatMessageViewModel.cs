using System.Globalization;
using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.ViewModels;

public sealed class ChatMessageViewModel : ObservableObject
{
    public const string DefaultTimestampFormat = "HH:mm:ss";

    private readonly DateTimeOffset timestamp;
    private bool hideDirectMessageText;
    private string timestampFormat = DefaultTimestampFormat;

    public ChatMessageViewModel(
        ChatMessage message,
        bool hideDirectMessageText = false,
        string? timestampFormat = DefaultTimestampFormat)
    {
        timestamp = message.Timestamp;
        Sender = message.Sender;
        Text = message.Text;
        IsDirect = message.IsDirect;
        DirectUserId = message.DirectUserId;
        this.hideDirectMessageText = hideDirectMessageText;
        this.timestampFormat = NormalizeTimestampFormat(timestampFormat);
    }

    public string Time => FormatTimestamp(timestamp, timestampFormat);

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
                OnTextPropertiesChanged();
            }
        }
    }

    public string TimestampFormat
    {
        get => timestampFormat;
        set
        {
            string normalized = NormalizeTimestampFormat(value);
            if (SetProperty(ref timestampFormat, normalized))
            {
                OnTextPropertiesChanged();
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

    public static string FormatTimestamp(DateTimeOffset timestamp, string? format)
    {
        string effectiveFormat = NormalizeTimestampFormat(format);
        try
        {
            return timestamp.ToLocalTime().ToString(effectiveFormat, CultureInfo.CurrentCulture);
        }
        catch (FormatException)
        {
            return timestamp.ToLocalTime().ToString(DefaultTimestampFormat, CultureInfo.CurrentCulture);
        }
    }

    private static string NormalizeTimestampFormat(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? DefaultTimestampFormat : value.Trim();
    }

    private void OnTextPropertiesChanged()
    {
        OnPropertyChanged(nameof(Time));
        OnPropertyChanged(nameof(DisplayText));
        OnPropertyChanged(nameof(FullDisplayText));
        OnPropertyChanged(nameof(AccessibleName));
        OnPropertyChanged(nameof(FullAccessibleName));
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
