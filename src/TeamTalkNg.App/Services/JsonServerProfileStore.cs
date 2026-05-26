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
        List<TeamTalkServerProfile>? profiles = await JsonSerializer.DeserializeAsync<List<TeamTalkServerProfile>>(stream, JsonOptions, cancellationToken);

        if (profiles is null || profiles.Count == 0)
        {
            return [CreateDefaultProfile()];
        }

        return profiles;
    }

    public async Task SaveAsync(IReadOnlyList<TeamTalkServerProfile> profiles, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(profilePath)!);

        TeamTalkServerProfile[] safeProfiles = profiles
            .Select(profile => profile with
            {
                Password = string.Empty,
                ChannelPassword = string.Empty
            })
            .ToArray();

        await using FileStream stream = File.Create(profilePath);
        await JsonSerializer.SerializeAsync(stream, safeProfiles, JsonOptions, cancellationToken);
    }

    private static TeamTalkServerProfile CreateDefaultProfile()
    {
        return new TeamTalkServerProfile
        {
            DisplayName = "TeamTalk official server",
            Host = "tt5us.bearware.dk",
            TcpPort = 10333,
            UdpPort = 10333,
            Username = "guest",
            Password = string.Empty,
            Nickname = Environment.UserName,
            ChannelPath = "/"
        };
    }
}
