using System.Runtime.InteropServices;
using TeamTalkNg.Core.TeamTalk;
using TeamTalkNg.TeamTalkSdk.Native;

namespace TeamTalkNg.TeamTalkSdk;

public sealed class TeamTalkSdkSession : ITeamTalkSession, IDisposable
{
    private const string ClientName = "TeamTalk NG";
    private const int MessageWaitMilliseconds = 100;

    private readonly TeamTalkSdkOptions options;
    private readonly Lock stateLock = new();
    private IntPtr instance;
    private CancellationTokenSource? pollingCancellation;
    private Task? pollingTask;
    private TeamTalkServerProfile? activeProfile;
    private int currentChannelId;
    private int myUserId;
    private int messageBufferSize;

    public TeamTalkSdkSession(TeamTalkSdkOptions options)
    {
        this.options = options;
    }

    public event EventHandler<ConnectionStatus>? ConnectionStatusChanged;
    public event EventHandler<ChatMessage>? ChannelMessageReceived;
    public event EventHandler<ChannelSummary>? ChannelAddedOrUpdated;
    public event EventHandler<int>? ChannelRemoved;
    public event EventHandler<UserSummary>? UserJoined;
    public event EventHandler<UserSummary>? UserLeft;

    public ConnectionStatus Status { get; private set; } = ConnectionStatus.Disconnected;

    public TeamTalkSdkAvailability Availability => TeamTalkNativeLibrary.Probe(options);

    public async Task ConnectAsync(TeamTalkServerProfile profile, CancellationToken cancellationToken = default)
    {
        TeamTalkSdkAvailability availability = TeamTalkNativeLibrary.ConfigureResolution(options);
        if (!availability.IsAvailable)
        {
            throw new InvalidOperationException(availability.Reason);
        }

        await StopPollingAsync().ConfigureAwait(false);

        lock (stateLock)
        {
            activeProfile = profile;
            currentChannelId = 0;
            myUserId = 0;
        }

        SetStatus(ConnectionStatus.Connecting);
        EnsureInstance();
        EnsureMessageBufferSize();

        int connected;
        lock (stateLock)
        {
            connected = TeamTalkNativeMethods.Connect(
                instance,
                profile.Host,
                profile.TcpPort,
                profile.UdpPort,
                localTcpPort: 0,
                localUdpPort: 0,
                encrypted: profile.IsEncrypted ? 1 : 0);
        }

        if (connected == 0)
        {
            SetStatus(ConnectionStatus.Disconnected);
            throw new InvalidOperationException("TeamTalk SDK refused to start the connection.");
        }

        pollingCancellation = new CancellationTokenSource();
        pollingTask = Task.Run(() => PollMessagesAsync(pollingCancellation.Token), CancellationToken.None);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await StopPollingAsync().ConfigureAwait(false);

        lock (stateLock)
        {
            if (instance != IntPtr.Zero)
            {
                TeamTalkNativeMethods.Disconnect(instance);
            }
        }

        CloseInstance();
        SetStatus(ConnectionStatus.Disconnected);
    }

    public Task JoinChannelAsync(string channelPath, string password = "", CancellationToken cancellationToken = default)
    {
        if (Status is ConnectionStatus.Disconnected or ConnectionStatus.Connecting)
        {
            throw new InvalidOperationException("You must be logged in before joining a channel.");
        }

        JoinChannel(channelPath, password);
        return Task.CompletedTask;
    }

