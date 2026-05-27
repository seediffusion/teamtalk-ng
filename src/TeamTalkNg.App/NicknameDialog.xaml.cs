using System.Windows;

namespace TeamTalkNg.App;

public partial class NicknameDialog : Window
{
    public NicknameDialog()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            NicknameBox.Focus();
            NicknameBox.SelectAll();
        };
    }
}
