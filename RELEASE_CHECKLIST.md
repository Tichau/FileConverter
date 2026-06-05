# ZFileConverter Release Checklist

Use this before publishing a GitHub release.

## Source

- Confirm the release branch is clean.
- Confirm `version.xml` points to the intended GitHub release asset.
- Update `CHANGELOG.md` with user-facing changes.
- Confirm `README.md` and `docs/BUILDING.md` match the release process.

## Build

- Build from a clean checkout on Windows.
- Run:

```powershell
msbuild FileConverter.sln /restore /m /p:Configuration=Release /p:Platform=x64
```

- Confirm the app output exists under `Application\FileConverter\bin\x64\Release`.
- Confirm `Installer\bin\x64\Release\ZFileConverter-setup.msi` exists.
- Confirm whether the installer is signed or intentionally unsigned.

## Smoke Test

- Launch Settings.
- Open Settings > Health and refresh dependency health.
- Run Explorer menu repair from Settings on a test machine.
- Convert image to JPG, PNG, WebP, and AVIF.
- Convert audio or video through FFmpeg.
- Convert PDF to image through ImageMagick/Ghostscript.
- Convert Office documents if Word, Excel, and PowerPoint are available.
- Retry a failed conversion from the queue.
- Open a completed output folder from the queue.
- Copy diagnostics logs and open the diagnostics folder.

## Installer

- Install on a clean Windows VM.
- Confirm Start Menu entries show `ZFileConverter`.
- Confirm Explorer right-click menu shows `ZFileConverter`.
- Confirm uninstall removes the Start Menu entry and registry path.
- Reinstall over the previous version and confirm presets survive.

## GitHub Release

- Create a tag matching the version, for example `v2.2.0-z1`.
- Upload the MSI and any app artifact zip.
- Include whether the installer is signed.
- Include known limitations and dependency notes.
- Link to troubleshooting and Settings > Health.
