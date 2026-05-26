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
- TeamTalk SDK boundary project that probes for `TeamTalk5.dll`, resolves it from the app or `sdk` folder, polls SDK events, logs in, joins configured channels, dispatches user/text events, and falls back to the mock session while native files are absent.
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

## Local TeamTalk SDK Binary

The native SDK DLL is not committed. Download the official TeamTalk 5 SDK Standard Edition for Windows x64 from BearWare and place `TeamTalk5.dll` in the ignored `sdk\` folder:

```powershell
sdk\TeamTalk5.dll
```

When the DLL is present, the app and test projects copy it to their output `sdk\` folders during build. When it is absent, the app falls back to the mock TeamTalk session.

## Parser Smoke Tests

```powershell
dotnet run --project tests\TeamTalkNg.Tests\TeamTalkNg.Tests.csproj
```

## Next Implementation Targets

- Exercise the native SDK path against a real TeamTalk server and fill any struct-layout gaps found by `TT_DBG_SIZEOF`.
- Add audio device initialization, voice transmission, push-to-talk, and voice activation using the native SDK.
- Add Windows protocol and file association registration for `.tt` files and `tt://` URLs.
- Add Preferences, Sound System, Shortcuts, Text to Speech, and Video Capture screens.
- Add automated UIA smoke checks and manual NVDA/JAWS verification notes.
