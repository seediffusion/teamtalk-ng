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
    public event EventHandler<UserSummary>? UserUpdated;
    public event EventHandler<UserSummary>? UserLeft;

    public ConnectionStatus Status { get; private set; } = ConnectionStatus.Disconnected;

    public Task<IReadOnlyList<AudioDeviceSummary>> GetAudioDevicesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AudioDeviceSummary> devices =
        [
            new AudioDeviceSummary(1, "Default microphone", SupportsInput: true, SupportsOutput: false, IsDefaultInput: true, IsDefaultOutput: false),
            new AudioDeviceSummary(2, "Default speaker", SupportsInput: false, SupportsOutput: true, IsDefaultInput: false, IsDefaultOutput: true)
        ];
        return Task.FromResult(devices);
    }

    public Task SetAudioDevicesAsync(int? inputDeviceId, int? outputDeviceId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SetAudioVolumeAsync(int inputVolumePercent, int outputVolumePercent, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SetUserStatusAsync(UserStatusRequest status, CancellationToken cancellationToken = default)
    {
        if (Status is ConnectionStatus.Disconnected or ConnectionStatus.Connecting)
        {
            throw new InvalidOperationException("You must be logged in before changing status.");
        }

        ChannelMessageReceived?.Invoke(this, new ChatMessage(
            DateTimeOffset.Now,
            "TeamTalk NG",
            status.IsAway ? "Status set to away." : "Status set to available.",
            IsSystem: true));
        return Task.CompletedTask;
    }

    public Task SetNicknameAsync(string nickname, CancellationToken cancellationToken = default)
    {
        if (Status is ConnectionStatus.Disconnected or ConnectionStatus.Connecting)
        {
            throw new InvalidOperationException("You must be logged in before changing nickname.");
        }

        string trimmedNickname = nickname.Trim();
        if (string.IsNullOrWhiteSpace(trimmedNickname))
        {
            throw new InvalidOperationException("Nickname cannot be empty.");
        }

        activeProfile = activeProfile is null ? null : activeProfile with { Nickname = trimmedNickname };
        UserUpdated?.Invoke(this, new UserSummary(1, trimmedNickname, activeProfile?.Username ?? string.Empty, activeProfile?.ChannelPath ?? "/", IsTalking: false, IsAway: false, IsOperator: true));
        ChannelMessageReceived?.Invoke(this, new ChatMessage(DateTimeOffset.Now, "TeamTalk NG", $"Nickname changed to {trimmedNickname}.", IsSystem: true));
        return Task.CompletedTask;
    }

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

    public Task JoinChannelAsync(string channelPath, string password = "", CancellationToken cancellationToken = default)
    {
        if (Status is ConnectionStatus.Disconnected or ConnectionStatus.Connecting)
        {
            throw new InvalidOperationException("You must be logged in before joining a channel.");
        }

        string normalizedPath = string.IsNullOrWhiteSpace(channelPath) ? "/" : channelPath;
        string channelName = GetChannelName(normalizedPath);
        ChannelAddedOrUpdated?.Invoke(this, new ChannelSummary(2, channelName, normalizedPath, 1, IsProtected: false, IsPermanent: true));
        UserJoined?.Invoke(this, new UserSummary(1, activeProfile?.Nickname ?? "You", activeProfile?.Username ?? string.Empty, normalizedPath, IsTalking: false, IsAway: false, IsOperator: true));
        SetStatus(ConnectionStatus.InChannel);
        return Task.CompletedTask;
    }

    public Task CreateChannelAsync(ChannelCreationRequest request, CancellationToken cancellationToken = default)
    {
        if (Status is ConnectionStatus.Disconnected or ConnectionStatus.Connecting)
        {
            throw new InvalidOperationException("You must be logged in before creating a channel.");
        }

        string channelName = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(channelName) || channelName.Contains('/', StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Channel names cannot be empty or contain slashes.");
        }

        string parentPath = string.IsNullOrWhiteSpace(request.ParentPath) || request.ParentPath == "/"
            ? string.Empty
            : request.ParentPath.TrimEnd('/');
        string path = $"{parentPath}/{channelName}";
        ChannelAddedOrUpdated?.Invoke(this, new ChannelSummary(
            Math.Abs(path.GetHashCode()),
            channelName,
            path,
            UserCount: 0,
            IsProtected: !string.IsNullOrEmpty(request.Password),
            request.IsPermanent,
            request.Topic));
        ChannelMessageReceived?.Invoke(this, new ChatMessage(DateTimeOffset.Now, "TeamTalk NG", $"Created channel {channelName}.", IsSystem: true));
        return Task.CompletedTask;
    }

    public Task SetChannelTopicAsync(string channelPath, string topic, CancellationToken cancellationToken = default)
    {
        if (Status is ConnectionStatus.Disconnected or ConnectionStatus.Connecting)
        {
            throw new InvalidOperationException("You must be logged in before editing a channel topic.");
        }

        string normalizedPath = string.IsNullOrWhiteSpace(channelPath) ? "/" : channelPath;
        ChannelAddedOrUpdated?.Invoke(this, new ChannelSummary(
            Math.Abs(normalizedPath.GetHashCode()),
            GetChannelName(normalizedPath),
            normalizedPath,
            UserCount: 1,
            IsProtected: false,
            IsPermanent: true,
            Topic: topic.Trim()));
        ChannelMessageReceived?.Invoke(this, new ChatMessage(DateTimeOffset.Now, "TeamTalk NG", $"Updated topic for {GetChannelName(normalizedPath)}.", IsSystem: true));
        return Task.CompletedTask;
    }

    public Task RemoveChannelAsync(string channelPath, CancellationToken cancellationToken = default)
    {
        if (Status is ConnectionStatus.Disconnected or ConnectionStatus.Connecting)
        {
            throw new InvalidOperationException("You must be logged in before deleting a channel.");
        }

        if (string.IsNullOrWhiteSpace(channelPath) || channelPath == "/")
        {
            throw new InvalidOperationException("The root channel cannot be deleted.");
        }

        ChannelRemoved?.Invoke(this, Math.Abs(channelPath.GetHashCode()));
        ChannelMessageReceived?.Invoke(this, new ChatMessage(DateTimeOffset.Now, "TeamTalk NG", $"Deleted channel {GetChannelName(channelPath)}.", IsSystem: true));
        return Task.CompletedTask;
    }

    public Task SendChannelMessageAsync(string text, CancellationToken cancellationToken = default)
    {
        string sender = activeProfile?.Nickname ?? "You";
        ChannelMessageReceived?.Invoke(this, new ChatMessage(DateTimeOffset.Now, sender, text));
        return Task.CompletedTask;
    }

    public Task SendDirectMessageAsync(int userId, string text, CancellationToken cancellationToken = default)
    {
        if (Status is ConnectionStatus.Disconnected or ConnectionStatus.Connecting)
        {
            throw new InvalidOperationException("You must be connected before sending a direct message.");
        }

        ChannelMessageReceived?.Invoke(this, new ChatMessage(
            DateTimeOffset.Now,
            $"Direct to User {userId}",
            text,
            IsDirect: true));
        return Task.CompletedTask;
    }

    public Task MoveUserAsync(int userId, string destinationChannelPath, CancellationToken cancellationToken = default)
    {
        if (Status is ConnectionStatus.Disconnected or ConnectionStatus.Connecting)
        {
            throw new InvalidOperationException("You must be logged in before moving a user.");
        }

        string destination = string.IsNullOrWhiteSpace(destinationChannelPath) ? "/" : destinationChannelPath;
        string oldChannel = activeProfile?.ChannelPath ?? "/";
        string nickname = userId == 1 ? activeProfile?.Nickname ?? "You" : $"User {userId}";
        string username = userId == 1 ? activeProfile?.Username ?? string.Empty : string.Empty;

        UserLeft?.Invoke(this, new UserSummary(userId, nickname, username, oldChannel, IsTalking: false, IsAway: false, IsOperator: userId == 1));
        UserJoined?.Invoke(this, new UserSummary(userId, nickname, username, destination, IsTalking: false, IsAway: false, IsOperator: userId == 1));
        ChannelMessageReceived?.Invoke(this, new ChatMessage(
            DateTimeOffset.Now,
            "TeamTalk NG",
            $"Moved {nickname} to {destination}.",
            IsSystem: true));
        return Task.CompletedTask;
    }

    public Task KickUserAsync(int userId, string channelPath, bool fromServer = false, CancellationToken cancellationToken = default)
    {
        if (Status is ConnectionStatus.Disconnected or ConnectionStatus.Connecting)
        {
            throw new InvalidOperationException("You must be logged in before kicking a user.");
        }

        ChannelMessageReceived?.Invoke(this, new ChatMessage(
            DateTimeOffset.Now,
            "TeamTalk NG",
            fromServer ? $"Kicked user {userId} from the server." : $"Kicked user {userId} from {channelPath}.",
            IsSystem: true));
        return Task.CompletedTask;
    }

    public Task BanUserAsync(int userId, string channelPath, bool fromServer = false, CancellationToken cancellationToken = default)
    {
        if (Status is ConnectionStatus.Disconnected or ConnectionStatus.Connecting)
        {
            throw new InvalidOperationException("You must be logged in before banning a user.");
        }

        ChannelMessageReceived?.Invoke(this, new ChatMessage(
            DateTimeOffset.Now,
            "TeamTalk NG",
            fromServer ? $"Banned user {userId} from the server." : $"Banned user {userId} from {channelPath}.",
            IsSystem: true));
        return Task.CompletedTask;
    }

    public Task SetVoiceTransmissionAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        if (Status != ConnectionStatus.InChannel)
        {
            throw new InvalidOperationException("You must be in a channel before transmitting voice.");
        }

        ChannelMessageReceived?.Invoke(this, new ChatMessage(
            DateTimeOffset.Now,
            "TeamTalk NG",
            enabled ? "Voice transmission enabled." : "Voice transmission disabled.",
            IsSystem: true));
        return Task.CompletedTask;
    }

    public Task SetVoiceActivationAsync(bool enabled, int level = 50, CancellationToken cancellationToken = default)
    {
        if (Status != ConnectionStatus.InChannel)
        {
            throw new InvalidOperationException("You must be in a channel before enabling voice activation.");
        }

        ChannelMessageReceived?.Invoke(this, new ChatMessage(
            DateTimeOffset.Now,
            "TeamTalk NG",
            enabled ? "Voice activation enabled." : "Voice activation disabled.",
            IsSystem: true));
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

    public void SimulateUserTalking()
    {
        UserUpdated?.Invoke(this, new UserSummary(44, "Morgan", "morgan", "/Lobby", IsTalking: true, IsAway: false, IsOperator: false));
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
