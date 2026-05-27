using System.Windows;
using TeamTalkNg.App.ViewModels;

namespace TeamTalkNg.App.Services;

public sealed class ChannelInformationDialogService : IChannelInformationDialogService
{
    public void ShowChannelInformationDialog(ChannelTreeItemViewModel channel)
    {
        var viewModel = new ChannelInformationDialogViewModel(channel);
        var dialog = new ChannelInformationDialog
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
