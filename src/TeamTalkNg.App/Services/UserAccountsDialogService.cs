using System.Windows;
using TeamTalkNg.App.ViewModels;
using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.Services;

public sealed class UserAccountsDialogService : IUserAccountsDialogService
{
    public UserAccountsDialogResult ShowUserAccountsDialog(IReadOnlyList<UserAccountSummary> accounts)
    {
        var viewModel = new UserAccountsDialogViewModel(accounts);
        var dialog = new UserAccountsDialog
        {
            Owner = Application.Current.MainWindow,
            DataContext = viewModel
        };

        viewModel.RequestClose += (_, action) =>
        {
            dialog.DialogResult = action != UserAccountsDialogAction.Close;
            dialog.Close();
        };

        dialog.ShowDialog();
        return new UserAccountsDialogResult(viewModel.SelectedAction, viewModel.SelectedAccount?.Account);
    }
}
