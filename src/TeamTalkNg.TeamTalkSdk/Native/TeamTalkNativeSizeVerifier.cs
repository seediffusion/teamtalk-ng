using System.Runtime.InteropServices;

namespace TeamTalkNg.TeamTalkSdk.Native;

public static class TeamTalkNativeSizeVerifier
{
    public static IReadOnlyList<string> VerifyLoadedSdkSizes()
    {
        List<string> mismatches = [];
        Compare<NativeChannel>(TTType.Channel, "Channel", mismatches);
        Compare<NativeBannedUser>(TTType.BannedUser, "BannedUser", mismatches);
        Compare<NativeRemoteFile>(TTType.RemoteFile, "RemoteFile", mismatches);
        Compare<NativeFileTransfer>(TTType.FileTransfer, "FileTransfer", mismatches);
        Compare<NativeTextMessage>(TTType.TextMessage, "TextMessage", mismatches);
        Compare<NativeUser>(TTType.User, "User", mismatches);
        Compare<NativeVideoFrame>(TTType.VideoFrame, "VideoFrame", mismatches);
        Compare<NativeUserAccount>(TTType.UserAccount, "UserAccount", mismatches);
        Compare<NativeClientErrorMsg>(TTType.ClientErrorMsg, "ClientErrorMsg", mismatches);
        Compare<NativeServerProperties>(TTType.ServerProperties, "ServerProperties", mismatches);
        Compare<NativeServerStatistics>(TTType.ServerStatistics, "ServerStatistics", mismatches);
        Compare<NativeDesktopWindow>(TTType.DesktopWindow, "DesktopWindow", mismatches);
        return mismatches;
    }

    private static void Compare<T>(TTType type, string name, List<string> mismatches)
    {
        int managedSize = Marshal.SizeOf<T>();
        int nativeSize = TeamTalkNativeMethods.DebugSizeOf(type);
        if (nativeSize != managedSize)
        {
            mismatches.Add($"{name}: managed {managedSize}, native {nativeSize}");
        }
    }
}
