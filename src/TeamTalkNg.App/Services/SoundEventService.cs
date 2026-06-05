using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Media;

namespace TeamTalkNg.App.Services;

public sealed class SoundEventService : ISoundEventService
{
    public const string DefaultSoundPackId = "";
    public const string DefaultSoundPackName = "Default";

    private static readonly SoundEventDefinition[] EventDefinitions =
    [
        new(SoundEvent.Connecting, nameof(SoundEvent.Connecting), "Connecting to server", "connecting.wav"),
        new(SoundEvent.Connected, nameof(SoundEvent.Connected), "Logged in to server", "logged_on.wav"),
        new(SoundEvent.Disconnected, nameof(SoundEvent.Disconnected), "Server lost", "serverlost.wav"),
        new(SoundEvent.UserLoggedIn, nameof(SoundEvent.UserLoggedIn), "User logged in", "logged_on.wav"),
        new(SoundEvent.UserLoggedOut, nameof(SoundEvent.UserLoggedOut), "User logged out", "logged_off.wav"),
        new(SoundEvent.JoinedChannel, nameof(SoundEvent.JoinedChannel), "Joined channel", "joinedchannel.wav"),
        new(SoundEvent.ChannelMessage, nameof(SoundEvent.ChannelMessage), "New channel message", "channel_msg.wav"),
        new(SoundEvent.ChannelMessageSent, nameof(SoundEvent.ChannelMessageSent), "Channel message sent", "channel_msg_sent.wav"),
        new(SoundEvent.BroadcastMessage, nameof(SoundEvent.BroadcastMessage), "Broadcast message received", "broadcast_msg.wav"),
        new(SoundEvent.DirectMessage, nameof(SoundEvent.DirectMessage), "New direct message", "user_msg.wav"),
        new(SoundEvent.DirectMessageSent, nameof(SoundEvent.DirectMessageSent), "Direct message sent", "user_msg_sent.wav"),
        new(SoundEvent.UserTypingDirectMessage, nameof(SoundEvent.UserTypingDirectMessage), "User typing direct message", "typing.wav"),
        new(SoundEvent.UserJoined, nameof(SoundEvent.UserJoined), "New user", "newuser.wav"),
        new(SoundEvent.UserLeft, nameof(SoundEvent.UserLeft), "User removed", "removeuser.wav"),
        new(SoundEvent.ChannelSilent, nameof(SoundEvent.ChannelSilent), "Channel silent", string.Empty),
        new(SoundEvent.FileTransferStarted, nameof(SoundEvent.FileTransferStarted), "File transfer started", "fileupdate.wav"),
        new(SoundEvent.FileTransferFinished, nameof(SoundEvent.FileTransferFinished), "File transfer complete", "filetx_complete.wav"),
        new(SoundEvent.FileTransferFailed, nameof(SoundEvent.FileTransferFailed), "File transfer failed", "filetx_complete.wav"),
        new(SoundEvent.FileTransferCanceled, nameof(SoundEvent.FileTransferCanceled), "File transfer canceled", "filetx_complete.wav"),
        new(SoundEvent.FilesUpdated, nameof(SoundEvent.FilesUpdated), "Files updated", "fileupdate.wav"),
        new(SoundEvent.QuestionModeEnabled, nameof(SoundEvent.QuestionModeEnabled), "User enabled question mode", "questionmode.wav"),
        new(SoundEvent.PushToTalkEnabled, nameof(SoundEvent.PushToTalkEnabled), "Hotkey pressed", "hotkey.wav"),
        new(SoundEvent.PushToTalkDisabled, nameof(SoundEvent.PushToTalkDisabled), "Hotkey released", "hotkey.wav"),
        new(SoundEvent.RemoteVoiceActivationEnabled, nameof(SoundEvent.RemoteVoiceActivationEnabled), "Voice activation enabled", "vox_enable.wav"),
        new(SoundEvent.RemoteVoiceActivationDisabled, nameof(SoundEvent.RemoteVoiceActivationDisabled), "Voice activation disabled", "vox_disable.wav"),
        new(SoundEvent.VoiceActivationEnabled, nameof(SoundEvent.VoiceActivationEnabled), "Voice activation enabled via Me menu", "vox_me_enable.wav"),
        new(SoundEvent.VoiceActivationDisabled, nameof(SoundEvent.VoiceActivationDisabled), "Voice activation disabled via Me menu", "vox_me_disable.wav"),
        new(SoundEvent.VoiceActivationTriggered, nameof(SoundEvent.VoiceActivationTriggered), "Voice activation triggered", "voiceact_on.wav"),
        new(SoundEvent.VoiceActivationStopped, nameof(SoundEvent.VoiceActivationStopped), "Voice activation stopped", "voiceact_off.wav"),
        new(SoundEvent.MasterVolumeMuted, nameof(SoundEvent.MasterVolumeMuted), "Mute master volume", "mute_all.wav"),
        new(SoundEvent.MasterVolumeUnmuted, nameof(SoundEvent.MasterVolumeUnmuted), "Unmute master volume", "unmute_all.wav"),
        new(SoundEvent.TransmitQueueReady, nameof(SoundEvent.TransmitQueueReady), "Transmit ready in no-interruption channel", "txqueue_start.wav"),
        new(SoundEvent.TransmitQueueStopped, nameof(SoundEvent.TransmitQueueStopped), "Transmit stopped in no-interruption channel", "txqueue_stop.wav"),
        new(SoundEvent.VideoStarted, nameof(SoundEvent.VideoStarted), "New video session", "videosession.wav"),
        new(SoundEvent.VideoStopped, nameof(SoundEvent.VideoStopped), "Video session stopped", "videosession.wav"),
        new(SoundEvent.DesktopShareStarted, nameof(SoundEvent.DesktopShareStarted), "New desktop session", "desktopsession.wav"),
        new(SoundEvent.DesktopShareStopped, nameof(SoundEvent.DesktopShareStopped), "Desktop session stopped", "desktopsession.wav"),
        new(SoundEvent.DesktopAccessRequested, nameof(SoundEvent.DesktopAccessRequested), "Desktop access request", "desktopaccessreq.wav"),
        new(SoundEvent.InterceptionStarted, nameof(SoundEvent.InterceptionStarted), "Interception by another user", "intercept.wav"),
        new(SoundEvent.InterceptionEnded, nameof(SoundEvent.InterceptionEnded), "End of interception by another user", "interceptEnd.wav")
    ];

