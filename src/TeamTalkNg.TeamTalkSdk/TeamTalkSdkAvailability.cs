namespace TeamTalkNg.TeamTalkSdk;

public sealed record TeamTalkSdkAvailability(bool IsAvailable, string? NativeLibraryPath, string? Reason)
{
    public static TeamTalkSdkAvailability Available(string nativeLibraryPath)
    {
        return new TeamTalkSdkAvailability(true, nativeLibraryPath, null);
    }

    public static TeamTalkSdkAvailability Unavailable(string reason)
    {
        return new TeamTalkSdkAvailability(false, null, reason);
    }
}
