using System.Windows;
using TeamTalkNg.App.ViewModels;

namespace TeamTalkNg.App.Services;

public sealed class UserInformationDialogService : IUserInformationDialogService
{
    public void ShowUserInformationDialog(ChannelTreeItemViewModel user)
    {
        var viewModel = new UserInformationDialogViewModel(user);
        var dialog = new UserInformationDialog
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
