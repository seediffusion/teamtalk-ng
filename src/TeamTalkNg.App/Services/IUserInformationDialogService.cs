using TeamTalkNg.App.ViewModels;

namespace TeamTalkNg.App.Services;

public interface IUserInformationDialogService
{
    void ShowUserInformationDialog(ChannelTreeItemViewModel user);
}
