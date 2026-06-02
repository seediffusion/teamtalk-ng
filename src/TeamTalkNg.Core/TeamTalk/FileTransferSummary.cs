namespace TeamTalkNg.Core.TeamTalk;

public sealed record FileTransferSummary(
    int TransferId,
    int ChannelId,
    string LocalFilePath,
    string RemoteFileName,
    long SizeBytes,
    long TransferredBytes,
    bool IsDownload,
    TeamTalkFileTransferStatus Status);
