namespace TeamTalkNg.App.Services;

public sealed record AppSettings
{
    public AppTheme Theme { get; init; } = AppTheme.Light;

    public bool AnnounceChannelMessages { get; init; } = true;

    public bool AnnouncePrivateMessages { get; init; } = true;

    public bool AnnounceUserJoinLeave { get; init; } = true;

    public bool AnnounceSelectionChanges { get; init; } = true;

    public bool SendAnnouncementsToBraille { get; init; } = true;
}
