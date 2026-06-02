using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.ViewModels;

public sealed record FileTransferViewModel(string Name, string Size, string Owner, string UploadedAt = "")
{
    public static FileTransferViewModel FromSummary(ChannelFileSummary file)
    {
        return new FileTransferViewModel(
            file.Name,
            FormatSize(file.SizeBytes),
            file.Owner,
            file.UploadedAt);
    }

    public string AccessibleName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Size)
                && string.IsNullOrWhiteSpace(Owner)
                && string.IsNullOrWhiteSpace(UploadedAt))
            {
                return Name;
            }

            string size = string.IsNullOrWhiteSpace(Size) ? "size unknown" : Size;
            string owner = string.IsNullOrWhiteSpace(Owner) ? "owner unknown" : Owner;
            string uploadTime = string.IsNullOrWhiteSpace(UploadedAt) ? "upload time unknown" : UploadedAt;
            return $"{Name}, {size}, {owner}, {uploadTime}";
        }
    }

    public override string ToString()
    {
        return AccessibleName;
    }

    private static string FormatSize(long sizeBytes)
    {
        if (sizeBytes < 1024)
        {
            return $"{sizeBytes} bytes";
        }

        string[] units = ["KB", "MB", "GB", "TB"];
        double value = sizeBytes;
        int unitIndex = -1;
        do
        {
            value /= 1024;
            unitIndex++;
        }
        while (value >= 1024 && unitIndex < units.Length - 1);

        return $"{value:0.#} {units[unitIndex]}";
    }
}
