namespace TeamTalkNg.Core.TeamTalk;

public sealed record MediaFrameSummary(
    int UserId,
    string DisplayName,
    MediaStreamKind Kind,
    int Width,
    int Height,
    int Stride,
    byte[] Bgra32Pixels,
    DateTimeOffset ReceivedAt);
