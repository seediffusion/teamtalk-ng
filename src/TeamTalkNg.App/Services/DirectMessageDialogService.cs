using System.Windows;
using TeamTalkNg.App.ViewModels;

namespace TeamTalkNg.App.Services;

public sealed class DirectMessageDialogService : IDirectMessageDialogService
{
    public string? ShowDirectMessageDialog(string recipientName, IReadOnlyList<ChatMessageViewModel> conversation)
    {
        var viewModel = new DirectMessageDialogViewModel(recipientName, conversation);
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
