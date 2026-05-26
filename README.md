# TeamTalk NG

TeamTalk NG is a modern Windows client for the TeamTalk 5 conferencing system. The UI is built with C#, .NET 10, and WPF.

## Current Status

This repository currently contains the first application skeleton:

- WPF main window with channels/users, chat, video, desktop, files, and volume areas.
- Accessible connection dialog with saved server profiles.
- Light and dark theme resource dictionaries.
- UI Automation live-region support for visible announcements.
- Screen-reader announcement abstraction with a Prismatoid adapter boundary.
- Preferences for theme and screen-reader announcement behavior.
- `tt://` URL and `.tt`/key-value connection target parsing for startup arguments.
- In-app Open Connection Target flow for pasted `tt://` URLs or browsed `.tt` files.
- TeamTalk SDK boundary project that probes for `TeamTalk5.dll`, resolves it from the app or `sdk` folder, and falls back to the mock session while native files are absent.
- Mock TeamTalk session service so UI and announcement flows can be exercised before the TeamTalk SDK adapter is connected.

## Accessibility Approach

TeamTalk NG uses two complementary accessibility paths:

- Standard WPF controls expose Name, Role, Value, State, keyboard focus, and control patterns through Windows UI Automation.
- Intentional TeamTalk events such as channel messages, joins/leaves, connection changes, and transmission state changes go through the announcement service and Prismatoid when available.

The announcement service falls back to debug output when Prismatoid is not present, so the app can still build and run during early development.

## Build

```powershell
dotnet build TeamTalkNg.slnx
```

## Parser Smoke Tests

```powershell
dotnet run --project tests\TeamTalkNg.Tests\TeamTalkNg.Tests.csproj
```

## Next Implementation Targets

- Replace `MockTeamTalkSession` with a real TeamTalk SDK adapter.
- Implement TeamTalk SDK event polling, login, channel join, and message commands.
- Add Windows protocol and file association registration for `.tt` files and `tt://` URLs.
- Add Preferences, Sound System, Shortcuts, Text to Speech, and Video Capture screens.
- Add automated UIA smoke checks and manual NVDA/JAWS verification notes.
