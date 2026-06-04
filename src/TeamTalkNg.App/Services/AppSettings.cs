namespace TeamTalkNg.App.Services;

public sealed record AppSettings
{
    public AppTheme Theme { get; init; } = AppTheme.Light;

    public bool AnnounceChannelMessages { get; init; } = true;

    public bool AnnounceDirectMessages { get; init; } = true;

    public bool AnnounceUserJoinLeave { get; init; } = true;

    public bool AnnounceSelectionChanges { get; init; } = true;

    public bool SendAnnouncementsToBraille { get; init; } = true;

    public bool PlaySoundEvents { get; init; } = true;

    public string SoundPack { get; init; } = SoundEventService.DefaultSoundPackId;

    public int? AudioInputDeviceId { get; init; }

    public int? AudioOutputDeviceId { get; init; }

    public int VoiceActivationLevel { get; init; } = 50;

    public bool ShowInputMeter { get; init; }

    public int InputVolume { get; init; } = 50;

    public int OutputVolume { get; init; } = 50;

    public string DefaultNickname { get; init; } = Environment.UserName;

    public bool IsAway { get; init; }

    public string StatusMessage { get; init; } = string.Empty;
}
