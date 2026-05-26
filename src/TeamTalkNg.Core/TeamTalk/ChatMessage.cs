namespace TeamTalkNg.Core.TeamTalk;

public sealed record ChatMessage(
    DateTimeOffset Timestamp,
    string Sender,
    string Text,
    bool IsPrivate = false,
    bool IsSystem = false);
