using System.Reflection;
using System.Runtime.InteropServices;

namespace TeamTalkNg.TeamTalkSdk.Native;

public static class TeamTalkNativeLibrary
{
    private const string LibraryFileName = "TeamTalk5.dll";
    private static readonly Lock ResolverLock = new();
    private static bool resolverRegistered;
    private static string? resolvedLibraryPath;

    public static TeamTalkSdkAvailability Probe(TeamTalkSdkOptions options)
    {
        string? configuredPath = options.NativeLibraryPath;
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return File.Exists(configuredPath)
                ? TeamTalkSdkAvailability.Available(configuredPath)
                : TeamTalkSdkAvailability.Unavailable($"Configured TeamTalk SDK library was not found: {configuredPath}");
        }

        string[] candidatePaths =
        [
            Path.Combine(AppContext.BaseDirectory, LibraryFileName),
            Path.Combine(AppContext.BaseDirectory, "sdk", LibraryFileName),
            Path.Combine(Environment.CurrentDirectory, LibraryFileName)
        ];

        string? foundPath = candidatePaths.FirstOrDefault(File.Exists);
        return foundPath is not null
            ? TeamTalkSdkAvailability.Available(foundPath)
            : TeamTalkSdkAvailability.Unavailable($"{LibraryFileName} was not found beside the app or in the local sdk folder.");
    }

    public static TeamTalkSdkAvailability ConfigureResolution(TeamTalkSdkOptions options)
    {
        TeamTalkSdkAvailability availability = Probe(options);
        if (!availability.IsAvailable || availability.NativeLibraryPath is null)
        {
            return availability;
        }

        lock (ResolverLock)
        {
            resolvedLibraryPath = availability.NativeLibraryPath;
            if (!resolverRegistered)
            {
                NativeLibrary.SetDllImportResolver(
                    typeof(TeamTalkNativeMethods).Assembly,
                    ResolveNativeLibrary);
                resolverRegistered = true;
            }
        }

        return availability;
    }

    private static IntPtr ResolveNativeLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (!string.Equals(libraryName, "TeamTalk5", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(resolvedLibraryPath))
        {
            return IntPtr.Zero;
        }

        return NativeLibrary.Load(resolvedLibraryPath);
    }
}
