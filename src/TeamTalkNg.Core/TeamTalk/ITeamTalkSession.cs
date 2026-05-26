namespace TeamTalkNg.Core.TeamTalk;

public interface ITeamTalkSession
{
    event EventHandler<ConnectionStatus>? ConnectionStatusChanged;
    event EventHandler<ChatMessage>? ChannelMessageReceived;
    event EventHandler<ChannelSummary>? ChannelAddedOrUpdated;
    event EventHandler<int>? ChannelRemoved;
    event EventHandler<UserSummary>? UserJoined;
    event EventHandler<UserSummary>? UserLeft;

    ConnectionStatus Status { get; }

    Task ConnectAsync(TeamTalkServerProfile profile, CancellationToken cancellationToken = default);

    Task DisconnectAsync(CancellationToken cancellationToken = default);

    Task SendChannelMessageAsync(string text, CancellationToken cancellationToken = default);
}
