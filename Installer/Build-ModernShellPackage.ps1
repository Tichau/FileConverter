param(
    [Parameter(Mandatory = $true)][string]$CertificatePath,
    [Parameter(Mandatory = $true)][string]$CertificatePassword,
    [string]$Configuration = "Release",
    [string]$OutputDirectory = (Join-Path $PSScriptRoot "..\artifacts\modern-shell")
)
$ErrorActionPreference = "Stop"
$repo = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
$repoPrefix = $repo.TrimEnd('\') + '\'
if (-not $OutputDirectory.StartsWith($repoPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "OutputDirectory must stay inside the repository so the build cannot remove unrelated files."
}
$project = Join-Path $repo "Application\ModernShell\FileConverter.ModernShell.csproj"
$manifest = Join-Path $repo "Application\ModernShell\Package.appxmanifest"
$stage = Join-Path $OutputDirectory "PackageRoot"
$runtime = Join-Path $stage "FileConverterModernShell"
$buildBin = Join-Path $OutputDirectory "build\bin"
$buildObj = Join-Path $OutputDirectory "build\obj"
$package = Join-Path $OutputDirectory "FileConverter.ModernShell.msix"

# Keep SDK first-run state and package restore caches with the separate test output.
$env:DOTNET_CLI_HOME = Join-Path $OutputDirectory ".dotnet-home"
$env:NUGET_PACKAGES = Join-Path $OutputDirectory ".nuget-packages"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
New-Item -ItemType Directory -Path $env:DOTNET_CLI_HOME, $env:NUGET_PACKAGES -Force | Out-Null

if (-not (Test-Path -LiteralPath $CertificatePath)) { throw "Signing certificate not found: $CertificatePath" }
$cert = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($CertificatePath, $CertificatePassword)
if ($cert.Subject -ne "CN=FileConverter Modern Shell") { throw "Certificate subject must be CN=FileConverter Modern Shell to match Package.appxmanifest." }

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
New-Item -ItemType Directory -Path $runtime -Force | Out-Null
dotnet publish $project -c $Configuration --self-contained false -o $runtime "-p:BaseOutputPath=$buildBin\" "-p:BaseIntermediateOutputPath=$buildObj\"
if ($LASTEXITCODE -ne 0) { throw "Modern shell component publish failed ($LASTEXITCODE)." }
Copy-Item -LiteralPath $manifest -Destination (Join-Path $stage "AppxManifest.xml")
New-Item -ItemType Directory -Path (Join-Path $stage "FileConverterModernShellAssets") -Force | Out-Null
foreach ($asset in @("StoreLogo.png", "Square150x150Logo.png", "Square44x44Logo.png")) {
    $path = Join-Path $stage "FileConverterModernShellAssets\$asset"
    Add-Type -AssemblyName System.Drawing
    $size = if ($asset -eq "Square150x150Logo.png") { 150 } elseif ($asset -eq "Square44x44Logo.png") { 44 } else { 50 }
    $bitmap = [System.Drawing.Bitmap]::new($size, $size)
    $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
}

$makeAppx = (Get-Command makeappx.exe -ErrorAction SilentlyContinue)?.Source
if (-not $makeAppx) {
    $kits = Join-Path ${env:ProgramFiles(x86)} "Windows Kits\10\bin"
    $makeAppx = Get-ChildItem -Path $kits -Filter makeappx.exe -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match "\\x64\\makeappx\.exe$" } | Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
}
$signTool = (Get-Command signtool.exe -ErrorAction SilentlyContinue)?.Source
if (-not $signTool) {
    $kits = Join-Path ${env:ProgramFiles(x86)} "Windows Kits\10\bin"
    $signTool = Get-ChildItem -Path $kits -Filter signtool.exe -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match "\\x64\\signtool\.exe$" } | Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
}
if (-not $makeAppx -or -not $signTool) { throw "Windows SDK makeappx.exe and signtool.exe are required." }
& $makeAppx pack /d $stage /p $package /o /nv
if ($LASTEXITCODE -ne 0) { throw "MSIX packaging failed ($LASTEXITCODE)." }
& $signTool sign /fd SHA256 /f $CertificatePath /p $CertificatePassword $package
if ($LASTEXITCODE -ne 0) { throw "MSIX signing failed ($LASTEXITCODE)." }
Write-Host "Package created: $package"
