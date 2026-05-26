namespace TeamTalkNg.Core.TeamTalk;

public sealed record TeamTalkServerProfile
{
    public string DisplayName { get; init; } = string.Empty;

    public string Host { get; init; } = string.Empty;

    public int TcpPort { get; init; } = 10333;

    public int UdpPort { get; init; } = 10333;

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string Nickname { get; init; } = string.Empty;

    public bool IsEncrypted { get; init; }

    public string? ChannelPath { get; init; } = "/";

    public string ChannelPassword { get; init; } = string.Empty;
}
