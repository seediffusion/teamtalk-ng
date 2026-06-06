using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Threading;
using TeamTalkNg.Core.Accessibility;

namespace TeamTalkNg.App.Accessibility;

public sealed class WpfAutomationScreenReaderOutput : IScreenReaderOutput
{
    public bool IsAvailable => Application.Current is not null;

    public void Speak(string message, bool interrupt = false)
    {
        RaiseNotification(message, interrupt);
    }

    public void Braille(string message)
    {
        RaiseNotification(message, interrupt: false);
    }

    public void Output(string message, bool interrupt = false)
    {
        RaiseNotification(message, interrupt);
    }

    public void Dispose()
    {
    }

    private static void RaiseNotification(string message, bool interrupt)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        Dispatcher? dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
        {
            return;
        }

        void RaiseOnUiThread()
        {
            Window? window = Application.Current?.MainWindow;
            if (window is null)
            {
                return;
            }

            AutomationPeer? peer = UIElementAutomationPeer.FromElement(window)
                ?? UIElementAutomationPeer.CreatePeerForElement(window);
            peer?.RaiseNotificationEvent(
                AutomationNotificationKind.Other,
                interrupt ? AutomationNotificationProcessing.ImportantMostRecent : AutomationNotificationProcessing.CurrentThenMostRecent,
                message,
                "TeamTalkNgAnnouncement");
        }

        if (dispatcher.CheckAccess())
        {
            RaiseOnUiThread();
        }
        else
        {
            _ = dispatcher.BeginInvoke(RaiseOnUiThread, DispatcherPriority.Background);
        }
    }
}
