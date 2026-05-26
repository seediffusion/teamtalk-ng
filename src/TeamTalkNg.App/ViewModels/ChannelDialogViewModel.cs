using System.Windows.Input;
using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.ViewModels;

public sealed class ChannelDialogViewModel : ObservableObject
{
    private string name = string.Empty;
    private string topic = string.Empty;
    private string password = string.Empty;
    private int maxUsers;
    private bool isPermanent = true;

    public ChannelDialogViewModel(string parentPath)
    {
        ParentPath = string.IsNullOrWhiteSpace(parentPath) ? "/" : parentPath;
        CreateCommand = new RelayCommand(() => RequestClose?.Invoke(this, true), CanCreate);
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, false));
    }

    public event EventHandler<bool>? RequestClose;

    public string ParentPath { get; }

    public ICommand CreateCommand { get; }

    public ICommand CancelCommand { get; }

    public string Name
    {
        get => name;
        set
        {
            if (SetProperty(ref name, value) && CreateCommand is RelayCommand command)
            {
                command.RaiseCanExecuteChanged();
            }
        }
    }

    public string Topic
    {
        get => topic;
        set => SetProperty(ref topic, value);
    }

    public string Password
    {
        get => password;
        set => SetProperty(ref password, value);
    }

    public int MaxUsers
    {
        get => maxUsers;
        set => SetProperty(ref maxUsers, Math.Max(0, value));
    }

    public bool IsPermanent
    {
        get => isPermanent;
        set => SetProperty(ref isPermanent, value);
    }

    public ChannelCreationRequest ToRequest()
    {
        return new ChannelCreationRequest(
            ParentPath,
            Name.Trim(),
            Topic.Trim(),
            Password,
            MaxUsers,
            IsPermanent);
    }

    private bool CanCreate()
    {
        return !string.IsNullOrWhiteSpace(Name)
            && !Name.Contains('/', StringComparison.Ordinal);
    }
}
