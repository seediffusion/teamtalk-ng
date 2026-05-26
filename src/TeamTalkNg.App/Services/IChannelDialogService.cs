using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.Services;

public interface IChannelDialogService
{
    ChannelCreationRequest? ShowCreateChannelDialog(string parentPath);
}
