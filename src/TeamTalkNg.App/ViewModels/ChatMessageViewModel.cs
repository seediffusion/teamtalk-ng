using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.ViewModels;

public sealed class ChatMessageViewModel
{
    public ChatMessageViewModel(ChatMessage message)
    {
        Time = message.Timestamp.ToLocalTime().ToString("HH:mm:ss");
        Sender = message.Sender;
        Text = message.Text;
    }

    public string Time { get; }

    public string Sender { get; }

    public string Text { get; }
}
