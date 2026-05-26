using System.Windows.Input;

namespace TeamTalkNg.App.ViewModels;

public sealed class ConnectionTargetDialogViewModel : ObservableObject
{
    private string target = string.Empty;

    public ConnectionTargetDialogViewModel()
    {
        ConnectCommand = new RelayCommand(() => RequestClose?.Invoke(this, true), () => !string.IsNullOrWhiteSpace(Target));
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, false));
    }

    public event EventHandler<bool>? RequestClose;

    public ICommand ConnectCommand { get; }

    public ICommand CancelCommand { get; }

    public string Target
    {
        get => target;
        set
        {
            if (SetProperty(ref target, value) && ConnectCommand is RelayCommand command)
            {
                command.RaiseCanExecuteChanged();
            }
        }
    }
}
