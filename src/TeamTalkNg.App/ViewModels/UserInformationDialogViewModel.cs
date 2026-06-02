using System.Windows.Input;

namespace TeamTalkNg.App.ViewModels;

public sealed class UserInformationDialogViewModel
{
    public UserInformationDialogViewModel(ChannelTreeItemViewModel user)
    {
        Nickname = user.Name;
        Username = string.IsNullOrWhiteSpace(user.Username) ? "Not available" : user.Username;
        Channel = string.IsNullOrWhiteSpace(user.Path) ? "/" : user.Path;
        Status = GetStatus(user);
        Operator = user.IsOperator ? "Yes" : "No";
        CloseCommand = new RelayCommand(() => RequestClose?.Invoke(this, EventArgs.Empty));
    }

    public event EventHandler? RequestClose;

    public string Nickname { get; }

    public string Username { get; }

    public string Channel { get; }

    public string Status { get; }

    public string Operator { get; }

    public ICommand CloseCommand { get; }

    private static string GetStatus(ChannelTreeItemViewModel user)
    {
        string availability = user.IsAway ? "Away" : "Available";
        string speaking = user.IsTalking ? ", transmitting" : string.Empty;
        string message = string.IsNullOrWhiteSpace(user.StatusMessage) ? string.Empty : $", {user.StatusMessage}";
        return $"{availability}{speaking}{message}";
    }
}
