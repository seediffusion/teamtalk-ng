# TeamTalk NG Windows Installer

This folder contains the Inno Setup script used to build a Windows installer for
TeamTalk NG.

## Requirements

- .NET 10 SDK
- Inno Setup 6

## Build Publish Output

From the repository root:

```powershell
dotnet publish src\TeamTalkNg.App\TeamTalkNg.App.csproj -c Release -r win-x64 --self-contained true -o artifacts\publish\TeamTalkNG
```

For a trusted prerelease build that bundles the TeamTalk runtime, copy
`TeamTalk5.dll` into `artifacts\publish\TeamTalkNG` before compiling the
installer.

If the release package includes BearWare runtime files or official TeamTalk
sound files, make sure the package also includes the relevant BearWare license
text or notice. `THIRD-PARTY-NOTICES.md` is copied into the app output by the
project file.

## Compile Installer

```powershell
iscc installer\windows\TeamTalkNg.iss
```

The installer is written to:

```text
artifacts\installer\TeamTalkNG_v0.1.0_Setup.exe
```

To compile from a different publish folder:

```powershell
iscc installer\windows\TeamTalkNg.iss /DPublishDir=C:\path\to\publish
```

## Installer Behavior

- Installs per user by default to `%LOCALAPPDATA%\Programs\TeamTalk NG`.
- Creates a Start Menu shortcut.
- Offers an optional desktop shortcut.
- Offers checked-by-default `tt://` URL and `.tt` file associations under the
  current user.
- Installs `LICENSE` and `THIRD-PARTY-NOTICES.md` beside the app.
