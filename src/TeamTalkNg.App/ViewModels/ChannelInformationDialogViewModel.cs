using System.Windows.Input;

namespace TeamTalkNg.App.ViewModels;

public sealed class ChannelInformationDialogViewModel
{
    public ChannelInformationDialogViewModel(ChannelTreeItemViewModel channel)
    {
        Name = channel.Name;
        Path = channel.Path;
        Topic = string.IsNullOrWhiteSpace(channel.Topic) ? "No topic" : channel.Topic;
        Protected = channel.IsProtected ? "Yes" : "No";
        UserCount = channel.UserCount.ToString();
        CloseCommand = new RelayCommand(() => RequestClose?.Invoke(this, EventArgs.Empty));
    }

    public event EventHandler? RequestClose;

    public string Name { get; }

    public string Path { get; }

    public string Topic { get; }

    public string Protected { get; }

    public string UserCount { get; }

    public ICommand CloseCommand { get; }
}
