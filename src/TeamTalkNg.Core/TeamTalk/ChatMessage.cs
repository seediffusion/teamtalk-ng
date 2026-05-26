namespace TeamTalkNg.Core.TeamTalk;

public sealed record ChatMessage(
    DateTimeOffset Timestamp,
    string Sender,
    string Text,
    bool IsDirect = false,
    bool IsSystem = false);
