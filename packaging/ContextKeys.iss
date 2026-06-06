#define MyAppName "ContextKeys"
#define MyAppVersion GetEnv("CONTEXTKEYS_VERSION")
#if MyAppVersion == ""
  #define MyAppVersion "0.1.0-beta"
#endif
#define MyAppPublisher "DawnKong"
#define MyAppExeName "ContextKeys.exe"
#define SourceDir GetEnv("CONTEXTKEYS_PUBLISH_DIR")
#if SourceDir == ""
  #define SourceDir "..\artifacts\publish\win-x64"
#endif
#define OutputDir GetEnv("CONTEXTKEYS_INSTALLER_DIR")
#if OutputDir == ""
  #define OutputDir "..\Installer"
#endif

[Setup]
AppId={{9A16C5E9-4C7B-45AC-86F3-C04977957048}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
AllowNoIcons=yes
OutputDir={#OutputDir}
OutputBaseFilename=ContextKeys-Setup-{#MyAppVersion}-win-x64
SetupIconFile={#SourceDir}\LKey.ico
UninstallDisplayIcon={app}\LKey.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
CloseApplicationsFilter=ContextKeys.exe
RestartApplications=no
VersionInfoVersion=0.1.0.0
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} Setup
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion=0.1.0.0

[Languages]
Name: "chinesesimp"; MessagesFile: "compiler:Default.isl"

[LangOptions]
LanguageName=简体中文
LanguageID=$0804
LanguageCodePage=936
DialogFontName=Microsoft YaHei UI
DialogFontSize=9
WelcomeFontName=Microsoft YaHei UI
WelcomeFontSize=14

[Messages]
SetupAppTitle=安装程序
SetupWindowTitle=安装 - %1
UninstallAppTitle=卸载程序
UninstallAppFullTitle=%1 卸载
InformationTitle=信息
ConfirmTitle=确认
ErrorTitle=错误
ExitSetupTitle=退出安装
ExitSetupMessage=安装尚未完成。如果现在退出，程序将不会被安装。%n%n你可以稍后再次运行安装程序完成安装。%n%n确定要退出安装吗？
ButtonBack=< 上一步(&B)
ButtonNext=下一步(&N) >
ButtonInstall=安装(&I)
ButtonOK=确定
ButtonCancel=取消
ButtonYes=是(&Y)
ButtonNo=否(&N)
ButtonFinish=完成(&F)
ButtonBrowse=浏览(&B)...
ButtonWizardBrowse=浏览(&B)...
ButtonNewFolder=新建文件夹(&M)
ClickNext=点击“下一步”继续，或点击“取消”退出安装。
BrowseDialogTitle=浏览文件夹
BrowseDialogLabel=请在下面的列表中选择一个文件夹，然后点击“确定”。
NewFolderName=新建文件夹
WelcomeLabel1=欢迎使用 [name] 安装向导
WelcomeLabel2=此向导将在你的电脑上安装 [name/ver]。%n%n建议继续之前关闭其他正在运行的应用程序。
WizardSelectDir=选择安装位置
SelectDirDesc=[name] 应该安装到哪里？
SelectDirLabel3=安装程序会将 [name] 安装到以下文件夹。
SelectDirBrowseLabel=点击“下一步”继续。如需选择其他文件夹，请点击“浏览”。
DiskSpaceGBLabel=至少需要 [gb] GB 可用磁盘空间。
DiskSpaceMBLabel=至少需要 [mb] MB 可用磁盘空间。
DirExistsTitle=文件夹已存在
DirExists=文件夹：%n%n%1%n%n已经存在。是否仍然安装到该文件夹？
DirDoesntExistTitle=文件夹不存在
DirDoesntExist=文件夹：%n%n%1%n%n不存在。是否创建该文件夹？
WizardSelectTasks=选择附加任务
SelectTasksDesc=需要执行哪些附加任务？
SelectTasksLabel2=请选择安装 [name] 时需要执行的附加任务，然后点击“下一步”。
WizardReady=准备安装
ReadyLabel1=安装程序已准备好开始安装 [name]。
ReadyLabel2a=点击“安装”继续；如需检查或修改设置，请点击“上一步”。
ReadyLabel2b=点击“安装”继续。
ReadyMemoDir=安装位置：
ReadyMemoGroup=开始菜单文件夹：
ReadyMemoTasks=附加任务：
WizardPreparing=正在准备安装
PreparingDesc=安装程序正在准备安装 [name]。
ApplicationsFound=以下应用正在使用需要更新的文件。建议允许安装程序自动关闭这些应用。
CloseApplications=自动关闭这些应用(&A)
DontCloseApplications=不要关闭这些应用(&D)
WizardInstalling=正在安装
InstallingLabel=请稍候，安装程序正在安装 [name]。
StatusClosingApplications=正在关闭应用程序...
StatusCreateDirs=正在创建目录...
StatusExtractFiles=正在解压文件...
StatusCreateIcons=正在创建快捷方式...
StatusCreateRegistryEntries=正在写入注册表...
StatusSavingUninstall=正在保存卸载信息...
StatusRunProgram=正在完成安装...
FinishedHeadingLabel=正在完成 [name] 安装向导
FinishedLabelNoIcons=安装程序已完成 [name] 的安装。
FinishedLabel=安装程序已完成 [name] 的安装。你可以通过已创建的快捷方式启动应用。
ClickFinish=点击“完成”退出安装程序。
RunEntryExec=运行 %1
SetupAborted=安装未完成。%n%n请修正问题后重新运行安装程序。
ConfirmUninstall=确定要完全移除 %1 及其所有组件吗？
UninstallStatusLabel=请稍候，正在从你的电脑中移除 %1。
UninstalledAll=%1 已成功从你的电脑中移除。
UninstalledMost=%1 卸载完成。%n%n部分内容未能移除，可手动删除。
WizardUninstalling=卸载状态
StatusUninstalling=正在卸载 %1...

[CustomMessages]
AdditionalIcons=附加快捷方式：
CreateDesktopIcon=创建桌面快捷方式(&D)

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startup"; Description: "开机自动启动 ContextKeys"; GroupDescription: "启动选项："; Flags: unchecked

[Files]
Source: "{#SourceDir}\ContextKeys.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\LKey.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\ContextKeys"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\LKey.ico"
Name: "{autodesktop}\ContextKeys"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\LKey.ico"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "ContextKeys"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "启动 ContextKeys"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "taskkill"; Parameters: "/IM ContextKeys.exe /F"; Flags: runhidden; RunOnceId: "StopContextKeys"
