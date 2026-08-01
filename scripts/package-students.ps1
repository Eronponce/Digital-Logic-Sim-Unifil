param(
    [ValidateSet("dev", "release")]
    [string]$Configuration = "release",

    [switch]$SkipBuild,
    [switch]$SkipInnoInstall,
    [switch]$Community
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
$buildDir = Join-Path $repoRoot "Builds\Windows"
$installerDir = Join-Path $repoRoot "Builds\Installer"
$releaseDir = Join-Path $repoRoot "Builds\Release"
$innoScript = Join-Path $repoRoot "installer\digital-logic-sim-unifil.iss"
$appExe = Join-Path $buildDir "Digital-Logic-Sim-Unifil.exe"

function Find-InnoCompiler {
    $candidates = @(
        $env:ISCC_EXE,
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

    return $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if (-not $SkipBuild) {
    $buildArgs = @($Configuration)
    if ($Community) { $buildArgs += "-Community" }
    & (Join-Path $repoRoot "build-windows.bat") @buildArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Unity build failed with exit code $LASTEXITCODE."
    }
}

if (-not (Test-Path $appExe)) {
    throw "Build output was not found: $appExe"
}

New-Item -ItemType Directory -Force -Path $installerDir | Out-Null
New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null

$env:DLS_BUILD_SOURCE_DIR = $buildDir
$env:DLS_APP_VERSION = "2.3.0"

$versionSuffix = "v$($env:DLS_APP_VERSION)"
$variant = if ($Community) { "community" } else { "turma" }
$portableRootName = "Digital-Logic-Sim-Unifil-Windows-$versionSuffix-$variant"
$portableStageDir = Join-Path $releaseDir $portableRootName
$portableZipPath = Join-Path $releaseDir "$portableRootName.zip"

if (-not $Community) {
    $isccExe = Find-InnoCompiler
    if (-not $isccExe -and -not $SkipInnoInstall) {
        Write-Host "Inno Setup not found. Installing it with winget..."
        winget install --id JRSoftware.InnoSetup --exact --silent --accept-source-agreements --accept-package-agreements
        $isccExe = Find-InnoCompiler
    }

    if (-not $isccExe) {
        throw "Inno Setup compiler was not found. Install Inno Setup 6 or set ISCC_EXE, then run package-students.bat again."
    }

    Write-Host "Using Inno Setup: $isccExe"
    Write-Host "Installer script: $innoScript"
    Write-Host "Build source: $buildDir"

    & $isccExe $innoScript
    if ($LASTEXITCODE -ne 0) {
        throw "Installer build failed with exit code $LASTEXITCODE."
    }

    $setupExe = Join-Path $installerDir "DigitalLogicSim-Unifil-Setup.exe"
    $releaseSetupExe = Join-Path $releaseDir "DigitalLogicSim-Unifil-Setup-$versionSuffix-turma.exe"

    if (Test-Path $releaseSetupExe) {
        Remove-Item -LiteralPath $releaseSetupExe -Force
    }
    Copy-Item -LiteralPath $setupExe -Destination $releaseSetupExe
    Write-Host "Release installer: $releaseSetupExe"
}

if (Test-Path $portableStageDir) {
    Remove-Item -LiteralPath $portableStageDir -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $portableStageDir | Out-Null
Copy-Item -Path (Join-Path $buildDir "*") -Destination $portableStageDir -Recurse -Force

if ($Community) {
    $credentialFile = Join-Path $portableStageDir "Digital-Logic-Sim-Unifil_Data\StreamingAssets\google-services-desktop.json"
    if (Test-Path $credentialFile) {
        Remove-Item -LiteralPath $credentialFile -Force
        Write-Host "Stripped google-services-desktop.json from community build."
    }
}

if (Test-Path $portableZipPath) {
    Remove-Item -LiteralPath $portableZipPath -Force
}

Compress-Archive -Path $portableStageDir -DestinationPath $portableZipPath -CompressionLevel Optimal
Remove-Item -LiteralPath $portableStageDir -Recurse -Force

Write-Host "Release zip: $portableZipPath"
