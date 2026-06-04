using System.Windows;
using TeamTalkNg.App.ViewModels;
using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.Services;

public sealed class BannedUsersDialogService : IBannedUsersDialogService
{
    public BannedUserSummary? ShowBannedUsersDialog(IReadOnlyList<BannedUserSummary> bannedUsers)
    {
        var viewModel = new BannedUsersDialogViewModel(bannedUsers);
        var dialog = new BannedUsersDialog
        {
            Owner = Application.Current.MainWindow,
            DataContext = viewModel
        };

        viewModel.RequestClose += (_, accepted) =>
        {
            dialog.DialogResult = accepted;
            dialog.Close();
        };

        return dialog.ShowDialog() == true ? viewModel.SelectedBan?.BannedUser : null;
    }
}
