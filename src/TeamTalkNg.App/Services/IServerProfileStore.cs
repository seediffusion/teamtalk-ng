using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.Services;

public interface IServerProfileStore
{
    Task<IReadOnlyList<TeamTalkServerProfile>> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(IReadOnlyList<TeamTalkServerProfile> profiles, CancellationToken cancellationToken = default);
}
