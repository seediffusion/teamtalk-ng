using TeamTalkNg.Core.TeamTalk;
using TeamTalkNg.TeamTalkSdk;

namespace TeamTalkNg.App.Services;

public static class TeamTalkSessionFactory
{
    public static ITeamTalkSession CreateDefaultSession()
    {
        var options = new TeamTalkSdkOptions();
        var sdkSession = new TeamTalkSdkSession(options);

        return sdkSession.Availability.IsAvailable
            ? sdkSession
            : new MockTeamTalkSession();
    }
}
