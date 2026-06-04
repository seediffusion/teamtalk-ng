using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.Services;

public interface IServerStatisticsDialogService
{
    void ShowServerStatisticsDialog(ServerStatisticsSummary serverStatistics);
}
