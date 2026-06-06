using System.Windows;
using TeamTalkNg.App.ViewModels;

namespace TeamTalkNg.App;

public partial class DirectMessageDialog : Window
{
    public DirectMessageDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => MessageBox.FocusNativeEdit();
    }

    private void MessageBox_OnSendRequested(object? sender, EventArgs e)
    {
        if (DataContext is DirectMessageDialogViewModel viewModel
            && viewModel.SendCommand.CanExecute(null))
        {
            viewModel.SendCommand.Execute(null);
        }
    }
}
