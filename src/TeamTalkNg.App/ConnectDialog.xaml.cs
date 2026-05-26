using System.Windows;
using System.Windows.Controls;
using TeamTalkNg.App.ViewModels;

namespace TeamTalkNg.App;

public partial class ConnectDialog : Window
{
    public ConnectDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => DisplayNameBox.Focus();
    }

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ConnectDialogViewModel viewModel && sender is PasswordBox passwordBox)
        {
            viewModel.Password = passwordBox.Password;
        }
    }

    private void ChannelPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ConnectDialogViewModel viewModel && sender is PasswordBox passwordBox)
        {
            viewModel.ChannelPassword = passwordBox.Password;
        }
    }
}
