# ContextKeys packaging

Build a normal wizard installer with Inno Setup from the project root:

```powershell
.\packaging\Build-InnoInstaller.ps1
```

Output:

- `..\Installer\ContextKeys-Setup-0.1.0-beta-win-x64.exe`

Build the fallback silent self-extracting package:

```powershell
.\packaging\Build-Installer.ps1
```

Outputs:

- `..\Installer\ContextKeys-Setup-0.1.0-beta-win-x64.exe`
- `..\Installer\ContextKeys-Portable-0.1.0-beta-win-x64.zip`

The Inno installer shows a normal setup wizard, installs per-user by default, creates Start Menu shortcuts, offers optional desktop and startup tasks, and registers an uninstall entry.
