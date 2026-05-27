using System.Collections.ObjectModel;

namespace TeamTalkNg.App.ViewModels;

public sealed class ChannelTreeItemViewModel : ObservableObject
{
    private string name;
    private bool isTalking;
    private bool isAway;
    private bool isProtected;
    private bool isPermanent;
    private string statusMessage = string.Empty;
    private string topic = string.Empty;
    private int userCount;

    public ChannelTreeItemViewModel(string name, ChannelTreeItemKind kind, int id = 0, string path = "")
    {
        this.name = name;
        Kind = kind;
        Id = id;
        Path = path;
    }

    private int id;

    public int Id
    {
        get => id;
        set => SetProperty(ref id, value);
    }

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
                OnPropertyChanged(nameof(AccessibleHelpText));
            }
        }
    }

    public bool IsAway
    {
        get => isAway;
        set
        {
            if (SetProperty(ref isAway, value))
            {
                OnPropertyChanged(nameof(AccessibleName));
                OnPropertyChanged(nameof(Icon));
                OnPropertyChanged(nameof(AccessibleHelpText));
            }
        }
    }

    public string StatusMessage
    {
        get => statusMessage;
        set
        {
            if (SetProperty(ref statusMessage, value))
            {
                OnPropertyChanged(nameof(AccessibleName));
                OnPropertyChanged(nameof(AccessibleHelpText));
            }
        }
    }

    public string Topic
    {
        get => topic;
        set => SetProperty(ref topic, value);
    }

    public bool IsProtected
    {
        get => isProtected;
        set
        {
            if (SetProperty(ref isProtected, value))
            {
                OnPropertyChanged(nameof(AccessibleName));
                OnPropertyChanged(nameof(AccessibleHelpText));
            }
        }
    }

    public bool IsPermanent
    {
        get => isPermanent;
        set
        {
            if (SetProperty(ref isPermanent, value))
            {
                OnPropertyChanged(nameof(IsPermanent));
            }
        }
    }

    public string Icon => Kind switch
    {
        ChannelTreeItemKind.Server => "Server",
        ChannelTreeItemKind.Channel => "#",
        ChannelTreeItemKind.User when IsTalking => "Speaking",
        ChannelTreeItemKind.User when IsAway => "Away",
        ChannelTreeItemKind.User => "User",
        _ => string.Empty
    };

    public string AccessibleName
    {
        get
        {
            string type = Kind.ToString().ToLowerInvariant();
            string state = IsTalking ? ", transmitting" : IsAway ? ", away" : string.Empty;
            string message = string.IsNullOrWhiteSpace(StatusMessage) ? string.Empty : $", status: {StatusMessage}";
            string protection = Kind == ChannelTreeItemKind.Channel && IsProtected ? ", password protected" : string.Empty;
            string count = Kind == ChannelTreeItemKind.Channel ? $", {UserCount} users" : string.Empty;
            return $"{Name}, {type}{count}{protection}{state}{message}";
        }
    }

    public string AccessibleHelpText => Kind switch
    {
        ChannelTreeItemKind.Server => "TeamTalk server",
        ChannelTreeItemKind.Channel when IsProtected => "TeamTalk channel, password protected",
        ChannelTreeItemKind.Channel => "TeamTalk channel",
        ChannelTreeItemKind.User when IsTalking && !string.IsNullOrWhiteSpace(StatusMessage) => $"TeamTalk user, currently transmitting, status: {StatusMessage}",
        ChannelTreeItemKind.User when IsTalking => "TeamTalk user, currently transmitting",
        ChannelTreeItemKind.User when IsAway && !string.IsNullOrWhiteSpace(StatusMessage) => $"TeamTalk user, away, status: {StatusMessage}",
        ChannelTreeItemKind.User when IsAway => "TeamTalk user, away",
        ChannelTreeItemKind.User when !string.IsNullOrWhiteSpace(StatusMessage) => $"TeamTalk user, status: {StatusMessage}",
        ChannelTreeItemKind.User => "TeamTalk user",
        _ => string.Empty
    };

    public override string ToString()
    {
        return AccessibleName;
    }
}
