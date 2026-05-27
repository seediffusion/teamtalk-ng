using System.Windows;

namespace TeamTalkNg.App;

public partial class ChannelInformationDialog : Window
{
    public ChannelInformationDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => NameBox.Focus();
    }
}
