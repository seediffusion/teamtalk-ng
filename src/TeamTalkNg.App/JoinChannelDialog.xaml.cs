using System.Windows;
using TeamTalkNg.App.ViewModels;

namespace TeamTalkNg.App;

public partial class JoinChannelDialog : Window
{
    public JoinChannelDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => PasswordBox.Focus();
    }

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is JoinChannelDialogViewModel viewModel)
        {
            viewModel.Password = PasswordBox.Password;
        }
    }
}
