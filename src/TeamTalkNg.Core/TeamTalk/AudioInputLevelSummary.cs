namespace TeamTalkNg.Core.TeamTalk;

public sealed record AudioInputLevelSummary(int Level, int VoiceActivationLevel)
{
    public int ClampedLevel => Math.Clamp(Level, 0, 100);

    public int ClampedVoiceActivationLevel => Math.Clamp(VoiceActivationLevel, 0, 100);
}
