using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.Services;

public interface IStatusDialogService
{
    UserStatusRequest? ShowStatusDialog(bool isAway, string statusMessage);
}
