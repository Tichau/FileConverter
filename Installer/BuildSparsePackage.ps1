param(
    [Parameter(Mandatory = $true)]
    [string] $PackageDirectory,

    [Parameter(Mandatory = $true)]
    [string] $OutputDirectory,

    [Parameter(Mandatory = $true)]
    [string] $ProductVersion,

    [Parameter(Mandatory = $true)]
    [string] $RepositoryRoot
)

$ErrorActionPreference = 'Stop'

$packageName = 'Tichau.FileConverter.ContextMenu'
$publisher = 'CN=File Converter Sparse Package'
$packageVersion = "$ProductVersion.0"
if ($ProductVersion -match '^\d+\.\d+\.\d+\.\d+$') {
    $packageVersion = $ProductVersion
}

Remove-Item -LiteralPath $PackageDirectory -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $PackageDirectory | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $PackageDirectory 'Assets') | Out-Null
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
Remove-Item -LiteralPath (Join-Path $OutputDirectory 'FileConverterSparse.msix') -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath (Join-Path $OutputDirectory 'FileConverterSparse.cer') -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath (Join-Path $OutputDirectory 'FileConverterSparse.pfx') -Force -ErrorAction SilentlyContinue

$icon = Join-Path $RepositoryRoot 'Resources\Icons\ApplicationIcon.png'
Copy-Item -LiteralPath $icon -Destination (Join-Path $PackageDirectory 'Assets\StoreLogo.png') -Force
Copy-Item -LiteralPath $icon -Destination (Join-Path $PackageDirectory 'Assets\Square44x44Logo.png') -Force
Copy-Item -LiteralPath $icon -Destination (Join-Path $PackageDirectory 'Assets\Square150x150Logo.png') -Force

$manifest = @"
<?xml version="1.0" encoding="utf-8"?>
<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10" xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10" xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities" xmlns:desktop4="http://schemas.microsoft.com/appx/manifest/desktop/windows10/4" xmlns:desktop5="http://schemas.microsoft.com/appx/manifest/desktop/windows10/5" xmlns:uap10="http://schemas.microsoft.com/appx/manifest/uap/windows10/10" xmlns:com="http://schemas.microsoft.com/appx/manifest/com/windows10" IgnorableNamespaces="uap rescap desktop4 desktop5 uap10 com">
  <Identity Name="$packageName" ProcessorArchitecture="neutral" Publisher="$publisher" Version="$packageVersion" />
  <Properties>
    <DisplayName>File Converter Context Menu</DisplayName>
    <PublisherDisplayName>File Converter</PublisherDisplayName>
    <Logo>Assets\StoreLogo.png</Logo>
    <uap10:AllowExternalContent>true</uap10:AllowExternalContent>
  </Properties>
  <Resources>
    <Resource Language="en-us" />
  </Resources>
  <Dependencies>
    <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.22000.0" MaxVersionTested="10.0.26100.0" />
  </Dependencies>
  <Capabilities>
    <rescap:Capability Name="runFullTrust" />
    <rescap:Capability Name="unvirtualizedResources" />
  </Capabilities>
  <Applications>
    <Application Id="FileConverterContextMenu" Executable="FileConverter.exe" uap10:TrustLevel="mediumIL" uap10:RuntimeBehavior="win32App">
      <uap:VisualElements AppListEntry="none" DisplayName="File Converter" Description="File Converter Context Menu" BackgroundColor="transparent" Square150x150Logo="Assets\Square150x150Logo.png" Square44x44Logo="Assets\Square44x44Logo.png" />
      <Extensions>
        <desktop4:Extension Category="windows.fileExplorerContextMenus">
          <desktop4:FileExplorerContextMenus>
            <desktop5:ItemType Type="*">
              <desktop5:Verb Id="FileConverterCommand" Clsid="C069DB02-F64B-4651-A69F-42E3B0B94C44" />
            </desktop5:ItemType>
          </desktop4:FileExplorerContextMenus>
        </desktop4:Extension>
        <com:Extension Category="windows.comServer" uap10:RuntimeBehavior="packagedClassicApp">
          <com:ComServer>
            <com:SurrogateServer DisplayName="File Converter context menu handler">
              <com:Class Id="C069DB02-F64B-4651-A69F-42E3B0B94C44" Path="FileConverterContextMenu.dll" ThreadingModel="STA" />
            </com:SurrogateServer>
          </com:ComServer>
        </com:Extension>
      </Extensions>
    </Application>
  </Applications>
</Package>
"@

Set-Content -LiteralPath (Join-Path $PackageDirectory 'AppxManifest.xml') -Value $manifest -Encoding UTF8
