using TeamTalkNg.App.Services;
using TeamTalkNg.Core.TeamTalk;
using TeamTalkNg.Core.TeamTalk.ConnectionTargets;
using TeamTalkNg.TeamTalkSdk;
using TeamTalkNg.TeamTalkSdk.Native;

ParserTests.RunAll();
SdkProbeTests.RunAll();
SdkDispatchTests.RunAll();
SoundPackTests.RunAll();

internal static class ParserTests
{
    public static void RunAll()
    {
        ParsesOfficialUrlShape();
        AppliesUrlDefaults();
        ParsesKeyValueFile();
        ParsesXmlFile();
        RejectsInvalidTarget();

        Console.WriteLine("TeamTalk NG parser tests passed.");
    }

    private static void ParsesOfficialUrlShape()
    {
        bool parsed = TeamTalkConnectionTargetParser.TryParse(
            "tt://tt5us.bearware.dk?tcpport=10335&udpport=10336&encrypted=true&username=guest&password=secret&channel=Lobby&chanpasswd=room",
            out TeamTalkServerProfile profile,
            out string error);

        Assert(parsed, error);
        AssertEqual("tt5us.bearware.dk", profile.Host);
        AssertEqual(10335, profile.TcpPort);
        AssertEqual(10336, profile.UdpPort);
        Assert(profile.IsEncrypted, "Expected encrypted URL flag.");
        AssertEqual("guest", profile.Username);
        AssertEqual("secret", profile.Password);
        AssertEqual("/Lobby", profile.ChannelPath);
        AssertEqual("room", profile.ChannelPassword);
    }

    private static void AppliesUrlDefaults()
    {
        bool parsed = TeamTalkConnectionTargetParser.TryParseUri("tt://example.org", out TeamTalkServerProfile profile, out string error);

        Assert(parsed, error);
        AssertEqual(10333, profile.TcpPort);
        AssertEqual(10333, profile.UdpPort);
        AssertEqual("/", profile.ChannelPath);
    }

    private static void ParsesKeyValueFile()
    {
        string path = WriteTempFile(
            "hostaddr=server.example.org",
            "tcpport=10443",
            "udpport=10444",
            "username=alex",
            "nickname=Alex",
            "channel=/Meetings",
            "chanpassword=door");

        bool parsed = TeamTalkConnectionTargetParser.TryParseFile(path, out TeamTalkServerProfile profile, out string error);

        Assert(parsed, error);
        AssertEqual("server.example.org", profile.Host);
        AssertEqual(10443, profile.TcpPort);
        AssertEqual(10444, profile.UdpPort);
        AssertEqual("alex", profile.Username);
        AssertEqual("Alex", profile.Nickname);
        AssertEqual("/Meetings", profile.ChannelPath);
        AssertEqual("door", profile.ChannelPassword);
    }

    private static void ParsesXmlFile()
    {
        string path = WriteTempFile(
            "<teamtalk>",
            "  <hostaddr>xml.example.org</hostaddr>",
            "  <tcpport>12000</tcpport>",
            "  <udpport>12001</udpport>",
            "  <encrypted>true</encrypted>",
            "  <channel>Training</channel>",
            "</teamtalk>");

        bool parsed = TeamTalkConnectionTargetParser.TryParseFile(path, out TeamTalkServerProfile profile, out string error);

        Assert(parsed, error);
        AssertEqual("xml.example.org", profile.Host);
        AssertEqual(12000, profile.TcpPort);
        AssertEqual(12001, profile.UdpPort);
        Assert(profile.IsEncrypted, "Expected encrypted XML flag.");
        AssertEqual("/Training", profile.ChannelPath);
    }

    private static void RejectsInvalidTarget()
    {
        bool parsed = TeamTalkConnectionTargetParser.TryParse("not a target", out _, out string error);

        Assert(!parsed, "Expected invalid target to fail.");
        Assert(!string.IsNullOrWhiteSpace(error), "Expected parser to explain failure.");
    }

    private static string WriteTempFile(params string[] lines)
    {
        string path = Path.Combine(Path.GetTempPath(), $"teamtalk-ng-test-{Guid.NewGuid():N}.tt");
        File.WriteAllLines(path, lines);
        return path;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void AssertEqual<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected {expected}, got {actual}.");
        }
    }
}

