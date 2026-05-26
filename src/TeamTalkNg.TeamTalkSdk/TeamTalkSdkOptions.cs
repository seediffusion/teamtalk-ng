namespace TeamTalkNg.TeamTalkSdk;

public sealed record TeamTalkSdkOptions
{
    public string? NativeLibraryPath { get; init; }

    public bool UseMockWhenUnavailable { get; init; } = true;
}
