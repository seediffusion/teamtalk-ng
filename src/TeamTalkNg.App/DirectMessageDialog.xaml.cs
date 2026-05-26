using System.Windows;

namespace TeamTalkNg.App;

public partial class DirectMessageDialog : Window
{
    public DirectMessageDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => MessageBox.Focus();
    }
}
