using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TeamTalkNg.App.ViewModels;

namespace TeamTalkNg.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => ChannelsTree.Focus();
    }

    private void ChannelsTree_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is MainWindowViewModel viewModel && e.NewValue is ChannelTreeItemViewModel item)
        {
            viewModel.SelectedChannelItem = item;
        }
    }

    private void MainToolBar_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (ReferenceEquals(e.NewFocus, MainToolBar))
        {
            ConnectToolBarButton.Focus();
        }
    }

    private void MainToolBar_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Right or Key.Down)
        {
            FocusToolbarButton(FocusNavigationDirection.Next);
            e.Handled = true;
            return;
        }

        if (e.Key is Key.Left or Key.Up)
        {
            FocusToolbarButton(FocusNavigationDirection.Previous);
            e.Handled = true;
            return;
        }

        if (e.Key != Key.Tab)
        {
            return;
        }

        MainToolBar.MoveFocus(new TraversalRequest(e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Shift)
            ? FocusNavigationDirection.Previous
            : FocusNavigationDirection.Next));
        e.Handled = true;
    }

    private void FocusToolbarButton(FocusNavigationDirection direction)
    {
        List<Button> buttons = GetToolbarButtons()
            .Where(button => button.IsEnabled && button.Focusable)
            .ToList();

        if (buttons.Count == 0)
        {
            return;
        }

        int currentIndex = buttons.FindIndex(button => button.IsKeyboardFocusWithin);
        int nextIndex = direction == FocusNavigationDirection.Previous
            ? (currentIndex <= 0 ? buttons.Count - 1 : currentIndex - 1)
            : (currentIndex < 0 || currentIndex >= buttons.Count - 1 ? 0 : currentIndex + 1);

        buttons[nextIndex].Focus();
    }

    private IEnumerable<Button> GetToolbarButtons()
    {
        return Descendants<Button>(MainToolBar)
            .Where(button => Equals(button.Tag, "ToolbarButton"));
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject root)
        where T : DependencyObject
    {
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int index = 0; index < count; index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, index);
            if (child is T match)
            {
                yield return match;
            }

            foreach (T descendant in Descendants<T>(child))
            {
                yield return descendant;
            }
        }
    }
}
