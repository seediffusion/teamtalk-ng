using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
        InputManager.Current.PreProcessInput += InputManager_OnPreProcessInput;
        Closed += (_, _) => InputManager.Current.PreProcessInput -= InputManager_OnPreProcessInput;
    }

    private void InputManager_OnPreProcessInput(object sender, PreProcessInputEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        InputEventArgs input = e.StagingItem.Input;
        if (input is KeyEventArgs
            or TextCompositionEventArgs
            or MouseButtonEventArgs
            or MouseWheelEventArgs)
        {
            viewModel.NotifyUserActivity();
        }
    }

    private void ChannelsTree_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is MainWindowViewModel viewModel && e.NewValue is ChannelTreeItemViewModel item)
        {
            viewModel.SelectedChannelItem = item;
        }
    }

    private async void ChannelsTree_OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        e.Handled = true;
        await ActivateSelectedTreeItemAsync();
    }

    private async void ChannelsTree_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        await ActivateSelectedTreeItemAsync();
    }

    private async Task ActivateSelectedTreeItemAsync()
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.ActivateSelectedTreeItemAsync();
        }
    }

    private async void ChatHistoryList_OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        if (DataContext is MainWindowViewModel viewModel)
        {
            e.Handled = true;
            await viewModel.OpenSelectedDirectMessageReplyAsync();
        }
    }

    private void MessageTextBox_OnSendRequested(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel
            && viewModel.SendMessageCommand.CanExecute(null))
        {
            viewModel.SendMessageCommand.Execute(null);
        }
    }

    private void MainToolBar_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (ReferenceEquals(e.NewFocus, MainToolBar))
        {
            ConnectToolBarButton.Focus();
        }
    }

    private void MainToolBar_OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
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
        List<System.Windows.Controls.Primitives.ButtonBase> buttons = GetToolbarButtons()
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

    private IEnumerable<System.Windows.Controls.Primitives.ButtonBase> GetToolbarButtons()
    {
        return Descendants<System.Windows.Controls.Primitives.ButtonBase>(MainToolBar)
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
