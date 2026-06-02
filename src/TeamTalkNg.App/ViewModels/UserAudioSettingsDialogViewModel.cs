using System.Windows.Input;
using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.ViewModels;

public sealed class UserAudioSettingsDialogViewModel : ObservableObject
{
    private int voiceVolumePercent;
    private bool isVoiceMuted;

    public UserAudioSettingsDialogViewModel(ChannelTreeItemViewModel user)
    {
        UserId = user.Id;
        UserName = user.Name;
        voiceVolumePercent = user.VoiceVolumePercent;
        isVoiceMuted = user.IsVoiceMuted;
        OkCommand = new RelayCommand(Accept);
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, false));
    }

    public event EventHandler<bool>? RequestClose;

    public int UserId { get; }

    public string UserName { get; }

    public int VoiceVolumePercent
    {
        get => voiceVolumePercent;
        set => SetProperty(ref voiceVolumePercent, Math.Clamp(value, 0, 200));
    }

    public bool IsVoiceMuted
    {
        get => isVoiceMuted;
        set => SetProperty(ref isVoiceMuted, value);
    }

    public ICommand OkCommand { get; }

    public ICommand CancelCommand { get; }

    public UserAudioSettingsRequest CreateRequest()
    {
        return new UserAudioSettingsRequest(UserId, VoiceVolumePercent, IsVoiceMuted);
    }

    private void Accept()
    {
        RequestClose?.Invoke(this, true);
    }
}
