param(
    [string]$Version = "0.1.0-beta",
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release",
    [switch]$NoInstaller
)

$ErrorActionPreference = "Stop"

$PackagingDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectDir = Split-Path -Parent $PackagingDir
$WorkspaceDir = Split-Path -Parent $ProjectDir
$ProjectPath = Join-Path $ProjectDir "ContextKeys.csproj"
$ArtifactsDir = Join-Path $WorkspaceDir "artifacts"
$PublishDir = Join-Path $ArtifactsDir "publish\$Runtime"
$PackageStageDir = Join-Path $ArtifactsDir "installer-stage"
$InstallerDir = Join-Path $WorkspaceDir "Installer"
$InstallerName = "ContextKeys-Setup-$Version-$Runtime.exe"
$InstallerPath = Join-Path $InstallerDir $InstallerName
$ZipPath = Join-Path $InstallerDir "ContextKeys-Portable-$Version-$Runtime.zip"
$SetupStubProject = Join-Path $PackagingDir "SetupStub\ContextKeysSetup.csproj"
$SetupStubPublishDir = Join-Path $ArtifactsDir "setup-stub\$Runtime"
$SetupStubExe = Join-Path $SetupStubPublishDir "ContextKeysSetup.exe"
$PayloadMarker = "`n--CONTEXTKEYS-PAYLOAD-V1--`n"

function Copy-Template {
    param(
        [string]$Source,
        [string]$Destination
    )

    $content = Get-Content $Source -Raw -Encoding UTF8
    $content = $content.Replace("__VERSION__", $Version)
    Set-Content -Path $Destination -Value $content -Encoding UTF8
}

New-Item -ItemType Directory -Force -Path $ArtifactsDir, $InstallerDir | Out-Null
Remove-Item -Recurse -Force -ErrorAction SilentlyContinue $PublishDir, $PackageStageDir, $SetupStubPublishDir
New-Item -ItemType Directory -Force -Path $PublishDir, $PackageStageDir, $SetupStubPublishDir | Out-Null

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

$requiredFiles = @("ContextKeys.exe", "LKey.ico")
foreach ($file in $requiredFiles) {
    $path = Join-Path $PublishDir $file
    if (-not (Test-Path $path)) {
        throw "Required publish file missing: $path"
    }
}

Copy-Item (Join-Path $PublishDir "ContextKeys.exe") $PackageStageDir
Copy-Item (Join-Path $PublishDir "LKey.ico") $PackageStageDir
Copy-Template (Join-Path $PackagingDir "Install-ContextKeys.ps1") (Join-Path $PackageStageDir "Install-ContextKeys.ps1")
Copy-Template (Join-Path $PackagingDir "Uninstall-ContextKeys.ps1") (Join-Path $PackageStageDir "Uninstall-ContextKeys.ps1")
Copy-Item (Join-Path $PackagingDir "install.cmd") $PackageStageDir

if (Test-Path $ZipPath) {
    Remove-Item -Force $ZipPath
}
Compress-Archive -Path (Join-Path $PackageStageDir "*") -DestinationPath $ZipPath -Force
Write-Host "Portable package: $ZipPath"

if ($NoInstaller) {
    return
}

if (Test-Path $InstallerPath) {
    Remove-Item -Force $InstallerPath
}

Write-Host "Publishing setup stub..."
dotnet publish $SetupStubProject `
    --configuration Release `
    --runtime $Runtime `
    --self-contained true `
    --output $SetupStubPublishDir `
    -p:PublishSingleFile=true `
    -p:PublishTrimmed=true `
    -p:TrimMode=partial `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false

if (-not (Test-Path $SetupStubExe)) {
    throw "Setup stub was not created: $SetupStubExe"
}

Write-Host "Combining setup stub and payload..."
$output = [System.IO.File]::Open($InstallerPath, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write)
try {
    $stub = [System.IO.File]::OpenRead($SetupStubExe)
    try { $stub.CopyTo($output) } finally { $stub.Dispose() }

    $markerBytes = [System.Text.Encoding]::UTF8.GetBytes($PayloadMarker)
    $output.Write($markerBytes, 0, $markerBytes.Length)

    $payload = [System.IO.File]::OpenRead($ZipPath)
    try { $payload.CopyTo($output) } finally { $payload.Dispose() }
}
finally {
    $output.Dispose()
}

if (-not (Test-Path $InstallerPath)) {
    throw "Installer was not created: $InstallerPath"
}

Write-Host "Self-testing setup payload..."
$selfTest = Start-Process -FilePath $InstallerPath -ArgumentList "--self-test" -Wait -PassThru
if ($selfTest.ExitCode -ne 0) {
    throw "Setup self-test failed with exit code $($selfTest.ExitCode)"
}

Write-Host "Installer package: $InstallerPath"
