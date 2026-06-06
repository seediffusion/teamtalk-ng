namespace TeamTalkNg.App.Services;

using TeamTalkNg.App.ViewModels;

public interface IDirectMessageDialogService
{
    string? ShowDirectMessageDialog(string recipientName, IReadOnlyList<ChatMessageViewModel> conversation);
}
