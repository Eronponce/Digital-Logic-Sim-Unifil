param(
    [ValidateSet("dev", "release")]
    [string]$Configuration = "release",

    [switch]$Community
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
$logDir = Join-Path $repoRoot "Builds\Logs"
$logPath = Join-Path $logDir "build-mac.log"
$lockFilePath = Join-Path $repoRoot "Temp\UnityLockfile"

$unityCandidates = @(
    $env:UNITY_EXE,
    "C:\Program Files\Unity 6000.0.46f1\Editor\Unity.exe",
    "C:\Eron_Lab\Unity\6000.0.46f1\Editor\Unity.exe",
    "C:\Program Files\Unity\Hub\Editor\6000.0.46f1\Editor\Unity.exe"
) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

$unityExe = $unityCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $unityExe) {
    throw "Unity 6000.0.46f1 was not found. Set UNITY_EXE or install the editor in a standard path."
}

$macSupportCandidates = @(
    (Join-Path (Split-Path -Parent $unityExe) "Data\PlaybackEngines\MacStandaloneSupport"),
    "C:\Program Files\Unity 6000.0.46f1\Editor\Data\PlaybackEngines\MacStandaloneSupport",
    "C:\Eron_Lab\Unity\6000.0.46f1\Editor\Data\PlaybackEngines\MacStandaloneSupport",
    "C:\Program Files\Unity\Hub\Editor\6000.0.46f1\Editor\Data\PlaybackEngines\MacStandaloneSupport"
) | Select-Object -Unique

$macSupportDir = $macSupportCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $macSupportDir) {
    throw "Mac Build Support (Mono) is not installed for Unity 6000.0.46f1. Install the mac-mono module and run build-mac.bat again."
}

# Use the Unity exe from the same installation as the Mac support module
$macUnityExe = Join-Path (Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $macSupportDir))) "Unity.exe"
if (Test-Path $macUnityExe) {
    $unityExe = $macUnityExe
}

New-Item -ItemType Directory -Force -Path $logDir | Out-Null

if (Test-Path $lockFilePath) {
    throw "The project is open in the Unity Editor. Close Unity before running the batch build."
}

$executeMethod = if ($Community) {
    "DLS.EditorTools.LocalBuildScript.BuildMacPlayerCommunityRelease"
} elseif ($Configuration -eq "release") {
    "DLS.EditorTools.LocalBuildScript.BuildMacPlayerRelease"
} else {
    "DLS.EditorTools.LocalBuildScript.BuildMacPlayerDev"
}

Write-Host "Using Unity: $unityExe"
Write-Host "Mac support: $macSupportDir"
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
    throw "Unity Mac build failed with exit code $($process.ExitCode). See $logPath"
}

$appPath = Join-Path $repoRoot "Builds\Mac\Digital-Logic-Sim-Unifil.app"
Write-Host "Mac build completed: $appPath"
