using System.Windows;
using TeamTalkNg.Accessibility;
using TeamTalkNg.App.Services;
using TeamTalkNg.App.ViewModels;
using TeamTalkNg.Core.Accessibility;
using TeamTalkNg.Core.TeamTalk;
using TeamTalkNg.Core.TeamTalk.ConnectionTargets;

namespace TeamTalkNg.App;

public partial class App : Application
{
    private IAnnouncementService? announcementService;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            IScreenReaderOutput screenReaderOutput = PrismatoidScreenReaderOutput.TryCreate(out IScreenReaderOutput prismatoidOutput)
                ? new CompositeScreenReaderOutput(prismatoidOutput, new DebugScreenReaderOutput())
                : new DebugScreenReaderOutput();

            announcementService = new QueuedAnnouncementService(screenReaderOutput);
            ITeamTalkSession teamTalkSession = TeamTalkSessionFactory.CreateDefaultSession();
            IThemeService themeService = new ThemeService();
            IAppSettingsStore settingsStore = new JsonAppSettingsStore();
            AppSettings appSettings = await settingsStore.LoadAsync();
            themeService.UseTheme(appSettings.Theme);

            IServerProfileStore profileStore = new JsonServerProfileStore();
            IConnectionDialogService connectionDialogService = new ConnectionDialogService();
            IConnectionTargetDialogService connectionTargetDialogService = new ConnectionTargetDialogService();
            IServerInformationDialogService serverInformationDialogService = new ServerInformationDialogService();
            IServerStatisticsDialogService serverStatisticsDialogService = new ServerStatisticsDialogService();
            IBannedUsersDialogService bannedUsersDialogService = new BannedUsersDialogService();
            IUserAccountsDialogService userAccountsDialogService = new UserAccountsDialogService();
            IUserAccountDialogService userAccountDialogService = new UserAccountDialogService();
            IPreferencesDialogService preferencesDialogService = new PreferencesDialogService();
            IChannelDialogService channelDialogService = new ChannelDialogService();
            IChannelInformationDialogService channelInformationDialogService = new ChannelInformationDialogService();
            IChannelTopicDialogService channelTopicDialogService = new ChannelTopicDialogService();
            IDirectMessageDialogService directMessageDialogService = new DirectMessageDialogService();
            IMoveUserDialogService moveUserDialogService = new MoveUserDialogService();
            IUserInformationDialogService userInformationDialogService = new UserInformationDialogService();
            IUserAudioSettingsDialogService userAudioSettingsDialogService = new UserAudioSettingsDialogService();
            IStatusDialogService statusDialogService = new StatusDialogService();
            IJoinChannelDialogService joinChannelDialogService = new JoinChannelDialogService();
            INicknameDialogService nicknameDialogService = new NicknameDialogService();
            IFileDialogService fileDialogService = new FileDialogService();

            var viewModel = new MainWindowViewModel(
                teamTalkSession,
                announcementService,
                themeService,
                profileStore,
                connectionDialogService,
                connectionTargetDialogService,
                serverInformationDialogService,
                serverStatisticsDialogService,
                bannedUsersDialogService,
                userAccountsDialogService,
                userAccountDialogService,
                settingsStore,
                preferencesDialogService,
                channelDialogService,
                channelInformationDialogService,
                channelTopicDialogService,
                directMessageDialogService,
                moveUserDialogService,
                userInformationDialogService,
                userAudioSettingsDialogService,
                statusDialogService,
                joinChannelDialogService,
                nicknameDialogService,
                fileDialogService,
                appSettings);
            var window = new MainWindow
            {
                DataContext = viewModel
            };

            MainWindow = window;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            window.Show();
            window.Activate();

            if (TryReadStartupConnectionTarget(e.Args, out TeamTalkServerProfile startupProfile, out string startupError))
            {
                _ = viewModel.ConnectToProfileAsync(startupProfile);
            }
            else if (!string.IsNullOrEmpty(startupError))
            {
                _ = announcementService.AnnounceAsync(new ScreenReaderAnnouncement(startupError, AnnouncementPriority.High, Interrupt: true));
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.ToString(),
                "TeamTalk NG failed to start",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (announcementService is not null)
        {
            await announcementService.DisposeAsync();
        }

        base.OnExit(e);
    }

    private static bool TryReadStartupConnectionTarget(string[] args, out TeamTalkServerProfile profile, out string error)
    {
        profile = new TeamTalkServerProfile();
        error = string.Empty;

        string? target = args.FirstOrDefault(argument =>
            argument.StartsWith("tt://", StringComparison.OrdinalIgnoreCase)
            || argument.EndsWith(".tt", StringComparison.OrdinalIgnoreCase));

        return target is not null && TeamTalkConnectionTargetParser.TryParse(target, out profile, out error);
    }
}
