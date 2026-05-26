using System.Windows;
using TeamTalkNg.App.ViewModels;
using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.Services;

public sealed class ChannelDialogService : IChannelDialogService
{
    public ChannelCreationRequest? ShowCreateChannelDialog(string parentPath)
    {
        var viewModel = new ChannelDialogViewModel(parentPath);
        var dialog = new ChannelDialog
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
