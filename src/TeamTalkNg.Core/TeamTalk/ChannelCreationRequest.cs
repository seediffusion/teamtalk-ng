namespace TeamTalkNg.Core.TeamTalk;

public sealed record ChannelCreationRequest(
    string ParentPath,
    string Name,
    string Topic,
    string Password,
    int MaxUsers,
    bool IsPermanent);
