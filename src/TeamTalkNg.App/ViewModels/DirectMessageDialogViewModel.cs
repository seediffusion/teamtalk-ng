using System.Windows.Input;

namespace TeamTalkNg.App.ViewModels;

public sealed class DirectMessageDialogViewModel : ObservableObject
{
    private string message = string.Empty;

    public DirectMessageDialogViewModel(string recipientName)
    {
        RecipientName = recipientName;
        SendCommand = new RelayCommand(() => RequestClose?.Invoke(this, true), CanSend);
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, false));
    }

    public event EventHandler<bool>? RequestClose;

    public string RecipientName { get; }

    public string Message
    {
        get => message;
        set
        {
            if (SetProperty(ref message, value) && SendCommand is RelayCommand command)
            {
                command.RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand SendCommand { get; }

    public ICommand CancelCommand { get; }

    private bool CanSend()
    {
        return !string.IsNullOrWhiteSpace(Message);
    }
}
