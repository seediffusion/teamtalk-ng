using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace TeamTalkNg.App.Services;

public static partial class AnnouncementTemplateFormatter
{
    public static readonly IReadOnlyList<AnnouncementTemplateDefinition> Definitions = new ReadOnlyCollection<AnnouncementTemplateDefinition>(
    [
        new(
            AnnouncementTemplateKind.ChannelMessage,
            "channel-message",
            "Channel message",
            "Channel message from {user}: {message}",
            "{user}, {username}, {message}, {server}"),
        new(
            AnnouncementTemplateKind.ChannelMessageSent,
            "channel-message-sent",
            "Channel message sent",
            "Channel message sent: {message}",
            "{message}, {server}"),
        new(
            AnnouncementTemplateKind.DirectMessage,
            "direct-message",
            "Direct message",
            "Direct message from {user}: {message}",
            "{user}, {username}, {message}, {server}"),
        new(
            AnnouncementTemplateKind.DirectMessageSent,
            "direct-message-sent",
            "Direct message sent",
            "Direct message sent: {message}",
            "{user}, {username}, {message}, {server}"),
        new(
            AnnouncementTemplateKind.UserJoinedChannel,
            "user-joined-channel",
            "User joined channel",
            "{user} joined channel {channel}",
            "{user}, {username}, {channel}, {server}"),
        new(
            AnnouncementTemplateKind.UserLeftChannel,
            "user-left-channel",
            "User left channel",
            "{user} left channel {channel}",
            "{user}, {username}, {channel}, {server}")
    ]);

    public static string Format(
        AppSettings settings,
        AnnouncementTemplateKind kind,
        IReadOnlyDictionary<string, string> values)
    {
        var normalizedValues = new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);
        AnnouncementTemplateDefinition definition = GetDefinition(kind);
        string template = settings.AnnouncementTemplates.TryGetValue(definition.Id, out string? customTemplate)
            && !string.IsNullOrWhiteSpace(customTemplate)
                ? customTemplate
                : definition.DefaultTemplate;

        return PlaceholderPattern().Replace(template, match =>
        {
            string placeholder = match.Groups["name"].Value;
            return normalizedValues.TryGetValue(placeholder, out string? value)
                ? value
                : match.Value;
        });
    }

    public static AnnouncementTemplateDefinition GetDefinition(AnnouncementTemplateKind kind)
    {
        return Definitions.First(definition => definition.Kind == kind);
    }

    [GeneratedRegex(@"\{(?<name>[A-Za-z0-9_]+)\}")]
    private static partial Regex PlaceholderPattern();
}
