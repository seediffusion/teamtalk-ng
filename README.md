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
- TeamTalk native boundary project that probes for `TeamTalk5.dll`, resolves it from the app folder or the official TeamTalk installation, polls TeamTalk events, logs in, joins configured channels, dispatches user/text events, and falls back to the mock session while native files are absent.
- Mock TeamTalk session service so UI and announcement flows can be exercised before the TeamTalk SDK adapter is connected.

## Accessibility Approach

TeamTalk NG uses two complementary accessibility paths:

- Standard WPF controls expose Name, Role, Value, State, keyboard focus, and control patterns through Windows UI Automation.
- Intentional TeamTalk events such as channel messages, joins/leaves, connection changes, and transmission state changes go through the announcement service and Prismatoid when available.

The announcement service prefers Prismatoid when it is present. When Prismatoid is not available, it falls back to WPF UI Automation notification events so NVDA, JAWS, and Narrator still receive TeamTalk announcements, with debug output kept as a developer trace.

## Build

```powershell
dotnet build TeamTalkNg.slnx
```

## Local TeamTalk Runtime Binary

The native TeamTalk DLL is not committed. For local testing, use the runtime DLL from an installed official TeamTalk client, or place `TeamTalk5.dll` directly beside `TeamTalkNg.App.exe`.

The app looks for `TeamTalk5.dll` in this order:

```powershell
TeamTalkNg.App.exe folder
Current working folder
C:\Program Files\TeamTalk5
C:\Program Files (x86)\TeamTalk5
```

You can also set `TEAMTALKNG_TEAMTALK5_DLL` to an explicit DLL path for local testing. Avoid using the downloadable TeamTalk 5 SDK package as the runtime source unless you have a BearWare SDK license, because SDK downloads are trial builds. When no suitable DLL is found, the app falls back to the mock TeamTalk session.

## Parser Smoke Tests

```powershell
dotnet run --project tests\TeamTalkNg.Tests\TeamTalkNg.Tests.csproj
```

## Windows Installer

The Windows installer script is in `installer\windows\TeamTalkNg.iss`.

Build a publish folder first:

```powershell
dotnet publish src\TeamTalkNg.App\TeamTalkNg.App.csproj -c Release -r win-x64 --self-contained true -o artifacts\publish\TeamTalkNG
```

For trusted prerelease builds that include BearWare's runtime, place
`TeamTalk5.dll` in that publish folder before compiling the installer.

Compile with Inno Setup:

```powershell
iscc installer\windows\TeamTalkNg.iss
```

## License

TeamTalk NG source code is licensed under the MIT License. See `LICENSE`.

Third-party runtime files and assets, including BearWare `TeamTalk5.dll`,
BearWare-compatible sound files, Prismatoid if bundled, and .NET runtime
components, remain under their own license terms. See `THIRD-PARTY-NOTICES.md`.

## Next Implementation Targets

- Exercise the native SDK path against a real TeamTalk server and fill any struct-layout gaps found by `TT_DBG_SIZEOF`.
- Add audio device initialization, voice transmission, push-to-talk, and voice activation using the native SDK.
- Add Windows protocol and file association registration for `.tt` files and `tt://` URLs.
- Add Preferences, Sound System, Shortcuts, Text to Speech, and Video Capture screens.
- Add automated UIA smoke checks and manual NVDA/JAWS verification notes.
