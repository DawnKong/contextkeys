param(
    [switch]$Quiet
)

$ErrorActionPreference = "SilentlyContinue"

$InstallDir = Join-Path $env:LOCALAPPDATA "Programs\ContextKeys"
$StartMenuDir = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\ContextKeys"
$DesktopShortcut = Join-Path ([Environment]::GetFolderPath("Desktop")) "ContextKeys.lnk"
$UninstallKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\ContextKeys"
$StartupKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"

Get-Process -Name "ContextKeys" -ErrorAction SilentlyContinue | Stop-Process -Force
Remove-Item -Force $DesktopShortcut
Remove-Item -Recurse -Force $StartMenuDir
Remove-Item -Recurse -Force $UninstallKey
Remove-ItemProperty -Path $StartupKey -Name "ContextKeys"

$command = "timeout /t 2 /nobreak >nul & rmdir /s /q `"$InstallDir`""
Start-Process -WindowStyle Hidden -FilePath "cmd.exe" -ArgumentList "/c $command"

if (-not $Quiet) {
    Add-Type -AssemblyName PresentationFramework
    [System.Windows.MessageBox]::Show(
        "ContextKeys 已卸载。用户配置保留在 AppData\Roaming\ContextKeys，方便以后重装继续使用。",
        "ContextKeys",
        "OK",
        "Information") | Out-Null
}
