using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.Services;

public interface IServerInformationDialogService
{
    void ShowServerInformationDialog(ServerInformationSummary serverInformation);
}
