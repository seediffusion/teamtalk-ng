namespace TeamTalkNg.App.Services;

public sealed record SoundPackOption(string Id, string Name)
{
    public override string ToString()
    {
        return Name;
    }
}
