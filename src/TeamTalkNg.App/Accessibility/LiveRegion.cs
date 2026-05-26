using System.Windows;
using System.Windows.Automation.Peers;

namespace TeamTalkNg.App.Accessibility;

public static class LiveRegion
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
        "Text",
        typeof(string),
        typeof(LiveRegion),
        new PropertyMetadata(string.Empty, OnTextChanged));

    public static string GetText(DependencyObject element)
    {
        return (string)element.GetValue(TextProperty);
    }

    public static void SetText(DependencyObject element, string value)
    {
        element.SetValue(TextProperty, value);
    }

    private static void OnTextChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not UIElement element || e.NewValue is not string text || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        AutomationPeer peer = UIElementAutomationPeer.FromElement(element)
            ?? UIElementAutomationPeer.CreatePeerForElement(element);

        peer?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
    }
}
