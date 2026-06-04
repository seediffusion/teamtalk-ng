using TeamTalkNg.Core.TeamTalk;

namespace TeamTalkNg.App.ViewModels;

public sealed class AudioDeviceOptionViewModel
{
    public const int DefaultDeviceId = int.MinValue;

    public AudioDeviceOptionViewModel(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public int Id { get; }

    public string Name { get; }

    public static AudioDeviceOptionViewModel DefaultInput { get; } = new(DefaultDeviceId, "Default microphone");

    public static AudioDeviceOptionViewModel DefaultOutput { get; } = new(DefaultDeviceId, "Default speaker");

    public static AudioDeviceOptionViewModel FromInputDevice(AudioDeviceSummary device)
    {
        string suffix = device.IsDefaultInput ? " (system default)" : string.Empty;
        return new AudioDeviceOptionViewModel(device.Id, $"{device.Name}{suffix}");
    }

    public static AudioDeviceOptionViewModel FromOutputDevice(AudioDeviceSummary device)
    {
        string suffix = device.IsDefaultOutput ? " (system default)" : string.Empty;
        return new AudioDeviceOptionViewModel(device.Id, $"{device.Name}{suffix}");
    }

    public override string ToString()
    {
        return Name;
    }
}
