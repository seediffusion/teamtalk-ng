namespace TeamTalkNg.Core.TeamTalk;

[Flags]
public enum UserAccountRights : uint
{
    None = 0x00000000,
    MultiLogin = 0x00000001,
    ViewAllUsers = 0x00000002,
    CreateTemporaryChannel = 0x00000004,
    ModifyChannels = 0x00000008,
    BroadcastTextMessages = 0x00000010,
    KickUsers = 0x00000020,
    BanUsers = 0x00000040,
    MoveUsers = 0x00000080,
    OperatorEnable = 0x00000100,
    UploadFiles = 0x00000200,
    DownloadFiles = 0x00000400,
    UpdateServerProperties = 0x00000800,
    TransmitVoice = 0x00001000,
    TransmitVideoCapture = 0x00002000,
    TransmitDesktop = 0x00004000,
    TransmitDesktopInput = 0x00008000,
    TransmitMediaFileAudio = 0x00010000,
    TransmitMediaFileVideo = 0x00020000,
    LockedNickname = 0x00040000,
    LockedStatus = 0x00080000,
    RecordVoice = 0x00100000,
    ViewHiddenChannels = 0x00200000,
    SendDirectMessages = 0x00400000,
    SendChannelMessages = 0x00800000
}
