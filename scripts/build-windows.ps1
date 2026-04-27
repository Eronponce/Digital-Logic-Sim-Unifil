param(
    [ValidateSet("dev", "release")]
    [string]$Configuration = "dev"
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
$packagesDir = Join-Path $repoRoot "GooglePackages"
$logDir = Join-Path $repoRoot "Builds\\Logs"
$logPath = Join-Path $logDir "build-windows.log"
$lockFilePath = Join-Path $repoRoot "Temp\\UnityLockfile"

$unityCandidates = @(
    $env:UNITY_EXE,
    "C:\\Program Files\\Unity 6000.0.46f1\\Editor\\Unity.exe",
    "C:\\Eron_Lab\\Unity\\6000.0.46f1\\Editor\\Unity.exe",
    "C:\\Program Files\\Unity\\Hub\\Editor\\6000.0.46f1\\Editor\\Unity.exe"
) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

$unityExe = $unityCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $unityExe) {
    throw "Unity 6000.0.46f1 was not found. Set UNITY_EXE or install the editor in a standard path."
}

New-Item -ItemType Directory -Force -Path $packagesDir | Out-Null
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

$firebasePackages = @(
    @{
        FileName = "com.google.external-dependency-manager-1.2.186.tgz"
        Url = "https://dl.google.com/games/registry/unity/com.google.external-dependency-manager/com.google.external-dependency-manager-1.2.186.tgz"
    },
    @{
        FileName = "com.google.firebase.app-13.9.0.tgz"
        Url = "https://dl.google.com/games/registry/unity/com.google.firebase.app/com.google.firebase.app-13.9.0.tgz"
    },
    @{
        FileName = "com.google.firebase.auth-13.9.0.tgz"
        Url = "https://dl.google.com/games/registry/unity/com.google.firebase.auth/com.google.firebase.auth-13.9.0.tgz"
    },
    @{
        FileName = "com.google.firebase.firestore-13.9.0.tgz"
        Url = "https://dl.google.com/games/registry/unity/com.google.firebase.firestore/com.google.firebase.firestore-13.9.0.tgz"
    }
)

foreach ($package in $firebasePackages) {
    $destination = Join-Path $packagesDir $package.FileName
    if (-not (Test-Path $destination)) {
        Write-Host "Downloading $($package.FileName)..."
        curl.exe -L --fail --output $destination $package.Url
    }
}

if (Test-Path $lockFilePath) {
    throw "The project is open in the Unity Editor. Close Unity before running the batch build."
}

$executeMethod = if ($Configuration -eq "release") {
    "DLS.EditorTools.LocalBuildScript.BuildWindowsPlayerRelease"
} else {
    "DLS.EditorTools.LocalBuildScript.BuildWindowsPlayerDev"
}

Write-Host "Using Unity: $unityExe"
Write-Host "Build configuration: $Configuration"
Write-Host "Build log: $logPath"

$process = Start-Process `
    -FilePath $unityExe `
    -ArgumentList @(
        "-batchmode",
        "-quit",
        "-projectPath", $repoRoot,
        "-executeMethod", $executeMethod,
        "-logFile", $logPath
    ) `
    -Wait `
    -PassThru

if ($process.ExitCode -ne 0) {
    throw "Unity build failed with exit code $($process.ExitCode). See $logPath"
}

$exePath = Join-Path $repoRoot "Builds\\Windows\\Digital-Logic-Sim-Unifil.exe"
Write-Host "Build completed: $exePath"
