namespace TeamTalkNg.Core.TeamTalk;

public sealed record ChannelFileSummary(
    int Id,
    string Name,
    long SizeBytes,
    string Owner,
    string UploadedAt);
