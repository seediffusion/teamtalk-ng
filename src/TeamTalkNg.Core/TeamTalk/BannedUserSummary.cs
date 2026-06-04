namespace TeamTalkNg.Core.TeamTalk;

public sealed record BannedUserSummary(
    string IpAddress,
    string ChannelPath,
    string BanTime,
    string Nickname,
    string Username,
    BannedUserType BanTypes,
    string Owner)
{
    public string TypeDescription
    {
        get
        {
            List<string> parts = [];
            if ((BanTypes & BannedUserType.IpAddress) != 0)
            {
                parts.Add("IP address");
            }

            if ((BanTypes & BannedUserType.Username) != 0)
            {
                parts.Add("Username");
            }

            if ((BanTypes & BannedUserType.Channel) != 0)
            {
                parts.Add("Channel");
            }

            return parts.Count == 0 ? "Unknown" : string.Join(", ", parts);
        }
    }

    public string DisplayName
    {
        get
        {
            string identity = FirstNonEmpty(Nickname, Username, IpAddress, "Unknown banned user");
            string scope = string.IsNullOrWhiteSpace(ChannelPath) ? "server" : ChannelPath;
            return $"{identity}, {TypeDescription}, {scope}";
        }
    }

    private static string FirstNonEmpty(params string[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
    }
}
