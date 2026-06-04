using System.Globalization;
using System.Windows.Input;
using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.ViewModels;

public sealed class ServerStatisticsDialogViewModel
{
    public ServerStatisticsDialogViewModel(ServerStatisticsSummary statistics)
    {
        TotalSent = FormatBytes(statistics.TotalBytesSent);
        TotalReceived = FormatBytes(statistics.TotalBytesReceived);
        VoiceSent = FormatBytes(statistics.VoiceBytesSent);
        VoiceReceived = FormatBytes(statistics.VoiceBytesReceived);
        VideoSent = FormatBytes(statistics.VideoCaptureBytesSent);
        VideoReceived = FormatBytes(statistics.VideoCaptureBytesReceived);
        MediaFileSent = FormatBytes(statistics.MediaFileBytesSent);
        MediaFileReceived = FormatBytes(statistics.MediaFileBytesReceived);
        DesktopSent = FormatBytes(statistics.DesktopBytesSent);
        DesktopReceived = FormatBytes(statistics.DesktopBytesReceived);
        FileSent = FormatBytes(statistics.FileBytesSent);
        FileReceived = FormatBytes(statistics.FileBytesReceived);
        UsersServed = statistics.UsersServed.ToString(CultureInfo.CurrentCulture);
        PeakUsers = statistics.PeakUsers.ToString(CultureInfo.CurrentCulture);
        Uptime = FormatDuration(statistics.UptimeMilliseconds);
        CloseCommand = new RelayCommand(() => RequestClose?.Invoke(this, EventArgs.Empty));
    }

    public event EventHandler? RequestClose;

    public string TotalSent { get; }

    public string TotalReceived { get; }

    public string VoiceSent { get; }

    public string VoiceReceived { get; }

    public string VideoSent { get; }

    public string VideoReceived { get; }

    public string MediaFileSent { get; }

    public string MediaFileReceived { get; }

    public string DesktopSent { get; }

    public string DesktopReceived { get; }

    public string FileSent { get; }

    public string FileReceived { get; }

    public string UsersServed { get; }

    public string PeakUsers { get; }

    public string Uptime { get; }

    public ICommand CloseCommand { get; }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["bytes", "KB", "MB", "GB", "TB"];
        double value = Math.Max(0, bytes);
        int unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return unit == 0
            ? $"{value:0} {units[unit]}"
            : $"{value:0.##} {units[unit]}";
    }

    private static string FormatDuration(long milliseconds)
    {
        TimeSpan duration = TimeSpan.FromMilliseconds(Math.Max(0, milliseconds));
        if (duration.TotalDays >= 1)
        {
            return $"{(int)duration.TotalDays} days, {duration.Hours} hours, {duration.Minutes} minutes";
        }

        if (duration.TotalHours >= 1)
        {
            return $"{(int)duration.TotalHours} hours, {duration.Minutes} minutes";
        }

        if (duration.TotalMinutes >= 1)
        {
            return $"{(int)duration.TotalMinutes} minutes, {duration.Seconds} seconds";
        }

        return $"{duration.Seconds} seconds";
    }
}
