using TeamTalkNg.Accessibility;
using TeamTalkNg.App.Services;
using TeamTalkNg.App.ViewModels;
using TeamTalkNg.Core.Accessibility;
using TeamTalkNg.Core.TeamTalk;
using TeamTalkNg.Core.TeamTalk.ConnectionTargets;
using TeamTalkNg.TeamTalkSdk;
using TeamTalkNg.TeamTalkSdk.Native;

ParserTests.RunAll();
AppSettingsTests.RunAll();
AppViewModelTests.RunAll();
SdkProbeTests.RunAll();
SdkDispatchTests.RunAll();
SoundPackTests.RunAll();
AnnouncementTemplateTests.RunAll();
AnnouncementBackendTests.RunAll();

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

internal static class AppSettingsTests
{
    public static void RunAll()
    {
        UsesOfficialStyleAudioDefaults();
        DoesNotHideDirectMessageTextByDefault();
        MigratesOldVoiceActivationDefault();
        PreservesOpenMicrophoneVoiceActivationLevel();
        DisablesAutoAwayByDefault();
        UsesAccessibleDisplayDefaults();
        UsesChatHistoryDefaults();

        Console.WriteLine("TeamTalk NG settings tests passed.");
    }

    private static void DoesNotHideDirectMessageTextByDefault()
    {
        var settings = new AppSettings();

        Assert(!settings.HideDirectMessageTextInChatHistory, "Expected direct message text to remain visible by default.");
    }

    private static void UsesOfficialStyleAudioDefaults()
    {
        var settings = new AppSettings();

        AssertEqual(AppSettings.CurrentSettingsVersion, settings.SettingsVersion);
        AssertEqual(2, settings.VoiceActivationLevel);
        Assert(!settings.EnableNoiseSuppression, "Expected noise suppression to be opt-in.");
        Assert(!settings.EnableEchoCancellation, "Expected echo cancellation to be opt-in.");
        Assert(!settings.EnableAutomaticGainControl, "Expected automatic gain control to be opt-in.");
    }

    private static void DisablesAutoAwayByDefault()
    {
        var settings = new AppSettings();

        AssertEqual(0, settings.InactivityTimeoutSeconds);
        Assert(!settings.DisableVoiceActivationDuringInactivity, "Expected inactivity voice activation disabling to be opt-in.");
        AssertEqual("Away due to inactivity", settings.InactivityStatusMessage);
    }

    private static void UsesAccessibleDisplayDefaults()
    {
        var settings = new AppSettings();

        Assert(settings.ShowVoiceActivationSlider, "Expected voice activation slider to be visible by default.");
        Assert(settings.ShowChannelUserCounts, "Expected channel user counts to be visible by default.");
        Assert(!settings.ShowUsernamesInsteadOfNicknames, "Expected nicknames to be shown by default.");
        Assert(settings.ShowChannelIcons, "Expected channel tree indicators to be visible by default.");
        Assert(!settings.ShowChannelTopicsInChannelList, "Expected channel topics to be hidden by default.");
        AssertEqual(ChannelSortMode.ServerOrder, settings.ChannelSortMode);
    }

    private static void UsesChatHistoryDefaults()
    {
        var settings = new AppSettings();

        AssertEqual(ChatHistoryViewMode.List, settings.ChatHistoryViewMode);
        AssertEqual(ChatMessageViewModel.DefaultTimestampFormat, settings.ChatTimestampFormat);
        Assert(!settings.ShowStatusEventsInChatHistory, "Expected status events to stay out of chat history by default.");
    }

