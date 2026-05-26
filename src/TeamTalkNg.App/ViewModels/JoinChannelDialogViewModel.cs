using System.Windows.Input;

namespace TeamTalkNg.App.ViewModels;

public sealed class JoinChannelDialogViewModel : ObservableObject
{
    private string password = string.Empty;

    public JoinChannelDialogViewModel(string channelName)
    {
        ChannelName = channelName;
        JoinCommand = new RelayCommand(() => RequestClose?.Invoke(this, true));
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, false));
    }

    public event EventHandler<bool>? RequestClose;

    public string ChannelName { get; }

    public string Password
    {
        get => password;
        set => SetProperty(ref password, value);
    }

    public ICommand JoinCommand { get; }

    public ICommand CancelCommand { get; }
}
