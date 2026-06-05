# Building ZFileConverter

ZFileConverter is a .NET Framework 4.8 WPF application with a SharpShell Explorer extension
and a WiX installer.

## Requirements

- Windows 10 or newer.
- Visual Studio 2022.
- .NET Framework 4.8 targeting pack.
- MSBuild from Visual Studio, usually available in Developer PowerShell.
- NuGet package restore enabled.
- WiX Toolset SDK packages restore through the installer project.

## Restore And Build

From a Visual Studio Developer PowerShell:

```powershell
msbuild FileConverter.sln /restore /m /p:Configuration=Release /p:Platform=x64
```

The application output is expected at:

```text
Application\FileConverter\bin\x64\Release\
```

The installer output is expected at:

```text
Installer\bin\x64\Release\ZFileConverter-setup.msi
```

## Unsigned Local Builds

The installer imports `Installer\Installer.sign` only when that file exists.
That means public CI and local contributors can build unsigned installers without private signing material.

Signed release builds should provide `Installer.sign` locally or through a secure release pipeline.

## Smoke Test

After a release build:

1. Launch `FileConverter.exe --settings`.
2. Open Settings > Health and confirm bundled dependencies are ready.
3. Convert one image to WebP or JPG.
4. Convert one video or audio sample through FFmpeg.
5. Convert a PDF page to PNG if Ghostscript is present.
6. Open Diagnostics, copy logs, and confirm the diagnostics folder opens.
7. Install the MSI in a clean VM and confirm the Explorer context menu appears.

## CI

The GitHub Actions workflow uses `microsoft/setup-msbuild`, restores the solution,
builds Release x64, and uploads application and installer artifacts when available.
