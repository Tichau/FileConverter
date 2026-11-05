param([Parameter(Mandatory = $true)][string]$PackagePath)
$ErrorActionPreference = "Stop"
$packageFile = (Resolve-Path -LiteralPath $PackagePath).Path
$appPath = (Get-ItemProperty -Path "HKCU:\Software\FileConverter" -Name Path -ErrorAction Stop).Path
$installFolder = Split-Path -Parent $appPath
if (-not (Test-Path -LiteralPath $appPath)) { throw "File Converter executable not found: $appPath" }
$assetFolder = Join-Path $installFolder "FileConverterModernShellAssets"
$serverFolder = Join-Path $installFolder "FileConverterModernShell"
if (Test-Path -LiteralPath $assetFolder) { throw "Integration assets already exist. Run Uninstall-ModernShell.ps1 before installing this package update: $assetFolder" }
if (Test-Path -LiteralPath $serverFolder) { throw "Integration COM files already exist. Run Uninstall-ModernShell.ps1 before installing this package update: $serverFolder" }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($packageFile)
try {
    foreach ($entry in $archive.Entries | Where-Object { ($_.FullName.StartsWith("FileConverterModernShellAssets/", [System.StringComparison]::OrdinalIgnoreCase) -or $_.FullName.StartsWith("FileConverterModernShell/", [System.StringComparison]::OrdinalIgnoreCase)) -and -not $_.FullName.EndsWith("/") }) {
        $relative = $entry.FullName.Replace('/', '\')
        $destination = Join-Path $installFolder $relative
        New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
        [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $destination, $false)
    }
}
catch {
    if (Test-Path -LiteralPath $assetFolder) { Remove-Item -LiteralPath $assetFolder -Recurse -Force }
    if (Test-Path -LiteralPath $serverFolder) { Remove-Item -LiteralPath $serverFolder -Recurse -Force }
    throw
}
finally { $archive.Dispose() }
try { Add-AppxPackage -Path $packageFile -ExternalLocation $installFolder }
catch {
    if (Test-Path -LiteralPath $assetFolder) { Remove-Item -LiteralPath $assetFolder -Recurse -Force }
    if (Test-Path -LiteralPath $serverFolder) { Remove-Item -LiteralPath $serverFolder -Recurse -Force }
    throw
}
Write-Host "Modern File Explorer integration registered for this user. Restart File Explorer if the menu is not refreshed."
