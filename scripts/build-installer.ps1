# Build a local Velopack installer for CoffeeUpdater.
# Usage:
#   .\scripts\build-installer.ps1 [-Version <ver>] [-SigningMetadata <path>] [-SkipDefenderExclusions]
#
# If version is omitted, inferred from RelaxVersioner (rv show).
# Output goes to .\Releases\

param(
    [string]$Version,
    [string]$SigningMetadata,
    [switch]$SkipDefenderExclusions
)

$ErrorActionPreference = "Stop"

# Load .env file if present (for local Azure Trusted Signing credentials)
$EnvFile = Join-Path (Split-Path -Parent $PSScriptRoot) ".env"
if (Test-Path $EnvFile) {
    Write-Host "==> Loading .env file"
    Get-Content $EnvFile | ForEach-Object {
        if ($_ -match '^\s*([^#][^=]+?)\s*=\s*(.+?)\s*$') {
            [Environment]::SetEnvironmentVariable($Matches[1], $Matches[2], "Process")
        }
    }
}

if (-not $Version) {
    if (Get-Command rv -ErrorAction SilentlyContinue) {
        $Version = (rv.exe show 2>$null).Trim()
    }
    if (-not $Version) {
        Write-Error "Could not infer version. Install RelaxVersioner CLI (dotnet tool install -g rv-cli) or pass -Version explicitly."
        exit 1
    } else {
        Write-Host "==> Version inferred from RelaxVersioner: $Version"
    }
}

$RootDir = Split-Path -Parent $PSScriptRoot
$PackDir = Join-Path $RootDir "build\publish\CoffeeUpdater\release"
$ReleaseDir = Join-Path $RootDir "Releases"
$IconPath = Join-Path $RootDir "images\icon-256.ico"

Write-Host "==> Building installer for CoffeeUpdater v$Version"

# Ensure vpk is installed
if (-not (Get-Command vpk -ErrorAction SilentlyContinue)) {
    Write-Host "==> Installing Velopack CLI (vpk)..."
    dotnet tool install -g vpk
}

Write-Host "==> Restoring dependencies..."
dotnet restore $RootDir
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "==> Building (Release)..."
dotnet build $RootDir --no-restore -c Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "==> Publishing..."
dotnet publish "$RootDir\src\CoffeeUpdater\CoffeeUpdater.csproj" -c Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Velopack's unsigned Setup.exe triggers Windows Defender false positives
# (Program:Win32/Contebrew.A!ml). Exclude both the output dir and vpk's
# temp dir so Defender doesn't quarantine the Setup.exe during packing.
if (-not $SkipDefenderExclusions) {
    $VpkTempDir = Join-Path $env:TEMP "Velopack"
    $pathsToExclude = @($ReleaseDir, $VpkTempDir)
    Write-Host "==> Ensuring Windows Defender exclusions..."
    $script = ($pathsToExclude | ForEach-Object { "Add-MpPreference -ExclusionPath '$_'" }) -join "; "
    $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($script))
    Start-Process powershell -Verb RunAs -Wait -ArgumentList "-EncodedCommand $encoded"
}

$vpkArgs = @(
    "pack",
    "--packId", "CoffeeUpdater",
    "--packVersion", $Version,
    "--packDir", $PackDir,
    "--mainExe", "CoffeeUpdater.exe",
    "--noPortable",
    "--icon", $IconPath
)

if ($SigningMetadata) {
    Write-Host "==> Code signing enabled (Azure Trusted Signing)"
    $vpkArgs += "--azureTrustedSignFile"
    $vpkArgs += $SigningMetadata
}

Write-Host "==> Packing with Velopack..."
vpk @vpkArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "==> Done! Installer artifacts are in:"
Get-ChildItem $ReleaseDir | Format-Table Name, Length -AutoSize
