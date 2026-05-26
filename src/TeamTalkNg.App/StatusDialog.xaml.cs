using System.Windows;

namespace TeamTalkNg.App;

public partial class StatusDialog : Window
{
    public StatusDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => StatusMessageBox.Focus();
    }
}
