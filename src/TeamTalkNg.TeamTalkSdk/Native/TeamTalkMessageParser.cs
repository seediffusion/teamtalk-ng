using System.Runtime.InteropServices;

namespace TeamTalkNg.TeamTalkSdk.Native;

internal static class TeamTalkMessageParser
{
    public static TeamTalkMessage Parse(IntPtr buffer)
    {
        ClientEvent clientEvent = (ClientEvent)Marshal.ReadInt32(buffer, 0);
        int source = Marshal.ReadInt32(buffer, 4);
        TTType type = (TTType)Marshal.ReadInt32(buffer, 8);
        IntPtr payload = IntPtr.Add(buffer, NativeConstants.MessageHeaderSize);

        NativeUser user = default;
        NativeTextMessage textMessage = default;
        NativeClientErrorMsg clientError = default;
        NativeChannel channel = default;
        NativeFileTransfer fileTransfer = default;
        int boolValue = 0;
        int intValue = 0;

        switch (type)
        {
            case TTType.User:
                user = Marshal.PtrToStructure<NativeUser>(payload);
                break;
            case TTType.TextMessage:
                textMessage = Marshal.PtrToStructure<NativeTextMessage>(payload);
                break;
            case TTType.ClientErrorMsg:
                clientError = Marshal.PtrToStructure<NativeClientErrorMsg>(payload);
                break;
            case TTType.Channel:
                channel = Marshal.PtrToStructure<NativeChannel>(payload);
                break;
            case TTType.FileTransfer:
                fileTransfer = Marshal.PtrToStructure<NativeFileTransfer>(payload);
                break;
            case TTType.TTBool:
                boolValue = Marshal.ReadInt32(payload);
                break;
            case TTType.Int32:
                intValue = Marshal.ReadInt32(payload);
                break;
        }

        return new TeamTalkMessage(clientEvent, source, type, user, textMessage, clientError, channel, boolValue, intValue, fileTransfer);
    }
}
