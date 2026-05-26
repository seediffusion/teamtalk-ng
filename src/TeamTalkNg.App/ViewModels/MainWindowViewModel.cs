using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using TeamTalkNg.App.Services;
using TeamTalkNg.Core.Accessibility;
using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private readonly ITeamTalkSession teamTalkSession;
    private readonly IAnnouncementService announcements;
    private readonly IThemeService themeService;
    private readonly IServerProfileStore profileStore;
    private readonly IConnectionDialogService connectionDialogService;
    private readonly IAppSettingsStore settingsStore;
    private readonly IPreferencesDialogService preferencesDialogService;
    private string connectionStatusText = "Disconnected";
    private string liveAnnouncement = "Ready";
    private string messageText = string.Empty;
    private double inputVolume = 75;
    private double outputVolume = 80;
    private ChannelTreeItemViewModel? selectedChannelItem;
    private bool pushToTalkEnabled;
    private bool voiceActivationEnabled;
    private TeamTalkServerProfile? activeProfile;
    private AppSettings settings;

    public MainWindowViewModel(
        ITeamTalkSession teamTalkSession,
        IAnnouncementService announcements,
        IThemeService themeService,
        IServerProfileStore profileStore,
        IConnectionDialogService connectionDialogService,
        IAppSettingsStore settingsStore,
        IPreferencesDialogService preferencesDialogService,
        AppSettings settings)
    {
        this.teamTalkSession = teamTalkSession;
        this.announcements = announcements;
        this.themeService = themeService;
        this.profileStore = profileStore;
        this.connectionDialogService = connectionDialogService;
        this.settingsStore = settingsStore;
        this.preferencesDialogService = preferencesDialogService;
        this.settings = settings;

        ConnectCommand = new AsyncRelayCommand(ConnectAsync, () => teamTalkSession.Status == ConnectionStatus.Disconnected);
        DisconnectCommand = new AsyncRelayCommand(DisconnectAsync, () => teamTalkSession.Status != ConnectionStatus.Disconnected);
        SendMessageCommand = new AsyncRelayCommand(SendMessageAsync, () => !string.IsNullOrWhiteSpace(MessageText));
        TogglePushToTalkCommand = new RelayCommand(TogglePushToTalk);
        ToggleVoiceActivationCommand = new RelayCommand(ToggleVoiceActivation);
        SimulateUserJoinedCommand = new RelayCommand(SimulateUserJoined);
        UseLightThemeCommand = new AsyncRelayCommand(() => SetThemeAsync(AppTheme.Light));
        UseDarkThemeCommand = new AsyncRelayCommand(() => SetThemeAsync(AppTheme.Dark));
        PreferencesCommand = new AsyncRelayCommand(ShowPreferencesAsync);
        AboutCommand = new RelayCommand(ShowAbout);
        ExitCommand = new RelayCommand(() => Application.Current.Shutdown());

        announcements.AnnouncementRaised += OnAnnouncementRaised;
        teamTalkSession.ConnectionStatusChanged += OnConnectionStatusChanged;
        teamTalkSession.ChannelMessageReceived += OnChannelMessageReceived;
        teamTalkSession.UserJoined += OnUserJoined;
        teamTalkSession.UserLeft += OnUserLeft;

        BuildDisconnectedTree();
        Files.Add(new FileTransferViewModel("No files in current channel", string.Empty, string.Empty));
    }

    public string WindowTitle => $"TeamTalk NG - {ConnectionStatusText}";

    public ObservableCollection<ChannelTreeItemViewModel> Channels { get; } = [];

    public ObservableCollection<ChatMessageViewModel> ChatMessages { get; } = [];

    public ObservableCollection<FileTransferViewModel> Files { get; } = [];

    public ICommand ConnectCommand { get; }

    public ICommand DisconnectCommand { get; }

    public ICommand SendMessageCommand { get; }

    public ICommand TogglePushToTalkCommand { get; }

    public ICommand ToggleVoiceActivationCommand { get; }

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
        set
        {
            if (SetProperty(ref messageText, value) && SendMessageCommand is AsyncRelayCommand command)
            {
                command.RaiseCanExecuteChanged();
            }
        }
    }

    public double InputVolume
    {
        get => inputVolume;
        set => SetProperty(ref inputVolume, value);
    }

    public double OutputVolume
    {
        get => outputVolume;
        set => SetProperty(ref outputVolume, value);
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
        }
    }

    private async Task ConnectAsync()
    {
        IReadOnlyList<TeamTalkServerProfile> profiles = await profileStore.LoadAsync();
        TeamTalkServerProfile? profile = connectionDialogService.ShowConnectDialog(profiles);
        if (profile is null)
        {
            await AnnounceAsync("Connect canceled", AnnouncementPriority.Low, AnnouncementKind.System, includeBraille: false);
            return;
        }

        activeProfile = profile;
        await SaveRecentProfileAsync(profiles, profile);
        await AnnounceAsync($"Connecting to {profile.DisplayName}", AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        await ConnectToProfileAsync(profile);
    }

    public async Task ConnectToProfileAsync(TeamTalkServerProfile profile)
    {
        activeProfile = profile;
        await teamTalkSession.ConnectAsync(profile);
        BuildConnectedTree();
        RaiseCommandStateChanged();
    }

    private async Task DisconnectAsync()
    {
        await teamTalkSession.DisconnectAsync();
        activeProfile = null;
        BuildDisconnectedTree();
        ChatMessages.Clear();
        await AnnounceAsync("Disconnected", AnnouncementPriority.High, AnnouncementKind.System, interrupt: true);
        RaiseCommandStateChanged();
    }

    private async Task SendMessageAsync()
    {
        string text = MessageText.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        MessageText = string.Empty;
        await teamTalkSession.SendChannelMessageAsync(text);
        await AnnounceAsync($"Sent message: {text}", AnnouncementPriority.Low, AnnouncementKind.System, includeBraille: false);
    }

    private void TogglePushToTalk()
    {
        pushToTalkEnabled = !pushToTalkEnabled;
        string state = pushToTalkEnabled ? "enabled" : "disabled";
        _ = AnnounceAsync($"Push to talk {state}", AnnouncementPriority.Normal, AnnouncementKind.System);
    }

    private void ToggleVoiceActivation()
    {
        voiceActivationEnabled = !voiceActivationEnabled;
        string state = voiceActivationEnabled ? "enabled" : "disabled";
        _ = AnnounceAsync($"Voice activation {state}", AnnouncementPriority.Normal, AnnouncementKind.System);
    }

    private async Task ShowPreferencesAsync()
    {
        AppSettings? updatedSettings = preferencesDialogService.ShowPreferencesDialog(settings);
        if (updatedSettings is null)
        {
            return;
        }

        settings = updatedSettings;
        themeService.UseTheme(settings.Theme);
        await settingsStore.SaveAsync(settings);
        await AnnounceAsync("Preferences saved", AnnouncementPriority.Normal, AnnouncementKind.System);
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

    private void OnConnectionStatusChanged(object? sender, ConnectionStatus status)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            ConnectionStatusText = status switch
            {
                ConnectionStatus.Disconnected => "Disconnected",
                ConnectionStatus.Connecting => "Connecting",
                ConnectionStatus.Connected => "Connected",
                ConnectionStatus.LoggedIn => "Logged in",
                ConnectionStatus.InChannel => activeProfile?.ChannelPath is { Length: > 0 } channel ? $"In {channel}" : "In channel",
                _ => status.ToString()
            };

            RaiseCommandStateChanged();
        });
    }

    private void OnChannelMessageReceived(object? sender, ChatMessage message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            ChatMessages.Add(new ChatMessageViewModel(message));
        });

        string announcement = message.IsSystem
            ? message.Text
            : $"{message.Sender}: {message.Text}";
        _ = AnnounceAsync(
            announcement,
            message.IsPrivate ? AnnouncementPriority.High : AnnouncementPriority.Normal,
            message.IsPrivate ? AnnouncementKind.PrivateMessage : AnnouncementKind.ChannelMessage);
    }

    private void OnUserJoined(object? sender, UserSummary user)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            ChannelTreeItemViewModel? channel = Channels.FirstOrDefault()?.Children.FirstOrDefault(item => item.Name == GetChannelName(user.ChannelPath));
            channel?.Children.Add(new ChannelTreeItemViewModel(user.Nickname, ChannelTreeItemKind.User));
        });

        _ = AnnounceAsync($"{user.Nickname} joined {GetChannelName(user.ChannelPath)}", AnnouncementPriority.Normal, AnnouncementKind.UserJoinLeave);
    }

    private void OnUserLeft(object? sender, UserSummary user)
    {
        _ = AnnounceAsync($"{user.Nickname} left {user.ChannelPath}", AnnouncementPriority.Normal, AnnouncementKind.UserJoinLeave);
    }

    private void OnAnnouncementRaised(object? sender, ScreenReaderAnnouncement announcement)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            LiveAnnouncement = announcement.Text;
        });
    }

    private void BuildDisconnectedTree()
    {
        Channels.Clear();
        Channels.Add(new ChannelTreeItemViewModel("Not connected", ChannelTreeItemKind.Server));
    }

    private void BuildConnectedTree()
    {
        Channels.Clear();

        string serverName = activeProfile?.DisplayName ?? "Connected server";
        string channelName = GetChannelName(activeProfile?.ChannelPath);
        string nickname = string.IsNullOrWhiteSpace(activeProfile?.Nickname) ? "You" : activeProfile.Nickname;

        var server = new ChannelTreeItemViewModel(serverName, ChannelTreeItemKind.Server);
        var lobby = new ChannelTreeItemViewModel(channelName, ChannelTreeItemKind.Channel);
        lobby.Children.Add(new ChannelTreeItemViewModel(nickname, ChannelTreeItemKind.User));
        lobby.Children.Add(new ChannelTreeItemViewModel("Riley", ChannelTreeItemKind.User) { IsTalking = true });

        var music = new ChannelTreeItemViewModel("Music Room", ChannelTreeItemKind.Channel);
        music.Children.Add(new ChannelTreeItemViewModel("Sam", ChannelTreeItemKind.User));

        server.Children.Add(lobby);
        server.Children.Add(music);
        Channels.Add(server);
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
        bool? includeBraille = null)
    {
        if (!ShouldAnnounce(kind))
        {
            return Task.CompletedTask;
        }

        return announcements.AnnounceAsync(new ScreenReaderAnnouncement(
            text,
            priority,
            interrupt,
            includeBraille ?? settings.SendAnnouncementsToBraille)).AsTask();
    }

    private bool ShouldAnnounce(AnnouncementKind kind)
    {
        return kind switch
        {
            AnnouncementKind.ChannelMessage => settings.AnnounceChannelMessages,
            AnnouncementKind.PrivateMessage => settings.AnnouncePrivateMessages,
            AnnouncementKind.UserJoinLeave => settings.AnnounceUserJoinLeave,
            AnnouncementKind.Selection => settings.AnnounceSelectionChanges,
            _ => true
        };
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
    }
}
