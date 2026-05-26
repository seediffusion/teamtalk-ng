using System.Windows;
using TeamTalkNg.App.ViewModels;

namespace TeamTalkNg.App.Services;

public sealed class PreferencesDialogService : IPreferencesDialogService
{
    public AppSettings? ShowPreferencesDialog(AppSettings currentSettings)
    {
        var viewModel = new PreferencesDialogViewModel(currentSettings);
        var dialog = new PreferencesDialog
        {
            Owner = Application.Current.MainWindow,
            DataContext = viewModel
        };

        viewModel.RequestClose += (_, accepted) =>
        {
            dialog.DialogResult = accepted;
            dialog.Close();
        };

        return dialog.ShowDialog() == true ? viewModel.ToSettings() : null;
    }
}
