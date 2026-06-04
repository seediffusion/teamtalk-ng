using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.Services;

public interface IPreferencesDialogService
{
    AppSettings? ShowPreferencesDialog(
        AppSettings currentSettings,
        IReadOnlyList<AudioDeviceSummary> audioDevices,
        IReadOnlyList<SoundPackOption> soundPacks,
        Func<Task<IReadOnlyList<AudioDeviceSummary>>> refreshAudioDevices,
        Func<Task<AudioInputLevelSummary>> getAudioInputLevel);
}
