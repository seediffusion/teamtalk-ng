namespace TeamTalkNg.Core.TeamTalk;

public sealed record UserAccountSummary(
    string Username,
    UserAccountType Type,
    UserAccountRights Rights,
    int UserData,
    string Note,
    string InitialChannel,
    int AudioCodecBitrateLimit,
    string LastModified,
    string LastLoginTime)
{
    public string DisplayName => string.IsNullOrWhiteSpace(Username) ? "Unnamed account" : Username;

    public string TypeDescription => Type == UserAccountType.Administrator ? "Administrator" : "Default";
}
