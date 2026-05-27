using TeamTalkNg.App.ViewModels;

namespace TeamTalkNg.App.Services;

public interface IChannelInformationDialogService
{
    void ShowChannelInformationDialog(ChannelTreeItemViewModel channel);
}
