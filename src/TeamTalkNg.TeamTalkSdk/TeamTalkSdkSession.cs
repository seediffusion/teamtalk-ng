using System.Runtime.InteropServices;
using TeamTalkNg.Core.TeamTalk;
using TeamTalkNg.TeamTalkSdk.Native;

namespace TeamTalkNg.TeamTalkSdk;

public sealed class TeamTalkSdkSession : ITeamTalkSession, IDisposable
{
    private const string ClientName = "TeamTalk NG";
    private const int MessageWaitMilliseconds = 100;
    private static readonly TimeSpan ConnectionProgressTimeout = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan ServerStatisticsTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan BannedUsersTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan UserAccountsTimeout = TimeSpan.FromSeconds(10);

    private readonly TeamTalkSdkOptions options;
    private readonly Lock stateLock = new();
    private IntPtr instance;
    private CancellationTokenSource? pollingCancellation;
    private Task? pollingTask;
    private int connectionAttemptId;
    private int loginCommandId;
    private int serverStatisticsCommandId;
    private TaskCompletionSource<ServerStatisticsSummary>? pendingServerStatistics;
    private int bannedUsersCommandId;
    private TaskCompletionSource<IReadOnlyList<BannedUserSummary>>? pendingBannedUsers;
    private List<BannedUserSummary>? pendingBannedUserItems;
    private int userAccountsCommandId;
    private TaskCompletionSource<IReadOnlyList<UserAccountSummary>>? pendingUserAccounts;
    private List<UserAccountSummary>? pendingUserAccountItems;
    private TeamTalkServerProfile? activeProfile;
    private int currentChannelId;
    private int myUserId;
    private int messageBufferSize;
    private bool soundInputInitialized;
    private bool soundOutputInitialized;
    private bool soundDuplexInitialized;
    private bool voiceTransmissionEnabled;
    private bool voiceActivationEnabled;
    private bool videoCaptureInitialized;
    private bool videoCaptureTransmitting;
    private bool desktopSharing;
    private int? configuredInputDeviceId;
    private int? configuredOutputDeviceId;
    private int configuredInputVolumePercent = 50;
    private int configuredOutputVolumePercent = 50;
    private AudioProcessingSettings audioProcessingSettings = new(
        EnableNoiseSuppression: true,
        EnableEchoCancellation: true,
        EnableAutomaticGainControl: false);
    private readonly Dictionary<int, string> userDisplayNames = [];

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
    public event EventHandler<FileTransferSummary>? FileTransferUpdated;
    public event EventHandler<MediaFrameSummary>? MediaFrameReceived;

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

            if (instance != IntPtr.Zero && (soundInputInitialized || soundOutputInitialized))
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

        if (instance == IntPtr.Zero || (!soundInputInitialized && !soundOutputInitialized))
        {
            return Task.CompletedTask;
        }

