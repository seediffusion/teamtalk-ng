using System.Windows.Input;

namespace TeamTalkNg.App.ViewModels;

public sealed class NicknameDialogViewModel : ObservableObject
{
    private string nickname;

    public NicknameDialogViewModel(string currentNickname)
    {
        nickname = currentNickname;
        SaveCommand = new RelayCommand(() => RequestClose?.Invoke(this, true), CanSave);
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, false));
    }

    public event EventHandler<bool>? RequestClose;

    public string Nickname
    {
        get => nickname;
        set
        {
            if (SetProperty(ref nickname, value) && SaveCommand is RelayCommand command)
            {
                command.RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand SaveCommand { get; }

    public ICommand CancelCommand { get; }

    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(Nickname);
    }
}
