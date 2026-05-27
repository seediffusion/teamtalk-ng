namespace TeamTalkNg.Core.TeamTalk;

public sealed record ChannelSummary(
    int Id,
    string Name,
    string Path,
    int UserCount,
    bool IsProtected,
    bool IsPermanent,
    string Topic = "");
