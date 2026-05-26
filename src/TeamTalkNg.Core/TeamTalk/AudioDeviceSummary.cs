namespace TeamTalkNg.Core.TeamTalk;

public sealed record AudioDeviceSummary(
    int Id,
    string Name,
    bool SupportsInput,
    bool SupportsOutput,
    bool IsDefaultInput,
    bool IsDefaultOutput);
