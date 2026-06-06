using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using TeamTalkNg.App.Services;
using TeamTalkNg.Core.Accessibility;
using TeamTalkNg.Core.TeamTalk;
using TeamTalkNg.Core.TeamTalk.ConnectionTargets;

namespace TeamTalkNg.App.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private static readonly TimeSpan InitialChannelRosterSoundSuppression = TimeSpan.FromSeconds(2);
    private readonly ITeamTalkSession teamTalkSession;
    private readonly IAnnouncementService announcements;
    private readonly IThemeService themeService;
    private readonly IServerProfileStore profileStore;
    private readonly IConnectionDialogService connectionDialogService;
    private readonly IConnectionTargetDialogService connectionTargetDialogService;
    private readonly IServerInformationDialogService serverInformationDialogService;
    private readonly IServerStatisticsDialogService serverStatisticsDialogService;
    private readonly IBannedUsersDialogService bannedUsersDialogService;
    private readonly IUserAccountsDialogService userAccountsDialogService;
    private readonly IUserAccountDialogService userAccountDialogService;
    private readonly IAppSettingsStore settingsStore;
    private readonly IPreferencesDialogService preferencesDialogService;
    private readonly IChannelDialogService channelDialogService;
    private readonly IChannelInformationDialogService channelInformationDialogService;
    private readonly IChannelTopicDialogService channelTopicDialogService;
    private readonly IDirectMessageDialogService directMessageDialogService;
    private readonly IMoveUserDialogService moveUserDialogService;
    private readonly IUserInformationDialogService userInformationDialogService;
    private readonly IUserAudioSettingsDialogService userAudioSettingsDialogService;
    private readonly IStatusDialogService statusDialogService;
    private readonly IJoinChannelDialogService joinChannelDialogService;
    private readonly INicknameDialogService nicknameDialogService;
    private readonly ISoundEventService soundEvents;
    private readonly DispatcherTimer inputLevelTimer;
    private readonly Dictionary<int, ObservableCollection<ChatMessageViewModel>> directMessageConversations = [];
    private string connectionStatusText = "Disconnected";
    private string liveAnnouncement = "Ready";
    private string messageText = string.Empty;
    private double inputVolume = 50;
    private double outputVolume = 50;
    private int inputLevelPercent;
    private int voiceActivationLevelPercent = 50;
    private ChannelTreeItemViewModel? selectedChannelItem;
    private ChatMessageViewModel? selectedChatMessage;
    private bool pushToTalkEnabled;
    private bool voiceActivationEnabled;
    private bool isInputMeterVisible;
    private bool isPollingInputLevel;
    private bool isAway;
    private string currentNickname = Environment.UserName;
    private string currentChannelPath = "/";
    private bool appliedInitialStatus;
    private TeamTalkServerProfile? activeProfile;
    private AppSettings settings;
    private ChannelTreeItemViewModel? serverTreeItem;
    private FileTransferViewModel? selectedFile;
    private TransferActivityViewModel? selectedTransfer;
    private MediaStreamViewModel? selectedVideoStream;
    private MediaStreamViewModel? selectedDesktopStream;
    private VideoCaptureDeviceSummary? selectedVideoCaptureDevice;
    private bool isVideoCaptureActive;
    private bool isDesktopSharingActive;
    private DateTimeOffset suppressUserJoinSoundsUntil = DateTimeOffset.MinValue;
    private readonly IFileDialogService fileDialogService;

    public MainWindowViewModel(
        ITeamTalkSession teamTalkSession,
        IAnnouncementService announcements,
        IThemeService themeService,
        IServerProfileStore profileStore,
        IConnectionDialogService connectionDialogService,
        IConnectionTargetDialogService connectionTargetDialogService,
        IServerInformationDialogService serverInformationDialogService,
        IServerStatisticsDialogService serverStatisticsDialogService,
        IBannedUsersDialogService bannedUsersDialogService,
        IUserAccountsDialogService userAccountsDialogService,
        IUserAccountDialogService userAccountDialogService,
        IAppSettingsStore settingsStore,
        IPreferencesDialogService preferencesDialogService,
        IChannelDialogService channelDialogService,
        IChannelInformationDialogService channelInformationDialogService,
        IChannelTopicDialogService channelTopicDialogService,
        IDirectMessageDialogService directMessageDialogService,
        IMoveUserDialogService moveUserDialogService,
        IUserInformationDialogService userInformationDialogService,
        IUserAudioSettingsDialogService userAudioSettingsDialogService,
        IStatusDialogService statusDialogService,
        IJoinChannelDialogService joinChannelDialogService,
        INicknameDialogService nicknameDialogService,
        IFileDialogService fileDialogService,
        ISoundEventService soundEvents,
        AppSettings settings)
    {
        this.teamTalkSession = teamTalkSession;
        this.announcements = announcements;
        this.themeService = themeService;
        this.profileStore = profileStore;
        this.connectionDialogService = connectionDialogService;
        this.connectionTargetDialogService = connectionTargetDialogService;
        this.serverInformationDialogService = serverInformationDialogService;
        this.serverStatisticsDialogService = serverStatisticsDialogService;
        this.bannedUsersDialogService = bannedUsersDialogService;
        this.userAccountsDialogService = userAccountsDialogService;
        this.userAccountDialogService = userAccountDialogService;
        this.settingsStore = settingsStore;
        this.preferencesDialogService = preferencesDialogService;
        this.channelDialogService = channelDialogService;
        this.channelInformationDialogService = channelInformationDialogService;
        this.channelTopicDialogService = channelTopicDialogService;
        this.directMessageDialogService = directMessageDialogService;
        this.moveUserDialogService = moveUserDialogService;
        this.userInformationDialogService = userInformationDialogService;
        this.userAudioSettingsDialogService = userAudioSettingsDialogService;
        this.statusDialogService = statusDialogService;
        this.joinChannelDialogService = joinChannelDialogService;
        this.nicknameDialogService = nicknameDialogService;
        this.fileDialogService = fileDialogService;
        this.soundEvents = soundEvents;
        this.settings = settings;
        inputVolume = Math.Clamp(settings.InputVolume, 0, 100);
        outputVolume = Math.Clamp(settings.OutputVolume, 0, 100);
        voiceActivationLevelPercent = Math.Clamp(settings.VoiceActivationLevel, 0, 100);
        isInputMeterVisible = settings.ShowInputMeter;
        currentNickname = GetDefaultNickname();
        isAway = settings.IsAway;
        inputLevelTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(150)
        };
        inputLevelTimer.Tick += OnInputLevelTimerTick;

        ConnectCommand = new AsyncRelayCommand(ConnectAsync, () => teamTalkSession.Status == ConnectionStatus.Disconnected);
        OpenConnectionTargetCommand = new AsyncRelayCommand(OpenConnectionTargetAsync, () => teamTalkSession.Status == ConnectionStatus.Disconnected);
        DisconnectCommand = new AsyncRelayCommand(DisconnectAsync, () => teamTalkSession.Status != ConnectionStatus.Disconnected);
        RefreshAudioDevicesCommand = new AsyncRelayCommand(RefreshAudioDevicesAsync);
        ServerInformationCommand = new AsyncRelayCommand(ShowServerInformationAsync, CanShowServerInformation);
        ServerStatisticsCommand = new AsyncRelayCommand(ShowServerStatisticsAsync, CanUseLoggedInServerCommand);
        BannedUsersCommand = new AsyncRelayCommand(ShowBannedUsersAsync, CanUseLoggedInServerCommand);
        UserAccountsCommand = new AsyncRelayCommand(ShowUserAccountsAsync, CanUseLoggedInServerCommand);
        SaveServerConfigurationCommand = new AsyncRelayCommand(SaveServerConfigurationAsync, CanUseLoggedInServerCommand);
        JoinSelectedChannelCommand = new AsyncRelayCommand(ActivateSelectedTreeItemAsync, CanJoinSelectedChannel);
        ChannelInformationCommand = new RelayCommand(ShowChannelInformation, CanShowChannelInformation);
        EditChannelTopicCommand = new AsyncRelayCommand(EditChannelTopicAsync, CanEditChannelTopic);
        CreateChannelCommand = new AsyncRelayCommand(CreateChannelAsync, CanCreateChannel);
        DeleteSelectedChannelCommand = new AsyncRelayCommand(DeleteSelectedChannelAsync, CanDeleteSelectedChannel);
        UserInformationCommand = new RelayCommand(ShowUserInformation, CanShowUserInformation);
        SendDirectMessageCommand = new AsyncRelayCommand(SendDirectMessageAsync, CanSendDirectMessage);
        MoveUserCommand = new AsyncRelayCommand(MoveUserAsync, CanMoveSelectedUser);
        KickUserFromChannelCommand = new AsyncRelayCommand(KickUserFromChannelAsync, CanModerateSelectedUser);
        KickUserFromServerCommand = new AsyncRelayCommand(KickUserFromServerAsync, CanModerateSelectedUser);
        BanUserFromServerCommand = new AsyncRelayCommand(BanUserFromServerAsync, CanModerateSelectedUser);
        UserAudioSettingsCommand = new AsyncRelayCommand(ShowUserAudioSettingsAsync, CanChangeUserAudioSettings);
        ToggleSelectedUserMuteCommand = new AsyncRelayCommand(ToggleSelectedUserMuteAsync, CanChangeUserAudioSettings);
        UploadFileCommand = new AsyncRelayCommand(UploadFileAsync, CanManageFiles);
        DownloadFileCommand = new AsyncRelayCommand(DownloadFileAsync, CanUseSelectedFile);
        DeleteFileCommand = new AsyncRelayCommand(DeleteFileAsync, CanUseSelectedFile);
        CancelTransferCommand = new AsyncRelayCommand(CancelTransferAsync, CanCancelSelectedTransfer);
        RefreshFilesCommand = new AsyncRelayCommand(() => RefreshFilesAsync(), CanRefreshFiles);
        SendMessageCommand = new AsyncRelayCommand(SendMessageAsync);
        TogglePushToTalkCommand = new AsyncRelayCommand(TogglePushToTalkAsync, CanUseVoiceControls);
        ToggleVoiceActivationCommand = new AsyncRelayCommand(ToggleVoiceActivationAsync, CanUseVoiceControls);
        RefreshVideoDevicesCommand = new AsyncRelayCommand(RefreshVideoDevicesAsync);
        ToggleVideoCaptureCommand = new AsyncRelayCommand(ToggleVideoCaptureAsync, CanToggleVideoCapture);
        ShareDesktopCommand = new AsyncRelayCommand(() => StartDesktopShareAsync(DesktopShareSource.FullDesktop), CanStartDesktopShare);
        ShareActiveWindowCommand = new AsyncRelayCommand(() => StartDesktopShareAsync(DesktopShareSource.ActiveWindow), CanStartDesktopShare);
        StopDesktopShareCommand = new AsyncRelayCommand(StopDesktopShareAsync, CanStopDesktopShare);
        ToggleInputMeterCommand = new AsyncRelayCommand(ToggleInputMeterAsync);
        ChangeNicknameCommand = new AsyncRelayCommand(ChangeNicknameAsync, CanSetProfileState);
        SetStatusCommand = new AsyncRelayCommand(SetStatusAsync, CanSetStatus);
        SimulateUserJoinedCommand = new RelayCommand(SimulateUserJoined);
        UseLightThemeCommand = new AsyncRelayCommand(() => SetThemeAsync(AppTheme.Light));
        UseDarkThemeCommand = new AsyncRelayCommand(() => SetThemeAsync(AppTheme.Dark));
        PreferencesCommand = new AsyncRelayCommand(ShowPreferencesAsync);
        AboutCommand = new RelayCommand(ShowAbout);
        ExitCommand = new RelayCommand(() => Application.Current.Shutdown());

        announcements.AnnouncementRaised += OnAnnouncementRaised;
        teamTalkSession.ConnectionStatusChanged += OnConnectionStatusChanged;
        teamTalkSession.ChannelMessageReceived += OnChannelMessageReceived;
        teamTalkSession.ChannelAddedOrUpdated += OnChannelAddedOrUpdated;
        teamTalkSession.ChannelRemoved += OnChannelRemoved;
        teamTalkSession.UserJoined += OnUserJoined;
        teamTalkSession.UserUpdated += OnUserUpdated;
        teamTalkSession.UserLeft += OnUserLeft;
        teamTalkSession.FileTransferUpdated += OnFileTransferUpdated;
        teamTalkSession.MediaFrameReceived += OnMediaFrameReceived;
        teamTalkSession.ServerInformationUpdated += OnServerInformationUpdated;

        BuildDisconnectedTree();
        ShowFilesPlaceholder("Join a channel to view files");
        UpdateInputLevelTimer();
    }

    public string WindowTitle => $"TeamTalk NG - {ConnectionStatusText}";

    public ObservableCollection<ChannelTreeItemViewModel> Channels { get; } = [];

    public ObservableCollection<ChatMessageViewModel> ChatMessages { get; } = [];

    public ChatMessageViewModel? SelectedChatMessage
    {
        get => selectedChatMessage;
        set => SetProperty(ref selectedChatMessage, value);
    }

    public ObservableCollection<FileTransferViewModel> Files { get; } = [];

    public ObservableCollection<TransferActivityViewModel> Transfers { get; } = [];

    public ObservableCollection<MediaStreamViewModel> VideoStreams { get; } = [];

    public ObservableCollection<MediaStreamViewModel> DesktopStreams { get; } = [];

    public ObservableCollection<VideoCaptureDeviceSummary> VideoCaptureDevices { get; } = [];

    public VideoCaptureDeviceSummary? SelectedVideoCaptureDevice
    {
        get => selectedVideoCaptureDevice;
        set
        {
            if (SetProperty(ref selectedVideoCaptureDevice, value))
            {
                OnPropertyChanged(nameof(SelectedVideoCaptureFormatText));
                RaiseMediaCommandStateChanged();
            }
        }
    }

    public string SelectedVideoCaptureFormatText => SelectBestVideoFormat(SelectedVideoCaptureDevice)?.DisplayName ?? "No camera format selected";

    public bool IsVideoCaptureActive
    {
        get => isVideoCaptureActive;
        private set
        {
            if (SetProperty(ref isVideoCaptureActive, value))
            {
                OnPropertyChanged(nameof(VideoCaptureButtonText));
                RaiseMediaCommandStateChanged();
            }
        }
    }

    public bool IsDesktopSharingActive
    {
        get => isDesktopSharingActive;
        private set
        {
            if (SetProperty(ref isDesktopSharingActive, value))
            {
                RaiseMediaCommandStateChanged();
            }
        }
    }

    public string VideoCaptureButtonText => IsVideoCaptureActive ? "Stop Video" : "Start Video";

    public MediaStreamViewModel? SelectedVideoStream
    {
        get => selectedVideoStream;
        set
        {
            if (SetProperty(ref selectedVideoStream, value))
            {
                RaiseMediaPreviewPropertiesChanged();
            }
        }
    }

    public MediaStreamViewModel? SelectedDesktopStream
    {
        get => selectedDesktopStream;
        set
        {
            if (SetProperty(ref selectedDesktopStream, value))
            {
                RaiseMediaPreviewPropertiesChanged();
            }
        }
    }

    public string VideoPreviewName => SelectedVideoStream?.PreviewName ?? "No active video streams";

    public string DesktopPreviewName => SelectedDesktopStream?.PreviewName ?? "No shared desktops";

    public Visibility VideoPreviewVisibility => SelectedVideoStream?.Frame is null ? Visibility.Collapsed : Visibility.Visible;

    public Visibility DesktopPreviewVisibility => SelectedDesktopStream?.Frame is null ? Visibility.Collapsed : Visibility.Visible;

    public Visibility NoVideoStreamsVisibility => VideoStreams.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility NoDesktopStreamsVisibility => DesktopStreams.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public FileTransferViewModel? SelectedFile
    {
        get => selectedFile;
        set
        {
            if (SetProperty(ref selectedFile, value))
            {
                RaiseFileCommandStateChanged();
            }
        }
    }

    public TransferActivityViewModel? SelectedTransfer
    {
        get => selectedTransfer;
        set
        {
            if (SetProperty(ref selectedTransfer, value) && CancelTransferCommand is AsyncRelayCommand command)
            {
                command.RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand ConnectCommand { get; }

    public ICommand OpenConnectionTargetCommand { get; }

    public ICommand DisconnectCommand { get; }

    public ICommand RefreshAudioDevicesCommand { get; }

    public ICommand ServerInformationCommand { get; }

    public ICommand ServerStatisticsCommand { get; }

    public ICommand BannedUsersCommand { get; }

    public ICommand UserAccountsCommand { get; }

    public ICommand SaveServerConfigurationCommand { get; }

    public ICommand JoinSelectedChannelCommand { get; }

    public ICommand ChannelInformationCommand { get; }

    public ICommand EditChannelTopicCommand { get; }

    public ICommand CreateChannelCommand { get; }

    public ICommand DeleteSelectedChannelCommand { get; }

    public ICommand UserInformationCommand { get; }

    public ICommand SendDirectMessageCommand { get; }

    public ICommand MoveUserCommand { get; }

    public ICommand KickUserFromChannelCommand { get; }

    public ICommand KickUserFromServerCommand { get; }

    public ICommand BanUserFromServerCommand { get; }

    public ICommand UserAudioSettingsCommand { get; }

    public ICommand ToggleSelectedUserMuteCommand { get; }

    public ICommand UploadFileCommand { get; }

    public ICommand DownloadFileCommand { get; }

    public ICommand DeleteFileCommand { get; }

    public ICommand CancelTransferCommand { get; }

    public ICommand RefreshFilesCommand { get; }

    public ICommand SendMessageCommand { get; }

    public ICommand TogglePushToTalkCommand { get; }

    public ICommand ToggleVoiceActivationCommand { get; }

    public ICommand RefreshVideoDevicesCommand { get; }

    public ICommand ToggleVideoCaptureCommand { get; }

    public ICommand ShareDesktopCommand { get; }

    public ICommand ShareActiveWindowCommand { get; }

    public ICommand StopDesktopShareCommand { get; }

    public ICommand ToggleInputMeterCommand { get; }

    public ICommand SetStatusCommand { get; }

    public ICommand ChangeNicknameCommand { get; }

    public ICommand SimulateUserJoinedCommand { get; }

    public ICommand UseLightThemeCommand { get; }

    public ICommand UseDarkThemeCommand { get; }

    public ICommand PreferencesCommand { get; }

    public ICommand AboutCommand { get; }

    public ICommand ExitCommand { get; }

    public string ConnectionStatusText
    {
        get => connectionStatusText;
        private set
        {
            if (SetProperty(ref connectionStatusText, value))
            {
                OnPropertyChanged(nameof(WindowTitle));
            }
        }
    }

    public string LiveAnnouncement
    {
        get => liveAnnouncement;
        private set => SetProperty(ref liveAnnouncement, value);
    }

    public string MessageText
    {
        get => messageText;
        set => SetProperty(ref messageText, value);
    }

    public double InputVolume
    {
        get => inputVolume;
        set
        {
            if (SetProperty(ref inputVolume, Math.Clamp(value, 0, 100)))
            {
                _ = ApplyAudioVolumeAsync();
            }
        }
    }

    public double OutputVolume
    {
        get => outputVolume;
        set
        {
            if (SetProperty(ref outputVolume, Math.Clamp(value, 0, 100)))
            {
                _ = ApplyAudioVolumeAsync();
            }
        }
    }

    public int InputLevelPercent
    {
        get => inputLevelPercent;
        private set
        {
            int clampedValue = Math.Clamp(value, 0, 100);
            if (SetProperty(ref inputLevelPercent, clampedValue))
            {
                OnPropertyChanged(nameof(InputLevelMeterWidth));
                OnPropertyChanged(nameof(InputLevelText));
            }
        }
    }

    public int VoiceActivationLevelPercent
    {
        get => voiceActivationLevelPercent;
        set
        {
            if (SetProperty(ref voiceActivationLevelPercent, Math.Clamp(value, 0, 100)))
            {
                OnPropertyChanged(nameof(VoiceActivationMarkerMargin));
                if (!isPollingInputLevel)
                {
                    _ = ApplyVoiceActivationLevelAsync();
                }
            }
        }
    }

    public double InputLevelMeterWidth => InputLevelPercent * 1.8;

    public Thickness VoiceActivationMarkerMargin => new(VoiceActivationLevelPercent * 1.8, 0, 0, 0);

    public string InputLevelText => $"Input level {InputLevelPercent} percent";

    public bool IsInputMeterVisible
    {
        get => isInputMeterVisible;
        private set
        {
            if (SetProperty(ref isInputMeterVisible, value))
            {
                UpdateInputLevelTimer();
            }
        }
    }

    public bool IsPushToTalkEnabled
    {
        get => pushToTalkEnabled;
        private set => SetProperty(ref pushToTalkEnabled, value);
    }

    public bool IsVoiceActivationEnabled
    {
        get => voiceActivationEnabled;
        private set => SetProperty(ref voiceActivationEnabled, value);
    }

    public bool IsAway
    {
        get => isAway;
        private set => SetProperty(ref isAway, value);
    }

    public ChannelTreeItemViewModel? SelectedChannelItem
    {
        get => selectedChannelItem;
        set
        {
            if (SetProperty(ref selectedChannelItem, value) && value is not null)
            {
                _ = AnnounceAsync($"Selected {value.AccessibleName}", AnnouncementPriority.Low, AnnouncementKind.Selection);
            }

            RaiseChannelCommandStateChanged();
        }
    }

    public async Task ActivateSelectedTreeItemAsync()
    {
        if (SelectedChannelItem is not { Kind: ChannelTreeItemKind.Channel } channel)
        {
            return;
        }

        string previousChannelPath = currentChannelPath;
        try
        {
            string password = string.Empty;
            if (channel.IsProtected)
            {
                string? enteredPassword = joinChannelDialogService.ShowJoinChannelDialog(channel.Name);
                if (enteredPassword is null)
                {
                    await AnnounceAsync("Join channel canceled", AnnouncementPriority.Low, AnnouncementKind.System, includeBraille: false);
                    return;
                }

                password = enteredPassword;
            }

            SetCurrentChannelPath(channel.Path);
            await AnnounceAsync($"Joining {channel.Name}", AnnouncementPriority.Normal, AnnouncementKind.System);
            await teamTalkSession.JoinChannelAsync(channel.Path, password);
            ExpandChannelPath(channel.Path);
        }
        catch (Exception ex)
        {
            SetCurrentChannelPath(previousChannelPath);
            OnPropertyChanged(nameof(IsPushToTalkEnabled));
            await AnnounceAsync(ex.Message, AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        }
    }

    public async Task OpenSelectedDirectMessageReplyAsync()
    {
        if (SelectedChatMessage is not { IsDirect: true, DirectUserId: > 0 } message)
        {
            return;
        }

        string recipientName = GetUserDisplayName(message.DirectUserId.Value, message.Sender);
        await SendDirectMessageToUserAsync(message.DirectUserId.Value, recipientName);
    }

    private async Task ConnectAsync()
    {
        IReadOnlyList<TeamTalkServerProfile> profiles = await profileStore.LoadAsync();
        IReadOnlyList<TeamTalkServerProfile> profilesWithIdentity = profiles.Select(ApplyIdentityDefaults).ToList();
        TeamTalkServerProfile? profile = connectionDialogService.ShowConnectDialog(profilesWithIdentity);
        if (profile is null)
        {
            await AnnounceAsync("Connect canceled", AnnouncementPriority.Low, AnnouncementKind.System, includeBraille: false);
            return;
        }

        TeamTalkServerProfile effectiveProfile = ApplyIdentityDefaults(profile);
        activeProfile = effectiveProfile;
        currentNickname = effectiveProfile.Nickname;
        await SaveRecentProfileAsync(profilesWithIdentity, effectiveProfile);
        await ConnectToProfileAsync(effectiveProfile);
    }

    public async Task ConnectToProfileAsync(TeamTalkServerProfile profile)
    {
        TeamTalkServerProfile effectiveProfile = ApplyIdentityDefaults(profile);
        activeProfile = effectiveProfile;
        SetCurrentChannelPath(effectiveProfile.ChannelPath);
        currentNickname = effectiveProfile.Nickname;
        appliedInitialStatus = false;
        await AnnounceAsync($"Connecting to {effectiveProfile.DisplayName}", AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        soundEvents.Play(SoundEvent.Connecting);
        BuildConnectingTree(effectiveProfile);
        try
        {
            await teamTalkSession.SetAudioDevicesAsync(settings.AudioInputDeviceId, settings.AudioOutputDeviceId);
            await teamTalkSession.SetAudioProcessingAsync(CreateAudioProcessingSettings(settings));
            await teamTalkSession.SetAudioVolumeAsync((int)Math.Round(InputVolume), (int)Math.Round(OutputVolume));
            await teamTalkSession.ConnectAsync(effectiveProfile);
            RaiseCommandStateChanged();
        }
        catch (Exception ex)
        {
            activeProfile = null;
            currentChannelPath = "/";
            BuildDisconnectedTree();
            await AnnounceAsync(ex.Message, AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
            RaiseCommandStateChanged();
        }
    }

    private async Task OpenConnectionTargetAsync()
    {
        string? target = connectionTargetDialogService.ShowConnectionTargetDialog();
        if (string.IsNullOrWhiteSpace(target))
        {
            await AnnounceAsync("Open connection target canceled", AnnouncementPriority.Low, AnnouncementKind.System, includeBraille: false);
            return;
        }

        if (!TeamTalkConnectionTargetParser.TryParse(target, out TeamTalkServerProfile profile, out string error))
        {
            await AnnounceAsync(error, AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
            return;
        }

        TeamTalkServerProfile effectiveProfile = ApplyIdentityDefaults(profile);
        IReadOnlyList<TeamTalkServerProfile> profiles = await profileStore.LoadAsync();
        await SaveRecentProfileAsync(profiles, effectiveProfile);
        await ConnectToProfileAsync(effectiveProfile);
    }

    private async Task DisconnectAsync()
    {
        await teamTalkSession.DisconnectAsync();
        activeProfile = null;
        currentChannelPath = "/";
        appliedInitialStatus = false;
        BuildDisconnectedTree();
        ChatMessages.Clear();
        directMessageConversations.Clear();
        Transfers.Clear();
        SelectedTransfer = null;
        ClearMediaStreams();
        ShowFilesPlaceholder("Join a channel to view files");
        await AnnounceAsync("Disconnected", AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        RaiseCommandStateChanged();
    }

    private async Task ShowServerInformationAsync()
    {
        try
        {
            ServerInformationSummary serverInformation = await teamTalkSession.GetServerInformationAsync();
            serverInformationDialogService.ShowServerInformationDialog(serverInformation);
        }
        catch (Exception ex)
        {
            await AnnounceAsync(ex.Message, AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        }
    }

    private async Task ShowServerStatisticsAsync()
    {
        try
        {
            ServerStatisticsSummary serverStatistics = await teamTalkSession.GetServerStatisticsAsync();
            serverStatisticsDialogService.ShowServerStatisticsDialog(serverStatistics);
        }
        catch (Exception ex)
        {
            await AnnounceAsync(ex.Message, AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        }
    }

    private async Task ShowBannedUsersAsync()
    {
        try
        {
            IReadOnlyList<BannedUserSummary> bannedUsers = await teamTalkSession.GetBannedUsersAsync();
            if (bannedUsers.Count == 0)
            {
                await AnnounceAsync("No banned users", AnnouncementPriority.Normal, AnnouncementKind.System);
                return;
            }

            BannedUserSummary? selectedBan = bannedUsersDialogService.ShowBannedUsersDialog(bannedUsers);
            if (selectedBan is null)
            {
                await AnnounceAsync("Banned users closed", AnnouncementPriority.Low, AnnouncementKind.System, includeBraille: false);
                return;
            }

            await teamTalkSession.UnbanUserAsync(selectedBan);
            await AnnounceAsync($"Remove ban command sent for {selectedBan.DisplayName}", AnnouncementPriority.Normal, AnnouncementKind.System);
        }
        catch (Exception ex)
        {
            await AnnounceAsync(ex.Message, AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        }
    }

    private async Task ShowUserAccountsAsync()
    {
        try
        {
            IReadOnlyList<UserAccountSummary> accounts = await teamTalkSession.GetUserAccountsAsync();
            UserAccountsDialogResult result = userAccountsDialogService.ShowUserAccountsDialog(accounts);
            switch (result.Action)
            {
                case UserAccountsDialogAction.New:
                    UserAccountCreationRequest? request = userAccountDialogService.ShowCreateUserAccountDialog();
                    if (request is null)
                    {
                        await AnnounceAsync("New user account canceled", AnnouncementPriority.Low, AnnouncementKind.System, includeBraille: false);
                        return;
                    }

                    await teamTalkSession.CreateUserAccountAsync(request);
                    await AnnounceAsync($"Create user account command sent for {request.Username}", AnnouncementPriority.Normal, AnnouncementKind.System);
                    break;
                case UserAccountsDialogAction.Delete:
                    if (result.SelectedAccount is null)
                    {
                        await AnnounceAsync("Select a user account before deleting", AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
                        return;
                    }

                    MessageBoxResult confirmation = MessageBox.Show(
                        $"Delete user account {result.SelectedAccount.DisplayName}?",
                        "Delete User Account",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning,
                        MessageBoxResult.No);
                    if (confirmation != MessageBoxResult.Yes)
                    {
                        await AnnounceAsync("Delete user account canceled", AnnouncementPriority.Low, AnnouncementKind.System, includeBraille: false);
                        return;
                    }

                    await teamTalkSession.DeleteUserAccountAsync(result.SelectedAccount.Username);
                    await AnnounceAsync($"Delete user account command sent for {result.SelectedAccount.DisplayName}", AnnouncementPriority.Normal, AnnouncementKind.System);
                    break;
                default:
                    await AnnounceAsync("User accounts closed", AnnouncementPriority.Low, AnnouncementKind.System, includeBraille: false);
                    break;
            }
        }
        catch (Exception ex)
        {
            await AnnounceAsync(ex.Message, AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        }
    }

    private async Task SaveServerConfigurationAsync()
    {
        try
        {
            await teamTalkSession.SaveServerConfigurationAsync();
            await AnnounceAsync("Save server configuration command sent", AnnouncementPriority.Normal, AnnouncementKind.System);
        }
        catch (Exception ex)
        {
            await AnnounceAsync(ex.Message, AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        }
    }

    private async Task RefreshAudioDevicesAsync()
    {
        try
        {
            await teamTalkSession.GetAudioDevicesAsync();
            await teamTalkSession.SetAudioDevicesAsync(settings.AudioInputDeviceId, settings.AudioOutputDeviceId);
            await teamTalkSession.SetAudioProcessingAsync(CreateAudioProcessingSettings(settings));
            await teamTalkSession.SetAudioVolumeAsync((int)Math.Round(InputVolume), (int)Math.Round(OutputVolume));
            IsPushToTalkEnabled = false;
            IsVoiceActivationEnabled = false;
            await AnnounceAsync("Audio devices refreshed", AnnouncementPriority.Normal, AnnouncementKind.System);
        }
        catch (Exception ex)
        {
            await AnnounceAsync(ex.Message, AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        }
    }

    private async Task RefreshVideoDevicesAsync()
    {
        try
        {
            IReadOnlyList<VideoCaptureDeviceSummary> devices = await teamTalkSession.GetVideoCaptureDevicesAsync();
            VideoCaptureDevices.Clear();
            foreach (VideoCaptureDeviceSummary device in devices)
            {
                VideoCaptureDevices.Add(device);
            }

            SelectedVideoCaptureDevice = VideoCaptureDevices.FirstOrDefault();
            string message = VideoCaptureDevices.Count == 0
                ? "No cameras found"
                : $"{VideoCaptureDevices.Count} camera{(VideoCaptureDevices.Count == 1 ? string.Empty : "s")} found";
            await AnnounceAsync(message, AnnouncementPriority.Normal, AnnouncementKind.System, includeBraille: false);
        }
        catch (Exception ex)
        {
            await AnnounceAsync(ex.Message, AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        }
    }

    private async Task ToggleVideoCaptureAsync()
    {
        if (IsVideoCaptureActive)
        {
            try
            {
                await teamTalkSession.StopVideoCaptureAsync();
                IsVideoCaptureActive = false;
                soundEvents.Play(SoundEvent.VideoStopped);
                await AnnounceAsync("Video transmission stopped", AnnouncementPriority.Normal, AnnouncementKind.System);
            }
            catch (Exception ex)
            {
                await AnnounceAsync(ex.Message, AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
            }

            return;
        }

        if (SelectedVideoCaptureDevice is null)
        {
            await RefreshVideoDevicesAsync();
        }

        VideoCaptureDeviceSummary? device = SelectedVideoCaptureDevice;
        VideoCaptureFormatSummary? format = SelectBestVideoFormat(device);
        if (device is null || format is null)
        {
            await AnnounceAsync("No camera with a supported format is available.", AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
            return;
        }

        try
        {
            await teamTalkSession.StartVideoCaptureAsync(device.DeviceId, format);
            IsVideoCaptureActive = true;
            soundEvents.Play(SoundEvent.VideoStarted);
            await AnnounceAsync($"Video transmission started using {device.Name}", AnnouncementPriority.Normal, AnnouncementKind.System);
        }
        catch (Exception ex)
        {
            IsVideoCaptureActive = false;
            await AnnounceAsync(ex.Message, AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        }
    }

    private async Task StartDesktopShareAsync(DesktopShareSource source)
    {
        try
        {
            await teamTalkSession.StartDesktopShareAsync(source);
            IsDesktopSharingActive = true;
            soundEvents.Play(SoundEvent.DesktopShareStarted);
            await AnnounceAsync(
                source == DesktopShareSource.ActiveWindow ? "Active window sharing started" : "Desktop sharing started",
                AnnouncementPriority.Normal,
                AnnouncementKind.System);
        }
        catch (Exception ex)
        {
            await AnnounceAsync(ex.Message, AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        }
    }

    private async Task StopDesktopShareAsync()
    {
        try
        {
            await teamTalkSession.StopDesktopShareAsync();
            IsDesktopSharingActive = false;
            soundEvents.Play(SoundEvent.DesktopShareStopped);
            await AnnounceAsync("Desktop sharing stopped", AnnouncementPriority.Normal, AnnouncementKind.System);
        }
        catch (Exception ex)
        {
            await AnnounceAsync(ex.Message, AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        }
    }

    private async Task SendMessageAsync()
    {
        string text = MessageText.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        MessageText = string.Empty;
        try
        {
            await teamTalkSession.SendChannelMessageAsync(text);
            soundEvents.Play(SoundEvent.ChannelMessageSent);
            await AnnounceAsync(
                FormatAnnouncementTemplate(AnnouncementTemplateKind.ChannelMessageSent, ("message", text)),
                AnnouncementPriority.Low,
                AnnouncementKind.System,
                includeBraille: false,
                eventKind: AnnouncementTemplateKind.ChannelMessageSent);
        }
        catch (Exception ex)
        {
            OnPropertyChanged(nameof(IsVoiceActivationEnabled));
            await AnnounceAsync(ex.Message, AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        }
    }

    private async Task RefreshFilesAsync(bool announce = true)
    {
        if (!CanRefreshFiles())
        {
            ShowFilesPlaceholder("Join a channel to view files");
            return;
        }

        try
        {
            IReadOnlyList<ChannelFileSummary> files = await teamTalkSession.GetChannelFilesAsync();
            Application.Current.Dispatcher.Invoke(() =>
            {
                SelectedFile = null;
                Files.Clear();
                foreach (FileTransferViewModel file in files
                    .OrderBy(file => file.Name, StringComparer.CurrentCultureIgnoreCase)
                    .Select(FileTransferViewModel.FromSummary))
                {
                    Files.Add(file);
                }

                if (Files.Count == 0)
                {
                    Files.Add(new FileTransferViewModel(0, "No files in current channel", string.Empty, string.Empty));
                }
            });

            if (announce)
            {
                string text = files.Count == 0
                    ? "No files in current channel"
                    : $"{files.Count} channel file{(files.Count == 1 ? string.Empty : "s")} listed";
                await AnnounceAsync(text, AnnouncementPriority.Normal, AnnouncementKind.System, includeBraille: false);
            }
        }
        catch (Exception ex)
        {
            ShowFilesPlaceholder("Channel files unavailable");
            if (announce)
            {
                await AnnounceAsync(ex.Message, AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
            }
        }
    }

    private async Task UploadFileAsync()
    {
        string? path = fileDialogService.ShowUploadFileDialog();
        if (string.IsNullOrWhiteSpace(path))
        {
            await AnnounceAsync("Upload file canceled", AnnouncementPriority.Low, AnnouncementKind.System, includeBraille: false);
            return;
        }

        try
        {
            await teamTalkSession.UploadFileAsync(path);
            await AnnounceAsync($"Upload command sent for {Path.GetFileName(path)}", AnnouncementPriority.Normal, AnnouncementKind.System);
            await RefreshFilesAsync(announce: false);
        }
        catch (Exception ex)
        {
            await AnnounceAsync(ex.Message, AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        }
    }

    private async Task DownloadFileAsync()
    {
        if (SelectedFile is not { IsPlaceholder: false } file)
        {
            await AnnounceAsync("Select a file before downloading", AnnouncementPriority.Low, AnnouncementKind.System, includeBraille: false);
            return;
        }

        string? path = fileDialogService.ShowDownloadFileDialog(file.Name);
        if (string.IsNullOrWhiteSpace(path))
        {
            await AnnounceAsync("Download file canceled", AnnouncementPriority.Low, AnnouncementKind.System, includeBraille: false);
            return;
        }

        try
        {
            await teamTalkSession.DownloadFileAsync(file.Id, path);
            await AnnounceAsync($"Download command sent for {file.Name}", AnnouncementPriority.Normal, AnnouncementKind.System);
        }
        catch (Exception ex)
        {
            await AnnounceAsync(ex.Message, AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        }
    }

    private async Task DeleteFileAsync()
    {
        if (SelectedFile is not { IsPlaceholder: false } file)
        {
            await AnnounceAsync("Select a file before deleting", AnnouncementPriority.Low, AnnouncementKind.System, includeBraille: false);
            return;
        }

        if (!ConfirmUserAction($"Delete file {file.Name}?", "Delete File"))
        {
            await AnnounceAsync("Delete file canceled", AnnouncementPriority.Low, AnnouncementKind.System, includeBraille: false);
            return;
        }

        try
        {
            await teamTalkSession.DeleteFileAsync(file.Id);
            await AnnounceAsync($"Delete command sent for {file.Name}", AnnouncementPriority.Normal, AnnouncementKind.System);
            await RefreshFilesAsync(announce: false);
        }
        catch (Exception ex)
        {
            await AnnounceAsync(ex.Message, AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        }
    }

    private async Task CancelTransferAsync()
    {
        if (SelectedTransfer is not { IsActive: true } transfer)
        {
            await AnnounceAsync("Select an active transfer before canceling", AnnouncementPriority.Low, AnnouncementKind.System, includeBraille: false);
            return;
        }

        try
        {
            await teamTalkSession.CancelFileTransferAsync(transfer.TransferId);
            await AnnounceAsync($"Cancel command sent for {transfer.RemoteFileName}", AnnouncementPriority.Normal, AnnouncementKind.System);
        }
        catch (Exception ex)
        {
            await AnnounceAsync(ex.Message, AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        }
    }

    private async Task CreateChannelAsync()
    {
        string parentPath = SelectedChannelItem is { Kind: ChannelTreeItemKind.Channel } channel
            ? channel.Path
            : "/";

        ChannelCreationRequest? request = channelDialogService.ShowCreateChannelDialog(parentPath);
        if (request is null)
        {
            await AnnounceAsync("Create channel canceled", AnnouncementPriority.Low, AnnouncementKind.System, includeBraille: false);
            return;
        }

        try
        {
            await teamTalkSession.CreateChannelAsync(request);
            await AnnounceAsync($"Creating channel {request.Name}", AnnouncementPriority.Normal, AnnouncementKind.System);
        }
        catch (Exception ex)
        {
            await AnnounceAsync(ex.Message, AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        }
    }

    private void ShowChannelInformation()
    {
        if (SelectedChannelItem is { Kind: ChannelTreeItemKind.Channel } channel)
        {
            channelInformationDialogService.ShowChannelInformationDialog(channel);
        }
    }

    private async Task EditChannelTopicAsync()
    {
        if (SelectedChannelItem is not { Kind: ChannelTreeItemKind.Channel } channel)
        {
            return;
        }

        string? topic = channelTopicDialogService.ShowChannelTopicDialog(channel.Name, channel.Topic);
        if (topic is null)
        {
            await AnnounceAsync("Channel topic edit canceled", AnnouncementPriority.Low, AnnouncementKind.System, includeBraille: false);
            return;
        }

        try
        {
            await teamTalkSession.SetChannelTopicAsync(channel.Path, topic);
            channel.Topic = topic.Trim();
            await AnnounceAsync($"Updated topic for {channel.Name}", AnnouncementPriority.Normal, AnnouncementKind.System);
        }
        catch (Exception ex)
        {
            await AnnounceAsync(ex.Message, AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        }
    }

    private async Task DeleteSelectedChannelAsync()
    {
        if (SelectedChannelItem is not { Kind: ChannelTreeItemKind.Channel } channel)
        {
            return;
        }

        MessageBoxResult result = MessageBox.Show(
            $"Delete channel {channel.Name}?",
            "Delete Channel",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        if (result != MessageBoxResult.Yes)
        {
            await AnnounceAsync("Delete channel canceled", AnnouncementPriority.Low, AnnouncementKind.System, includeBraille: false);
            return;
        }

        try
        {
            await teamTalkSession.RemoveChannelAsync(channel.Path);
            await AnnounceAsync($"Deleting channel {channel.Name}", AnnouncementPriority.Normal, AnnouncementKind.System);
        }
        catch (Exception ex)
        {
            await AnnounceAsync(ex.Message, AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        }
    }

    private void ShowUserInformation()
    {
        if (SelectedChannelItem is { Kind: ChannelTreeItemKind.User } user)
        {
            userInformationDialogService.ShowUserInformationDialog(user);
        }
    }

    private async Task ShowUserAudioSettingsAsync()
    {
        if (SelectedChannelItem is not { Kind: ChannelTreeItemKind.User } user)
        {
            return;
        }

        UserAudioSettingsRequest? request = userAudioSettingsDialogService.ShowUserAudioSettingsDialog(user);
        if (request is null)
        {
            await AnnounceAsync("User audio settings canceled", AnnouncementPriority.Low, AnnouncementKind.System, includeBraille: false);
            return;
        }

        try
        {
            await teamTalkSession.SetUserAudioSettingsAsync(request);
            user.VoiceVolumePercent = request.VoiceVolumePercent;
            user.IsVoiceMuted = request.IsVoiceMuted;
            string message = request.IsVoiceMuted
                ? $"Muted voice for {user.Name}"
                : $"Set voice volume for {user.Name} to {request.VoiceVolumePercent} percent";
            await AnnounceAsync(message, AnnouncementPriority.Normal, AnnouncementKind.System);
        }
        catch (Exception ex)
        {
            await AnnounceAsync(ex.Message, AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        }
    }

    private async Task ToggleSelectedUserMuteAsync()
    {
        if (SelectedChannelItem is not { Kind: ChannelTreeItemKind.User } user)
        {
            return;
        }

        bool targetMuted = !user.IsVoiceMuted;
        var request = new UserAudioSettingsRequest(user.Id, user.VoiceVolumePercent, targetMuted);

        try
        {
            await teamTalkSession.SetUserAudioSettingsAsync(request);
            user.IsVoiceMuted = targetMuted;
            string message = targetMuted
                ? $"Muted voice for {user.Name}"
                : $"Unmuted voice for {user.Name}";
            await AnnounceAsync(message, AnnouncementPriority.Normal, AnnouncementKind.System);
        }
        catch (Exception ex)
        {
            await AnnounceAsync(ex.Message, AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        }
    }

    private async Task SendDirectMessageAsync()
    {
        if (SelectedChannelItem is not { Kind: ChannelTreeItemKind.User } user)
        {
            return;
        }

        await SendDirectMessageToUserAsync(user.Id, user.Name);
    }

    private async Task SendDirectMessageToUserAsync(int userId, string recipientName)
    {
        try
        {
            string? message = directMessageDialogService.ShowDirectMessageDialog(
                recipientName,
                GetDirectMessageConversation(userId));
            if (string.IsNullOrWhiteSpace(message))
            {
                await AnnounceAsync("Direct message canceled", AnnouncementPriority.Low, AnnouncementKind.System, includeBraille: false);
                return;
            }

            await teamTalkSession.SendDirectMessageAsync(userId, message);
            soundEvents.Play(SoundEvent.DirectMessageSent);
            await AnnounceAsync(
                FormatAnnouncementTemplate(
                    AnnouncementTemplateKind.DirectMessageSent,
                    ("user", recipientName),
                    ("username", recipientName),
                    ("message", message)),
                AnnouncementPriority.Low,
                AnnouncementKind.System,
                includeBraille: false,
                eventKind: AnnouncementTemplateKind.DirectMessageSent);
        }
        catch (Exception ex)
        {
            await AnnounceAsync(ex.Message, AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        }
    }

    private async Task MoveUserAsync()
    {
        if (SelectedChannelItem is not { Kind: ChannelTreeItemKind.User } user)
        {
            return;
        }

        IReadOnlyList<MoveUserDestinationViewModel> destinations = BuildMoveUserDestinations(user.Path);
        if (destinations.Count == 0)
        {
            await AnnounceAsync("No destination channels are available.", AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
            return;
        }

        string? destinationPath = moveUserDialogService.ShowMoveUserDialog(user.Name, destinations);
        if (destinationPath is null)
        {
            await AnnounceAsync("Move user canceled", AnnouncementPriority.Low, AnnouncementKind.System, includeBraille: false);
            return;
        }

        try
        {
            await teamTalkSession.MoveUserAsync(user.Id, destinationPath);
            await AnnounceAsync($"Move command sent for {user.Name}", AnnouncementPriority.Normal, AnnouncementKind.System);
        }
        catch (Exception ex)
        {
            await AnnounceAsync(ex.Message, AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        }
    }

    private async Task KickUserFromChannelAsync()
    {
        if (SelectedChannelItem is not { Kind: ChannelTreeItemKind.User } user)
        {
            return;
        }

        if (!ConfirmUserAction($"Kick {user.Name} from {GetChannelName(user.Path)}?", "Kick User"))
        {
            await AnnounceAsync("Kick user canceled", AnnouncementPriority.Low, AnnouncementKind.System, includeBraille: false);
            return;
        }

        try
        {
            await teamTalkSession.KickUserAsync(user.Id, user.Path);
            await AnnounceAsync($"Kick command sent for {user.Name}", AnnouncementPriority.Normal, AnnouncementKind.System);
        }
        catch (Exception ex)
        {
            await AnnounceAsync(ex.Message, AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        }
    }

    private async Task KickUserFromServerAsync()
    {
        if (SelectedChannelItem is not { Kind: ChannelTreeItemKind.User } user)
        {
            return;
        }

        if (!ConfirmUserAction($"Kick {user.Name} from the server?", "Kick User"))
        {
            await AnnounceAsync("Kick user canceled", AnnouncementPriority.Low, AnnouncementKind.System, includeBraille: false);
            return;
        }

        try
        {
            await teamTalkSession.KickUserAsync(user.Id, user.Path, fromServer: true);
            await AnnounceAsync($"Server kick command sent for {user.Name}", AnnouncementPriority.Normal, AnnouncementKind.System);
        }
        catch (Exception ex)
        {
            await AnnounceAsync(ex.Message, AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        }
    }

    private async Task BanUserFromServerAsync()
    {
        if (SelectedChannelItem is not { Kind: ChannelTreeItemKind.User } user)
        {
            return;
        }

        if (!ConfirmUserAction($"Ban {user.Name} from the server?", "Ban User"))
        {
            await AnnounceAsync("Ban user canceled", AnnouncementPriority.Low, AnnouncementKind.System, includeBraille: false);
            return;
        }

        try
        {
            await teamTalkSession.BanUserAsync(user.Id, user.Path, fromServer: true);
            await AnnounceAsync($"Server ban command sent for {user.Name}", AnnouncementPriority.Normal, AnnouncementKind.System);
        }
        catch (Exception ex)
        {
            await AnnounceAsync(ex.Message, AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        }
    }

    private async Task TogglePushToTalkAsync()
    {
        bool target = !IsPushToTalkEnabled;
        try
        {
            await teamTalkSession.SetVoiceTransmissionAsync(target);
            IsPushToTalkEnabled = target;
            if (target)
            {
                IsVoiceActivationEnabled = false;
            }

            string state = target ? "enabled" : "disabled";
            soundEvents.Play(target ? SoundEvent.PushToTalkEnabled : SoundEvent.PushToTalkDisabled);
            await AnnounceAsync($"Push to talk {state}", AnnouncementPriority.Normal, AnnouncementKind.System);
        }
        catch (Exception ex)
        {
            await AnnounceAsync(ex.Message, AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        }
    }

    private async Task ToggleVoiceActivationAsync()
    {
        bool target = !IsVoiceActivationEnabled;
        try
        {
            await teamTalkSession.SetVoiceActivationAsync(target, settings.VoiceActivationLevel);
            IsVoiceActivationEnabled = target;
            if (target)
            {
                IsPushToTalkEnabled = false;
            }

            string state = target ? "enabled" : "disabled";
            soundEvents.Play(target ? SoundEvent.VoiceActivationEnabled : SoundEvent.VoiceActivationDisabled);
            await AnnounceAsync($"Voice activation {state}", AnnouncementPriority.Normal, AnnouncementKind.System);
        }
        catch (Exception ex)
        {
            await AnnounceAsync(ex.Message, AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        }
    }

    private async Task SetStatusAsync()
    {
        UserStatusRequest? request = statusDialogService.ShowStatusDialog(IsAway, settings.StatusMessage);
        if (request is null)
        {
            await AnnounceAsync("Status change canceled", AnnouncementPriority.Low, AnnouncementKind.System, includeBraille: false);
            return;
        }

        try
        {
            await teamTalkSession.SetUserStatusAsync(request);
            IsAway = request.IsAway;
            settings = settings with { IsAway = request.IsAway, StatusMessage = request.Message };
            await settingsStore.SaveAsync(settings);
            await AnnounceAsync(request.IsAway ? "Status set to away" : "Status set to available", AnnouncementPriority.Normal, AnnouncementKind.System);
        }
        catch (Exception ex)
        {
            await AnnounceAsync(ex.Message, AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        }
    }

    private async Task ChangeNicknameAsync()
    {
        string? nickname = nicknameDialogService.ShowNicknameDialog(currentNickname);
        if (string.IsNullOrWhiteSpace(nickname))
        {
            await AnnounceAsync("Nickname change canceled", AnnouncementPriority.Low, AnnouncementKind.System, includeBraille: false);
            return;
        }

        try
        {
            await teamTalkSession.SetNicknameAsync(nickname);
            currentNickname = nickname.Trim();
            activeProfile = activeProfile is null ? null : activeProfile with { Nickname = currentNickname };
            settings = settings with { DefaultNickname = currentNickname };
            await settingsStore.SaveAsync(settings);
            await AnnounceAsync($"Nickname changed to {currentNickname}", AnnouncementPriority.Normal, AnnouncementKind.System);
        }
        catch (Exception ex)
        {
            await AnnounceAsync(ex.Message, AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        }
    }

    private async Task ShowPreferencesAsync()
    {
        IReadOnlyList<AudioDeviceSummary> audioDevices = await teamTalkSession.GetAudioDevicesAsync();
        AppSettings? updatedSettings = preferencesDialogService.ShowPreferencesDialog(
            settings,
            audioDevices,
            soundEvents.GetSoundEvents(),
            soundEvents.GetSoundPacks(),
            () => teamTalkSession.GetAudioDevicesAsync(),
            () => teamTalkSession.GetAudioInputLevelAsync(),
            soundEvents.GetSoundFileName,
            soundEvents.Preview);
        if (updatedSettings is null)
        {
            return;
        }

        settings = updatedSettings;
        themeService.UseTheme(settings.Theme);
        soundEvents.Configure(settings.PlaySoundEvents, settings.SoundPack, settings.SoundEventVolume, settings.SoundEventEnabled);
        ApplyChatHistoryPrivacySetting();
        IsInputMeterVisible = settings.ShowInputMeter;
        VoiceActivationLevelPercent = settings.VoiceActivationLevel;
        if (teamTalkSession.Status == ConnectionStatus.Disconnected)
        {
            currentNickname = GetDefaultNickname();
            IsAway = settings.IsAway;
        }

        await teamTalkSession.SetAudioDevicesAsync(settings.AudioInputDeviceId, settings.AudioOutputDeviceId);
        await teamTalkSession.SetAudioProcessingAsync(CreateAudioProcessingSettings(settings));
        await teamTalkSession.SetAudioVolumeAsync((int)Math.Round(InputVolume), (int)Math.Round(OutputVolume));
        IsPushToTalkEnabled = false;
        IsVoiceActivationEnabled = false;
        await settingsStore.SaveAsync(settings);
        await AnnounceAsync("Preferences saved", AnnouncementPriority.Normal, AnnouncementKind.System);
    }

    private async Task ToggleInputMeterAsync()
    {
        IsInputMeterVisible = !IsInputMeterVisible;
        settings = settings with { ShowInputMeter = IsInputMeterVisible };
        await settingsStore.SaveAsync(settings);
        if (!IsInputMeterVisible)
        {
            InputLevelPercent = 0;
        }

        await AnnounceAsync(IsInputMeterVisible ? "Input meter shown" : "Input meter hidden", AnnouncementPriority.Normal, AnnouncementKind.System);
    }

    private async Task ApplyAudioVolumeAsync()
    {
        int input = (int)Math.Round(InputVolume);
        int output = (int)Math.Round(OutputVolume);
        settings = settings with
        {
            InputVolume = input,
            OutputVolume = output
        };

        try
        {
            await teamTalkSession.SetAudioVolumeAsync(input, output);
            await settingsStore.SaveAsync(settings);
        }
        catch (Exception ex)
        {
            await AnnounceAsync(ex.Message, AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        }
    }

    private async Task ApplyVoiceActivationLevelAsync()
    {
        int level = VoiceActivationLevelPercent;
        settings = settings with { VoiceActivationLevel = level };

        try
        {
            if (teamTalkSession.Status == ConnectionStatus.InChannel && IsVoiceActivationEnabled)
            {
                await teamTalkSession.SetVoiceActivationAsync(true, level);
            }

            await settingsStore.SaveAsync(settings);
        }
        catch (Exception ex)
        {
            await AnnounceAsync(ex.Message, AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        }
    }

    private static AudioProcessingSettings CreateAudioProcessingSettings(AppSettings settings)
    {
        return new AudioProcessingSettings(
            settings.EnableNoiseSuppression,
            settings.EnableEchoCancellation,
            settings.EnableAutomaticGainControl);
    }

    private void SimulateUserJoined()
    {
        if (teamTalkSession is MockTeamTalkSession mock)
        {
            mock.SimulateUserJoined();
        }
    }

    private static void ShowAbout()
    {
        MessageBox.Show(
            "TeamTalk NG\nModern accessible Windows client for TeamTalk 5.",
            "About TeamTalk NG",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private static bool ConfirmUserAction(string message, string title)
    {
        return MessageBox.Show(
            message,
            title,
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No) == MessageBoxResult.Yes;
    }

    private void OnConnectionStatusChanged(object? sender, ConnectionStatus status)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            ConnectionStatusText = status switch
            {
                ConnectionStatus.Disconnected => "Disconnected",
                ConnectionStatus.Connecting => "Connecting",
                ConnectionStatus.Connected => "Connected; logging in",
                ConnectionStatus.LoggedIn => "Logged in",
                ConnectionStatus.InChannel => $"In {currentChannelPath}",
                _ => status.ToString()
            };

            if (status != ConnectionStatus.InChannel)
            {
                suppressUserJoinSoundsUntil = DateTimeOffset.MinValue;
                if (status == ConnectionStatus.Disconnected)
                {
                    currentChannelPath = "/";
                }

                IsPushToTalkEnabled = false;
                IsVoiceActivationEnabled = false;
                IsVideoCaptureActive = false;
                IsDesktopSharingActive = false;
                InputLevelPercent = 0;
                ClearMediaStreams();
                ShowFilesPlaceholder("Join a channel to view files");
            }
            else
            {
                ExpandChannelPath(currentChannelPath);
                _ = RefreshFilesAsync(announce: false);
            }

            UpdateInputLevelTimer();
            RaiseCommandStateChanged();
            RaiseChannelCommandStateChanged();
        });

        switch (status)
        {
            case ConnectionStatus.LoggedIn:
                soundEvents.Play(SoundEvent.Connected);
                break;
            case ConnectionStatus.InChannel:
                suppressUserJoinSoundsUntil = DateTimeOffset.UtcNow.Add(InitialChannelRosterSoundSuppression);
                soundEvents.Play(SoundEvent.JoinedChannel);
                break;
            case ConnectionStatus.Disconnected:
                soundEvents.Play(SoundEvent.Disconnected);
                break;
        }

        if (status is ConnectionStatus.LoggedIn or ConnectionStatus.InChannel)
        {
            _ = ApplyInitialStatusAsync();
        }
    }

    private void OnServerInformationUpdated(object? sender, ServerInformationSummary serverInformation)
    {
        string serverName = serverInformation.ServerName.Trim();
        if (string.IsNullOrWhiteSpace(serverName))
        {
            return;
        }

        Application.Current.Dispatcher.Invoke(() =>
        {
            if (serverTreeItem is not null)
            {
                serverTreeItem.Name = serverName;
            }
        });
    }

    private async void OnInputLevelTimerTick(object? sender, EventArgs e)
    {
        if (!IsInputMeterVisible || isPollingInputLevel)
        {
            return;
        }

        isPollingInputLevel = true;
        try
        {
            AudioInputLevelSummary level = await teamTalkSession.GetAudioInputLevelAsync();
            InputLevelPercent = level.ClampedLevel;
            VoiceActivationLevelPercent = level.ClampedVoiceActivationLevel;
        }
        catch
        {
            InputLevelPercent = 0;
            UpdateInputLevelTimer();
        }
        finally
        {
            isPollingInputLevel = false;
        }
    }

    private void UpdateInputLevelTimer()
    {
        if (IsInputMeterVisible && teamTalkSession.Status == ConnectionStatus.InChannel)
        {
            if (!inputLevelTimer.IsEnabled)
            {
                inputLevelTimer.Start();
            }
        }
        else if (inputLevelTimer.IsEnabled)
        {
            inputLevelTimer.Stop();
        }
    }

    private void OnChannelMessageReceived(object? sender, ChatMessage message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var chatMessage = new ChatMessageViewModel(message, settings.HideDirectMessageTextInChatHistory);
            ChatMessages.Add(chatMessage);
            RememberDirectMessage(chatMessage);
        });

        string announcement = message.IsSystem
            ? message.Text
            : FormatAnnouncementTemplate(
                message.IsDirect ? AnnouncementTemplateKind.DirectMessage : AnnouncementTemplateKind.ChannelMessage,
                ("user", GetAnnouncementSender(message)),
                ("username", GetAnnouncementSender(message)),
                ("message", message.Text));
        if (!message.IsSystem)
        {
            soundEvents.Play(message.IsDirect ? SoundEvent.DirectMessage : SoundEvent.ChannelMessage);
        }
        else if (message.Sender.StartsWith("Broadcast from ", StringComparison.OrdinalIgnoreCase))
        {
            soundEvents.Play(SoundEvent.BroadcastMessage);
        }

        _ = AnnounceAsync(
            announcement,
            message.IsDirect ? AnnouncementPriority.High : AnnouncementPriority.Normal,
            message.IsSystem
                ? AnnouncementKind.System
                : message.IsDirect ? AnnouncementKind.DirectMessage : AnnouncementKind.ChannelMessage,
            eventKind: message.IsSystem
                ? null
                : message.IsDirect ? AnnouncementTemplateKind.DirectMessage : AnnouncementTemplateKind.ChannelMessage);
    }

    private void OnFileTransferUpdated(object? sender, FileTransferSummary transfer)
    {
        bool isNew = false;
        TransferActivityViewModel? transferViewModel = null;
        Application.Current.Dispatcher.Invoke(() =>
        {
            transferViewModel = Transfers.FirstOrDefault(item => item.TransferId == transfer.TransferId);
            if (transferViewModel is null)
            {
                transferViewModel = new TransferActivityViewModel(transfer);
                Transfers.Insert(0, transferViewModel);
                isNew = true;
            }
            else
            {
                transferViewModel.Update(transfer);
            }

            if (SelectedTransfer?.TransferId == transfer.TransferId)
            {
                SelectedTransfer = transferViewModel;
            }

            if (transfer.Status == TeamTalkFileTransferStatus.Finished && !transfer.IsDownload)
            {
                _ = RefreshFilesAsync(announce: false);
            }

            if (CancelTransferCommand is AsyncRelayCommand command)
            {
                command.RaiseCanExecuteChanged();
            }
        });

        if (isNew && transfer.Status == TeamTalkFileTransferStatus.Active)
        {
            soundEvents.Play(SoundEvent.FileTransferStarted);
            _ = AnnounceAsync($"{(transfer.IsDownload ? "Download" : "Upload")} started for {transfer.RemoteFileName}", AnnouncementPriority.Normal, AnnouncementKind.System);
        }
        else if (transfer.Status == TeamTalkFileTransferStatus.Finished)
        {
            soundEvents.Play(SoundEvent.FileTransferFinished);
            _ = AnnounceAsync($"{(transfer.IsDownload ? "Download" : "Upload")} finished for {transfer.RemoteFileName}", AnnouncementPriority.Normal, AnnouncementKind.System);
        }
        else if (transfer.Status == TeamTalkFileTransferStatus.Error)
        {
            soundEvents.Play(SoundEvent.FileTransferFailed);
            _ = AnnounceAsync($"File transfer failed for {transfer.RemoteFileName}", AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        }
        else if (transfer.Status == TeamTalkFileTransferStatus.Closed)
        {
            soundEvents.Play(SoundEvent.FileTransferCanceled);
            _ = AnnounceAsync($"File transfer canceled for {transfer.RemoteFileName}", AnnouncementPriority.Normal, AnnouncementKind.System);
        }
    }

    private void OnMediaFrameReceived(object? sender, MediaFrameSummary frame)
    {
        bool isNewStream = false;
        Application.Current.Dispatcher.Invoke(() =>
        {
            ObservableCollection<MediaStreamViewModel> streams = frame.Kind == MediaStreamKind.Video
                ? VideoStreams
                : DesktopStreams;
            MediaStreamViewModel? stream = streams.FirstOrDefault(item => item.UserId == frame.UserId);
            if (stream is null)
            {
                stream = new MediaStreamViewModel(frame);
                streams.Add(stream);
                isNewStream = true;

                if (frame.Kind == MediaStreamKind.Video && SelectedVideoStream is null)
                {
                    SelectedVideoStream = stream;
                }
                else if (frame.Kind == MediaStreamKind.Desktop && SelectedDesktopStream is null)
                {
                    SelectedDesktopStream = stream;
                }
            }
            else
            {
                stream.Update(frame);
            }

            RaiseMediaPreviewPropertiesChanged();
        });

        if (isNewStream)
        {
            string kind = frame.Kind == MediaStreamKind.Video ? "video" : "desktop";
            _ = AnnounceAsync($"{frame.DisplayName} {kind} stream available", AnnouncementPriority.Normal, AnnouncementKind.System, includeBraille: false);
        }
    }

    private void OnUserJoined(object? sender, UserSummary user)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            AddOrUpdateUser(user);
        });

        if (ShouldPlayLiveUserChannelEvent(user.ChannelPath))
        {
            soundEvents.Play(SoundEvent.UserJoined);
            _ = AnnounceAsync(
                FormatAnnouncementTemplate(
                    AnnouncementTemplateKind.UserJoinedChannel,
                    ("user", user.Nickname),
                    ("username", user.Username),
                    ("channel", GetChannelName(user.ChannelPath))),
                AnnouncementPriority.Normal,
                AnnouncementKind.UserJoinLeave,
                eventKind: AnnouncementTemplateKind.UserJoinedChannel);
        }
    }

    private void OnUserUpdated(object? sender, UserSummary user)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            AddOrUpdateUser(user);
        });
    }

    private void OnUserLeft(object? sender, UserSummary user)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            ChannelTreeItemViewModel? channel = FindChannelByPath(user.ChannelPath);
            ChannelTreeItemViewModel? existingUser = channel?.Children.FirstOrDefault(item => item.Kind == ChannelTreeItemKind.User && item.Id == user.Id);
            if (existingUser is not null && channel is not null)
            {
                channel.Children.Remove(existingUser);
                channel.UserCount = channel.Children.Count(item => item.Kind == ChannelTreeItemKind.User);
            }

            RemoveMediaStreamsForUser(user.Id);
        });

        if (ShouldPlayLiveUserChannelEvent(user.ChannelPath))
        {
            soundEvents.Play(SoundEvent.UserLeft);
            _ = AnnounceAsync(
                FormatAnnouncementTemplate(
                    AnnouncementTemplateKind.UserLeftChannel,
                    ("user", user.Nickname),
                    ("username", user.Username),
                    ("channel", GetChannelName(user.ChannelPath))),
                AnnouncementPriority.Normal,
                AnnouncementKind.UserJoinLeave,
                eventKind: AnnouncementTemplateKind.UserLeftChannel);
        }
    }

    private void AddOrUpdateUser(UserSummary user)
    {
        ChannelTreeItemViewModel channel = EnsureChannel(user.ChannelPath, id: 0);
        ChannelTreeItemViewModel? existingUser = Descendants(serverTreeItem)
            .FirstOrDefault(item => item.Kind == ChannelTreeItemKind.User && item.Id == user.Id);

        if (existingUser is not null && !string.Equals(existingUser.Path, user.ChannelPath, StringComparison.OrdinalIgnoreCase))
        {
            RemoveTreeItem(existingUser);
            RefreshChannelUserCounts();
            existingUser = null;
        }

        if (existingUser is null)
        {
            existingUser = new ChannelTreeItemViewModel(user.Nickname, ChannelTreeItemKind.User, user.Id, user.ChannelPath);
            channel.Children.Add(existingUser);
        }

        existingUser.Name = user.Nickname;
        existingUser.Username = user.Username;
        existingUser.IsTalking = user.IsTalking;
        existingUser.IsAway = user.IsAway;
        existingUser.IsOperator = user.IsOperator;
        existingUser.StatusMessage = user.StatusMessage;
        existingUser.VoiceVolumePercent = user.VoiceVolumePercent;
        existingUser.IsVoiceMuted = user.IsVoiceMuted;
        channel.UserCount = channel.Children.Count(item => item.Kind == ChannelTreeItemKind.User);
    }

    private bool ShouldPlayLiveUserChannelEvent(string userChannelPath)
    {
        return teamTalkSession.Status == ConnectionStatus.InChannel
            && DateTimeOffset.UtcNow >= suppressUserJoinSoundsUntil
            && IsSameChannelPathForLiveUserEvent(currentChannelPath, userChannelPath);
    }

    private void OnChannelAddedOrUpdated(object? sender, ChannelSummary channel)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            ChannelTreeItemViewModel item = EnsureChannel(channel.Path, channel.Id);
            item.Name = channel.Name;
            item.IsProtected = channel.IsProtected;
            item.IsPermanent = channel.IsPermanent;
            item.Topic = channel.Topic;
            item.UserCount = channel.UserCount > 0 ? channel.UserCount : item.Children.Count(child => child.Kind == ChannelTreeItemKind.User);
        });
    }

    private void OnChannelRemoved(object? sender, int channelId)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (serverTreeItem is null)
            {
                return;
            }

            ChannelTreeItemViewModel? channel = FindChannelById(channelId);
            if (channel is not null)
            {
                RemoveTreeItem(channel);
            }
        });
    }

    private void OnAnnouncementRaised(object? sender, ScreenReaderAnnouncement announcement)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (announcement.UpdateLiveRegion)
            {
                if (Keyboard.FocusedElement is TextBoxBase)
                {
                    return;
                }

                LiveAnnouncement = announcement.Text;
            }
        });
    }

    private void BuildDisconnectedTree()
    {
        Channels.Clear();
        serverTreeItem = new ChannelTreeItemViewModel("Not connected", ChannelTreeItemKind.Server);
        Channels.Add(serverTreeItem);
    }

    private void BuildConnectingTree(TeamTalkServerProfile profile)
    {
        Channels.Clear();
        serverTreeItem = new ChannelTreeItemViewModel(profile.DisplayName, ChannelTreeItemKind.Server);
        Channels.Add(serverTreeItem);
    }

    private void ShowFilesPlaceholder(string text)
    {
        SelectedFile = null;
        Files.Clear();
        Files.Add(new FileTransferViewModel(0, text, string.Empty, string.Empty));
    }

    private void ClearMediaStreams()
    {
        SelectedVideoStream = null;
        SelectedDesktopStream = null;
        VideoStreams.Clear();
        DesktopStreams.Clear();
        RaiseMediaPreviewPropertiesChanged();
    }

    private void RemoveMediaStreamsForUser(int userId)
    {
        MediaStreamViewModel? video = VideoStreams.FirstOrDefault(item => item.UserId == userId);
        if (video is not null)
        {
            VideoStreams.Remove(video);
            if (SelectedVideoStream == video)
            {
                SelectedVideoStream = VideoStreams.FirstOrDefault();
            }
        }

        MediaStreamViewModel? desktop = DesktopStreams.FirstOrDefault(item => item.UserId == userId);
        if (desktop is not null)
        {
            DesktopStreams.Remove(desktop);
            if (SelectedDesktopStream == desktop)
            {
                SelectedDesktopStream = DesktopStreams.FirstOrDefault();
            }
        }

        RaiseMediaPreviewPropertiesChanged();
    }

    private void RaiseMediaPreviewPropertiesChanged()
    {
        OnPropertyChanged(nameof(VideoPreviewName));
        OnPropertyChanged(nameof(DesktopPreviewName));
        OnPropertyChanged(nameof(VideoPreviewVisibility));
        OnPropertyChanged(nameof(DesktopPreviewVisibility));
        OnPropertyChanged(nameof(NoVideoStreamsVisibility));
        OnPropertyChanged(nameof(NoDesktopStreamsVisibility));
    }

    private ChannelTreeItemViewModel EnsureChannel(string? channelPath, int id)
    {
        string normalizedPath = NormalizeChannelPath(channelPath);
        serverTreeItem ??= Channels.FirstOrDefault();
        if (serverTreeItem is null)
        {
            serverTreeItem = new ChannelTreeItemViewModel(activeProfile?.DisplayName ?? "Connected server", ChannelTreeItemKind.Server);
            Channels.Add(serverTreeItem);
        }

        ChannelTreeItemViewModel? existing = FindChannelByPath(normalizedPath);
        if (existing is not null)
        {
            if (id > 0 && existing.Id == 0)
            {
                existing.Id = id;
            }

            return existing;
        }

        ChannelTreeItemViewModel parent = serverTreeItem;
        string currentPath = string.Empty;
        foreach (string segment in normalizedPath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            currentPath += "/" + segment;
            ChannelTreeItemViewModel? child = parent.Children.FirstOrDefault(item => item.Kind == ChannelTreeItemKind.Channel
                && string.Equals(item.Path, currentPath, StringComparison.OrdinalIgnoreCase));
            if (child is null)
            {
                child = new ChannelTreeItemViewModel(segment, ChannelTreeItemKind.Channel, currentPath == normalizedPath ? id : 0, currentPath);
                parent.Children.Add(child);
            }
            else if (currentPath == normalizedPath && id > 0 && child.Id == 0)
            {
                child.Id = id;
            }

            parent = child;
        }

        if (normalizedPath == "/")
        {
            ChannelTreeItemViewModel? rootChannel = serverTreeItem.Children.FirstOrDefault(item => item.Kind == ChannelTreeItemKind.Channel && item.Path == "/");
            if (rootChannel is null)
            {
                rootChannel = new ChannelTreeItemViewModel("Root", ChannelTreeItemKind.Channel, id, "/");
                rootChannel.IsPermanent = true;
                serverTreeItem.Children.Add(rootChannel);
            }

            return rootChannel;
        }

        return parent;
    }

    private void ExpandChannelPath(string? channelPath)
    {
        if (serverTreeItem is null)
        {
            return;
        }

        serverTreeItem.IsExpanded = true;
        string normalizedPath = NormalizeChannelPath(channelPath);
        if (normalizedPath == "/")
        {
            ChannelTreeItemViewModel? rootChannel = FindChannelByPath("/");
            if (rootChannel is not null)
            {
                rootChannel.IsExpanded = true;
            }

            return;
        }

        string currentPath = string.Empty;
        foreach (string segment in normalizedPath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            currentPath += "/" + segment;
            ChannelTreeItemViewModel? channel = FindChannelByPath(currentPath);
            if (channel is null)
            {
                return;
            }

            channel.IsExpanded = true;
        }
    }

    private ChannelTreeItemViewModel? FindChannelByPath(string? channelPath)
    {
        string normalizedPath = NormalizeChannelPath(channelPath);
        return Descendants(serverTreeItem).FirstOrDefault(item => item.Kind == ChannelTreeItemKind.Channel
            && string.Equals(item.Path, normalizedPath, StringComparison.OrdinalIgnoreCase));
    }

    private ChannelTreeItemViewModel? FindChannelById(int channelId)
    {
        return channelId <= 0
            ? null
            : Descendants(serverTreeItem).FirstOrDefault(item => item.Kind == ChannelTreeItemKind.Channel && item.Id == channelId);
    }

    private bool RemoveTreeItem(ChannelTreeItemViewModel item)
    {
        if (serverTreeItem is null)
        {
            return false;
        }

        return RemoveTreeItem(serverTreeItem.Children, item);
    }

    private void RefreshChannelUserCounts()
    {
        foreach (ChannelTreeItemViewModel channel in Descendants(serverTreeItem).Where(item => item.Kind == ChannelTreeItemKind.Channel))
        {
            channel.UserCount = channel.Children.Count(child => child.Kind == ChannelTreeItemKind.User);
        }
    }

    private IReadOnlyList<MoveUserDestinationViewModel> BuildMoveUserDestinations(string currentChannelPath)
    {
        string normalizedCurrentChannelPath = NormalizeChannelPath(currentChannelPath);
        return Descendants(serverTreeItem)
            .Where(item => item.Kind == ChannelTreeItemKind.Channel)
            .Where(item => !string.Equals(NormalizeChannelPath(item.Path), normalizedCurrentChannelPath, StringComparison.OrdinalIgnoreCase))
            .Select(item => new MoveUserDestinationViewModel(item.Name, NormalizeChannelPath(item.Path)))
            .OrderBy(item => item.Path, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static bool RemoveTreeItem(IList<ChannelTreeItemViewModel> collection, ChannelTreeItemViewModel item)
    {
        if (collection.Remove(item))
        {
            return true;
        }

        foreach (ChannelTreeItemViewModel child in collection)
        {
            if (RemoveTreeItem(child.Children, item))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<ChannelTreeItemViewModel> Descendants(ChannelTreeItemViewModel? root)
    {
        if (root is null)
        {
            yield break;
        }

        foreach (ChannelTreeItemViewModel child in root.Children)
        {
            yield return child;

            foreach (ChannelTreeItemViewModel descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }

    private static string NormalizeChannelPath(string? channelPath)
    {
        if (string.IsNullOrWhiteSpace(channelPath) || channelPath == "/")
        {
            return "/";
        }

        string trimmed = channelPath.Trim();
        return "/" + trimmed.Trim('/');
    }

    private void SetCurrentChannelPath(string? channelPath)
    {
        currentChannelPath = NormalizeChannelPath(channelPath);
        activeProfile = activeProfile is null ? null : activeProfile with { ChannelPath = currentChannelPath };

        if (teamTalkSession.Status == ConnectionStatus.InChannel)
        {
            ConnectionStatusText = $"In {currentChannelPath}";
        }
    }

    internal static bool IsSameChannelPathForLiveUserEvent(string? currentChannelPath, string? userChannelPath)
    {
        return string.Equals(
            NormalizeChannelPath(currentChannelPath),
            NormalizeChannelPath(userChannelPath),
            StringComparison.OrdinalIgnoreCase);
    }

    private async Task SaveRecentProfileAsync(IReadOnlyList<TeamTalkServerProfile> profiles, TeamTalkServerProfile selectedProfile)
    {
        List<TeamTalkServerProfile> updatedProfiles = profiles
            .Where(profile => !string.Equals(profile.Host, selectedProfile.Host, StringComparison.OrdinalIgnoreCase)
                || profile.TcpPort != selectedProfile.TcpPort
                || profile.UdpPort != selectedProfile.UdpPort)
            .ToList();

        updatedProfiles.Insert(0, selectedProfile);
        await profileStore.SaveAsync(updatedProfiles);
    }

    private async Task ApplyInitialStatusAsync()
    {
        if (appliedInitialStatus || teamTalkSession.Status is not (ConnectionStatus.LoggedIn or ConnectionStatus.InChannel))
        {
            return;
        }

        appliedInitialStatus = true;
        if (!settings.IsAway && string.IsNullOrWhiteSpace(settings.StatusMessage))
        {
            return;
        }

        var request = new UserStatusRequest(settings.IsAway, settings.StatusMessage);
        try
        {
            await teamTalkSession.SetUserStatusAsync(request);
            Application.Current.Dispatcher.Invoke(() => IsAway = request.IsAway);
        }
        catch (Exception ex)
        {
            await AnnounceAsync($"Could not set startup status: {ex.Message}", AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        }
    }

    private TeamTalkServerProfile ApplyIdentityDefaults(TeamTalkServerProfile profile)
    {
        string nickname = string.IsNullOrWhiteSpace(profile.Nickname)
            ? GetDefaultNickname()
            : profile.Nickname.Trim();
        return profile with { Nickname = nickname };
    }

    private string GetDefaultNickname()
    {
        return string.IsNullOrWhiteSpace(settings.DefaultNickname)
            ? Environment.UserName
            : settings.DefaultNickname.Trim();
    }

    private async Task SetThemeAsync(AppTheme theme)
    {
        settings = settings with { Theme = theme };
        themeService.UseTheme(theme);
        await settingsStore.SaveAsync(settings);
        await AnnounceAsync($"{theme} theme selected", AnnouncementPriority.Normal, AnnouncementKind.System);
    }

    private Task AnnounceAsync(
        string text,
        AnnouncementPriority priority,
        AnnouncementKind kind,
        bool interrupt = false,
        bool? includeBraille = null,
        AnnouncementTemplateKind? eventKind = null)
    {
        if (!ShouldAnnounce(kind) || !ShouldAnnounceEvent(eventKind))
        {
            return Task.CompletedTask;
        }

        bool updateLiveRegion = ShouldUpdateStatusBar(kind);
        bool effectiveInterrupt = interrupt && settings.InterruptImportantAnnouncements;
        bool allowPriorityInterrupt = settings.InterruptImportantAnnouncements;

        return announcements.AnnounceAsync(new ScreenReaderAnnouncement(
            text,
            priority,
            effectiveInterrupt,
            includeBraille ?? settings.SendAnnouncementsToBraille,
            updateLiveRegion,
            allowPriorityInterrupt)).AsTask();
    }

    private bool ShouldUpdateStatusBar(AnnouncementKind kind)
    {
        if (!settings.ShowAnnouncementsInStatusBar || kind == AnnouncementKind.Selection)
        {
            return false;
        }

        return kind is not (AnnouncementKind.ChannelMessage or AnnouncementKind.DirectMessage)
            || settings.ShowMessageAnnouncementsInStatusBar;
    }

    private bool ShouldAnnounce(AnnouncementKind kind)
    {
        return kind switch
        {
            AnnouncementKind.ChannelMessage => settings.AnnounceChannelMessages,
            AnnouncementKind.DirectMessage => settings.AnnounceDirectMessages,
            AnnouncementKind.UserJoinLeave => settings.AnnounceUserJoinLeave,
            AnnouncementKind.Selection => settings.AnnounceSelectionChanges,
            _ => true
        };
    }

    private bool ShouldAnnounceEvent(AnnouncementTemplateKind? eventKind)
    {
        return eventKind is null || AnnouncementTemplateFormatter.IsEnabled(settings, eventKind.Value);
    }

    private string FormatAnnouncementTemplate(AnnouncementTemplateKind kind, params (string Key, string Value)[] values)
    {
        var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["server"] = activeProfile?.DisplayName ?? "server"
        };

        foreach ((string key, string value) in values)
        {
            replacements[key] = value;
        }

        return AnnouncementTemplateFormatter.Format(settings, kind, replacements);
    }

    private static string GetAnnouncementSender(ChatMessage message)
    {
        if (!message.IsDirect)
        {
            return message.Sender;
        }

        const string directFromPrefix = "Direct from ";
        const string directToPrefix = "Direct to ";
        if (message.Sender.StartsWith(directFromPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return message.Sender[directFromPrefix.Length..];
        }

        if (message.Sender.StartsWith(directToPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return message.Sender[directToPrefix.Length..];
        }

        return message.Sender.StartsWith("Direct ", StringComparison.OrdinalIgnoreCase)
            ? message.Sender["Direct ".Length..]
            : message.Sender;
    }

    private string GetUserDisplayName(int userId, string fallback)
    {
        ChannelTreeItemViewModel? user = Descendants(serverTreeItem)
            .FirstOrDefault(item => item.Kind == ChannelTreeItemKind.User && item.Id == userId);

        if (user is not null && !string.IsNullOrWhiteSpace(user.Name))
        {
            return user.Name;
        }

        string cleanedFallback = GetDirectMessageParticipantName(fallback);
        return string.IsNullOrWhiteSpace(cleanedFallback) ? $"User {userId}" : cleanedFallback;
    }

    private IReadOnlyList<ChatMessageViewModel> GetDirectMessageConversation(int userId)
    {
        return directMessageConversations.TryGetValue(userId, out ObservableCollection<ChatMessageViewModel>? conversation)
            ? conversation
            : [];
    }

    private void RememberDirectMessage(ChatMessageViewModel message)
    {
        if (message is not { IsDirect: true, DirectUserId: > 0 })
        {
            return;
        }

        if (!directMessageConversations.TryGetValue(message.DirectUserId.Value, out ObservableCollection<ChatMessageViewModel>? conversation))
        {
            conversation = [];
            directMessageConversations[message.DirectUserId.Value] = conversation;
        }

        conversation.Add(message);
    }

    private void ApplyChatHistoryPrivacySetting()
    {
        foreach (ChatMessageViewModel message in ChatMessages)
        {
            message.HideDirectMessageText = settings.HideDirectMessageTextInChatHistory;
        }
    }

    private static string GetDirectMessageParticipantName(string value)
    {
        const string directFromPrefix = "Direct from ";
        const string directToPrefix = "Direct to ";

        if (value.StartsWith(directFromPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return value[directFromPrefix.Length..];
        }

        if (value.StartsWith(directToPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return value[directToPrefix.Length..];
        }

        return value.StartsWith("Direct ", StringComparison.OrdinalIgnoreCase)
            ? value["Direct ".Length..]
            : value;
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

    private static VideoCaptureFormatSummary? SelectBestVideoFormat(VideoCaptureDeviceSummary? device)
    {
        return device?.Formats
            .OrderBy(format => Math.Abs((format.Width * format.Height) - (640 * 480)))
            .ThenByDescending(format => format.FpsDenominator <= 0 ? 0 : (double)format.FpsNumerator / format.FpsDenominator)
            .FirstOrDefault();
    }

    private void RaiseCommandStateChanged()
    {
        if (ConnectCommand is AsyncRelayCommand connect)
        {
            connect.RaiseCanExecuteChanged();
        }

        if (DisconnectCommand is AsyncRelayCommand disconnect)
        {
            disconnect.RaiseCanExecuteChanged();
        }

        if (OpenConnectionTargetCommand is AsyncRelayCommand openTarget)
        {
            openTarget.RaiseCanExecuteChanged();
        }

        if (ServerInformationCommand is AsyncRelayCommand serverInformation)
        {
            serverInformation.RaiseCanExecuteChanged();
        }

        if (ServerStatisticsCommand is AsyncRelayCommand serverStatistics)
        {
            serverStatistics.RaiseCanExecuteChanged();
        }

        if (BannedUsersCommand is AsyncRelayCommand bannedUsers)
        {
            bannedUsers.RaiseCanExecuteChanged();
        }

        if (UserAccountsCommand is AsyncRelayCommand userAccounts)
        {
            userAccounts.RaiseCanExecuteChanged();
        }

        if (SaveServerConfigurationCommand is AsyncRelayCommand saveServerConfiguration)
        {
            saveServerConfiguration.RaiseCanExecuteChanged();
        }

        RaiseFileCommandStateChanged();

        if (TogglePushToTalkCommand is AsyncRelayCommand pushToTalk)
        {
            pushToTalk.RaiseCanExecuteChanged();
        }

        if (ToggleVoiceActivationCommand is AsyncRelayCommand voiceActivation)
        {
            voiceActivation.RaiseCanExecuteChanged();
        }

        RaiseMediaCommandStateChanged();

        if (SetStatusCommand is AsyncRelayCommand status)
        {
            status.RaiseCanExecuteChanged();
        }

        if (ChangeNicknameCommand is AsyncRelayCommand nickname)
        {
            nickname.RaiseCanExecuteChanged();
        }

        RaiseChannelCommandStateChanged();
    }

    private void RaiseFileCommandStateChanged()
    {
        if (UploadFileCommand is AsyncRelayCommand uploadFile)
        {
            uploadFile.RaiseCanExecuteChanged();
        }

        if (DownloadFileCommand is AsyncRelayCommand downloadFile)
        {
            downloadFile.RaiseCanExecuteChanged();
        }

        if (DeleteFileCommand is AsyncRelayCommand deleteFile)
        {
            deleteFile.RaiseCanExecuteChanged();
        }

        if (CancelTransferCommand is AsyncRelayCommand cancelTransfer)
        {
            cancelTransfer.RaiseCanExecuteChanged();
        }

        if (RefreshFilesCommand is AsyncRelayCommand refreshFiles)
        {
            refreshFiles.RaiseCanExecuteChanged();
        }
    }

    private void RaiseMediaCommandStateChanged()
    {
        if (ToggleVideoCaptureCommand is AsyncRelayCommand videoCapture)
        {
            videoCapture.RaiseCanExecuteChanged();
        }

        if (ShareDesktopCommand is AsyncRelayCommand shareDesktop)
        {
            shareDesktop.RaiseCanExecuteChanged();
        }

        if (ShareActiveWindowCommand is AsyncRelayCommand shareActiveWindow)
        {
            shareActiveWindow.RaiseCanExecuteChanged();
        }

        if (StopDesktopShareCommand is AsyncRelayCommand stopDesktopShare)
        {
            stopDesktopShare.RaiseCanExecuteChanged();
        }
    }

    private void RaiseChannelCommandStateChanged()
    {
        if (JoinSelectedChannelCommand is AsyncRelayCommand join)
        {
            join.RaiseCanExecuteChanged();
        }

        if (ChannelInformationCommand is RelayCommand information)
        {
            information.RaiseCanExecuteChanged();
        }

        if (EditChannelTopicCommand is AsyncRelayCommand topic)
        {
            topic.RaiseCanExecuteChanged();
        }

        if (CreateChannelCommand is AsyncRelayCommand create)
        {
            create.RaiseCanExecuteChanged();
        }

        if (DeleteSelectedChannelCommand is AsyncRelayCommand delete)
        {
            delete.RaiseCanExecuteChanged();
        }

        if (UserInformationCommand is RelayCommand userInformation)
        {
            userInformation.RaiseCanExecuteChanged();
        }

        if (SendDirectMessageCommand is AsyncRelayCommand directMessage)
        {
            directMessage.RaiseCanExecuteChanged();
        }

        if (UserAudioSettingsCommand is AsyncRelayCommand userAudioSettings)
        {
            userAudioSettings.RaiseCanExecuteChanged();
        }

        if (ToggleSelectedUserMuteCommand is AsyncRelayCommand toggleSelectedUserMute)
        {
            toggleSelectedUserMute.RaiseCanExecuteChanged();
        }

        if (MoveUserCommand is AsyncRelayCommand moveUser)
        {
            moveUser.RaiseCanExecuteChanged();
        }

        if (KickUserFromChannelCommand is AsyncRelayCommand kickChannel)
        {
            kickChannel.RaiseCanExecuteChanged();
        }

        if (KickUserFromServerCommand is AsyncRelayCommand kickServer)
        {
            kickServer.RaiseCanExecuteChanged();
        }

        if (BanUserFromServerCommand is AsyncRelayCommand banServer)
        {
            banServer.RaiseCanExecuteChanged();
        }
    }

    private bool CanJoinSelectedChannel()
    {
        return SelectedChannelItem is { Kind: ChannelTreeItemKind.Channel }
            && (teamTalkSession.Status is ConnectionStatus.LoggedIn or ConnectionStatus.InChannel);
    }

    private bool CanShowServerInformation()
    {
        return CanUseLoggedInServerCommand();
    }

    private bool CanUseLoggedInServerCommand()
    {
        return teamTalkSession.Status is ConnectionStatus.LoggedIn or ConnectionStatus.InChannel;
    }

    private bool CanRefreshFiles()
    {
        return teamTalkSession.Status == ConnectionStatus.InChannel;
    }

    private bool CanManageFiles()
    {
        return teamTalkSession.Status == ConnectionStatus.InChannel;
    }

    private bool CanUseSelectedFile()
    {
        return CanManageFiles() && SelectedFile is { IsPlaceholder: false };
    }

    private bool CanCancelSelectedTransfer()
    {
        return teamTalkSession.Status != ConnectionStatus.Disconnected
            && SelectedTransfer is { IsActive: true };
    }

    private bool CanCreateChannel()
    {
        return (SelectedChannelItem is null or { Kind: ChannelTreeItemKind.Server or ChannelTreeItemKind.Channel })
            && (teamTalkSession.Status is ConnectionStatus.LoggedIn or ConnectionStatus.InChannel);
    }

    private bool CanShowChannelInformation()
    {
        return SelectedChannelItem is { Kind: ChannelTreeItemKind.Channel };
    }

    private bool CanEditChannelTopic()
    {
        return SelectedChannelItem is { Kind: ChannelTreeItemKind.Channel }
            && (teamTalkSession.Status is ConnectionStatus.LoggedIn or ConnectionStatus.InChannel);
    }

    private bool CanDeleteSelectedChannel()
    {
        return SelectedChannelItem is { Kind: ChannelTreeItemKind.Channel, Path: not "/" }
            && (teamTalkSession.Status is ConnectionStatus.LoggedIn or ConnectionStatus.InChannel);
    }

    private bool CanUseVoiceControls()
    {
        return teamTalkSession.Status == ConnectionStatus.InChannel;
    }

    private bool CanToggleVideoCapture()
    {
        return IsVideoCaptureActive || teamTalkSession.Status == ConnectionStatus.InChannel;
    }

    private bool CanStartDesktopShare()
    {
        return teamTalkSession.Status == ConnectionStatus.InChannel;
    }

    private bool CanStopDesktopShare()
    {
        return IsDesktopSharingActive;
    }

    private bool CanSetStatus()
    {
        return CanSetProfileState();
    }

    private bool CanSetProfileState()
    {
        return teamTalkSession.Status is ConnectionStatus.LoggedIn or ConnectionStatus.InChannel;
    }

    private bool CanSendDirectMessage()
    {
        return SelectedChannelItem is { Kind: ChannelTreeItemKind.User }
            && (teamTalkSession.Status is ConnectionStatus.LoggedIn or ConnectionStatus.InChannel);
    }

    private bool CanShowUserInformation()
    {
        return SelectedChannelItem is { Kind: ChannelTreeItemKind.User };
    }

    private bool CanChangeUserAudioSettings()
    {
        return SelectedChannelItem is { Kind: ChannelTreeItemKind.User }
            && (teamTalkSession.Status is ConnectionStatus.LoggedIn or ConnectionStatus.InChannel);
    }

    private bool CanMoveSelectedUser()
    {
        return CanModerateSelectedUser();
    }

    private bool CanModerateSelectedUser()
    {
        return SelectedChannelItem is { Kind: ChannelTreeItemKind.User }
            && (teamTalkSession.Status is ConnectionStatus.LoggedIn or ConnectionStatus.InChannel);
    }
}
