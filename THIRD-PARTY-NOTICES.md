# Third-Party Notices

TeamTalk NG source code is licensed under the MIT License. See `LICENSE`.

This file lists third-party components, runtime files, and assets that are not
owned by TeamTalk NG. Their original licenses and notices remain in effect.

## BearWare TeamTalk 5 Runtime

TeamTalk NG can load BearWare's native TeamTalk runtime library,
`TeamTalk5.dll`, to connect to TeamTalk 5 servers.

`TeamTalk5.dll` is not part of the TeamTalk NG MIT-licensed source code. It is
owned by BearWare.dk and remains subject to BearWare's license terms. If a
release package includes this DLL, the package should also include the relevant
BearWare license text and should not imply that the DLL is licensed under MIT.

Project:
https://bearware.dk

Upstream repository:
https://github.com/BearWare/TeamTalk5

## BearWare TeamTalk Sound Files

TeamTalk NG supports the official TeamTalk sound pack layout. Some bundled or
compatible sound files may originate from BearWare TeamTalk 5 releases or the
BearWare TeamTalk 5 repository under `Setup/Client/Sounds`.

These sound files are not part of the TeamTalk NG MIT-licensed source code
unless explicitly stated otherwise. Their original notices and license terms
remain in effect.

Upstream sound layout:
https://github.com/BearWare/TeamTalk5/tree/master/Setup/Client/Sounds

## Prismatoid

TeamTalk NG can use Prismatoid as an optional screen-reader output backend when
the Prismatoid assembly is available at runtime. TeamTalk NG currently discovers
Prismatoid dynamically and can run without it.

Prismatoid is not part of the TeamTalk NG MIT-licensed source code unless it is
separately bundled in a release package. If Prismatoid is bundled, include its
license and notices alongside this file.

Project:
https://github.com/the-byte-bender/Prismatoid

## .NET

TeamTalk NG is built with Microsoft .NET and WPF. .NET runtime components and
framework assemblies remain subject to Microsoft's license terms.

.NET:
https://dotnet.microsoft.com/