    private static void MigratesOldVoiceActivationDefault()
    {
        string path = WriteTempSettings(
            "{",
            "  \"VoiceActivationLevel\": 50,",
            "  \"EnableNoiseSuppression\": true,",
            "  \"EnableEchoCancellation\": true,",
            "  \"EnableAutomaticGainControl\": false",
            "}");

        try
        {
            AppSettings settings = new JsonAppSettingsStore(path).LoadAsync().GetAwaiter().GetResult();

            AssertEqual(AppSettings.CurrentSettingsVersion, settings.SettingsVersion);
            AssertEqual(2, settings.VoiceActivationLevel);
            Assert(!settings.EnableNoiseSuppression, "Expected old aggressive noise suppression default to be migrated off.");
            Assert(!settings.EnableEchoCancellation, "Expected old aggressive echo cancellation default to be migrated off.");
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static void PreservesOpenMicrophoneVoiceActivationLevel()
    {
        string path = WriteTempSettings(
            "{",
            $"  \"SettingsVersion\": {AppSettings.CurrentSettingsVersion},",
            "  \"VoiceActivationLevel\": 0",
            "}");

        try
        {
            AppSettings settings = new JsonAppSettingsStore(path).LoadAsync().GetAwaiter().GetResult();

            AssertEqual(0, settings.VoiceActivationLevel);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string WriteTempSettings(params string[] lines)
    {
        string path = Path.Combine(Path.GetTempPath(), $"teamtalk-ng-settings-{Guid.NewGuid():N}.json");
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

internal static class AppViewModelTests
{
    public static void RunAll()
    {
        ShowsDirectMessageTextWhenPrivacyModeIsOff();
        HidesDirectMessageTextInChatHistoryWhenPrivacyModeIsOn();
        KeepsFullDirectMessageTextAvailableForThreadView();
        AppliesCustomChatTimestampFormat();
        FallsBackWhenChatTimestampFormatIsInvalid();
        ScopesLiveUserChannelEventsToExactCurrentChannel();
        FormatsInactivityStatusMessage();
        AppliesChannelTreeDisplaySettingsToVisibleAndAccessibleText();

        Console.WriteLine("TeamTalk NG app view-model tests passed.");
    }

    private static void ShowsDirectMessageTextWhenPrivacyModeIsOff()
    {
        var message = new ChatMessage(
            DateTimeOffset.Parse("2026-06-06T12:34:56+00:00"),
            "Alex",
            "Secret stream note",
            IsDirect: true,
            DirectUserId: 7);

        var viewModel = new ChatMessageViewModel(message);

        Assert(viewModel.DisplayText.Contains("Secret stream note", StringComparison.Ordinal), "Expected direct message text to be visible by default.");
        Assert(viewModel.AccessibleName.Contains("Secret stream note", StringComparison.Ordinal), "Expected accessible name to include direct message text by default.");
    }

    private static void HidesDirectMessageTextInChatHistoryWhenPrivacyModeIsOn()
    {
        var message = new ChatMessage(
            DateTimeOffset.Parse("2026-06-06T12:34:56+00:00"),
            "Alex",
            "Secret stream note",
            IsDirect: true,
            DirectUserId: 7);

        var viewModel = new ChatMessageViewModel(message, hideDirectMessageText: true);

        Assert(!viewModel.DisplayText.Contains("Secret stream note", StringComparison.Ordinal), "Expected direct message text to be hidden in chat history.");
        AssertEqual($"{viewModel.Time} Direct message from Alex. Click or press Enter to view.", viewModel.DisplayText);
        AssertEqual($"{viewModel.Time}, Direct message from Alex. Click or press Enter to view.", viewModel.AccessibleName);
    }

    private static void KeepsFullDirectMessageTextAvailableForThreadView()
    {
        var message = new ChatMessage(
            DateTimeOffset.Parse("2026-06-06T12:34:56+00:00"),
            "Alex",
            "Secret stream note",
            IsDirect: true,
            DirectUserId: 7);

        var viewModel = new ChatMessageViewModel(message, hideDirectMessageText: true);

        Assert(viewModel.FullDisplayText.Contains("Secret stream note", StringComparison.Ordinal), "Expected thread view text to keep the full direct message.");
        Assert(viewModel.FullAccessibleName.Contains("Secret stream note", StringComparison.Ordinal), "Expected thread view accessible name to keep the full direct message.");
    }

    private static void AppliesCustomChatTimestampFormat()
    {
        DateTimeOffset timestamp = DateTimeOffset.Parse("2026-06-06T12:34:56+00:00");
        var message = new ChatMessage(
            timestamp,
            "Alex",
            "Test");

        var viewModel = new ChatMessageViewModel(message, timestampFormat: "HH:mm");

        AssertEqual(timestamp.ToLocalTime().ToString("HH:mm"), viewModel.Time);
    }

    private static void FallsBackWhenChatTimestampFormatIsInvalid()
    {
        DateTimeOffset timestamp = DateTimeOffset.Parse("2026-06-06T12:34:56+00:00");
        var message = new ChatMessage(
            timestamp,
            "Alex",
            "Test");

        var viewModel = new ChatMessageViewModel(message, timestampFormat: "Q");

        AssertEqual(timestamp.ToLocalTime().ToString(ChatMessageViewModel.DefaultTimestampFormat), viewModel.Time);
    }

    private static void ScopesLiveUserChannelEventsToExactCurrentChannel()
    {
        var method = typeof(MainWindowViewModel).GetMethod(
            "IsSameChannelPathForLiveUserEvent",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Assert(method is not null, "Expected live user channel matching helper.");

        bool Matches(string? currentChannelPath, string? userChannelPath)
        {
            return (bool)method!.Invoke(null, [currentChannelPath, userChannelPath])!;
        }

        Assert(Matches("/Root", "Root"), "Expected equivalent channel paths to match.");
        Assert(Matches("/Root/Side Room", "Root/Side Room"), "Expected nested channel paths to match exactly.");
        Assert(!Matches("/Root", "/Root/Side Room"), "Expected parent and child channels to be distinct.");
        Assert(!Matches("/Root/Side Room", "/Root"), "Expected child and parent channels to be distinct.");
        Assert(Matches("/", "/"), "Expected root channel to match itself.");
        Assert(!Matches("/", "/Lobby"), "Expected root channel and Lobby to be distinct.");
    }

    private static void FormatsInactivityStatusMessage()
    {
        var method = typeof(MainWindowViewModel).GetMethod(
            "GetEffectiveInactivityStatusMessage",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Assert(method is not null, "Expected inactivity status message helper.");

        string Format(string? inactivityStatusMessage, string? currentStatusMessage)
        {
            return (string)method!.Invoke(null, [inactivityStatusMessage, currentStatusMessage])!;
        }

        AssertEqual("Stepped away", Format("  Stepped away  ", "Available"));
        AssertEqual("Available", Format("", "  Available  "));
        AssertEqual(string.Empty, Format("", ""));
    }

    private static void AppliesChannelTreeDisplaySettingsToVisibleAndAccessibleText()
    {
        var channel = new ChannelTreeItemViewModel("Lobby", ChannelTreeItemKind.Channel, path: "/Lobby")
        {
            UserCount = 3,
            Topic = "General chat"
        };

        AssertEqual("Lobby (3)", channel.DisplayText);
        Assert(channel.AccessibleName.Contains("3 users", StringComparison.Ordinal), "Expected channel user count in accessible name.");
        Assert(!channel.AccessibleName.Contains("General chat", StringComparison.Ordinal), "Expected channel topic to be hidden by default.");

        channel.ApplyDisplaySettings(showUserCounts: false, showUsernamesInsteadOfNicknames: false, showChannelIcons: false, showChannelTopics: true);

        AssertEqual("Lobby: General chat", channel.DisplayText);
        AssertEqual(string.Empty, channel.VisualIndicator);
        Assert(!channel.AccessibleName.Contains("3 users", StringComparison.Ordinal), "Expected hidden user count to be omitted from accessible name.");
        Assert(channel.AccessibleName.Contains("topic: General chat", StringComparison.Ordinal), "Expected visible topic in accessible name.");

        var user = new ChannelTreeItemViewModel("Alex", ChannelTreeItemKind.User)
        {
            Username = "alex"
        };
        user.ApplyDisplaySettings(showUserCounts: true, showUsernamesInsteadOfNicknames: true, showChannelIcons: true, showChannelTopics: false);

        AssertEqual("alex", user.DisplayText);
        Assert(user.AccessibleName.StartsWith("alex, user", StringComparison.Ordinal), "Expected username in accessible name when username display is enabled.");
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
        VerifiesAudioPreprocessorManagedSizes();
        WritesNativeTextMessageStringsAsUtf16();
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

    private static void VerifiesAudioPreprocessorManagedSizes()
    {
        AssertEqual(40, System.Runtime.InteropServices.Marshal.SizeOf<NativeSpeexDsp>());
        AssertEqual(12, System.Runtime.InteropServices.Marshal.SizeOf<NativeTeamTalkAudioPreprocessor>());
        AssertEqual(52, System.Runtime.InteropServices.Marshal.SizeOf<NativeWebRtcAudioPreprocessor>());
        AssertEqual(56, System.Runtime.InteropServices.Marshal.SizeOf<NativeAudioPreprocessor>());
    }

    private static void WritesNativeTextMessageStringsAsUtf16()
    {
        NativeTextMessage message = default;
        message.WriteMessage("For real, its ridiculous");

        int size = System.Runtime.InteropServices.Marshal.SizeOf<NativeTextMessage>();
        IntPtr buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(size);
        try
        {
            System.Runtime.InteropServices.Marshal.StructureToPtr(message, buffer, false);
            IntPtr messageOffset = IntPtr.Add(
                buffer,
                (int)System.Runtime.InteropServices.Marshal.OffsetOf<NativeTextMessage>(nameof(NativeTextMessage.Message)));
            string? marshaled = System.Runtime.InteropServices.Marshal.PtrToStringUni(messageOffset);

            AssertEqual("For real, its ridiculous", marshaled);
        }
        finally
        {
            System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer);
        }
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

    private static void AssertEqual<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected {expected}, got {actual}.");
        }
    }
}

internal static class SoundPackTests
{
    public static void RunAll()
    {
        ExposesOfficialStyleSoundEventList();
        ResolvesOfficialDefaultSoundNames();
        DiscoversOfficialSoundPackFolders();
        TreatsOfficialDefaultPackNameAsRootSoundsFolder();
        Console.WriteLine("TeamTalk NG sound pack tests passed.");
    }

    private static void ExposesOfficialStyleSoundEventList()
    {
        var service = new SoundEventService();
        IReadOnlyList<SoundEventDefinition> soundEvents = service.GetSoundEvents();

        Assert(soundEvents.Count >= 31, "Expected at least the official TeamTalk sound event set.");
        Assert(soundEvents.Any(item => item.Event == SoundEvent.BroadcastMessage), "Expected broadcast message sound event.");
        Assert(soundEvents.Any(item => item.Event == SoundEvent.UserTypingDirectMessage), "Expected direct message typing sound event.");
        Assert(soundEvents.Any(item => item.Event == SoundEvent.InterceptionStarted), "Expected interception sound event.");
    }

    private static void ResolvesOfficialDefaultSoundNames()
    {
        string soundsRoot = CreateSoundRoot(
            "newuser.wav",
            "removeuser.wav",
            "serverlost.wav",
            "logged_on.wav",
            "logged_off.wav",
            "user_msg.wav",
            "user_msg_sent.wav",
            "typing.wav",
            "channel_msg.wav",
            "channel_msg_sent.wav",
            "broadcast_msg.wav",
            "filetx_complete.wav",
            "fileupdate.wav",
            "questionmode.wav",
            "hotkey.wav",
            "videosession.wav",
            "desktopsession.wav",
            "desktopaccessreq.wav",
            "vox_enable.wav",
            "vox_disable.wav",
            "voiceact_on.wav",
            "voiceact_off.wav",
            "vox_me_enable.wav",
            "vox_me_disable.wav",
            "mute_all.wav",
            "unmute_all.wav",
            "txqueue_start.wav",
            "txqueue_stop.wav",
            "intercept.wav",
            "interceptEnd.wav");

        try
        {
            var service = new SoundEventService(soundsRoot);
            service.Configure(enabled: true, SoundEventService.DefaultSoundPackId, volume: 100, new Dictionary<string, bool>());

            AssertEqual(Path.Combine(soundsRoot, "newuser.wav"), service.ResolveSoundPathForTest(SoundEvent.UserJoined));
            AssertEqual(Path.Combine(soundsRoot, "removeuser.wav"), service.ResolveSoundPathForTest(SoundEvent.UserLeft));
            AssertEqual(Path.Combine(soundsRoot, "serverlost.wav"), service.ResolveSoundPathForTest(SoundEvent.Disconnected));
            AssertEqual(Path.Combine(soundsRoot, "logged_on.wav"), service.ResolveSoundPathForTest(SoundEvent.UserLoggedIn));
            AssertEqual(Path.Combine(soundsRoot, "logged_off.wav"), service.ResolveSoundPathForTest(SoundEvent.UserLoggedOut));
            AssertEqual(Path.Combine(soundsRoot, "user_msg.wav"), service.ResolveSoundPathForTest(SoundEvent.DirectMessage));
            AssertEqual(Path.Combine(soundsRoot, "user_msg_sent.wav"), service.ResolveSoundPathForTest(SoundEvent.DirectMessageSent));
            AssertEqual(Path.Combine(soundsRoot, "typing.wav"), service.ResolveSoundPathForTest(SoundEvent.UserTypingDirectMessage));
            AssertEqual(Path.Combine(soundsRoot, "channel_msg.wav"), service.ResolveSoundPathForTest(SoundEvent.ChannelMessage));
            AssertEqual(Path.Combine(soundsRoot, "channel_msg_sent.wav"), service.ResolveSoundPathForTest(SoundEvent.ChannelMessageSent));
            AssertEqual(Path.Combine(soundsRoot, "broadcast_msg.wav"), service.ResolveSoundPathForTest(SoundEvent.BroadcastMessage));
            AssertEqual(Path.Combine(soundsRoot, "fileupdate.wav"), service.ResolveSoundPathForTest(SoundEvent.FilesUpdated));
            AssertEqual(Path.Combine(soundsRoot, "filetx_complete.wav"), service.ResolveSoundPathForTest(SoundEvent.FileTransferFinished));
            AssertEqual(Path.Combine(soundsRoot, "questionmode.wav"), service.ResolveSoundPathForTest(SoundEvent.QuestionModeEnabled));
            AssertEqual(Path.Combine(soundsRoot, "hotkey.wav"), service.ResolveSoundPathForTest(SoundEvent.PushToTalkEnabled));
            AssertEqual(Path.Combine(soundsRoot, "videosession.wav"), service.ResolveSoundPathForTest(SoundEvent.VideoStarted));
            AssertEqual(Path.Combine(soundsRoot, "desktopsession.wav"), service.ResolveSoundPathForTest(SoundEvent.DesktopShareStarted));
            AssertEqual(Path.Combine(soundsRoot, "desktopaccessreq.wav"), service.ResolveSoundPathForTest(SoundEvent.DesktopAccessRequested));
            AssertEqual(Path.Combine(soundsRoot, "vox_enable.wav"), service.ResolveSoundPathForTest(SoundEvent.RemoteVoiceActivationEnabled));
            AssertEqual(Path.Combine(soundsRoot, "vox_disable.wav"), service.ResolveSoundPathForTest(SoundEvent.RemoteVoiceActivationDisabled));
            AssertEqual(Path.Combine(soundsRoot, "voiceact_on.wav"), service.ResolveSoundPathForTest(SoundEvent.VoiceActivationTriggered));
            AssertEqual(Path.Combine(soundsRoot, "voiceact_off.wav"), service.ResolveSoundPathForTest(SoundEvent.VoiceActivationStopped));
            AssertEqual(Path.Combine(soundsRoot, "vox_me_enable.wav"), service.ResolveSoundPathForTest(SoundEvent.VoiceActivationEnabled));
            AssertEqual(Path.Combine(soundsRoot, "vox_me_disable.wav"), service.ResolveSoundPathForTest(SoundEvent.VoiceActivationDisabled));
            AssertEqual(Path.Combine(soundsRoot, "mute_all.wav"), service.ResolveSoundPathForTest(SoundEvent.MasterVolumeMuted));
            AssertEqual(Path.Combine(soundsRoot, "unmute_all.wav"), service.ResolveSoundPathForTest(SoundEvent.MasterVolumeUnmuted));
            AssertEqual(Path.Combine(soundsRoot, "txqueue_start.wav"), service.ResolveSoundPathForTest(SoundEvent.TransmitQueueReady));
            AssertEqual(Path.Combine(soundsRoot, "txqueue_stop.wav"), service.ResolveSoundPathForTest(SoundEvent.TransmitQueueStopped));
            AssertEqual(Path.Combine(soundsRoot, "intercept.wav"), service.ResolveSoundPathForTest(SoundEvent.InterceptionStarted));
            AssertEqual(Path.Combine(soundsRoot, "interceptEnd.wav"), service.ResolveSoundPathForTest(SoundEvent.InterceptionEnded));
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

            service.Configure(enabled: true, "Majorly-G", volume: 100, new Dictionary<string, bool>());
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
            service.Configure(enabled: true, "Default", volume: 100, new Dictionary<string, bool>());

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

internal static class AnnouncementBackendTests
{
    public static void RunAll()
    {
        HonorsPriorityInterruptOptOut();
        ContinuesAfterOutputFailure();
        Console.WriteLine("TeamTalk NG announcement backend tests passed.");
    }

    private static void HonorsPriorityInterruptOptOut()
    {
        var output = new RecordingScreenReaderOutput();
        var service = new QueuedAnnouncementService(output);

        try
        {
            service.AnnounceAsync(new ScreenReaderAnnouncement(
                "important but queued",
                AnnouncementPriority.High,
                AllowPriorityInterrupt: false)).AsTask().GetAwaiter().GetResult();

            RecordingScreenReaderOutput.OutputCall call = output.WaitForCall();
            AssertEqual("important but queued", call.Message);
            Assert(!call.Interrupt, "Expected high priority announcement to respect priority interrupt opt-out.");
        }
        finally
        {
            service.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    private static void ContinuesAfterOutputFailure()
    {
        var output = new RecordingScreenReaderOutput(failFirstCall: true);
        var service = new QueuedAnnouncementService(output);

        try
        {
            service.AnnounceAsync(new ScreenReaderAnnouncement("first")).AsTask().GetAwaiter().GetResult();
            output.WaitForAttempt();

            service.AnnounceAsync(new ScreenReaderAnnouncement("second")).AsTask().GetAwaiter().GetResult();
            RecordingScreenReaderOutput.OutputCall call = output.WaitForCall();

            AssertEqual("second", call.Message);
        }
        finally
        {
            service.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
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

    private sealed class RecordingScreenReaderOutput : IScreenReaderOutput
    {
        private readonly bool failFirstCall;
        private readonly ManualResetEventSlim callRecorded = new();
        private readonly ManualResetEventSlim attemptRecorded = new();
        private int attempts;
        private OutputCall? lastCall;

        public RecordingScreenReaderOutput(bool failFirstCall = false)
        {
            this.failFirstCall = failFirstCall;
        }

        public bool IsAvailable => true;

        public void Speak(string message, bool interrupt = false)
        {
            Record(message, interrupt);
        }

        public void Braille(string message)
        {
        }

        public void Output(string message, bool interrupt = false)
        {
            Record(message, interrupt);
        }

        public OutputCall WaitForCall()
        {
            if (!callRecorded.Wait(TimeSpan.FromSeconds(3)))
            {
                throw new InvalidOperationException("Timed out waiting for announcement output.");
            }

            return lastCall ?? throw new InvalidOperationException("Announcement output was not recorded.");
        }

        public void WaitForAttempt()
        {
            if (!attemptRecorded.Wait(TimeSpan.FromSeconds(3)))
            {
                throw new InvalidOperationException("Timed out waiting for announcement output attempt.");
            }
        }

        public void Dispose()
        {
            callRecorded.Dispose();
            attemptRecorded.Dispose();
        }

        private void Record(string message, bool interrupt)
        {
            if (Interlocked.Increment(ref attempts) == 1)
            {
                attemptRecorded.Set();
                if (failFirstCall)
                {
                    throw new InvalidOperationException("Simulated output failure.");
                }
            }

            lastCall = new OutputCall(message, interrupt);
            callRecorded.Set();
        }

        public sealed record OutputCall(string Message, bool Interrupt);
    }
}

internal static class AnnouncementTemplateTests
{
    public static void RunAll()
    {
        FormatsOfficialStyleChannelMessage();
        UsesTeamTalkNgDirectMessageTerminology();
        AppliesCustomTemplateOverrides();
        LeavesUnknownPlaceholdersIntact();
        TreatsAnnouncementEventsAsEnabledByDefault();
        RespectsDisabledAnnouncementEvents();
        Console.WriteLine("TeamTalk NG announcement template tests passed.");
    }

    private static void FormatsOfficialStyleChannelMessage()
    {
        string announcement = AnnouncementTemplateFormatter.Format(
            new AppSettings(),
            AnnouncementTemplateKind.ChannelMessage,
            new Dictionary<string, string>
            {
                ["user"] = "Alexoloopios",
                ["message"] = "Test channel message"
            });

        AssertEqual("Channel message from Alexoloopios: Test channel message", announcement);
    }

    private static void UsesTeamTalkNgDirectMessageTerminology()
    {
        string announcement = AnnouncementTemplateFormatter.Format(
            new AppSettings(),
            AnnouncementTemplateKind.DirectMessage,
            new Dictionary<string, string>
            {
                ["user"] = "Alex",
                ["message"] = "Hello"
            });

        AssertEqual("Direct message from Alex: Hello", announcement);
    }

    private static void AppliesCustomTemplateOverrides()
    {
        var settings = new AppSettings
        {
            AnnouncementTemplates = new Dictionary<string, string>
            {
                ["channel-message"] = "{User} says {message}"
            }
        };

        string announcement = AnnouncementTemplateFormatter.Format(
            settings,
            AnnouncementTemplateKind.ChannelMessage,
            new Dictionary<string, string>
            {
                ["user"] = "Alex",
                ["message"] = "Hello"
            });

        AssertEqual("Alex says Hello", announcement);
    }

    private static void LeavesUnknownPlaceholdersIntact()
    {
        var settings = new AppSettings
        {
            AnnouncementTemplates = new Dictionary<string, string>
            {
                ["user-joined-channel"] = "{user} joined {channel} from {unknown}"
            }
        };

        string announcement = AnnouncementTemplateFormatter.Format(
            settings,
            AnnouncementTemplateKind.UserJoinedChannel,
            new Dictionary<string, string>
            {
                ["user"] = "Alex",
                ["channel"] = "Lobby"
            });

        AssertEqual("Alex joined Lobby from {unknown}", announcement);
    }

    private static void TreatsAnnouncementEventsAsEnabledByDefault()
    {
        Assert(AnnouncementTemplateFormatter.IsEnabled(new AppSettings(), AnnouncementTemplateKind.ChannelMessage), "Expected channel message announcements to be enabled by default.");
    }

    private static void RespectsDisabledAnnouncementEvents()
    {
        var settings = new AppSettings
        {
            AnnouncementEventEnabled = new Dictionary<string, bool>
            {
                ["direct-message-sent"] = false
            }
        };

        Assert(!AnnouncementTemplateFormatter.IsEnabled(settings, AnnouncementTemplateKind.DirectMessageSent), "Expected disabled direct message sent event.");
        Assert(AnnouncementTemplateFormatter.IsEnabled(settings, AnnouncementTemplateKind.DirectMessage), "Expected other events to remain enabled.");
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
        DispatchesBroadcastTextMessage();
        DispatchesCustomTextMessage();
        DispatchesUserJoinedAndLeft();
        DispatchesUserUpdated();
        DispatchesUserStateChange();
        DispatchesUserLoggedInAndOutMessages();
        DispatchesChannelAddedOrUpdated();
        DispatchesChannelRemoved();
        DispatchesServerUpdateMessage();
        DispatchesInternalErrorMessage();
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
        StoresAudioProcessingBeforeNativeInstanceExists();
        Console.WriteLine("TeamTalk NG SDK dispatch tests passed.");
    }

    private static void DispatchesChannelTextMessage()
    {
        using var session = new TeamTalkSdkSession(new TeamTalkSdkOptions());
        ChatMessage? received = null;
        session.ChannelMessageReceived += (_, message) => received = message;

        NativeUser user = CreateUser(7, "alex", "Alexoloopios", 1);
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
        AssertEqual("Alexoloopios", received!.Sender);
        AssertEqual("hello from sdk", received.Text);
    }

    private static void DispatchesDirectTextMessage()
    {
        using var session = new TeamTalkSdkSession(new TeamTalkSdkOptions());
        ChatMessage? received = null;
        session.ChannelMessageReceived += (_, message) => received = message;

        NativeUser user = CreateUser(7, "alex", "Alexoloopios", 1);
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
        AssertEqual("Alexoloopios", received.Sender);
        AssertEqual("hello directly", received.Text);
        AssertEqual<int?>(7, received.DirectUserId);
    }

    private static void DispatchesBroadcastTextMessage()
    {
        using var session = new TeamTalkSdkSession(new TeamTalkSdkOptions());
        ChatMessage? received = null;
        session.ChannelMessageReceived += (_, message) => received = message;

        NativeTextMessage textMessage = default;
        textMessage.MessageType = TextMsgType.Broadcast;
        textMessage.FromUserId = 7;
        WriteString(textMessage.FromUsername, "admin");
        textMessage.WriteMessage("server-wide notice");

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

        Assert(received is not null, "Expected broadcast message event.");
        Assert(received!.IsSystem, "Expected broadcast message to be marked as system text.");
        AssertEqual("Broadcast from admin", received.Sender);
        AssertEqual("server-wide notice", received.Text);
    }

    private static void DispatchesCustomTextMessage()
    {
        using var session = new TeamTalkSdkSession(new TeamTalkSdkOptions());
        ChatMessage? received = null;
        session.ChannelMessageReceived += (_, message) => received = message;

        NativeTextMessage textMessage = default;
        textMessage.MessageType = TextMsgType.Custom;
        textMessage.FromUserId = 7;
        WriteString(textMessage.FromUsername, "integration");
        textMessage.WriteMessage("custom payload");

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

        Assert(received is not null, "Expected custom message event.");
        Assert(received!.IsSystem, "Expected custom message to be marked as system text.");
        AssertEqual("Custom message from integration", received.Sender);
        AssertEqual("custom payload", received.Text);
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

    private static void DispatchesUserStateChange()
    {
        using var session = new TeamTalkSdkSession(new TeamTalkSdkOptions());
        UserSummary? updated = null;
        session.UserUpdated += (_, user) => updated = user;

        NativeUser user = CreateUser(42, "alex", "Alex", channelId: 12);
        user.UserState = 0x00000001;

        session.DispatchMessageForTest(new TeamTalkMessage(
            ClientEvent.UserStateChange,
            Source: 42,
            TTType.User,
            user,
            default,
            default,
            default,
            0,
            0));

        Assert(updated is not null, "Expected user state change to dispatch a user update.");
        AssertEqual(42, updated!.Id);
        Assert(updated.IsTalking, "Expected talking state to be mapped from user state change.");
    }

    private static void DispatchesUserLoggedInAndOutMessages()
    {
        using var session = new TeamTalkSdkSession(new TeamTalkSdkOptions());
        List<ChatMessage> received = [];
        session.ChannelMessageReceived += (_, message) => received.Add(message);

        NativeUser user = CreateUser(42, "alex", "Alex", channelId: 0);

        session.DispatchMessageForTest(new TeamTalkMessage(
            ClientEvent.CommandUserLoggedIn,
            Source: 42,
            TTType.User,
            user,
            default,
            default,
            default,
            0,
            0));
        session.DispatchMessageForTest(new TeamTalkMessage(
            ClientEvent.CommandUserLoggedOut,
            Source: 42,
            TTType.User,
            user,
            default,
            default,
            default,
            0,
            0));

        AssertEqual(2, received.Count);
        Assert(received.All(message => message.IsSystem), "Expected login and logout messages to be system text.");
        AssertEqual("Alex logged in.", received[0].Text);
        AssertEqual("Alex logged out.", received[1].Text);
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

    private static void DispatchesServerUpdateMessage()
    {
        using var session = new TeamTalkSdkSession(new TeamTalkSdkOptions());
        ChatMessage? received = null;
        session.ChannelMessageReceived += (_, message) => received = message;

        NativeServerProperties properties = default;
        WriteString(properties.ServerName, "Test Server");
        WriteString(properties.Motd, "Welcome");
        properties.MaxUsers = 100;
        properties.TcpPort = 10333;
        properties.UdpPort = 10333;

        session.DispatchMessageForTest(new TeamTalkMessage(
            ClientEvent.CommandServerUpdate,
            Source: 0,
            TTType.ServerProperties,
            default,
            default,
            default,
            default,
            0,
            0,
            ServerProperties: properties));

        Assert(received is not null, "Expected server update message.");
        Assert(received!.IsSystem, "Expected server update to be marked as system text.");
        AssertEqual("Server information updated for Test Server.", received.Text);
    }

    private static void DispatchesInternalErrorMessage()
    {
        using var session = new TeamTalkSdkSession(new TeamTalkSdkOptions());
        ChatMessage? received = null;
        session.ChannelMessageReceived += (_, message) => received = message;

        NativeClientErrorMsg error = default;
        WriteString(error.ErrorMessage, "decoder failed");

        session.DispatchMessageForTest(new TeamTalkMessage(
            ClientEvent.InternalError,
            Source: 0,
            TTType.ClientErrorMsg,
            default,
            default,
            error,
            default,
            0,
            0));

        Assert(received is not null, "Expected internal error message.");
        Assert(received!.IsSystem, "Expected internal error to be marked as system text.");
        AssertEqual("TeamTalk SDK internal error: decoder failed.", received.Text);
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

    private static void StoresAudioProcessingBeforeNativeInstanceExists()
    {
        using var session = new TeamTalkSdkSession(new TeamTalkSdkOptions());

        session.SetAudioProcessingAsync(new AudioProcessingSettings(
            EnableNoiseSuppression: true,
            EnableEchoCancellation: true,
            EnableAutomaticGainControl: true)).GetAwaiter().GetResult();
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
