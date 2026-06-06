$ErrorActionPreference = "Stop"

$Version = "__VERSION__"
$AppName = "ContextKeys"
$Publisher = "DawnKong"
$InstallDir = Join-Path $env:LOCALAPPDATA "Programs\ContextKeys"
$StartMenuDir = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\ContextKeys"
$DesktopShortcut = Join-Path ([Environment]::GetFolderPath("Desktop")) "ContextKeys.lnk"
$StartMenuShortcut = Join-Path $StartMenuDir "ContextKeys.lnk"
$ExePath = Join-Path $InstallDir "ContextKeys.exe"
$IconPath = Join-Path $InstallDir "LKey.ico"
$UninstallScript = Join-Path $InstallDir "Uninstall-ContextKeys.ps1"
$UninstallKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\ContextKeys"

function New-Shortcut {
    param(
        [string]$ShortcutPath,
        [string]$TargetPath,
        [string]$IconLocation
    )

    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($ShortcutPath)
    $shortcut.TargetPath = $TargetPath
    $shortcut.WorkingDirectory = Split-Path -Parent $TargetPath
    $shortcut.IconLocation = $IconLocation
    $shortcut.Description = "ContextKeys"
    $shortcut.Save()
}

Get-Process -Name "ContextKeys" -ErrorAction SilentlyContinue | Stop-Process -Force

New-Item -ItemType Directory -Force -Path $InstallDir, $StartMenuDir | Out-Null
Copy-Item -Force (Join-Path $PSScriptRoot "ContextKeys.exe") $ExePath
Copy-Item -Force (Join-Path $PSScriptRoot "LKey.ico") $IconPath
Copy-Item -Force (Join-Path $PSScriptRoot "Uninstall-ContextKeys.ps1") $UninstallScript

New-Shortcut -ShortcutPath $DesktopShortcut -TargetPath $ExePath -IconLocation $IconPath
New-Shortcut -ShortcutPath $StartMenuShortcut -TargetPath $ExePath -IconLocation $IconPath

$estimatedSize = [int]((Get-ChildItem $InstallDir -Recurse -File | Measure-Object Length -Sum).Sum / 1KB)
New-Item -Path $UninstallKey -Force | Out-Null
Set-ItemProperty -Path $UninstallKey -Name "DisplayName" -Value $AppName
Set-ItemProperty -Path $UninstallKey -Name "DisplayVersion" -Value $Version
Set-ItemProperty -Path $UninstallKey -Name "Publisher" -Value $Publisher
Set-ItemProperty -Path $UninstallKey -Name "InstallLocation" -Value $InstallDir
Set-ItemProperty -Path $UninstallKey -Name "DisplayIcon" -Value $IconPath
Set-ItemProperty -Path $UninstallKey -Name "UninstallString" -Value "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$UninstallScript`""
Set-ItemProperty -Path $UninstallKey -Name "QuietUninstallString" -Value "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$UninstallScript`" -Quiet"
Set-ItemProperty -Path $UninstallKey -Name "EstimatedSize" -Value $estimatedSize -Type DWord
Set-ItemProperty -Path $UninstallKey -Name "NoModify" -Value 1 -Type DWord
Set-ItemProperty -Path $UninstallKey -Name "NoRepair" -Value 1 -Type DWord

Start-Process $ExePath
