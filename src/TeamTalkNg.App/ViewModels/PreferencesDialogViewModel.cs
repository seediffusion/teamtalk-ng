using System.Collections.ObjectModel;
using System.Windows.Input;
using TeamTalkNg.App.Services;
using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.ViewModels;

public sealed class PreferencesDialogViewModel : ObservableObject
{
    private AppTheme selectedTheme;
    private bool announceChannelMessages;
    private bool announceDirectMessages;
    private bool announceUserJoinLeave;
    private bool announceSelectionChanges;
    private bool sendAnnouncementsToBraille;
    private int selectedInputDeviceId;
    private int selectedOutputDeviceId;
    private int voiceActivationLevel;
    private string defaultNickname = Environment.UserName;
    private bool isAway;
    private string statusMessage = string.Empty;
    private string audioDeviceRefreshStatus = "Audio devices loaded";
    private string voiceActivationCalibrationStatus = "Voice activation calibration not run";
    private bool showInputMeter;
    private readonly int inputVolume;
    private readonly int outputVolume;
    private readonly Func<Task<IReadOnlyList<AudioDeviceSummary>>> refreshAudioDevices;
    private readonly Func<Task<AudioInputLevelSummary>> getAudioInputLevel;

    public PreferencesDialogViewModel(
        AppSettings settings,
        IReadOnlyList<AudioDeviceSummary> audioDevices,
        Func<Task<IReadOnlyList<AudioDeviceSummary>>> refreshAudioDevices,
        Func<Task<AudioInputLevelSummary>> getAudioInputLevel)
    {
        this.refreshAudioDevices = refreshAudioDevices;
        this.getAudioInputLevel = getAudioInputLevel;
        inputVolume = settings.InputVolume;
        outputVolume = settings.OutputVolume;
        Themes =
        [
            new ThemeOptionViewModel(AppTheme.Light, "Light"),
            new ThemeOptionViewModel(AppTheme.Dark, "Dark")
        ];
        InputDevices = [];
        OutputDevices = [];

        selectedTheme = settings.Theme;
        announceChannelMessages = settings.AnnounceChannelMessages;
        announceDirectMessages = settings.AnnounceDirectMessages;
        announceUserJoinLeave = settings.AnnounceUserJoinLeave;
        announceSelectionChanges = settings.AnnounceSelectionChanges;
        sendAnnouncementsToBraille = settings.SendAnnouncementsToBraille;
        selectedInputDeviceId = settings.AudioInputDeviceId ?? AudioDeviceOptionViewModel.DefaultDeviceId;
        selectedOutputDeviceId = settings.AudioOutputDeviceId ?? AudioDeviceOptionViewModel.DefaultDeviceId;
        voiceActivationLevel = Math.Clamp(settings.VoiceActivationLevel, 0, 100);
        showInputMeter = settings.ShowInputMeter;
        defaultNickname = string.IsNullOrWhiteSpace(settings.DefaultNickname) ? Environment.UserName : settings.DefaultNickname;
        isAway = settings.IsAway;
        statusMessage = settings.StatusMessage;
        ReplaceAudioDevices(audioDevices);

        SaveCommand = new RelayCommand(() => RequestClose?.Invoke(this, true));
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, false));
        RefreshAudioDevicesCommand = new AsyncRelayCommand(RefreshAudioDevicesAsync);
        CalibrateVoiceActivationCommand = new AsyncRelayCommand(CalibrateVoiceActivationAsync);
    }

    public event EventHandler<bool>? RequestClose;

    public ObservableCollection<ThemeOptionViewModel> Themes { get; }

    public ObservableCollection<AudioDeviceOptionViewModel> InputDevices { get; }

    public ObservableCollection<AudioDeviceOptionViewModel> OutputDevices { get; }

    public ICommand SaveCommand { get; }

    public ICommand CancelCommand { get; }

    public ICommand RefreshAudioDevicesCommand { get; }

    public ICommand CalibrateVoiceActivationCommand { get; }

    public string AudioDeviceRefreshStatus
    {
        get => audioDeviceRefreshStatus;
        private set => SetProperty(ref audioDeviceRefreshStatus, value);
    }

    public string VoiceActivationCalibrationStatus
    {
        get => voiceActivationCalibrationStatus;
        private set => SetProperty(ref voiceActivationCalibrationStatus, value);
    }

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

    public bool AnnounceDirectMessages
    {
        get => announceDirectMessages;
        set => SetProperty(ref announceDirectMessages, value);
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

    public bool ShowInputMeter
    {
        get => showInputMeter;
        set => SetProperty(ref showInputMeter, value);
    }

    public string DefaultNickname
    {
        get => defaultNickname;
        set => SetProperty(ref defaultNickname, value);
    }

    public bool IsAway
    {
        get => isAway;
        set => SetProperty(ref isAway, value);
    }

    public string StatusMessage
    {
        get => statusMessage;
        set => SetProperty(ref statusMessage, value);
    }

    public AppSettings ToSettings()
    {
        return new AppSettings
        {
            Theme = SelectedTheme,
            AnnounceChannelMessages = AnnounceChannelMessages,
            AnnounceDirectMessages = AnnounceDirectMessages,
            AnnounceUserJoinLeave = AnnounceUserJoinLeave,
            AnnounceSelectionChanges = AnnounceSelectionChanges,
            SendAnnouncementsToBraille = SendAnnouncementsToBraille,
            AudioInputDeviceId = SelectedInputDeviceId == AudioDeviceOptionViewModel.DefaultDeviceId ? null : SelectedInputDeviceId,
            AudioOutputDeviceId = SelectedOutputDeviceId == AudioDeviceOptionViewModel.DefaultDeviceId ? null : SelectedOutputDeviceId,
            VoiceActivationLevel = VoiceActivationLevel,
            ShowInputMeter = ShowInputMeter,
            InputVolume = inputVolume,
            OutputVolume = outputVolume,
            DefaultNickname = string.IsNullOrWhiteSpace(DefaultNickname) ? Environment.UserName : DefaultNickname.Trim(),
            IsAway = IsAway,
            StatusMessage = StatusMessage.Trim()
        };
    }

    private async Task RefreshAudioDevicesAsync()
    {
        try
        {
            IReadOnlyList<AudioDeviceSummary> devices = await refreshAudioDevices();
            ReplaceAudioDevices(devices);
            AudioDeviceRefreshStatus = "Audio devices refreshed";
        }
        catch (Exception ex)
        {
            AudioDeviceRefreshStatus = ex.Message;
        }
    }

    private async Task CalibrateVoiceActivationAsync()
    {
        try
        {
            VoiceActivationCalibrationStatus = "Calibrating voice activation";
            int peak = 0;
            for (int sample = 0; sample < 12; sample++)
            {
                AudioInputLevelSummary level = await getAudioInputLevel();
                peak = Math.Max(peak, level.ClampedLevel);
                await Task.Delay(100);
            }

            VoiceActivationLevel = Math.Clamp(peak + 10, 5, 95);
            VoiceActivationCalibrationStatus = $"Voice activation level set to {VoiceActivationLevel}";
        }
        catch (Exception ex)
        {
            VoiceActivationCalibrationStatus = ex.Message;
        }
    }

    private void ReplaceAudioDevices(IReadOnlyList<AudioDeviceSummary> audioDevices)
    {
        int previousInputId = SelectedInputDeviceId;
        int previousOutputId = SelectedOutputDeviceId;

        InputDevices.Clear();
        OutputDevices.Clear();
        InputDevices.Add(AudioDeviceOptionViewModel.DefaultInput);
        OutputDevices.Add(AudioDeviceOptionViewModel.DefaultOutput);

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

        SelectedInputDeviceId = InputDevices.Any(device => device.Id == previousInputId)
            ? previousInputId
            : AudioDeviceOptionViewModel.DefaultDeviceId;
        SelectedOutputDeviceId = OutputDevices.Any(device => device.Id == previousOutputId)
            ? previousOutputId
            : AudioDeviceOptionViewModel.DefaultDeviceId;
    }
}
