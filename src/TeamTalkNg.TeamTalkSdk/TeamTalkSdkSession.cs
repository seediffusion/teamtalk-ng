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
    private bool audioDevicesInitialized;
    private bool voiceTransmissionEnabled;
    private bool voiceActivationEnabled;
    private int? configuredInputDeviceId;
    private int? configuredOutputDeviceId;
    private int configuredInputVolumePercent = 50;
    private int configuredOutputVolumePercent = 50;

    public TeamTalkSdkSession(TeamTalkSdkOptions options)
    {
        this.options = options;
    }

    public event EventHandler<ConnectionStatus>? ConnectionStatusChanged;
    public event EventHandler<ChatMessage>? ChannelMessageReceived;
    public event EventHandler<ChannelSummary>? ChannelAddedOrUpdated;
    public event EventHandler<int>? ChannelRemoved;
    public event EventHandler<UserSummary>? UserJoined;
    public event EventHandler<UserSummary>? UserUpdated;
    public event EventHandler<UserSummary>? UserLeft;

    public ConnectionStatus Status { get; private set; } = ConnectionStatus.Disconnected;

    public TeamTalkSdkAvailability Availability => TeamTalkNativeLibrary.Probe(options);

    public Task<IReadOnlyList<AudioDeviceSummary>> GetAudioDevicesAsync(CancellationToken cancellationToken = default)
    {
        TeamTalkSdkAvailability availability = TeamTalkNativeLibrary.ConfigureResolution(options);
        if (!availability.IsAvailable)
        {
            return Task.FromResult<IReadOnlyList<AudioDeviceSummary>>([]);
        }

        return Task.FromResult<IReadOnlyList<AudioDeviceSummary>>(ReadAudioDevices());
    }

    public Task SetAudioDevicesAsync(int? inputDeviceId, int? outputDeviceId, CancellationToken cancellationToken = default)
    {
        lock (stateLock)
        {
            configuredInputDeviceId = inputDeviceId;
            configuredOutputDeviceId = outputDeviceId;

            if (instance != IntPtr.Zero && audioDevicesInitialized)
            {
                StopVoiceInput();
                CloseSoundDevices();
            }
        }

        if (instance != IntPtr.Zero && Status is ConnectionStatus.LoggedIn or ConnectionStatus.InChannel)
        {
            InitializeDefaultAudioDevices();
        }

        return Task.CompletedTask;
    }

    public Task SetAudioVolumeAsync(int inputVolumePercent, int outputVolumePercent, CancellationToken cancellationToken = default)
    {
        configuredInputVolumePercent = Math.Clamp(inputVolumePercent, 0, 100);
        configuredOutputVolumePercent = Math.Clamp(outputVolumePercent, 0, 100);

        if (instance == IntPtr.Zero || !audioDevicesInitialized)
        {
            return Task.CompletedTask;
        }

        ApplyConfiguredAudioVolume();
        return Task.CompletedTask;
    }

    public Task SetUserStatusAsync(UserStatusRequest status, CancellationToken cancellationToken = default)
    {
        if (Status is ConnectionStatus.Disconnected or ConnectionStatus.Connecting)
        {
            throw new InvalidOperationException("You must be logged in before changing status.");
        }

        int statusMode = status.IsAway ? StatusMode.Away : StatusMode.Available;
        int commandId;
        lock (stateLock)
        {
            EnsureConnectedInstance();
            commandId = TeamTalkNativeMethods.DoChangeStatus(instance, statusMode, status.Message);
        }

        if (commandId <= 0)
        {
            RaiseSystemMessage("TeamTalk SDK did not accept the status command.");
        }

        return Task.CompletedTask;
    }

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
            audioDevicesInitialized = false;
            voiceTransmissionEnabled = false;
            voiceActivationEnabled = false;
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
                StopVoiceInput();
                CloseSoundDevices();
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

        string parentPath = string.IsNullOrWhiteSpace(request.ParentPath) ? "/" : request.ParentPath;
        int parentId = ResolveChannelId(parentPath);
        if (parentId <= 0)
        {
            throw new InvalidOperationException($"Parent channel {parentPath} was not found.");
        }

        NativeChannel channel = CreateDefaultChannel(parentId);
        channel.WriteName(channelName);
        channel.WriteTopic(request.Topic);
        channel.WritePassword(request.Password);
        channel.HasPassword = string.IsNullOrEmpty(request.Password) ? 0 : 1;
        channel.MaxUsers = Math.Max(0, request.MaxUsers);
        channel.ChannelType = request.IsPermanent
            ? (uint)ChannelType.Permanent
            : (uint)ChannelType.Default;

        int commandId;
        lock (stateLock)
        {
            EnsureConnectedInstance();
            commandId = TeamTalkNativeMethods.DoMakeChannel(instance, ref channel);
        }

        if (commandId <= 0)
        {
            RaiseSystemMessage($"TeamTalk SDK did not accept the create channel command for {channelName}.");
        }

        return Task.CompletedTask;
    }

    public Task RemoveChannelAsync(string channelPath, CancellationToken cancellationToken = default)
    {
        if (Status is ConnectionStatus.Disconnected or ConnectionStatus.Connecting)
        {
            throw new InvalidOperationException("You must be logged in before deleting a channel.");
        }

        int channelId = ResolveChannelId(channelPath);
        if (channelId <= 0)
        {
            throw new InvalidOperationException($"Channel {channelPath} was not found.");
        }

        int rootChannelId;
        lock (stateLock)
        {
            EnsureConnectedInstance();
            rootChannelId = TeamTalkNativeMethods.GetRootChannelId(instance);
        }

        if (channelId == rootChannelId)
        {
            throw new InvalidOperationException("The root channel cannot be deleted.");
        }

        int commandId;
        lock (stateLock)
        {
            EnsureConnectedInstance();
            commandId = TeamTalkNativeMethods.DoRemoveChannel(instance, channelId);
        }

        if (commandId <= 0)
        {
            RaiseSystemMessage($"TeamTalk SDK did not accept the delete command for {channelPath}.");
        }

        return Task.CompletedTask;
    }

    public Task SetVoiceTransmissionAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        if (Status != ConnectionStatus.InChannel)
        {
            throw new InvalidOperationException("You must be in a channel before transmitting voice.");
        }

        EnsureAudioDevicesInitialized();

        int success;
        lock (stateLock)
        {
            EnsureConnectedInstance();
            if (enabled && voiceActivationEnabled)
            {
                TeamTalkNativeMethods.EnableVoiceActivation(instance, 0);
                voiceActivationEnabled = false;
            }

            success = TeamTalkNativeMethods.EnableVoiceTransmission(instance, enabled ? 1 : 0);
            if (success != 0)
            {
                voiceTransmissionEnabled = enabled;
            }
        }

        if (success == 0)
        {
            throw new InvalidOperationException(enabled
                ? "TeamTalk could not start voice transmission."
                : "TeamTalk could not stop voice transmission.");
        }

        return Task.CompletedTask;
    }

    public Task SetVoiceActivationAsync(bool enabled, int level = 50, CancellationToken cancellationToken = default)
    {
        if (Status != ConnectionStatus.InChannel)
        {
            throw new InvalidOperationException("You must be in a channel before enabling voice activation.");
        }

        EnsureAudioDevicesInitialized();

        int clampedLevel = Math.Clamp(level, 0, 100);
        int success;
        lock (stateLock)
        {
            EnsureConnectedInstance();
            if (enabled && voiceTransmissionEnabled)
            {
                TeamTalkNativeMethods.EnableVoiceTransmission(instance, 0);
                voiceTransmissionEnabled = false;
            }

            if (enabled && TeamTalkNativeMethods.SetVoiceActivationLevel(instance, clampedLevel) == 0)
            {
                throw new InvalidOperationException("TeamTalk could not set the voice activation level.");
            }

            success = TeamTalkNativeMethods.EnableVoiceActivation(instance, enabled ? 1 : 0);
            if (success != 0)
            {
                voiceActivationEnabled = enabled;
            }
        }

        if (success == 0)
        {
            throw new InvalidOperationException(enabled
                ? "TeamTalk could not enable voice activation."
                : "TeamTalk could not disable voice activation.");
        }

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

    public Task SendDirectMessageAsync(int userId, string text, CancellationToken cancellationToken = default)
    {
        if (Status is ConnectionStatus.Disconnected or ConnectionStatus.Connecting)
        {
            throw new InvalidOperationException("You must be connected before sending a direct message.");
        }

        if (userId <= 0)
        {
            throw new InvalidOperationException("Select a user before sending a direct message.");
        }

        NativeTextMessage message = default;
        message.MessageType = TextMsgType.User;
        message.ToUserId = userId;
        message.WriteMessage(text);

        int commandId;
        lock (stateLock)
        {
            EnsureConnectedInstance();
            commandId = TeamTalkNativeMethods.DoTextMessage(instance, ref message);
        }

        if (commandId <= 0)
        {
            RaiseSystemMessage("TeamTalk SDK did not accept the direct message command.");
        }
        else
        {
            ChannelMessageReceived?.Invoke(this, new ChatMessage(
                DateTimeOffset.Now,
                $"Direct to User {userId}",
                text,
                IsDirect: true));
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

            StopVoiceInput();
            CloseSoundDevices();
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
                InitializeDefaultAudioDevices();
                JoinConfiguredChannel();
                break;
            case ClientEvent.CommandMyselfLoggedOut:
            case ClientEvent.CommandMyselfKicked:
                HandleDisconnected("Logged out.");
                break;
            case ClientEvent.CommandUserJoined:
                DispatchUserJoined(message.User);
                break;
            case ClientEvent.CommandUserUpdate:
                DispatchUserUpdated(message.User);
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

        int channelId = ResolveChannelId(normalizedPath);

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

    private int ResolveChannelId(string channelPath)
    {
        string normalizedPath = string.IsNullOrWhiteSpace(channelPath) ? "/" : channelPath;
        lock (stateLock)
        {
            EnsureConnectedInstance();
            return normalizedPath == "/"
                ? TeamTalkNativeMethods.GetRootChannelId(instance)
                : TeamTalkNativeMethods.GetChannelIdFromPath(instance, normalizedPath);
        }
    }

    private NativeChannel CreateDefaultChannel(int parentId)
    {
        NativeChannel channel = default;
        channel.ParentId = parentId;
        channel.MaxUsers = 0;

        lock (stateLock)
        {
            if (TeamTalkNativeMethods.GetChannel(instance, parentId, out NativeChannel parentChannel) != 0)
            {
                channel.AudioCodec = parentChannel.AudioCodec;
                channel.AudioConfig = parentChannel.AudioConfig;
                if (channel.AudioCodec.Codec != Codec.NoCodec)
                {
                    return channel;
                }
            }
        }

        channel.AudioCodec.Codec = Codec.Opus;
        channel.AudioCodec.Value.Opus.SampleRate = 48000;
        channel.AudioCodec.Value.Opus.Channels = 1;
        channel.AudioCodec.Value.Opus.Application = 2048;
        channel.AudioCodec.Value.Opus.Complexity = 5;
        channel.AudioCodec.Value.Opus.BitRate = 32000;
        channel.AudioCodec.Value.Opus.Vbr = 1;
        channel.AudioCodec.Value.Opus.TransmitIntervalMilliseconds = 20;
        channel.AudioCodec.Value.Opus.FrameSizeMilliseconds = 20;
        return channel;
    }

    private void InitializeDefaultAudioDevices()
    {
        if (instance == IntPtr.Zero || audioDevicesInitialized)
        {
            return;
        }

        if (TeamTalkNativeMethods.GetDefaultSoundDevices(out int inputDeviceId, out int outputDeviceId) == 0)
        {
            RaiseSystemMessage("TeamTalk could not find default audio devices. Voice features are unavailable.");
            return;
        }

        inputDeviceId = configuredInputDeviceId ?? inputDeviceId;
        outputDeviceId = configuredOutputDeviceId ?? outputDeviceId;

        int inputReady;
        int outputReady;
        lock (stateLock)
        {
            if (instance == IntPtr.Zero)
            {
                return;
            }

            inputReady = TeamTalkNativeMethods.InitSoundInputDevice(instance, inputDeviceId);
            outputReady = TeamTalkNativeMethods.InitSoundOutputDevice(instance, outputDeviceId);
            audioDevicesInitialized = inputReady != 0 && outputReady != 0;
        }

        if (!audioDevicesInitialized)
        {
            CloseSoundDevices();
            RaiseSystemMessage("TeamTalk could not initialize the default microphone and speaker. Voice features are unavailable.");
            return;
        }

        ApplyConfiguredAudioVolume();
    }

    private void EnsureAudioDevicesInitialized()
    {
        if (!audioDevicesInitialized)
        {
            InitializeDefaultAudioDevices();
        }

        if (!audioDevicesInitialized)
        {
            throw new InvalidOperationException("Audio devices are not ready.");
        }
    }

    private void StopVoiceInput()
    {
        if (instance == IntPtr.Zero)
        {
            return;
        }

        TeamTalkNativeMethods.EnableVoiceTransmission(instance, 0);
        TeamTalkNativeMethods.EnableVoiceActivation(instance, 0);
        voiceTransmissionEnabled = false;
        voiceActivationEnabled = false;
    }

    private void CloseSoundDevices()
    {
        if (instance == IntPtr.Zero)
        {
            return;
        }

        TeamTalkNativeMethods.CloseSoundInputDevice(instance);
        TeamTalkNativeMethods.CloseSoundOutputDevice(instance);
        audioDevicesInitialized = false;
    }

    private static IReadOnlyList<AudioDeviceSummary> ReadAudioDevices()
    {
        TeamTalkNativeMethods.RestartSoundSystem();

        int count = 0;
        if (TeamTalkNativeMethods.GetSoundDevices(IntPtr.Zero, ref count) == 0 || count <= 0)
        {
            return [];
        }

        int size = Marshal.SizeOf<NativeSoundDevice>();
        IntPtr buffer = Marshal.AllocHGlobal(size * count);
        try
        {
            if (TeamTalkNativeMethods.GetSoundDevices(buffer, ref count) == 0 || count <= 0)
            {
                return [];
            }

            TeamTalkNativeMethods.GetDefaultSoundDevices(out int defaultInputId, out int defaultOutputId);
            List<AudioDeviceSummary> devices = [];
            for (int index = 0; index < count; index++)
            {
                IntPtr deviceAddress = IntPtr.Add(buffer, index * size);
                NativeSoundDevice nativeDevice = Marshal.PtrToStructure<NativeSoundDevice>(deviceAddress);
                bool supportsInput = nativeDevice.MaxInputChannels > 0;
                bool supportsOutput = nativeDevice.MaxOutputChannels > 0;
                if (!supportsInput && !supportsOutput)
                {
                    continue;
                }

                string name = nativeDevice.ReadName();
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = $"Audio device {nativeDevice.DeviceId}";
                }

                devices.Add(new AudioDeviceSummary(
                    nativeDevice.DeviceId,
                    name,
                    supportsInput,
                    supportsOutput,
                    nativeDevice.DeviceId == defaultInputId,
                    nativeDevice.DeviceId == defaultOutputId));
            }

            return devices;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private void ApplyConfiguredAudioVolume()
    {
        int inputGainLevel = PercentToTeamTalkSoundLevel(configuredInputVolumePercent, SoundLevel.GainDefault, SoundLevel.GainMax);
        int outputVolume = PercentToTeamTalkSoundLevel(configuredOutputVolumePercent, SoundLevel.VolumeDefault, SoundLevel.VolumeMax);

        int inputReady;
        int outputReady;
        lock (stateLock)
        {
            if (instance == IntPtr.Zero)
            {
                return;
            }

            inputReady = TeamTalkNativeMethods.SetSoundInputGainLevel(instance, inputGainLevel);
            outputReady = TeamTalkNativeMethods.SetSoundOutputVolume(instance, outputVolume);
        }

        if (inputReady == 0)
        {
            RaiseSystemMessage("TeamTalk could not apply the microphone volume.");
        }

        if (outputReady == 0)
        {
            RaiseSystemMessage("TeamTalk could not apply the speaker volume.");
        }
    }

    private static int PercentToTeamTalkSoundLevel(int percent, int defaultLevel, int maxLevel)
    {
        int clampedPercent = Math.Clamp(percent, 0, 100);
        if (clampedPercent <= 50)
        {
            return (int)Math.Round(defaultLevel * (clampedPercent / 50.0));
        }

        int boostedLevel = defaultLevel + (int)Math.Round(defaultLevel * ((clampedPercent - 50) / 50.0));
        return Math.Clamp(boostedLevel, 0, maxLevel);
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

    private void DispatchUserUpdated(NativeUser user)
    {
        UserUpdated?.Invoke(this, CreateUserSummary(user, user.ChannelId));
    }

    private void DispatchTextMessage(NativeTextMessage textMessage)
    {
        string sender = textMessage.ReadFromUsername();
        if (string.IsNullOrWhiteSpace(sender))
        {
            sender = $"User {textMessage.FromUserId}";
        }

        if (textMessage.MessageType == TextMsgType.Channel)
        {
            ChannelMessageReceived?.Invoke(this, new ChatMessage(
                DateTimeOffset.Now,
                sender,
                textMessage.ReadMessage()));
        }
        else if (textMessage.MessageType == TextMsgType.User)
        {
            ChannelMessageReceived?.Invoke(this, new ChatMessage(
                DateTimeOffset.Now,
                $"Direct from {sender}",
                textMessage.ReadMessage(),
                IsDirect: true));
        }
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
            IsPermanent: (channel.ChannelType & (uint)ChannelType.Permanent) != 0));
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
            isOperator,
            user.ReadStatusMessage());
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
