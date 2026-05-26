using System.Windows;
using TeamTalkNg.Accessibility;
using TeamTalkNg.App.Services;
using TeamTalkNg.App.ViewModels;
using TeamTalkNg.Core.Accessibility;
using TeamTalkNg.Core.TeamTalk;

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
        IServerProfileStore profileStore = new JsonServerProfileStore();
        IConnectionDialogService connectionDialogService = new ConnectionDialogService();

        var viewModel = new MainWindowViewModel(
            teamTalkSession,
            announcementService,
            themeService,
            profileStore,
            connectionDialogService);
        var window = new MainWindow
        {
            DataContext = viewModel
        };

        window.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (announcementService is not null)
        {
            await announcementService.DisposeAsync();
        }

        base.OnExit(e);
    }
}
