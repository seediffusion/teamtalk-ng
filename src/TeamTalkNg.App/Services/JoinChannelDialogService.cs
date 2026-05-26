using System.Windows;
using TeamTalkNg.App.ViewModels;

namespace TeamTalkNg.App.Services;

public sealed class JoinChannelDialogService : IJoinChannelDialogService
{
    public string? ShowJoinChannelDialog(string channelName)
    {
        var viewModel = new JoinChannelDialogViewModel(channelName);
        var dialog = new JoinChannelDialog
        {
            Owner = Application.Current.MainWindow,
            DataContext = viewModel
        };

        viewModel.RequestClose += (_, accepted) =>
        {
            dialog.DialogResult = accepted;
            dialog.Close();
        };

        return dialog.ShowDialog() == true ? viewModel.Password : null;
    }
}