internal static class SdkProbeTests
{
    public static void RunAll()
    {
        ReportsMissingConfiguredLibrary();
        VerifiesNativeSizesWhenSdkIsPresent();
        EnumeratesAudioDevicesWhenSdkIsPresent();
        Console.WriteLine("TeamTalk NG SDK probe tests passed.");
    }

    private static void ReportsMissingConfiguredLibrary()
    {
        var options = new TeamTalkSdkOptions
        {
            NativeLibraryPath = Path.Combine(Path.GetTempPath(), $"missing-teamtalk-{Guid.NewGuid():N}.dll")
        };

        TeamTalkSdkAvailability availability = TeamTalkNativeLibrary.Probe(options);

        Assert(!availability.IsAvailable, "Expected missing configured SDK library to be unavailable.");
        Assert(!string.IsNullOrWhiteSpace(availability.Reason), "Expected missing SDK library to provide a reason.");
    }

    private static void VerifiesNativeSizesWhenSdkIsPresent()
    {
        TeamTalkSdkAvailability availability = TeamTalkNativeLibrary.ConfigureResolution(new TeamTalkSdkOptions());
        if (!availability.IsAvailable)
        {
            return;
        }

        IReadOnlyList<string> mismatches = TeamTalkNativeSizeVerifier.VerifyLoadedSdkSizes();
        Assert(mismatches.Count == 0, string.Join(Environment.NewLine, mismatches));
    }

