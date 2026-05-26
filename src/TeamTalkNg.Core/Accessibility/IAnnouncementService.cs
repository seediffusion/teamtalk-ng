namespace TeamTalkNg.Core.Accessibility;

public interface IAnnouncementService : IAsyncDisposable
{
    event EventHandler<ScreenReaderAnnouncement>? AnnouncementRaised;

    ValueTask AnnounceAsync(ScreenReaderAnnouncement announcement, CancellationToken cancellationToken = default);
}
