using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.Services;

public interface IUserAccountDialogService
{
    UserAccountCreationRequest? ShowCreateUserAccountDialog();
}
