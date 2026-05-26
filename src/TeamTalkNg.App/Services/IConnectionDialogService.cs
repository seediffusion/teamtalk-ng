using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.Services;

public interface IConnectionDialogService
{
    TeamTalkServerProfile? ShowConnectDialog(IReadOnlyList<TeamTalkServerProfile> profiles);
}
