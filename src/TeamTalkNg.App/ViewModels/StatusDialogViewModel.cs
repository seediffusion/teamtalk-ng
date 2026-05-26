using System.Windows.Input;
using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.ViewModels;

public sealed class StatusDialogViewModel : ObservableObject
{
    private bool isAway;
    private string statusMessage;

    public StatusDialogViewModel(bool isAway, string statusMessage)
    {
        this.isAway = isAway;
        this.statusMessage = statusMessage;
        SaveCommand = new RelayCommand(() => RequestClose?.Invoke(this, true));
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, false));
    }

    public event EventHandler<bool>? RequestClose;

    public bool IsAway
    {
        get => isAway;
        set => SetProperty(ref isAway, value);
    }

    public string StatusMessage
    {
        get => statusMessage;
        set => SetProperty(ref statusMessage, value);
    }

    public ICommand SaveCommand { get; }

    public ICommand CancelCommand { get; }

    public UserStatusRequest ToRequest()
    {
        return new UserStatusRequest(IsAway, StatusMessage.Trim());
    }
}
