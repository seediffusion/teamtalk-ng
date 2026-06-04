using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.Services;

public interface IBannedUsersDialogService
{
    BannedUserSummary? ShowBannedUsersDialog(IReadOnlyList<BannedUserSummary> bannedUsers);
}
