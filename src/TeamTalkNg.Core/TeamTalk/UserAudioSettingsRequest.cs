namespace TeamTalkNg.Core.TeamTalk;

public sealed record UserAudioSettingsRequest(
    int UserId,
    int VoiceVolumePercent,
    bool IsVoiceMuted);
