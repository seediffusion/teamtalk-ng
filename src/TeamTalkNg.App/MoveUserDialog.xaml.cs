using System.Windows;

namespace TeamTalkNg.App;

public partial class MoveUserDialog : Window
{
    public MoveUserDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => DestinationList.Focus();
    }
}
