using System.Windows;
using System.Windows.Controls;
using TeamTalkNg.App.ViewModels;

namespace TeamTalkNg.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void ChannelsTree_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is MainWindowViewModel viewModel && e.NewValue is ChannelTreeItemViewModel item)
        {
            viewModel.SelectedChannelItem = item;
        }
    }
}
