using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.Services;

public sealed record UserAccountsDialogResult(UserAccountsDialogAction Action, UserAccountSummary? SelectedAccount);
