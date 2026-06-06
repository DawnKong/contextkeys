param(
    [string]$Version = "0.1.0-beta",
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$PackagingDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectDir = Split-Path -Parent $PackagingDir
$WorkspaceDir = Split-Path -Parent $ProjectDir
$ProjectPath = Join-Path $ProjectDir "ContextKeys.csproj"
$ArtifactsDir = Join-Path $WorkspaceDir "artifacts"
$PublishDir = Join-Path $ArtifactsDir "publish\$Runtime"
$InstallerDir = Join-Path $WorkspaceDir "Installer"
$InnoScript = Join-Path $PackagingDir "ContextKeys.iss"
$InstallerPath = Join-Path $InstallerDir "ContextKeys-Setup-$Version-$Runtime.exe"

function Find-InnoCompiler {
    $command = Get-Command "iscc.exe" -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $candidates = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
    )

    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path $candidate)) {
            return $candidate
        }
    }

    return $null
}

New-Item -ItemType Directory -Force -Path $PublishDir, $InstallerDir | Out-Null

Write-Host "Publishing ContextKeys $Version ($Runtime)..."
dotnet publish $ProjectPath `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained true `
    --output $PublishDir `
    -p:Version=$Version `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false

Copy-Item -Force (Join-Path $ProjectDir "Lkey.ico") (Join-Path $PublishDir "LKey.ico")

foreach ($file in @("ContextKeys.exe", "LKey.ico")) {
    $path = Join-Path $PublishDir $file
    if (-not (Test-Path $path)) {
        throw "Required publish file missing: $path"
    }
}

$iscc = Find-InnoCompiler
if (-not $iscc) {
    throw "Inno Setup compiler (ISCC.exe) not found. Install Inno Setup 6, then rerun this script."
}

if (Test-Path $InstallerPath) {
    Remove-Item -Force $InstallerPath
}

$env:CONTEXTKEYS_VERSION = $Version
$env:CONTEXTKEYS_PUBLISH_DIR = $PublishDir
$env:CONTEXTKEYS_INSTALLER_DIR = $InstallerDir

Write-Host "Building Inno Setup installer..."
& $iscc $InnoScript
if ($LASTEXITCODE -ne 0) {
    throw "ISCC failed with exit code $LASTEXITCODE"
}

if (-not (Test-Path $InstallerPath)) {
    throw "Installer was not created: $InstallerPath"
}

Write-Host "Inno installer: $InstallerPath"