    private static readonly Dictionary<SoundEvent, string[]> OfficialEventFileNames = new()
    {
        [SoundEvent.Connecting] = ["connecting", "connect"],
        [SoundEvent.Connected] = ["logged_on", "connected", "loggedin", "login", "serverconnected"],
        [SoundEvent.Disconnected] = ["serverlost", "disconnected", "disconnect", "serverdisconnected", "logged_off"],
        [SoundEvent.UserLoggedIn] = ["logged_on", "userloggedin", "userlogin"],
        [SoundEvent.UserLoggedOut] = ["logged_off", "userloggedout", "userlogout"],
        [SoundEvent.JoinedChannel] = ["joinedchannel", "channeljoined", "joinchannel", "joined"],
        [SoundEvent.ChannelMessage] = ["channel_msg", "channelmessage", "message", "newmessage", "msg"],
        [SoundEvent.ChannelMessageSent] = ["channel_msg_sent", "channelmessagesent", "messagesent"],
        [SoundEvent.BroadcastMessage] = ["broadcast_msg", "broadcastmessage", "broadcast"],
        [SoundEvent.DirectMessage] = ["user_msg", "directmessage", "privatemessage", "usermessage", "dm"],
        [SoundEvent.DirectMessageSent] = ["user_msg_sent", "directmessagesent", "privatemessagesent", "dmsent"],
        [SoundEvent.UserTypingDirectMessage] = ["typing", "usertyping", "directmessagetyping", "privatemessagetyping"],
        [SoundEvent.UserJoined] = ["newuser", "userjoined", "userjoin", "joinedchanneluser"],
        [SoundEvent.UserLeft] = ["removeuser", "userleft", "userleave", "leftchanneluser"],
        [SoundEvent.ChannelSilent] = ["silence", "channelsilent", "silent"],
        [SoundEvent.FileTransferStarted] = ["fileupdate", "filetransferstarted", "filestarted", "transferstarted"],
        [SoundEvent.FileTransferFinished] = ["filetx_complete", "filetransferfinished", "filetransfercomplete", "filefinished", "transfercomplete"],
        [SoundEvent.FileTransferFailed] = ["filetransferfailed", "filetransfererror", "filefailed", "transferfailed"],
        [SoundEvent.FileTransferCanceled] = ["filetransfercanceled", "filetransfercancelled", "filecanceled", "filecancelled"],
        [SoundEvent.FilesUpdated] = ["fileupdate", "filesupdated", "filesupdate"],
        [SoundEvent.QuestionModeEnabled] = ["questionmode", "questionmodeenabled"],
        [SoundEvent.PushToTalkEnabled] = ["hotkey", "pushtotalkenabled", "voicetxenabled", "transmitenabled", "txon"],
        [SoundEvent.PushToTalkDisabled] = ["hotkey", "pushtotalkdisabled", "voicetxdisabled", "transmitdisabled", "txoff"],
        [SoundEvent.RemoteVoiceActivationEnabled] = ["vox_enable", "voiceactivationenabled", "voiceactenabled", "voxenabled"],
        [SoundEvent.RemoteVoiceActivationDisabled] = ["vox_disable", "voiceactivationdisabled", "voiceactdisabled", "voxdisabled"],
        [SoundEvent.VoiceActivationEnabled] = ["vox_me_enable", "vox_enable", "voiceactivationenabled", "voiceactenabled", "voxenabled"],
        [SoundEvent.VoiceActivationDisabled] = ["vox_me_disable", "vox_disable", "voiceactivationdisabled", "voiceactdisabled", "voxdisabled"],
        [SoundEvent.VoiceActivationTriggered] = ["voiceact_on", "voiceactivationtriggered", "voiceacton"],
        [SoundEvent.VoiceActivationStopped] = ["voiceact_off", "voiceactivationstopped", "voiceactoff"],
        [SoundEvent.MasterVolumeMuted] = ["mute_all", "muteall", "mastermuted"],
        [SoundEvent.MasterVolumeUnmuted] = ["unmute_all", "unmuteall", "masterunmuted"],
        [SoundEvent.TransmitQueueReady] = ["txqueue_start", "transmitqueueready", "transmitqueuestart"],
        [SoundEvent.TransmitQueueStopped] = ["txqueue_stop", "transmitqueuestopped", "transmitqueuestop"],
        [SoundEvent.VideoStarted] = ["videosession", "videostarted", "videotransmissionstarted", "videoon"],
        [SoundEvent.VideoStopped] = ["videostopped", "videotransmissionstopped", "videooff"],
        [SoundEvent.DesktopShareStarted] = ["desktopsession", "desktopsharestarted", "desktopstarted", "desktopon"],
        [SoundEvent.DesktopShareStopped] = ["desktopsharestopped", "desktopstopped", "desktopoff"],
        [SoundEvent.DesktopAccessRequested] = ["desktopaccessreq", "desktopaccessrequest"],
        [SoundEvent.InterceptionStarted] = ["intercept", "interception"],
        [SoundEvent.InterceptionEnded] = ["interceptEnd", "interceptstopped", "interceptionended"]
    };

