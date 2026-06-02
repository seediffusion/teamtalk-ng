using System.Runtime.InteropServices;

namespace TeamTalkNg.TeamTalkSdk.Native;

public static class TeamTalkNativeSizeVerifier
{
    public static IReadOnlyList<string> VerifyLoadedSdkSizes()
    {
        List<string> mismatches = [];
        Compare<NativeChannel>(TTType.Channel, "Channel", mismatches);
        Compare<NativeTextMessage>(TTType.TextMessage, "TextMessage", mismatches);
        Compare<NativeUser>(TTType.User, "User", mismatches);
        Compare<NativeClientErrorMsg>(TTType.ClientErrorMsg, "ClientErrorMsg", mismatches);
        Compare<NativeServerProperties>(TTType.ServerProperties, "ServerProperties", mismatches);
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
