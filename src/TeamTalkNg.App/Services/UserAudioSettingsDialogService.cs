using System.Windows;
using TeamTalkNg.App.ViewModels;
using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.Services;

public sealed class UserAudioSettingsDialogService : IUserAudioSettingsDialogService
{
    public UserAudioSettingsRequest? ShowUserAudioSettingsDialog(ChannelTreeItemViewModel user)
    {
        var viewModel = new UserAudioSettingsDialogViewModel(user);
        var dialog = new UserAudioSettingsDialog
        {
            Owner = Application.Current.MainWindow,
            DataContext = viewModel
        };

        viewModel.RequestClose += (_, accepted) =>
        {
            dialog.DialogResult = accepted;
            dialog.Close();
        };

        return dialog.ShowDialog() == true ? viewModel.CreateRequest() : null;
    }
}
