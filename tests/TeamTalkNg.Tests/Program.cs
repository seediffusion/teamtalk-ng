using TeamTalkNg.Core.TeamTalk;
using TeamTalkNg.Core.TeamTalk.ConnectionTargets;
using TeamTalkNg.TeamTalkSdk;
using TeamTalkNg.TeamTalkSdk.Native;

ParserTests.RunAll();
SdkProbeTests.RunAll();

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

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
