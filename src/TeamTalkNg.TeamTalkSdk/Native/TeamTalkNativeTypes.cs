using System.Runtime.InteropServices;

namespace TeamTalkNg.TeamTalkSdk.Native;

internal enum ClientEvent
{
    None = 0,
    ConnectionSuccess = 10,
    ConnectionCryptError = 15,
    ConnectionFailed = 20,
    ConnectionLost = 30,
    CommandError = 210,
    CommandMyselfLoggedIn = 230,
    CommandMyselfLoggedOut = 240,
    CommandMyselfKicked = 250,
    CommandUserLoggedIn = 260,
    CommandUserLoggedOut = 270,
    CommandUserUpdate = 280,
    CommandUserJoined = 290,
    CommandUserLeft = 300,
    CommandUserTextMessage = 310,
    CommandChannelNew = 320,
    CommandChannelUpdate = 330,
    CommandChannelRemove = 340,
    InternalError = 1000
}

internal enum TTType
{
    None = 0,
    Channel = 5,
    TextMessage = 14,
    TTMessage = 16,
    User = 17,
    ClientErrorMsg = 28,
    TTBool = 29,
    Int32 = 30
}

internal enum TextMsgType
{
    None = 0,
    User = 1,
    Channel = 2,
    Broadcast = 3,
    Custom = 4
}

internal enum SoundSystem
{
    None = 0,
    WinMm = 1,
    DSound = 2,
    Alsa = 3,
    CoreAudio = 4,
    Wasapi = 5,
    OpenSlesAndroid = 7,
    AudioUnit = 8,
    PulseAudio = 10
}

internal static class SoundLevel
{
    public const int VolumeMin = 0;
    public const int VolumeDefault = 1000;
    public const int VolumeMax = 32000;
    public const int GainMin = 0;
    public const int GainDefault = 1000;
    public const int GainMax = 32000;
}

internal static class StatusMode
{
    public const int Available = 0x00000000;
    public const int Away = 0x00000001;
}

[Flags]
internal enum ChannelType : uint
{
    Default = 0x0000,
    Permanent = 0x0001,
    SoloTransmit = 0x0002,
    Classroom = 0x0004,
    OperatorReceiveOnly = 0x0008,
    NoVoiceActivation = 0x0010,
    NoRecording = 0x0020,
    Hidden = 0x0040
}

internal enum Codec
{
    NoCodec = 0,
    Speex = 1,
    SpeexVbr = 2,
    Opus = 3,
    WebMVp8 = 128
}

internal static unsafe class NativeConstants
{
    public const int StringLength = 512;
    public const int TransmitUsersMax = 128;
    public const int TransmitQueueMax = 16;
    public const int SampleRatesMax = 16;
    public const int MessageHeaderSize = 16;

    public static string ReadString(char* value)
    {
        return new string(value).TrimEnd('\0');
    }

