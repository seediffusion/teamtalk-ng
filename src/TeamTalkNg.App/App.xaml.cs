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

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        IScreenReaderOutput screenReaderOutput = PrismatoidScreenReaderOutput.TryCreate(out IScreenReaderOutput prismatoidOutput)
            ? new CompositeScreenReaderOutput(prismatoidOutput, new DebugScreenReaderOutput())
            : new DebugScreenReaderOutput();

        announcementService = new QueuedAnnouncementService(screenReaderOutput);
        ITeamTalkSession teamTalkSession = new MockTeamTalkSession();
        IThemeService themeService = new ThemeService();
        IAppSettingsStore settingsStore = new JsonAppSettingsStore();
        AppSettings appSettings = settingsStore.LoadAsync().GetAwaiter().GetResult();
        themeService.UseTheme(appSettings.Theme);

        IServerProfileStore profileStore = new JsonServerProfileStore();
        IConnectionDialogService connectionDialogService = new ConnectionDialogService();
        IPreferencesDialogService preferencesDialogService = new PreferencesDialogService();

        var viewModel = new MainWindowViewModel(
            teamTalkSession,
            announcementService,
            themeService,
            profileStore,
            connectionDialogService,
            settingsStore,
            preferencesDialogService,
            appSettings);
        var window = new MainWindow
        {
            DataContext = viewModel
        };

        window.Show();

        if (TryReadStartupConnectionTarget(e.Args, out TeamTalkServerProfile startupProfile, out string startupError))
        {
            _ = viewModel.ConnectToProfileAsync(startupProfile);
        }
        else if (!string.IsNullOrEmpty(startupError))
        {
            _ = announcementService.AnnounceAsync(new ScreenReaderAnnouncement(startupError, AnnouncementPriority.High, Interrupt: true));
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
