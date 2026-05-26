using System.Windows;
using TeamTalkNg.App.ViewModels;

namespace TeamTalkNg.App.Services;

public sealed class ConnectionTargetDialogService : IConnectionTargetDialogService
{
    public string? ShowConnectionTargetDialog()
    {
        var viewModel = new ConnectionTargetDialogViewModel();
        var dialog = new ConnectionTargetDialog
        {
            Owner = Application.Current.MainWindow,
            DataContext = viewModel
        };

        viewModel.RequestClose += (_, accepted) =>
        {
            dialog.DialogResult = accepted;
            dialog.Close();
        };

        return dialog.ShowDialog() == true ? viewModel.Target : null;
    }
}
