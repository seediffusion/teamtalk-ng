using TeamTalkNg.Core.TeamTalk;
using TeamTalkNg.TeamTalkSdk.Native;

namespace TeamTalkNg.TeamTalkSdk;

public sealed class TeamTalkSdkSession : ITeamTalkSession, IDisposable
{
    private readonly TeamTalkSdkOptions options;
    private IntPtr instance;

    public TeamTalkSdkSession(TeamTalkSdkOptions options)
    {
        this.options = options;
    }

    public event EventHandler<ConnectionStatus>? ConnectionStatusChanged;
    public event EventHandler<ChatMessage>? ChannelMessageReceived;
    public event EventHandler<UserSummary>? UserJoined;
    public event EventHandler<UserSummary>? UserLeft;

    public ConnectionStatus Status { get; private set; } = ConnectionStatus.Disconnected;

    public TeamTalkSdkAvailability Availability => TeamTalkNativeLibrary.Probe(options);

    public Task ConnectAsync(TeamTalkServerProfile profile, CancellationToken cancellationToken = default)
    {
        TeamTalkSdkAvailability availability = TeamTalkNativeLibrary.ConfigureResolution(options);
        if (!availability.IsAvailable)
        {
            throw new InvalidOperationException(availability.Reason);
        }

        SetStatus(ConnectionStatus.Connecting);
        EnsureInstance();

        int connected = TeamTalkNativeMethods.Connect(
            instance,
            profile.Host,
            profile.TcpPort,
            profile.UdpPort,
            localTcpPort: 0,
            localUdpPort: 0,
            encrypted: profile.IsEncrypted ? 1 : 0);

        if (connected == 0)
        {
            SetStatus(ConnectionStatus.Disconnected);
            throw new InvalidOperationException("TeamTalk SDK refused to start the connection.");
        }

        SetStatus(ConnectionStatus.Connected);
        ChannelMessageReceived?.Invoke(this, new ChatMessage(
            DateTimeOffset.Now,
            "TeamTalk NG",
            "The native TeamTalk SDK connection was started. Event polling and login are the next implementation step.",
            IsSystem: true));

        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        CloseInstance();
        SetStatus(ConnectionStatus.Disconnected);
        return Task.CompletedTask;
    }

    public Task SendChannelMessageAsync(string text, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Channel message sending requires the TeamTalk SDK command/event layer.");
    }

    public void Dispose()
    {
        CloseInstance();
    }

    private void EnsureInstance()
    {
        if (instance != IntPtr.Zero)
        {
            return;
        }

        instance = TeamTalkNativeMethods.InitTeamTalkPoll();
        if (instance == IntPtr.Zero)
        {
            throw new InvalidOperationException("TeamTalk SDK failed to create a client instance.");
        }
    }

    private void CloseInstance()
    {
        if (instance == IntPtr.Zero)
        {
            return;
        }

        TeamTalkNativeMethods.CloseTeamTalk(instance);
        instance = IntPtr.Zero;
    }

    private void SetStatus(ConnectionStatus status)
    {
        if (Status == status)
        {
            return;
        }

        Status = status;
        ConnectionStatusChanged?.Invoke(this, status);
    }

    private void RaiseUserJoined(UserSummary user)
    {
        UserJoined?.Invoke(this, user);
    }

    private void RaiseUserLeft(UserSummary user)
    {
        UserLeft?.Invoke(this, user);
    }
}
