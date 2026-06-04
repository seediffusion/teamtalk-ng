using TeamTalkNg.App.Services;

namespace TeamTalkNg.App.ViewModels;

public sealed record ThemeOptionViewModel(AppTheme Value, string Name)
{
    public override string ToString()
    {
        return Name;
    }
}
