namespace TeamTalkNg.Core.TeamTalk;

public sealed record ServerStatisticsSummary(
    long TotalBytesSent,
    long TotalBytesReceived,
    long VoiceBytesSent,
    long VoiceBytesReceived,
    long VideoCaptureBytesSent,
    long VideoCaptureBytesReceived,
    long MediaFileBytesSent,
    long MediaFileBytesReceived,
    long DesktopBytesSent,
    long DesktopBytesReceived,
    int UsersServed,
    int PeakUsers,
    long FileBytesSent,
    long FileBytesReceived,
    long UptimeMilliseconds);
