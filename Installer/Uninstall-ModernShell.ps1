$ErrorActionPreference = "Stop"
$package = Get-AppxPackage -Name "FileConverter.ModernShell" -ErrorAction SilentlyContinue
if ($package) { Remove-AppxPackage -Package $package.PackageFullName }
$appPath = (Get-ItemProperty -Path "HKCU:\Software\FileConverter" -Name Path -ErrorAction SilentlyContinue).Path
if ($appPath) {
    $installFolder = Split-Path -Parent $appPath
    foreach ($ownedFolder in @("FileConverterModernShellAssets", "FileConverterModernShell")) {
        $folder = Join-Path $installFolder $ownedFolder
        if (Test-Path -LiteralPath $folder) { Remove-Item -LiteralPath $folder -Recurse -Force }
    }
}
Write-Host "Modern File Explorer integration removed for this user. The existing File Converter installation and classic extension were not changed."
