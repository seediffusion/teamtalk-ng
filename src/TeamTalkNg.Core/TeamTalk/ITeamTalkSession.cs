namespace TeamTalkNg.Core.TeamTalk;

public interface ITeamTalkSession
{
    event EventHandler<ConnectionStatus>? ConnectionStatusChanged;
    event EventHandler<ChatMessage>? ChannelMessageReceived;
    event EventHandler<ChannelSummary>? ChannelAddedOrUpdated;
    event EventHandler<int>? ChannelRemoved;
    event EventHandler<UserSummary>? UserJoined;
    event EventHandler<UserSummary>? UserUpdated;
    event EventHandler<UserSummary>? UserLeft;
    event EventHandler<FileTransferSummary>? FileTransferUpdated;

    ConnectionStatus Status { get; }

    Task<IReadOnlyList<AudioDeviceSummary>> GetAudioDevicesAsync(CancellationToken cancellationToken = default);

    Task SetAudioDevicesAsync(int? inputDeviceId, int? outputDeviceId, CancellationToken cancellationToken = default);

    Task SetAudioVolumeAsync(int inputVolumePercent, int outputVolumePercent, CancellationToken cancellationToken = default);

    Task SetUserStatusAsync(UserStatusRequest status, CancellationToken cancellationToken = default);

    Task SetNicknameAsync(string nickname, CancellationToken cancellationToken = default);

    Task SetUserAudioSettingsAsync(UserAudioSettingsRequest request, CancellationToken cancellationToken = default);

    Task<ServerInformationSummary> GetServerInformationAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChannelFileSummary>> GetChannelFilesAsync(CancellationToken cancellationToken = default);

    Task UploadFileAsync(string localFilePath, CancellationToken cancellationToken = default);

    Task DownloadFileAsync(int fileId, string localFilePath, CancellationToken cancellationToken = default);

    Task DeleteFileAsync(int fileId, CancellationToken cancellationToken = default);

    Task CancelFileTransferAsync(int transferId, CancellationToken cancellationToken = default);

    Task ConnectAsync(TeamTalkServerProfile profile, CancellationToken cancellationToken = default);

    Task DisconnectAsync(CancellationToken cancellationToken = default);

    Task JoinChannelAsync(string channelPath, string password = "", CancellationToken cancellationToken = default);

    Task CreateChannelAsync(ChannelCreationRequest request, CancellationToken cancellationToken = default);

    Task SetChannelTopicAsync(string channelPath, string topic, CancellationToken cancellationToken = default);

    Task RemoveChannelAsync(string channelPath, CancellationToken cancellationToken = default);

    Task SendChannelMessageAsync(string text, CancellationToken cancellationToken = default);

    Task SendDirectMessageAsync(int userId, string text, CancellationToken cancellationToken = default);

    Task MoveUserAsync(int userId, string destinationChannelPath, CancellationToken cancellationToken = default);

    Task KickUserAsync(int userId, string channelPath, bool fromServer = false, CancellationToken cancellationToken = default);

    Task BanUserAsync(int userId, string channelPath, bool fromServer = false, CancellationToken cancellationToken = default);

    Task SetVoiceTransmissionAsync(bool enabled, CancellationToken cancellationToken = default);

    Task SetVoiceActivationAsync(bool enabled, int level = 50, CancellationToken cancellationToken = default);
}
