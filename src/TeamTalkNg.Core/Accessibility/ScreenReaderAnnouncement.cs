namespace TeamTalkNg.Core.Accessibility;

public sealed record ScreenReaderAnnouncement(
    string Text,
    AnnouncementPriority Priority = AnnouncementPriority.Normal,
    bool Interrupt = false,
    bool IncludeBraille = true);
