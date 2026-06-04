namespace TeamTalkNg.App.ViewModels;

public sealed record MoveUserDestinationViewModel(string Name, string Path)
{
    public string AccessibleName => string.IsNullOrWhiteSpace(Name) || string.Equals(Name, Path, StringComparison.Ordinal)
        ? Path
        : $"{Name}, {Path}";

    public override string ToString()
    {
        return AccessibleName;
    }
}