    public static void WriteString(char* destination, string? value)
    {
        string safeValue = value ?? string.Empty;
        int length = Math.Min(safeValue.Length, StringLength - 1);

        for (int index = 0; index < length; index++)
        {
            destination[index] = safeValue[index];
        }

        destination[length] = '\0';

        for (int index = length + 1; index < StringLength; index++)
        {
            destination[index] = '\0';
        }
    }
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeSoundDevice
{
    public int DeviceId;
    public SoundSystem SoundSystem;
    public fixed char DeviceName[NativeConstants.StringLength];
    public fixed char DeviceIdentifier[NativeConstants.StringLength];
    public int WaveDeviceId;
    public int Supports3D;
    public int MaxInputChannels;
    public int MaxOutputChannels;
    public fixed int InputSampleRates[NativeConstants.SampleRatesMax];
    public fixed int OutputSampleRates[NativeConstants.SampleRatesMax];
    public int DefaultSampleRate;
    public uint SoundDeviceFeatures;

    public string ReadName()
    {
        fixed (char* value = DeviceName)
        {
            return NativeConstants.ReadString(value);
        }
    }
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeSpeexCodec
{
    public int BandMode;
    public int Quality;
    public int TransmitIntervalMilliseconds;
    public int StereoPlayback;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeSpeexVbrCodec
{
    public int BandMode;
    public int Quality;
    public int BitRate;
    public int MaxBitRate;
    public int Dtx;
    public int TransmitIntervalMilliseconds;
    public int StereoPlayback;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeOpusCodec
{
    public int SampleRate;
    public int Channels;
    public int Application;
    public int Complexity;
    public int Fec;
    public int Dtx;
    public int BitRate;
    public int Vbr;
    public int VbrConstraint;
    public int TransmitIntervalMilliseconds;
    public int FrameSizeMilliseconds;
}

[StructLayout(LayoutKind.Explicit)]
internal unsafe struct NativeAudioCodecUnion
{
    [FieldOffset(0)]
    public NativeSpeexCodec Speex;

    [FieldOffset(0)]
    public NativeSpeexVbrCodec SpeexVbr;

    [FieldOffset(0)]
    public NativeOpusCodec Opus;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeAudioCodec
{
    public Codec Codec;
    public NativeAudioCodecUnion Value;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeAudioConfig
{
    public int EnableAgc;
    public int GainLevel;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeChannel
{
    public int ParentId;
    public int ChannelId;
    public fixed char Name[NativeConstants.StringLength];
    public fixed char Topic[NativeConstants.StringLength];
    public fixed char Password[NativeConstants.StringLength];
    public int HasPassword;
    public uint ChannelType;
    public int UserData;
    public long DiskQuota;
    public fixed char OperatorPassword[NativeConstants.StringLength];
    public int MaxUsers;
    public NativeAudioCodec AudioCodec;
    public NativeAudioConfig AudioConfig;
    public fixed int TransmitUsers[NativeConstants.TransmitUsersMax * 2];
    public fixed int TransmitUsersQueue[NativeConstants.TransmitQueueMax];
    public int TransmitUsersQueueDelayMilliseconds;
    public int TimeoutTimerVoiceMilliseconds;
    public int TimeoutTimerMediaFileMilliseconds;

    public string ReadName()
    {
        fixed (char* value = Name)
        {
            return NativeConstants.ReadString(value);
        }
    }

    public string ReadTopic()
    {
        fixed (char* value = Topic)
        {
            return NativeConstants.ReadString(value);
        }
    }

    public void WriteName(string value)
    {
        fixed (char* target = Name)
        {
            NativeConstants.WriteString(target, value);
        }
    }

    public void WriteTopic(string value)
    {
        fixed (char* target = Topic)
        {
            NativeConstants.WriteString(target, value);
        }
    }

    public void WritePassword(string value)
    {
        fixed (char* target = Password)
        {
            NativeConstants.WriteString(target, value);
        }
    }
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeClientErrorMsg
{
    public int ErrorNumber;
    public fixed char ErrorMessage[NativeConstants.StringLength];

    public string ReadMessage()
    {
        fixed (char* value = ErrorMessage)
        {
            return NativeConstants.ReadString(value);
        }
    }
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeTextMessage
{
    public TextMsgType MessageType;
    public int FromUserId;
    public fixed char FromUsername[NativeConstants.StringLength];
    public int ToUserId;
    public int ChannelId;
    public fixed char Message[NativeConstants.StringLength];
    public int More;

    public string ReadFromUsername()
    {
        fixed (char* value = FromUsername)
        {
            return NativeConstants.ReadString(value);
        }
    }

    public string ReadMessage()
    {
        fixed (char* value = Message)
        {
            return NativeConstants.ReadString(value);
        }
    }

    public void WriteMessage(string value)
    {
        fixed (char* target = Message)
        {
            NativeConstants.WriteString(target, value);
        }
    }
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeUser
{
    public int UserId;
    public fixed char Username[NativeConstants.StringLength];
    public int UserData;
    public uint UserType;
    public fixed char IpAddress[NativeConstants.StringLength];
    public uint Version;
    public int ChannelId;
    public uint LocalSubscriptions;
    public uint PeerSubscriptions;
    public fixed char Nickname[NativeConstants.StringLength];
    public int StatusMode;
    public fixed char StatusMessage[NativeConstants.StringLength];
    public uint UserState;
    public fixed char MediaStorageDirectory[NativeConstants.StringLength];
    public int VolumeVoice;
    public int VolumeMediaFile;
    public int StoppedDelayVoice;
    public int StoppedDelayMediaFile;
    public fixed float SoundPositionVoice[3];
    public fixed float SoundPositionMediaFile[3];
    public fixed int StereoPlaybackVoice[2];
    public fixed int StereoPlaybackMediaFile[2];
    public int BufferMillisecondsVoice;
    public int BufferMillisecondsMediaFile;
    public int ActiveAdaptiveDelayMilliseconds;
    public fixed char ClientName[NativeConstants.StringLength];

    public string ReadUsername()
    {
        fixed (char* value = Username)
        {
            return NativeConstants.ReadString(value);
        }
    }

    public string ReadNickname()
    {
        fixed (char* value = Nickname)
        {
            return NativeConstants.ReadString(value);
        }
    }

    public string ReadStatusMessage()
    {
        fixed (char* value = StatusMessage)
        {
            return NativeConstants.ReadString(value);
        }
    }
}

internal sealed record TeamTalkMessage(
    ClientEvent ClientEvent,
    int Source,
    TTType Type,
    NativeUser User,
    NativeTextMessage TextMessage,
    NativeClientErrorMsg ClientError,
    NativeChannel Channel,
    int BoolValue,
    int IntValue);
