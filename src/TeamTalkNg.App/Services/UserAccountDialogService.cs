using System.Windows;
using TeamTalkNg.App.ViewModels;
using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.Services;

public sealed class UserAccountDialogService : IUserAccountDialogService
{
    public UserAccountCreationRequest? ShowCreateUserAccountDialog()
    {
        var viewModel = new UserAccountDialogViewModel();
        var dialog = new UserAccountDialog
        {
            Owner = Application.Current.MainWindow,
            DataContext = viewModel
        };

        viewModel.RequestClose += (_, accepted) =>
        {
            dialog.DialogResult = accepted;
            dialog.Close();
        };

        return dialog.ShowDialog() == true ? viewModel.CreateRequest : null;
    }
}
