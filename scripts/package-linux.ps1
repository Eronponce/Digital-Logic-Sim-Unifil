param(
    [ValidateSet("dev", "release")]
    [string]$Configuration = "release",

    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
$buildDir = Join-Path $repoRoot "Builds\Linux"
$releaseDir = Join-Path $repoRoot "Builds\Release"
$binaryPath = Join-Path $buildDir "Digital-Logic-Sim-Unifil.x86_64"
$version = "2.1.7"
$archiveRootName = "Digital-Logic-Sim-Unifil-Linux-v$version"
$stageDir = Join-Path $releaseDir $archiveRootName
$zipPath = Join-Path $releaseDir "$archiveRootName.zip"
$readmePath = Join-Path $stageDir "README-Linux.txt"

if (-not $SkipBuild) {
    & (Join-Path $repoRoot "build-linux.bat") $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "Linux build failed with exit code $LASTEXITCODE."
    }
}

if (-not (Test-Path $binaryPath)) {
    throw "Linux build output was not found: $binaryPath"
}

New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null

if (Test-Path $stageDir) {
    Remove-Item -LiteralPath $stageDir -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $stageDir | Out-Null
Copy-Item -Path (Join-Path $buildDir "*") -Destination $stageDir -Recurse -Force

$readme = @(
    "Digital Logic Sim Unifil - Linux build",
    "",
    "1. Extract this package.",
    "2. Open a terminal in the extracted folder.",
    "3. If needed, run: chmod +x Digital-Logic-Sim-Unifil.x86_64",
    "4. Start the app with: ./Digital-Logic-Sim-Unifil.x86_64",
    "",
    "If the system blocks execution, also verify the executable permission on the main binary."
) -join [Environment]::NewLine

Set-Content -Path $readmePath -Value $readme -Encoding ASCII

if (Test-Path $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -Path $stageDir -DestinationPath $zipPath -CompressionLevel Optimal
Remove-Item -LiteralPath $stageDir -Recurse -Force

Write-Host "Linux release zip: $zipPath"
