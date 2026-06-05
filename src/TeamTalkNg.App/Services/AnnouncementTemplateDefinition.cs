namespace TeamTalkNg.App.Services;

public sealed record AnnouncementTemplateDefinition(
    AnnouncementTemplateKind Kind,
    string Id,
    string Name,
    string DefaultTemplate,
    string PlaceholderSummary);
