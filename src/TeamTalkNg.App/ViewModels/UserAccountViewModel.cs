using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.ViewModels;

public sealed class UserAccountViewModel
{
    public UserAccountViewModel(UserAccountSummary account)
    {
        Account = account;
        Username = string.IsNullOrWhiteSpace(account.Username) ? "Unnamed account" : account.Username;
        Type = account.TypeDescription;
        Rights = FormatRights(account.Type, account.Rights);
        InitialChannel = string.IsNullOrWhiteSpace(account.InitialChannel) ? "None" : account.InitialChannel;
        Note = string.IsNullOrWhiteSpace(account.Note) ? "None" : account.Note;
        LastModified = string.IsNullOrWhiteSpace(account.LastModified) ? "Not available" : account.LastModified;
        LastLoginTime = string.IsNullOrWhiteSpace(account.LastLoginTime) ? "Never" : account.LastLoginTime;
        AccessibleName = $"{Username}, {Type}, rights {Rights}, initial channel {InitialChannel}, last login {LastLoginTime}";
    }

    public UserAccountSummary Account { get; }

    public string Username { get; }

    public string Type { get; }

    public string Rights { get; }

    public string InitialChannel { get; }

    public string Note { get; }

    public string LastModified { get; }

    public string LastLoginTime { get; }

    public string AccessibleName { get; }

    public override string ToString()
    {
        return AccessibleName;
    }

    private static string FormatRights(UserAccountType type, UserAccountRights rights)
    {
        if (type == UserAccountType.Administrator)
        {
            return "Unrestricted";
        }

        if (rights == UserAccountRights.None)
        {
            return "None";
        }

        string[] names = Enum.GetValues<UserAccountRights>()
            .Where(right => right != UserAccountRights.None && rights.HasFlag(right))
            .Select(FormatRightName)
            .ToArray();
        return names.Length == 0 ? "None" : string.Join(", ", names);
    }

    private static string FormatRightName(UserAccountRights right)
    {
        return right switch
        {
            UserAccountRights.MultiLogin => "multi login",
            UserAccountRights.ViewAllUsers => "view all users",
            UserAccountRights.CreateTemporaryChannel => "create temporary channels",
            UserAccountRights.ModifyChannels => "modify channels",
            UserAccountRights.BroadcastTextMessages => "broadcast messages",
            UserAccountRights.KickUsers => "kick users",
            UserAccountRights.BanUsers => "ban users",
            UserAccountRights.MoveUsers => "move users",
            UserAccountRights.OperatorEnable => "become operator",
            UserAccountRights.UploadFiles => "upload files",
            UserAccountRights.DownloadFiles => "download files",
            UserAccountRights.UpdateServerProperties => "update server properties",
            UserAccountRights.TransmitVoice => "transmit voice",
            UserAccountRights.TransmitVideoCapture => "transmit video",
            UserAccountRights.TransmitDesktop => "transmit desktop",
            UserAccountRights.TransmitDesktopInput => "desktop input",
            UserAccountRights.TransmitMediaFileAudio => "stream media audio",
            UserAccountRights.TransmitMediaFileVideo => "stream media video",
            UserAccountRights.LockedNickname => "locked nickname",
            UserAccountRights.LockedStatus => "locked status",
            UserAccountRights.RecordVoice => "record voice",
            UserAccountRights.ViewHiddenChannels => "view hidden channels",
            UserAccountRights.SendDirectMessages => "send direct messages",
            UserAccountRights.SendChannelMessages => "send channel messages",
            _ => right.ToString()
        };
    }
}
