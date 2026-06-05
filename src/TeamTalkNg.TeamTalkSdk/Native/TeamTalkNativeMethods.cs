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

    [DllImport(LibraryName, EntryPoint = "TT_GetMessage")]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static extern int GetMessage(IntPtr instance, IntPtr message, ref int waitMilliseconds);

    [DllImport(LibraryName, EntryPoint = "TT_GetDefaultSoundDevices")]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static extern int GetDefaultSoundDevices(out int inputDeviceId, out int outputDeviceId);

    [DllImport(LibraryName, EntryPoint = "TT_GetSoundDevices")]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static extern int GetSoundDevices(IntPtr soundDevices, ref int deviceCount);

    [DllImport(LibraryName, EntryPoint = "TT_RestartSoundSystem")]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static extern int RestartSoundSystem();

    [DllImport(LibraryName, EntryPoint = "TT_InitSoundInputDevice")]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static extern int InitSoundInputDevice(IntPtr instance, int inputDeviceId);

    [DllImport(LibraryName, EntryPoint = "TT_InitSoundOutputDevice")]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static extern int InitSoundOutputDevice(IntPtr instance, int outputDeviceId);

    [DllImport(LibraryName, EntryPoint = "TT_InitSoundDuplexDevices")]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static extern int InitSoundDuplexDevices(IntPtr instance, int inputDeviceId, int outputDeviceId);

    [DllImport(LibraryName, EntryPoint = "TT_CloseSoundInputDevice")]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static extern int CloseSoundInputDevice(IntPtr instance);

    [DllImport(LibraryName, EntryPoint = "TT_CloseSoundOutputDevice")]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static extern int CloseSoundOutputDevice(IntPtr instance);

    [DllImport(LibraryName, EntryPoint = "TT_CloseSoundDuplexDevices")]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static extern int CloseSoundDuplexDevices(IntPtr instance);

    [DllImport(LibraryName, EntryPoint = "TT_SetSoundDeviceEffects")]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static extern int SetSoundDeviceEffects(IntPtr instance, ref NativeSoundDeviceEffects effects);

    [DllImport(LibraryName, EntryPoint = "TT_SetSoundInputPreprocessEx")]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static extern int SetSoundInputPreprocess(IntPtr instance, ref NativeAudioPreprocessor preprocessor);

    [DllImport(LibraryName, EntryPoint = "TT_EnableVoiceTransmission")]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static extern int EnableVoiceTransmission(IntPtr instance, [MarshalAs(UnmanagedType.I4)] int enabled);

    [DllImport(LibraryName, EntryPoint = "TT_EnableVoiceActivation")]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static extern int EnableVoiceActivation(IntPtr instance, [MarshalAs(UnmanagedType.I4)] int enabled);

    [DllImport(LibraryName, EntryPoint = "TT_SetVoiceActivationLevel")]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static extern int SetVoiceActivationLevel(IntPtr instance, int level);

    [DllImport(LibraryName, EntryPoint = "TT_GetVoiceActivationLevel")]
    internal static extern int GetVoiceActivationLevel(IntPtr instance);

    [DllImport(LibraryName, EntryPoint = "TT_GetSoundInputLevel")]
    internal static extern int GetSoundInputLevel(IntPtr instance);

    [DllImport(LibraryName, EntryPoint = "TT_GetVideoCaptureDevices")]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static extern int GetVideoCaptureDevices(IntPtr videoDevices, ref int deviceCount);

    [DllImport(LibraryName, EntryPoint = "TT_InitVideoCaptureDevice", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static extern int InitVideoCaptureDevice(IntPtr instance, string deviceId, ref NativeVideoFormat videoFormat);

    [DllImport(LibraryName, EntryPoint = "TT_CloseVideoCaptureDevice")]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static extern int CloseVideoCaptureDevice(IntPtr instance);

    [DllImport(LibraryName, EntryPoint = "TT_StartVideoCaptureTransmission")]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static extern int StartVideoCaptureTransmission(IntPtr instance, ref NativeVideoCodec videoCodec);

    [DllImport(LibraryName, EntryPoint = "TT_StopVideoCaptureTransmission")]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static extern int StopVideoCaptureTransmission(IntPtr instance);

    [DllImport(LibraryName, EntryPoint = "TT_Windows_GetDesktopHWND")]
    internal static extern IntPtr WindowsGetDesktopHwnd();

    [DllImport(LibraryName, EntryPoint = "TT_Windows_GetDesktopActiveHWND")]
    internal static extern IntPtr WindowsGetDesktopActiveHwnd();

    [DllImport(LibraryName, EntryPoint = "TT_SendDesktopWindowFromHWND")]
    internal static extern int SendDesktopWindowFromHwnd(IntPtr instance, IntPtr windowHandle, BitmapFormat bitmapFormat, DesktopProtocol desktopProtocol);

    [DllImport(LibraryName, EntryPoint = "TT_CloseDesktopWindow")]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static extern int CloseDesktopWindow(IntPtr instance);

    [DllImport(LibraryName, EntryPoint = "TT_SetSoundInputGainLevel")]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static extern int SetSoundInputGainLevel(IntPtr instance, int level);

    [DllImport(LibraryName, EntryPoint = "TT_SetSoundOutputVolume")]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static extern int SetSoundOutputVolume(IntPtr instance, int volume);

    [DllImport(LibraryName, EntryPoint = "TT_SetUserVolume")]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static extern int SetUserVolume(IntPtr instance, int userId, StreamType streamType, int volume);

    [DllImport(LibraryName, EntryPoint = "TT_SetUserMute")]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static extern int SetUserMute(IntPtr instance, int userId, StreamType streamType, [MarshalAs(UnmanagedType.I4)] int muted);

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

    [DllImport(LibraryName, EntryPoint = "TT_Disconnect")]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static extern int Disconnect(IntPtr instance);

    [DllImport(LibraryName, EntryPoint = "TT_DoLoginEx", CharSet = CharSet.Unicode)]
    internal static extern int DoLoginEx(
        IntPtr instance,
        string nickname,
        string username,
        string password,
        string clientName);

    [DllImport(LibraryName, EntryPoint = "TT_DoChangeStatus", CharSet = CharSet.Unicode)]
    internal static extern int DoChangeStatus(IntPtr instance, int statusMode, string statusMessage);

    [DllImport(LibraryName, EntryPoint = "TT_DoChangeNickname", CharSet = CharSet.Unicode)]
    internal static extern int DoChangeNickname(IntPtr instance, string nickname);

    [DllImport(LibraryName, EntryPoint = "TT_DoSubscribe")]
    internal static extern int DoSubscribe(IntPtr instance, int userId, Subscription subscriptions);

    [DllImport(LibraryName, EntryPoint = "TT_DoUnsubscribe")]
    internal static extern int DoUnsubscribe(IntPtr instance, int userId, Subscription subscriptions);

    [DllImport(LibraryName, EntryPoint = "TT_AcquireUserVideoCaptureFrame")]
    internal static extern IntPtr AcquireUserVideoCaptureFrame(IntPtr instance, int userId);

    [DllImport(LibraryName, EntryPoint = "TT_ReleaseUserVideoCaptureFrame")]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static extern int ReleaseUserVideoCaptureFrame(IntPtr instance, IntPtr videoFrame);

    [DllImport(LibraryName, EntryPoint = "TT_AcquireUserMediaVideoFrame")]
    internal static extern IntPtr AcquireUserMediaVideoFrame(IntPtr instance, int userId);

    [DllImport(LibraryName, EntryPoint = "TT_ReleaseUserMediaVideoFrame")]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static extern int ReleaseUserMediaVideoFrame(IntPtr instance, IntPtr videoFrame);

    [DllImport(LibraryName, EntryPoint = "TT_AcquireUserDesktopWindowEx")]
    internal static extern IntPtr AcquireUserDesktopWindowEx(IntPtr instance, int userId, BitmapFormat bitmapFormat);

    [DllImport(LibraryName, EntryPoint = "TT_ReleaseUserDesktopWindow")]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static extern int ReleaseUserDesktopWindow(IntPtr instance, IntPtr desktopWindow);

    [DllImport(LibraryName, EntryPoint = "TT_GetServerProperties")]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static extern int GetServerProperties(IntPtr instance, out NativeServerProperties serverProperties);

    [DllImport(LibraryName, EntryPoint = "TT_DoSaveConfig")]
    internal static extern int DoSaveConfig(IntPtr instance);

    [DllImport(LibraryName, EntryPoint = "TT_DoQueryServerStats")]
    internal static extern int DoQueryServerStats(IntPtr instance);

    [DllImport(LibraryName, EntryPoint = "TT_DoListBans")]
    internal static extern int DoListBans(IntPtr instance, int channelId, int index, int count);

    [DllImport(LibraryName, EntryPoint = "TT_DoUnBanUserEx")]
    internal static extern int DoUnBanUserEx(IntPtr instance, ref NativeBannedUser bannedUser);

    [DllImport(LibraryName, EntryPoint = "TT_DoListUserAccounts")]
    internal static extern int DoListUserAccounts(IntPtr instance, int index, int count);

    [DllImport(LibraryName, EntryPoint = "TT_DoNewUserAccount")]
    internal static extern int DoNewUserAccount(IntPtr instance, ref NativeUserAccount userAccount);

    [DllImport(LibraryName, EntryPoint = "TT_DoDeleteUserAccount", CharSet = CharSet.Unicode)]
    internal static extern int DoDeleteUserAccount(IntPtr instance, string username);

    [DllImport(LibraryName, EntryPoint = "TT_GetChannelFiles")]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static extern int GetChannelFiles(IntPtr instance, int channelId, IntPtr remoteFiles, ref int fileCount);

    [DllImport(LibraryName, EntryPoint = "TT_DoJoinChannelByID", CharSet = CharSet.Unicode)]
    internal static extern int DoJoinChannelById(IntPtr instance, int channelId, string password);

    [DllImport(LibraryName, EntryPoint = "TT_DoMakeChannel")]
    internal static extern int DoMakeChannel(IntPtr instance, ref NativeChannel channel);

    [DllImport(LibraryName, EntryPoint = "TT_DoUpdateChannel")]
    internal static extern int DoUpdateChannel(IntPtr instance, ref NativeChannel channel);

    [DllImport(LibraryName, EntryPoint = "TT_DoRemoveChannel")]
    internal static extern int DoRemoveChannel(IntPtr instance, int channelId);

    [DllImport(LibraryName, EntryPoint = "TT_DoTextMessage")]
    internal static extern int DoTextMessage(IntPtr instance, ref NativeTextMessage textMessage);

    [DllImport(LibraryName, EntryPoint = "TT_DoSendFile", CharSet = CharSet.Unicode)]
    internal static extern int DoSendFile(IntPtr instance, int channelId, string localFilePath);

    [DllImport(LibraryName, EntryPoint = "TT_DoRecvFile", CharSet = CharSet.Unicode)]
    internal static extern int DoRecvFile(IntPtr instance, int channelId, int fileId, string localFilePath);

    [DllImport(LibraryName, EntryPoint = "TT_DoDeleteFile")]
    internal static extern int DoDeleteFile(IntPtr instance, int channelId, int fileId);

    [DllImport(LibraryName, EntryPoint = "TT_CancelFileTransfer")]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static extern int CancelFileTransfer(IntPtr instance, int transferId);

    [DllImport(LibraryName, EntryPoint = "TT_DoKickUser")]
    internal static extern int DoKickUser(IntPtr instance, int userId, int channelId);

    [DllImport(LibraryName, EntryPoint = "TT_DoBanUser")]
    internal static extern int DoBanUser(IntPtr instance, int userId, int channelId);

    [DllImport(LibraryName, EntryPoint = "TT_DoMoveUser")]
    internal static extern int DoMoveUser(IntPtr instance, int userId, int channelId);

    [DllImport(LibraryName, EntryPoint = "TT_GetChannelIDFromPath", CharSet = CharSet.Unicode)]
    internal static extern int GetChannelIdFromPath(IntPtr instance, string channelPath);

    [DllImport(LibraryName, EntryPoint = "TT_GetChannelPath", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static extern int GetChannelPath(IntPtr instance, int channelId, [Out] char[] channelPath);

    [DllImport(LibraryName, EntryPoint = "TT_GetMyChannelID")]
    internal static extern int GetMyChannelId(IntPtr instance);

    [DllImport(LibraryName, EntryPoint = "TT_GetRootChannelID")]
    internal static extern int GetRootChannelId(IntPtr instance);

    [DllImport(LibraryName, EntryPoint = "TT_GetChannel")]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static extern int GetChannel(IntPtr instance, int channelId, out NativeChannel channel);

    [DllImport(LibraryName, EntryPoint = "TT_DBG_SIZEOF")]
    internal static extern int DebugSizeOf(TTType type);
}
