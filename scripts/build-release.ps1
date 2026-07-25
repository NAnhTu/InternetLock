# Powershell script to build, publish and package InternetLock for Windows x64 locally
$ErrorActionPreference = 'Stop'

# Dynamically determine Repository Root directory (parent of scripts/)
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$RepoRoot = Split-Path -Parent $ScriptDir

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "   InternetLock Local Release Builder    " -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Repository Root: $RepoRoot"

Set-Location $RepoRoot

$CsprojPath = Join-Path $RepoRoot "src\InternetLock\InternetLock.csproj"
$ArtifactsDir = Join-Path $RepoRoot "artifacts"
$TempPublishDir = Join-Path $ArtifactsDir "temp-publish"
$OutputZipPath = Join-Path $ArtifactsDir "InternetLock-win-x64.zip"

if (-not (Test-Path $CsprojPath)) {
    Write-Error "Project file not found at '$CsprojPath'!"
    exit 1
}

# Clean previous build artifacts safely
if (Test-Path $ArtifactsDir) {
    Write-Host "Cleaning previous artifacts directory..." -ForegroundColor Yellow
    Remove-Item -Path $ArtifactsDir -Recurse -Force
}

New-Item -ItemType Directory -Path $TempPublishDir -Force | Out-Null

Write-Host "Restoring dependencies..." -ForegroundColor Green
dotnet restore $CsprojPath
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet restore failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "Publishing self-contained win-x64 Release..." -ForegroundColor Green
dotnet publish $CsprojPath `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    --output $TempPublishDir

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

$ExePath = Join-Path $TempPublishDir "InternetLock.exe"
if (-not (Test-Path $ExePath)) {
    Write-Warning "Single-file executable was not found directly in output. Packaging full publish folder..."
}

Write-Host "Packaging build into ZIP archive..." -ForegroundColor Green
Compress-Archive -Path "$TempPublishDir\*" -DestinationPath $OutputZipPath -Force

# Clean up temp publish directory
Remove-Item -Path $TempPublishDir -Recurse -Force

if (Test-Path $OutputZipPath) {
    Write-Host "==========================================" -ForegroundColor Green
    Write-Host " BUILD SUCCESSFUL! " -ForegroundColor Green
    Write-Host " Artifact generated at:" -ForegroundColor White
    Write-Host " $OutputZipPath" -ForegroundColor Cyan
    Write-Host "==========================================" -ForegroundColor Green
} else {
    Write-Error "Failed to generate ZIP archive at '$OutputZipPath'."
    exit 1
}