    private static void EnumeratesAudioDevicesWhenSdkIsPresent()
    {
        TeamTalkSdkAvailability availability = TeamTalkNativeLibrary.ConfigureResolution(new TeamTalkSdkOptions());
        if (!availability.IsAvailable)
        {
            return;
        }

        using var session = new TeamTalkSdkSession(new TeamTalkSdkOptions());
        IReadOnlyList<AudioDeviceSummary> devices = session.GetAudioDevicesAsync().GetAwaiter().GetResult();
        Assert(devices.All(device => !string.IsNullOrWhiteSpace(device.Name)), "Expected audio devices to have display names.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}

internal static class SoundPackTests
{
    public static void RunAll()
    {
        ResolvesOfficialDefaultSoundNames();
        DiscoversOfficialSoundPackFolders();
        TreatsOfficialDefaultPackNameAsRootSoundsFolder();
        Console.WriteLine("TeamTalk NG sound pack tests passed.");
    }

    private static void ResolvesOfficialDefaultSoundNames()
    {
        string soundsRoot = CreateSoundRoot(
            "newuser.wav",
            "removeuser.wav",
            "serverlost.wav",
            "user_msg.wav",
            "user_msg_sent.wav",
            "channel_msg.wav",
            "channel_msg_sent.wav",
            "filetx_complete.wav",
            "hotkey.wav",
            "videosession.wav",
            "desktopsession.wav",
            "vox_me_enable.wav",
            "vox_me_disable.wav");

        try
        {
            var service = new SoundEventService(soundsRoot);
            service.Configure(enabled: true, SoundEventService.DefaultSoundPackId);

            AssertEqual(Path.Combine(soundsRoot, "newuser.wav"), service.ResolveSoundPathForTest(SoundEvent.UserJoined));
            AssertEqual(Path.Combine(soundsRoot, "removeuser.wav"), service.ResolveSoundPathForTest(SoundEvent.UserLeft));
            AssertEqual(Path.Combine(soundsRoot, "serverlost.wav"), service.ResolveSoundPathForTest(SoundEvent.Disconnected));
            AssertEqual(Path.Combine(soundsRoot, "user_msg.wav"), service.ResolveSoundPathForTest(SoundEvent.DirectMessage));
            AssertEqual(Path.Combine(soundsRoot, "user_msg_sent.wav"), service.ResolveSoundPathForTest(SoundEvent.DirectMessageSent));
            AssertEqual(Path.Combine(soundsRoot, "channel_msg.wav"), service.ResolveSoundPathForTest(SoundEvent.ChannelMessage));
            AssertEqual(Path.Combine(soundsRoot, "channel_msg_sent.wav"), service.ResolveSoundPathForTest(SoundEvent.ChannelMessageSent));
            AssertEqual(Path.Combine(soundsRoot, "filetx_complete.wav"), service.ResolveSoundPathForTest(SoundEvent.FileTransferFinished));
            AssertEqual(Path.Combine(soundsRoot, "hotkey.wav"), service.ResolveSoundPathForTest(SoundEvent.PushToTalkEnabled));
            AssertEqual(Path.Combine(soundsRoot, "videosession.wav"), service.ResolveSoundPathForTest(SoundEvent.VideoStarted));
            AssertEqual(Path.Combine(soundsRoot, "desktopsession.wav"), service.ResolveSoundPathForTest(SoundEvent.DesktopShareStarted));
            AssertEqual(Path.Combine(soundsRoot, "vox_me_enable.wav"), service.ResolveSoundPathForTest(SoundEvent.VoiceActivationEnabled));
            AssertEqual(Path.Combine(soundsRoot, "vox_me_disable.wav"), service.ResolveSoundPathForTest(SoundEvent.VoiceActivationDisabled));
        }
        finally
        {
            Directory.Delete(soundsRoot, recursive: true);
        }
    }

    private static void DiscoversOfficialSoundPackFolders()
    {
        string soundsRoot = CreateSoundRoot("newuser.wav");
        string packDirectory = Path.Combine(soundsRoot, "Majorly-G");
        Directory.CreateDirectory(packDirectory);
        File.WriteAllBytes(Path.Combine(packDirectory, "newuser.wav"), []);

        try
        {
            var service = new SoundEventService(soundsRoot);
            IReadOnlyList<SoundPackOption> soundPacks = service.GetSoundPacks();

            Assert(soundPacks.Any(pack => pack.Id == SoundEventService.DefaultSoundPackId && pack.Name == SoundEventService.DefaultSoundPackName), "Expected Default sound pack.");
            Assert(soundPacks.Any(pack => pack.Id == "Majorly-G" && pack.Name == "Majorly-G"), "Expected official sound pack folder to be discovered.");

            service.Configure(enabled: true, "Majorly-G");
            AssertEqual(Path.Combine(packDirectory, "newuser.wav"), service.ResolveSoundPathForTest(SoundEvent.UserJoined));
        }
        finally
        {
            Directory.Delete(soundsRoot, recursive: true);
        }
    }

    private static void TreatsOfficialDefaultPackNameAsRootSoundsFolder()
    {
        string soundsRoot = CreateSoundRoot("channel_msg.wav");

        try
        {
            var service = new SoundEventService(soundsRoot);
            service.Configure(enabled: true, "Default");

            AssertEqual(Path.Combine(soundsRoot, "channel_msg.wav"), service.ResolveSoundPathForTest(SoundEvent.ChannelMessage));
        }
        finally
        {
            Directory.Delete(soundsRoot, recursive: true);
        }
    }

    private static string CreateSoundRoot(params string[] fileNames)
    {
        string soundsRoot = Path.Combine(Path.GetTempPath(), $"teamtalk-ng-sounds-{Guid.NewGuid():N}", "Sounds");
        Directory.CreateDirectory(soundsRoot);
        foreach (string fileName in fileNames)
        {
            File.WriteAllBytes(Path.Combine(soundsRoot, fileName), []);
        }

        return soundsRoot;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void AssertEqual<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected {expected}, got {actual}.");
        }
    }
}

internal static unsafe class SdkDispatchTests
{
    public static void RunAll()
    {
        DispatchesChannelTextMessage();
        DispatchesDirectTextMessage();
        DispatchesUserJoinedAndLeft();
        DispatchesUserUpdated();
        DispatchesChannelAddedOrUpdated();
        DispatchesChannelRemoved();
        DispatchesFileTransferUpdate();
        DispatchesServerStatisticsResponse();
        DispatchesBannedUserListResponse();
        DispatchesUserAccountListResponse();
        DispatchesConnectionLost();
        DispatchesLoggedInStatusWithoutNativeInstance();
        RejectsVoiceControlsBeforeJoiningChannel();
        RejectsStatusChangeBeforeLogin();
        RejectsNicknameChangeBeforeLogin();
        RejectsUserAudioSettingsBeforeLogin();
        RejectsServerInformationBeforeLogin();
        RejectsServerStatisticsBeforeLogin();
        RejectsBannedUsersBeforeLogin();
        RejectsUserAccountsBeforeLogin();
        RejectsServerConfigurationSaveBeforeLogin();
        RejectsChannelFilesBeforeJoiningChannel();
        RejectsFileCommandsBeforeJoiningChannel();
        RejectsCancelFileTransferBeforeConnection();
        RejectsChannelTopicChangeBeforeLogin();
        RejectsUserModerationBeforeLogin();
        StoresAudioVolumeBeforeNativeInstanceExists();
        Console.WriteLine("TeamTalk NG SDK dispatch tests passed.");
    }

    private static void DispatchesChannelTextMessage()
    {
        using var session = new TeamTalkSdkSession(new TeamTalkSdkOptions());
        ChatMessage? received = null;
        session.ChannelMessageReceived += (_, message) => received = message;

        NativeTextMessage textMessage = default;
        textMessage.MessageType = TextMsgType.Channel;
        textMessage.FromUserId = 7;
        WriteString(textMessage.FromUsername, "alex");
        textMessage.ChannelId = 1;
        textMessage.WriteMessage("hello from sdk");

        session.DispatchMessageForTest(new TeamTalkMessage(
            ClientEvent.CommandUserTextMessage,
            Source: 0,
            TTType.TextMessage,
            default,
            textMessage,
            default,
            default,
            0,
            0));

        Assert(received is not null, "Expected channel message event.");
        AssertEqual("alex", received!.Sender);
        AssertEqual("hello from sdk", received.Text);
    }

    private static void DispatchesDirectTextMessage()
    {
        using var session = new TeamTalkSdkSession(new TeamTalkSdkOptions());
        ChatMessage? received = null;
        session.ChannelMessageReceived += (_, message) => received = message;

        NativeTextMessage textMessage = default;
        textMessage.MessageType = TextMsgType.User;
        textMessage.FromUserId = 7;
        WriteString(textMessage.FromUsername, "alex");
        textMessage.WriteMessage("hello directly");

        session.DispatchMessageForTest(new TeamTalkMessage(
            ClientEvent.CommandUserTextMessage,
            Source: 0,
            TTType.TextMessage,
            default,
            textMessage,
            default,
            default,
            0,
            0));

        Assert(received is not null, "Expected direct message event.");
        Assert(received!.IsDirect, "Expected message to be marked as direct.");
        AssertEqual("Direct from alex", received.Sender);
        AssertEqual("hello directly", received.Text);
    }

    private static void DispatchesUserJoinedAndLeft()
    {
        using var session = new TeamTalkSdkSession(new TeamTalkSdkOptions());
        UserSummary? joined = null;
        UserSummary? left = null;
        session.UserJoined += (_, user) => joined = user;
        session.UserLeft += (_, user) => left = user;

        NativeUser user = CreateUser(42, "alex", "Alex", channelId: 12);

        session.DispatchMessageForTest(new TeamTalkMessage(
            ClientEvent.CommandUserJoined,
            Source: 0,
            TTType.User,
            user,
            default,
            default,
            default,
            0,
            0));
        session.DispatchMessageForTest(new TeamTalkMessage(
            ClientEvent.CommandUserLeft,
            Source: 12,
            TTType.User,
            user,
            default,
            default,
            default,
            0,
            0));

        Assert(joined is not null, "Expected user joined event.");
        Assert(left is not null, "Expected user left event.");
        AssertEqual(42, joined!.Id);
        AssertEqual("Alex", joined.Nickname);
        AssertEqual("alex", joined.Username);
        AssertEqual(42, left!.Id);
    }

    private static void DispatchesUserUpdated()
    {
        using var session = new TeamTalkSdkSession(new TeamTalkSdkOptions());
        UserSummary? updated = null;
        session.UserUpdated += (_, user) => updated = user;

        NativeUser user = CreateUser(42, "alex", "Alex", channelId: 12);
        user.UserState = 0x00000001;
        user.StatusMode = 0x00000001;
        user.VolumeVoice = 1500;
        WriteString(user.StatusMessage, "Stepped away");

        session.DispatchMessageForTest(new TeamTalkMessage(
            ClientEvent.CommandUserUpdate,
            Source: 0,
            TTType.User,
            user,
            default,
            default,
            default,
            0,
            0));

        Assert(updated is not null, "Expected user update event.");
        AssertEqual(42, updated!.Id);
        Assert(updated.IsTalking, "Expected user update to preserve talking state.");
        Assert(updated.IsAway, "Expected user update to preserve away state.");
        AssertEqual("Stepped away", updated.StatusMessage);
        AssertEqual(150, updated.VoiceVolumePercent);
        Assert(!updated.IsVoiceMuted, "Expected non-zero voice volume to be unmuted.");
    }

    private static void DispatchesConnectionLost()
    {
        using var session = new TeamTalkSdkSession(new TeamTalkSdkOptions());
        ChatMessage? systemMessage = null;
        session.ChannelMessageReceived += (_, message) => systemMessage = message;

        session.DispatchMessageForTest(new TeamTalkMessage(
            ClientEvent.ConnectionLost,
            Source: 0,
            TTType.None,
            default,
            default,
            default,
            default,
            0,
            0));

        AssertEqual(ConnectionStatus.Disconnected, session.Status);
        Assert(systemMessage is { IsSystem: true }, "Expected system message for connection lost.");
    }

    private static void DispatchesChannelAddedOrUpdated()
    {
        using var session = new TeamTalkSdkSession(new TeamTalkSdkOptions());
        ChannelSummary? received = null;
        session.ChannelAddedOrUpdated += (_, channel) => received = channel;

        NativeChannel channel = default;
        channel.ChannelId = 22;
        channel.HasPassword = 1;
        channel.ChannelType = (uint)ChannelType.Permanent;
        WriteString(channel.Name, "Lobby");
        WriteString(channel.Topic, "General conversation");

        session.DispatchMessageForTest(new TeamTalkMessage(
            ClientEvent.CommandChannelNew,
            Source: 22,
            TTType.Channel,
            default,
            default,
            default,
            channel,
            0,
            0));

        Assert(received is not null, "Expected channel event.");
        AssertEqual(22, received!.Id);
        AssertEqual("Lobby", received.Name);
        AssertEqual("General conversation", received.Topic);
        Assert(received.IsProtected, "Expected channel to preserve password-protected state.");
        Assert(received.IsPermanent, "Expected channel to preserve permanent state.");
    }

    private static void DispatchesChannelRemoved()
    {
        using var session = new TeamTalkSdkSession(new TeamTalkSdkOptions());
        int removedChannelId = 0;
        session.ChannelRemoved += (_, channelId) => removedChannelId = channelId;

        session.DispatchMessageForTest(new TeamTalkMessage(
            ClientEvent.CommandChannelRemove,
            Source: 45,
            TTType.Int32,
            default,
            default,
            default,
            default,
            0,
            45));

        AssertEqual(45, removedChannelId);
    }

    private static void DispatchesFileTransferUpdate()
    {
        using var session = new TeamTalkSdkSession(new TeamTalkSdkOptions());
        FileTransferSummary? received = null;
        session.FileTransferUpdated += (_, transfer) => received = transfer;

        NativeFileTransfer fileTransfer = default;
        fileTransfer.Status = NativeFileTransferStatus.Active;
        fileTransfer.TransferId = 12;
        fileTransfer.ChannelId = 34;
        WriteString(fileTransfer.RemoteFileName, "notes.txt");
        fileTransfer.FileSize = 1000;
        fileTransfer.Transferred = 250;
        fileTransfer.Inbound = 1;

        session.DispatchMessageForTest(new TeamTalkMessage(
            ClientEvent.FileTransfer,
            Source: 12,
            TTType.FileTransfer,
            default,
            default,
            default,
            default,
            0,
            0,
            fileTransfer));

        Assert(received is not null, "Expected file transfer update event.");
        AssertEqual(12, received!.TransferId);
        AssertEqual(34, received.ChannelId);
        AssertEqual("notes.txt", received.RemoteFileName);
        AssertEqual(1000L, received.SizeBytes);
        AssertEqual(250L, received.TransferredBytes);
        Assert(received.IsDownload, "Expected inbound transfer to be a download.");
        AssertEqual(TeamTalkFileTransferStatus.Active, received.Status);
    }

    private static void DispatchesServerStatisticsResponse()
    {
        using var session = new TeamTalkSdkSession(new TeamTalkSdkOptions());

        Task<ServerStatisticsSummary> request = session.BeginServerStatisticsRequestForTest(88);
        NativeServerStatistics statistics = default;
        statistics.TotalBytesTx = 1024;
        statistics.TotalBytesRx = 2048;
        statistics.VoiceBytesTx = 512;
        statistics.VoiceBytesRx = 256;
        statistics.VideoCaptureBytesTx = 128;
        statistics.VideoCaptureBytesRx = 64;
        statistics.MediaFileBytesTx = 32;
        statistics.MediaFileBytesRx = 16;
        statistics.DesktopBytesTx = 8;
        statistics.DesktopBytesRx = 4;
        statistics.UsersServed = 12;
        statistics.UsersPeak = 5;
        statistics.FilesTx = 4096;
        statistics.FilesRx = 8192;
        statistics.UptimeMilliseconds = 123456;

        session.DispatchMessageForTest(new TeamTalkMessage(
            ClientEvent.CommandServerStatistics,
            Source: 0,
            TTType.ServerStatistics,
            default,
            default,
            default,
            default,
            0,
            0,
            ServerStatistics: statistics));

        ServerStatisticsSummary summary = request.GetAwaiter().GetResult();
        AssertEqual(1024L, summary.TotalBytesSent);
        AssertEqual(2048L, summary.TotalBytesReceived);
        AssertEqual(12, summary.UsersServed);
        AssertEqual(5, summary.PeakUsers);
        AssertEqual(4096L, summary.FileBytesSent);
        AssertEqual(8192L, summary.FileBytesReceived);
        AssertEqual(123456L, summary.UptimeMilliseconds);
    }

    private static unsafe void DispatchesBannedUserListResponse()
    {
        using var session = new TeamTalkSdkSession(new TeamTalkSdkOptions());

        Task<IReadOnlyList<BannedUserSummary>> request = session.BeginBannedUsersRequestForTest(91);
        NativeBannedUser bannedUser = default;
        WriteString(bannedUser.IpAddress, "192.0.2.*");
        WriteString(bannedUser.ChannelPath, "/Lobby");
        WriteString(bannedUser.BanTime, "2026-06-04");
        WriteString(bannedUser.Nickname, "Bad Guest");
        WriteString(bannedUser.Username, "guest");
        bannedUser.BanTypes = (uint)(BannedUserType.IpAddress | BannedUserType.Channel);
        WriteString(bannedUser.Owner, "admin");

        session.DispatchMessageForTest(new TeamTalkMessage(
            ClientEvent.CommandBannedUser,
            Source: 0,
            TTType.BannedUser,
            default,
            default,
            default,
            default,
            0,
            0,
            BannedUser: bannedUser));
        session.DispatchMessageForTest(new TeamTalkMessage(
            ClientEvent.CommandProcessing,
            Source: 91,
            TTType.TTBool,
            default,
            default,
            default,
            default,
            0,
            0));

        IReadOnlyList<BannedUserSummary> bannedUsers = request.GetAwaiter().GetResult();
        AssertEqual(1, bannedUsers.Count);
        AssertEqual("192.0.2.*", bannedUsers[0].IpAddress);
        AssertEqual("/Lobby", bannedUsers[0].ChannelPath);
        AssertEqual("guest", bannedUsers[0].Username);
        Assert((bannedUsers[0].BanTypes & BannedUserType.IpAddress) != 0, "Expected IP address ban type.");
        Assert((bannedUsers[0].BanTypes & BannedUserType.Channel) != 0, "Expected channel ban type.");
    }

    private static unsafe void DispatchesUserAccountListResponse()
    {
        using var session = new TeamTalkSdkSession(new TeamTalkSdkOptions());

        Task<IReadOnlyList<UserAccountSummary>> request = session.BeginUserAccountsRequestForTest(92);
        NativeUserAccount account = default;
        WriteString(account.Username, "alex");
        account.UserType = (uint)UserAccountType.Default;
        account.UserRights = (uint)(UserAccountRights.ViewAllUsers | UserAccountRights.TransmitVoice | UserAccountRights.SendChannelMessages);
        account.UserData = 42;
        WriteString(account.Note, "Test account");
        WriteString(account.InitialChannel, "/Lobby");
        account.AudioCodecBitrateLimit = 64000;
        WriteString(account.LastModified, "2026-06-04");
        WriteString(account.LastLoginTime, "2026-06-03");

        session.DispatchMessageForTest(new TeamTalkMessage(
            ClientEvent.CommandUserAccount,
            Source: 0,
            TTType.UserAccount,
            default,
            default,
            default,
            default,
            0,
            0,
            UserAccount: account));
        session.DispatchMessageForTest(new TeamTalkMessage(
            ClientEvent.CommandProcessing,
            Source: 92,
            TTType.TTBool,
            default,
            default,
            default,
            default,
            0,
            0));

        IReadOnlyList<UserAccountSummary> accounts = request.GetAwaiter().GetResult();
        AssertEqual(1, accounts.Count);
        AssertEqual("alex", accounts[0].Username);
        AssertEqual(UserAccountType.Default, accounts[0].Type);
        Assert((accounts[0].Rights & UserAccountRights.TransmitVoice) != 0, "Expected transmit voice right.");
        AssertEqual(42, accounts[0].UserData);
        AssertEqual("Test account", accounts[0].Note);
        AssertEqual("/Lobby", accounts[0].InitialChannel);
        AssertEqual(64000, accounts[0].AudioCodecBitrateLimit);
        AssertEqual("2026-06-03", accounts[0].LastLoginTime);
    }

    private static void DispatchesLoggedInStatusWithoutNativeInstance()
    {
        using var session = new TeamTalkSdkSession(new TeamTalkSdkOptions());

        session.DispatchMessageForTest(new TeamTalkMessage(
            ClientEvent.CommandMyselfLoggedIn,
            Source: 99,
            TTType.None,
            default,
            default,
            default,
            default,
            0,
            0));

        AssertEqual(ConnectionStatus.LoggedIn, session.Status);
    }

    private static void RejectsVoiceControlsBeforeJoiningChannel()
    {
        using var session = new TeamTalkSdkSession(new TeamTalkSdkOptions());

        AssertThrows(() => session.SetVoiceTransmissionAsync(true).GetAwaiter().GetResult());
        AssertThrows(() => session.SetVoiceActivationAsync(true).GetAwaiter().GetResult());
    }

    private static void RejectsStatusChangeBeforeLogin()
    {
        using var session = new TeamTalkSdkSession(new TeamTalkSdkOptions());

        AssertThrows(() => session.SetUserStatusAsync(new UserStatusRequest(IsAway: true, "Away")).GetAwaiter().GetResult());
    }

    private static void RejectsNicknameChangeBeforeLogin()
    {
        using var session = new TeamTalkSdkSession(new TeamTalkSdkOptions());

        AssertThrows(() => session.SetNicknameAsync("Alex").GetAwaiter().GetResult());
    }

    private static void RejectsUserAudioSettingsBeforeLogin()
    {
        using var session = new TeamTalkSdkSession(new TeamTalkSdkOptions());

        AssertThrows(() => session.SetUserAudioSettingsAsync(new UserAudioSettingsRequest(42, 100, IsVoiceMuted: false)).GetAwaiter().GetResult());
    }

    private static void RejectsServerInformationBeforeLogin()
    {
        using var session = new TeamTalkSdkSession(new TeamTalkSdkOptions());

        AssertThrows(() => session.GetServerInformationAsync().GetAwaiter().GetResult());
    }

    private static void RejectsServerStatisticsBeforeLogin()
    {
        using var session = new TeamTalkSdkSession(new TeamTalkSdkOptions());

        AssertThrows(() => session.GetServerStatisticsAsync().GetAwaiter().GetResult());
    }

    private static void RejectsBannedUsersBeforeLogin()
    {
        using var session = new TeamTalkSdkSession(new TeamTalkSdkOptions());

        var bannedUser = new BannedUserSummary("192.0.2.*", string.Empty, string.Empty, string.Empty, string.Empty, BannedUserType.IpAddress, string.Empty);
        AssertThrows(() => session.GetBannedUsersAsync().GetAwaiter().GetResult());
        AssertThrows(() => session.UnbanUserAsync(bannedUser).GetAwaiter().GetResult());
    }

    private static void RejectsUserAccountsBeforeLogin()
    {
        using var session = new TeamTalkSdkSession(new TeamTalkSdkOptions());

        var account = new UserAccountCreationRequest(
            "alex",
            "secret",
            UserAccountType.Default,
            UserAccountRights.TransmitVoice,
            string.Empty,
            string.Empty);
        AssertThrows(() => session.GetUserAccountsAsync().GetAwaiter().GetResult());
        AssertThrows(() => session.CreateUserAccountAsync(account).GetAwaiter().GetResult());
        AssertThrows(() => session.DeleteUserAccountAsync("alex").GetAwaiter().GetResult());
    }

    private static void RejectsServerConfigurationSaveBeforeLogin()
    {
        using var session = new TeamTalkSdkSession(new TeamTalkSdkOptions());

        AssertThrows(() => session.SaveServerConfigurationAsync().GetAwaiter().GetResult());
    }

    private static void RejectsChannelFilesBeforeJoiningChannel()
    {
        using var session = new TeamTalkSdkSession(new TeamTalkSdkOptions());

        AssertThrows(() => session.GetChannelFilesAsync().GetAwaiter().GetResult());
    }

    private static void RejectsFileCommandsBeforeJoiningChannel()
    {
        using var session = new TeamTalkSdkSession(new TeamTalkSdkOptions());

        AssertThrows(() => session.UploadFileAsync("upload.txt").GetAwaiter().GetResult());
        AssertThrows(() => session.DownloadFileAsync(1, "download.txt").GetAwaiter().GetResult());
        AssertThrows(() => session.DeleteFileAsync(1).GetAwaiter().GetResult());
    }

    private static void RejectsCancelFileTransferBeforeConnection()
    {
        using var session = new TeamTalkSdkSession(new TeamTalkSdkOptions());

        AssertThrows(() => session.CancelFileTransferAsync(1).GetAwaiter().GetResult());
    }

    private static void RejectsChannelTopicChangeBeforeLogin()
    {
        using var session = new TeamTalkSdkSession(new TeamTalkSdkOptions());

        AssertThrows(() => session.SetChannelTopicAsync("/Lobby", "New topic").GetAwaiter().GetResult());
    }

    private static void RejectsUserModerationBeforeLogin()
    {
        using var session = new TeamTalkSdkSession(new TeamTalkSdkOptions());

        AssertThrows(() => session.MoveUserAsync(42, "/Lobby").GetAwaiter().GetResult());
        AssertThrows(() => session.KickUserAsync(42, "/Lobby").GetAwaiter().GetResult());
        AssertThrows(() => session.BanUserAsync(42, "/Lobby", fromServer: true).GetAwaiter().GetResult());
    }

    private static void StoresAudioVolumeBeforeNativeInstanceExists()
    {
        using var session = new TeamTalkSdkSession(new TeamTalkSdkOptions());

        session.SetAudioVolumeAsync(inputVolumePercent: 0, outputVolumePercent: 100).GetAwaiter().GetResult();
    }

    private static NativeUser CreateUser(int id, string username, string nickname, int channelId)
    {
        NativeUser user = default;
        user.UserId = id;
        user.ChannelId = channelId;
        WriteString(user.Username, username);
        WriteString(user.Nickname, nickname);
        return user;
    }

    private static void WriteString(char* target, string value)
    {
        NativeConstants.WriteString(target, value);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void AssertEqual<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected {expected}, got {actual}.");
        }
    }

    private static void AssertThrows(Action action)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException)
        {
            return;
        }

        throw new InvalidOperationException("Expected InvalidOperationException.");
    }
}
