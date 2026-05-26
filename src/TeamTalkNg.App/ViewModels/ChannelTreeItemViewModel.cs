using System.Collections.ObjectModel;

namespace TeamTalkNg.App.ViewModels;

public sealed class ChannelTreeItemViewModel : ObservableObject
{
    private string name;
    private bool isTalking;
    private int userCount;

    public ChannelTreeItemViewModel(string name, ChannelTreeItemKind kind, int id = 0, string path = "")
    {
        this.name = name;
        Kind = kind;
        Id = id;
        Path = path;
    }

    public int Id { get; }

    public string Path { get; }

    public string Name
    {
        get => name;
        set
        {
            if (SetProperty(ref name, value))
            {
                OnPropertyChanged(nameof(AccessibleName));
            }
        }
    }

    public ChannelTreeItemKind Kind { get; }

    public ObservableCollection<ChannelTreeItemViewModel> Children { get; } = [];

    public int UserCount
    {
        get => userCount;
        set
        {
            if (SetProperty(ref userCount, value))
            {
                OnPropertyChanged(nameof(AccessibleName));
            }
        }
    }

    public bool IsTalking
    {
        get => isTalking;
        set
        {
            if (SetProperty(ref isTalking, value))
            {
                OnPropertyChanged(nameof(AccessibleName));
                OnPropertyChanged(nameof(Icon));
            }
        }
    }

    public string Icon => Kind switch
    {
        ChannelTreeItemKind.Server => "Server",
        ChannelTreeItemKind.Channel => "#",
        ChannelTreeItemKind.User when IsTalking => "Speaking",
        ChannelTreeItemKind.User => "User",
        _ => string.Empty
    };

    public string AccessibleName
    {
        get
        {
            string type = Kind.ToString().ToLowerInvariant();
            string state = IsTalking ? ", transmitting" : string.Empty;
            string count = Kind == ChannelTreeItemKind.Channel ? $", {UserCount} users" : string.Empty;
            return $"{Name}, {type}{count}{state}";
        }
    }

    public string AccessibleHelpText => Kind switch
    {
        ChannelTreeItemKind.Server => "TeamTalk server",
        ChannelTreeItemKind.Channel => "TeamTalk channel",
        ChannelTreeItemKind.User when IsTalking => "TeamTalk user, currently transmitting",
        ChannelTreeItemKind.User => "TeamTalk user",
        _ => string.Empty
    };

    public override string ToString()
    {
        return AccessibleName;
    }
}
