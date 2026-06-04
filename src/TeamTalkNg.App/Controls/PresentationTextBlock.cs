using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace TeamTalkNg.App.Controls;

public sealed class PresentationTextBlock : TextBlock
{
    protected override AutomationPeer? OnCreateAutomationPeer()
    {
        return null;
    }
}
