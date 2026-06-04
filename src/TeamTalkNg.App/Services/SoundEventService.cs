using System.IO;
using System.Media;

namespace TeamTalkNg.App.Services;

public sealed class SoundEventService : ISoundEventService
{
    public const string DefaultSoundPackId = "";
    public const string DefaultSoundPackName = "Default";

    private static readonly Dictionary<SoundEvent, string[]> OfficialEventFileNames = new()
    {
        [SoundEvent.Connecting] = ["connecting", "connect"],
        [SoundEvent.Connected] = ["logged_on", "connected", "loggedin", "login", "serverconnected"],
        [SoundEvent.Disconnected] = ["serverlost", "disconnected", "disconnect", "serverdisconnected", "logged_off"],
        [SoundEvent.JoinedChannel] = ["joinedchannel", "channeljoined", "joinchannel", "joined"],
        [SoundEvent.ChannelMessage] = ["channel_msg", "channelmessage", "message", "newmessage", "msg"],
        [SoundEvent.ChannelMessageSent] = ["channel_msg_sent", "channelmessagesent", "messagesent"],
        [SoundEvent.DirectMessage] = ["user_msg", "directmessage", "privatemessage", "usermessage", "dm"],
        [SoundEvent.DirectMessageSent] = ["user_msg_sent", "directmessagesent", "privatemessagesent", "dmsent"],
        [SoundEvent.UserJoined] = ["newuser", "userjoined", "userjoin", "joinedchanneluser"],
        [SoundEvent.UserLeft] = ["removeuser", "userleft", "userleave", "leftchanneluser"],
        [SoundEvent.FileTransferStarted] = ["filetransferstarted", "filestarted", "transferstarted"],
        [SoundEvent.FileTransferFinished] = ["filetx_complete", "filetransferfinished", "filetransfercomplete", "filefinished", "transfercomplete"],
        [SoundEvent.FileTransferFailed] = ["filetransferfailed", "filetransfererror", "filefailed", "transferfailed"],
        [SoundEvent.FileTransferCanceled] = ["filetransfercanceled", "filetransfercancelled", "filecanceled", "filecancelled"],
        [SoundEvent.PushToTalkEnabled] = ["hotkey", "pushtotalkenabled", "voicetxenabled", "transmitenabled", "txon"],
        [SoundEvent.PushToTalkDisabled] = ["hotkey", "pushtotalkdisabled", "voicetxdisabled", "transmitdisabled", "txoff"],
        [SoundEvent.VoiceActivationEnabled] = ["vox_me_enable", "vox_enable", "voiceactivationenabled", "voiceactenabled", "voxenabled"],
        [SoundEvent.VoiceActivationDisabled] = ["vox_me_disable", "vox_disable", "voiceactivationdisabled", "voiceactdisabled", "voxdisabled"],
        [SoundEvent.VideoStarted] = ["videosession", "videostarted", "videotransmissionstarted", "videoon"],
        [SoundEvent.VideoStopped] = ["videostopped", "videotransmissionstopped", "videooff"],
        [SoundEvent.DesktopShareStarted] = ["desktopsession", "desktopsharestarted", "desktopstarted", "desktopon"],
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
        string? selectedPath = IsDefaultSoundPack(selectedSoundPack)
            ? null
            : ResolveSoundPathInDirectory(Path.Combine(soundsRoot, selectedSoundPack), soundEvent);
        return selectedPath ?? ResolveSoundPathInDirectory(soundsRoot, soundEvent);
    }

    internal string? ResolveSoundPathForTest(SoundEvent soundEvent)
    {
        return ResolveSoundPath(soundEvent);
    }

    private static bool IsDefaultSoundPack(string soundPack)
    {
        return string.IsNullOrWhiteSpace(soundPack)
            || string.Equals(soundPack, DefaultSoundPackName, StringComparison.OrdinalIgnoreCase);
    }

    private static string? ResolveSoundPathInDirectory(string directory, SoundEvent soundEvent)
    {
        if (!Directory.Exists(directory) || !OfficialEventFileNames.TryGetValue(soundEvent, out string[]? fileNames))
        {
            return null;
        }

        HashSet<string> normalizedAliases = fileNames
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
