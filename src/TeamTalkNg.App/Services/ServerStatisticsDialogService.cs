using System.Windows;
using TeamTalkNg.App.ViewModels;
using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.Services;

public sealed class ServerStatisticsDialogService : IServerStatisticsDialogService
{
    public void ShowServerStatisticsDialog(ServerStatisticsSummary serverStatistics)
    {
        var viewModel = new ServerStatisticsDialogViewModel(serverStatistics);
        var dialog = new ServerStatisticsDialog
        {
            Owner = Application.Current.MainWindow,
            DataContext = viewModel
        };

        viewModel.RequestClose += (_, _) =>
        {
            dialog.DialogResult = true;
            dialog.Close();
        };

        dialog.ShowDialog();
    }
}
