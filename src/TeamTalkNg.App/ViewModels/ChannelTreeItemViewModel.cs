using System.Collections.ObjectModel;

namespace TeamTalkNg.App.ViewModels;

public sealed class ChannelTreeItemViewModel : ObservableObject
{
    private string name;
    private bool isTalking;
    private bool isAway;
    private bool isProtected;
    private bool isPermanent;
    private bool isOperator;
    private string statusMessage = string.Empty;
    private string topic = string.Empty;
    private string username = string.Empty;
    private int userCount;
    private int voiceVolumePercent = 100;
    private bool isVoiceMuted;
    private bool isExpanded;
    private bool showUserCount = true;
    private bool showUsername;
    private bool showIcon = true;
    private bool showChannelTopic;

    public ChannelTreeItemViewModel(string name, ChannelTreeItemKind kind, int id = 0, string path = "", int sortOrder = 0)
    {
        this.name = name;
        Kind = kind;
        Id = id;
        Path = path;
        SortOrder = sortOrder;
    }

    private int id;

    public int Id
    {
        get => id;
        set => SetProperty(ref id, value);
    }

    public string Path { get; }

    public int SortOrder { get; }

    public string Name
    {
        get => name;
        set
        {
            if (SetProperty(ref name, value))
            {
                OnPropertyChanged(nameof(AccessibleName));
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    public ChannelTreeItemKind Kind { get; }

    public ObservableCollection<ChannelTreeItemViewModel> Children { get; } = [];

    public bool IsExpanded
    {
        get => isExpanded;
        set => SetProperty(ref isExpanded, value);
    }

    public int UserCount
    {
        get => userCount;
        set
        {
            if (SetProperty(ref userCount, value))
            {
                OnPropertyChanged(nameof(AccessibleName));
                OnPropertyChanged(nameof(DisplayText));
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
                OnPropertyChanged(nameof(VisualIndicator));
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
                OnPropertyChanged(nameof(VisualIndicator));
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
        set
        {
            if (SetProperty(ref topic, value))
            {
                OnPropertyChanged(nameof(AccessibleName));
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    public string Username
    {
        get => username;
        set
        {
            if (SetProperty(ref username, value))
            {
                OnPropertyChanged(nameof(AccessibleName));
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    public bool IsOperator
    {
        get => isOperator;
        set => SetProperty(ref isOperator, value);
    }

    public int VoiceVolumePercent
    {
        get => voiceVolumePercent;
        set => SetProperty(ref voiceVolumePercent, Math.Clamp(value, 0, 200));
    }

    public bool IsVoiceMuted
    {
        get => isVoiceMuted;
        set
        {
            if (SetProperty(ref isVoiceMuted, value))
            {
                OnPropertyChanged(nameof(AccessibleName));
                OnPropertyChanged(nameof(VisualIndicator));
                OnPropertyChanged(nameof(AccessibleHelpText));
            }
        }
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

    public string VisualIndicator => !showIcon ? string.Empty : Kind switch
    {
        ChannelTreeItemKind.Channel => "#",
        ChannelTreeItemKind.User when IsVoiceMuted => "M",
        ChannelTreeItemKind.User when IsTalking => ">",
        ChannelTreeItemKind.User when IsAway => "-",
        _ => string.Empty
    };

    public string Icon => VisualIndicator;

    public string DisplayName => Kind == ChannelTreeItemKind.User && showUsername && !string.IsNullOrWhiteSpace(Username)
        ? Username
        : Name;

    public string DisplayText
    {
        get
        {
            string text = DisplayName;
            if (Kind == ChannelTreeItemKind.Channel && showUserCount)
            {
                text += $" ({UserCount})";
            }

            if (Kind == ChannelTreeItemKind.Channel && showChannelTopic && !string.IsNullOrWhiteSpace(Topic))
            {
                text += $": {Topic}";
            }

            return text;
        }
    }

    public string AccessibleName
    {
        get
        {
            string type = Kind.ToString().ToLowerInvariant();
            string state = IsTalking ? ", transmitting" : IsAway ? ", away" : string.Empty;
            string mute = Kind == ChannelTreeItemKind.User && IsVoiceMuted ? ", muted" : string.Empty;
            string message = string.IsNullOrWhiteSpace(StatusMessage) ? string.Empty : $", status: {StatusMessage}";
            string protection = Kind == ChannelTreeItemKind.Channel && IsProtected ? ", password protected" : string.Empty;
            string count = Kind == ChannelTreeItemKind.Channel && showUserCount ? $", {UserCount} users" : string.Empty;
            string topicText = Kind == ChannelTreeItemKind.Channel && showChannelTopic && !string.IsNullOrWhiteSpace(Topic) ? $", topic: {Topic}" : string.Empty;
            return $"{DisplayName}, {type}{count}{protection}{topicText}{state}{mute}{message}";
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
        ChannelTreeItemKind.User when IsVoiceMuted => "TeamTalk user, locally muted",
        ChannelTreeItemKind.User => "TeamTalk user",
        _ => string.Empty
    };

    public override string ToString()
    {
        return AccessibleName;
    }

    public void ApplyDisplaySettings(bool showUserCounts, bool showUsernamesInsteadOfNicknames, bool showChannelIcons, bool showChannelTopics)
    {
        showUserCount = showUserCounts;
        showUsername = showUsernamesInsteadOfNicknames;
        showIcon = showChannelIcons;
        showChannelTopic = showChannelTopics;
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(DisplayText));
        OnPropertyChanged(nameof(VisualIndicator));
        OnPropertyChanged(nameof(Icon));
        OnPropertyChanged(nameof(AccessibleName));
    }
}
