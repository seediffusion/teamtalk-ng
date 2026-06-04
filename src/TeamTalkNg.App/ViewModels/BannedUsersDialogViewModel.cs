using System.Collections.ObjectModel;
using System.Windows.Input;
using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.ViewModels;

public sealed class BannedUsersDialogViewModel : ObservableObject
{
    private BannedUserViewModel? selectedBan;

    public BannedUsersDialogViewModel(IReadOnlyList<BannedUserSummary> bannedUsers)
    {
        BannedUsers = new ObservableCollection<BannedUserViewModel>(
            bannedUsers
                .OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .Select(item => new BannedUserViewModel(item)));
        selectedBan = BannedUsers.FirstOrDefault();
        RemoveBanCommand = new RelayCommand(() => RequestClose?.Invoke(this, true), () => SelectedBan is not null);
        CloseCommand = new RelayCommand(() => RequestClose?.Invoke(this, false));
    }

    public event EventHandler<bool>? RequestClose;

    public ObservableCollection<BannedUserViewModel> BannedUsers { get; }

    public BannedUserViewModel? SelectedBan
    {
        get => selectedBan;
        set
        {
            if (SetProperty(ref selectedBan, value) && RemoveBanCommand is RelayCommand command)
            {
                command.RaiseCanExecuteChanged();
            }
        }
    }

    public bool HasBannedUsers => BannedUsers.Count > 0;

    public string EmptyMessage => "No banned users";

    public ICommand RemoveBanCommand { get; }

    public ICommand CloseCommand { get; }
}
