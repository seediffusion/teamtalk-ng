namespace TeamTalkNg.Core.TeamTalk;

public sealed record AudioProcessingSettings(
    bool EnableNoiseSuppression,
    bool EnableEchoCancellation,
    bool EnableAutomaticGainControl);
