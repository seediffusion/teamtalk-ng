using System.Windows;
using TeamTalkNg.App.ViewModels;

namespace TeamTalkNg.App;

public partial class ChannelDialog : Window
{
    public ChannelDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => NameBox.Focus();
    }

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ChannelDialogViewModel viewModel)
        {
            viewModel.Password = PasswordBox.Password;
        }
    }
}
