namespace TeamTalkNg.Core.TeamTalk;

[Flags]
public enum BannedUserType : uint
{
    None = 0,
    Channel = 0x01,
    IpAddress = 0x02,
    Username = 0x04
}
