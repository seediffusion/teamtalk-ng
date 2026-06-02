using TeamTalkNg.App.ViewModels;
using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.Services;

public interface IUserAudioSettingsDialogService
{
    UserAudioSettingsRequest? ShowUserAudioSettingsDialog(ChannelTreeItemViewModel user);
}
