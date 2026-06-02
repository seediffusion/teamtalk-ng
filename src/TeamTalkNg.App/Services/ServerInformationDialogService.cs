using System.Windows;
using TeamTalkNg.App.ViewModels;
using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.Services;

public sealed class ServerInformationDialogService : IServerInformationDialogService
{
    public void ShowServerInformationDialog(ServerInformationSummary serverInformation)
    {
        var viewModel = new ServerInformationDialogViewModel(serverInformation);
        var dialog = new ServerInformationDialog
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
