using System.Windows;
using TeamTalkNg.App.ViewModels;
using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.Services;

public sealed class PreferencesDialogService : IPreferencesDialogService
{
    public AppSettings? ShowPreferencesDialog(
        AppSettings currentSettings,
        IReadOnlyList<AudioDeviceSummary> audioDevices,
        IReadOnlyList<SoundEventDefinition> soundEvents,
        IReadOnlyList<SoundPackOption> soundPacks,
        Func<Task<IReadOnlyList<AudioDeviceSummary>>> refreshAudioDevices,
        Func<Task<AudioInputLevelSummary>> getAudioInputLevel,
        Func<SoundEvent, string, string> getSoundFileName,
        Action<SoundEvent, string, int> previewSoundEvent)
    {
        var viewModel = new PreferencesDialogViewModel(
            currentSettings,
            audioDevices,
            soundEvents,
            soundPacks,
            refreshAudioDevices,
            getAudioInputLevel,
            getSoundFileName,
            previewSoundEvent);
        var dialog = new PreferencesDialog
        {
            Owner = Application.Current.MainWindow,
            DataContext = viewModel
        };

        viewModel.RequestClose += (_, accepted) =>
        {
            dialog.DialogResult = accepted;
            dialog.Close();
        };

        return dialog.ShowDialog() == true ? viewModel.ToSettings() : null;
    }
}