        ApplyConfiguredAudioVolume();
        return Task.CompletedTask;
    }

    public Task SetAudioProcessingAsync(AudioProcessingSettings settings, CancellationToken cancellationToken = default)
    {
        audioProcessingSettings = settings;
        if (instance == IntPtr.Zero || (!soundInputInitialized && !soundOutputInitialized))
        {
            return Task.CompletedTask;
        }

        lock (stateLock)
        {
            if (instance != IntPtr.Zero && (soundInputInitialized || soundOutputInitialized))
            {
                StopVoiceInput();
                CloseSoundDevices();
            }
        }

        if (Status is ConnectionStatus.LoggedIn or ConnectionStatus.InChannel)
        {
            InitializeDefaultAudioDevices();
        }

        return Task.CompletedTask;
    }

    public Task<AudioInputLevelSummary> GetAudioInputLevelAsync(CancellationToken cancellationToken = default)
    {
        if (Status != ConnectionStatus.InChannel)
        {
            throw new InvalidOperationException("You must be in a channel before monitoring microphone input.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        EnsureSoundInputInitialized();

        int inputLevel;
        int activationLevel;
        lock (stateLock)
        {
            EnsureConnectedInstance();
            inputLevel = TeamTalkNativeMethods.GetSoundInputLevel(instance);
            activationLevel = TeamTalkNativeMethods.GetVoiceActivationLevel(instance);
        }

        return Task.FromResult(new AudioInputLevelSummary(
            Math.Clamp(inputLevel, SoundLevel.VuMin, SoundLevel.VuMax),
            Math.Clamp(activationLevel, SoundLevel.VuMin, SoundLevel.VuMax)));
    }

    public Task<IReadOnlyList<VideoCaptureDeviceSummary>> GetVideoCaptureDevicesAsync(CancellationToken cancellationToken = default)
    {
        TeamTalkSdkAvailability availability = TeamTalkNativeLibrary.ConfigureResolution(options);
        if (!availability.IsAvailable)
        {
            return Task.FromResult<IReadOnlyList<VideoCaptureDeviceSummary>>([]);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<VideoCaptureDeviceSummary>>(ReadVideoCaptureDevices());
    }

    public Task StartVideoCaptureAsync(string deviceId, VideoCaptureFormatSummary format, CancellationToken cancellationToken = default)
    {
        if (Status != ConnectionStatus.InChannel)
        {
            throw new InvalidOperationException("You must be in a channel before transmitting video.");
        }

        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new InvalidOperationException("Select a camera before starting video.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        NativeVideoFormat nativeFormat = CreateNativeVideoFormat(format);
        NativeVideoCodec codec = CreateDefaultVideoCodec();
        int initialized;
        int started;
        lock (stateLock)
        {
            EnsureConnectedInstance();
            if (videoCaptureTransmitting)
            {
                TeamTalkNativeMethods.StopVideoCaptureTransmission(instance);
                videoCaptureTransmitting = false;
            }

            if (videoCaptureInitialized)
            {
                TeamTalkNativeMethods.CloseVideoCaptureDevice(instance);
                videoCaptureInitialized = false;
            }

            initialized = TeamTalkNativeMethods.InitVideoCaptureDevice(instance, deviceId, ref nativeFormat);
            videoCaptureInitialized = initialized != 0;
            started = initialized == 0
                ? 0
                : TeamTalkNativeMethods.StartVideoCaptureTransmission(instance, ref codec);
            videoCaptureTransmitting = started != 0;
        }

        if (initialized == 0)
        {
            throw new InvalidOperationException("TeamTalk could not initialize the selected camera.");
        }

        if (started == 0)
        {
            throw new InvalidOperationException("TeamTalk could not start video transmission.");
        }

        RaiseSystemMessage("Video transmission started.");
        return Task.CompletedTask;
    }

    public Task StopVideoCaptureAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        StopVideoCapture();
        RaiseSystemMessage("Video transmission stopped.");
        return Task.CompletedTask;
    }

    public Task StartDesktopShareAsync(DesktopShareSource source, CancellationToken cancellationToken = default)
    {
        if (Status != ConnectionStatus.InChannel)
        {
            throw new InvalidOperationException("You must be in a channel before sharing your desktop.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        IntPtr windowHandle = source == DesktopShareSource.ActiveWindow
            ? TeamTalkNativeMethods.WindowsGetDesktopActiveHwnd()
            : TeamTalkNativeMethods.WindowsGetDesktopHwnd();
        if (windowHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException("TeamTalk could not find the requested desktop window.");
        }

        int result;
        lock (stateLock)
        {
            EnsureConnectedInstance();
            result = TeamTalkNativeMethods.SendDesktopWindowFromHwnd(instance, windowHandle, BitmapFormat.Rgb32, DesktopProtocol.Zlib1);
            desktopSharing = result > 0;
        }

        if (result <= 0)
        {
            throw new InvalidOperationException(result == 0
                ? "TeamTalk did not detect a desktop change to send yet."
                : "TeamTalk could not start desktop sharing.");
        }

        RaiseSystemMessage(source == DesktopShareSource.ActiveWindow
            ? "Active window sharing started."
            : "Desktop sharing started.");
        return Task.CompletedTask;
    }

    public Task StopDesktopShareAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        StopDesktopSharing();
        RaiseSystemMessage("Desktop sharing stopped.");
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

        int commandId;
        lock (stateLock)
        {
            EnsureConnectedInstance();
            commandId = TeamTalkNativeMethods.DoChangeNickname(instance, trimmedNickname);
        }

        if (commandId <= 0)
        {
            RaiseSystemMessage("TeamTalk SDK did not accept the nickname command.");
        }

        return Task.CompletedTask;
    }

    public Task SetUserAudioSettingsAsync(UserAudioSettingsRequest request, CancellationToken cancellationToken = default)
    {
        if (Status is ConnectionStatus.Disconnected or ConnectionStatus.Connecting)
        {
            throw new InvalidOperationException("You must be connected before changing user audio settings.");
        }

        if (request.UserId <= 0)
        {
            throw new InvalidOperationException("Select a user before changing audio settings.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        int voiceVolume = UserVolumePercentToTeamTalkLevel(request.VoiceVolumePercent);
        int volumeReady;
        int muteReady;
        lock (stateLock)
        {
            EnsureConnectedInstance();
            volumeReady = TeamTalkNativeMethods.SetUserVolume(instance, request.UserId, StreamType.Voice, voiceVolume);
            muteReady = TeamTalkNativeMethods.SetUserMute(instance, request.UserId, StreamType.Voice, request.IsVoiceMuted ? 1 : 0);
        }

        if (volumeReady == 0)
        {
            throw new InvalidOperationException("TeamTalk could not set the selected user's voice volume.");
        }

        if (muteReady == 0)
        {
            throw new InvalidOperationException("TeamTalk could not set the selected user's voice mute state.");
        }

        return Task.CompletedTask;
    }

    public Task<ServerInformationSummary> GetServerInformationAsync(CancellationToken cancellationToken = default)
    {
        if (Status is not (ConnectionStatus.LoggedIn or ConnectionStatus.InChannel))
        {
            throw new InvalidOperationException("You must be logged in before viewing server information.");
        }

        NativeServerProperties properties;
        int success;
        lock (stateLock)
        {
            EnsureConnectedInstance();
            success = TeamTalkNativeMethods.GetServerProperties(instance, out properties);
        }

        if (success == 0)
        {
            throw new InvalidOperationException("TeamTalk server information is not available.");
        }

        return Task.FromResult(CreateServerInformationSummary(properties));
    }

    private static ServerInformationSummary CreateServerInformationSummary(NativeServerProperties properties)
    {
        return new ServerInformationSummary(
            properties.ReadServerName(),
            properties.ReadMotd(),
            properties.MaxUsers,
            properties.TcpPort,
            properties.UdpPort,
            properties.UserTimeout,
            properties.ReadServerVersion(),
            properties.ReadServerProtocolVersion(),
            properties.LoginDelayMilliseconds);
    }

    public async Task<ServerStatisticsSummary> GetServerStatisticsAsync(CancellationToken cancellationToken = default)
    {
        if (Status is not (ConnectionStatus.LoggedIn or ConnectionStatus.InChannel))
        {
            throw new InvalidOperationException("You must be logged in before viewing server statistics.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var request = new TaskCompletionSource<ServerStatisticsSummary>(TaskCreationOptions.RunContinuationsAsynchronously);
        int commandId;
        lock (stateLock)
        {
            EnsureConnectedInstance();
            if (pendingServerStatistics is not null)
            {
                throw new InvalidOperationException("A server statistics request is already in progress.");
            }

            commandId = TeamTalkNativeMethods.DoQueryServerStats(instance);
            if (commandId <= 0)
            {
                throw new InvalidOperationException("TeamTalk SDK did not accept the server statistics command.");
            }

            serverStatisticsCommandId = commandId;
            pendingServerStatistics = request;
        }

        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(ServerStatisticsTimeout);

        try
        {
            return await request.Task.WaitAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException("TeamTalk server statistics did not arrive in time.");
        }
        finally
        {
            ClearPendingServerStatistics(request);
        }
    }

    public async Task<IReadOnlyList<BannedUserSummary>> GetBannedUsersAsync(CancellationToken cancellationToken = default)
    {
        if (Status is not (ConnectionStatus.LoggedIn or ConnectionStatus.InChannel))
        {
            throw new InvalidOperationException("You must be logged in before viewing banned users.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var request = new TaskCompletionSource<IReadOnlyList<BannedUserSummary>>(TaskCreationOptions.RunContinuationsAsynchronously);
        int commandId;
        lock (stateLock)
        {
            EnsureConnectedInstance();
            if (pendingBannedUsers is not null)
            {
                throw new InvalidOperationException("A banned users request is already in progress.");
            }

            commandId = TeamTalkNativeMethods.DoListBans(instance, channelId: 0, index: 0, count: 1000);
            if (commandId <= 0)
            {
                throw new InvalidOperationException("TeamTalk SDK did not accept the banned users command.");
            }

            bannedUsersCommandId = commandId;
            pendingBannedUserItems = [];
            pendingBannedUsers = request;
        }

        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(BannedUsersTimeout);

        try
        {
            return await request.Task.WaitAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException("TeamTalk banned users did not arrive in time.");
        }
        finally
        {
            ClearPendingBannedUsers(request);
        }
    }

    public Task UnbanUserAsync(BannedUserSummary bannedUser, CancellationToken cancellationToken = default)
    {
        if (Status is not (ConnectionStatus.LoggedIn or ConnectionStatus.InChannel))
        {
            throw new InvalidOperationException("You must be logged in before removing bans.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        NativeBannedUser nativeBannedUser = CreateNativeBannedUser(bannedUser);
        int commandId;
        lock (stateLock)
        {
            EnsureConnectedInstance();
            commandId = TeamTalkNativeMethods.DoUnBanUserEx(instance, ref nativeBannedUser);
        }

        if (commandId <= 0)
        {
            RaiseSystemMessage("TeamTalk SDK did not accept the remove ban command.");
        }

        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<UserAccountSummary>> GetUserAccountsAsync(CancellationToken cancellationToken = default)
    {
        if (Status is not (ConnectionStatus.LoggedIn or ConnectionStatus.InChannel))
        {
            throw new InvalidOperationException("You must be logged in before viewing user accounts.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var request = new TaskCompletionSource<IReadOnlyList<UserAccountSummary>>(TaskCreationOptions.RunContinuationsAsynchronously);
        int commandId;
        lock (stateLock)
        {
            EnsureConnectedInstance();
            if (pendingUserAccounts is not null)
            {
                throw new InvalidOperationException("A user accounts request is already in progress.");
            }

            commandId = TeamTalkNativeMethods.DoListUserAccounts(instance, index: 0, count: 1000);
            if (commandId <= 0)
            {
                throw new InvalidOperationException("TeamTalk SDK did not accept the user accounts command.");
            }

            userAccountsCommandId = commandId;
            pendingUserAccountItems = [];
            pendingUserAccounts = request;
        }

        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(UserAccountsTimeout);

        try
        {
            return await request.Task.WaitAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException("TeamTalk user accounts did not arrive in time.");
        }
        finally
        {
            ClearPendingUserAccounts(request);
        }
    }

    public Task CreateUserAccountAsync(UserAccountCreationRequest account, CancellationToken cancellationToken = default)
    {
        if (Status is not (ConnectionStatus.LoggedIn or ConnectionStatus.InChannel))
        {
            throw new InvalidOperationException("You must be logged in before creating user accounts.");
        }

        string username = account.Username.Trim();
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new InvalidOperationException("Username cannot be empty.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        NativeUserAccount nativeAccount = CreateNativeUserAccount(account with { Username = username });
        int commandId;
        lock (stateLock)
        {
            EnsureConnectedInstance();
            commandId = TeamTalkNativeMethods.DoNewUserAccount(instance, ref nativeAccount);
        }

        if (commandId <= 0)
        {
            RaiseSystemMessage("TeamTalk SDK did not accept the create user account command.");
        }

        return Task.CompletedTask;
    }

    public Task DeleteUserAccountAsync(string username, CancellationToken cancellationToken = default)
    {
        if (Status is not (ConnectionStatus.LoggedIn or ConnectionStatus.InChannel))
        {
            throw new InvalidOperationException("You must be logged in before deleting user accounts.");
        }

        string trimmedUsername = username.Trim();
        if (string.IsNullOrWhiteSpace(trimmedUsername))
        {
            throw new InvalidOperationException("Select a user account before deleting.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        int commandId;
        lock (stateLock)
        {
            EnsureConnectedInstance();
            commandId = TeamTalkNativeMethods.DoDeleteUserAccount(instance, trimmedUsername);
        }

        if (commandId <= 0)
        {
            RaiseSystemMessage("TeamTalk SDK did not accept the delete user account command.");
        }

        return Task.CompletedTask;
    }

    public Task SaveServerConfigurationAsync(CancellationToken cancellationToken = default)
    {
        if (Status is not (ConnectionStatus.LoggedIn or ConnectionStatus.InChannel))
        {
            throw new InvalidOperationException("You must be logged in before saving server configuration.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        int commandId;
        lock (stateLock)
        {
            EnsureConnectedInstance();
            commandId = TeamTalkNativeMethods.DoSaveConfig(instance);
        }

        if (commandId <= 0)
        {
            RaiseSystemMessage("TeamTalk SDK did not accept the save server configuration command.");
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ChannelFileSummary>> GetChannelFilesAsync(CancellationToken cancellationToken = default)
    {
        if (Status != ConnectionStatus.InChannel || currentChannelId <= 0)
        {
            throw new InvalidOperationException("You must be in a channel before viewing channel files.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        int count = 0;
        int success;
        lock (stateLock)
        {
            EnsureConnectedInstance();
            success = TeamTalkNativeMethods.GetChannelFiles(instance, currentChannelId, IntPtr.Zero, ref count);
        }

        if (success == 0 || count <= 0)
        {
            return Task.FromResult<IReadOnlyList<ChannelFileSummary>>([]);
        }

        int size = Marshal.SizeOf<NativeRemoteFile>();
        IntPtr buffer = Marshal.AllocHGlobal(size * count);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            int fileCount = count;
            lock (stateLock)
            {
                EnsureConnectedInstance();
                success = TeamTalkNativeMethods.GetChannelFiles(instance, currentChannelId, buffer, ref fileCount);
            }

            if (success == 0)
            {
                throw new InvalidOperationException("TeamTalk could not read the channel file list.");
            }

            List<ChannelFileSummary> files = [];
            for (int index = 0; index < fileCount; index++)
            {
                NativeRemoteFile file = Marshal.PtrToStructure<NativeRemoteFile>(IntPtr.Add(buffer, index * size));
                string name = file.ReadFileName();
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = $"File {file.FileId}";
                }

                files.Add(new ChannelFileSummary(
                    file.FileId,
                    name,
                    Math.Max(0, file.FileSize),
                    file.ReadUsername(),
                    file.ReadUploadTime()));
            }

            return Task.FromResult<IReadOnlyList<ChannelFileSummary>>(files);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public Task UploadFileAsync(string localFilePath, CancellationToken cancellationToken = default)
    {
        if (Status != ConnectionStatus.InChannel || currentChannelId <= 0)
        {
            throw new InvalidOperationException("You must be in a channel before uploading files.");
        }

        string path = RequireLocalPath(localFilePath, "Select a file to upload.");
        if (!File.Exists(path))
        {
            throw new InvalidOperationException("The selected upload file was not found.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        int commandId;
        lock (stateLock)
        {
            EnsureConnectedInstance();
            commandId = TeamTalkNativeMethods.DoSendFile(instance, currentChannelId, path);
        }

        if (commandId <= 0)
        {
            RaiseSystemMessage("TeamTalk SDK did not accept the upload file command.");
        }

        return Task.CompletedTask;
    }

    public Task DownloadFileAsync(int fileId, string localFilePath, CancellationToken cancellationToken = default)
    {
        if (Status != ConnectionStatus.InChannel || currentChannelId <= 0)
        {
            throw new InvalidOperationException("You must be in a channel before downloading files.");
        }

        if (fileId <= 0)
        {
            throw new InvalidOperationException("Select a file before downloading.");
        }

        string path = RequireLocalPath(localFilePath, "Select where to save the downloaded file.");
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            throw new InvalidOperationException("The selected download folder was not found.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        int commandId;
        lock (stateLock)
        {
            EnsureConnectedInstance();
            commandId = TeamTalkNativeMethods.DoRecvFile(instance, currentChannelId, fileId, path);
        }

        if (commandId <= 0)
        {
            RaiseSystemMessage("TeamTalk SDK did not accept the download file command.");
        }

        return Task.CompletedTask;
    }

    public Task DeleteFileAsync(int fileId, CancellationToken cancellationToken = default)
    {
        if (Status != ConnectionStatus.InChannel || currentChannelId <= 0)
        {
            throw new InvalidOperationException("You must be in a channel before deleting files.");
        }

        if (fileId <= 0)
        {
            throw new InvalidOperationException("Select a file before deleting.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        int commandId;
        lock (stateLock)
        {
            EnsureConnectedInstance();
            commandId = TeamTalkNativeMethods.DoDeleteFile(instance, currentChannelId, fileId);
        }

        if (commandId <= 0)
        {
            RaiseSystemMessage("TeamTalk SDK did not accept the delete file command.");
        }

        return Task.CompletedTask;
    }

    public Task CancelFileTransferAsync(int transferId, CancellationToken cancellationToken = default)
    {
        if (Status is ConnectionStatus.Disconnected or ConnectionStatus.Connecting)
        {
            throw new InvalidOperationException("You must be connected before canceling a file transfer.");
        }

        if (transferId <= 0)
        {
            throw new InvalidOperationException("Select an active transfer before canceling.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        int success;
        lock (stateLock)
        {
            EnsureConnectedInstance();
            success = TeamTalkNativeMethods.CancelFileTransfer(instance, transferId);
        }

        if (success == 0)
        {
            throw new InvalidOperationException("TeamTalk could not cancel the selected file transfer.");
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
            userDisplayNames.Clear();
            loginCommandId = 0;
            soundInputInitialized = false;
            soundOutputInitialized = false;
            voiceTransmissionEnabled = false;
            voiceActivationEnabled = false;
            videoCaptureInitialized = false;
            videoCaptureTransmitting = false;
            desktopSharing = false;
        }

        int attemptId = Interlocked.Increment(ref connectionAttemptId);
        SetStatus(ConnectionStatus.Connecting);
        EnsureInstance();
        EnsureMessageBufferSize();
        RaiseSystemMessage($"Connection attempt started for {profile.Host}:{profile.TcpPort}. Native library: {availability.NativeLibraryPath}.");

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
        StartConnectionProgressWatchdog(attemptId, profile, availability.NativeLibraryPath);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref connectionAttemptId);
        await StopPollingAsync().ConfigureAwait(false);

        lock (stateLock)
        {
            if (instance != IntPtr.Zero)
            {
                StopVoiceInput();
                StopVideoCapture();
                StopDesktopSharing();
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

    public Task SetChannelTopicAsync(string channelPath, string topic, CancellationToken cancellationToken = default)
    {
        if (Status is ConnectionStatus.Disconnected or ConnectionStatus.Connecting)
        {
            throw new InvalidOperationException("You must be logged in before editing a channel topic.");
        }

        int channelId = ResolveChannelId(channelPath);
        if (channelId <= 0)
        {
            throw new InvalidOperationException($"Channel {channelPath} was not found.");
        }

        int commandId;
        lock (stateLock)
        {
            EnsureConnectedInstance();
            if (TeamTalkNativeMethods.GetChannel(instance, channelId, out NativeChannel channel) == 0)
            {
                throw new InvalidOperationException($"Channel {channelPath} was not found.");
            }

            channel.WriteTopic(topic.Trim());
            commandId = TeamTalkNativeMethods.DoUpdateChannel(instance, ref channel);
        }

        if (commandId <= 0)
        {
            RaiseSystemMessage($"TeamTalk SDK did not accept the topic update command for {channelPath}.");
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

        EnsureSoundInputInitialized();

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

        EnsureSoundInputInitialized();

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

    public Task MoveUserAsync(int userId, string destinationChannelPath, CancellationToken cancellationToken = default)
    {
        if (Status is ConnectionStatus.Disconnected or ConnectionStatus.Connecting)
        {
            throw new InvalidOperationException("You must be logged in before moving a user.");
        }

        if (userId <= 0)
        {
            throw new InvalidOperationException("Select a valid user before moving.");
        }

        int destinationChannelId = ResolveChannelId(destinationChannelPath);
        if (destinationChannelId <= 0)
        {
            throw new InvalidOperationException($"Channel {destinationChannelPath} was not found.");
        }

        int commandId;
        lock (stateLock)
        {
            EnsureConnectedInstance();
            commandId = TeamTalkNativeMethods.DoMoveUser(instance, userId, destinationChannelId);
        }

        if (commandId <= 0)
        {
            RaiseSystemMessage("TeamTalk SDK did not accept the move user command.");
        }

        return Task.CompletedTask;
    }

    public Task KickUserAsync(int userId, string channelPath, bool fromServer = false, CancellationToken cancellationToken = default)
    {
        if (Status is ConnectionStatus.Disconnected or ConnectionStatus.Connecting)
        {
            throw new InvalidOperationException("You must be logged in before kicking a user.");
        }

        if (userId <= 0)
        {
            throw new InvalidOperationException("Select a valid user before kicking.");
        }

        int channelId = fromServer ? 0 : ResolveChannelId(channelPath);
        if (!fromServer && channelId <= 0)
        {
            throw new InvalidOperationException($"Channel {channelPath} was not found.");
        }

        int commandId;
        lock (stateLock)
        {
            EnsureConnectedInstance();
            commandId = TeamTalkNativeMethods.DoKickUser(instance, userId, channelId);
        }

        if (commandId <= 0)
        {
            RaiseSystemMessage("TeamTalk SDK did not accept the kick user command.");
        }

        return Task.CompletedTask;
    }

    public Task BanUserAsync(int userId, string channelPath, bool fromServer = false, CancellationToken cancellationToken = default)
    {
        if (Status is ConnectionStatus.Disconnected or ConnectionStatus.Connecting)
        {
            throw new InvalidOperationException("You must be logged in before banning a user.");
        }

        if (userId <= 0)
        {
            throw new InvalidOperationException("Select a valid user before banning.");
        }

        int channelId = fromServer ? 0 : ResolveChannelId(channelPath);
        if (!fromServer && channelId <= 0)
        {
            throw new InvalidOperationException($"Channel {channelPath} was not found.");
        }

        int commandId;
        lock (stateLock)
        {
            EnsureConnectedInstance();
            commandId = TeamTalkNativeMethods.DoBanUser(instance, userId, channelId);
        }

        if (commandId <= 0)
        {
            RaiseSystemMessage("TeamTalk SDK did not accept the ban user command.");
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

    internal Task<ServerStatisticsSummary> BeginServerStatisticsRequestForTest(int commandId)
    {
        var request = new TaskCompletionSource<ServerStatisticsSummary>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (stateLock)
        {
            serverStatisticsCommandId = commandId;
            pendingServerStatistics = request;
        }

        return request.Task;
    }

    internal Task<IReadOnlyList<BannedUserSummary>> BeginBannedUsersRequestForTest(int commandId)
    {
        var request = new TaskCompletionSource<IReadOnlyList<BannedUserSummary>>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (stateLock)
        {
            bannedUsersCommandId = commandId;
            pendingBannedUserItems = [];
            pendingBannedUsers = request;
        }

        return request.Task;
    }

    internal Task<IReadOnlyList<UserAccountSummary>> BeginUserAccountsRequestForTest(int commandId)
    {
        var request = new TaskCompletionSource<IReadOnlyList<UserAccountSummary>>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (stateLock)
        {
            userAccountsCommandId = commandId;
            pendingUserAccountItems = [];
            pendingUserAccounts = request;
        }

        return request.Task;
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
            StopVideoCapture();
            StopDesktopSharing();
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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
            catch (Exception ex)
            {
                RaiseSystemMessage($"TeamTalk event polling stopped unexpectedly: {ex.Message}");
                currentChannelId = 0;
                myUserId = 0;
                userDisplayNames.Clear();
                CloseInstance();
                SetStatus(ConnectionStatus.Disconnected);
            }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private void StartConnectionProgressWatchdog(int attemptId, TeamTalkServerProfile profile, string? nativeLibraryPath)
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(ConnectionProgressTimeout).ConfigureAwait(false);
            if (Volatile.Read(ref connectionAttemptId) != attemptId)
            {
                return;
            }

            if (Status is not (ConnectionStatus.Connecting or ConnectionStatus.Connected))
            {
                return;
            }

            string encryption = profile.IsEncrypted ? "enabled" : "disabled";
            RaiseSystemMessage(
                $"Connection to {profile.Host}:{profile.TcpPort} did not complete within {ConnectionProgressTimeout.TotalSeconds:0} seconds. " +
                $"Check the host, TCP port, UDP port, and encryption setting. Encryption is currently {encryption}. " +
                $"Native library: {nativeLibraryPath ?? "unknown"}.");
            await DisconnectAsync().ConfigureAwait(false);
        });
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
            case ClientEvent.SoundDeviceAdded:
            case ClientEvent.SoundDeviceRemoved:
            case ClientEvent.SoundDeviceUnplugged:
            case ClientEvent.SoundDeviceNewDefaultInput:
            case ClientEvent.SoundDeviceNewDefaultOutput:
            case ClientEvent.SoundDeviceNewDefaultInputCommunication:
            case ClientEvent.SoundDeviceNewDefaultOutputCommunication:
                HandleSoundDeviceChanged(message.ClientEvent, message.SoundDevice);
                break;
            case ClientEvent.InternalError:
                DispatchInternalError(message);
                break;
            case ClientEvent.CommandProcessing:
                HandleCommandProcessing(message);
                break;
            case ClientEvent.CommandError:
                HandleCommandError(message);
                break;
            case ClientEvent.CommandMyselfLoggedIn:
                myUserId = message.Source;
                loginCommandId = 0;
                SetStatus(ConnectionStatus.LoggedIn);
                RaiseSystemMessage("Logged in to the TeamTalk server.");
                InitializeDefaultAudioDevices();
                JoinConfiguredChannel();
                break;
            case ClientEvent.CommandMyselfLoggedOut:
            case ClientEvent.CommandMyselfKicked:
                HandleDisconnected("Logged out.");
                break;
            case ClientEvent.CommandUserLoggedIn:
                DispatchUserLoggedIn(message.User);
                break;
            case ClientEvent.CommandUserLoggedOut:
                DispatchUserLoggedOut(message.User);
                break;
            case ClientEvent.CommandUserJoined:
                DispatchUserJoined(message.User);
                break;
            case ClientEvent.CommandUserUpdate:
            case ClientEvent.UserStateChange:
                DispatchUserUpdated(message.User);
                break;
            case ClientEvent.CommandUserLeft:
                DispatchUserLeft(message.User, message.Source);
                break;
            case ClientEvent.CommandUserTextMessage:
                DispatchTextMessage(message.TextMessage);
                break;
            case ClientEvent.UserVideoCapture:
                DispatchVideoFrame(message.Source);
                break;
            case ClientEvent.UserMediaFileVideo:
                DispatchVideoFrame(message.Source, isMediaFile: true);
                break;
            case ClientEvent.UserDesktopWindow:
                DispatchDesktopFrame(message.Source);
                break;
            case ClientEvent.DesktopWindowTransfer:
                DispatchDesktopTransfer(message);
                break;
            case ClientEvent.CommandChannelNew:
            case ClientEvent.CommandChannelUpdate:
                DispatchChannelAddedOrUpdated(message.Channel);
                break;
            case ClientEvent.CommandChannelRemove:
                ChannelRemoved?.Invoke(this, message.Source);
                break;
            case ClientEvent.CommandServerUpdate:
                DispatchServerUpdate(message.ServerProperties);
                break;
            case ClientEvent.CommandServerStatistics:
                CompleteServerStatistics(message);
                break;
            case ClientEvent.CommandBannedUser:
                AddPendingBannedUser(message.BannedUser);
                break;
            case ClientEvent.CommandUserAccount:
                AddPendingUserAccount(message.UserAccount);
                break;
            case ClientEvent.FileTransfer:
                DispatchFileTransfer(message.FileTransfer);
                break;
        }
    }

    private void DispatchInternalError(TeamTalkMessage message)
    {
        string errorMessage = message.ClientError.ReadMessage();
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            errorMessage = message.IntValue != 0
                ? $"error {message.IntValue}"
                : "unknown error";
        }

        RaiseSystemMessage($"TeamTalk SDK internal error: {errorMessage}.");
    }

    private void DispatchServerUpdate(NativeServerProperties properties)
    {
        ServerInformationSummary summary = CreateServerInformationSummary(properties);
        string serverName = string.IsNullOrWhiteSpace(summary.ServerName)
            ? "server"
            : summary.ServerName;
        RaiseSystemMessage($"Server information updated for {serverName}.");
    }

    private void DispatchDesktopTransfer(TeamTalkMessage message)
    {
        _ = message;
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
        RaiseSystemMessage("Connected to the TeamTalk server. Logging in.");

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
        else
        {
            loginCommandId = commandId;
            RaiseSystemMessage($"Login command sent for {nickname}.");
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

    private static string RequireLocalPath(string localFilePath, string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(localFilePath))
        {
            throw new InvalidOperationException(errorMessage);
        }

        try
        {
            return Path.GetFullPath(localFilePath);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new InvalidOperationException("The selected file path is not valid.", ex);
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
        if (instance == IntPtr.Zero || (soundInputInitialized && soundOutputInitialized))
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

        IReadOnlyList<NativeSoundDevice> nativeDevices = ReadNativeSoundDevices(restartSoundSystem: false);
        NativeSoundDevice? inputDevice = FindSoundDevice(nativeDevices, inputDeviceId);
        NativeSoundDevice? outputDevice = FindSoundDevice(nativeDevices, outputDeviceId);

        int inputReady = soundInputInitialized ? 1 : 0;
        int outputReady = soundOutputInitialized ? 1 : 0;
        lock (stateLock)
        {
            if (instance == IntPtr.Zero)
            {
                return;
            }

            if (!soundInputInitialized && !soundOutputInitialized)
            {
                ApplySoundDeviceEffects(inputDevice);
                if (CanUseDuplexAudio(inputDevice, outputDevice)
                    && TeamTalkNativeMethods.InitSoundDuplexDevices(instance, inputDeviceId, outputDeviceId) != 0)
                {
                    soundInputInitialized = true;
                    soundOutputInitialized = true;
                    soundDuplexInitialized = true;
                    inputReady = 1;
                    outputReady = 1;
                }
            }

            if (!soundInputInitialized)
            {
                inputReady = TeamTalkNativeMethods.InitSoundInputDevice(instance, inputDeviceId);
                soundInputInitialized = inputReady != 0;
            }

            if (!soundOutputInitialized)
            {
                outputReady = TeamTalkNativeMethods.InitSoundOutputDevice(instance, outputDeviceId);
                soundOutputInitialized = outputReady != 0;
            }
        }

        if (inputReady == 0)
        {
            RaiseSystemMessage("TeamTalk could not initialize the microphone. Voice transmission is unavailable until an input device is selected.");
        }

        if (outputReady == 0)
        {
            RaiseSystemMessage("TeamTalk could not initialize the speaker. You may not hear channel audio until an output device is selected.");
        }

        ApplyConfiguredAudioVolume();
    }

    private void ApplySoundDeviceEffects(NativeSoundDevice? inputDevice)
    {
        if (instance == IntPtr.Zero || inputDevice is not { } device)
        {
            return;
        }

        SoundDeviceFeature features = (SoundDeviceFeature)device.SoundDeviceFeatures;
        var effects = new NativeSoundDeviceEffects
        {
            EnableDenoise = audioProcessingSettings.EnableNoiseSuppression && features.HasFlag(SoundDeviceFeature.Denoise) ? 1 : 0,
            EnableEchoCancellation = audioProcessingSettings.EnableEchoCancellation && features.HasFlag(SoundDeviceFeature.AcousticEchoCancellation) ? 1 : 0,
            EnableAutomaticGainControl = audioProcessingSettings.EnableAutomaticGainControl && features.HasFlag(SoundDeviceFeature.AutomaticGainControl) ? 1 : 0
        };

        if (effects.EnableDenoise == 0
            && effects.EnableEchoCancellation == 0
            && effects.EnableAutomaticGainControl == 0)
        {
            return;
        }

        TeamTalkNativeMethods.SetSoundDeviceEffects(instance, ref effects);
    }

    private static bool CanUseDuplexAudio(NativeSoundDevice? inputDevice, NativeSoundDevice? outputDevice)
    {
        return inputDevice is { } input
            && outputDevice is { } output
            && input.SoundSystem == output.SoundSystem
            && input.SoundSystem is SoundSystem.Wasapi or SoundSystem.DSound
            && ((SoundDeviceFeature)input.SoundDeviceFeatures).HasFlag(SoundDeviceFeature.DuplexMode)
            && ((SoundDeviceFeature)output.SoundDeviceFeatures).HasFlag(SoundDeviceFeature.DuplexMode);
    }

    private static NativeSoundDevice? FindSoundDevice(IReadOnlyList<NativeSoundDevice> devices, int deviceId)
    {
        foreach (NativeSoundDevice device in devices)
        {
            if (device.DeviceId == deviceId)
            {
                return device;
            }
        }

        return null;
    }

    private void EnsureSoundInputInitialized()
    {
        if (!soundInputInitialized)
        {
            InitializeDefaultAudioDevices();
        }

        if (!soundInputInitialized)
        {
            throw new InvalidOperationException("The microphone is not ready.");
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

        if (soundDuplexInitialized)
        {
            TeamTalkNativeMethods.CloseSoundDuplexDevices(instance);
        }
        else
        {
            TeamTalkNativeMethods.CloseSoundInputDevice(instance);
            TeamTalkNativeMethods.CloseSoundOutputDevice(instance);
        }

        soundInputInitialized = false;
        soundOutputInitialized = false;
        soundDuplexInitialized = false;
    }

    private static IReadOnlyList<AudioDeviceSummary> ReadAudioDevices()
    {
        IReadOnlyList<NativeSoundDevice> nativeDevices = ReadNativeSoundDevices(restartSoundSystem: true);
        if (nativeDevices.Count == 0)
        {
            return [];
        }

        TeamTalkNativeMethods.GetDefaultSoundDevices(out int defaultInputId, out int defaultOutputId);
        List<AudioDeviceSummary> devices = [];
        foreach (NativeSoundDevice nativeDevice in nativeDevices)
        {
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

    private static IReadOnlyList<NativeSoundDevice> ReadNativeSoundDevices(bool restartSoundSystem)
    {
        if (restartSoundSystem)
        {
            TeamTalkNativeMethods.RestartSoundSystem();
        }

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

            List<NativeSoundDevice> devices = [];
            for (int index = 0; index < count; index++)
            {
                IntPtr deviceAddress = IntPtr.Add(buffer, index * size);
                NativeSoundDevice nativeDevice = Marshal.PtrToStructure<NativeSoundDevice>(deviceAddress);
                devices.Add(nativeDevice);
            }

            return devices;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static IReadOnlyList<VideoCaptureDeviceSummary> ReadVideoCaptureDevices()
    {
        int count = 0;
        if (TeamTalkNativeMethods.GetVideoCaptureDevices(IntPtr.Zero, ref count) == 0 || count <= 0)
        {
            return [];
        }

        int size = Marshal.SizeOf<NativeVideoCaptureDevice>();
        IntPtr buffer = Marshal.AllocHGlobal(size * count);
        try
        {
            if (TeamTalkNativeMethods.GetVideoCaptureDevices(buffer, ref count) == 0 || count <= 0)
            {
                return [];
            }

            List<VideoCaptureDeviceSummary> devices = [];
            for (int index = 0; index < count; index++)
            {
                IntPtr deviceAddress = IntPtr.Add(buffer, index * size);
                NativeVideoCaptureDevice nativeDevice = Marshal.PtrToStructure<NativeVideoCaptureDevice>(deviceAddress);
                string deviceId = nativeDevice.ReadDeviceId();
                if (string.IsNullOrWhiteSpace(deviceId))
                {
                    continue;
                }

                string name = nativeDevice.ReadName();
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = $"Camera {index + 1}";
                }

                int formatCount = Math.Clamp(nativeDevice.VideoFormatsCount, 0, NativeConstants.VideoFormatsMax);
                List<VideoCaptureFormatSummary> formats = [];
                for (int formatIndex = 0; formatIndex < formatCount; formatIndex++)
                {
                    NativeVideoFormat nativeFormat = nativeDevice.VideoFormats[formatIndex];
                    if (nativeFormat.Width <= 0 || nativeFormat.Height <= 0 || nativeFormat.FpsNumerator <= 0 || nativeFormat.FpsDenominator <= 0)
                    {
                        continue;
                    }

                    formats.Add(new VideoCaptureFormatSummary(
                        nativeFormat.Width,
                        nativeFormat.Height,
                        nativeFormat.FpsNumerator,
                        nativeFormat.FpsDenominator,
                        nativeFormat.FourCC.ToString()));
                }

                devices.Add(new VideoCaptureDeviceSummary(deviceId, name, nativeDevice.ReadCaptureApi(), formats));
            }

            return devices;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static NativeVideoFormat CreateNativeVideoFormat(VideoCaptureFormatSummary format)
    {
        return new NativeVideoFormat
        {
            Width = Math.Max(1, format.Width),
            Height = Math.Max(1, format.Height),
            FpsNumerator = Math.Max(1, format.FpsNumerator),
            FpsDenominator = Math.Max(1, format.FpsDenominator),
            FourCC = Enum.TryParse(format.PixelFormat, ignoreCase: true, out FourCC fourCC)
                ? fourCC
                : FourCC.I420
        };
    }

    private static NativeVideoCodec CreateDefaultVideoCodec()
    {
        return new NativeVideoCodec
        {
            Codec = Codec.WebMVp8,
            WebMVp8 = new NativeWebMVP8Codec
            {
                TargetBitrate = 256,
                EncodeDeadline = 1
            }
        };
    }

    private void StopVideoCapture()
    {
        if (instance == IntPtr.Zero)
        {
            videoCaptureInitialized = false;
            videoCaptureTransmitting = false;
            return;
        }

        if (videoCaptureTransmitting)
        {
            TeamTalkNativeMethods.StopVideoCaptureTransmission(instance);
            videoCaptureTransmitting = false;
        }

        if (videoCaptureInitialized)
        {
            TeamTalkNativeMethods.CloseVideoCaptureDevice(instance);
            videoCaptureInitialized = false;
        }
    }

    private void StopDesktopSharing()
    {
        if (instance == IntPtr.Zero)
        {
            desktopSharing = false;
            return;
        }

        if (desktopSharing)
        {
            TeamTalkNativeMethods.CloseDesktopWindow(instance);
            desktopSharing = false;
        }
    }

    private void ApplyConfiguredAudioVolume()
    {
        int inputGainLevel = PercentToTeamTalkSoundLevel(configuredInputVolumePercent, SoundLevel.GainDefault, SoundLevel.GainMax);
        int outputVolume = PercentToTeamTalkSoundLevel(configuredOutputVolumePercent, SoundLevel.VolumeDefault, SoundLevel.VolumeMax);

        int inputReady = 1;
        int outputReady = 1;
        lock (stateLock)
        {
            if (instance == IntPtr.Zero)
            {
                return;
            }

            if (soundInputInitialized)
            {
                inputReady = TeamTalkNativeMethods.SetSoundInputGainLevel(instance, inputGainLevel);
            }

            if (soundOutputInitialized)
            {
                outputReady = TeamTalkNativeMethods.SetSoundOutputVolume(instance, outputVolume);
            }
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

    private static int UserVolumePercentToTeamTalkLevel(int percent)
    {
        int clampedPercent = Math.Clamp(percent, 0, 200);
        return Math.Clamp(clampedPercent * 10, SoundLevel.VolumeMin, SoundLevel.VolumeMax);
    }

    private static int TeamTalkVolumeToUserPercent(int volume)
    {
        if (volume <= 0)
        {
            return 0;
        }

        return Math.Clamp((int)Math.Round(volume / 10.0), 0, 200);
    }

    private void DispatchUserJoined(NativeUser user)
    {
        UserSummary summary = CreateUserSummary(user, user.ChannelId);
        RememberUserDisplayName(summary);
        UserJoined?.Invoke(this, summary);

        if (user.UserId == myUserId)
        {
            currentChannelId = user.ChannelId;
            SetStatus(ConnectionStatus.InChannel);
        }
        else
        {
            EnsureMediaSubscriptions(user);
        }
    }

    private void DispatchUserLoggedIn(NativeUser user)
    {
        UserSummary summary = CreateUserSummary(user, user.ChannelId);
        RememberUserDisplayName(summary);
        RaiseSystemMessage($"{summary.Nickname} logged in.");
    }

    private void DispatchUserLoggedOut(NativeUser user)
    {
        string displayName = GetNativeUserDisplayName(user);
        if (user.UserId > 0)
        {
            userDisplayNames.Remove(user.UserId);
        }

        RaiseSystemMessage($"{displayName} logged out.");
    }

    private void DispatchUserLeft(NativeUser user, int previousChannelId)
    {
        UserSummary summary = CreateUserSummary(user, previousChannelId);
        UserLeft?.Invoke(this, summary);
        userDisplayNames.Remove(user.UserId);

        if (user.UserId == myUserId)
        {
            currentChannelId = 0;
            SetStatus(ConnectionStatus.LoggedIn);
        }
    }

    private void DispatchUserUpdated(NativeUser user)
    {
        UserSummary summary = CreateUserSummary(user, user.ChannelId);
        RememberUserDisplayName(summary);
        UserUpdated?.Invoke(this, summary);
        EnsureMediaSubscriptions(user);
    }

    private void EnsureMediaSubscriptions(NativeUser user)
    {
        const Subscription desiredSubscriptions = Subscription.Voice | Subscription.VideoCapture | Subscription.Desktop | Subscription.MediaFile;
        Subscription localSubscriptions = (Subscription)user.LocalSubscriptions;
        Subscription missingSubscriptions = desiredSubscriptions & ~localSubscriptions;

        if (instance == IntPtr.Zero
            || currentChannelId <= 0
            || user.UserId <= 0
            || user.UserId == myUserId
            || user.ChannelId != currentChannelId
            || missingSubscriptions == Subscription.None)
        {
            return;
        }

        int commandId;
        lock (stateLock)
        {
            if (instance == IntPtr.Zero)
            {
                return;
            }

            commandId = TeamTalkNativeMethods.DoSubscribe(instance, user.UserId, missingSubscriptions);
        }

        if (commandId <= 0)
        {
            string nickname = user.ReadNickname();
            if (string.IsNullOrWhiteSpace(nickname))
            {
                nickname = user.ReadUsername();
            }

            RaiseSystemMessage($"TeamTalk could not subscribe to media from {nickname}.");
        }
    }

    private void DispatchVideoFrame(int userId, bool isMediaFile = false)
    {
        if (userId <= 0 || instance == IntPtr.Zero)
        {
            return;
        }

        NativeVideoFrame frame;
        byte[] pixels;
        int stride;

        lock (stateLock)
        {
            if (instance == IntPtr.Zero)
            {
                return;
            }

            IntPtr framePointer = isMediaFile
                ? TeamTalkNativeMethods.AcquireUserMediaVideoFrame(instance, userId)
                : TeamTalkNativeMethods.AcquireUserVideoCaptureFrame(instance, userId);
            if (framePointer == IntPtr.Zero)
            {
                return;
            }

            try
            {
                frame = Marshal.PtrToStructure<NativeVideoFrame>(framePointer);
                stride = frame.Width * 4;
                if (!CanCopyBgraFrame(frame.Width, frame.Height, stride, frame.FrameBuffer, frame.FrameBufferSize))
                {
                    return;
                }

                int bytesToCopy = stride * frame.Height;
                pixels = new byte[bytesToCopy];
                Marshal.Copy(frame.FrameBuffer, pixels, 0, bytesToCopy);
            }
            finally
            {
                if (isMediaFile)
                {
                    TeamTalkNativeMethods.ReleaseUserMediaVideoFrame(instance, framePointer);
                }
                else
                {
                    TeamTalkNativeMethods.ReleaseUserVideoCaptureFrame(instance, framePointer);
                }
            }
        }

        string displayName = GetUserDisplayName(userId);
        if (isMediaFile)
        {
            displayName = $"{displayName} media file";
        }

        MediaFrameReceived?.Invoke(this, new MediaFrameSummary(
            userId,
            displayName,
            MediaStreamKind.Video,
            frame.Width,
            frame.Height,
            stride,
            pixels,
            DateTimeOffset.Now));
    }

    private void DispatchDesktopFrame(int userId)
    {
        if (userId <= 0 || instance == IntPtr.Zero)
        {
            return;
        }

        NativeDesktopWindow desktop;
        byte[] pixels;
        int stride;

        lock (stateLock)
        {
            if (instance == IntPtr.Zero)
            {
                return;
            }

            IntPtr desktopPointer = TeamTalkNativeMethods.AcquireUserDesktopWindowEx(instance, userId, BitmapFormat.Rgb32);
            if (desktopPointer == IntPtr.Zero)
            {
                return;
            }

            try
            {
                desktop = Marshal.PtrToStructure<NativeDesktopWindow>(desktopPointer);
                stride = desktop.BytesPerLine > 0 ? desktop.BytesPerLine : desktop.Width * 4;
                if (desktop.BitmapFormat != BitmapFormat.Rgb32
                    || !CanCopyBgraFrame(desktop.Width, desktop.Height, stride, desktop.FrameBuffer, desktop.FrameBufferSize))
                {
                    return;
                }

                int bytesToCopy = stride * desktop.Height;
                pixels = new byte[bytesToCopy];
                Marshal.Copy(desktop.FrameBuffer, pixels, 0, bytesToCopy);
            }
            finally
            {
                TeamTalkNativeMethods.ReleaseUserDesktopWindow(instance, desktopPointer);
            }
        }

        MediaFrameReceived?.Invoke(this, new MediaFrameSummary(
            userId,
            GetUserDisplayName(userId),
            MediaStreamKind.Desktop,
            desktop.Width,
            desktop.Height,
            stride,
            pixels,
            DateTimeOffset.Now));
    }

    private static bool CanCopyBgraFrame(int width, int height, int stride, IntPtr buffer, int bufferSize)
    {
        if (width <= 0 || height <= 0 || stride < width * 4 || buffer == IntPtr.Zero || bufferSize <= 0)
        {
            return false;
        }

        long requiredBytes = (long)stride * height;
        return requiredBytes <= int.MaxValue && bufferSize >= requiredBytes;
    }

    private void RememberUserDisplayName(UserSummary user)
    {
        if (user.Id > 0 && !string.IsNullOrWhiteSpace(user.Nickname))
        {
            userDisplayNames[user.Id] = user.Nickname;
        }
    }

    private string GetUserDisplayName(int userId)
    {
        return userDisplayNames.TryGetValue(userId, out string? displayName) && !string.IsNullOrWhiteSpace(displayName)
            ? displayName
            : $"User {userId}";
    }

    private string GetNativeUserDisplayName(NativeUser user)
    {
        string displayName = user.ReadNickname();
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName;
        }

        displayName = user.ReadUsername();
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName;
        }

        return user.UserId > 0 ? GetUserDisplayName(user.UserId) : "A user";
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
        else if (textMessage.MessageType == TextMsgType.Broadcast)
        {
            ChannelMessageReceived?.Invoke(this, new ChatMessage(
                DateTimeOffset.Now,
                $"Broadcast from {sender}",
                textMessage.ReadMessage(),
                IsSystem: true));
        }
        else if (textMessage.MessageType == TextMsgType.Custom)
        {
            ChannelMessageReceived?.Invoke(this, new ChatMessage(
                DateTimeOffset.Now,
                $"Custom message from {sender}",
                textMessage.ReadMessage(),
                IsSystem: true));
        }
    }

    private void DispatchFileTransfer(NativeFileTransfer fileTransfer)
    {
        string remoteFileName = fileTransfer.ReadRemoteFileName();
        if (string.IsNullOrWhiteSpace(remoteFileName))
        {
            string localPath = fileTransfer.ReadLocalFilePath();
            remoteFileName = string.IsNullOrWhiteSpace(localPath)
                ? $"Transfer {fileTransfer.TransferId}"
                : Path.GetFileName(localPath);
        }

        var status = (TeamTalkFileTransferStatus)(int)fileTransfer.Status;
        var summary = new FileTransferSummary(
            fileTransfer.TransferId,
            fileTransfer.ChannelId,
            fileTransfer.ReadLocalFilePath(),
            remoteFileName,
            Math.Max(0, fileTransfer.FileSize),
            Math.Max(0, fileTransfer.Transferred),
            fileTransfer.Inbound != 0,
            status);
        FileTransferUpdated?.Invoke(this, summary);

        if (status == TeamTalkFileTransferStatus.Finished)
        {
            RaiseSystemMessage($"{(summary.IsDownload ? "Downloaded" : "Uploaded")} {summary.RemoteFileName}.");
        }
        else if (status == TeamTalkFileTransferStatus.Error)
        {
            RaiseSystemMessage($"File transfer failed for {summary.RemoteFileName}.");
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
            IsPermanent: (channel.ChannelType & (uint)ChannelType.Permanent) != 0,
            Topic: channel.ReadTopic()));
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
            user.ReadStatusMessage(),
            TeamTalkVolumeToUserPercent(user.VolumeVoice),
            user.VolumeVoice == 0);
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
        userDisplayNames.Clear();
        loginCommandId = 0;
        FailPendingServerStatistics(new InvalidOperationException(reason));
        FailPendingBannedUsers(new InvalidOperationException(reason));
        FailPendingUserAccounts(new InvalidOperationException(reason));
        CloseInstance();
        SetStatus(ConnectionStatus.Disconnected);
    }

    private void HandleSoundDeviceChanged(ClientEvent clientEvent, NativeSoundDevice soundDevice)
    {
        if (Status is not (ConnectionStatus.LoggedIn or ConnectionStatus.InChannel) || instance == IntPtr.Zero)
        {
            return;
        }

        bool inputDefaultChanged = clientEvent is ClientEvent.SoundDeviceNewDefaultInput
            or ClientEvent.SoundDeviceNewDefaultInputCommunication;
        bool outputDefaultChanged = clientEvent is ClientEvent.SoundDeviceNewDefaultOutput
            or ClientEvent.SoundDeviceNewDefaultOutputCommunication;
        bool deviceListChanged = clientEvent is ClientEvent.SoundDeviceAdded
            or ClientEvent.SoundDeviceRemoved
            or ClientEvent.SoundDeviceUnplugged;

        if (!deviceListChanged
            && !(inputDefaultChanged && configuredInputDeviceId is null)
            && !(outputDefaultChanged && configuredOutputDeviceId is null))
        {
            return;
        }

        bool restartVoiceTransmission;
        bool restartVoiceActivation;
        int activationLevel = 50;
        lock (stateLock)
        {
            restartVoiceTransmission = voiceTransmissionEnabled;
            restartVoiceActivation = voiceActivationEnabled;
            if (instance != IntPtr.Zero)
            {
                activationLevel = Math.Clamp(
                    TeamTalkNativeMethods.GetVoiceActivationLevel(instance),
                    SoundLevel.VuMin,
                    SoundLevel.VuMax);
            }

            StopVoiceInput();
            CloseSoundDevices();
        }

        InitializeDefaultAudioDevices();

        if (restartVoiceTransmission)
        {
            TryRestoreVoiceTransmission();
        }
        else if (restartVoiceActivation)
        {
            TryRestoreVoiceActivation(activationLevel);
        }

        RaiseSystemMessage($"{DescribeSoundDeviceEvent(clientEvent, soundDevice)} TeamTalk NG refreshed the microphone and speaker.");
    }

    private static string DescribeSoundDeviceEvent(ClientEvent clientEvent, NativeSoundDevice soundDevice)
    {
        string name = soundDevice.ReadName();
        if (string.IsNullOrWhiteSpace(name))
        {
            name = "audio device";
        }

        return clientEvent switch
        {
            ClientEvent.SoundDeviceAdded => $"Audio device added: {name}.",
            ClientEvent.SoundDeviceRemoved => $"Audio device removed: {name}.",
            ClientEvent.SoundDeviceUnplugged => $"Audio device unplugged: {name}.",
            ClientEvent.SoundDeviceNewDefaultInput => $"Default microphone changed to {name}.",
            ClientEvent.SoundDeviceNewDefaultOutput => $"Default speaker changed to {name}.",
            ClientEvent.SoundDeviceNewDefaultInputCommunication => $"Default communications microphone changed to {name}.",
            ClientEvent.SoundDeviceNewDefaultOutputCommunication => $"Default communications speaker changed to {name}.",
            _ => "Audio devices changed."
        };
    }

    private void TryRestoreVoiceTransmission()
    {
        try
        {
            EnsureSoundInputInitialized();
            lock (stateLock)
            {
                if (instance != IntPtr.Zero && TeamTalkNativeMethods.EnableVoiceTransmission(instance, 1) != 0)
                {
                    voiceTransmissionEnabled = true;
                }
            }
        }
        catch (Exception ex)
        {
            RaiseSystemMessage($"Audio devices refreshed, but voice transmission could not be restored: {ex.Message}");
        }
    }

    private void TryRestoreVoiceActivation(int activationLevel)
    {
        try
        {
            EnsureSoundInputInitialized();
            lock (stateLock)
            {
                if (instance == IntPtr.Zero)
                {
                    return;
                }

                TeamTalkNativeMethods.SetVoiceActivationLevel(instance, activationLevel);
                if (TeamTalkNativeMethods.EnableVoiceActivation(instance, 1) != 0)
                {
                    voiceActivationEnabled = true;
                }
            }
        }
        catch (Exception ex)
        {
            RaiseSystemMessage($"Audio devices refreshed, but voice activation could not be restored: {ex.Message}");
        }
    }

    private void HandleCommandError(TeamTalkMessage message)
    {
        string error = ReadErrorMessage(message);
        if (loginCommandId > 0 && message.Source == loginCommandId)
        {
            HandleDisconnected($"Login failed: {error}");
            return;
        }

        if (serverStatisticsCommandId > 0 && message.Source == serverStatisticsCommandId)
        {
            FailPendingServerStatistics(new InvalidOperationException(error));
        }

        if (bannedUsersCommandId > 0 && message.Source == bannedUsersCommandId)
        {
            FailPendingBannedUsers(new InvalidOperationException(error));
        }

        if (userAccountsCommandId > 0 && message.Source == userAccountsCommandId)
        {
            FailPendingUserAccounts(new InvalidOperationException(error));
        }

        RaiseSystemMessage(error);
    }

    private void HandleCommandProcessing(TeamTalkMessage message)
    {
        if (message.BoolValue != 0)
        {
            return;
        }

        if (bannedUsersCommandId > 0 && message.Source == bannedUsersCommandId)
        {
            CompletePendingBannedUsers();
        }

        if (userAccountsCommandId > 0 && message.Source == userAccountsCommandId)
        {
            CompletePendingUserAccounts();
        }
    }

    private void CompleteServerStatistics(TeamTalkMessage message)
    {
        TaskCompletionSource<ServerStatisticsSummary>? request = null;
        lock (stateLock)
        {
            if (pendingServerStatistics is null)
            {
                return;
            }

            request = pendingServerStatistics;
            pendingServerStatistics = null;
            serverStatisticsCommandId = 0;
        }

        request.TrySetResult(CreateServerStatisticsSummary(message.ServerStatistics));
    }

    private void AddPendingBannedUser(NativeBannedUser bannedUser)
    {
        lock (stateLock)
        {
            if (pendingBannedUserItems is null)
            {
                return;
            }

            pendingBannedUserItems.Add(CreateBannedUserSummary(bannedUser));
        }
    }

    private void CompletePendingBannedUsers()
    {
        TaskCompletionSource<IReadOnlyList<BannedUserSummary>>? request = null;
        IReadOnlyList<BannedUserSummary> users = [];
        lock (stateLock)
        {
            if (pendingBannedUsers is null)
            {
                return;
            }

            request = pendingBannedUsers;
            users = pendingBannedUserItems?.ToList() ?? [];
            pendingBannedUsers = null;
            pendingBannedUserItems = null;
            bannedUsersCommandId = 0;
        }

        request.TrySetResult(users);
    }

    private void AddPendingUserAccount(NativeUserAccount userAccount)
    {
        lock (stateLock)
        {
            if (pendingUserAccountItems is null)
            {
                return;
            }

            pendingUserAccountItems.Add(CreateUserAccountSummary(userAccount));
        }
    }

    private void CompletePendingUserAccounts()
    {
        TaskCompletionSource<IReadOnlyList<UserAccountSummary>>? request = null;
        IReadOnlyList<UserAccountSummary> accounts = [];
        lock (stateLock)
        {
            if (pendingUserAccounts is null)
            {
                return;
            }

            request = pendingUserAccounts;
            accounts = pendingUserAccountItems?.ToList() ?? [];
            pendingUserAccounts = null;
            pendingUserAccountItems = null;
            userAccountsCommandId = 0;
        }

        request.TrySetResult(accounts);
    }

    private void FailPendingServerStatistics(Exception exception)
    {
        TaskCompletionSource<ServerStatisticsSummary>? request = null;
        lock (stateLock)
        {
            request = pendingServerStatistics;
            pendingServerStatistics = null;
            serverStatisticsCommandId = 0;
        }

        request?.TrySetException(exception);
    }

    private void FailPendingBannedUsers(Exception exception)
    {
        TaskCompletionSource<IReadOnlyList<BannedUserSummary>>? request = null;
        lock (stateLock)
        {
            request = pendingBannedUsers;
            pendingBannedUsers = null;
            pendingBannedUserItems = null;
            bannedUsersCommandId = 0;
        }

        request?.TrySetException(exception);
    }

    private void FailPendingUserAccounts(Exception exception)
    {
        TaskCompletionSource<IReadOnlyList<UserAccountSummary>>? request = null;
        lock (stateLock)
        {
            request = pendingUserAccounts;
            pendingUserAccounts = null;
            pendingUserAccountItems = null;
            userAccountsCommandId = 0;
        }

        request?.TrySetException(exception);
    }

    private void ClearPendingServerStatistics(TaskCompletionSource<ServerStatisticsSummary> request)
    {
        lock (stateLock)
        {
            if (ReferenceEquals(pendingServerStatistics, request))
            {
                pendingServerStatistics = null;
                serverStatisticsCommandId = 0;
            }
        }
    }

    private void ClearPendingBannedUsers(TaskCompletionSource<IReadOnlyList<BannedUserSummary>> request)
    {
        lock (stateLock)
        {
            if (ReferenceEquals(pendingBannedUsers, request))
            {
                pendingBannedUsers = null;
                pendingBannedUserItems = null;
                bannedUsersCommandId = 0;
            }
        }
    }

    private void ClearPendingUserAccounts(TaskCompletionSource<IReadOnlyList<UserAccountSummary>> request)
    {
        lock (stateLock)
        {
            if (ReferenceEquals(pendingUserAccounts, request))
            {
                pendingUserAccounts = null;
                pendingUserAccountItems = null;
                userAccountsCommandId = 0;
            }
        }
    }

    private static ServerStatisticsSummary CreateServerStatisticsSummary(NativeServerStatistics statistics)
    {
        return new ServerStatisticsSummary(
            Math.Max(0, statistics.TotalBytesTx),
            Math.Max(0, statistics.TotalBytesRx),
            Math.Max(0, statistics.VoiceBytesTx),
            Math.Max(0, statistics.VoiceBytesRx),
            Math.Max(0, statistics.VideoCaptureBytesTx),
            Math.Max(0, statistics.VideoCaptureBytesRx),
            Math.Max(0, statistics.MediaFileBytesTx),
            Math.Max(0, statistics.MediaFileBytesRx),
            Math.Max(0, statistics.DesktopBytesTx),
            Math.Max(0, statistics.DesktopBytesRx),
            Math.Max(0, statistics.UsersServed),
            Math.Max(0, statistics.UsersPeak),
            Math.Max(0, statistics.FilesTx),
            Math.Max(0, statistics.FilesRx),
            Math.Max(0, statistics.UptimeMilliseconds));
    }

    private static BannedUserSummary CreateBannedUserSummary(NativeBannedUser bannedUser)
    {
        return new BannedUserSummary(
            bannedUser.ReadIpAddress(),
            bannedUser.ReadChannelPath(),
            bannedUser.ReadBanTime(),
            bannedUser.ReadNickname(),
            bannedUser.ReadUsername(),
            (BannedUserType)bannedUser.BanTypes,
            bannedUser.ReadOwner());
    }

    private static NativeBannedUser CreateNativeBannedUser(BannedUserSummary bannedUser)
    {
        NativeBannedUser nativeBannedUser = default;
        nativeBannedUser.WriteIpAddress(bannedUser.IpAddress);
        nativeBannedUser.WriteChannelPath(bannedUser.ChannelPath);
        nativeBannedUser.WriteNickname(bannedUser.Nickname);
        nativeBannedUser.WriteUsername(bannedUser.Username);
        nativeBannedUser.BanTypes = (uint)bannedUser.BanTypes;
        return nativeBannedUser;
    }

    private static UserAccountSummary CreateUserAccountSummary(NativeUserAccount account)
    {
        var type = (UserAccountType)account.UserType;
        if (type is not (UserAccountType.Default or UserAccountType.Administrator))
        {
            type = UserAccountType.Default;
        }

        return new UserAccountSummary(
            account.ReadUsername(),
            type,
            (UserAccountRights)account.UserRights,
            account.UserData,
            account.ReadNote(),
            account.ReadInitialChannel(),
            Math.Max(0, account.AudioCodecBitrateLimit),
            account.ReadLastModified(),
            account.ReadLastLoginTime());
    }

    private static NativeUserAccount CreateNativeUserAccount(UserAccountCreationRequest account)
    {
        NativeUserAccount nativeAccount = default;
        nativeAccount.WriteUsername(account.Username);
        nativeAccount.WritePassword(account.Password);
        nativeAccount.UserType = (uint)account.Type;
        nativeAccount.UserRights = (uint)account.Rights;
        nativeAccount.WriteNote(account.Note);
        nativeAccount.WriteInitialChannel(NormalizeNativeChannelPath(account.InitialChannel));
        nativeAccount.AudioCodecBitrateLimit = Math.Max(0, account.AudioCodecBitrateLimit);
        return nativeAccount;
    }

    private static string NormalizeNativeChannelPath(string? channelPath)
    {
        if (string.IsNullOrWhiteSpace(channelPath) || channelPath == "/")
        {
            return string.Empty;
        }

        return "/" + channelPath.Trim().Trim('/');
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
