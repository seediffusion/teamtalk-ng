namespace TeamTalkNg.App.ViewModels;

public sealed record FileTransferViewModel(string Name, string Size, string Owner)
{
    public string AccessibleName
    {
        get
        {
            string size = string.IsNullOrWhiteSpace(Size) ? "size unknown" : Size;
            string owner = string.IsNullOrWhiteSpace(Owner) ? "owner unknown" : Owner;
            return $"{Name}, {size}, {owner}";
        }
    }

    public override string ToString()
    {
        return AccessibleName;
    }
}
