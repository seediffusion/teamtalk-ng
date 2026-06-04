using System.Windows.Media;
using System.Windows.Media.Imaging;
using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.ViewModels;

public sealed class MediaStreamViewModel : ObservableObject
{
    private string displayName;
    private string resolution;
    private string updatedAt;
    private string accessibleName;
    private ImageSource? frame;

    public MediaStreamViewModel(MediaFrameSummary summary)
    {
        UserId = summary.UserId;
        Kind = summary.Kind;
        displayName = summary.DisplayName;
        resolution = FormatResolution(summary.Width, summary.Height);
        updatedAt = FormatUpdatedAt(summary.ReceivedAt);
        accessibleName = BuildAccessibleName();
        Update(summary);
    }

    public int UserId { get; }

    public MediaStreamKind Kind { get; }

    public string DisplayName
    {
        get => displayName;
        private set => SetProperty(ref displayName, value);
    }

    public string Resolution
    {
        get => resolution;
        private set => SetProperty(ref resolution, value);
    }

    public string UpdatedAt
    {
        get => updatedAt;
        private set => SetProperty(ref updatedAt, value);
    }

    public string AccessibleName
    {
        get => accessibleName;
        private set => SetProperty(ref accessibleName, value);
    }

    public ImageSource? Frame
    {
        get => frame;
        private set => SetProperty(ref frame, value);
    }

    public string PreviewName => $"{DisplayName} {KindLabel} preview, {Resolution}";

    private string KindLabel => Kind == MediaStreamKind.Video ? "video stream" : "desktop stream";

    public override string ToString()
    {
        return AccessibleName;
    }

    public void Update(MediaFrameSummary summary)
    {
        DisplayName = summary.DisplayName;
        Resolution = FormatResolution(summary.Width, summary.Height);
        UpdatedAt = FormatUpdatedAt(summary.ReceivedAt);

        BitmapSource bitmap = BitmapSource.Create(
            summary.Width,
            summary.Height,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            summary.Bgra32Pixels,
            summary.Stride);
        bitmap.Freeze();
        Frame = bitmap;
        AccessibleName = BuildAccessibleName();
        OnPropertyChanged(nameof(PreviewName));
    }

    private string BuildAccessibleName()
    {
        return $"{DisplayName}, {KindLabel}, {Resolution}, updated {UpdatedAt}";
    }

    private static string FormatResolution(int width, int height)
    {
        return $"{width} by {height}";
    }

    private static string FormatUpdatedAt(DateTimeOffset receivedAt)
    {
        return receivedAt.LocalDateTime.ToString("T");
    }
}
