# Installing ZFileConverter

## Recommended: GitHub Release

Download the newest `ZFileConverter-*-x64-setup.msi` from the GitHub Releases page.
ZFileConverter is released as a 64-bit Windows app:

```text
https://github.com/ZaidNAlAsali/FileConverter/releases
```

Run the MSI and follow the installer. After installation, right-click a supported file in
Windows Explorer and choose `ZFileConverter`.

If Windows SmartScreen warns about an unsigned installer, choose to keep/run it only if you
downloaded it from the project release page. Code signing can be added later when a signing
certificate is available.

## From GitHub Actions

Every push and pull request builds Windows x64 artifacts. Open the workflow run, download
`ZFileConverter-installer-x64`, unzip it, and run `ZFileConverter-setup.msi`.

## From Source

Install:

- Windows 10 or newer.
- Visual Studio 2022 or Build Tools.
- .NET Framework 4.8 targeting tools.
- WiX SDK packages, restored automatically by MSBuild.

Then run:

```powershell
.\build.ps1 -Configuration Release -Platform x64
```

Install the generated MSI:

```text
Installer\bin\x64\Release\ZFileConverter-setup.msi
```

## First-Run Checks

Open Settings > Health and refresh the checks. It should tell you whether FFmpeg,
ImageMagick, Ghostscript, Microsoft Office support, settings files, and Explorer integration
are available.

If the Explorer menu is missing, use Settings > Health > Repair Explorer Menu.
