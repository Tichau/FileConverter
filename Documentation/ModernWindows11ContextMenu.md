# Windows 11 modern context menu (experimental)

The existing File Converter application and SharpShell extension target .NET Framework 4.8. This integration adds a separate .NET 8 COM-hosted `IExplorerCommand` server and a sparse MSIX identity package. The package declares `windows.fileExplorerContextMenus`, which is the supported Windows 11 modern-menu registration path. It does not replace or edit the existing installer or classic extension registration.

The COM server reads `Settings.user.xml` from `%LOCALAPPDATA%\FileConverter` and falls back to `Settings.default.xml` beside the executable recorded at `HKCU\Software\FileConverter\Path`. Preset names, input extensions, and folder paths come from the existing XML. Since Explorer only supports a single command submenu level, nested preset paths are shown as labels such as `Video / To Mp4`. Commands pass the selected file paths to the existing converter through its `--conversion-preset` and `--input-files` command-line options. The settings command opens the existing settings UI.

## Build a separate package

Use Windows 11, .NET 8 SDK, Visual Studio Build Tools (for the existing application if it must be built), and the Windows 10/11 SDK (MakeAppx and SignTool). Build outputs go under `artifacts\modern-shell`; the existing install and normal project outputs are not overwritten.

The manifest publisher is `CN=FileConverter Modern Shell`. Create or obtain a code-signing certificate with that exact subject. To make a private development certificate:

```powershell
$cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject 'CN=FileConverter Modern Shell' -CertStoreLocation 'Cert:\CurrentUser\My'
$password = Read-Host 'PFX password' -AsSecureString
Export-PfxCertificate -Cert $cert -FilePath .\FileConverterModernShell.pfx -Password $password
```

Build and sign the package:

```powershell
.Installer\Build-ModernShellPackage.ps1 -CertificatePath .\FileConverterModernShell.pfx -CertificatePassword '<pfx-password>'
```

The resulting package is `artifacts\modern-shell\FileConverter.ModernShell.msix`. The build also exports `TestSigningCertificate.cer`; it contains only the public test certificate. A production distribution must use a publisher certificate trusted by the target Windows account. For this development package, import the test certificate into the local computer's `TrustedPeople` store from an elevated PowerShell window before package installation:

```powershell
Import-Certificate -FilePath .\artifacts\modern-shell\TestSigningCertificate.cer -CertStoreLocation Cert:\LocalMachine\TrustedPeople
```

This trusts the test signer on the computer, so remove that certificate entry after removing the package if it is no longer needed.

## Install and remove

Run from the project root under the same Windows account that uses File Converter:

```powershell
.Installer\Install-ModernShell.ps1 -PackagePath .\artifacts\modern-shell\FileConverter.ModernShell.msix
```

This registers one per-user sparse package with `Add-AppxPackage -ExternalLocation`, using the existing install folder read from `HKCU\Software\FileConverter\Path`. The script copies the COM host payload and three package visual assets into uniquely named `FileConverterModernShell` and `FileConverterModernShellAssets` folders beside the app because sparse-package payload paths resolve from the external location. It refuses to overwrite these folders; run the uninstall script before installing a package update. Uninstallation removes these folders. If File Converter is installed under Program Files, run installation and uninstallation in an elevated PowerShell window for the same user account.

The integration itself only reads the existing File Converter registry value; Windows manages package identity and COM activation registration. It does not write custom context-menu registry keys or change global menu settings. Restart File Explorer or sign out and back in if the entry does not appear immediately.

To roll back:

```powershell
.Installer\Uninstall-ModernShell.ps1
```

This removes only the `FileConverter.ModernShell` package for the current user. It leaves the File Converter MSI installation, settings, and existing classic extension alone. Remove the development certificate from `LocalMachine\TrustedPeople` manually only if you imported it solely for this package.

## Validation status and limitations

The extension is x64, matching the x64 File Explorer process and the existing x64 installer. Windows 11 21H2 (build 22000) or later is declared in the package manifest. The application itself remains on .NET Framework 4.8; only the isolated shell component requires the .NET 8 runtime. The test package is framework dependent, so the target machine needs the .NET 8 Desktop Runtime.

The current development environment did not have a .NET SDK, Visual Studio/MSBuild, Windows SDK packaging tools, a signed package, or an available test Windows 11 Explorer session. Therefore package build, package installation, COM activation, visibility in the primary menu, invocation, and classic-menu duplicate behavior have not been verified here. The build command failed before compilation because no .NET SDK is installed. These checks must be completed on a Windows 11 development machine with the prerequisites above before distributing this experimental integration.
