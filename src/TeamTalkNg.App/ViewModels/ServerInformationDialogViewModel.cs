using System.Windows.Input;
using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.ViewModels;

public sealed class ServerInformationDialogViewModel
{
    public ServerInformationDialogViewModel(ServerInformationSummary serverInformation)
    {
        ServerName = string.IsNullOrWhiteSpace(serverInformation.ServerName) ? "Not available" : serverInformation.ServerName;
        MessageOfTheDay = string.IsNullOrWhiteSpace(serverInformation.MessageOfTheDay) ? "No message of the day" : serverInformation.MessageOfTheDay;
        MaxUsers = serverInformation.MaxUsers.ToString();
        TcpPort = serverInformation.TcpPort.ToString();
        UdpPort = serverInformation.UdpPort.ToString();
        UserTimeout = $"{serverInformation.UserTimeoutSeconds} seconds";
        ServerVersion = string.IsNullOrWhiteSpace(serverInformation.ServerVersion) ? "Not available" : serverInformation.ServerVersion;
        ProtocolVersion = string.IsNullOrWhiteSpace(serverInformation.ProtocolVersion) ? "Not available" : serverInformation.ProtocolVersion;
        LoginDelay = $"{serverInformation.LoginDelayMilliseconds} milliseconds";
        CloseCommand = new RelayCommand(() => RequestClose?.Invoke(this, EventArgs.Empty));
    }

    public event EventHandler? RequestClose;

    public string ServerName { get; }

    public string MessageOfTheDay { get; }

    public string MaxUsers { get; }

    public string TcpPort { get; }

    public string UdpPort { get; }

    public string UserTimeout { get; }

    public string ServerVersion { get; }

    public string ProtocolVersion { get; }

    public string LoginDelay { get; }

    public ICommand CloseCommand { get; }
}
