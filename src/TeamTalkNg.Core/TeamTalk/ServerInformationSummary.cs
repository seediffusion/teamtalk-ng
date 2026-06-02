namespace TeamTalkNg.Core.TeamTalk;

public sealed record ServerInformationSummary(
    string ServerName,
    string MessageOfTheDay,
    int MaxUsers,
    int TcpPort,
    int UdpPort,
    int UserTimeoutSeconds,
    string ServerVersion,
    string ProtocolVersion,
    int LoginDelayMilliseconds);
