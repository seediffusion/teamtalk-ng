using System.Collections.ObjectModel;
using System.Windows.Input;
using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.ViewModels;

public sealed class ConnectDialogViewModel : ObservableObject
{
    private ConnectionProfileViewModel? selectedProfile;
    private string displayName = string.Empty;
    private string host = string.Empty;
    private int tcpPort = 10333;
    private int udpPort = 10333;
    private string username = string.Empty;
    private string password = string.Empty;
    private string nickname = Environment.UserName;
    private bool isEncrypted;
    private string channelPath = "/";
    private string channelPassword = string.Empty;

    public ConnectDialogViewModel(IReadOnlyList<TeamTalkServerProfile> profiles)
    {
        foreach (TeamTalkServerProfile profile in profiles)
        {
            Profiles.Add(new ConnectionProfileViewModel(profile));
        }

        SelectProfileCommand = new RelayCommand(profile =>
        {
            if (profile is ConnectionProfileViewModel item)
            {
                SelectedProfile = item;
            }
        });
        ConnectCommand = new RelayCommand(() => RequestClose?.Invoke(this, true), CanConnect);
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, false));

        SelectedProfile = Profiles.FirstOrDefault();
    }

    public event EventHandler<bool>? RequestClose;

    public ObservableCollection<ConnectionProfileViewModel> Profiles { get; } = [];

    public ICommand SelectProfileCommand { get; }

    public ICommand ConnectCommand { get; }

    public ICommand CancelCommand { get; }

    public ConnectionProfileViewModel? SelectedProfile
    {
        get => selectedProfile;
        set
        {
            if (SetProperty(ref selectedProfile, value) && value is not null)
            {
                ApplyProfile(value.Profile);
            }
        }
    }

    public string DisplayName
    {
        get => displayName;
        set
        {
            if (SetProperty(ref displayName, value))
            {
                RaiseCanConnectChanged();
            }
        }
    }

    public string Host
    {
        get => host;
        set
        {
            if (SetProperty(ref host, value))
            {
                RaiseCanConnectChanged();
            }
        }
    }

    public int TcpPort
    {
        get => tcpPort;
        set
        {
            if (SetProperty(ref tcpPort, value))
            {
                RaiseCanConnectChanged();
            }
        }
    }

    public int UdpPort
    {
        get => udpPort;
        set
        {
            if (SetProperty(ref udpPort, value))
            {
                RaiseCanConnectChanged();
            }
        }
    }

    public string Username
    {
        get => username;
        set => SetProperty(ref username, value);
    }

    public string Password
    {
        get => password;
        set => SetProperty(ref password, value);
    }

    public string Nickname
    {
        get => nickname;
        set => SetProperty(ref nickname, value);
    }

    public bool IsEncrypted
    {
        get => isEncrypted;
        set => SetProperty(ref isEncrypted, value);
    }

    public string ChannelPath
    {
        get => channelPath;
        set => SetProperty(ref channelPath, value);
    }

    public string ChannelPassword
    {
        get => channelPassword;
        set => SetProperty(ref channelPassword, value);
    }

    public TeamTalkServerProfile ToProfile()
    {
        return new TeamTalkServerProfile
        {
            DisplayName = string.IsNullOrWhiteSpace(DisplayName) ? Host.Trim() : DisplayName.Trim(),
            Host = Host.Trim(),
            TcpPort = TcpPort,
            UdpPort = UdpPort,
            Username = Username.Trim(),
            Password = Password,
            Nickname = Nickname.Trim(),
            IsEncrypted = IsEncrypted,
            ChannelPath = string.IsNullOrWhiteSpace(ChannelPath) ? "/" : ChannelPath.Trim(),
            ChannelPassword = ChannelPassword
        };
    }

    private void ApplyProfile(TeamTalkServerProfile profile)
    {
        DisplayName = profile.DisplayName;
        Host = profile.Host;
        TcpPort = profile.TcpPort;
        UdpPort = profile.UdpPort;
        Username = profile.Username;
        Password = profile.Password;
        Nickname = string.IsNullOrWhiteSpace(profile.Nickname) ? Environment.UserName : profile.Nickname;
        IsEncrypted = profile.IsEncrypted;
        ChannelPath = string.IsNullOrWhiteSpace(profile.ChannelPath) ? "/" : profile.ChannelPath;
        ChannelPassword = profile.ChannelPassword;
    }

    private bool CanConnect()
    {
        return !string.IsNullOrWhiteSpace(Host)
            && TcpPort is > 0 and <= 65535
            && UdpPort is > 0 and <= 65535;
    }

    private void RaiseCanConnectChanged()
    {
        if (ConnectCommand is RelayCommand command)
        {
            command.RaiseCanExecuteChanged();
        }
    }
}
