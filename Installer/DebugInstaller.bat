@echo off

setlocal EnableDelayedExpansion

REM Analyse command arguments
set quiet="false"
for %%x in (%*) do (
    if %%x==--debug (
        set msi="bin\x64\Debug\FileConverter-setup.msi"
    )
    if %%x==-d (
        set msi="bin\x64\Debug\FileConverter-setup.msi"
    )

    if %%x==--release (
        set msi="bin\x64\Release\FileConverter-setup.msi"
    )
    if %%x==-r (
        set msi="bin\x64\Release\FileConverter-setup.msi"
    )

    if %%x==--install (
        set action="install"
    )
    if %%x==-i (
        set action="install"
    )

    if %%x==--uninstall (
        set action="uninstall"
    )
    if %%x==-u (
        set action="uninstall"
    )

    if %%x==--quiet (
        set quiet="true"
    )
    if %%x==-q (
        set quiet="true"
    )
)

if "%msi%"=="" (
    echo "Target not defined. Format: DebugInstaller.bat -install|-uninstall -release|-debug"
    exit
)

if "%action%"=="" (
    echo "Action not defined. Format: DebugInstaller.bat -install|-uninstall -release|-debug"
    exit
)

REM Execute action.
REM msiexec documentation: https://www.advancedinstaller.com/user-guide/msiexec.html
if %action%=="install" (
    echo "Install File Converter using %MSI%..."

    if %quiet%=="true" (
        msiexec /i %MSI% /l*v %TEMP%\vmmsi.log /quiet
    ) else (
        msiexec /i %MSI% /l*v %TEMP%\vmmsi.log
    )
    
    echo "Open install logs"
    code "%TEMP%\vmmsi.log"
    exit
) 
if %action%=="uninstall" (
    echo "Uninstall File Converter using %MSI%..."

    if %quiet%=="true" (
        msiexec /x %MSI% /l*v %TEMP%\vmmsi.log /quiet
    ) else (
        msiexec /x %MSI% /l*v %TEMP%\vmmsi.log
    )

    echo "Open install logs"
    code "%TEMP%\vmmsi.log"
    exit
)
    
echo "Invalid action " %action%
