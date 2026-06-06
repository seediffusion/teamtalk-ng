namespace TeamTalkNg.App.Services;

public sealed record AppSettings
{
    public const int CurrentSettingsVersion = 4;

    public int SettingsVersion { get; init; } = CurrentSettingsVersion;

    public AppTheme Theme { get; init; } = AppTheme.Light;

    public bool AnnounceChannelMessages { get; init; } = true;

    public bool AnnounceDirectMessages { get; init; } = true;

    public bool AnnounceUserJoinLeave { get; init; } = true;

    public bool AnnounceSelectionChanges { get; init; } = true;

    public bool SendAnnouncementsToBraille { get; init; } = true;

    public bool InterruptImportantAnnouncements { get; init; } = true;

    public bool ShowAnnouncementsInStatusBar { get; init; } = true;

    public bool ShowMessageAnnouncementsInStatusBar { get; init; }

    public bool HideDirectMessageTextInChatHistory { get; init; }

    public Dictionary<string, string> AnnouncementTemplates { get; init; } = [];

    public Dictionary<string, bool> AnnouncementEventEnabled { get; init; } = [];

    public bool PlaySoundEvents { get; init; } = true;

    public string SoundPack { get; init; } = SoundEventService.DefaultSoundPackId;

    public int SoundEventVolume { get; init; } = 100;

    public Dictionary<string, bool> SoundEventEnabled { get; init; } = [];

    public int? AudioInputDeviceId { get; init; }

    public int? AudioOutputDeviceId { get; init; }

    public int VoiceActivationLevel { get; init; } = 2;

    public bool ShowInputMeter { get; init; }

    public int InputVolume { get; init; } = 50;

    public int OutputVolume { get; init; } = 50;

    public bool EnableNoiseSuppression { get; init; }

    public bool EnableEchoCancellation { get; init; }

    public bool EnableAutomaticGainControl { get; init; }

    public string DefaultNickname { get; init; } = Environment.UserName;

    public bool IsAway { get; init; }

    public string StatusMessage { get; init; } = string.Empty;

    public int InactivityTimeoutSeconds { get; init; }

    public bool DisableVoiceActivationDuringInactivity { get; init; }

    public string InactivityStatusMessage { get; init; } = "Away due to inactivity";

    public bool ShowVoiceActivationSlider { get; init; } = true;

    public bool ShowChannelUserCounts { get; init; } = true;

    public bool ShowUsernamesInsteadOfNicknames { get; init; }

    public bool ShowChannelIcons { get; init; } = true;

    public bool ShowChannelTopicsInChannelList { get; init; }

    public ChannelSortMode ChannelSortMode { get; init; } = ChannelSortMode.ServerOrder;
}
