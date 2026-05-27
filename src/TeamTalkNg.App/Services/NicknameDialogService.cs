using System.Windows;
using TeamTalkNg.App.ViewModels;

namespace TeamTalkNg.App.Services;

public sealed class NicknameDialogService : INicknameDialogService
{
    public string? ShowNicknameDialog(string currentNickname)
    {
        var viewModel = new NicknameDialogViewModel(currentNickname);
        var dialog = new NicknameDialog
        {
            Owner = Application.Current.MainWindow,
            DataContext = viewModel
        };

        viewModel.RequestClose += (_, accepted) =>
        {
            dialog.DialogResult = accepted;
            dialog.Close();
        };

        return dialog.ShowDialog() == true ? viewModel.Nickname.Trim() : null;
    }
}
