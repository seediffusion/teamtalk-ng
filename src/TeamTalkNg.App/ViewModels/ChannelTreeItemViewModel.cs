using System.Collections.ObjectModel;

namespace TeamTalkNg.App.ViewModels;

public sealed class ChannelTreeItemViewModel : ObservableObject
{
    private bool isTalking;

    public ChannelTreeItemViewModel(string name, ChannelTreeItemKind kind)
    {
        Name = name;
        Kind = kind;
    }

    public string Name { get; }

    public ChannelTreeItemKind Kind { get; }

    public ObservableCollection<ChannelTreeItemViewModel> Children { get; } = [];

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
            return $"{Name}, {type}{state}";
        }
    }
}
