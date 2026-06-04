using System.Threading.Channels;
using TeamTalkNg.Core.Accessibility;

namespace TeamTalkNg.Accessibility;

public sealed class QueuedAnnouncementService : IAnnouncementService
{
    private readonly IScreenReaderOutput output;
    private readonly Channel<ScreenReaderAnnouncement> channel;
    private readonly CancellationTokenSource shutdown = new();
    private readonly Task worker;

    public QueuedAnnouncementService(IScreenReaderOutput output)
    {
        this.output = output;
        channel = Channel.CreateUnbounded<ScreenReaderAnnouncement>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
        worker = Task.Run(ProcessAnnouncementsAsync);
    }

    public event EventHandler<ScreenReaderAnnouncement>? AnnouncementRaised;

    public async ValueTask AnnounceAsync(ScreenReaderAnnouncement announcement, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(announcement.Text))
        {
            return;
        }

        await channel.Writer.WriteAsync(announcement, cancellationToken);
    }

    private async Task ProcessAnnouncementsAsync()
    {
        await foreach (ScreenReaderAnnouncement announcement in channel.Reader.ReadAllAsync(shutdown.Token))
        {
            bool interrupt = announcement.Interrupt
                || (announcement.AllowPriorityInterrupt && announcement.Priority is AnnouncementPriority.High or AnnouncementPriority.Critical);

            try
            {
                if (announcement.IncludeBraille)
                {
                    output.Output(announcement.Text, interrupt);
                }
                else
                {
                    output.Speak(announcement.Text, interrupt);
                }
            }
            catch (Exception)
            {
                // A failing screen reader backend should not stop later announcements.
            }

            AnnouncementRaised?.Invoke(this, announcement);

            if (announcement.Priority == AnnouncementPriority.Low)
            {
                await Task.Delay(150, shutdown.Token);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        channel.Writer.TryComplete();
        await shutdown.CancelAsync();

        try
        {
            await worker;
        }
        catch (OperationCanceledException)
        {
        }

        output.Dispose();
        shutdown.Dispose();
    }
}
