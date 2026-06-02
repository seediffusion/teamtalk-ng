using TeamTalkNg.App.ViewModels;

namespace TeamTalkNg.App.Services;

public interface IMoveUserDialogService
{
    string? ShowMoveUserDialog(string userName, IEnumerable<MoveUserDestinationViewModel> destinations);
}
