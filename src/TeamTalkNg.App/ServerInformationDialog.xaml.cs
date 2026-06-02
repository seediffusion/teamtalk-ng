using System.Windows;

namespace TeamTalkNg.App;

public partial class ServerInformationDialog : Window
{
    public ServerInformationDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => ServerNameBox.Focus();
    }
}
