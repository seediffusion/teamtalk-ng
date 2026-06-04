namespace TeamTalkNg.Core.TeamTalk;

public sealed record UserAccountCreationRequest(
    string Username,
    string Password,
    UserAccountType Type,
    UserAccountRights Rights,
    string Note,
    string InitialChannel,
    int AudioCodecBitrateLimit = 0);
