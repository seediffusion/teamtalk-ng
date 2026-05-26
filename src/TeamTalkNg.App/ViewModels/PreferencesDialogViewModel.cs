using System.Collections.ObjectModel;
using System.Windows.Input;
using TeamTalkNg.App.Services;
using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.ViewModels;

public sealed class PreferencesDialogViewModel : ObservableObject
{
    private AppTheme selectedTheme;
    private bool announceChannelMessages;
    private bool announcePrivateMessages;
    private bool announceUserJoinLeave;
    private bool announceSelectionChanges;
    private bool sendAnnouncementsToBraille;
    private int selectedInputDeviceId;
    private int selectedOutputDeviceId;
    private int voiceActivationLevel;

    public PreferencesDialogViewModel(AppSettings settings, IReadOnlyList<AudioDeviceSummary> audioDevices)
    {
        Themes =
        [
            new ThemeOptionViewModel(AppTheme.Light, "Light"),
            new ThemeOptionViewModel(AppTheme.Dark, "Dark")
        ];
        InputDevices = [AudioDeviceOptionViewModel.DefaultInput];
        OutputDevices = [AudioDeviceOptionViewModel.DefaultOutput];
        foreach (AudioDeviceSummary device in audioDevices)
        {
            if (device.SupportsInput)
            {
                InputDevices.Add(AudioDeviceOptionViewModel.FromInputDevice(device));
            }

            if (device.SupportsOutput)
            {
                OutputDevices.Add(AudioDeviceOptionViewModel.FromOutputDevice(device));
            }
        }

        selectedTheme = settings.Theme;
        announceChannelMessages = settings.AnnounceChannelMessages;
        announcePrivateMessages = settings.AnnouncePrivateMessages;
        announceUserJoinLeave = settings.AnnounceUserJoinLeave;
        announceSelectionChanges = settings.AnnounceSelectionChanges;
        sendAnnouncementsToBraille = settings.SendAnnouncementsToBraille;
        selectedInputDeviceId = settings.AudioInputDeviceId ?? AudioDeviceOptionViewModel.DefaultDeviceId;
        selectedOutputDeviceId = settings.AudioOutputDeviceId ?? AudioDeviceOptionViewModel.DefaultDeviceId;
        voiceActivationLevel = Math.Clamp(settings.VoiceActivationLevel, 0, 100);

        SaveCommand = new RelayCommand(() => RequestClose?.Invoke(this, true));
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, false));
    }

    public event EventHandler<bool>? RequestClose;

    public ObservableCollection<ThemeOptionViewModel> Themes { get; }

    public ObservableCollection<AudioDeviceOptionViewModel> InputDevices { get; }

    public ObservableCollection<AudioDeviceOptionViewModel> OutputDevices { get; }

    public ICommand SaveCommand { get; }

    public ICommand CancelCommand { get; }

    public AppTheme SelectedTheme
    {
        get => selectedTheme;
        set => SetProperty(ref selectedTheme, value);
    }

    public bool AnnounceChannelMessages
    {
        get => announceChannelMessages;
        set => SetProperty(ref announceChannelMessages, value);
    }

    public bool AnnouncePrivateMessages
    {
        get => announcePrivateMessages;
        set => SetProperty(ref announcePrivateMessages, value);
    }

    public bool AnnounceUserJoinLeave
    {
        get => announceUserJoinLeave;
        set => SetProperty(ref announceUserJoinLeave, value);
    }

    public bool AnnounceSelectionChanges
    {
        get => announceSelectionChanges;
        set => SetProperty(ref announceSelectionChanges, value);
    }

    public bool SendAnnouncementsToBraille
    {
        get => sendAnnouncementsToBraille;
        set => SetProperty(ref sendAnnouncementsToBraille, value);
    }

    public int SelectedInputDeviceId
    {
        get => selectedInputDeviceId;
        set => SetProperty(ref selectedInputDeviceId, value);
    }

    public int SelectedOutputDeviceId
    {
        get => selectedOutputDeviceId;
        set => SetProperty(ref selectedOutputDeviceId, value);
    }

    public int VoiceActivationLevel
    {
        get => voiceActivationLevel;
        set => SetProperty(ref voiceActivationLevel, Math.Clamp(value, 0, 100));
    }

    public AppSettings ToSettings()
    {
        return new AppSettings
        {
            Theme = SelectedTheme,
            AnnounceChannelMessages = AnnounceChannelMessages,
            AnnouncePrivateMessages = AnnouncePrivateMessages,
            AnnounceUserJoinLeave = AnnounceUserJoinLeave,
            AnnounceSelectionChanges = AnnounceSelectionChanges,
            SendAnnouncementsToBraille = SendAnnouncementsToBraille,
            AudioInputDeviceId = SelectedInputDeviceId == AudioDeviceOptionViewModel.DefaultDeviceId ? null : SelectedInputDeviceId,
            AudioOutputDeviceId = SelectedOutputDeviceId == AudioDeviceOptionViewModel.DefaultDeviceId ? null : SelectedOutputDeviceId,
            VoiceActivationLevel = VoiceActivationLevel
        };
    }
}
