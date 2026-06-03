using System.IO;
using System.Text.Json;
using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.Services;

public sealed class JsonServerProfileStore : IServerProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string profilePath;

    public JsonServerProfileStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TeamTalk NG",
            "server-profiles.json"))
    {
    }

    public JsonServerProfileStore(string profilePath)
    {
        this.profilePath = profilePath;
    }

    public async Task<IReadOnlyList<TeamTalkServerProfile>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(profilePath))
        {
            return [CreateDefaultProfile()];
        }

        await using FileStream stream = File.OpenRead(profilePath);
        List<TeamTalkServerProfile>? profiles = await JsonSerializer
            .DeserializeAsync<List<TeamTalkServerProfile>>(stream, JsonOptions, cancellationToken)
            .ConfigureAwait(false);

        if (profiles is null || profiles.Count == 0)
        {
            return [CreateDefaultProfile()];
        }

        return profiles.Select(ApplyProfileDefaults).ToList();
    }

    public async Task SaveAsync(IReadOnlyList<TeamTalkServerProfile> profiles, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(profilePath)!);

        await using FileStream stream = File.Create(profilePath);
        await JsonSerializer.SerializeAsync(stream, profiles, JsonOptions, cancellationToken).ConfigureAwait(false);
    }

    private static TeamTalkServerProfile CreateDefaultProfile()
    {
        return new TeamTalkServerProfile
        {
            DisplayName = "TeamTalk official server",
            Host = "tt5us.bearware.dk",
            TcpPort = 10335,
            UdpPort = 10335,
            Username = "guest",
            Password = "guest",
            Nickname = Environment.UserName,
            ChannelPath = "/"
        };
    }

    private static TeamTalkServerProfile ApplyProfileDefaults(TeamTalkServerProfile profile)
    {
        if (!string.Equals(profile.Host, "tt5us.bearware.dk", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(profile.Username, "guest", StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrEmpty(profile.Password))
        {
            return profile;
        }

        return profile with
        {
            Password = "guest",
            TcpPort = profile.TcpPort == 10333 ? 10335 : profile.TcpPort,
            UdpPort = profile.UdpPort == 10333 ? 10335 : profile.UdpPort
        };
    }
}
