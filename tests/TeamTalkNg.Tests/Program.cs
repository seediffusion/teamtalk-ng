using TeamTalkNg.Core.TeamTalk;
using TeamTalkNg.Core.TeamTalk.ConnectionTargets;
using TeamTalkNg.TeamTalkSdk;
using TeamTalkNg.TeamTalkSdk.Native;

ParserTests.RunAll();
SdkProbeTests.RunAll();
SdkDispatchTests.RunAll();

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

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}

internal static unsafe class SdkDispatchTests
{
    public static void RunAll()
    {
        DispatchesChannelTextMessage();
        DispatchesUserJoinedAndLeft();
        DispatchesChannelAddedOrUpdated();
        DispatchesChannelRemoved();
        DispatchesConnectionLost();
        DispatchesLoggedInStatusWithoutNativeInstance();
        RejectsVoiceControlsBeforeJoiningChannel();
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
        WriteString(channel.Name, "Lobby");

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
