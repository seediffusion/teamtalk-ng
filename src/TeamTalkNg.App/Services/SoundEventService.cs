using System.IO;
using System.Media;

namespace TeamTalkNg.App.Services;

public sealed class SoundEventService : ISoundEventService
{
    public const string DefaultSoundPackId = "";
    public const string DefaultSoundPackName = "Default";

    private static readonly Dictionary<SoundEvent, string[]> EventAliases = new()
    {
        [SoundEvent.Connecting] = ["connecting", "connect"],
        [SoundEvent.Connected] = ["connected", "loggedin", "login", "serverconnected"],
        [SoundEvent.Disconnected] = ["disconnected", "disconnect", "loggedout", "serverdisconnected"],
        [SoundEvent.JoinedChannel] = ["joinedchannel", "channeljoined", "joinchannel", "joined"],
        [SoundEvent.ChannelMessage] = ["channelmessage", "message", "newmessage", "msg"],
        [SoundEvent.DirectMessage] = ["directmessage", "privatemessage", "usermessage", "dm"],
        [SoundEvent.UserJoined] = ["userjoined", "userjoin", "joinedchanneluser", "newuser"],
        [SoundEvent.UserLeft] = ["userleft", "userleave", "leftchanneluser"],
        [SoundEvent.FileTransferStarted] = ["filetransferstarted", "filestarted", "transferstarted"],
        [SoundEvent.FileTransferFinished] = ["filetransferfinished", "filetransfercomplete", "filefinished", "transfercomplete"],
        [SoundEvent.FileTransferFailed] = ["filetransferfailed", "filetransfererror", "filefailed", "transferfailed"],
        [SoundEvent.FileTransferCanceled] = ["filetransfercanceled", "filetransfercancelled", "filecanceled", "filecancelled"],
        [SoundEvent.PushToTalkEnabled] = ["pushtotalkenabled", "voicetxenabled", "transmitenabled", "txon"],
        [SoundEvent.PushToTalkDisabled] = ["pushtotalkdisabled", "voicetxdisabled", "transmitdisabled", "txoff"],
        [SoundEvent.VoiceActivationEnabled] = ["voiceactivationenabled", "voiceactenabled", "voxenabled"],
        [SoundEvent.VoiceActivationDisabled] = ["voiceactivationdisabled", "voiceactdisabled", "voxdisabled"],
        [SoundEvent.VideoStarted] = ["videostarted", "videotransmissionstarted", "videoon"],
        [SoundEvent.VideoStopped] = ["videostopped", "videotransmissionstopped", "videooff"],
        [SoundEvent.DesktopShareStarted] = ["desktopsharestarted", "desktopstarted", "desktopon"],
        [SoundEvent.DesktopShareStopped] = ["desktopsharestopped", "desktopstopped", "desktopoff"]
    };

    private readonly string soundsRoot;
    private bool enabled;
    private string selectedSoundPack = DefaultSoundPackId;

    public SoundEventService()
        : this(Path.Combine(AppContext.BaseDirectory, "Sounds"))
    {
    }

    public SoundEventService(string soundsRoot)
    {
        this.soundsRoot = soundsRoot;
    }

    public IReadOnlyList<SoundPackOption> GetSoundPacks()
    {
        List<SoundPackOption> soundPacks = [new(DefaultSoundPackId, DefaultSoundPackName)];
        if (!Directory.Exists(soundsRoot))
        {
            return soundPacks;
        }

        foreach (string directory in Directory.EnumerateDirectories(soundsRoot).OrderBy(directory => Path.GetFileName(directory) ?? string.Empty, StringComparer.CurrentCultureIgnoreCase))
        {
            string name = Path.GetFileName(directory) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(name) && Directory.EnumerateFiles(directory, "*.wav").Any())
            {
                soundPacks.Add(new SoundPackOption(name, name));
            }
        }

        return soundPacks;
    }

    public void Configure(bool enabled, string soundPack)
    {
        this.enabled = enabled;
        selectedSoundPack = soundPack ?? DefaultSoundPackId;
    }

    public void Play(SoundEvent soundEvent)
    {
        if (!enabled)
        {
            return;
        }

        string? soundPath = ResolveSoundPath(soundEvent);
        if (soundPath is null)
        {
            return;
        }

        try
        {
            _ = Task.Run(() =>
            {
                try
                {
                    using var player = new SoundPlayer(soundPath);
                    player.PlaySync();
                }
                catch
                {
                }
            });
        }
        catch
        {
            // Sound packs are optional user content. Bad WAV files should never interrupt the client.
        }
    }

    private string? ResolveSoundPath(SoundEvent soundEvent)
    {
        string? selectedPath = string.IsNullOrWhiteSpace(selectedSoundPack)
            ? null
            : ResolveSoundPathInDirectory(Path.Combine(soundsRoot, selectedSoundPack), soundEvent);
        return selectedPath ?? ResolveSoundPathInDirectory(soundsRoot, soundEvent);
    }

    private static string? ResolveSoundPathInDirectory(string directory, SoundEvent soundEvent)
    {
        if (!Directory.Exists(directory) || !EventAliases.TryGetValue(soundEvent, out string[]? aliases))
        {
            return null;
        }

        HashSet<string> normalizedAliases = aliases
            .Append(soundEvent.ToString())
            .Select(NormalizeSoundName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (string file in Directory.EnumerateFiles(directory, "*.wav"))
        {
            string stem = Path.GetFileNameWithoutExtension(file) ?? string.Empty;
            if (normalizedAliases.Contains(NormalizeSoundName(stem)))
            {
                return file;
            }
        }

        return null;
    }

    private static string NormalizeSoundName(string value)
    {
        return new string(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
    }
}
