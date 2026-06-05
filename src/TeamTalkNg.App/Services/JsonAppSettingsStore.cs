using System.IO;
using System.Text.Json;

namespace TeamTalkNg.App.Services;

public sealed class JsonAppSettingsStore : IAppSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string settingsPath;

    public JsonAppSettingsStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TeamTalk NG",
            "settings.json"))
    {
    }

    public JsonAppSettingsStore(string settingsPath)
    {
        this.settingsPath = settingsPath;
    }

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(settingsPath))
        {
            return new AppSettings();
        }

        try
        {
            string json = await File.ReadAllTextAsync(settingsPath, cancellationToken).ConfigureAwait(false);
            using JsonDocument document = JsonDocument.Parse(json);
            bool hasSettingsVersion = document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.EnumerateObject().Any(property =>
                    string.Equals(property.Name, nameof(AppSettings.SettingsVersion), StringComparison.OrdinalIgnoreCase));

            AppSettings settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions)
                ?? new AppSettings();
            return NormalizeSettings(settings, hasSettingsVersion);
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        await using FileStream stream = File.Create(settingsPath);
        await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, cancellationToken).ConfigureAwait(false);
    }

    private static AppSettings NormalizeSettings(AppSettings settings, bool hasSettingsVersion)
    {
        if (hasSettingsVersion && settings.SettingsVersion >= AppSettings.CurrentSettingsVersion)
        {
            return settings;
        }

        bool oldAggressiveProcessingDefaults = settings.EnableNoiseSuppression
            && settings.EnableEchoCancellation
            && !settings.EnableAutomaticGainControl;

        return settings with
        {
            SettingsVersion = AppSettings.CurrentSettingsVersion,
            VoiceActivationLevel = settings.VoiceActivationLevel == 50
                ? 2
                : Math.Clamp(settings.VoiceActivationLevel, 0, 100),
            EnableNoiseSuppression = oldAggressiveProcessingDefaults ? false : settings.EnableNoiseSuppression,
            EnableEchoCancellation = oldAggressiveProcessingDefaults ? false : settings.EnableEchoCancellation
        };
    }
}
