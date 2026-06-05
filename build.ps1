param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [ValidateSet("x64")]
    [string]$Platform = "x64"
)

$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Find-MSBuild {
    if ($env:MSBUILD_PATH -and (Test-Path -LiteralPath $env:MSBUILD_PATH)) {
        return (Resolve-Path -LiteralPath $env:MSBUILD_PATH).Path
    }

    $vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path -LiteralPath $vswhere) {
        $candidate = & $vswhere -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1
        if ($candidate -and (Test-Path -LiteralPath $candidate)) {
            return $candidate
        }
    }

    $pathCommand = Get-Command msbuild.exe -ErrorAction SilentlyContinue
    if ($pathCommand -and $pathCommand.Source -notlike "$env:windir\Microsoft.NET\Framework*") {
        return $pathCommand.Source
    }

    return $null
}

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $repoRoot

if ($env:OS -ne "Windows_NT") {
    throw "ZFileConverter can only be built on Windows because it targets WPF, .NET Framework, SharpShell, and WiX."
}

$targetingPack = Join-Path ${env:ProgramFiles(x86)} "Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\RedistList\FrameworkList.xml"
if (!(Test-Path -LiteralPath $targetingPack)) {
    throw ".NET Framework 4.8 Developer Pack / targeting pack is missing. Install Visual Studio 2022 with .NET Framework 4.8 targeting tools, then rerun .\build.ps1."
}

$msbuild = Find-MSBuild
if (!$msbuild) {
    throw "Visual Studio MSBuild was not found. Install Visual Studio 2022 Build Tools or run this from Developer PowerShell. The legacy .NET Framework MSBuild is not enough for package restore and WiX SDK projects."
}

Write-Step "Using MSBuild"
Write-Host $msbuild

Write-Step "Building ZFileConverter $Configuration $Platform"
& $msbuild "$repoRoot\FileConverter.sln" /restore /m /p:Configuration=$Configuration /p:Platform=$Platform
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$appExe = Join-Path $repoRoot "Application\FileConverter\bin\$Platform\$Configuration\FileConverter.exe"
$installer = Join-Path $repoRoot "Installer\bin\$Platform\$Configuration\ZFileConverter-setup.msi"

Write-Step "Checking outputs"
if (!(Test-Path -LiteralPath $appExe)) {
    throw "Build finished, but the app executable was not found at $appExe"
}

if ($Configuration -eq "Release" -and !(Test-Path -LiteralPath $installer)) {
    throw "Build finished, but the MSI was not found at $installer"
}

Write-Host "App:       $appExe" -ForegroundColor Green
if (Test-Path -LiteralPath $installer) {
    Write-Host "Installer: $installer" -ForegroundColor Green
}

Write-Step "Done"
