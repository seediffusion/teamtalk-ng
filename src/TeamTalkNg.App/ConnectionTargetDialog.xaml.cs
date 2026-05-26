using System.Windows;
using Microsoft.Win32;
using TeamTalkNg.App.ViewModels;

namespace TeamTalkNg.App;

public partial class ConnectionTargetDialog : Window
{
    public ConnectionTargetDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => TargetBox.Focus();
    }

    private void BrowseButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open TeamTalk Connection File",
            Filter = "TeamTalk connection files (*.tt)|*.tt|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == true && DataContext is ConnectionTargetDialogViewModel viewModel)
        {
            viewModel.Target = dialog.FileName;
        }
    }
}
