using System.Windows;

namespace TeamTalkNg.App;

public partial class ChannelTopicDialog : Window
{
    public ChannelTopicDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => TopicBox.Focus();
    }
}
