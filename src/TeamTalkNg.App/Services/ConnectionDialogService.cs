using System.Windows;
using TeamTalkNg.App.ViewModels;
using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.Services;

public sealed class ConnectionDialogService : IConnectionDialogService
{
    public TeamTalkServerProfile? ShowConnectDialog(IReadOnlyList<TeamTalkServerProfile> profiles)
    {
        var viewModel = new ConnectDialogViewModel(profiles);
        var dialog = new ConnectDialog
        {
            Owner = Application.Current.MainWindow,
            DataContext = viewModel
        };

        viewModel.RequestClose += (_, accepted) =>
        {
            dialog.DialogResult = accepted;
            dialog.Close();
        };

        return dialog.ShowDialog() == true ? viewModel.ToProfile() : null;
    }
}
