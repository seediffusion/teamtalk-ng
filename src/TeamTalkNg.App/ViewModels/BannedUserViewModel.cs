using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.ViewModels;

public sealed class BannedUserViewModel
{
    public BannedUserViewModel(BannedUserSummary bannedUser)
    {
        BannedUser = bannedUser;
        IpAddress = string.IsNullOrWhiteSpace(bannedUser.IpAddress) ? "Not specified" : bannedUser.IpAddress;
        Username = string.IsNullOrWhiteSpace(bannedUser.Username) ? "Not specified" : bannedUser.Username;
        Nickname = string.IsNullOrWhiteSpace(bannedUser.Nickname) ? "Not specified" : bannedUser.Nickname;
        ChannelPath = string.IsNullOrWhiteSpace(bannedUser.ChannelPath) ? "Server" : bannedUser.ChannelPath;
        BanTypes = bannedUser.TypeDescription;
        Owner = string.IsNullOrWhiteSpace(bannedUser.Owner) ? "Not specified" : bannedUser.Owner;
        BanTime = string.IsNullOrWhiteSpace(bannedUser.BanTime) ? "Not specified" : bannedUser.BanTime;
        AccessibleName = $"{bannedUser.DisplayName}, owner {Owner}, banned {BanTime}";
    }

    public BannedUserSummary BannedUser { get; }

    public string IpAddress { get; }

    public string Username { get; }

    public string Nickname { get; }

    public string ChannelPath { get; }

    public string BanTypes { get; }

    public string Owner { get; }

    public string BanTime { get; }

    public string AccessibleName { get; }

    public override string ToString()
    {
        return AccessibleName;
    }
}
