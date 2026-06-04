namespace TeamTalkNg.Core.TeamTalk;

public sealed record VideoCaptureDeviceSummary(
    string DeviceId,
    string Name,
    string CaptureApi,
    IReadOnlyList<VideoCaptureFormatSummary> Formats)
{
    public string DisplayName => string.IsNullOrWhiteSpace(CaptureApi)
        ? Name
        : $"{Name} ({CaptureApi})";

    public override string ToString()
    {
        return DisplayName;
    }
}
