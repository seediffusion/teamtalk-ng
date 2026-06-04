using System.Collections.ObjectModel;
using System.Windows.Input;
using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.ViewModels;

public sealed class UserAccountDialogViewModel : ObservableObject
{
    private string username = string.Empty;
    private string password = string.Empty;
    private UserAccountType selectedUserType = UserAccountType.Default;
    private string note = string.Empty;
    private string initialChannel = string.Empty;
    private int audioCodecBitrateLimit;

    public UserAccountDialogViewModel()
    {
        Rights =
        [
            new("Multi login", UserAccountRights.MultiLogin, false),
            new("View all users", UserAccountRights.ViewAllUsers, true),
            new("Create temporary channels", UserAccountRights.CreateTemporaryChannel, true),
            new("Modify channels", UserAccountRights.ModifyChannels, false),
            new("Broadcast messages", UserAccountRights.BroadcastTextMessages, false),
            new("Kick users", UserAccountRights.KickUsers, false),
            new("Ban users", UserAccountRights.BanUsers, false),
            new("Move users", UserAccountRights.MoveUsers, false),
            new("Become operator", UserAccountRights.OperatorEnable, false),
            new("Upload files", UserAccountRights.UploadFiles, false),
            new("Download files", UserAccountRights.DownloadFiles, true),
            new("Update server properties", UserAccountRights.UpdateServerProperties, false),
            new("Transmit voice", UserAccountRights.TransmitVoice, true),
            new("Transmit video", UserAccountRights.TransmitVideoCapture, false),
            new("Transmit desktop", UserAccountRights.TransmitDesktop, false),
            new("Desktop input", UserAccountRights.TransmitDesktopInput, false),
            new("Stream media audio", UserAccountRights.TransmitMediaFileAudio, false),
            new("Stream media video", UserAccountRights.TransmitMediaFileVideo, false),
            new("Locked nickname", UserAccountRights.LockedNickname, false),
            new("Locked status", UserAccountRights.LockedStatus, false),
            new("Record voice", UserAccountRights.RecordVoice, false),
            new("View hidden channels", UserAccountRights.ViewHiddenChannels, false),
            new("Send direct messages", UserAccountRights.SendDirectMessages, true),
            new("Send channel messages", UserAccountRights.SendChannelMessages, true)
        ];
        CreateCommand = new RelayCommand(() => RequestClose?.Invoke(this, true), CanCreate);
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, false));
    }

    public event EventHandler<bool>? RequestClose;

    public IReadOnlyList<UserAccountType> UserTypes { get; } =
    [
        UserAccountType.Default,
        UserAccountType.Administrator
    ];

    public ObservableCollection<UserAccountRightOptionViewModel> Rights { get; }

    public string Username
    {
        get => username;
        set
        {
            if (SetProperty(ref username, value))
            {
                RaiseCreateCanExecuteChanged();
            }
        }
    }

    public string Password
    {
        get => password;
        set => SetProperty(ref password, value);
    }

    public UserAccountType SelectedUserType
    {
        get => selectedUserType;
        set => SetProperty(ref selectedUserType, value);
    }

    public string Note
    {
        get => note;
        set => SetProperty(ref note, value);
    }

    public string InitialChannel
    {
        get => initialChannel;
        set => SetProperty(ref initialChannel, value);
    }

    public int AudioCodecBitrateLimit
    {
        get => audioCodecBitrateLimit;
        set => SetProperty(ref audioCodecBitrateLimit, Math.Max(0, value));
    }

    public UserAccountCreationRequest CreateRequest => new(
        Username.Trim(),
        Password,
        SelectedUserType,
        SelectedUserType == UserAccountType.Administrator ? UserAccountRights.None : SelectedRights(),
        Note.Trim(),
        InitialChannel.Trim(),
        AudioCodecBitrateLimit);

    public ICommand CreateCommand { get; }

    public ICommand CancelCommand { get; }

    private bool CanCreate()
    {
        return !string.IsNullOrWhiteSpace(Username);
    }

    private UserAccountRights SelectedRights()
    {
        UserAccountRights rights = UserAccountRights.None;
        foreach (UserAccountRightOptionViewModel option in Rights.Where(item => item.IsSelected))
        {
            rights |= option.Right;
        }

        return rights;
    }

    private void RaiseCreateCanExecuteChanged()
    {
        if (CreateCommand is RelayCommand command)
        {
            command.RaiseCanExecuteChanged();
        }
    }
}
