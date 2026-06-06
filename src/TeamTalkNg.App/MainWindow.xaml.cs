using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using TeamTalkNg.App.ViewModels;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace TeamTalkNg.App;

public partial class MainWindow : Window
{
    private Forms.NotifyIcon? notifyIcon;
    private bool isRestoringFromTray;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_OnLoaded;
        StateChanged += MainWindow_OnStateChanged;
        Closing += MainWindow_OnClosing;
        InputManager.Current.PreProcessInput += InputManager_OnPreProcessInput;
        Closed += MainWindow_OnClosed;
    }

    public void ApplyInitialWindowBehavior()
    {
        if (DataContext is MainWindowViewModel { StartMinimized: true, MinimizeToTray: true })
        {
            HideToTray();
        }
    }

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (WindowState != WindowState.Minimized && IsVisible)
        {
            ChannelsTree.Focus();
        }
    }

    private void MainWindow_OnStateChanged(object? sender, EventArgs e)
    {
        if (isRestoringFromTray)
        {
            return;
        }

        if (WindowState == WindowState.Minimized
            && DataContext is MainWindowViewModel { MinimizeToTray: true })
        {
            HideToTray();
        }
    }

    private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        if (DataContext is MainWindowViewModel { ConfirmExit: true })
        {
            MessageBoxResult result = MessageBox.Show(
                this,
                "Exit TeamTalk NG?",
                "Exit TeamTalk NG",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);
            if (result != MessageBoxResult.Yes)
            {
                e.Cancel = true;
                return;
            }
        }

        DisposeTrayIcon();
    }

    private void MainWindow_OnClosed(object? sender, EventArgs e)
    {
        InputManager.Current.PreProcessInput -= InputManager_OnPreProcessInput;
        DisposeTrayIcon();
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

    private void HideToTray()
    {
        EnsureTrayIcon();
        ShowInTaskbar = false;
        Hide();
    }

    private void RestoreFromTray()
    {
        Dispatcher.Invoke(() =>
        {
            isRestoringFromTray = true;
            ShowInTaskbar = true;
            Show();
            WindowState = WindowState.Normal;
            Activate();
            ChannelsTree.Focus();
            isRestoringFromTray = false;
            HideTrayIcon();
        });
    }

    private void EnsureTrayIcon()
    {
        if (notifyIcon is not null)
        {
            notifyIcon.Visible = true;
            return;
        }

        var openItem = new Forms.ToolStripMenuItem("Open TeamTalk NG");
        openItem.Click += (_, _) => RestoreFromTray();

        var exitItem = new Forms.ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => Dispatcher.Invoke(Close);

        var contextMenu = new Forms.ContextMenuStrip();
        contextMenu.Items.Add(openItem);
        contextMenu.Items.Add(exitItem);

        notifyIcon = new Forms.NotifyIcon
        {
            Icon = Drawing.SystemIcons.Application,
            Text = "TeamTalk NG",
            Visible = true,
            ContextMenuStrip = contextMenu
        };
        notifyIcon.DoubleClick += (_, _) => RestoreFromTray();
    }

    private void HideTrayIcon()
    {
        if (notifyIcon is not null)
        {
            notifyIcon.Visible = false;
        }
    }

    private void DisposeTrayIcon()
    {
        if (notifyIcon is null)
        {
            return;
        }

        Forms.ContextMenuStrip? contextMenu = notifyIcon.ContextMenuStrip;
        notifyIcon.Visible = false;
        notifyIcon.Dispose();
        contextMenu?.Dispose();
        notifyIcon = null;
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
