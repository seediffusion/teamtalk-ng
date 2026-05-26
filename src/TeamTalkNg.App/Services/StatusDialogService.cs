using System.Windows;
using TeamTalkNg.App.ViewModels;
using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.Services;

public sealed class StatusDialogService : IStatusDialogService
{
    public UserStatusRequest? ShowStatusDialog(bool isAway, string statusMessage)
    {
        var viewModel = new StatusDialogViewModel(isAway, statusMessage);
        var dialog = new StatusDialog
        {
            Owner = Application.Current.MainWindow,
            DataContext = viewModel
        };

        viewModel.RequestClose += (_, accepted) =>
        {
            dialog.DialogResult = accepted;
            dialog.Close();
        };

        return dialog.ShowDialog() == true ? viewModel.ToRequest() : null;
    }
}