    public Task SendChannelMessageAsync(string text, CancellationToken cancellationToken = default)
    {
        if (Status != ConnectionStatus.InChannel || currentChannelId <= 0)
        {
            throw new InvalidOperationException("You must be in a channel before sending a channel message.");
        }

        NativeTextMessage message = default;
        message.MessageType = TextMsgType.Channel;
        message.ChannelId = currentChannelId;
        message.WriteMessage(text);

        int commandId;
        lock (stateLock)
        {
            EnsureConnectedInstance();
            commandId = TeamTalkNativeMethods.DoTextMessage(instance, ref message);
        }

        if (commandId <= 0)
        {
            RaiseSystemMessage("TeamTalk SDK did not accept the channel message command.");
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        StopPollingAsync().GetAwaiter().GetResult();
        CloseInstance();
    }

    internal void DispatchMessageForTest(TeamTalkMessage message)
    {
        DispatchMessage(message);
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

    private void EnsureConnectedInstance()
    {
        if (instance == IntPtr.Zero)
        {
            throw new InvalidOperationException("TeamTalk SDK client instance is not active.");
        }
    }

    private void EnsureMessageBufferSize()
    {
        if (messageBufferSize > 0)
        {
            return;
        }

        int nativeSize = TeamTalkNativeMethods.DebugSizeOf(TTType.TTMessage);
        messageBufferSize = nativeSize > 0 ? nativeSize : 65536;
    }

    private void CloseInstance()
    {
        lock (stateLock)
        {
            if (instance == IntPtr.Zero)
            {
                return;
            }

            TeamTalkNativeMethods.CloseTeamTalk(instance);
            instance = IntPtr.Zero;
        }
    }

    private async Task StopPollingAsync()
    {
        CancellationTokenSource? cancellation = pollingCancellation;
        Task? task = pollingTask;

        pollingCancellation = null;
        pollingTask = null;

        if (cancellation is null)
        {
            return;
        }

        await cancellation.CancelAsync().ConfigureAwait(false);
        try
        {
            if (task is not null)
            {
                await task.ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            cancellation.Dispose();
        }
    }

    private async Task PollMessagesAsync(CancellationToken cancellationToken)
    {
        IntPtr buffer = Marshal.AllocHGlobal(messageBufferSize);
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                int waitMilliseconds = MessageWaitMilliseconds;
                int hasMessage;
                lock (stateLock)
                {
                    if (instance == IntPtr.Zero)
                    {
                        return;
                    }

                    hasMessage = TeamTalkNativeMethods.GetMessage(instance, buffer, ref waitMilliseconds);
                }

                if (hasMessage != 0)
                {
                    DispatchMessage(TeamTalkMessageParser.Parse(buffer));
                }
                else
                {
                    await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private void DispatchMessage(TeamTalkMessage message)
    {
        switch (message.ClientEvent)
        {
            case ClientEvent.ConnectionSuccess:
                HandleConnectionSuccess();
                break;
            case ClientEvent.ConnectionFailed:
                HandleDisconnected("Connection failed.");
                break;
            case ClientEvent.ConnectionCryptError:
                HandleDisconnected("Encrypted connection failed.");
                break;
            case ClientEvent.ConnectionLost:
                HandleDisconnected("Connection lost.");
                break;
            case ClientEvent.CommandError:
                RaiseSystemMessage(ReadErrorMessage(message));
                break;
            case ClientEvent.CommandMyselfLoggedIn:
                myUserId = message.Source;
                SetStatus(ConnectionStatus.LoggedIn);
                JoinConfiguredChannel();
                break;
            case ClientEvent.CommandMyselfLoggedOut:
            case ClientEvent.CommandMyselfKicked:
                HandleDisconnected("Logged out.");
                break;
            case ClientEvent.CommandUserJoined:
                DispatchUserJoined(message.User);
                break;
            case ClientEvent.CommandUserLeft:
                DispatchUserLeft(message.User, message.Source);
                break;
            case ClientEvent.CommandUserTextMessage:
                DispatchTextMessage(message.TextMessage);
                break;
            case ClientEvent.CommandChannelNew:
            case ClientEvent.CommandChannelUpdate:
                DispatchChannelAddedOrUpdated(message.Channel);
                break;
            case ClientEvent.CommandChannelRemove:
                ChannelRemoved?.Invoke(this, message.Source);
                break;
        }
    }

    private void HandleConnectionSuccess()
    {
        TeamTalkServerProfile? profile = activeProfile;
        if (profile is null)
        {
            RaiseSystemMessage("Connected, but no active server profile is available for login.");
            SetStatus(ConnectionStatus.Connected);
            return;
        }

        SetStatus(ConnectionStatus.Connected);

        string nickname = string.IsNullOrWhiteSpace(profile.Nickname)
            ? Environment.UserName
            : profile.Nickname;

        int commandId;
        lock (stateLock)
        {
            EnsureConnectedInstance();
            commandId = TeamTalkNativeMethods.DoLoginEx(
                instance,
                nickname,
                profile.Username,
                profile.Password,
                ClientName);
        }

        if (commandId <= 0)
        {
            RaiseSystemMessage("TeamTalk SDK did not accept the login command.");
        }
    }

    private void JoinConfiguredChannel()
    {
        TeamTalkServerProfile? profile = activeProfile;
        string channelPath = string.IsNullOrWhiteSpace(profile?.ChannelPath) ? "/" : profile.ChannelPath!;

        if (instance == IntPtr.Zero)
        {
            return;
        }

        JoinChannel(channelPath, profile?.ChannelPassword ?? string.Empty);
    }

    private void JoinChannel(string channelPath, string password)
    {
        string normalizedPath = string.IsNullOrWhiteSpace(channelPath) ? "/" : channelPath;

        int channelId;
        lock (stateLock)
        {
            EnsureConnectedInstance();
            channelId = TeamTalkNativeMethods.GetChannelIdFromPath(instance, normalizedPath);
        }

        if (channelId <= 0)
        {
            RaiseSystemMessage($"Channel {normalizedPath} was not found.");
            return;
        }

        int commandId;
        lock (stateLock)
        {
            EnsureConnectedInstance();
            commandId = TeamTalkNativeMethods.DoJoinChannelById(instance, channelId, password);
        }

        if (commandId <= 0)
        {
            RaiseSystemMessage($"TeamTalk SDK did not accept the join command for {normalizedPath}.");
        }
    }

    private void DispatchUserJoined(NativeUser user)
    {
        UserSummary summary = CreateUserSummary(user, user.ChannelId);
        UserJoined?.Invoke(this, summary);

        if (user.UserId == myUserId)
        {
            currentChannelId = user.ChannelId;
            SetStatus(ConnectionStatus.InChannel);
        }
    }

    private void DispatchUserLeft(NativeUser user, int previousChannelId)
    {
        UserLeft?.Invoke(this, CreateUserSummary(user, previousChannelId));

        if (user.UserId == myUserId)
        {
            currentChannelId = 0;
            SetStatus(ConnectionStatus.LoggedIn);
        }
    }

    private void DispatchTextMessage(NativeTextMessage textMessage)
    {
        if (textMessage.MessageType != TextMsgType.Channel)
        {
            return;
        }

        string sender = textMessage.ReadFromUsername();
        if (string.IsNullOrWhiteSpace(sender))
        {
            sender = $"User {textMessage.FromUserId}";
        }

        ChannelMessageReceived?.Invoke(this, new ChatMessage(
            DateTimeOffset.Now,
            sender,
            textMessage.ReadMessage()));
    }

    private void DispatchChannelAddedOrUpdated(NativeChannel channel)
    {
        string channelName = channel.ReadName();
        string channelPath = GetChannelPath(channel.ChannelId);
        if (string.IsNullOrWhiteSpace(channelName))
        {
            channelName = channelPath == "/" ? "Root" : channelPath.Trim('/').Split('/').LastOrDefault() ?? "Channel";
        }

        ChannelAddedOrUpdated?.Invoke(this, new ChannelSummary(
            channel.ChannelId,
            channelName,
            channelPath,
            UserCount: 0,
            IsProtected: channel.HasPassword != 0,
            IsPermanent: true));
    }

    private UserSummary CreateUserSummary(NativeUser user, int channelId)
    {
        string nickname = user.ReadNickname();
        if (string.IsNullOrWhiteSpace(nickname))
        {
            nickname = user.ReadUsername();
        }

        if (string.IsNullOrWhiteSpace(nickname))
        {
            nickname = $"User {user.UserId}";
        }

        uint userState = user.UserState;
        bool isTalking = (userState & 0x00000001) != 0;
        bool isAway = (user.StatusMode & 0x00000001) != 0;
        bool isOperator = currentChannelId > 0 && channelId == currentChannelId && user.UserId == myUserId;

        return new UserSummary(
            user.UserId,
            nickname,
            user.ReadUsername(),
            GetChannelPath(channelId),
            isTalking,
            isAway,
            isOperator);
    }

    private string GetChannelPath(int channelId)
    {
        if (channelId <= 0 || instance == IntPtr.Zero)
        {
            return "/";
        }

        char[] buffer = new char[NativeConstants.StringLength];
        int success;
        lock (stateLock)
        {
            if (instance == IntPtr.Zero)
            {
                return "/";
            }

            success = TeamTalkNativeMethods.GetChannelPath(instance, channelId, buffer);
        }

        if (success == 0)
        {
            return "/";
        }

        int length = Array.IndexOf(buffer, '\0');
        return new string(buffer, 0, length >= 0 ? length : buffer.Length);
    }

    private void HandleDisconnected(string reason)
    {
        RaiseSystemMessage(reason);
        currentChannelId = 0;
        myUserId = 0;
        CloseInstance();
        SetStatus(ConnectionStatus.Disconnected);
    }

    private void RaiseSystemMessage(string text)
    {
        ChannelMessageReceived?.Invoke(this, new ChatMessage(
            DateTimeOffset.Now,
            "TeamTalk NG",
            text,
            IsSystem: true));
    }

    private static string ReadErrorMessage(TeamTalkMessage message)
    {
        string error = message.ClientError.ReadMessage();
        return string.IsNullOrWhiteSpace(error)
            ? $"TeamTalk command failed with error {message.ClientError.ErrorNumber}."
            : error;
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
