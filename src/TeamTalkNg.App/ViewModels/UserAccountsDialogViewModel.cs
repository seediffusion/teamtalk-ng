using System.Collections.ObjectModel;
using System.Windows.Input;
using TeamTalkNg.App.Services;
using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.ViewModels;

public sealed class UserAccountsDialogViewModel : ObservableObject
{
    private UserAccountViewModel? selectedAccount;

    public UserAccountsDialogViewModel(IReadOnlyList<UserAccountSummary> accounts)
    {
        Accounts = new ObservableCollection<UserAccountViewModel>(
            accounts
                .OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .Select(item => new UserAccountViewModel(item)));
        selectedAccount = Accounts.FirstOrDefault();
        NewCommand = new RelayCommand(() => Close(UserAccountsDialogAction.New));
        DeleteCommand = new RelayCommand(() => Close(UserAccountsDialogAction.Delete), () => SelectedAccount is not null);
        CloseCommand = new RelayCommand(() => Close(UserAccountsDialogAction.Close));
    }

    public event EventHandler<UserAccountsDialogAction>? RequestClose;

    public ObservableCollection<UserAccountViewModel> Accounts { get; }

    public UserAccountViewModel? SelectedAccount
    {
        get => selectedAccount;
        set
        {
            if (SetProperty(ref selectedAccount, value) && DeleteCommand is RelayCommand command)
            {
                command.RaiseCanExecuteChanged();
            }
        }
    }

    public UserAccountsDialogAction SelectedAction { get; private set; } = UserAccountsDialogAction.Close;

    public ICommand NewCommand { get; }

    public ICommand DeleteCommand { get; }

    public ICommand CloseCommand { get; }

    private void Close(UserAccountsDialogAction action)
    {
        SelectedAction = action;
        RequestClose?.Invoke(this, action);
    }
}
