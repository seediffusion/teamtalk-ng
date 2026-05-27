using System.Windows;
using TeamTalkNg.App.ViewModels;

namespace TeamTalkNg.App.Services;

public sealed class ChannelTopicDialogService : IChannelTopicDialogService
{
    public string? ShowChannelTopicDialog(string channelName, string currentTopic)
    {
        var viewModel = new ChannelTopicDialogViewModel(channelName, currentTopic);
        var dialog = new ChannelTopicDialog
        {
            Owner = Application.Current.MainWindow,
            DataContext = viewModel
        };

        viewModel.RequestClose += (_, accepted) =>
        {
            dialog.DialogResult = accepted;
            dialog.Close();
        };

        return dialog.ShowDialog() == true ? viewModel.Topic.Trim() : null;
    }
}
