@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-ContextKeys.ps1"
exit /b %errorlevel%
