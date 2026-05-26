using System.Windows;
using TeamTalkNg.App.ViewModels;

namespace TeamTalkNg.App.Services;

public sealed class DirectMessageDialogService : IDirectMessageDialogService
{
    public string? ShowDirectMessageDialog(string recipientName)
    {
        var viewModel = new DirectMessageDialogViewModel(recipientName);
        var dialog = new DirectMessageDialog
        {
            Owner = Application.Current.MainWindow,
            DataContext = viewModel
        };

        viewModel.RequestClose += (_, accepted) =>
        {
            dialog.DialogResult = accepted;
            dialog.Close();
        };

        return dialog.ShowDialog() == true ? viewModel.Message.Trim() : null;
    }
}
