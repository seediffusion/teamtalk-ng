namespace TeamTalkNg.Core.TeamTalk;

public sealed record UserSummary(
    int Id,
    string Nickname,
    string Username,
    string ChannelPath,
    bool IsTalking,
    bool IsAway,
    bool IsOperator,
    string StatusMessage = "");
