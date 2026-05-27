using System.Windows.Input;

namespace TeamTalkNg.App.ViewModels;

public sealed class ChannelTopicDialogViewModel : ObservableObject
{
    private string topic;

    public ChannelTopicDialogViewModel(string channelName, string currentTopic)
    {
        ChannelName = channelName;
        topic = currentTopic;
        SaveCommand = new RelayCommand(() => RequestClose?.Invoke(this, true));
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, false));
    }

    public event EventHandler<bool>? RequestClose;

    public string ChannelName { get; }

    public string Topic
    {
        get => topic;
        set => SetProperty(ref topic, value);
    }

    public ICommand SaveCommand { get; }

    public ICommand CancelCommand { get; }
}
