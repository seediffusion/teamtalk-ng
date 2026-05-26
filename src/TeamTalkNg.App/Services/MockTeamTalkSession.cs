using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.Services;

public sealed class MockTeamTalkSession : ITeamTalkSession
{
    private TeamTalkServerProfile? activeProfile;

    public event EventHandler<ConnectionStatus>? ConnectionStatusChanged;
    public event EventHandler<ChatMessage>? ChannelMessageReceived;
    public event EventHandler<ChannelSummary>? ChannelAddedOrUpdated;
    public event EventHandler<int>? ChannelRemoved;
    public event EventHandler<UserSummary>? UserJoined;
    public event EventHandler<UserSummary>? UserLeft;

    public ConnectionStatus Status { get; private set; } = ConnectionStatus.Disconnected;

    public async Task ConnectAsync(TeamTalkServerProfile profile, CancellationToken cancellationToken = default)
    {
        activeProfile = profile;
        SetStatus(ConnectionStatus.Connecting);
        await Task.Delay(300, cancellationToken);
        SetStatus(ConnectionStatus.Connected);
        await Task.Delay(250, cancellationToken);
        SetStatus(ConnectionStatus.LoggedIn);
        await Task.Delay(250, cancellationToken);
        SetStatus(ConnectionStatus.InChannel);
        string channelPath = string.IsNullOrWhiteSpace(profile.ChannelPath) ? "/" : profile.ChannelPath;
        ChannelAddedOrUpdated?.Invoke(this, new ChannelSummary(1, GetChannelName(channelPath), channelPath, 1, IsProtected: false, IsPermanent: true));
        UserJoined?.Invoke(this, new UserSummary(1, profile.Nickname, profile.Username, channelPath, IsTalking: false, IsAway: false, IsOperator: true));

        ChannelMessageReceived?.Invoke(this, new ChatMessage(
            DateTimeOffset.Now,
            "Server",
            $"Welcome to {profile.DisplayName}. This is mocked until the TeamTalk SDK adapter is connected.",
            IsSystem: true));
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (Status == ConnectionStatus.Disconnected)
        {
            return Task.CompletedTask;
        }

        activeProfile = null;
        SetStatus(ConnectionStatus.Disconnected);
        return Task.CompletedTask;
    }

    public Task SendChannelMessageAsync(string text, CancellationToken cancellationToken = default)
    {
        string sender = activeProfile?.Nickname ?? "You";
        ChannelMessageReceived?.Invoke(this, new ChatMessage(DateTimeOffset.Now, sender, text));
        return Task.CompletedTask;
    }

    public void SimulateUserJoined()
    {
        UserJoined?.Invoke(this, new UserSummary(44, "Morgan", "morgan", "/Lobby", IsTalking: false, IsAway: false, IsOperator: false));
    }

    public void SimulateUserLeft()
    {
        UserLeft?.Invoke(this, new UserSummary(44, "Morgan", "morgan", "/Lobby", IsTalking: false, IsAway: false, IsOperator: false));
    }

    public void SimulateChannelRemoved(int channelId)
    {
        ChannelRemoved?.Invoke(this, channelId);
    }

    private void SetStatus(ConnectionStatus status)
    {
        Status = status;
        ConnectionStatusChanged?.Invoke(this, status);
    }

    private static string GetChannelName(string? channelPath)
    {
        if (string.IsNullOrWhiteSpace(channelPath) || channelPath == "/")
        {
            return "Root";
        }

        string trimmed = channelPath.Trim().Trim('/');
        int lastSlash = trimmed.LastIndexOf('/');
        return lastSlash >= 0 ? trimmed[(lastSlash + 1)..] : trimmed;
    }
}
