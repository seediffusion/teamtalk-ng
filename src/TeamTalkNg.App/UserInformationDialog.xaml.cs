using System.Windows;

namespace TeamTalkNg.App;

public partial class UserInformationDialog : Window
{
    public UserInformationDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => NicknameBox.Focus();
    }
}