    private readonly string soundsRoot;
    private readonly object playersLock = new();
    private readonly List<MediaPlayer> activePlayers = [];
    private bool enabled;
    private int volume = 100;
    private string selectedSoundPack = DefaultSoundPackId;
    private IReadOnlyDictionary<string, bool> eventEnabled = new Dictionary<string, bool>();

    public SoundEventService()
        : this(Path.Combine(AppContext.BaseDirectory, "Sounds"))
    {
    }

    public SoundEventService(string soundsRoot)
    {
        this.soundsRoot = soundsRoot;
    }

    public IReadOnlyList<SoundEventDefinition> GetSoundEvents()
    {
        return EventDefinitions;
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

    public string GetSoundFileName(SoundEvent soundEvent, string soundPack)
    {
        string? soundPath = ResolveSoundPath(soundPack, soundEvent);
        if (soundPath is not null)
        {
            return Path.GetFileName(soundPath) ?? string.Empty;
        }

        SoundEventDefinition? definition = EventDefinitions.FirstOrDefault(item => item.Event == soundEvent);
        return definition?.OfficialFileName ?? string.Empty;
    }

    public void Configure(bool enabled, string soundPack, int volume, IReadOnlyDictionary<string, bool> eventEnabled)
    {
        this.enabled = enabled;
        selectedSoundPack = soundPack ?? DefaultSoundPackId;
        this.volume = Math.Clamp(volume, 0, 100);
        this.eventEnabled = new Dictionary<string, bool>(eventEnabled, StringComparer.OrdinalIgnoreCase);
    }

    public void Play(SoundEvent soundEvent)
    {
        if (!enabled || !IsEventEnabled(soundEvent))
        {
            return;
        }

        string? soundPath = ResolveSoundPath(selectedSoundPack, soundEvent);
        if (soundPath is null)
        {
            return;
        }

        PlaySoundPath(soundPath, volume);
    }

    public void Preview(SoundEvent soundEvent, string soundPack, int volume)
    {
        string? soundPath = ResolveSoundPath(soundPack, soundEvent);
        if (soundPath is not null)
        {
            PlaySoundPath(soundPath, volume);
        }
    }

    private void PlaySoundPath(string soundPath, int volume)
    {
        try
        {
            if (Application.Current?.Dispatcher is { } dispatcher)
            {
                _ = dispatcher.BeginInvoke(() =>
                {
                    PlayWithMediaPlayer(soundPath, volume);
                });
            }
            else
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
        }
        catch
        {
            // Sound packs are optional user content. Bad WAV files should never interrupt the client.
        }
    }

    private void PlayWithMediaPlayer(string soundPath, int volume)
    {
        var player = new MediaPlayer
        {
            Volume = Math.Clamp(volume, 0, 100) / 100.0
        };

        EventHandler cleanup = (_, _) => ClosePlayer(player);
        EventHandler<ExceptionEventArgs> fail = (_, _) => ClosePlayer(player);
        player.MediaEnded += cleanup;
        player.MediaFailed += fail;

        lock (playersLock)
        {
            activePlayers.Add(player);
        }

        player.Open(new Uri(soundPath, UriKind.Absolute));
        player.Play();
    }

    private void ClosePlayer(MediaPlayer player)
    {
        try
        {
            player.Close();
        }
        finally
        {
            lock (playersLock)
            {
                activePlayers.Remove(player);
            }
        }
    }

    private string? ResolveSoundPath(string soundPack, SoundEvent soundEvent)
    {
        string? selectedPath = IsDefaultSoundPack(soundPack)
            ? null
            : ResolveSoundPathInDirectory(Path.Combine(soundsRoot, soundPack), soundEvent);
        return selectedPath ?? ResolveSoundPathInDirectory(soundsRoot, soundEvent);
    }

    internal string? ResolveSoundPathForTest(SoundEvent soundEvent)
    {
        return ResolveSoundPath(selectedSoundPack, soundEvent);
    }

    private bool IsEventEnabled(SoundEvent soundEvent)
    {
        string id = soundEvent.ToString();
        return !eventEnabled.TryGetValue(id, out bool enabledForEvent) || enabledForEvent;
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

        List<string> normalizedAliases = fileNames
            .Append(soundEvent.ToString())
            .Select(NormalizeSoundName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        Dictionary<string, string> filesByNormalizedStem = Directory.EnumerateFiles(directory, "*.wav")
            .GroupBy(file => NormalizeSoundName(Path.GetFileNameWithoutExtension(file) ?? string.Empty), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        foreach (string alias in normalizedAliases)
        {
            if (filesByNormalizedStem.TryGetValue(alias, out string? file))
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
