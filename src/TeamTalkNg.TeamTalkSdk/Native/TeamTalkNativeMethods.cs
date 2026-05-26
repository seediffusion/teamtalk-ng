using System.Runtime.InteropServices;

namespace TeamTalkNg.TeamTalkSdk.Native;

internal static partial class TeamTalkNativeMethods
{
    private const string LibraryName = "TeamTalk5";

    [DllImport(LibraryName, EntryPoint = "TT_InitTeamTalkPoll")]
    internal static extern IntPtr InitTeamTalkPoll();

    [DllImport(LibraryName, EntryPoint = "TT_CloseTeamTalk")]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static extern int CloseTeamTalk(IntPtr instance);

    [DllImport(LibraryName, EntryPoint = "TT_Connect", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static extern int Connect(
        IntPtr instance,
        string hostAddress,
        int tcpPort,
        int udpPort,
        int localTcpPort,
        int localUdpPort,
        [MarshalAs(UnmanagedType.I4)] int encrypted);
}
