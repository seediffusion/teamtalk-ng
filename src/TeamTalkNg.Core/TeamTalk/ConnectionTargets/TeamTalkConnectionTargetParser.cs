using System.Globalization;
using System.Xml.Linq;

namespace TeamTalkNg.Core.TeamTalk.ConnectionTargets;

public static class TeamTalkConnectionTargetParser
{
    private const int DefaultPort = 10333;

    public static bool TryParse(string input, out TeamTalkServerProfile profile, out string error)
    {
        profile = new TeamTalkServerProfile();
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(input))
        {
            error = "No connection target was provided.";
            return false;
        }

        string trimmed = input.Trim();
        if (trimmed.StartsWith("tt://", StringComparison.OrdinalIgnoreCase))
        {
            return TryParseUri(trimmed, out profile, out error);
        }

        if (File.Exists(trimmed))
        {
            return TryParseFile(trimmed, out profile, out error);
        }

        error = "The connection target is not a TeamTalk URL or an existing file.";
        return false;
    }

    public static bool TryParseUri(string input, out TeamTalkServerProfile profile, out string error)
    {
        profile = new TeamTalkServerProfile();
        error = string.Empty;

        if (!Uri.TryCreate(input, UriKind.Absolute, out Uri? uri) || !string.Equals(uri.Scheme, "tt", StringComparison.OrdinalIgnoreCase))
        {
            error = "The URL is not a valid tt:// URL.";
            return false;
        }

        Dictionary<string, string> query = ParseQuery(uri.Query);
        string host = uri.Host;
        if (string.IsNullOrWhiteSpace(host))
        {
            error = "The TeamTalk URL does not contain a server address.";
            return false;
        }

        int tcpPort = ReadPort(query, "tcpport", uri.Port > 0 ? uri.Port : DefaultPort);
        int udpPort = ReadPort(query, "udpport", tcpPort);

        profile = new TeamTalkServerProfile
        {
            DisplayName = host,
            Host = host,
            TcpPort = tcpPort,
            UdpPort = udpPort,
            IsEncrypted = ReadBool(query, "encrypted"),
            Username = ReadString(query, "username"),
            Password = ReadString(query, "password"),
            Nickname = ReadString(query, "nickname"),
            ChannelPath = NormalizeChannel(ReadString(query, "channel")),
            ChannelPassword = ReadString(query, "chanpasswd")
        };
        return true;
    }

    public static bool TryParseFile(string path, out TeamTalkServerProfile profile, out string error)
    {
        profile = new TeamTalkServerProfile();
        error = string.Empty;

        try
        {
            string content = File.ReadAllText(path).Trim();
            if (content.StartsWith("tt://", StringComparison.OrdinalIgnoreCase))
            {
                return TryParseUri(content, out profile, out error);
            }

            return content.StartsWith("<", StringComparison.Ordinal)
                ? TryParseXml(content, path, out profile, out error)
                : TryParseKeyValueFile(content, path, out profile, out error);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            error = $"The TeamTalk file could not be read: {ex.Message}";
            return false;
        }
    }

    private static bool TryParseXml(string content, string path, out TeamTalkServerProfile profile, out string error)
    {
        profile = new TeamTalkServerProfile();
        error = string.Empty;

        try
        {
            XDocument document = XDocument.Parse(content);
            Dictionary<string, string> values = document
                .Descendants()
                .Where(element => !element.HasElements)
                .GroupBy(element => NormalizeKey(element.Name.LocalName))
                .ToDictionary(group => group.Key, group => group.First().Value.Trim(), StringComparer.OrdinalIgnoreCase);

            return TryBuildProfile(values, Path.GetFileNameWithoutExtension(path), out profile, out error);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.Xml.XmlException)
        {
            error = $"The TeamTalk XML file could not be parsed: {ex.Message}";
            return false;
        }
    }

    private static bool TryParseKeyValueFile(string content, string path, out TeamTalkServerProfile profile, out string error)
    {
        profile = new TeamTalkServerProfile();
        error = string.Empty;

        Dictionary<string, string> values = [];
        foreach (string line in content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("#", StringComparison.Ordinal) || trimmed.StartsWith(";", StringComparison.Ordinal))
            {
                continue;
            }

            int separator = trimmed.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            string key = NormalizeKey(trimmed[..separator]);
            string value = trimmed[(separator + 1)..].Trim();
            values[key] = value;
        }

        return TryBuildProfile(values, Path.GetFileNameWithoutExtension(path), out profile, out error);
    }

    private static bool TryBuildProfile(
        IReadOnlyDictionary<string, string> values,
        string fallbackDisplayName,
        out TeamTalkServerProfile profile,
        out string error)
    {
        profile = new TeamTalkServerProfile();
        error = string.Empty;

        string host = ReadString(values, "hostaddr", "host", "address", "server");
        if (string.IsNullOrWhiteSpace(host))
        {
            error = "The TeamTalk file does not contain a server address.";
            return false;
        }

        int tcpPort = ReadPort(values, "tcpport", DefaultPort);
        int udpPort = ReadPort(values, "udpport", tcpPort);

        profile = new TeamTalkServerProfile
        {
            DisplayName = ReadString(values, "name", "displayname", "server-name", "servername") is { Length: > 0 } name
                ? name
                : fallbackDisplayName,
            Host = host,
            TcpPort = tcpPort,
            UdpPort = udpPort,
            IsEncrypted = ReadBool(values, "encrypted"),
            Username = ReadString(values, "username"),
            Password = ReadString(values, "password"),
            Nickname = ReadString(values, "nickname", "nick"),
            ChannelPath = NormalizeChannel(ReadString(values, "channel")),
            ChannelPassword = ReadString(values, "chanpasswd", "chanpassword", "channelpassword")
        };
        return true;
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
        string trimmed = query.TrimStart('?');
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return values;
        }

        foreach (string part in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] pair = part.Split('=', 2);
            string key = Uri.UnescapeDataString(pair[0]);
            string value = pair.Length == 2 ? Uri.UnescapeDataString(pair[1].Replace("+", "%20", StringComparison.Ordinal)) : string.Empty;
            values[NormalizeKey(key)] = value;
        }

        return values;
    }

    private static string ReadString(IReadOnlyDictionary<string, string> values, params string[] keys)
    {
        foreach (string key in keys.Select(NormalizeKey))
        {
            if (values.TryGetValue(key, out string? value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }

    private static int ReadPort(IReadOnlyDictionary<string, string> values, string key, int fallback)
    {
        string value = ReadString(values, key);
        return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int port) && port is > 0 and <= 65535
            ? port
            : fallback;
    }

    private static bool ReadBool(IReadOnlyDictionary<string, string> values, string key)
    {
        string value = ReadString(values, key);
        return value.Equals("true", StringComparison.OrdinalIgnoreCase)
            || value.Equals("1", StringComparison.OrdinalIgnoreCase)
            || value.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeChannel(string channel)
    {
        if (string.IsNullOrWhiteSpace(channel))
        {
            return "/";
        }

        string trimmed = channel.Trim();
        return trimmed.StartsWith("/", StringComparison.Ordinal) ? trimmed : $"/{trimmed}";
    }

    private static string NormalizeKey(string key)
    {
        return key.Trim()
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();
    }
}
