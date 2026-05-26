using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.ViewModels;

public sealed class ConnectionProfileViewModel
{
    public ConnectionProfileViewModel(TeamTalkServerProfile profile)
    {
        Profile = profile;
    }

    public TeamTalkServerProfile Profile { get; }

    public string DisplayName => string.IsNullOrWhiteSpace(Profile.DisplayName) ? Profile.Host : Profile.DisplayName;

    public string Summary => $"{Profile.Host}, TCP {Profile.TcpPort}, UDP {Profile.UdpPort}";
}
