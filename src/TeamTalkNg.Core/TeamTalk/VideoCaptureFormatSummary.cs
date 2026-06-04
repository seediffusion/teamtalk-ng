namespace TeamTalkNg.Core.TeamTalk;

public sealed record VideoCaptureFormatSummary(
    int Width,
    int Height,
    int FpsNumerator,
    int FpsDenominator,
    string PixelFormat)
{
    public string DisplayName
    {
        get
        {
            double framesPerSecond = FpsDenominator <= 0 ? 0 : (double)FpsNumerator / FpsDenominator;
            string frameRate = framesPerSecond > 0 ? $"{framesPerSecond:0.##} frames per second" : "unknown frame rate";
            return $"{Width} by {Height}, {frameRate}, {PixelFormat}";
        }
    }
}
