using System.Windows;
using TeamTalkNg.App.ViewModels;

namespace TeamTalkNg.App.Services;

public sealed class MoveUserDialogService : IMoveUserDialogService
{
    public string? ShowMoveUserDialog(string userName, IEnumerable<MoveUserDestinationViewModel> destinations)
    {
        var viewModel = new MoveUserDialogViewModel(userName, destinations);
        var dialog = new MoveUserDialog
        {
            Owner = Application.Current.MainWindow,
            DataContext = viewModel
        };

        viewModel.RequestClose += (_, accepted) =>
        {
            dialog.DialogResult = accepted;
            dialog.Close();
        };

        return dialog.ShowDialog() == true ? viewModel.SelectedDestination?.Path : null;
    }
}
