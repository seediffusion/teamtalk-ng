using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.ViewModels;

public sealed class UserAccountRightOptionViewModel : ObservableObject
{
    private bool isSelected;

    public UserAccountRightOptionViewModel(string name, UserAccountRights right, bool isSelected)
    {
        Name = name;
        Right = right;
        this.isSelected = isSelected;
    }

    public string Name { get; }

    public UserAccountRights Right { get; }

    public bool IsSelected
    {
        get => isSelected;
        set => SetProperty(ref isSelected, value);
    }

    public string AccessibleName => Name;

    public override string ToString()
    {
        return AccessibleName;
    }
}
