using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.Services;

public interface IUserAccountsDialogService
{
    UserAccountsDialogResult ShowUserAccountsDialog(IReadOnlyList<UserAccountSummary> accounts);
}
