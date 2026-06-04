using System.IO;
using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.ViewModels;

public sealed class TransferActivityViewModel : ObservableObject
{
    private long transferredBytes;
    private TeamTalkFileTransferStatus status;

    public TransferActivityViewModel(FileTransferSummary transfer)
    {
        TransferId = transfer.TransferId;
        ChannelId = transfer.ChannelId;
        LocalFilePath = transfer.LocalFilePath;
        RemoteFileName = transfer.RemoteFileName;
        SizeBytes = transfer.SizeBytes;
        IsDownload = transfer.IsDownload;
        transferredBytes = transfer.TransferredBytes;
        status = transfer.Status;
    }

    public int TransferId { get; }

    public int ChannelId { get; }

    public string LocalFilePath { get; private set; }

    public string RemoteFileName { get; private set; }

    public long SizeBytes { get; private set; }

    public bool IsDownload { get; }

    public long TransferredBytes
    {
        get => transferredBytes;
        private set
        {
            if (SetProperty(ref transferredBytes, value))
            {
                OnProgressChanged();
            }
        }
    }

    public TeamTalkFileTransferStatus Status
    {
        get => status;
        private set
        {
            if (SetProperty(ref status, value))
            {
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(IsActive));
                OnPropertyChanged(nameof(AccessibleName));
            }
        }
    }

    public string Direction => IsDownload ? "Download" : "Upload";

    public double PercentComplete => SizeBytes <= 0
        ? 0
        : Math.Clamp(TransferredBytes * 100.0 / SizeBytes, 0, 100);

    public string ProgressText => SizeBytes <= 0
        ? $"{FormatSize(TransferredBytes)} transferred"
        : $"{PercentComplete:0}%";

    public string StatusText => Status switch
    {
        TeamTalkFileTransferStatus.Active => "Active",
        TeamTalkFileTransferStatus.Finished => "Finished",
        TeamTalkFileTransferStatus.Error => "Error",
        TeamTalkFileTransferStatus.Closed => "Canceled",
        _ => Status.ToString()
    };

    public bool IsActive => Status == TeamTalkFileTransferStatus.Active;

    public string AccessibleName => $"{Direction} {RemoteFileName}, {ProgressText}, {StatusText}";

    public override string ToString()
    {
        return AccessibleName;
    }

    public void Update(FileTransferSummary transfer)
    {
        LocalFilePath = transfer.LocalFilePath;
        RemoteFileName = string.IsNullOrWhiteSpace(transfer.RemoteFileName)
            ? Path.GetFileName(transfer.LocalFilePath)
            : transfer.RemoteFileName;
        SizeBytes = transfer.SizeBytes;
        TransferredBytes = transfer.TransferredBytes;
        Status = transfer.Status;
        OnPropertyChanged(nameof(LocalFilePath));
        OnPropertyChanged(nameof(RemoteFileName));
        OnPropertyChanged(nameof(SizeBytes));
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(PercentComplete));
        OnPropertyChanged(nameof(AccessibleName));
    }

    private void OnProgressChanged()
    {
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(PercentComplete));
        OnPropertyChanged(nameof(AccessibleName));
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
